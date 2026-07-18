# TÀI LIỆU ĐẶC TẢ KỸ THUẬT (SPECIFICATION)
## GERBER VIEWER & PNG CONVERTER — C# / WINFORMS (.NET FRAMEWORK 4.8)

**Tên file:** `Spec_CSharpWinform_VI.md`  
**Phiên bản:** 1.1  
**Ngày:** 2026-07-18  
**Thay thế:** `Spec_CSharpWinform.md` phiên bản 1.0  
**Tham chiếu UI/UX:** chức năng tương đương `https://onlinegerberviewer.com/`  
**Tham chiếu kiến trúc:** ý tưởng parser → plotter → SVG renderer của `@tracespace/renderer`; không sử dụng trực tiếp package JavaScript trong lõi C#.

---

## 1. MỤC TIÊU (BR — Business Requirements)

| ID | Mô tả |
|---|---|
| BR-001 | Ứng dụng desktop Windows cho phép mở, xem, cấu hình và chuyển đổi file Gerber (RS-274X, Gerber X2) sang ảnh PNG chất lượng cao. |
| BR-002 | Trải nghiệm tương đương Online Gerber Viewer: kéo-thả nhiều file, danh sách lớp có bật/tắt và đổi màu, canvas zoom/pan, hiển thị tọa độ chuột theo mm/inch, render realistic. |
| BR-003 | Lõi xử lý `GerberEngine` phải tách khỏi UI, đóng gói thành class library có API công khai để tái sử dụng trong console, service hoặc ứng dụng khác. |
| BR-004 | Preview phải sử dụng mô hình hình học vector và SVG, độc lập với DPI xuất PNG; người dùng phải có thể zoom sâu mà không bị vỡ hoặc mờ do giới hạn độ phân giải của bitmap preview. |
| BR-005 | Zoom phải phản hồi tức thời bằng phép biến đổi viewport, sau đó trình hiển thị tự rasterize lại vector ở độ phân giải màn hình hiện tại; file Gerber không được parse lại theo mỗi thao tác zoom/pan. |

---

## 2. CÔNG NGHỆ VÀ RÀNG BUỘC BẮT BUỘC

- Ngôn ngữ: **C# 7.3**, tương thích .NET Framework 4.8; không dùng `record`, target-typed `new`, switch expression hoặc cú pháp yêu cầu C# 8+.
- UI: **Windows Forms (.NET Framework 4.8)**.
- Parser, graphics state, plotter, mô hình hình học và logic polarity phải tự viết trong `GerberEngine`.
- Kiến trúc render bắt buộc là **dual-rendering**:
  1. **SVG vector renderer** cho preview tương tác.
  2. **GDI+ raster renderer** cho xuất PNG theo DPI xác định.
- `@tracespace/renderer` chỉ là kiến trúc tham khảo. Ứng dụng không chạy Node.js và không phụ thuộc runtime JavaScript để parse Gerber.
- `GerberEngine.dll` không được tham chiếu `System.Windows.Forms`, WebView2 hoặc control UI.
- UI được phép sử dụng **Microsoft Edge WebView2** làm host hiển thị SVG cục bộ. WebView2 chỉ nhận chuỗi hoặc file SVG/HTML được tạo nội bộ, không được đảm nhận parser Gerber.
- Nếu WebView2 Runtime không khả dụng, chương trình phải thông báo rõ và có thể dùng bitmap preview dự phòng; PNG export vẫn phải hoạt động.
- Không sử dụng thư viện Gerber parser/renderer bên thứ ba trong lõi. Dependency UI như WebView2 phải được cô lập trong project `GerberViewer`.

---

## 3. YÊU CẦU CHỨC NĂNG (FR)

Ngôn ngữ **“phải”** (`shall`) thể hiện yêu cầu bắt buộc. Mỗi FR phải truy vết về BR và tới file mã nguồn ở mục 9.

### 3.1. Định dạng và đa lớp

