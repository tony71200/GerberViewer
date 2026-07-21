# Prompt cho Codex — Sửa và hoàn thiện Tab 2 Create Gerber Sample

## 1. Vai trò

Bạn đang sửa một solution C# WinForms .NET Framework 4.8 có HALCON và `EWindowControl`.

Nguồn cần đọc trước khi sửa:

- Toàn bộ solution hiện tại.
- `CreateGerberSampleControl.cs`
- `CreateGerberSampleControl.Designer.cs`
- `MainForm.cs`
- Các class thuộc `Workflow`
- `WorkflowContext`
- `GerberSampleConfig`
- Code `EWindowControl`
- Các service crop, traversal, config và manifest hiện có
- Project/source tham khảo `StitchingImage`

Không sửa Tab 3 trong phase này, trừ interface hoặc model dùng chung bắt buộc để Tab 2 build được.

---

## 2. Mục tiêu

Hoàn thiện flow Tab 2 theo đúng thứ tự:

```text
Initialize
→ ensure/load config
→ open sample image
→ display image
→ calculate grid from config
→ draw red grid and OrderIndex
→ create sample tiles
→ update completed tile to green
→ save config/manifest/overlay
→ publish WorkflowContext
```

Các lỗi cần giải quyết:

1. Chọn TIFF nhưng `sampleWindow` không hiển thị ảnh.
2. Flow hiện tại có thể chỉ lưu đường dẫn mà chưa decode và gán ảnh vào viewer.
3. Chưa có nút **Save Config** ở vị trí cố định.
4. **Load Config** phải đọc từ đường dẫn cố định trong chương trình.
5. Nếu config chưa tồn tại, chương trình phải tạo config mặc định và lưu file mới.
6. Khi ảnh được load, phải lập tức tính grid từ config hiện tại.
7. Tất cả ô ban đầu phải có viền đỏ và số thứ tự.
8. Sau khi xử lý xong một tile, ô tương ứng phải chuyển xanh lá.
9. Mỗi task phải kiểm tra độc lập được.
10. Phải rà kỹ trạng thái từng ô, không chỉ progress tổng.

---

## 3. Quy tắc bắt buộc

### 3.1. Không viết nghiệp vụ trong Designer

`CreateGerberSampleControl.Designer.cs` chỉ chứa:

- khai báo control
- layout
- property UI
- event wiring
- `Dispose(bool)`

Không đặt:

- load JSON
- đọc TIFF
- crop
- traversal
- thread
- manifest logic

### 3.2. Không làm file UI thành monolithic

Tách service/model khi logic vượt trách nhiệm UI.

### 3.3. Không truy cập UI từ worker thread

Dùng:

- `Task.Run`
- `CancellationToken`
- `IProgress<T>`
- `BeginInvoke` hoặc `SynchronizationContext` khi thật sự cần

### 3.4. Ownership tài nguyên

Dispose đúng:

- `Bitmap`
- `Mat`
- `HObject`
- `HTuple`
- stream
- temporary image
- previous sample source

Không dispose image source trong khi `sampleWindow` vẫn đang sử dụng.

### 3.5. Không nuốt exception

Không dùng `catch { }`.

Mọi lỗi phải:

- log exception đầy đủ
- hiển thị file/config/tile liên quan
- giữ state UI nhất quán

---

## 4. Đường dẫn config cố định

Tạo class tập trung:

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

Yêu cầu:

- `Load Config` luôn đọc đường dẫn này.
- `Save Config` luôn ghi đường dẫn này.
- Không mở dialog chọn config trong flow mặc định.
- Tạo thư mục `Config` nếu chưa tồn tại.
- Nếu file chưa tồn tại, tạo `GerberSampleConfig` mặc định và lưu.
- Nếu file JSON lỗi:
  - backup thành `.invalid_<timestamp>.json`
  - tạo config mặc định mới
  - lưu lại
  - báo warning rõ ràng

---

## 5. Bố trí UI cố định

Trong command panel của Tab 2, sắp xếp theo thứ tự:

```text
Open Sample
Sample Path
Load Config
Save Config
Create Sample
Cancel
Progress
Status
```

