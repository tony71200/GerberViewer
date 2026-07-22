# Codex Prompt — Fix and Complete Tab 3 Implementation v2

## 1. Vai trò

Bạn đang sửa một solution C# WinForms production:

```text
Repository:
tony71200/GerberViewer
```

Công nghệ:

```text
C# 7.3
.NET Framework 4.8
WinForms
HALCON 25.05
OpenCvSharp4
EWindowControl
ELog_1_0
GerberStitching.Core
```

Mục tiêu chính:

```text
Hoàn thiện Tab 3 — Align and Stitching.
```

Tập trung vào:

1. Xây dựng matcher production:

   * `IMatcher`
   * `EccMatcher`
   * `PharseCorrMatcher`
   * `NCC_HalconMatcher`
2. Thay các alignment placeholder hiện tại.
3. Hoàn thiện camera-to-sample alignment.
4. Hoàn thiện neighbor recovery.
5. Stitch camera images bằng global transforms.
6. So sánh stitched result với processed Gerber sample.
7. Thay `tabComparison` bằng UserControl có cấu trúc tương tự `OffsetPreviewControl`.
8. Giữ Designer trong `.Designer.cs`.
9. Thêm test có thể chạy bằng dữ liệu synthetic và dữ liệu thực.

Luôn dùng:

```text
AGENT.md
```

làm nguyên tắc code bắt buộc.

File này là định hướng triển khai chi tiết.

---

# 2. Quy tắc ưu tiên

Trước khi sửa code:

1. Đọc toàn bộ `AGENT.md`.
2. Đọc toàn bộ file này.
3. Đọc task được giao.
4. Kiểm tra branch hiện tại.
5. Kiểm tra working tree.
6. Không sửa ngoài phạm vi task.
7. Không báo runtime thành công nếu chưa chạy runtime test.

Nếu nội dung cũ nói Tab 3 nằm ngoài phạm vi, nội dung đó bị thay thế bởi:

```text
Tab 3 hiện là mục tiêu triển khai chính.
```

Các rule còn lại vẫn giữ nguyên:

* Không sửa `EWindowControl`.
* Không sửa source trong `reference/`.
* Không xóa external, legacy, sample hoặc ZIP.
* Không tạo fake matcher.
* Không sử dụng C# mới hơn 7.3.
* Không phá Tab 1 hoặc Tab 2.

---

# 3. Source bắt buộc phải đọc

## 3.1 Production source

Đọc đầy đủ:

```text
AGENT.md

GerberViewer/MainForm.cs
GerberViewer/MainForm.Designer.cs

GerberViewer/Views/AlignStitchingControl.cs
GerberViewer/Views/AlignStitchingControl.Designer.cs
GerberViewer/Views/AlignStitchingControl.resx
GerberViewer/Views/ManualAlignmentDialog.cs
GerberViewer/Views/GerberSampleWindow.cs

GerberViewer/Stitching/PathCanvasControl.cs
GerberViewer/Workflow/Models/WorkflowContext.cs
GerberViewer/Workflow/HalconRuntimeValidator.cs

GerberStitching.Core/GerberStitching.Core.csproj
GerberStitching.Core/Models/SampleManifest.cs
GerberStitching.Core/Models/WorkflowModels.cs
GerberStitching.Core/Arrangement/CapturedImageLoader.cs
GerberStitching.Core/Arrangement/NaturalSortService.cs
GerberStitching.Core/Alignment/SampleAlignmentModels.cs
GerberStitching.Core/Alignment/ModalityAwarePreprocessor.cs
GerberStitching.Core/Alignment/SampleAligners.cs
GerberStitching.Core/Alignment/NeighborMatchAcceptance.cs
GerberStitching.Core/Alignment/AlignStitchWorkflowService.cs
GerberStitching.Core/Stitching/GlobalTransformStitcher.cs
GerberStitching.Core/Utils/TiffBigWriter.cs
GerberStitching.Core/Utils/ImageRead.cs

GerberViewer/GerberViewer.csproj
GerberViewer.sln
```

