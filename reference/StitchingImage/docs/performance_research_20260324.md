<!-- [Codex] [Change time: 260324] [Research and proposal for performance, memory, and configuration unification in StitchingImage] -->

# Nghiên cứu nâng cấp hiệu năng StitchingImage (2026-03-24)

## Phạm vi

Tài liệu này tổng hợp hiện trạng và đề xuất cho các yêu cầu:
- PathCanvasControl load chậm với số lượng node lớn.
- HALCON stitching xong chưa giải phóng đủ tài nguyên, lượt chạy sau dễ tăng RAM/oom.
- Đồng bộ lưu cấu hình AppSettings về YAML/JSON.
- Đề xuất sắp xếp các giá trị trong AppSettings + phần còn thiếu (chỉ đề xuất, chưa code).
- Hướng stitching bằng HALCON tốn ít bộ nhớ hơn.

---

## 1) PathCanvasControl chậm khi nhiều node

### Hiện trạng kỹ thuật

Trong `PathCanvasControl`, mỗi lần `Paint` đang làm nhiều bước O(N) và có đoạn thành O(N^2):
- Gom toàn bộ node từ traversal + arrange, group theo `ImageId`.
- Vẽ label cho tất cả node bằng `DrawString`.
- Ở `DrawArrangeSegments`, mỗi `PathSegment` đều `FirstOrDefault` trên `comp.Items` để tìm from/to.

Điểm nghẽn chính với dataset lớn (5k-50k node) là:
1. Tìm from/to theo tuyến tính cho từng segment (`FirstOrDefault`) -> tăng mạnh CPU.
2. Vẽ text label cho mọi node -> GDI+ draw text rất tốn thời gian.
3. Mỗi repaint đều recompute map/bounds/virtual coords từ đầu.
4. `SmoothingMode.AntiAlias` áp cho toàn bộ canvas làm tăng cost.

### Đề xuất tối ưu (ưu tiên theo tác động)

**P1 - Index lookup O(1)**
- Tạo dictionary `id -> ImageInfo` 1 lần mỗi component trước vòng segment.
- Tránh `FirstOrDefault` trong inner-loop.

**P1 - Adaptive rendering theo zoom / số node**
- Nếu node > ngưỡng (ví dụ 1500) thì:
  - tắt label mặc định;
  - chỉ hiển thị label cho node được chọn/hover;
  - giảm kích thước marker.
- Thêm chế độ “Performance Mode” cho canvas.

**P1 - Dirty cache**
- Cache kết quả map/bounds/virtual coords theo hash dữ liệu + kích thước canvas.
- Chỉ rebuild khi data thay đổi (SetData) hoặc resize.

**P2 - LOD (Level of Detail)**
- Khi zoom out: gộp node theo grid cell, vẽ density thay vì từng node.
- Khi zoom in: vẽ đầy đủ node/label.

**P2 - Giảm chi phí GDI+**
- AntiAlias cho edges chính, marker dùng `SmoothingMode.None`.
- Dùng `TextRenderer.DrawText` cho label ngắn trên WinForms để giảm overhead.

**P3 - Background prepare**
- Tiền xử lý layout/segment list ở thread nền, UI thread chỉ render snapshot immutable.

### KPI khuyến nghị
- 10k node: `Paint` p95 < 80ms.
- 30k node: p95 < 140ms ở chế độ Performance.
- Không lock UI > 250ms khi toggle checkbox.

---

## 2) HALCON stitching xong chưa giải phóng tài nguyên (rò RAM/peak RAM)

### Hiện trạng kỹ thuật

`StitchHalcon(...)` đã có `finally` dispose `mosaic`, `images`, `poseFull`, `usedEdgeMatricesFull`.
Tuy nhiên vẫn có rủi ro tăng RAM cao ở luồng dài:

1. **HTuple tăng dần bằng `TupleConcat` trong loop**
   - `mappingSource`, `mappingDest`, `homMatrices2D` được nối lặp nhiều lần.
   - Pattern này tạo nhiều object trung gian, áp lực GC lớn.