Nút mới:

```csharp
private Button btnSaveConfig;
```

Yêu cầu layout:

- Không dùng tọa độ chồng lấn.
- Dùng `TableLayoutPanel` hoặc `FlowLayoutPanel` nếu phù hợp.
- `Load Config` và `Save Config` luôn nằm cạnh nhau.
- Khi resize form, vị trí tương đối không thay đổi.
- Giữ format chuẩn do Visual Studio Designer sinh ra.

---

# 6. Chia công việc thành task độc lập

Không thực hiện thành một patch lớn.

Mỗi task phải có:

1. Phạm vi file.
2. Thay đổi.
3. Điều kiện hoàn thành.
4. Test độc lập.
5. Kết quả build.
6. Log thay đổi ngắn.

Sau mỗi task:

- build solution
- chạy test liên quan
- không chuyển task tiếp theo khi task hiện tại chưa pass

---

## Task 0 — Audit baseline

### Mục tiêu

Đọc flow Tab 2 hiện tại và lập bản đồ thực thi.

### Việc cần làm

Tìm và ghi lại:

- nơi tạo `CreateGerberSampleControl`
- event `btnOpenSample`
- event `btnLoadConfig`
- event `btnCreateSample`
- nơi khởi tạo `sampleWindow`
- API hiện dùng để display ảnh
- field giữ sample image
- nơi tính grid
- nơi vẽ overlay
- nơi cập nhật progress
- nơi lưu manifest
- nơi cập nhật `WorkflowContext`

### Output

Tạo:

```text
docs/tab2_baseline_audit.md
```

Nội dung phải có call flow và danh sách lỗi tìm thấy.

### Test độc lập

- Build solution trước khi sửa.
- Ghi lại warning/error baseline.
- Không sửa nghiệp vụ trong task này, trừ lỗi build chặn audit.

---

## Task 1 — Config model validation

### Mục tiêu

Bảo đảm `GerberSampleConfig` có default rõ ràng và validate được.

### Field tối thiểu

```text
CropOrder
StartOrder
InvertImage
Rows
Columns
OverlapValue
OverlapUnit
PreprocessMode
PreprocessWidth
PreprocessHeight
KeepAspectRatio
OutputDirectory
OutputFormat
TileNamePattern
DrawOrderLabels
SaveOverlayPreview
```

### Yêu cầu

Tạo validator trả về danh sách lỗi có field name.

Không throw ngay ở lỗi đầu tiên nếu có thể thu thập nhiều lỗi.

### Test độc lập

Test tối thiểu:

- Rows = 0
- Columns = 0
- Overlap percent >= 100
- Pixel overlap quá lớn
- OutputDirectory rỗng
- Config mặc định hợp lệ hoặc chỉ thiếu field bắt buộc được xác định rõ

---

## Task 2 — SampleConfigStore

### Mục tiêu

Tách load/save config khỏi UI.

### API đề xuất

```csharp
public sealed class SampleConfigStore
{
    public GerberSampleConfig LoadOrCreateDefault();
    public GerberSampleConfig Load();
    public void Save(GerberSampleConfig config);
}
```

### Yêu cầu

- Dùng `AppPaths.SampleConfigPath`.
- Tạo directory nếu thiếu.
- Atomic save bằng file tạm.
- Deserialize lại file tạm trước khi replace.
- Backup JSON lỗi.
- Không phụ thuộc WinForms control.

### Test độc lập

Dùng temp directory hoặc injectable path để test:

1. Folder chưa tồn tại.
2. File chưa tồn tại.
3. File hợp lệ.
4. File JSON lỗi.
5. Save rồi load lại giữ nguyên giá trị.
6. Không để lại `.tmp` khi thành công.

---

## Task 3 — Thêm Save Config UI

### Mục tiêu

Thêm `btnSaveConfig` ở vị trí cố định cạnh `btnLoadConfig`.

### File

- `CreateGerberSampleControl.Designer.cs`
- resource liên quan nếu có

### Yêu cầu

- Format chuẩn Visual Studio.
- Không đặt logic save trong Designer.
- Event handler nằm trong `.cs`.
- Tab order hợp lý.
- Anchor/Dock đúng.
- Không làm vỡ layout khi resize.

