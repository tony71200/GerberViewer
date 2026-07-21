# Flow Tab 3 — Align and Stitching

## 1. Mục tiêu

Tab 3 nhận:

- `sample_manifest.json` được tạo từ Tab 2
- thư mục ảnh camera
- cấu hình alignment và stitching

Sau đó chương trình:

1. Sắp xếp ảnh camera.
2. Map ảnh camera với sample tile.
3. Align từng ảnh camera với sample tile tương ứng.
4. Recovery các ảnh align thất bại.
5. Tính global pose.
6. Stitch ảnh.
7. Xuất TIFF hoặc BigTIFF.

---

## 2. Thành phần chính

### UI

- `btnOpenImageFolder`
- `txtImageFolder`
- `lstCapturedImages`
- `lblImageCount`
- `btnRunAlignStitch`
- `btnCancelAlignStitch`
- `prgAlignStitch`
- `resultTabControl`
- `orderView`
- `stitchedImageWindow`
- `alignConfigGrid`

### Dữ liệu

- `SampleManifest`
- `SampleTileInfo`
- `CapturedImageInfo`
- `AlignStitchConfig`
- `SampleAlignmentResult`
- `StitchImagePose`
- `ProcessingReport`

### Dịch vụ

- `SampleManifestReader`
- `CapturedImageLoader`
- `NaturalFileNameComparer`
- `SampleAligner`
- `NeighborAligner`
- `PoseRecoveryService`
- `AbsolutePoseStitcher`
- `ProcessingReportWriter`

---

## 3. Flow khởi tạo Tab 3

```text
AlignStitchingControl được tạo
        ↓
Đọc WorkflowContext
        ↓
Nếu có SampleManifestPath
        ↓
Load manifest
        ↓
Validate manifest
        ↓
Hiển thị:
    - Rows
    - Columns
    - CropOrder
    - StartOrder
    - Overlap
    - ExpectedTileCount
        ↓
Khởi tạo Order View
        ↓
Trạng thái Ready
```

Nếu chưa có manifest:

```text
Disable Run
        ↓
Hiển thị yêu cầu hoàn thành Tab 2
```

---

## 4. Flow mở thư mục ảnh camera

```text
Người dùng nhấn Open Image Folder
        ↓
FolderBrowserDialog
        ↓
Enumerate:
    - BMP
    - PNG
    - JPG/JPEG
    - TIF/TIFF
        ↓
Natural sort filename
        ↓
Validate từng file
        ↓
Đọc width, height, channel, bit depth
        ↓
So sánh số lượng với manifest
        ↓
Gán OrderIndex 0..N-1
        ↓
Map ảnh K với sample tile K
        ↓
Hiển thị danh sách và Order View
```

Quy tắc bắt buộc:

```text
CapturedImages[OrderIndex K]
        ↕
SampleManifest.Tiles[OrderIndex K]
```

Không remap bằng:

- Robot coordinate
- `PositionId`
- Nearest content
- List selection
- Filename metadata tùy ý

Natural sort phải hoàn tất trước khi gán `OrderIndex`.

---

## 5. Validate trước khi chạy

```text
Sample manifest tồn tại
Captured folder tồn tại
Image count = tile count
Không có duplicate natural-sort key
Tất cả ảnh đọc được
OutputPath ghi được
HALCON sẵn sàng
OpenCV runtime sẵn sàng
Config alignment hợp lệ
```

Nếu một điều kiện thất bại, block execution và chỉ rõ file hoặc index lỗi.

---

## 6. Preprocess theo modality

### Sample tile

Đặc điểm:

- Binary
- Nền đơn giản
- Không có ánh sáng thật

Pipeline:

```text
Load sample tile
        ↓
Grayscale
        ↓
Optional invert
        ↓
Threshold
        ↓
Edge extraction
        ↓
Gerber-content mask
```

### Captured image

Đặc điểm:

- Grayscale hoặc color
- Có ánh sáng
- Noise
- Texture vật liệu
- Có thể khác polarity

Pipeline:

```text
Load captured image
        ↓
Grayscale
        ↓
Normalize contrast
        ↓
Try normal/inverted polarity
        ↓
Fixed/Otsu/adaptive threshold
        ↓
Edge extraction
        ↓
Valid image mask
```

Không ghi đè ảnh input.

---

## 7. Direct sample alignment

Với mỗi `OrderIndex K`:

```text
SampleTile[K]
CapturedImage[K]
        ↓
Preprocess
        ↓
HALCON NCC coarse localization
        ↓
Validate NCC score và pose
        ↓
Chuyển NCC pose sang OpenCV transform
        ↓
Pyramid ECC refinement
        ↓
Validate:
    - correlation
    - translation
    - rotation
    - overlap
    - matrix finite
        ↓
Accept hoặc Reject
```

Default:

```text
NCC → Pyramid ECC
```

Policy:

- NCC pass + ECC pass → dùng ECC pose.
- NCC pass + ECC fail → chỉ dùng NCC khi score và geometry vẫn pass.
- NCC fail → có thể thử Pyramid ECC từ expected-grid initialization.
- Tất cả direct method fail → chuyển recovery.

---

## 8. Tính global pose

Với sample alignment thành công:

```text
SampleGlobalTranslation =
    Translation(tile.CropX, tile.CropY)

CapturedGlobalPose =
    SampleGlobalTranslation
    × CapturedToSampleTransform
```

Chuẩn nội bộ:

- Matrix `3 × 3`
- Type `CV_64F`
- Direction thống nhất: `Captured → Sample`
- Không trộn `Sample → Captured`

---

## 9. Recovery pipeline

Thứ tự bắt buộc:

```text
A. Neighbor alignment
        ↓
B. Anchor adjustment / interpolation
        ↓
C. Expected-grid pose
        ↓
D. Manual adjustment
```

### A. Neighbor alignment

```text
Chọn neighbor đã có global pose
        ↓
Captured B align với Captured A
        ↓
Kiểm tra pairResult.Eval.IsMatch == true
        ↓
GlobalPoseB = GlobalPoseA × TransformBToA
```

Ưu tiên:

1. Traversal predecessor.
2. Horizontal/vertical neighbor tốt nhất.
3. Traversal successor trong second pass.

### B. Anchor adjustment / interpolation

```text
Xác định valid anchor trước và sau
        ↓
Nội suy translation
        ↓
Nội suy rotation
        ↓
Điều chỉnh theo grid origin
        ↓
Validate deviation
```

Pose source:

- `Interpolated`
- `AnchorAdjusted`

### C. Expected-grid pose

```text
Sample tile origin
Image dimensions
Overlap
Expected step
        ↓
Deterministic expected pose
```

Pose source:

- `ExpectedGridOffset`

Không đánh dấu là direct alignment success.

### D. Manual adjustment

Dialog phải hỗ trợ:

- Sample view
- Captured view
- Alpha overlay
- Difference view
- Translate X/Y
- Rotation
- Keyboard nudge
- Reset expected-grid
- Reset automatic candidate
- Accept
- Skip
- Cancel Run

Pose source:

- `Manual`
- `Excluded`

---

## 10. Status của từng ảnh

```text
Pending
Processing
SampleAlignOk
NeighborAlignOk
Interpolated
AnchorAdjusted
ExpectedGridOffset
Manual
Failed
Excluded
```

Mỗi status phải hiển thị trong:

- `lstCapturedImages`
- Order View
- Processing report

---

## 11. Stitching flow

```text
Danh sách StitchImagePose
        ↓
Loại Excluded nếu policy yêu cầu
        ↓
Tính transformed bounds
        ↓
Tính canvas translation
        ↓
Tạo preview canvas có giới hạn megapixel
        ↓
Warp từng ảnh
        ↓
Warp valid mask
        ↓
Blend overlap
        ↓
Update preview mỗi N ảnh
        ↓
Tính output full-resolution
        ↓
Chọn TIFF hoặc BigTIFF
        ↓
Ghi output
```

Preview scale không được làm thay đổi output full-resolution.

---

## 12. Output

```text
<RunOutput>/
├─ stitched_output.tif
├─ processing_report.json
├─ processing_log.txt
└─ diagnostics/
   ├─ sample_000.png
   ├─ captured_000.png
   ├─ warped_000.png
   └─ difference_000.png
```

---

## 13. Processing report

Mỗi ảnh phải ghi:

- Tile ID
- Row
- Column
- OrderIndex
- Sample path
- Captured path
- Method attempted
- Method selected
- Success/failure
- NCC score
- ECC correlation
- PairEval
- Transform matrix
- Translation
- Rotation
- Pose source
- Fallback reason
- Processing time

---

## 14. Threading

- Alignment và stitching chạy ngoài UI thread.
- Không truy cập WinForms từ worker.
- Dùng `CancellationToken`.
- Dùng `IProgress<T>`.
- Disable control xung đột khi chạy.
- Restore UI trong `finally`.
- Dispose `Mat`, `Bitmap`, `HObject`, `HTuple`, NCC model handle.

---

## 15. State machine

```text
Idle
  ↓
ManifestReady
  ↓
ImagesLoaded
  ↓
Validated
  ↓
Aligning
  ↓
Recovering
  ↓
Stitching
  ├─ Completed
  ├─ Cancelled
  └─ Failed
```

---

## 16. Checklist kiểm thử

### Input mapping

- [ ] Natural sort đúng: 1, 2, 10.
- [ ] Image K map đúng Sample K.
- [ ] Count mismatch bị block.
- [ ] Duplicate sort key bị block.

### Alignment

- [ ] `Eval.IsMatch == false` không được đánh dấu thành công.
- [ ] Matrix direction thống nhất.
- [ ] NCC below threshold bị reject.
- [ ] ECC correlation below threshold bị reject.
- [ ] Global pose dùng tile origin.

### Recovery

- [ ] Neighbor chạy trước interpolation.
- [ ] Interpolation chạy trước expected-grid.
- [ ] Expected-grid chạy trước manual.
- [ ] Pose source được ghi đúng.
- [ ] Không giả expected-grid thành manual success.

### Stitching

- [ ] Canvas bounds đúng.
- [ ] Preview update không freeze UI.
- [ ] Output full-resolution độc lập preview.
- [ ] TIFF/BigTIFF được chọn đúng.
- [ ] Cancel giải phóng file handle.
