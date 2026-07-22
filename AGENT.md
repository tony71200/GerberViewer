# AGENT.md — GerberViewer Repository Rules

## 1. Mục đích và mức độ ưu tiên

File này định nghĩa các nguyên tắc bắt buộc cho mọi tác vụ Codex trong repository:

```text
tony71200/GerberViewer
```

Nhánh triển khai hiện tại:

```text
2026-07-21_use-agent.md-as-program-title
```

Ưu tiên hiện tại:

1. Hoàn thiện **Tab 3 — Align and Stitching**.
2. Xây dựng matcher production:

   * `IMatcher`
   * `EccMatcher`
   * `PharseCorrMatcher`
   * `NCC_HalconMatcher`
3. Hoàn thiện `resultTabControl/tabComparison` trong `AlignStitchingControl`.
4. Giữ nguyên contract dữ liệu giữa Tab 2 và Tab 3.
5. Không làm hỏng Tab 1 và Tab 2.
6. Không báo hoàn thành nếu chỉ kiểm tra source tĩnh mà chưa build hoặc chạy test cần thiết.

Các quy tắc trong file này có hiệu lực cao hơn các tài liệu cũ có nội dung cho rằng Tab 3 nằm ngoài phạm vi.

---

# 2. Công nghệ và giới hạn môi trường

Solution sử dụng:

```text
C# 7.3
.NET Framework 4.8
WinForms
HALCON 25.05
OpenCvSharp4
System.Drawing
EWindowControl
ELog_1_0
GerberStitching.Core
```

Quy tắc bắt buộc:

* Không dùng cú pháp mới hơn C# 7.3.
* Không chuyển project sang .NET Core, .NET 5+ hoặc SDK-style project.
* Không đổi target framework.
* Không thay HALCON 25.05 bằng thư viện khác.
* Không thay OpenCvSharp bằng OpenCV wrapper khác.
* Không tạo `.ps1` trừ khi người dùng yêu cầu rõ ràng.
* Không thêm dependency mới khi chưa chứng minh dependency hiện có không đáp ứng được.
* Các project có HALCON phải ưu tiên build `x64`.
* Không tuyên bố `AnyCPU` hoạt động nếu chưa kiểm thử với HALCON runtime thực tế.

---

# 3. Phạm vi project

Các project chính:

```text
GerberViewer
GerberEngine
GerberStitching.Core
EWindowControl
EasyFile
Elog_1_0
```

Phân chia trách nhiệm:

## 3.1 `GerberViewer`

Chỉ chứa:

* WinForms UI.
* UserControl.
* Dialog.
* View model dành cho UI.
* UI event wiring.
* Chuyển dữ liệu từ domain sang preview.
* Hiển thị progress, diagnostics và logs.
* UI-thread dispatch.

Không đặt thuật toán alignment, HALCON matching, OpenCV matching hoặc stitching production trực tiếp trong event handler.

## 3.2 `GerberStitching.Core`

Chứa:

* Matcher contract.
* HALCON NCC matcher.
* OpenCV phase-correlation matcher.
* OpenCV ECC matcher.
* Preprocessing.
* Transform conversion.
* Alignment pipeline.
* Recovery pipeline.
* Stitching.
* Comparison generation.
* Manifest model và validator.
* Processing report.
* Image interoperability.
* Resource ownership.

## 3.3 `EWindowControl`

Được xem là shared/external source và mặc định chỉ đọc.

Không trực tiếp:

* sửa file,
* đổi tên,
* format lại,
* regenerate Designer,
* expose private field chỉ để phục vụ Tab 3,
* copy toàn bộ class sang project khác.

Khi cần mở rộng, dùng:

* inheritance,
* composition,
* adapter,
* wrapper control,
* public event hoặc public API hiện có.

---

# 4. Source được bảo vệ

Không xóa, đổi tên, di chuyển hoặc ghi đè các source sau:

```text
reference/
Sources/
third_party/
vendor/
external/
legacy/
ZIP archives
sample images
sample Gerber files
HALCON reference code
OpenCV reference code
```