- **FR-001** (BR-001): Hệ thống phải parse RS-274X gồm `FS`, `MO`, `AD`, `AM`, `D01/D02/D03`, `G01/G02/G03`, `G36/G37`, `LPD/LPC` và `M02`.
- **FR-002** (BR-001): Hệ thống phải đọc thuộc tính Gerber X2 `TF.FileFunction` để tự nhận diện loại lớp; nếu không có, phải nhận diện theo phần mở rộng `.gtl`, `.gbl`, `.gts`, `.gbs`, `.gto`, `.gbo`, `.gko`, `.gm1` và từ khóa tên file.
- **FR-003** (BR-002): Người dùng phải nạp được nhiều file cùng lúc bằng multi-select hoặc kéo-thả vào form; mỗi file được quản lý như một lớp độc lập có tên, loại, trạng thái hiển thị, màu và thứ tự vẽ.
- **FR-004** (BR-002): Người dùng phải bật/tắt từng lớp, đổi màu, xóa lớp và thay đổi thứ tự vẽ mà không parse lại các file không thay đổi.

### 3.2. Lõi đồ họa và aperture

- **FR-005** (BR-001): Engine phải dựng đúng bốn aperture chuẩn: Circle, Rectangle, Obround và Polygon, cho cả flash `D03` và stroke `D01`.
- **FR-006** (BR-001): Stroke dùng aperture tròn phải có round cap và round join; stroke của aperture không tròn phải được biểu diễn theo hình học aperture, không được mặc định thay thế bằng một đường có `Pen.Width`.
- **FR-007** (BR-001): Engine phải hỗ trợ Aperture Macro tối thiểu các primitive 1, 4, 5, 20 và 21. Primitive chưa hỗ trợ phải suy giảm an toàn về bounding geometry và ghi cảnh báo, không được làm toàn bộ quá trình render bị lỗi.
- **FR-008** (BR-004): Parser/plotter phải tạo mô hình trung gian độc lập renderer, gọi là `GerberScene`, gồm primitive theo tọa độ millimeter, layer, polarity, aperture và bounding box. `GerberScene` không được chứa pixel hoặc DPI.

### 3.3. Tọa độ, đơn vị và phép biến đổi

- **FR-009** (BR-001): Engine phải tự phát hiện đơn vị MM/IN và định dạng tọa độ từ `FS`/`MO`; nội bộ chuẩn hóa toàn bộ tọa độ và kích thước về **millimeter dạng `double`**.
- **FR-010** (BR-004): `CoordinateTransformer` phải tách hai không gian chuyển đổi:
  1. **World/vector transform:** millimeter ↔ SVG user units/viewBox, không dùng DPI.
  2. **Export raster transform:** millimeter → pixel theo `px = mm / 25.4 × dpi`.
- **FR-011** (BR-002): UI phải hiển thị tọa độ con trỏ theo mm và inch bằng phép biến đổi ngược từ viewport screen coordinate về world millimeter.
- **FR-012** (BR-004): Thay đổi Export DPI không được làm thay đổi zoom, vị trí camera, độ sắc nét hoặc kích thước logic của SVG preview.
- **FR-013** (BR-005): Zoom phải neo tại vị trí con trỏ, nghĩa là world point dưới con trỏ trước và sau khi zoom phải giữ nguyên trong giới hạn sai số hiển thị.

### 3.4. SVG vector preview

- **FR-014** (BR-004): `GerberSvgRenderer` phải chuyển `GerberScene` thành SVG hợp lệ với `viewBox` dựa trên bounding box hợp nhất và margin cấu hình.
- **FR-015** (BR-004): SVG phải giữ hình học dưới dạng vector (`path`, `circle`, `rect`, `polygon`, `use`, `defs`, `mask` hoặc phần tử tương đương); không được nhúng toàn bộ preview thành một bitmap base64.
- **FR-016** (BR-004): Mỗi Gerber layer phải được tạo thành một SVG group riêng, có định danh ổn định để UI bật/tắt, đổi màu và thay đổi opacity mà không parse lại file.
- **FR-017** (BR-004): Polarity `LPC` phải được thực hiện trong phạm vi của chính layer bằng SVG mask hoặc composition tương đương; vùng clear của một layer không được khoét xuyên các layer bên dưới.
- **FR-018** (BR-004): Region `G36/G37`, kể cả contour chứa cung `G02/G03`, phải xuất thành path kín với fill rule nhất quán (`nonzero` hoặc `evenodd` theo mô hình đã chọn và được kiểm thử).
- **FR-019** (BR-004): SVG renderer phải tái sử dụng geometry lặp lại qua `<defs>/<use>` khi việc tái sử dụng giúp giảm kích thước DOM; không bắt buộc dùng `<use>` nếu số lượng instance làm browser chậm hơn.
- **FR-020** (BR-004): SVG được tạo phải tự chứa (`self-contained`), không tham chiếu font, script, ảnh hoặc tài nguyên mạng bên ngoài.