## 3.2 Documentation hiện có

```text
docs/Tab3_Align_Stitching_Flow.md
docs/tab3_implementation_baseline.md
docs/tab3_fix_task_results.md
docs/tab2_manifest_contract.md
docs/tab2_image_ownership.md
GerberView_Align_Stitching_Spec_v0.2.md
```

## 3.3 Matcher reference

```text
reference/Matcher/IMatcher.cs
reference/Matcher/MatcherHelper.cs
reference/Matcher/EccMatcher.cs
reference/Matcher/PharseCorrMatcher.cs
reference/Matcher/PharseCorrMatcher2.cs
reference/Matcher/PyramidPhaseMatcher.cs
reference/Matcher/SKILL.md
```

## 3.4 UI reference

```text
reference/StitchingImage/StitchingImage/
Stitch_Tools/DesignControls/OffsetPreviewControl.cs

reference/StitchingImage/StitchingImage/
Stitch_Tools/DesignControls/OffsetPreviewControl.Designer.cs

reference/StitchingImage/StitchingImage/
Stitch_Tools/DesignControls/OffsetPreviewControl.resx

reference/StitchingImage/StitchingImage/
Stitch_Tools/DesignControls/ImagePreviewControl.cs
```

Reference chỉ dùng để tham khảo.

Không sửa hoặc copy nguyên.

---

# 4. Các lỗi hiện tại phải xử lý

## 4.1 `AGENT.md` cũ từng khóa Tab 3

Phải bảo đảm repository guidance mới cho phép triển khai Tab 3.

Không để Codex dựa vào rule cũ và chỉ tạo placeholder.

## 4.2 `WorkflowContext` setter sai

Setter hiện tại có nguy cơ chỉ assign `value` khi context cũ đã khác null.

Phải dùng flow:

```text
if same reference: return
unsubscribe old
assign new
subscribe new
refresh
```

Lần gán đầu từ `MainForm` phải hoạt động.

## 4.3 Duplicate `AlignStitchConfig`

Hiện có nhiều model cùng tên hoặc cùng vai trò.

Phải:

* giữ một canonical model tại Core,
* cho `WorkflowContext` tham chiếu model đó,
* không copy config giữa UI và Core bằng tay,
* không để PropertyGrid bind một model còn workflow dùng model khác.

## 4.4 HALCON NCC hiện là placeholder

Không được giữ implementation:

```text
model handle chỉ lưu Width/Height
managed brute-force NCC
```

rồi gọi tên là HALCON NCC.

## 4.5 Pyramid ECC hiện là placeholder

Không được:

```text
gọi NCC
→ lưu kết quả vào EccCorrelation
```

Phải dùng `Cv2.FindTransformECC`.

## 4.6 Phase matcher reference chưa đúng

`PharseCorrMatcher` reference có thể dùng brute-force array và `GetPixel`.

Production `PharseCorrMatcher` phải dùng `Cv2.PhaseCorrelate`.

## 4.7 Transform direction mơ hồ

Không dùng contract `src/dst` không rõ ràng.

Canonical:

```text
Moving image
→
Reference image
```

## 4.8 `tabComparison` chỉ là PictureBox

Phải thay bằng UserControl production.

## 4.9 Stitching chưa nối từ UI

Sau alignment thành công phải:

```text
final states
→ stitcher
→ stitched output
→ comparison service
→ update result tabs
```

## 4.10 Stitcher hiện chưa production

Các lỗi cần tránh:

* map bằng `Row:Column`,
* bounds chỉ dùng translation,
* bỏ qua transformed corners,
* full-size `Bitmap`,
* không warp valid mask,
* TIFF giả BigTIFF,
* preview scale làm ảnh hưởng output.

## 4.11 Comparison chưa có authoritative coordinate mapping

Không resize source sample tùy ý.

Phải dùng:

```text
ProcessedSampleGlobalPixels
```

cho cả sample reference và stitched result.

---

# 5. Kết quả kiến trúc mục tiêu