2. **Nạp tất cả ảnh vào `images` trước khi stitch**
   - `ConcatObj(images, img, out tmp)` lặp cho toàn bộ tile => memory peak cao.

3. **Compose scale chưa đủ thấp cho bộ ảnh lớn**
   - Dù có `composeScale`, nếu nhiều tile thì tổng dữ liệu trong HALCON object vẫn cao.

4. **Ngoại lệ bị nuốt**
   - `catch` chỉ log rồi trả `result` có thể `null`, vòng ngoài có thể không reset một số state theo nhánh thành công/thất bại.

### Đề xuất kỹ thuật để ổn định giải phóng và giảm peak RAM

**P1 - Giảm memory peak trước**
- Hạ `composeScale` theo tổng pixels ước lượng của toàn batch (không chỉ root image).
- Giới hạn cứng tổng input megapixel cho HALCON path, vượt ngưỡng thì fallback qua OpenCV path hoặc chia batch.

**P1 - Chunk mosaic / hierarchical mosaic**
- Thay vì 1 mosaic toàn cục: stitch theo cụm (component/row chunk), lưu tạm TIFF/PNG, sau đó ghép cấp 2.
- Ưu điểm: memory peak tuyến tính theo chunk, không theo toàn bộ dataset.

**P1 - Quản lý vòng đời tuple/object chặt hơn**
- Tránh `TupleConcat` trong vòng lớn; chuẩn bị mảng index/homography trước rồi build `HTuple` theo block.
- Dọn tuple/object ngay sau `GenProjectiveMosaic`.

**P2 - Tăng tính an toàn lifecycle**
- Dùng vùng guard rõ ràng cho mỗi tile read/scale/concat, đảm bảo dispose ở cả nhánh lỗi.
- Bổ sung log peak memory mỗi giai đoạn (read, concat, mosaic, save) để truy nguồn tăng RAM.

**P2 - Sau mỗi run**
- Trigger compact GC theo policy cho chế độ long-running batch (`GC.Collect`, `WaitForPendingFinalizers`, `GC.Collect`) ở điểm an toàn (không mỗi tile).

### Check-list quan sát runtime
- Memory before/after từng batch.
- Số tile đã read.
- Tổng megapixel input sau scale.
- Kích thước mosaic output.
- Thời gian xử lý từng phase.

---

## 3) Đồng bộ AppSettings về YAML/JSON

### Hiện trạng

`AppSettings` đang lưu phân tán:
- `last_folder.txt`
- `filename_pattern.txt`
- `node_interval.txt`
- `match_config.yaml`
- `system_config.yaml`
- hỗ trợ legacy `match_config.txt`

### Đề xuất chuẩn hóa

**Mục tiêu**: 1 file cấu hình chính + tương thích ngược.

Phương án khuyến nghị:
1. Tạo `app_settings.yaml` (hoặc `app_settings.json`) chứa:
   - `lastFolderPath`
   - `filenamePattern`
   - `nodeInterval`
   - `matchConfig` (embed object)
   - `systemConfig` (embed object)
2. Giữ đọc legacy file cũ trong 1-2 phiên bản.
3. Khi save:
   - ghi file unified;
   - (tuỳ chọn) không ghi file cũ nữa sau khi migrate thành công.
4. Thêm version field: `schemaVersion: 1` để dễ evolve.

### Nên chọn YAML hay JSON?
- **YAML**: phù hợp vì repo đã có `match_config.yaml`, `system_config.yaml`.
- **JSON**: parser chuẩn, ít lỗi format thủ công hơn.

Khuyến nghị ngắn hạn: **YAML thống nhất** để migration ít rủi ro.

---

## 4) Đề xuất sắp xếp AppSettings + phần còn thiếu (không code)

### 4.1 Nhóm cấu hình đề xuất cho UI

1. **General**
   - LastFolderPath
   - FilenamePattern
   - AutoLoadLastFolder
   - AutoLoadFolder