### 3.5. Vector zoom và re-rasterization

- **FR-021** (BR-005): Zoom/pan phải được thực hiện bằng camera/viewport transformation trên SVG đang có; không gọi parser và không tạo lại `GerberScene` cho mỗi sự kiện chuột.
- **FR-022** (BR-005): Trong lúc wheel hoặc drag liên tục, UI phải áp dụng transformation ngay để phản hồi tương tác. Browser/WebView2 sẽ rasterize lại SVG theo mức zoom và `devicePixelRatio` hiện tại.
- **FR-023** (BR-005): Sau khi chuỗi zoom/pan dừng trong khoảng debounce cấu hình từ 80–200 ms, UI được phép thực hiện bước **progressive refinement**: cập nhật viewport chính xác, culling hoặc tái tạo SVG vùng nhìn thấy nếu cần.
- **FR-024** (BR-005): Với scene lớn, hệ thống phải hỗ trợ **viewport culling** dựa trên bounding box và spatial index; chỉ geometry giao với viewport mở rộng mới cần được đưa vào SVG chi tiết.
- **FR-025** (BR-005): Có thể dùng Level of Detail:
  - mức zoom thấp: bỏ qua primitive nhỏ hơn ngưỡng sub-pixel hoặc gộp geometry;
  - mức zoom cao: hiển thị đầy đủ primitive.
  Việc LOD không được làm sai kích thước world coordinate hoặc kết quả PNG export.
- **FR-026** (BR-005): Khi progressive refinement đang chạy, canvas phải giữ frame hiện tại để tránh nháy trắng; kết quả mới được thay thế theo cơ chế atomic update.
- **FR-027** (BR-005): Fit-to-view phải tính camera từ combined bounds theo mm, không tạo bitmap toàn board.

### 3.6. Cấu hình ảnh PNG đầu ra

- **FR-028** (BR-001): Export DPI chọn được từ ComboBox: 150, 300, 600 và 1200. Nhãn UI phải là **Export DPI**, không được gây hiểu nhầm đây là DPI của preview.
- **FR-029** (BR-001): Hai chế độ màu:
  1. **Binary Mask:** nét trắng nền đen, hỗ trợ invert.
  2. **Realistic:** phối màu PCB như copper, solder mask, silkscreen và nền tối.
- **FR-030** (BR-001): Người dùng phải xuất PNG cho từng lớp được chọn hoặc composite tất cả layer đang hiển thị.
- **FR-031** (BR-001): PNG export phải dùng `GerberRasterRenderer` trực tiếp từ `GerberScene`; không chụp screenshot từ WebView2 và không rasterize từ kích thước hiện tại của canvas.
- **FR-032** (BR-001): Export phải bảo đảm bounding box và margin không cắt lẹm, đồng thời kiểm tra kích thước và bộ nhớ trước khi tạo `Bitmap`.
- **FR-033** (BR-001): Hình học SVG preview và PNG export phải dùng chung `GerberScene`; sai khác vị trí/kích thước giữa hai renderer phải nằm trong tolerance kiểm thử.

### 3.7. UI/UX WinForms

- **FR-034** (BR-002): Bố cục gồm ToolStrip trên cùng, SplitContainer, panel quản lý lớp bên trái, SVG preview bên phải và StatusStrip dưới cùng.
- **FR-035** (BR-002): ToolStrip tối thiểu gồm Open, Export DPI, Color Mode, Refresh Preview, Export Selected, Export Combined và Fit.
- **FR-036** (BR-002): Canvas phải zoom bằng wheel neo con trỏ, pan bằng kéo giữ chuột, Fit-to-view và hiển thị zoom factor.
- **FR-037** (BR-002): Thay đổi visible/color/opacity của layer phải cập nhật preview mà không parse lại file.
- **FR-038** (BR-002): Parse, tạo `GerberScene`, SVG generation nặng và PNG export phải chạy nền bằng `Task.Run` hoặc worker tương đương; cập nhật control phải qua `Invoke/BeginInvoke`.
- **FR-039** (BR-002): UI phải hiển thị các trạng thái riêng biệt: Parsing, Building scene, Generating SVG, Refining viewport, Exporting PNG và Ready.
- **FR-040** (BR-002): SVG preview host phải chặn navigation ra URL bên ngoài, chặn popup và chỉ hiển thị nội dung local/in-memory do ứng dụng tạo.