### Test độc lập

- Mở form ở kích thước mặc định.
- Resize nhỏ/lớn.
- Load Config và Save Config không chồng nhau.
- Build Designer không lỗi.

---

## Task 4 — Wire Load Config và Save Config

### Mục tiêu

Hoàn thiện hành vi hai nút.

### Load Config

```text
Click
→ LoadOrCreateDefault
→ Validate
→ bind grid
→ commit current config
→ update WorkflowContext
→ nếu sample đã load thì rebuild grid
```

### Save Config

```text
Click
→ read grid to temporary config
→ validate
→ Save
→ reload saved file
→ commit current config
→ nếu sample đã load thì rebuild grid
```

### Yêu cầu

- Nếu validation lỗi, highlight row/property lỗi.
- Không commit config lỗi.
- Hiển thị đường dẫn file đã load/save.
- Không mở file dialog.

### Test độc lập

1. Xóa config rồi nhấn Load Config.
2. Kiểm tra file mới được tạo.
3. Chỉnh Rows/Columns rồi Save Config.
4. Mở lại chương trình và Load Config.
5. Giá trị giữ nguyên.
6. Config lỗi không được ghi file.

---

## Task 5 — Sửa load và display TIFF

### Mục tiêu

Sau khi chọn ảnh, `sampleWindow` phải hiển thị ảnh thật.

### Flow bắt buộc

```text
OpenFileDialog
→ validate path
→ decode image
→ validate dimensions/channels
→ replace owned source
→ update path/context
→ set viewer source
→ show image
→ display
→ fit
```

### Decoder

Ưu tiên HALCON cho TIFF:

```csharp
HOperatorSet.ReadImage(out HObject image, filePath);
```

Nếu có abstraction hiện tại, dùng abstraction nhưng phải hỗ trợ TIFF ổn định.

### API viewer

Không sửa trực tiếp field nội bộ của `EWindowControl` từ UI nếu có thể tránh.

Tạo API rõ ràng trong `EWindowControl` hoặc adapter:

```csharp
void SetSourceImage(HObject image);
```

API phải:

- clone hoặc định nghĩa ownership rõ
- dispose source cũ
- `SetShowImage(true)`
- display source
- `FitImage()`

### Test độc lập

- PNG
- BMP
- TIFF 8-bit
- TIFF grayscale
- TIFF color
- file không hợp lệ
- thay ảnh liên tục 20 lần
- ảnh cũ không bị lock
- memory không tăng bất thường

---

## Task 6 — Sample grid geometry

### Mục tiêu

Tách calculation khỏi UI và tạo layout deterministic.

### API đề xuất

```csharp
SampleGridLayout Calculate(
    int processedWidth,
    int processedHeight,
    GerberSampleConfig config);
```

### Layout phải chứa

```text
Tiles
TileWidth
TileHeight
StepX
StepY
Rows
Columns
ExpectedTileCount
```

Mỗi tile:

```text
Row
Column
OrderIndex
Rectangle
Predecessor
Successor
HorizontalNeighbors
VerticalNeighbors
Status
```

### Yêu cầu

- Physical matrix trước, traversal sau.
- Boundary array deterministic.
- Không tile vượt ảnh.
- Hỗ trợ pixel và percent overlap.
- Bốn StartOrder preset.
- Không dùng legacy `RobotOrderer`.

### Test độc lập

Matrix:

- 1×1
- 2×2
- 4×4
- 8×8

Start order:

- TopLeftRight
- TopLeftDown
- BottomRightLeft
- BottomRightUp

Overlap:

- 0 px
- 60 px
- percent hợp lệ
- overlap invalid

Assert:

- count = Rows × Columns
- OrderIndex unique
- Row/Column unique
- bounds hợp lệ
- first/last order đúng
- overlap sai số <= 1 px

---

## Task 7 — Vẽ grid đỏ và số thứ tự ngay khi load ảnh

### Mục tiêu

Khi ảnh load thành công hoặc config thay đổi:

```text
Calculate layout
→ reset all tile status to Pending
→ draw red rectangles
→ draw OrderIndex
```