`reference/` là read-only.

Được phép:

* đọc,
* phân tích,
* port có chọn lọc,
* chuyển namespace,
* chuyển model,
* viết lại theo contract production,
* tạo file production mới dựa trên ý tưởng từ reference.

Không được phép:

* copy nguyên project reference vào solution production,
* giữ namespace reference trong production,
* kéo theo các class không liên quan,
* sửa trực tiếp file dưới `reference/`,
* thêm reference project vào production chỉ để dùng tạm.

Nếu một file có vẻ không sử dụng:

* không tự xóa,
* ghi nhận trong báo cáo,
* chỉ loại khỏi build khi có bằng chứng và không làm hỏng runtime,
* cần phê duyệt người dùng trước khi xóa source ngoài phạm vi hiện tại.

---

# 5. Quy tắc bảo vệ Tab 1 và Tab 2

Tab 3 được phép đọc output từ Tab 2 nhưng không được phá vỡ workflow hiện tại.

Contract bắt buộc:

```text
Captured image OrderIndex K
↔
Sample tile OrderIndex K
```

Không remap bằng:

* thứ tự JSON hiện tại,
* row/column gần nhất,
* robot coordinate,
* filename tùy ý,
* content similarity,
* selection index của ListBox.

Phải:

1. Validate manifest.
2. Natural sort captured images.
3. Gán hoặc xác nhận `OrderIndex`.
4. Map bằng dictionary theo `OrderIndex`.
5. Block execution nếu thiếu, trùng hoặc sai count.

Không tạo DTO manifest thứ hai dành riêng cho Tab 3.

---

# 6. Canonical coordinate spaces

Mọi code mới phải dùng tên coordinate space rõ ràng.

Các coordinate space chính:

```text
SampleTileLocalPixels
ProcessedSampleGlobalPixels
CapturedImageLocalPixels
StitchedCanvasPixels
OriginalSamplePixels
PreviewPixels
```

Ý nghĩa:

```text
SampleTileInfo.ExpectedX / ExpectedY
=
tọa độ góc tile trong ProcessedSampleGlobalPixels
```

Không gọi các giá trị này là:

```text
RobotX
RobotY
MachineX
MachineY
```

trừ khi dữ liệu thực sự đã được chuyển sang robot hoặc machine coordinate.

---

# 7. Canonical transform direction

Đây là quy tắc quan trọng nhất của Tab 3.

## 7.1 Matcher contract

Mọi matcher nhận:

```text
Reference image
Moving image
```

và trả về:

```text
MovingToReferenceTransform
```

Không dùng tên mơ hồ như:

```text
src
dst
image1
image2
```

trong public contract nếu không có giải thích rõ vai trò.

## 7.2 Camera-to-sample

```text
Reference = Sample tile
Moving    = Captured camera image

Output:
CapturedToSampleTransform
```

Direction:

```text
CapturedImageLocalPixels
→
SampleTileLocalPixels
```

## 7.3 Neighbor recovery

```text
Reference = Anchor captured image
Moving    = Target captured image

Output:
TargetToAnchorTransform
```

Direction:

```text
TargetCapturedLocalPixels
→
AnchorCapturedLocalPixels
```

Global pose:

```text
TargetGlobalPose
=
AnchorGlobalPose
×
TargetToAnchorTransform
```

## 7.4 Global camera pose

```text
CapturedGlobalPose
=
Translation(tile.ExpectedX, tile.ExpectedY)
×
CapturedToSampleTransform
```

Direction:

```text
CapturedImageLocalPixels
→
ProcessedSampleGlobalPixels
```

## 7.5 Adapter boundary

HALCON và OpenCV có thể có convention khác nhau.

Tại adapter boundary phải:

1. Ghi rõ library trả về transform nào.
2. Convert hoặc invert đúng một lần.
3. Chuyển sang canonical direction.
4. Không invert lại trong workflow.
5. Có synthetic test chứng minh direction.

Không chỉ dựa vào comment.

---

# 8. Canonical matrix representation

Domain layer sử dụng một representation duy nhất:

```csharp
double[,]
```

với kích thước:

```text
3 × 3
```

hoặc một wrapper immutable như:

```csharp
Transform2D
```

nếu wrapper được triển khai đầy đủ.

Adapter được phép dùng:

```text
OpenCV: Mat CV_64F
HALCON: HTuple HomMat2D
```

Conversion phải tập trung trong một service hoặc utility có test.

Không truyền `Mat` hoặc `HTuple` xuyên suốt toàn bộ domain nếu không có ownership contract.

---

# 9. Kiến trúc Matcher bắt buộc

Các file production matcher phải nằm trong:

```text
GerberStitching.Core/Matching/
```

Tối thiểu gồm:

```text
IMatcher.cs
MatchRequest.cs
MatchResult.cs
MatcherOptions.cs
MatcherFactory.cs
MatcherGeometryValidator.cs
EccMatcher.cs
PharseCorrMatcher.cs
NCC_HalconMatcher.cs
```

Có thể thêm:

```text
MatcherBase.cs
PreparedMatchImage.cs
MatcherPipeline.cs
MatcherTransformConverter.cs
MatcherDiagnostics.cs
```

nếu cần.

## 9.1 `IMatcher`

`IMatcher` là public contract chung.

Tối thiểu:

```csharp
public interface IMatcher : IDisposable
{
    string MatcherName { get; }

    MatchResult Match(
        MatchRequest request,
        CancellationToken cancellationToken);
}
```

Không tiếp tục dùng contract:

```csharp
Run(Bitmap src, Rectangle srcRoi, Bitmap dst, Rectangle dstRoi)
```

nếu vai trò reference/moving không rõ ràng.

## 9.2 `MatchRequest`

Phải chứa rõ:

```text
Reference image
Moving image
Reference ROI hoặc mask
Moving ROI hoặc mask
Initial MovingToReference transform
Matcher options
Purpose
OrderIndex hoặc pair context
```

Ownership của image phải được ghi rõ.

## 9.3 `MatchResult`

Phải tách:

```text
Success
MovingToReferenceTransform
TranslationX
TranslationY
RotationDeg
Scale
RawScore
NormalizedConfidence
OverlapRatio
MatcherName
FailureReason
ProcessingTime
Diagnostics
```

Không ghi NCC score vào `EccCorrelation`.

Không ghi phase response vào NCC score.

Không so sánh trực tiếp score giữa các thuật toán nếu chưa normalize bằng policy rõ ràng.

---

# 10. Quy tắc `NCC_HalconMatcher`

`NCC_HalconMatcher` phải dùng HALCON NCC thật.

Lifecycle bắt buộc:

```text
create_ncc_model
find_ncc_model
clear_ncc_model
```

hoặc API HALCON 25.05 tương đương chính xác.

Không dùng:

```csharp
new object()
```

hoặc class chỉ lưu Width/Height làm model giả.

Matcher phải:

* tạo model theo sample tile,
* cache model theo stable key,
* bao gồm preprocessing variant trong key,
* hỗ trợ angle range cấu hình,
* trả về HALCON score thật,
* convert Row/Column/Angle thành canonical transform,
* dispose từng model đúng một lần,
* không gọi `clear_all_ncc_models`,
* block success nếu score dưới threshold,
* hỗ trợ cancellation giữa các tile,
* ghi context khi lỗi.

Cache key nên bao gồm:

```text
SampleTileId
PreprocessingVariant
AngleStart
AngleExtent
AngleStep
Metric-related options
```

Nếu matcher được chia sẻ giữa worker threads, cache phải thread-safe hoặc matcher phải dùng theo run-scope riêng.

---

# 11. Quy tắc `EccMatcher`

`EccMatcher` phải gọi:

```csharp
Cv2.FindTransformECC(...)
```

Không dùng NCC proxy.

Phải hỗ trợ pyramid coarse-to-fine.

Tại mỗi level:

1. Resize hoặc lấy pyramid image.
2. Scale initial transform đúng level.
3. Chạy `FindTransformECC`.
4. Kiểm tra matrix finite.
5. Propagate transform lên level tiếp theo.
6. Lưu correlation thật.
7. Dispose temporary `Mat`.

Motion model được phép:

```text
Translation
Euclidean
Affine
```

Default nên là:

```text
Euclidean
```

Không bật Homography nếu chưa có test đầy đủ.

Phải dùng config:

```text
PyramidLevels
EccIterations
EccEpsilon
EccMinCorrelation
```

Nếu ECC không hội tụ:

* không throw ra ngoài mà không có context,
* trả `Success = false`,
* ghi failure reason,
* không thay bằng score giả.

---

# 12. Quy tắc `PharseCorrMatcher`

Tên `PharseCorrMatcher` được giữ theo yêu cầu hiện tại dù spelling không chuẩn.

Implementation phải dùng:

```csharp
Cv2.PhaseCorrelate(...)
```

Không được dùng brute-force `double[,]` rồi gọi là phase correlation.

Phải:

* dùng grayscale single-channel,
* convert sang `CV_32F`,
* bảo đảm hai Mat cùng kích thước,
* dùng Hanning window thật,
* trả response thật,
* chỉ claim translation,
* không claim rotation hoặc scale,
* dispose window và temporary Mat,
* hỗ trợ cancellation,
* validate ROI và overlap.

Vai trò ưu tiên:

```text
captured-to-captured neighbor matching
```

Không mặc định dùng phase correlation làm camera-to-Gerber matcher chính nếu modality khác nhau đáng kể.

---

# 13. Matcher pipeline

Không chạy tất cả matcher rồi chọn score lớn nhất.

## 13.1 Direct camera-to-sample

Default:

```text
Preprocess candidates
→ HALCON NCC coarse
→ geometry validation
→ OpenCV Pyramid ECC refinement
→ final geometry validation
→ accept hoặc reject
```

Policy:

```text
NCC pass + ECC pass
    dùng ECC result

NCC pass + ECC fail
    chỉ dùng NCC nếu AllowNccOnlyAcceptance = true
    và NCC geometry vẫn hợp lệ

NCC fail
    có thể chạy ECC từ expected transform
    nếu AllowEccFromExpectedWhenNccFails = true

Tất cả fail
    chuyển recovery
```

## 13.2 Neighbor recovery

Default:

```text
Select solved anchor
→ select overlap ROI
→ PharseCorrMatcher coarse
→ optional EccMatcher refinement
→ pair geometry validation
→ compose target global pose
```

Acceptance phải dựa trên:

```text
score
overlap
finite matrix
direction consistency
translation deviation
rotation deviation
expected neighbor geometry
```

Không hard-code:

```csharp
IsMatch = true;
Score = 1;
```

---

# 14. Preprocessing

Không sử dụng production path dựa trên:

```text
Bitmap.GetPixel
Bitmap.SetPixel
double[,]
float[,]
```

cho ảnh lớn hoặc matcher chính.

Canonical policy:

```text
Decode và sample lifecycle: HALCON HObject
OpenCV alignment/warping: OpenCvSharp Mat
WinForms bounded preview: Bitmap
```

Preprocessing có thể gồm:

```text
grayscale
contrast normalization
illumination normalization
polarity inversion
fixed threshold
Otsu threshold
adaptive threshold
Sobel
Canny
content mask
valid image mask
working-scale resize
```

Nếu enum hoặc PropertyGrid expose một mode:

* phải implement thật,
* hoặc disable/remove mode,
* không map tất cả mode về cùng một implementation.

`PolarityMode.Auto` phải thử candidate thật và ghi candidate được chọn.

---

# 15. Image interoperability và ownership

Nên có service:

```text
GerberStitching.Core/Imaging/ImageInterop/
```

Tối thiểu:

```csharp
HObject ToHObjectCopy(Mat source);
Mat ToMatCopy(HObject source);
Bitmap ToBitmapCopy(Mat source);
Mat ToMatCopy(Bitmap source);
```

Quy tắc:

* Method name phải nói rõ copy hoặc ownership.
* Không trả buffer dựa trên memory đã dispose.
* Không giữ pointer tạm của HALCON trong Bitmap.
* Ghi rõ RGB/BGR.
* Hỗ trợ Mono8, Mono16 và 3-channel color.
* Không convert full-size stitched image sang Bitmap chỉ để preview.
* Mọi `Mat`, `Bitmap`, `HObject`, `HTuple`, model ID phải có owner rõ ràng.

Class giữ resource vượt khỏi method scope phải implement `IDisposable`.

---

# 16. WinForms và Designer

Mọi UserControl production có layout phải có:

```text
Control.cs
Control.Designer.cs
Control.resx
```

Nếu reference control có `.Designer.cs`, khi port phải sử dụng cấu trúc Designer tương ứng.

`.Designer.cs` chỉ được chứa:

* declarations,
* property assignments,
* layout,
* `SuspendLayout`/`ResumeLayout`,
* event wiring,
* standard `Dispose(bool)`.

Không đặt trong `.Designer.cs`:

* HALCON operators,
* OpenCV processing,
* matching,
* image loading,
* JSON logic,
* async workflow,
* stitching,
* comparison generation,
* report writing.

Giữ formatting tương thích Visual Studio Designer.

Cập nhật `.csproj` đúng:

```xml
<SubType>UserControl</SubType>
<DependentUpon>...</DependentUpon>
<EmbeddedResource ...>
```

Không tạo toàn bộ UI bằng code trong constructor nếu control cần chỉnh bằng Visual Studio Designer.

---

# 17. `AlignStitchingControl`

`AlignStitchingControl` chịu trách nhiệm:

* chọn manifest,
* chọn captured folder,
* chọn output folder,
* bind config,
* start/cancel workflow,
* nhận progress,
* update UI snapshot,
* hiển thị result,
* hiển thị comparison,
* hiển thị log.

Không chứa matcher algorithm.

Phải sửa đúng lifecycle của `WorkflowContext`:

```text
unsubscribe old context
assign new context
subscribe new context
refresh UI
```

Lần gán đầu tiên khi field đang `null` vẫn phải hoạt động.

Không giữ hai `AlignStitchConfig` khác nhau trong Core và UI.

---

# 18. `resultTabControl/tabComparison`

`tabComparison` không được chỉ giữ một `PictureBox`.

Phải tạo production control:

```text
GerberViewer/Views/SampleComparisonControl.cs
GerberViewer/Views/SampleComparisonControl.Designer.cs
GerberViewer/Views/SampleComparisonControl.resx
```

Có thể tạo thêm:

```text
GerberViewer/Views/ComparisonImageView.cs
```

Layout phải tham khảo có chọn lọc từ:

```text
reference/StitchingImage/StitchingImage/
Stitch_Tools/DesignControls/OffsetPreviewControl.cs

reference/StitchingImage/StitchingImage/
Stitch_Tools/DesignControls/OffsetPreviewControl.Designer.cs
```

Không copy nguyên các dependency về:

```text
Robot distance
Robot delta
TraversalBatchResult
EdgeInfo
PairMatching
Horizontal/vertical robot offsets
```

nếu không phù hợp với sample comparison.

Layout mục tiêu:

```text
35% bên trái:
    comparison mode
    alpha
    blink interval
    metrics
    coordinate status
    save/open actions
    status

65% bên phải:
    Reference preview
    Comparison/output preview
```

Hai preview có thể đặt trong `SplitContainer` theo chiều ngang như reference.

Mode tối thiểu:

```text
Sample only
Stitched only
Alpha overlay
Absolute difference
Edge overlay
Blink comparison
```

Control phải:

* zoom,
* pan,
* fit,
* dispose ảnh cũ,
* không sửa source image khi thay alpha,
* không giữ full-resolution bitmap,
* hiển thị warning nếu coordinate space không authoritative.

Trong `AlignStitchingControl.Designer.cs`:

* loại `picComparison`,
* thêm `SampleComparisonControl`,
* `Dock = Fill`.

---