---

## 4. YÊU CẦU PHI CHỨC NĂNG (NFR)

- **NFR-001 — Parse/scene performance:** Một layer có tối đa 50.000 primitive phải được parse và dựng `GerberScene` trong ≤ 5 giây trên máy văn phòng tham chiếu.
- **NFR-002 — PNG performance:** Một layer có tối đa 50.000 primitive, board ≤ 100 × 100 mm, xuất 600 DPI phải hoàn tất trong ≤ 10 giây trên máy tham chiếu.
- **NFR-003 — Zoom responsiveness:** Với scene ≤ 50.000 primitive đã nạp, phản hồi camera transform sau wheel/drag phải bắt đầu trong ≤ 50 ms; frame refined phải sẵn sàng trong ≤ 300 ms ở điều kiện không cần tái tạo toàn bộ scene.
- **NFR-004 — DPI independence:** Cùng một camera và viewport, thay Export DPI giữa 150–1200 không được thay đổi SVG viewBox hoặc world-to-screen transform.
- **NFR-005 — Build:** Bắt buộc build x64.
- **NFR-006 — Robustness:** File Gerber lỗi cú pháp phải trả về cảnh báo theo dòng, tiếp tục render phần hợp lệ và không crash.
- **NFR-007 — Core isolation:** `GerberEngine.dll` không tham chiếu `System.Windows.Forms`, WebView2 hoặc project UI.
- **NFR-008 — Determinism:** Cùng input và render options phải tạo SVG có hình học tương đương và PNG cùng kích thước pixel.
- **NFR-009 — Memory:** Preview vector không được cấp phát bitmap toàn board ở Export DPI. Bitmap lớn chỉ được tạo trong luồng export và phải được dispose sau khi lưu.
- **NFR-010 — Offline operation:** Parse, preview và export phải hoạt động không cần Internet sau khi WebView2 Runtime đã sẵn sàng.
- **NFR-011 — Security:** SVG/HTML generated không được chứa script không cần thiết, remote URL, `foreignObject` hoặc active content từ file Gerber.
- **NFR-012 — Compatibility:** Chương trình phải xử lý Windows display scaling 100%, 125%, 150% và nhiều màn hình bằng `PerMonitorV2`.

---

## 5. THIẾT KẾ RENDER BẮT BUỘC

### 5.1. Pipeline dùng chung

```text
Gerber / X2 text
        ↓
GerberParser
        ↓
Ordered commands + graphics state
        ↓
GerberSceneBuilder / Plotter
        ↓
GerberScene (mm, vector, DPI-independent)
        ├── GerberSvgRenderer → SVG preview
        └── GerberRasterRenderer → Bitmap/PNG at Export DPI
```

Nguyên tắc bắt buộc:

1. Parser không được gọi API vẽ.
2. Renderer không được đọc trực tiếp raw Gerber text.
3. `GerberScene` là nguồn dữ liệu hình học duy nhất cho cả SVG và PNG.
4. Preview DPI và Export DPI là hai khái niệm tách biệt.
5. Thao tác camera không làm thay đổi world geometry.

### 5.2. SVG preview pipeline

```text
GerberScene
    ↓ layer filtering / color / opacity
SVG document with viewBox
    ↓
WebView2 local/in-memory host
    ↓ camera matrix / viewBox update
Browser vector re-rasterization
    ↓
Physical screen pixels
```

- SVG coordinate có thể dùng millimeter trực tiếp hoặc user unit theo tỉ lệ cố định, nhưng phải bảo toàn world measurement.
- `viewBox` xác định vùng world đang hiển thị.
- `devicePixelRatio` và DPI màn hình chỉ ảnh hưởng bước rasterize cuối; không thay đổi geometry.
- Không tạo lại SVG chỉ vì người dùng thay Export DPI.
- Có thể tái tạo một phần SVG khi viewport culling/LOD cần cập nhật scene chi tiết.

### 5.3. Progressive vector rendering