### Yêu cầu hiển thị

- Pending: viền đỏ.
- Processing: viền vàng, nếu implement.
- Completed: viền xanh lá.
- Failed: đỏ đậm và dấu X, nếu implement.
- OrderIndex luôn hiển thị.
- Overlay dùng image coordinate.
- Zoom/pan không làm lệch rectangle.
- Không rasterize overlay trực tiếp lên source image nếu viewer hỗ trợ overlay riêng.

### API đề xuất

```csharp
void RenderGridOverlay(
    SampleGridLayout layout,
    IReadOnlyDictionary<int, SampleTileState> states);
```

### Test độc lập

1. Load ảnh với config 4×4.
2. Có đúng 16 ô đỏ.
3. Label từ 0 đến 15.
4. Đổi StartOrder và Load Config.
5. Rectangle không đổi vị trí vật lý nhưng OrderIndex đổi đúng.
6. Zoom/pan/Fit không làm lệch overlay.

---

## Task 8 — Crop service và trạng thái từng ô

### Mục tiêu

Mỗi tile phải có state riêng, không suy ra màu từ progress tổng.

### State đề xuất

```csharp
public enum SampleTileState
{
    Pending,
    Processing,
    Completed,
    Failed
}
```

### Progress model

```csharp
public sealed class SampleCropProgress
{
    public int OrderIndex { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public SampleTileState State { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public string OutputPath { get; set; }
    public string Message { get; set; }
}
```

### Flow từng tile

```text
Report Processing
→ UI đổi ô hiện tại
→ crop
→ save
→ verify saved file
→ add manifest entry
→ Report Completed
→ UI đổi ô sang xanh lá
```

### Yêu cầu

- Mỗi tile chỉ được chuyển `Completed` một lần.
- Chỉ xanh khi crop và save đã thành công.
- File save lỗi thì không xanh.
- `CompletedCount` không tăng hai lần.
- Cancellation giữ các ô chưa chạy ở Pending.
- Manifest không được đánh dấu complete nếu có tile lỗi hoặc cancel.

### Test độc lập

Dùng fake writer/fake cropper:

- tất cả tile thành công
- tile giữa lỗi
- cancel tại tile N
- writer throw exception
- progress event đúng thứ tự
- state của từng OrderIndex đúng

---

## Task 9 — Manifest và output transaction

### Mục tiêu

Chỉ publish manifest hoàn chỉnh khi run thành công.

### Yêu cầu

Output tạm:

```text
<OutputDirectory>/.creating_<runId>/
```

Khi thành công:

```text
validate all tiles
→ write config
→ write manifest
→ write overlay
→ move/replace final output
→ publish WorkflowContext
```

Khi lỗi/cancel:

- xóa output tạm hoặc đánh dấu incomplete
- không publish manifest path như thành công

### Test độc lập

- success
- cancel
- crop failure
- manifest serialization failure
- output directory không writable

---

## Task 10 — Threading và UI state

### Mục tiêu

Không freeze UI và không cross-thread exception.

### Yêu cầu

- `CancellationTokenSource` cho mỗi run.
- Prevent double-run.
- Disable:
  - Open Sample
  - Load Config
  - Save Config
  - Create Sample
- Enable Cancel khi đang chạy.
- Restore control trong `finally`.
- `IProgress<SampleCropProgress>` được tạo trên UI thread.
- Worker không gọi `sampleWindow` trực tiếp.

### Test độc lập

- click Create hai lần
- cancel giữa run
- exception giữa run
- form vẫn responsive
- control được restore

---

## Task 11 — Integration test Tab 2

### Scenario A — First run

```text
Không có Config folder
→ mở chương trình
→ config mặc định được tạo
→ load TIFF
→ ảnh hiển thị
→ grid đỏ + OrderIndex
→ create sample
→ từng ô chuyển xanh
→ manifest được tạo
```

### Scenario B — Save/Load config

```text
đổi Rows/Columns/Overlap
→ Save Config
→ restart
→ Load Config
→ giá trị được phục hồi
→ grid đúng
```

### Scenario C — Failure

