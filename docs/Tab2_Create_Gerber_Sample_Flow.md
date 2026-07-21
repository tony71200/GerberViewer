# Flow Tab 2 — Create Gerber Sample

## 1. Mục tiêu

Tab 2 nhận một ảnh raster bên ngoài đại diện cho Gerber sample, hiển thị ảnh, chia ảnh thành lưới theo cấu hình, xác định thứ tự duyệt, sau đó cắt và lưu từng sample tile.

Nguồn ảnh hỗ trợ tối thiểu:

- PNG
- BMP
- TIFF
- BigTIFF nếu decoder hiện tại hỗ trợ

Tab 2 không lấy trực tiếp ảnh đang render ở Tab 1 trong phiên bản hiện tại.

---

## 2. Thành phần chính

### 2.1. UI

Các control đề xuất theo thứ tự cố định:

1. `btnOpenSample`
2. `txtSamplePath`
3. `btnLoadConfig`
4. `btnSaveConfig`
5. `btnCreateSample`
6. `btnCancelCreateSample`
7. `prgCreateSample`
8. `lblCreateSampleStatus`

Khu vực chính:

- Bên trái: `sampleWindow`
- Bên phải: `sampleConfigGrid`

### 2.2. Dữ liệu

Các đối tượng chính:

- `GerberSampleConfig`
- `WorkflowContext`
- `SampleGridLayout`
- `SampleTileInfo`
- `SampleManifest`
- `SampleCropProgress`

### 2.3. Dịch vụ

Nên tách thành các service độc lập:

- `SampleConfigStore`
- `SampleImageLoader`
- `SampleGeometryCalculator`
- `SampleTraversalBuilder`
- `SampleOverlayRenderer`
- `SampleCropService`
- `SampleManifestWriter`

---

## 3. Đường dẫn cấu hình cố định

Dùng một đường dẫn duy nhất trong chương trình:

```csharp
Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "Config",
    "gerber_sample_config.json")
```

Nên khai báo tập trung:

```csharp
public static class AppPaths
{
    public static string SampleConfigPath =>
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Config",
            "gerber_sample_config.json");
}
```

Không dùng `OpenFileDialog` cho nút **Load Config** và không dùng `SaveFileDialog` cho nút **Save Config** trong flow mặc định.

---

## 4. Flow khởi tạo Tab 2

```text
CreateGerberSampleControl được tạo
        ↓
Khởi tạo control và event
        ↓
Khởi tạo GerberSampleConfig mặc định
        ↓
Kiểm tra thư mục Config
        ├─ Chưa có → tạo thư mục
        └─ Đã có   → tiếp tục
        ↓
Kiểm tra gerber_sample_config.json
        ├─ Chưa có
        │    ↓
        │  Tạo config mặc định
        │    ↓
        │  Validate
        │    ↓
        │  Lưu file mới
        │
        └─ Đã có
             ↓
           Load file
             ↓
           Deserialize
             ↓
           Validate
             ↓
           Đưa config lên sampleConfigGrid
        ↓
Cập nhật trạng thái Ready
```

Nếu file config tồn tại nhưng lỗi JSON:

```text
Báo lỗi rõ ràng
        ↓
Sao lưu file lỗi thành *.invalid_<timestamp>.json
        ↓
Tạo config mặc định mới
        ↓
Lưu lại tại đường dẫn chuẩn
```

Không được âm thầm bỏ qua lỗi deserialize.

---

## 5. Flow Load Config

```text
Người dùng nhấn Load Config
        ↓
Lấy AppPaths.SampleConfigPath
        ↓
Kiểm tra file
        ├─ Không tồn tại
        │    ↓
        │  Tạo config mặc định
        │    ↓
        │  Validate
        │    ↓
        │  Lưu file mới
        │
        └─ Tồn tại
             ↓
           Deserialize
             ↓
           Validate
        ↓
Đưa config lên sampleConfigGrid
        ↓
Nếu ảnh sample đã load
        ↓
Tính lại lưới theo config mới
        ↓
Vẽ lại overlay màu đỏ và số thứ tự
        ↓
Cập nhật WorkflowContext
        ↓
Hiển thị thông báo Load Config thành công
```

### Quy tắc

- Load Config luôn đọc từ đường dẫn chuẩn của chương trình.
- Nếu file chưa tồn tại, Load Config phải tạo file mặc định rồi load lại.
- Nếu config mới làm geometry không hợp lệ, không được thay thế layout đang hiển thị bằng layout lỗi.
- Chỉ commit config vào UI và `WorkflowContext` sau khi validate thành công.