# 19. Comparison coordinate rules

Authoritative comparison phải dùng cùng coordinate space:

```text
ProcessedSampleGlobalPixels
```

Nguồn sample ưu tiên:

```text
ProcessedSamplePath
```

Manifest cần hỗ trợ metadata:

```text
ProcessedSamplePath
SourceToProcessedTransform
PreprocessMode
```

Nếu manifest cũ không có:

* chỉ dùng `SourceRasterPath` khi source và processed geometry giống nhau,
* nếu chỉ resize và transform xác định được, reconstruct rõ ràng,
* với FitPad/CenterCrop phải có transform đầy đủ,
* nếu không xác định được mapping, block authoritative overlay,
* không resize tùy ý rồi báo comparison chính xác.

Comparison output tối thiểu:

```text
sample_reference_preview.png
stitched_preview.png
overlay_comparison.png
difference_comparison.png
edge_comparison.png
comparison_metadata.json
```

Metrics không được chỉ có một “accuracy percentage”.

Nên báo:

```text
valid overlap ratio
edge overlap
binary mask IoU
normalized cross-correlation
distance-transform error
```

---

# 20. Stitching

Chỉ stitch state có:

```text
IsStitchable == true
```

Không suy luận alignment success và stitchability từ một enum duy nhất.

State nên có:

```text
AlignmentSucceeded
IsFallbackPose
IsStitchable
PoseSource
```

Stitcher phải:

* map image bằng `OrderIndex`,
* transform đủ bốn góc để tính bounds,
* warp image,
* warp valid mask,
* hỗ trợ rotation/scale thật,
* blend overlap,
* bỏ black-border contamination,
* tạo bounded preview,
* giữ full-resolution output độc lập với preview scale,
* hỗ trợ cancellation.

Không dùng:

```text
Row + ":" + Column
```

làm identity chính khi `OrderIndex` đã tồn tại.

Không tạo full-size `Bitmap` nếu output có thể vượt giới hạn memory.

---

# 21. Threading và cancellation

Các tác vụ sau phải chạy ngoài UI thread:

```text
manifest validation lớn
captured image validation
decode
preprocessing
HALCON NCC
OpenCV phase correlation
OpenCV ECC
neighbor recovery
stitching
TIFF/BigTIFF writing
comparison generation
report serialization
```

Dùng:

```text
async/await
CancellationTokenSource
CancellationToken
IProgress<T>
immutable UI snapshots
```

Không dùng:

```text
Application.DoEvents()
Thread.Sleep()
Task.Result trên UI thread
Task.Wait() trên UI thread
Control access từ worker
```

Phải:

* disable control gây xung đột khi chạy,
* prevent double Run,
* restore UI trong `finally`,
* không thay manifest/folder trong khi chạy,
* cancel trước stage tốn thời gian tiếp theo,
* không publish completed output sau cancellation.

Manual dialog phải được marshal về UI thread.

---

# 22. Logging và error handling

Không dùng:

```csharp
catch
{
}
```

Không bỏ qua exception.

Mọi lỗi matcher phải chứa context:

```text
OrderIndex
Row
Column
Sample path
Captured path
Matcher name
Stage
Image dimensions
Channel
Bit depth
Preprocessing variant
Transform direction
```

Phân biệt:

```text
Rejected match
Cancellation
Runtime failure
Invalid input
Unsupported pixel format
```

Logging không thay thế structured report.

Phải có:

```text
human-readable log
processing_report.json
```

Không xóa log hoặc output root của người dùng.

---

# 23. Output safety

Không được:

```csharp
Directory.Delete(userSelectedOutputRoot, true);
```

Tạo thư mục application-owned:

```text
<OutputRoot>/
└─ AlignStitch_<runId>/
   └─ .creating/
```

Success:

```text
validate outputs
→ write report
→ write comparison metadata
→ reopen output for verification
→ publish final directory
```

Failure/cancellation:

```text
do not publish completed report
do not update LastStitchedOutputPath
clean only current .creating directory
preserve previous successful output
```

---

# 24. Build và test