```text
writer lỗi tại tile K
→ tile K không xanh
→ status Failed
→ manifest không publish complete
→ UI được restore
```

### Scenario D — Cancel

```text
cancel tại tile K
→ tile trước K giữ xanh
→ tile chưa chạy giữ đỏ
→ output incomplete không được publish
```

---

# 7. Kiểm tra từng ô bắt buộc

Sau mỗi run, tạo bảng kiểm tra nội bộ:

| OrderIndex | Row | Column | Expected Rect | Actual Rect | State | File Exists | File Readable |
|---:|---:|---:|---|---|---|---|---|

Assert cho từng ô:

```text
OrderIndex unique
Row/Column đúng
Rectangle đúng
State transition hợp lệ
Completed ↔ file tồn tại và đọc được
Failed ↔ có error message
```

Không chỉ kiểm tra:

```text
progressBar.Value == 100
```

Progress 100% không đủ để chứng minh mọi tile thành công.

---

# 8. Build và quality gate

Sau mỗi task:

1. Restore package nếu cần.
2. Build x64 Debug.
3. Build x64 Release.
4. Chạy unit test.
5. Chạy test thủ công task tương ứng.
6. Kiểm tra warning mới.
7. Không để:
   - cross-thread warning
   - undisposed image
   - file lock
   - empty catch
   - duplicate event subscription

Không sửa lỗi bằng cách:

- comment code lỗi
- catch rồi bỏ qua
- hard-code đường dẫn cá nhân
- thêm delay để che race condition
- gọi `Application.DoEvents()`

---

# 9. Output Codex phải trả về

Sau khi hoàn thành, tạo:

```text
docs/tab2_baseline_audit.md
docs/tab2_task_results.md
docs/tab2_test_report.md
docs/tab2_changed_files.md
```

`tab2_task_results.md` phải có bảng:

| Task | Status | Files changed | Build | Independent test | Notes |
|---|---|---|---|---|---|

`tab2_changed_files.md` phải chỉ rõ:

- file
- class/method
- mục đích sửa
- hành vi trước
- hành vi sau

Cuối cùng báo cáo:

1. Các lỗi root cause đã sửa.
2. Đường dẫn config thực tế.
3. API dùng để load/display TIFF.
4. Cách tính và vẽ grid.
5. Cách state từng tile chuyển đỏ → xanh.
6. Test nào đã pass.
7. Hạng mục nào chưa làm được và lý do.

---

# 10. Definition of Done

Tab 2 chỉ được xem là hoàn thành khi:

- [x] Chạy lần đầu tự tạo config.
- [x] Load Config dùng đường dẫn cố định.
- [x] Save Config dùng cùng đường dẫn.
- [x] TIFF/BigTIFF hiển thị trên `sampleWindow` bằng HALCON `ReadImage`.
- [x] Load ảnh lập tức vẽ grid theo config.
- [x] Tất cả tile Pending có viền đỏ dạng margin, không fill kín ảnh.
- [x] Mỗi tile có OrderIndex đúng và label hiển thị rõ trên overlay.
- [x] Có nút Refresh để cập nhật lại layout/order preview từ config hiện tại.
- [x] Không thêm API mới trực tiếp vào project EWindowControl; logic preview HALCON nằm trong control kế thừa `GerberSampleWindow`.
- [x] Đã ghi các lỗi trong cuộc thoại vào `docs/error_Tab2.md`.
- [x] Tile chỉ chuyển xanh sau khi crop và save thành công.
- [ ] State từng tile được kiểm tra độc lập.
- [ ] Cancel và lỗi không tạo manifest hoàn chỉnh giả.
- [x] UI không freeze.
- [x] Không có cross-thread exception.
- [x] Sửa lỗi build CS0029 do gán nhầm <code>GerberSampleConfig</code> sang <code>SampleGerberConfig</code>.
- [x] Không leak hoặc lock ảnh trong flow preview Tab 2: source HALCON cũ được dispose khi thay ảnh/Dispose control.
- [ ] Build x64 Debug và Release thành công. _(Blocked in this container: no msbuild/dotnet/xbuild/mcs installed.)_
- [x] Báo cáo task và test được tạo đầy đủ.