```text
Tab 2 sample_manifest.json
        ↓
Manifest validation
        ↓
Captured folder natural sort
        ↓
OrderIndex mapping
        ↓
Prepared match images
        ↓
NCC_HalconMatcher
        ↓
EccMatcher
        ↓
Direct result validation
        ↓
Rejected tiles
        ↓
PharseCorrMatcher + EccMatcher neighbor recovery
        ↓
Final global poses
        ↓
GlobalTransformStitcher
        ↓
TIFF / BigTIFF
        ↓
SampleComparisonService
        ↓
SampleComparisonControl
        ↓
Report + diagnostics + logs
```

---

# 6. File cần tạo

## 6.1 Matcher

Tạo:

```text
GerberStitching.Core/Matching/IMatcher.cs
GerberStitching.Core/Matching/MatchRequest.cs
GerberStitching.Core/Matching/MatchResult.cs
GerberStitching.Core/Matching/MatcherOptions.cs
GerberStitching.Core/Matching/MatcherFactory.cs
GerberStitching.Core/Matching/MatcherGeometryValidator.cs
GerberStitching.Core/Matching/MatcherTransformConverter.cs
GerberStitching.Core/Matching/EccMatcher.cs
GerberStitching.Core/Matching/PharseCorrMatcher.cs
GerberStitching.Core/Matching/NCC_HalconMatcher.cs
```

Có thể tạo khi cần:

```text
GerberStitching.Core/Matching/MatcherBase.cs
GerberStitching.Core/Matching/MatcherPipeline.cs
GerberStitching.Core/Matching/PreparedMatchImage.cs
GerberStitching.Core/Matching/MatcherDiagnostics.cs
```

## 6.2 Image interoperability

Tạo hoặc chuẩn hóa:

```text
GerberStitching.Core/Imaging/ImageInterop/IImageInteropService.cs
GerberStitching.Core/Imaging/ImageInterop/ImageInteropService.cs
GerberStitching.Core/Imaging/ImageInterop/ImagePixelFormatInfo.cs
GerberStitching.Core/Imaging/ImageInterop/PreparedAlignmentImages.cs
```

## 6.3 Comparison core

Tạo:

```text
GerberStitching.Core/Comparison/ComparisonMode.cs
GerberStitching.Core/Comparison/ComparisonMetrics.cs
GerberStitching.Core/Comparison/SampleComparisonRequest.cs
GerberStitching.Core/Comparison/SampleComparisonResult.cs
GerberStitching.Core/Comparison/SampleComparisonService.cs
```

## 6.4 Comparison UI

Tạo:

```text
GerberViewer/Views/SampleComparisonControl.cs
GerberViewer/Views/SampleComparisonControl.Designer.cs
GerberViewer/Views/SampleComparisonControl.resx
GerberViewer/Views/ComparisonImageView.cs
```

## 6.5 Tests

Ưu tiên tạo:

```text
GerberStitching.Tests/GerberStitching.Tests.csproj
GerberStitching.Tests/Matching/MatcherSyntheticImageFactory.cs
GerberStitching.Tests/Matching/TransformAssert.cs
GerberStitching.Tests/Matching/PharseCorrMatcherTests.cs
GerberStitching.Tests/Matching/EccMatcherTests.cs
GerberStitching.Tests/Matching/NCC_HalconMatcherTests.cs
GerberStitching.Tests/Alignment/DirectAlignmentPipelineTests.cs
GerberStitching.Tests/Alignment/NeighborRecoveryTests.cs
GerberStitching.Tests/Stitching/GlobalTransformStitcherTests.cs
GerberStitching.Tests/Comparison/SampleComparisonServiceTests.cs
```

Nếu solution hiện có test project, dùng test project đó thay vì tạo duplicate.

---

# 7. File cần thay đổi

Tối thiểu xem xét:

```text
AGENT.md
GerberViewer.sln

GerberStitching.Core/GerberStitching.Core.csproj
GerberStitching.Core/Models/SampleManifest.cs
GerberStitching.Core/Models/WorkflowModels.cs
GerberStitching.Core/Alignment/SampleAlignmentModels.cs
GerberStitching.Core/Alignment/ModalityAwarePreprocessor.cs
GerberStitching.Core/Alignment/SampleAligners.cs
GerberStitching.Core/Alignment/NeighborMatchAcceptance.cs
GerberStitching.Core/Alignment/AlignStitchWorkflowService.cs
GerberStitching.Core/Stitching/GlobalTransformStitcher.cs
GerberStitching.Core/Utils/TiffBigWriter.cs

GerberViewer/GerberViewer.csproj
GerberViewer/Workflow/Models/WorkflowContext.cs
GerberViewer/Views/AlignStitchingControl.cs
GerberViewer/Views/AlignStitchingControl.Designer.cs
GerberViewer/Views/AlignStitchingControl.resx
GerberViewer/Stitching/PathCanvasControl.cs
```

Không bắt buộc sửa tất cả trong một task.

Chỉ sửa khi phase hiện tại yêu cầu.

---

# 8. Canonical matcher contract

## 8.1 `IMatcher`

Contract đề xuất:

```csharp
public interface IMatcher : IDisposable
{
    string MatcherName { get; }

    MatchResult Match(
        MatchRequest request,
        CancellationToken cancellationToken);
}
```

## 8.2 `MatchRequest`

Phải thể hiện:

```text
ReferenceImage
MovingImage
ReferenceMask
MovingMask
ReferenceRoi
MovingRoi
InitialMovingToReferenceTransform
MatcherOptions
MatchPurpose
Context
```

Không bắt buộc mọi field đều có giá trị.

## 8.3 `MatchResult`

Phải chứa:

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
Warning
ProcessingTime
Diagnostic metadata
```

Không trả transform chỉ dưới dạng Tx/Ty nếu matcher hỗ trợ rotation.

## 8.4 Match purpose

Nên có:

```csharp
public enum MatchPurpose
{
    CapturedToSample,
    TargetCapturedToAnchorCaptured,
    ManualPreview,
    SyntheticTest
}
```

---

# 9. Matcher implementation requirements

## 9.1 `NCC_HalconMatcher`

Phải thực thi HALCON NCC thật.

Lifecycle:

```text
create_ncc_model
find_ncc_model
clear_ncc_model
```

Yêu cầu:

* cache một model cho mỗi sample tile và preprocessing variant,
* stable key,
* actual HALCON model handle,
* actual HALCON score,
* configured angle range,
* convert row/column/angle đúng,
* canonical `CapturedToSampleTransform`,
* clear model đúng một lần,
* cancellation giữa tile,
* context-rich error.

Không dùng:

```csharp
object
fake handle
Width/Height-only handle
managed NCC thay cho HALCON NCC
```

Synthetic test phải tạo sample có feature bất đối xứng để phát hiện đảo transform.

## 9.2 `EccMatcher`

Phải dùng:

```csharp
Cv2.FindTransformECC(...)
```

Pyramid flow:

```text
build pyramid
→ start coarse level
→ scale initial matrix
→ run ECC
→ validate
→ propagate to finer level
→ final full-resolution matrix
```

Default:

```text
MotionTypes.Euclidean
```

Expose có kiểm soát:

```text
Translation
Euclidean
Affine
```

Không claim Homography khi chưa test.

Phải dùng:

```text
PyramidLevels
MaxIterations
Epsilon
MinCorrelation
```

## 9.3 `PharseCorrMatcher`

Tên giữ theo yêu cầu hiện tại.

Phải dùng:

```csharp
Cv2.PhaseCorrelate
Cv2.CreateHanningWindow
```

Phải:

* grayscale,
* `CV_32F`,
* same-size pair,
* actual response,
* translation only,
* deterministic disposal,
* no `GetPixel`,
* no brute-force array loop.

Vai trò chính:

```text
neighbor captured-image matching
```

Có thể dùng làm coarse translation trước ECC.

---

# 10. Geometry validation

Tạo validation tập trung.

Kiểm tra:

```text
matrix dimensions
finite matrix
transform direction
translation range
rotation range
scale range
overlap ratio
content support
expected direction
expected neighbor displacement
correlation threshold
NCC threshold
phase response threshold
```

Không duplicate validation logic trong từng UI event.

Kết quả reject phải có reason code ổn định:

```text
InvalidInput
UnsupportedPixelFormat
NonFiniteTransform
ScoreBelowThreshold
CorrelationBelowThreshold
ResponseBelowThreshold
TranslationOutOfRange
RotationOutOfRange
ScaleOutOfRange
OverlapBelowThreshold
DirectionInconsistent
NoContentSupport
Cancelled
RuntimeFailure
```

---

# 11. Direct camera-to-sample pipeline

Cho mỗi `OrderIndex K`:

```text
Sample tile K
Captured image K
        ↓