Sau mỗi task logic:

```text
Build Debug x64
Build Release x64
Run relevant tests
```

Nếu môi trường không có:

```text
MSBuild
HALCON runtime
HALCON license
OpenCV native runtime
test images
```

phải báo chính xác:

* command đã chạy,
* lỗi nhận được,
* phần nào chưa được xác minh,
* không đánh dấu task complete dựa trên static inspection.

Test tối thiểu:

## Matcher synthetic tests

```text
translation
rotation
noise
illumination gradient
normal polarity
inverted polarity
partial overlap
low texture rejection
invalid ROI
cancellation
```

## Mapping tests

```text
1×1
2×2
4×4
8×8
filename 1, 2, 10
duplicate OrderIndex
missing OrderIndex
count mismatch
```

## Runtime smoke tests

```text
load real manifest
load real captured folder
run NCC
run ECC
run neighbor matcher
stitch output
open stitched TIFF
generate comparison
change comparison mode
cancel running operation
close Form safely
```

Test phải kiểm tra transform direction, không chỉ score.

---

# 25. Quy trình bắt buộc cho mỗi task

Trước khi sửa:

1. Đọc `AGENT.md`.
2. Đọc `docs/Codex_Prompt_Fix_Tab3_implement_v2.md`.
3. Đọc task hiện tại.
4. Kiểm tra branch hiện tại.
5. Kiểm tra working tree.
6. Ghi baseline build/test.
7. Liệt kê file dự kiến tạo và thay đổi.
8. Xác định resource ownership.
9. Xác định ảnh hưởng tới manifest và transform contract.

Trong khi sửa:

1. Chỉ sửa phạm vi task.
2. Không cleanup không liên quan.
3. Giữ solution buildable sau từng nhóm thay đổi.
4. Không sửa `reference/`.
5. Không sửa `EWindowControl`.
6. Không tạo fake success.
7. Không để placeholder được gọi từ production UI.
8. Cập nhật `.csproj`, `.Designer.cs`, `.resx` cùng task nếu tạo control.
9. Thêm test cùng implementation.
10. Dispose resource rõ ràng.

Sau khi sửa:

1. Build Debug x64.
2. Build Release x64.
3. Chạy test task hiện tại.
4. Chạy regression test liên quan.
5. Liệt kê file thay đổi.
6. Liệt kê class/method thay đổi.
7. Ghi resource ownership.
8. Ghi transform direction.
9. Ghi phần chưa test được.
10. Không dùng từ “complete” nếu acceptance criteria chưa đạt.

---

# 26. Definition of Done

Tab 3 chỉ được coi là hoàn thành khi:

* `WorkflowContext` hoạt động từ lần gán đầu.
* Chỉ còn một canonical `AlignStitchConfig`.
* `IMatcher` production được sử dụng.
* `NCC_HalconMatcher` dùng HALCON NCC thật.
* `EccMatcher` dùng `Cv2.FindTransformECC`.
* `PharseCorrMatcher` dùng `Cv2.PhaseCorrelate`.
* Không còn NCC/ECC proxy trong production.
* Transform direction có synthetic test.
* Direct alignment chạy theo NCC → ECC policy.
* Neighbor recovery có image-based matching thật.
* Global pose được compose đúng direction.
* Stitcher dùng `OrderIndex`.
* Bounds được tính từ transformed corners.
* Image và valid mask được warp.
* Comparison dùng `ProcessedSampleGlobalPixels`.
* `SampleComparisonControl` có `.cs`, `.Designer.cs`, `.resx`.
* `tabComparison` không còn chỉ là `PictureBox`.
* Preview hỗ trợ zoom/pan/fit.
* Resource được dispose rõ ràng.
* Cancellation không publish output giả.
* Debug x64 và Release x64 được build hoặc báo trung thực lý do không build.
* Synthetic tests chạy.
* Ít nhất một real-data smoke test được ghi nhận.
* Không sửa source dưới `reference/`.
* Không sửa `EWindowControl`.
* Không phá Tab 1 hoặc Tab 2.