Hệ thống phải chia tương tác thành hai pha:

1. **Interactive transform**
   - Áp dụng matrix/viewBox ngay.
   - Ưu tiên phản hồi chuột.
   - Giữ nội dung cũ trong khi thao tác.

2. **Refinement**
   - Chạy sau debounce.
   - Query spatial index theo viewport mở rộng.
   - Bổ sung hoặc thay thế geometry chi tiết.
   - Browser/WebView2 rasterize lại vector ở độ phân giải hiện tại.
   - Swap document/layer group theo cách không tạo frame trắng.

Thuật ngữ chính thức dùng trong code và tài liệu:

- Vector zoom
- Browser re-rasterization
- Progressive refinement
- Viewport culling
- Level of Detail (LOD)
- Spatial index

Không gọi cơ chế này là “load ảnh DPI cao hơn” trừ khi một implementation tương lai thực sự dùng image tile pyramid.

### 5.4. PNG raster export pipeline

```text
GerberScene
    ↓
Combined bounds + margin
    ↓
mm → export pixels using DPI
    ↓
GDI+ Bitmap
    ↓
Layer-local polarity composition
    ↓
PNG encoder
```

DPI chỉ được sử dụng ở pipeline này và các tính năng export bitmap tương đương.

---

## 6. GIỚI HẠN VÀ RÀNG BUỘC WINFORMS 4.8

### 6.1. Giới hạn nền tảng

1. **WinForms không có retained-mode vector canvas gốc:** SVG phải được host qua WebView2 hoặc adapter tương đương; không ép preview vector thành bitmap DPI cố định.
2. **WebView2 là UI dependency:** không được rò rỉ type WebView2 vào `GerberEngine`.
3. **GDI+ vẫn chỉ chạy CPU:** chỉ dùng cho PNG export và bitmap fallback.
4. **Giới hạn Bitmap:** bitmap dùng khoảng 4 byte/pixel. Board 300 × 300 mm ở 1200 DPI xấp xỉ 14.173 × 14.173 pixel và khoảng 800 MB trước overhead; phải kiểm tra trước khi cấp phát.
5. **Single UI thread:** mọi cập nhật WinForms/WebView2 phải về UI thread.
6. **Ownership:** chuỗi SVG có thể truyền immutable; `Bitmap`, `Graphics`, `Pen`, `Brush`, `GraphicsPath` phải được dispose.
7. **DPI màn hình:** dùng `PerMonitorV2` và `AutoScaleMode.Dpi`; DPI màn hình không phải Export DPI.
8. **Mouse/keyboard focus:** preview host phải nhận wheel và keyboard shortcut ổn định.
9. **WebView2 initialization:** phải có trạng thái initializing/error/retry; không được gọi API CoreWebView2 trước khi initialization hoàn tất.
10. **Navigation security:** hủy mọi navigation không thuộc nội dung do ứng dụng tạo.

### 6.2. Phân cấp thiết kế UI trong `.Designer.cs`

1. `MainForm.Designer.cs` chỉ chứa `InitializeComponent()`, field control và `Dispose(bool)`.
2. Không viết parser, async workflow, SVG generation hoặc WebView2 event logic nghiệp vụ trong Designer.
3. `GerberPreviewHost` phải là UserControl/class riêng, đóng gói WebView2, initialization, navigation blocking và API `SetSvgAsync`.
4. Event handler được wire trong Designer nhưng thân handler nằm ở `MainForm.cs` hoặc controller/service phù hợp.
5. Bọc khởi tạo bằng `SuspendLayout/ResumeLayout`.
6. Kiểm soát đúng z-order của SplitContainer, StatusStrip và ToolStrip.
7. Danh sách layer có thể dùng `ListView` với checkbox và owner draw; không yêu cầu MVVM.

---

## 7. GIỚI HẠN PHẠM VI

- Chưa bắt buộc hỗ trợ Excellon/NC Drill, trừ khi lớp khoan được cung cấp dưới dạng Gerber.
- Chỉ đọc, hiển thị và xuất ảnh; không chỉnh sửa và không xuất ngược Gerber.
- SVG DOM rất lớn có thể chậm; viewport culling và LOD là yêu cầu mở rộng bắt buộc khi vượt ngưỡng hiệu năng.
- GDI+ export DPI cao vẫn chịu giới hạn RAM và thời gian theo số pixel.
- Arc phải giữ dạng cung vector trong `GerberScene`; chỉ được flatten thành segment khi renderer bắt buộc và phải dùng tolerance theo world/screen scale.
- SVG preview không phải bằng chứng CAM/fabrication; cần bộ reference test riêng.
- Bitmap fallback có thể kém sắc khi zoom sâu và chỉ là chế độ dự phòng, không phải đường preview chính.