---

## 6. Flow Save Config

```text
Người dùng chỉnh sampleConfigGrid
        ↓
Người dùng nhấn Save Config
        ↓
Đọc giá trị từ grid vào GerberSampleConfig tạm
        ↓
Validate toàn bộ field
        ├─ Lỗi
        │    ↓
        │  Highlight field lỗi
        │    ↓
        │  Không ghi file
        │
        └─ Hợp lệ
             ↓
           Tạo thư mục Config nếu thiếu
             ↓
           Serialize ra file tạm
             ↓
           Đọc lại file tạm để xác nhận
             ↓
           Replace file chính theo kiểu atomic
             ↓
           Cập nhật config đang dùng
             ↓
           Cập nhật WorkflowContext
             ↓
           Nếu sample đã load → vẽ lại grid
```

Khuyến nghị ghi file an toàn:

```text
gerber_sample_config.json.tmp
        ↓
Serialize
        ↓
Deserialize kiểm tra
        ↓
Replace gerber_sample_config.json
```

---

## 7. Flow Open Sample

```text
Người dùng nhấn Open Sample
        ↓
OpenFileDialog
        ↓
Chọn PNG/BMP/TIF/TIFF
        ↓
Kiểm tra file tồn tại
        ↓
Đọc ảnh bằng SampleImageLoader
        ↓
Kiểm tra:
    - width > 0
    - height > 0
    - channel hợp lệ
    - bit depth được hỗ trợ
    - frame/page được hỗ trợ
        ↓
Giữ image source trong field có ownership rõ ràng
        ↓
Cập nhật txtSamplePath
        ↓
Cập nhật WorkflowContext
        ↓
Gán ảnh vào sampleWindow
        ↓
SetShowImage(true)
        ↓
Display
        ↓
FitImage()
        ↓
Lấy config hiện tại
        ↓
Tính grid layout
        ↓
Tạo traversal order
        ↓
Vẽ tất cả ô màu đỏ
        ↓
Vẽ OrderIndex trong từng ô
        ↓
Cập nhật derived values
```

### Trạng thái lưới ngay sau khi load ảnh

Mỗi ô phải có:

- `Row`
- `Column`
- `OrderIndex`
- `CropRectangle`
- `Status = Pending`

Hiển thị:

- Viền đỏ: `Pending`
- Số thứ tự: `OrderIndex`
- Có thể hiển thị thêm `R{row} C{column}` khi bật debug

---

## 8. Flow tính geometry

### 8.1. Preprocess

```text
Original sample
        ↓
PreprocessMode
    ├─ None
    ├─ Resize
    ├─ FitPad
    └─ CenterCrop
        ↓
KeepAspectRatio
        ↓
Optional InvertImage
        ↓
Processed sample
```

Mọi crop rectangle dùng tọa độ của processed sample.

### 8.2. Overlap theo pixel

```text
tileWidth = (W + (Columns - 1) × OverlapX) / Columns
stepX     = tileWidth - OverlapX

tileHeight = (H + (Rows - 1) × OverlapY) / Rows
stepY      = tileHeight - OverlapY
```

Phiên bản hiện tại:

```text
OverlapX = OverlapY = OverlapValue
```

### 8.3. Overlap theo phần trăm

```text
p = OverlapValue / 100

tileWidth  = W / [1 + (Columns - 1) × (1 - p)]
tileHeight = H / [1 + (Rows - 1) × (1 - p)]

stepX = tileWidth × (1 - p)
stepY = tileHeight × (1 - p)
```

### 8.4. Boundary policy

- Tính bằng `double`.
- Tạo boundary array trước.
- Boundary đầu bằng 0.
- Boundary cuối bằng width/height của processed sample.
- Không tile nào vượt khỏi ảnh.
- Sai số overlap do rounding không quá 1 px.
- Reject geometry khi `tileWidth <= overlap` hoặc `tileHeight <= overlap`.

---

## 9. Flow tạo thứ tự lưới

```text
Tạo physical matrix [row, column]
        ↓
Giữ nguyên Row và Column
        ↓
Resolve StartOrder
        ↓
Resolve CropOrder
        ↓
Tạo traversal graph
        ↓
Gán OrderIndex 0..N-1
        ↓
Tạo predecessor/successor
        ↓
Tạo horizontal/vertical neighbors
```

Bốn preset được hỗ trợ:

- `TopLeftRight`
- `TopLeftDown`
- `BottomRightLeft`
- `BottomRightUp`

Không được dùng `OrderIndex` để thay đổi danh tính vật lý của ô.

---

## 10. Flow Create Sample

```text
Người dùng nhấn Create Sample
        ↓
Chụp snapshot config từ UI
        ↓
Validate config
        ↓
Validate sample image
        ↓
Tính lại layout cuối cùng
        ↓
Kiểm tra ExpectedTileCount = Rows × Columns
        ↓
Disable control xung đột
        ↓
Reset progress
        ↓
Đặt tất cả ô = Pending / đỏ
        ↓
Chạy SampleCropService trên worker
        ↓
Lặp theo OrderIndex
```

Flow của từng ô:

```text
Tile K bắt đầu
        ↓
Đổi tile K sang Processing
        ↓
Có thể hiển thị viền vàng
        ↓
Crop processed sample
        ↓
Kiểm tra crop size
        ↓
Lưu tile
        ↓
Kiểm tra file vừa lưu đọc lại được
        ↓
Tạo SampleTileInfo
        ↓
Đổi tile K sang Completed
        ↓
Cập nhật viền xanh lá
        ↓
Cập nhật progress bar
        ↓
Chuyển sang tile K + 1
```

Nếu lỗi:

```text
Tile K lỗi
        ↓
Status = Failed
        ↓
Giữ đỏ đậm hoặc thêm dấu X
        ↓
Ghi lỗi kèm Row, Column, OrderIndex
        ↓
Dừng hoặc tiếp tục theo policy đã cấu hình
```

Yêu cầu tối thiểu của phiên bản hiện tại:

- Pending: đỏ
- Completed: xanh lá
- OrderIndex luôn còn nhìn thấy
- UI không bị freeze
- Mỗi cập nhật viewer phải marshal về UI thread

---

## 11. Output

```text
<OutputDirectory>/
├─ tiles/
│  ├─ Sample_R00_C00_O000.png
│  ├─ Sample_R00_C01_O001.png
│  └─ ...
├─ sample_manifest.json
├─ sample_config.json
└─ sample_overlay.png
```

`sample_manifest.json` là contract giữa Tab 2 và Tab 3.

---

## 12. WorkflowContext sau khi hoàn thành

Cập nhật tối thiểu:

```text
SampleRasterPath
SampleConfig
SampleProcessedWidth
SampleProcessedHeight
SampleOutputDirectory
SampleManifestPath
SampleCreationStatus
```

Chỉ publish `SampleManifestPath` khi toàn bộ output bắt buộc đã được ghi thành công.

---

## 13. State machine của Tab 2

```text
Idle
  ↓
ConfigReady
  ↓
SampleLoaded
  ↓
GridPreviewReady
  ↓
Creating
  ├─ Completed
  ├─ Cancelled
  └─ Failed
```

Không cho phép:

- `Creating → Open Sample`
- `Creating → Load Config`
- `Creating → Save Config`
- chạy hai crop operation đồng thời

---

## 14. Checklist kiểm thử

### Config

- [ ] Không có thư mục Config → tự tạo.
- [ ] Không có file config → tự tạo file mặc định.
- [ ] Load Config đọc đúng đường dẫn cố định.
- [ ] Save Config ghi đúng đường dẫn cố định.
- [ ] JSON lỗi → backup file lỗi và tạo config mới.
- [ ] Config invalid → không commit vào UI.

### Load image

- [ ] PNG hiển thị.
- [ ] BMP hiển thị.
- [ ] TIFF 8-bit hiển thị.
- [ ] TIFF 16-bit có thông báo hoặc convert đúng.
- [ ] Ảnh mới thay ảnh cũ mà không leak.
- [ ] `sampleWindow.FitImage()` hoạt động.

### Grid

- [ ] Số ô bằng `Rows × Columns`.
- [ ] Tất cả ô ban đầu màu đỏ.
- [ ] Số `OrderIndex` đúng.
- [ ] Thay config thì grid được tính lại.
- [ ] Không ô nào vượt khỏi ảnh.
- [ ] StartOrder đúng cho cả bốn preset.

### Crop

- [ ] Mỗi ô hoàn thành chuyển xanh lá.
- [ ] Progress tăng đúng một lần cho mỗi tile.
- [ ] Tile file đọc lại được.
- [ ] Cancel không để manifest hoàn chỉnh giả.
- [ ] Worker không truy cập WinForms trực tiếp.