prepare preprocessing candidates
        ↓
NCC_HalconMatcher
        ↓
validate NCC result
        ↓
EccMatcher using NCC initial transform
        ↓
validate ECC result
        ↓
choose final direct result
        ↓
compose global pose
```

Policy:

```text
NCC pass + ECC pass
    use ECC

NCC pass + ECC fail
    use NCC only if explicitly enabled

NCC fail
    optional ECC from expected initialization

all direct methods fail
    mark direct rejected
    enter recovery
```

Mỗi tile report phải ghi:

```text
preprocessing candidates
selected candidate
NCC score
ECC correlation
matrix
translation
rotation
scale
overlap
selected method
rejection reason
processing time
```

---

# 12. Neighbor recovery

Priority:

```text
1. Traversal predecessor
2. Solved horizontal/vertical neighbor
3. Traversal successor in second pass
```

Flow:

```text
select anchor
→ calculate expected overlap side
→ crop/prepare overlap ROIs
→ PharseCorrMatcher
→ optional EccMatcher
→ validate pair transform
→ compose target global pose
→ update state
→ record recovery edge
```

Không dùng expected grid như image-based success.

Expected grid phải có state riêng:

```text
PoseSource.ExpectedGridOffset
AlignmentSucceeded = false
IsFallbackPose = true
IsStitchable = policy dependent
```

Recovery report phải ghi:

```text
anchor OrderIndex
target OrderIndex
matcher
pair score
pair transform
accept/reject reason
final pose source
```

---

# 13. Workflow state model

`TileWorkflowState` cần tách rõ:

```csharp
public bool AlignmentSucceeded { get; set; }
public bool IsFallbackPose { get; set; }
public bool IsStitchable { get; set; }
```

Không chỉ dùng:

```csharp
HasValidPose
```

để quyết định tất cả.

Run status nên dùng enum:

```csharp
public enum AlignStitchRunStatus
{
    Completed,
    CompletedWithFallback,
    CompletedWithExcludedTiles,
    Cancelled,
    Failed
}
```

Sau tất cả recovery pass:

```text
rebuild final state list
→ rebuild report poses
→ pass same state list to PathCanvas
→ pass same state list to stitcher
```

Không append pose trước khi state cuối được xác định.

---

# 14. Stitching requirements

Input:

```text
CapturedImageInfo matched by OrderIndex
TileWorkflowState with IsStitchable = true
```

Không map bằng `Row:Column`.

## 14.1 Bounds

Transform đủ bốn corners:

```text
(0,0)
(width,0)
(width,height)
(0,height)
```

## 14.2 Warp

Warp:

```text
image
valid mask
```

Dùng:

```text
WarpAffine
```

cho translation/euclidean/affine.

Không dùng `DrawImageUnscaled` cho transform có rotation.

## 14.3 Blend

Tối thiểu:

```text
NoBlend
WeightedAverage
Feather
```

Black pixels ngoài valid mask không được làm bẩn overlap.

## 14.4 Preview

Preview có megapixel limit.

Full output không được kế thừa preview scale.

## 14.5 Output

Không tạo nhiều full-resolution Bitmap.

Nếu output lớn:

* dùng tiled hoặc strip-oriented writing,
* chọn Standard TIFF hoặc BigTIFF dựa trên estimated byte count,
* ghi vào `.creating`,
* reopen và validate trước publish.

---

# 15. Manifest và comparison metadata

Mở rộng manifest theo version nếu cần.

Field đề xuất:

```text
ProcessedSamplePath
SourceToProcessedTransform
PreprocessMode
ProcessedSampleChannelCount
ProcessedSampleBitDepth
```

Backward compatibility:

```text
Manifest v1:
    đọc được
    chỉ comparison authoritative khi geometry chứng minh được