---

## 8. KIẾN TRÚC SOLUTION VÀ API

```text
Solution GerberViewer.sln
├── GerberEngine
│   ├── Models
│   │   ├── GerberModels.cs
│   │   ├── GerberScene.cs
│   │   ├── GerberPrimitive.cs
│   │   └── RectangleD.cs
│   ├── Parsing
│   │   ├── GerberTokenizer.cs
│   │   ├── GerberParser.cs
│   │   └── GerberDiagnostics.cs
│   ├── Plotting
│   │   ├── GerberGraphicsState.cs
│   │   ├── GerberSceneBuilder.cs
│   │   └── ApertureMacroProcessor.cs
│   ├── Spatial
│   │   ├── ISpatialIndex.cs
│   │   └── RTreeSpatialIndex.cs
│   ├── Rendering
│   │   ├── GerberSvgRenderer.cs
│   │   ├── GerberRasterRenderer.cs
│   │   ├── SvgRenderOptions.cs
│   │   └── RasterRenderOptions.cs
│   ├── CoordinateTransformer.cs
│   └── GerberEngineFacade.cs
└── GerberViewer
    ├── Program.cs
    ├── MainForm.cs
    ├── MainForm.Designer.cs
    ├── GerberPreviewHost.cs
    ├── GerberPreviewHost.Designer.cs
    ├── ViewportController.cs
    ├── PreviewRenderCoordinator.cs
    └── app.manifest
```

### 8.1. API facade

```csharp
public sealed class GerberEngineFacade
{
    public IReadOnlyList<GerberLayer> Layers { get; }

    public GerberLayer LoadLayer(string filePath);
    public void RemoveLayer(GerberLayer layer);
    public void MoveLayer(GerberLayer layer, int newIndex);

    public GerberScene BuildScene(SceneBuildOptions options);
    public RectangleD GetCombinedBoundsMm();

    public string RenderLayerSvg(
        GerberLayer layer,
        SvgRenderOptions options);

    public string RenderCombinedSvg(
        SvgRenderOptions options);

    public Bitmap RenderLayerBitmap(
        GerberLayer layer,
        RasterRenderOptions options);

    public Bitmap RenderCombinedBitmap(
        RasterRenderOptions options);

    public void ExportLayerPng(
        GerberLayer layer,
        RasterRenderOptions options,
        string path);

    public void ExportCombinedPng(
        RasterRenderOptions options,
        string path);

    public event EventHandler<EngineProgressEventArgs> ProgressChanged;
}
```

### 8.2. SVG options

```csharp
public sealed class SvgRenderOptions
{
    public double MarginMm { get; set; }
    public ColorMode Mode { get; set; }
    public string BackgroundCss { get; set; }
    public RectangleD? ViewportMm { get; set; }
    public double LodScreenTolerancePx { get; set; }
    public bool EnableViewportCulling { get; set; }
    public bool ReuseDefinitions { get; set; }
}
```

`SvgRenderOptions` không được có thuộc tính DPI xuất ảnh.

### 8.3. Raster options

```csharp
public sealed class RasterRenderOptions
{
    public int Dpi { get; set; }             // 150/300/600/1200
    public ColorMode Mode { get; set; }
    public double MarginMm { get; set; }
    public Color Background { get; set; }
    public bool InvertBinary { get; set; }
}
```

### 8.4. Viewport state

```csharp
public sealed class ViewportState
{
    public RectangleD WorldViewportMm { get; set; }
    public double ZoomFactor { get; set; }
    public double DevicePixelRatio { get; set; }
}
```

`DevicePixelRatio` chỉ phục vụ ước lượng LOD hoặc screen tolerance, không thay đổi world geometry.

---

## 9. TRUY VẾT (Traceability)

