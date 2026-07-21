# Error Log — Tab 2 Create Gerber Sample

## 1. CS0029 khi cập nhật WorkflowContext

- **Triệu chứng:** `Cannot implicitly convert type 'GerberViewer.Stitching.Configuration.GerberSampleConfig' to 'GerberViewer.Workflow.Models.SampleGerberConfig'` tại `CreateGerberSampleControl.cs`.
- **Nguyên nhân:** Tab 2 dùng model cấu hình đầy đủ `GerberSampleConfig`, trong khi `WorkflowContext.SampleConfig` đang là `Workflow.Models.SampleGerberConfig`. Hai kiểu không tương thích.
- **Cách sửa:** Không gán trực tiếp object khác kiểu; chỉ map trường tương thích `SourceRasterPath` sang `WorkflowContext.SampleConfig.SourceRasterPath`.
- **Trạng thái:** Đã sửa.

## 2. Không mở được TIFF/BigTIFF lớn bằng Bitmap

- **Triệu chứng:** Dialog `Open sample failed`, exception `ImageRead.ImageReadException`, inner exception `System.ArgumentException: Parameter is not valid` khi gọi `System.Drawing.Bitmap..ctor(string filename)` với file TIFF lớn.
- **Nguyên nhân:** GDI+/`System.Drawing.Bitmap` không ổn định với một số TIFF/BigTIFF lớn hoặc format TIFF chuyên dụng.
- **Cách sửa:** Flow preview Tab 2 đọc ảnh bằng HALCON `HOperatorSet.ReadImage`, lấy kích thước bằng `GetImageSize`, và gán `HObject` vào control preview kế thừa từ `EWindowControl`.
- **Trạng thái:** Đã sửa cho preview/open image. Crop generator vẫn cần phase riêng nếu muốn crop BigTIFF hoàn toàn bằng HALCON.

## 3. Overlay grid fill kín ảnh và không thấy OrderIndex

- **Triệu chứng:** Preview bị phủ màu đỏ kín vùng ảnh; người dùng không nhìn được ảnh gốc, không nhìn được thứ tự tile.
- **Nguyên nhân:** HALCON region rectangle được display theo draw mode `fill`, nên region bị tô đặc thay vì chỉ vẽ viền.
- **Cách sửa:** Overlay renderer dùng `SetDraw("margin")` trước khi display rectangle và render `OrderIndex` bằng text màu vàng có box.
- **Trạng thái:** Đã sửa.

## 4. Không được sửa trực tiếp project EWindowControl

- **Triệu chứng:** Các API preview HALCON ban đầu được thêm trực tiếp vào `EWindowControl/EWindowControl.cs`.
- **Nguyên nhân:** Chưa tuân thủ yêu cầu chỉ mở rộng ở control mới kế thừa từ `EWindowControl`.
- **Cách sửa:** Di chuyển logic HALCON sample preview/overlay sang `GerberViewer.Views.GerberSampleWindow : EWindowControl.EWindowControl`; `CreateGerberSampleControl` dùng control kế thừa này.
- **Trạng thái:** Đã sửa trong lượt này.

## 5. Thiếu nút Refresh để cập nhật lại sắp xếp/order trên preview

- **Triệu chứng:** Sau khi chỉnh config grid, người dùng cần nút rõ ràng để tính lại layout và order trên preview mà không phải load lại ảnh/config.
- **Cách sửa:** Thêm `btnRefreshPreview` vào command table. Handler đọc config hiện tại từ `PropertyGrid`, validate, tính lại `SampleGridLayout`, reset state Pending, và render lại overlay.
- **Trạng thái:** Đã sửa.