2. **Connectivity**
   - RobotHost
   - RobotPort
   - ConnectAttempts
   - ConnectDelaySeconds

3. **Ordering/Layout**
   - ClusterOrderMode
   - OrderMode
   - StartCorner
   - RobotMovement
   - InvertX
   - NodeInterval
   - GapFactor
   - GapRow

4. **Matching**
   - Method, ORB/SIFT params
   - RANSAC params
   - overlap/inlier/rmse constraints
   - manual offset / phase correlation tuning

5. **Stitch Output**
   - SaveMode
   - ComposeMegapix
   - MaxCanvasMegapix
   - output format policy

6. **Advanced/Fallback**
   - UseRigidForGlobal
   - fallback offsets (H/V)
   - memory profile / chunk size

### 4.2 Giá trị đang thiếu trong config (hiện bị hard-code)

Trong `BuildRunConfig()` hiện có nhiều giá trị hard-code, nên đưa vào config để user chỉnh được:
- `UseRigidForGlobal = true`
- `MaxCanvasMegapix = 250`
- `FallbackOffsetHorizontal = (-15, -339)`
- `FallbackOffsetVertical = (-311, -13)`
- Các override matchCfg runtime:
  - `WorkMegapix = 10`
  - `CoarseWorkMegapix = 2`
  - `FineWorkMegapix = 10`
  - `RansacThresh = 5`
  - `EnforceRobotDirection = true`
  - `MaxPerpOffsetPx = 10`
  - `PreferPerpOffsetConstraint = true`

Đề xuất: thêm nhóm `runtimeOverrides` trong config, bật/tắt bằng cờ `enableRuntimeOverrides`.

---

## 5) Hướng HALCON stitching ít tốn bộ nhớ hơn

### Chiến lược A - Multi-stage mosaic (khuyến nghị)
- Stage 1: stitch theo chunk (row/component), output intermediate.
- Stage 2: stitch các intermediate.
- Có thể kết hợp downscale động theo ngân sách RAM.

**Ưu điểm**: giảm peak RAM mạnh, dễ kiểm soát lỗi theo chunk.

### Chiến lược B - Streaming/Windowed mosaic
- Chỉ giữ vùng đang xử lý và vùng overlap cần thiết.
- Flush block ra đĩa theo tile.

**Ưu điểm**: phù hợp dataset cực lớn; **nhược**: cài đặt phức tạp.

### Chiến lược C - Hybrid HALCON + OpenCV
- HALCON để estimate global transform tốt.
- OpenCV để warp/blend + ghi big image theo tile (BigTIFF).

**Ưu điểm**: tận dụng điểm mạnh mỗi engine, tiết kiệm RAM khi ghi file lớn.

---

## 6) Lộ trình triển khai gợi ý

### Sprint 1 (an toàn, tác động nhanh)
- Tối ưu PathCanvasControl (index O(1), tắt label adaptive, cache dirty).
- Bổ sung memory telemetry cho `StitchHalcon`.
- Thiết kế schema `app_settings.yaml` + loader tương thích ngược.

### Sprint 2 (ổn định runtime)
- Triển khai chunked mosaic cho HALCON path.
- Chuyển hard-coded runtime params vào config.
- Thêm profile cấu hình theo RAM máy (Low/Medium/High).

### Sprint 3 (nâng cao)
- LOD/virtualization cho canvas lớn.
- Hybrid HALCON + tiled writer pipeline.
- Benchmark tự động và dashboard p95/p99.

---

## 7) Tóm tắt quyết định đề xuất

1. PathCanvasControl: ưu tiên tối ưu thuật toán vẽ + adaptive label trước khi đổi framework UI.
2. HALCON: giải quyết peak RAM bằng chunked/hierarchical mosaic là đòn bẩy lớn nhất.
3. AppSettings: gom về 1 file YAML có version + migrate legacy.
4. Mở config cho toàn bộ runtime hard-code để user chủ động tuning.
5. Với batch lớn: áp dụng memory budget ngay từ đầu pipeline (input MP cap + dynamic scale).