Manifest v2:
    dùng ProcessedSamplePath và transform metadata
```

Không silently đổi JSON shape mà không tăng version hoặc update reader.

---

# 16. Sample comparison service

Input:

```text
processed sample reference
stitched result
coordinate transform metadata
preview megapixel limit
comparison options
```

Output:

```text
sample preview
stitched preview
overlay preview
absolute difference
edge overlay
metrics
warnings
metadata
```

Mode:

```text
SampleOnly
StitchedOnly
AlphaOverlay
AbsoluteDifference
EdgeOverlay
Blink
```

Metrics:

```text
ValidOverlapRatio
NormalizedCrossCorrelation
BinaryMaskIoU
EdgeOverlap
DistanceTransformError
```

Không gọi một metric duy nhất là “accuracy”.

Phải ghi rõ:

```text
authoritative comparison
hoặc
non-authoritative visual preview
```

---

# 17. `SampleComparisonControl`

Tạo:

```text
SampleComparisonControl.cs
SampleComparisonControl.Designer.cs
SampleComparisonControl.resx
```

Tham khảo layout `OffsetPreviewControl`.

Không copy nguyên domain của reference.

## 17.1 Layout

Main layout:

```text
TableLayoutPanel
    Column 0 = 35%
    Column 1 = 65%
```

Left panel:

```text
Mode ComboBox
Alpha TrackBar/NumericUpDown
Blink interval
Run/Refresh button
Stop Blink button
Metrics group
Coordinate status
Warnings
Save comparison button
Status label
```

Right panel:

```text
SplitContainer Orientation.Horizontal

Panel 1:
    GroupBox "Sample Reference"
    ComparisonImageView

Panel 2:
    GroupBox "Stitched / Comparison"
    ComparisonImageView