| Requirement | File hiện thực | Kiểm thử gợi ý |
|---|---|---|
| FR-001–FR-003, FR-009 | `GerberTokenizer.cs`, `GerberParser.cs` | Parse file mẫu KiCad/Altium và file lỗi |
| FR-005–FR-008 | `GerberSceneBuilder.cs`, `ApertureMacroProcessor.cs`, model classes | So sánh primitive/bounds với reference |
| FR-010–FR-013 | `CoordinateTransformer.cs`, `ViewportController.cs` | Round-trip world/screen; zoom anchor |
| FR-014–FR-020 | `GerberSvgRenderer.cs` | Validate XML/SVG; compare bounds; mask polarity |
| FR-021–FR-027 | `GerberPreviewHost.cs`, `ViewportController.cs`, `PreviewRenderCoordinator.cs`, spatial index | Wheel/pan stress test; timing; no white flash |
| FR-028–FR-033 | `GerberRasterRenderer.cs`, `GerberEngineFacade.cs` | Export 4 DPI × 2 color modes |
| FR-034–FR-040 | `MainForm.*`, `GerberPreviewHost.*` | UI test; thread test; navigation blocking |
| NFR-003, NFR-004 | Preview benchmark and viewport tests | Measure latency; confirm DPI independence |
| NFR-006 | Parser diagnostics | Inject malformed commands |
| NFR-007 | `GerberEngine.csproj` | Verify references |
| NFR-009 | Memory test | Confirm no Export-DPI preview bitmap |
| NFR-011 | SVG security test | Reject external URL/active content |

Không được có requirement mồ côi; mọi FR/NFR phải truy vết về BR-001 đến BR-005.

---

## 10. ACCEPTANCE CRITERIA

### 10.1. Gherkin scenarios

```gherkin
Scenario: Preview SVG không phụ thuộc Export DPI
  Given người dùng đã nạp một Gerber layer và Fit-to-view
  And Export DPI đang là 150
  When người dùng thay Export DPI thành 1200
  Then SVG viewBox và camera không thay đổi
  And kích thước logic của board trên preview không thay đổi
  And không tạo bitmap preview 1200 DPI

Scenario: Zoom vector giữ độ sắc nét
  Given SVG preview đã hiển thị board
  When người dùng zoom 20 lần tại cùng một vùng
  Then world point dưới con trỏ vẫn được giữ ổn định
  And browser re-rasterize vector ở mức zoom mới
  And preview không dùng screenshot phóng lớn làm kết quả cuối

Scenario: Progressive refinement không làm trắng canvas
  Given board lớn cần viewport culling
  When người dùng pan nhanh sang một vùng mới
  Then frame hiện tại được transform ngay
  And refinement chạy sau debounce
  And geometry chi tiết mới được thay thế mà không xuất hiện frame trắng

Scenario: SVG và PNG dùng chung geometry
  Given một layer chứa flash, line, arc, region, LPD và LPC
  When hệ thống tạo SVG preview và PNG 600 DPI
  Then bounding box vật lý của hai kết quả tương đương
  And clear polarity chỉ tác động trong layer của nó

Scenario: Nạp nhiều lớp và render gộp
  Given người dùng kéo-thả ba file .gtl, .gts và .gto
  When cả ba layer ở trạng thái visible
  Then SVG preview hiển thị composite theo đúng thứ tự và màu
  And StatusStrip hiển thị kích thước board theo mm
```

### 10.2. Checklist

- [ ] ToolStrip có Open, Export DPI, Color Mode, Refresh Preview, Export Selected, Export Combined và Fit.
- [ ] Preview chính là SVG vector, không phải bitmap được tạo ở Export DPI.
- [ ] Đổi Export DPI không thay đổi preview.
- [ ] Wheel zoom neo con trỏ; pan không flicker.
- [ ] Sau zoom sâu, đường và aperture được browser re-rasterize rõ nét.
- [ ] Layer visible/color/opacity cập nhật không parse lại.
- [ ] LPC không khoét xuyên layer dưới trong cả SVG và PNG.
- [ ] PNG export không dùng screenshot WebView2.
- [ ] Board lớn có viewport culling/LOD hoặc cảnh báo rõ khi vượt ngưỡng.
- [ ] Build x64 và `PerMonitorV2`.
- [ ] `GerberEngine.dll` không tham chiếu WinForms hoặc WebView2.
- [ ] SVG không có URL hoặc active content bên ngoài.