```

## 17.2 Preview behavior

`ComparisonImageView` phải:

* zoom bằng mouse wheel,
* pan bằng mouse,
* fit,
* reset,
* double buffering,
* dispose old Bitmap,
* không chỉnh source image trực tiếp.

## 17.3 Integration

Trong `AlignStitchingControl.Designer.cs`:

```text
remove picComparison
add sampleComparisonControl
Dock = Fill
```

Cập nhật `.csproj` đúng Visual Studio format.

---

# 18. `AlignStitchingControl` integration

Run flow cuối:

```text
validate UI inputs
→ create run directory
→ run alignment
→ update final states
→ run stitching
→ load bounded stitched preview
→ run comparison
→ bind SampleComparisonControl
→ save report
→ validate output
→ publish run directory
→ update WorkflowContext
```

UI phải:

* disable input controls trong run,
* support cancellation,
* update progress bằng `IProgress<T>`,
* không mutate worker object trực tiếp khi paint,
* clear stale preview khi input thay đổi,
* dispose old preview,
* update `LastStitchedOutputPath` chỉ khi publish thành công.

---

# 19. Phase triển khai

## Phase 1 — Baseline, scope và canonical models

Mục tiêu:

* xác nhận branch,
* ghi baseline,
* cập nhật scope,
* sửa `WorkflowContext`,
* hợp nhất config,
* chuẩn hóa state và transform contract.

Checklist:

* [ ] `AGENT.md` được dùng.
* [ ] Tab 3 nằm trong scope.
* [ ] `WorkflowContext` setter đúng.
* [ ] Chỉ còn một canonical `AlignStitchConfig`.
* [ ] Có `AlignmentSucceeded`, `IsFallbackPose`, `IsStitchable`.
* [ ] Transform direction được ghi trong code và tests.
* [ ] Debug x64 build.
* [ ] Release x64 build.

## Phase 2 — Image interoperability và matcher contract

Mục tiêu:

* tạo image interop,
* tạo `IMatcher`,
* tạo request/result/options,
* tạo test harness.

Checklist:

* [ ] Không còn contract `src/dst` mơ hồ.
* [ ] Ownership được ghi rõ.
* [ ] Mono8 test.
* [ ] Mono16 test.
* [ ] Color test.
* [ ] RGB/BGR test.
* [ ] Synthetic image factory.
* [ ] Transform assert helper.
* [ ] Build hai configuration.

## Phase 3 — Implement ba matcher

Mục tiêu:

* implement phase correlation thật,
* implement ECC thật,
* implement HALCON NCC thật.

Checklist:

* [ ] `PharseCorrMatcher` dùng `Cv2.PhaseCorrelate`.
* [ ] Có Hanning window.
* [ ] `EccMatcher` dùng `Cv2.FindTransformECC`.
* [ ] ECC pyramid hoạt động.
* [ ] `NCC_HalconMatcher` dùng HALCON model thật.
* [ ] Model cache và disposal đúng.
* [ ] Không có proxy score.
* [ ] Synthetic translation test.
* [ ] Synthetic rotation test.
* [ ] Inverted-polarity test.
* [ ] Cancellation test.
* [ ] Real HALCON runtime smoke test.

## Phase 4 — Direct alignment và recovery

Mục tiêu:

* thay placeholder aligner,
* dùng matcher pipeline,
* implement neighbor recovery.

Checklist:

* [ ] NCC → ECC policy.
* [ ] Geometry validation tập trung.
* [ ] Actual scores trong report.
* [ ] Direct fail chuyển recovery.
* [ ] Neighbor ROI đúng hướng.
* [ ] TargetToAnchor transform đúng.
* [ ] Global pose composition đúng.
* [ ] PathCanvas hiển thị recovery edge.
* [ ] Không có fake success.
* [ ] Test 2×2 và 4×4.

## Phase 5 — Production stitching

Mục tiêu:

* stitch bằng full transform,
* warp mask,
* blend,
* bounded preview,
* TIFF/BigTIFF.

Checklist:

* [ ] Map bằng `OrderIndex`.
* [ ] Bounds từ transformed corners.
* [ ] Rotation test.
* [ ] Scale test.
* [ ] Valid mask.
* [ ] NoBlend.
* [ ] WeightedAverage.
* [ ] Feather.
* [ ] Preview scale độc lập.
* [ ] Output reopen validation.
* [ ] Cancellation không publish output.

## Phase 6 — Comparison service và UI

Mục tiêu:

* thêm manifest metadata,
* tạo comparison service,
* tạo comparison control,
* tích hợp `tabComparison`.

Checklist:

* [ ] Manifest backward compatibility.
* [ ] Authoritative coordinate check.
* [ ] `SampleComparisonControl.cs`.
* [ ] `.Designer.cs`.
* [ ] `.resx`.
* [ ] `ComparisonImageView`.
* [ ] Sample only.
* [ ] Stitched only.
* [ ] Alpha overlay.
* [ ] Absolute difference.
* [ ] Edge overlay.
* [ ] Blink.
* [ ] Metrics.
* [ ] Dispose preview.
* [ ] `picComparison` bị loại.

## Phase 7 — End-to-end verification

Mục tiêu:

* chạy workflow hoàn chỉnh bằng dữ liệu thực,
* kiểm tra regression,
* hoàn thiện báo cáo.

Checklist:

* [ ] Manifest v1.
* [ ] Manifest v2.
* [ ] Natural sort 1,2,10.
* [ ] Count mismatch.
* [ ] Duplicate OrderIndex.
* [ ] Real camera/sample alignment.
* [ ] Neighbor recovery.
* [ ] Stitched TIFF mở được.
* [ ] Comparison modes hoạt động.
* [ ] Cancel hoạt động.
* [ ] Form close an toàn.
* [ ] Debug x64 build.
* [ ] Release x64 build.
* [ ] Test results được ghi trung thực.
* [ ] Không sửa `reference/`.
* [ ] Không sửa `EWindowControl`.
* [ ] Tab 1 và Tab 2 smoke test pass.

---

# 20. Test data và acceptance

## 20.1 Synthetic images

Synthetic image phải có:

* feature bất đối xứng,
* nhiều mức intensity,
* edge rõ,
* vùng trống,
* đủ texture,
* có thể invert polarity.

Không dùng checkerboard đối xứng hoàn toàn vì khó phát hiện transform direction.

## 20.2 Tolerance đề xuất

Translation:

```text
Phase correlation:
≤ 1.0 pixel trên dữ liệu sạch
≤ 2.0 pixels với noise
```

ECC:

```text
translation ≤ 1.5 pixels
rotation ≤ 0.3 degree
```

HALCON NCC:

```text
translation/angle theo tolerance phù hợp model và pyramid
score phải vượt configured threshold
```

Tolerance phải được ghi trong test, không hard-code rải rác.

## 20.3 Real-data smoke test

Tối thiểu:

```text
1 manifest thực
1 captured folder thực
ít nhất 4 tile
1 tile có direct alignment
1 case neighbor recovery nếu có thể
1 stitched TIFF
1 comparison output
```

Nếu chưa có real dataset:

* không invent result,
* ghi rõ thiếu dataset,
* hoàn thành synthetic tests trước,
* tạo checklist để người dùng chạy test thực.

---

# 21. Output structure

```text
<OutputRoot>/
└─ AlignStitch_<runId>/
   ├─ stitched_output.tif
   ├─ processing_report.json
   ├─ processing_log.txt
   ├─ config_snapshot.json
   ├─ manifest_snapshot.json
   ├─ comparison/
   │  ├─ sample_reference_preview.png
   │  ├─ stitched_preview.png
   │  ├─ overlay_comparison.png
   │  ├─ difference_comparison.png
   │  ├─ edge_comparison.png
   │  └─ comparison_metadata.json
   └─ diagnostics/
      ├─ tile_000/
      │  ├─ sample.png
      │  ├─ captured.png
      │  ├─ sample_preprocessed.png
      │  ├─ captured_preprocessed.png
      │  ├─ warped.png
      │  ├─ difference.png
      │  └─ metadata.json
      └─ ...
```

Diagnostics phải có config để tránh sinh quá nhiều file.

---

# 22. Báo cáo sau mỗi task

Sau mỗi task, trả về:

```text
1. Task summary
2. Files created
3. Files changed
4. Classes/methods changed
5. Transform direction affected
6. Resource ownership changes
7. Build Debug x64 result
8. Build Release x64 result
9. Tests executed
10. Tests passed/failed
11. Runtime tests not executed and reason
12. Remaining risks
13. Next task
```

Không chỉ ghi:

```text
Implemented successfully
```

mà không có build/test evidence.

---

# 23. Điều bị cấm

Không được:

```text
fake HALCON model
fake ECC score
fake phase score
hard-coded IsMatch = true
expected grid được gọi là alignment success
catch rỗng
Application.DoEvents
GetPixel/SetPixel trong production matcher
control access từ worker
copy nguyên reference project
sửa reference source
sửa EWindowControl
xóa output root
publish output sau cancellation
claim BigTIFF khi vẫn dùng Bitmap.Save
claim comparison authoritative khi coordinate mapping không xác định
```

---

# 24. Definition of Done

Implementation chỉ hoàn thành khi:

* Matcher architecture được tạo.
* Ba matcher chạy thuật toán thật.
* Transform direction được test.
* Direct alignment chạy thật.
* Neighbor recovery chạy thật.
* Stitching dùng full transform.
* Output được validate.
* Comparison service chạy.
* Comparison control có Designer.
* `tabComparison` được thay.
* UI async và cancellation đúng.
* Report chứa score và transform thật.
* Debug x64 và Release x64 build.
* Synthetic tests pass.
* Real-data smoke test được chạy hoặc ghi rõ lý do chưa thể chạy.
* Không sửa protected source.
