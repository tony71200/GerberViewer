# Codex Prompt — Fix and Complete Tab 3 Implementation

## 1. Role and authorization

You are modifying a C# 7.3 WinForms solution targeting .NET Framework 4.8 with:

- HALCON 25.05
- OpenCvSharp4
- `EWindowControl`
- `ELog_1_0`
- `GerberStitching.Core`

Repository:

```text
tony71200/GerberViewer
```

Target branch:

```text
2026-07-21_Implement-Tab3
```

Primary target:

```text
Tab 3 — Align and Stitching
```

This prompt explicitly authorizes implementation and refactoring of Tab 3. It overrides only the old `AGENT.md` statement that Tab 3 is out of scope. Every other repository rule remains mandatory, especially:

- Do not directly modify `EWindowControl`.
- Do not delete external, reference, legacy, sample, ZIP, or third-party source files.
- Do not copy an entire external project into the production code without adapting it.
- Keep C# syntax compatible with C# 7.3 and .NET Framework 4.8.
- Keep WinForms layout code in `.Designer.cs` and resources in `.resx`.
- Do not claim runtime success based only on static inspection.

---

# 2. Mandatory sources to read before editing

Read the following production files completely:

```text
AGENT.md

GerberViewer/Views/AlignStitchingControl.cs
GerberViewer/Views/AlignStitchingControl.Designer.cs
GerberViewer/Views/ManualAlignmentDialog.cs
GerberViewer/Stitching/PathCanvasControl.cs
GerberViewer/Workflow/Models/WorkflowContext.cs
GerberViewer/Workflow/HalconRuntimeValidator.cs

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
GerberStitching.Core/RobotManager/Domain.cs
GerberStitching.Core/RobotManager/RobotArrange.cs
GerberStitching.Core/RobotManager/TraversalGraph.cs

Elog_1_0/Elog.cs
Elog_1_0/EasyFile.cs

docs/Tab3_Align_Stitching_Flow.md
GerberView_Align_Stitching_Spec_v0.2.md
docs/tab2_manifest_contract.md
docs/tab2_image_ownership.md
```

Read these reference files as design and implementation references:

```text
reference/StitchingImage/StitchingImage/Stitch_Tools/DesignControls/PathCanvasControl.cs
reference/StitchingImage/StitchingImage/Stitch_Tools/DesignControls/PathCanvasControl.Designer.cs
reference/StitchingImage/StitchingImage/Stitch_Tools/DesignControls/PathCanvasControl.resx

reference/StitchingImage/StitchingImage/Stitch_Tools/RobotManager/TraversalGraph.cs
reference/StitchingImage/StitchingImage/Stitch_Tools/Utils/TiffBigWriter.cs
reference/StitchingImage/StitchingImage/Stitch_Tools/Matcher/
```

Also read the supplied review:

```text
Analyzed_From_Claude.md
```

Reference source is read-only. Do not delete, rename, move, or edit files under:

```text
reference/
```

---

# 3. Primary objective

The purpose of Tab 3 is not merely to place camera images according to the expected grid.

The main objective is:

```text
Adjust each camera image so its content aligns with the corresponding Gerber sample tile.
```

Then use the verified global poses to stitch the camera images into one result.

The final stitched camera result must be compared with the canonical sample image through an overlay/difference workflow.

Required processing flow:

```text
Select sample_manifest.json
→ validate manifest
→ resolve sample root, tile paths, and sample reference
→ select captured-image folder
→ natural sort captured images
→ map Captured OrderIndex K ↔ Sample OrderIndex K
→ validate all inputs
→ direct camera-to-sample alignment
→ real recovery for rejected tiles
→ produce final global poses
→ stitch camera images
→ compare stitched image with the sample reference
→ write report, diagnostics, and logs
```

Do not treat expected-grid placement as proof of successful alignment.

---

# 4. Critical truthfulness rules

The current source contains placeholder behavior. Remove or block every false-success path.

The following are forbidden in the completed implementation:

```csharp
new object(); // used as a fake HALCON NCC model
```

```csharp
IsMatch = true;
Score = 1;
```

without image-based matching.

```csharp
report.Succeeded = solved.Values.Any(v => v.HasValidPose);
```

when an expected-grid pose is counted as alignment success.

```csharp
Application.DoEvents();
```

```csharp
var correlation = Ncc(...); // stored as EccCorrelation
```

```csharp
TryAnchorOrInterpolation(...) => ExpectedGridPose(...)
```

while labeling the result as real anchor adjustment or interpolation.

Do not connect the production UI to a fake backend and then present the result as aligned.

If a phase is not implemented yet:

- mark it as unavailable
- block production output
- log a clear error
- do not silently substitute expected-grid behavior
- do not assign a successful alignment state

---

# 5. Canonical coordinate and transform contract

## 5.1 Coordinate spaces

Use explicit names for every coordinate space:

```text
SampleTileLocalPixels
ProcessedSampleGlobalPixels
CapturedImageLocalPixels
StitchedCanvasPixels
OriginalSamplePixels
```

`SampleTileInfo.ExpectedX` and `ExpectedY` are:

```text
ProcessedSampleGlobalPixels
```

Do not label these values as robot coordinates.

Rename UI labels and diagnostic text accordingly:

```text
Expected X/Y — processed-sample pixels
Global X/Y — processed-sample pixels
Virtual Row/Column
```

## 5.2 Canonical transform direction

The internal alignment transform must be:

```text
CapturedImageLocalPixels
→ SampleTileLocalPixels
```

Name it:

```csharp
CapturedToSampleTransform
```

The global pose is:

```text
CapturedImageLocalPixels
→ ProcessedSampleGlobalPixels
```

Calculated as:

```text
SampleTileGlobalTranslation
× CapturedToSampleTransform
```

Do not mix this with:

```text
SampleToCaptured
```

For every HALCON/OpenCV result:

1. Document the transform direction returned by the library call.
2. Convert or invert it once at the adapter boundary.
3. Store only the canonical direction internally.
4. Add synthetic unit tests that prove the direction.

Do not rely on comments alone.

## 5.3 Matrix type

Use one canonical matrix representation inside the domain layer.

Preferred:

```csharp
double[3, 3]
```

or a dedicated immutable `Transform2D` wrapper.

OpenCV adapters may use:

```csharp
Mat CV_64F
```

HALCON adapters may use:

```csharp
HTuple HomMat2D
```

Conversions must be centralized and tested.

---

# 6. UI requirements

Refactor Tab 3 UI into a production workflow.

## 6.1 Manifest selection

Add:

```text
Button: Select Manifest
Read-only TextBox: sample_manifest.json path
```

Suggested names:

```csharp
btnSelectManifest
txtManifestPath
```

Use an `OpenFileDialog` filtered to:

```text
sample_manifest.json|sample_manifest.json
JSON files|*.json
All files|*.*
```

After selection:

1. Read through the shared manifest serializer.
2. Run `SampleManifestValidator`.
3. Reject unsupported versions and invalid tiles.
4. Resolve relative tile paths against the manifest directory.
5. Do not depend on the current working directory.
6. Update `WorkflowContext.ManifestPath`.
7. Do not mutate the manifest file.

## 6.2 Derived manifest information

Display read-only fields or labels for:

```text
Manifest file
Manifest folder
Sample run root
Sample tiles folder
Source sample raster
Processed sample/reference image
Rows × Columns
Expected tile count
Processed width × height
Crop order
Start order
Coordinate space
```

The user requested folder information derived from the selected manifest. Resolve it as follows:

```text
ManifestFolder = directory containing sample_manifest.json
SampleRoot = validated RootDirectory when valid, otherwise ManifestFolder
TilesFolder = common parent of resolved ExpectedPath values
SourceSample = resolved SourceRasterPath
```

Do not trust an absolute `RootDirectory` blindly if the run folder was copied.

Resolution policy:

1. If an `ExpectedPath` is relative, combine it with `ManifestFolder`.
2. If it is absolute and exists, use it.
3. If it is absolute but missing, try:
   - `ManifestFolder + relative suffix`
   - `ManifestFolder/tiles/<filename>`
4. Log any relocation.
5. Block execution if multiple ambiguous candidates exist.

## 6.3 Captured folder and output folder

Keep or add:

```text
Button: Select Captured Folder
Read-only TextBox: Captured Folder

Button: Select Output Folder
Read-only TextBox: Output Folder
```

The selected output root must never be recursively deleted.

Use an application-owned run directory:

```text
<OutputRoot>/AlignStitch_<runId>/.creating/
```

Publish only after output validation.

## 6.4 Configuration editor

Add a `PropertyGrid` or dedicated configuration control:

```csharp
alignConfigGrid
```

It must edit one canonical `AlignStitchConfig`.

Expose at minimum:

### Direct alignment

```text
Alignment method
NCC minimum score
ECC minimum correlation
Maximum translation pixels
Maximum absolute rotation
Minimum scale
Maximum scale
Minimum overlap ratio
HALCON angle start
HALCON angle extent
HALCON angle step
HALCON pyramid levels
OpenCV ECC motion type
ECC pyramid levels
ECC iterations
ECC epsilon
Allow NCC-only acceptance
Allow ECC from expected initialization
```

### Preprocessing

```text
Contrast normalization
Polarity mode
Threshold mode
Fixed threshold
Adaptive block size/radius
Adaptive constant
Edge mode
Canny low threshold
Canny high threshold
Gerber content mask
Diagnostic image generation
Normalized working size
```

### Recovery

```text
Enable neighbor recovery
Neighbor minimum score
Maximum neighbor translation deviation
Maximum neighbor rotation deviation
Enable anchor interpolation
Maximum interpolation deviation
Allow expected-grid fallback
Require manual confirmation for expected-grid
```

### Stitching

```text
Preview update interval
Preview megapixel limit
Blend mode
Feather width
TIFF mode
BigTIFF tile width
BigTIFF tile height
Compression
Output pixel format
```

## 6.5 Tab layout

Add or refine result tabs:

```text
Order and Status
Alignment Diagnostics
Stitched Result
Sample Comparison
Logs
```

### Order and Status

Contains the improved `PathCanvasControl`.

### Alignment Diagnostics

Show selected tile data:

```text
OrderIndex
Row/Column
Sample path
Captured path
Preprocessing variant
NCC score
ECC correlation
Transform
Translation
Rotation
Scale
Overlap
Pose source
Rejection/fallback reason
Processing time
```

Provide optional sample/captured/preprocessed/difference previews.

### Stitched Result

Show the camera mosaic.

Do not use a simple `PictureBox` for huge full-resolution data without limits. Display a bounded preview; keep full-resolution output on disk.

### Sample Comparison

Display:

```text
Sample reference
Stitched camera result
Alpha overlay
Difference/edge comparison
```

Controls:

```text
Alpha slider
Overlay checkbox
Difference checkbox
Fit
Zoom in/out
Save comparison
```

### Logs

Use a `ListBox` connected to `ELog_1_0`.

Suggested name:

```csharp
lstTab3Log
```

---

# 7. `ELog_1_0` integration

Use the existing logger API. Do not create a second ad hoc logging system for Tab 3.

Recommended member:

```csharp
private readonly Elog_1_0.Elog _logger = new Elog_1_0.Elog();
```

Initialize after controls exist:

```csharp
_logger.Debug = true;
_logger.SetOpenListBox(true, lstTab3Log);
_logger.SetOpenFile(
    true,
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Tab3"),
    "AlignStitch");
```

Use:

```csharp
_logger.WriteInfo(...);
_logger.WriteWarning(...);
_logger.WriteError(...);
```

Log at minimum:

- manifest selection
- manifest validation
- path relocation
- captured-folder selection
- image count and dimensions
- run start and end
- config snapshot
- each tile stage
- NCC result
- ECC result
- every rejection
- recovery method and anchor
- manual action
- stitching bounds
- TIFF/BigTIFF selection
- output paths
- cancellation
- complete exceptions with context

Every tile log must include:

```text
OrderIndex
Row
Column
Sample file
Captured file
Stage
```

Dispose logger in the control's `Dispose(bool)`.

Do not call `SetDeleteFile` on:

- source folders
- manifest folder
- captured folder
- output root

It may only be used on the application-owned log directory, and only with an explicit retention policy.

Logging must not replace structured reports. Keep both:

```text
human-readable log
structured processing_report.json
```

Do not swallow logger errors silently in the Tab 3 workflow.

---

# 8. Improve `PathCanvasControl`

Use the extracted reference as a design reference, not as a file to copy unchanged.

Required production files:

```text
GerberViewer/Stitching/PathCanvasControl.cs
GerberViewer/Stitching/PathCanvasControl.Designer.cs
GerberViewer/Stitching/PathCanvasControl.resx
```

Update `GerberViewer.csproj` with correct:

```xml
DependentUpon
EmbeddedResource
SubType
```

## 8.1 Preserve and extend current functionality

Keep:

- per-node state colors
- `CapturedImageInfo` support
- `OrderIndex`
- row/column display

Add from reference:

- standard Designer structure
- dedicated buffered canvas
- graph visibility checkboxes
- status strip and legend
- auto-fit from logical bounds
- physical-coordinate mode
- virtual row/column mode
- cached screen points
- arrow rendering
- component/path visualization
- adaptive label suppression
- redraw on resize

## 8.2 Required graph layers

Render independently toggleable layers:

```text
Expected order
Traversal graph
Neighbor recovery
Interpolation anchors
```

Recommended styles:

```text
Expected order      gray solid arrow
Traversal graph     red solid arrow
Neighbor recovery   blue dashed arrow
Interpolation       purple dashed arrow
```

Node fill colors must represent final state:

```text
Pending
Processing
SampleAlignOk
NeighborAlignOk
AnchorAdjusted
Interpolated
ExpectedGridOffset
Manual
Failed
Excluded
```

Selected node must have a strong border and synchronize with:

```text
lstCapturedImages
Alignment Diagnostics
```

## 8.3 Do not bind directly to mutable worker objects

Create immutable UI snapshots:

```csharp
PathCanvasNode
PathCanvasEdge
PathCanvasSnapshot
```

Worker threads must never mutate data being painted by WinForms.

Update snapshots through `IProgress<T>` on the UI thread.

## 8.4 Stable identity

Enforce:

```text
PathCanvas NodeId
=
CapturedImageInfo.OrderIndex
=
SampleTileInfo.OrderIndex
```

Do not assign order with:

```csharp
Select((x, i) => ...)
```

after the canonical `OrderIndex` already exists.

## 8.5 Coordinate labels

Do not label `ExpectedX/ExpectedY` as `XRobot/YRobot`.

Display the active layout mode:

```text
Expected Sample Coordinates
Final Global Coordinates
Virtual Row/Column
```

## 8.6 Rendering quality

Use an actually double-buffered canvas, such as:

```csharp
internal sealed class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }
}
```

Dispose custom fonts, pens, brushes, and cached bitmaps.

Do not use reflection to change control internals.

## 8.7 Tests

Test:

```text
1×1
2×2
4×4
8×8
large node count
Zigzag
Branch
BranchDown
TopLeftRight
TopLeftDown
BottomRightLeft
BottomRightUp
resize control
missing coordinates
mixed states
recovery edges
```

Verify all edges connect the correct stable IDs.

---

# 9. Manifest and input mapping

## 9.1 Shared validator

Tab 3 must use the canonical:

```csharp
SampleManifestSerializer
SampleManifestValidator
```

Do not deserialize through a separate private JSON implementation.

Validate:

- manifest version
- tile count
- duplicate `OrderIndex`
- missing `OrderIndex`
- duplicate `(Row, Column)`
- invalid dimensions
- out-of-bounds tile rectangles
- missing paths
- inconsistent root
- unreadable sample tile files

## 9.2 Map by `OrderIndex`, not JSON list position

Required:

```csharp
var tileByOrder = manifest.Tiles.ToDictionary(t => t.OrderIndex);
```

For natural-sorted captured image K:

```csharp
var tile = tileByOrder[K];
```

Store:

```csharp
OrderIndex = tile.OrderIndex;
```

Do not use:

```csharp
manifest.Tiles[i]
```

without first normalizing and validating the order.

## 9.3 Captured image validation

Validate each captured image:

```text
readability
width
height
channel count
bit depth
pixel type
TIFF page count where applicable
```

Support at minimum:

```text
Mono8
Mono16
RGB8/BGR8
```

Define conversion behavior explicitly.

Do not use the first image silently as the expected format without recording that policy.

## 9.4 Clear stale state

On any failed manifest or folder load:

```csharp
_capturedImages = new List<CapturedImageInfo>();
```

Clear:

- ListBox
- PathCanvas
- diagnostics
- stitched preview
- comparison preview
- previous workflow result

Disable Run.

---

# 10. Canonical image and interop layer

Do not perform production processing through repeated `Bitmap.GetPixel()` or `SetPixel()`.

Create or reuse one central image interop service:

```text
HALCON HObject
OpenCvSharp Mat
System.Drawing.Bitmap
```

Recommended policy:

```text
Input decode and sample lifecycle: HObject
OpenCV alignment and warping: Mat
WinForms bounded preview only: Bitmap
```

Conversion methods must clearly state ownership:

```csharp
HObject ToHObjectCopy(Mat source)
Mat ToMatCopy(HObject source)
Bitmap ToBitmapCopy(Mat source)
Mat ToMatCopy(Bitmap source)
```

Requirements:

- support grayscale 8-bit
- support grayscale 16-bit
- support three-channel color
- document RGB/BGR conversion
- do not return memory backed by disposed temporary buffers
- deterministic disposal
- no full-size Bitmap conversion for large source or stitched output
- unit tests for size, channel, bit depth, and representative pixels

---

# 11. Preprocessing implementation

Replace the current `float[,]` and `GetPixel` pipeline with HALCON/OpenCV operations.

## 11.1 Sample preprocessing

The sample tile is normally binary or synthetic.

Pipeline candidates:

```text
grayscale
contrast normalization when needed
polarity candidate
threshold
edge extraction
content mask
optional resize for working scale
```

## 11.2 Camera preprocessing

Camera images may contain illumination, noise, texture, and polarity differences.

Pipeline candidates:

```text
grayscale
illumination normalization
contrast normalization
optional denoise
normal and inverted polarity candidates
fixed/Otsu/adaptive threshold
Sobel/Canny
valid-image mask
working-scale normalization
```

## 11.3 Implement every exposed enum truthfully

If the UI exposes:

```text
Adaptive
Canny
HALCON equivalent
Histogram stretch
Content mask
```

then implement it.

Do not map every threshold mode to Otsu.

Do not map every edge mode to Sobel.

Do not leave content-mask methods empty.

If a mode is not implemented, remove or disable it and log why.

## 11.4 Candidate selection

For `PolarityMode.Auto`, evaluate explicit candidates:

```text
AsIs
InvertSample
InvertCaptured
InvertBoth
```

Select based on real alignment score plus geometric validation.

Record the selected candidate in the tile report.

---

# 12. Real direct camera-to-sample alignment

Implement and test both methods thoroughly.

## 12.1 HALCON NCC coarse alignment

Replace the fake model cache with real HALCON handles.

Required HALCON lifecycle:

```text
create_ncc_model
find_ncc_model
clear_ncc_model
```

or the correct equivalent for HALCON 25.05.

Requirements:

- create/cache one model per sample tile and preprocessing variant
- use stable cache keys
- dispose every model ID exactly once
- support configured angle range
- return actual HALCON score
- convert HALCON row/column/angle to the canonical transform
- use masks/ROI where valid
- support cancellation between tiles
- do not store model handles as `object`

Do not assume the HALCON transform direction. Prove it with a synthetic test.

## 12.2 OpenCvSharp Pyramid ECC refinement

Implement real:

```csharp
Cv2.FindTransformECC(...)
```

Use a pyramid from coarse to fine.

At each level:

1. scale images and masks
2. scale initial transform correctly
3. run ECC
4. propagate transform to the next level
5. validate finite matrix and correlation

Supported motion models should be explicit:

```text
Translation
Euclidean
Affine
```

Do not claim homography support unless it is fully implemented and tested.

Use:

```text
PyramidLevels
EccIterations
EccEpsilon
```

from config.

Do not store NCC as `EccCorrelation`.

## 12.3 Combined policy

Default:

```text
HALCON NCC coarse
→ OpenCV Pyramid ECC refinement
```

Policy:

```text
NCC pass + ECC pass
    use ECC result

NCC pass + ECC fail
    use NCC only when explicitly enabled and NCC geometry remains valid

NCC fail
    optionally try ECC from expected-grid initialization

all direct candidates fail
    mark direct alignment rejected and enter recovery
```

## 12.4 Geometric validation

Validate:

```text
finite matrix
translation
rotation
scale
overlap
correlation
content-mask support
transform direction
```

Calculate overlap from transformed corners or masks, not only axis-aligned translation.

## 12.5 Synthetic tests

Generate deterministic synthetic images and known transforms:

```text
translation
rotation
scale
noise
illumination gradient
normal polarity
inverted polarity
partial overlap
```

Assert recovered transform tolerance.

These tests are mandatory before using real camera data.

---

# 13. Real recovery pipeline

Required order:

```text
Neighbor image alignment
→ Anchor adjustment/interpolation
→ Expected-grid degraded fallback
→ Manual review
```

## 13.1 Neighbor alignment

Replace the current hard-coded `PairMatching`.

Actual flow:

```text
anchor captured image
target captured image
→ overlap ROI selection based on expected direction
→ image-to-image alignment
→ pair transform
→ pair score and evaluation
→ target global pose
```

Compute:

```text
TargetGlobalPose
=
AnchorGlobalPose
× TargetToAnchorTransform
```

`PairMatchEval.IsMatch` must depend on:

```text
score
overlap
direction consistency
translation deviation
rotation deviation
finite matrix
```

Never hard-code:

```csharp
IsMatch = true;
```

Record:

```text
anchor OrderIndex
target OrderIndex
pair score
pair transform
accept/reject reason
```

## 13.2 Neighbor candidate priority

Use:

1. traversal predecessor
2. physical horizontal/vertical solved neighbors
3. traversal successor in second pass

PathCanvas must show the accepted recovery edge.

## 13.3 Anchor interpolation

Implement actual interpolation.

For a missing target:

1. find valid anchors before and after it in the appropriate row/column/traversal segment
2. calculate interpolation parameter
3. interpolate translation
4. interpolate angle using wrap-safe angle interpolation
5. interpolate scale only if enabled
6. compare against expected-grid pose
7. reject excessive deviation

Do not label expected-grid as `AnchorAdjusted` or `Interpolated`.

## 13.4 Expected-grid fallback

Expected-grid is a degraded fallback, not alignment evidence.

Required policy options:

```text
Disallow
Allow with warning
Require manual confirmation
```

A tile using only expected-grid must have:

```text
PoseSource.ExpectedGridOffset
AlignmentSucceeded = false
Stitchable = policy dependent
```

## 13.5 Manual alignment

The manual dialog must:

- run on the UI thread
- show sample and captured image
- show alpha overlay
- show difference/edge view
- support X/Y
- support rotation
- support scale when enabled
- keyboard nudge
- reset to expected-grid
- reset to best automatic candidate
- Accept
- Skip
- Cancel Run

When accepted, return:

```text
translation + rotation + scale
```

not translation only.

Dispose old preview images when a new request is loaded.

---

# 14. Workflow state and success policy

## 14.1 Separate alignment success from stitchability

Add explicit state properties:

```csharp
bool AlignmentSucceeded
bool IsFallbackPose
bool IsStitchable
```

Do not infer all three from one `PoseSource`.

## 14.2 Run outcome

Use an enum such as:

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

Do not define run success as:

```text
any finite pose exists
```

Define configurable acceptance criteria:

```text
minimum directly/neighbor aligned ratio
maximum fallback ratio
maximum excluded count
required anchor coverage
```

## 14.3 Final-state report

Do not append final poses before second-pass recovery is finished.

After all passes:

```text
rebuild report poses from final states
```

Ensure:

```text
result.States
=
report.Poses
=
states sent to stitcher
=
states shown in PathCanvas
```

---

# 15. Stitching implementation

## 15.1 Inputs

Stitch only states where:

```text
IsStitchable == true
```

Apply exclusion/fallback policy explicitly.

## 15.2 Bounds

Calculate bounds by transforming all four image corners.

Do not use only:

```text
X
Y
X + Width
Y + Height
```

when rotation or scale exists.

## 15.3 Warp

Use the full supported transform:

```text
OpenCV WarpAffine for affine/euclidean
OpenCV WarpPerspective only when homography is truly supported
```

Warp both:

```text
image
valid mask
```

## 15.4 Blending

Implement at minimum:

```text
NoBlend
Feather
WeightedAverage
```

Do not expose `EnableBlending` without using it.

For overlap:

- warp valid masks
- calculate weights
- avoid black-border contamination
- preserve grayscale/colour policy
- log blend mode

## 15.5 Preview versus full-resolution

The preview may be scaled to a megapixel limit.

The full-resolution output must not inherit preview scaling.

## 15.6 TIFF and BigTIFF

The current production `TiffBigWriter` is not a real BigTIFF writer.

Use the extracted reference as an interim reference because it uses:

```text
TiffLibrary
useBigTiff: true
tiled encoding
```

But improve memory behavior.

Production target:

```text
tile/strip streaming writer
```

Do not require one full stitched `Bitmap`.

Do not create multiple full-resolution duplicate pixel buffers.

Select output based on:

```text
estimated byte count
dimensions
pixel format
configured TiffMode
```

Write to a temporary application-owned path and publish atomically.

Validate output by reopening metadata and reading sample tiles/regions.

---

# 16. Compare stitched result with sample

The final result must include a comparison between:

```text
stitched camera mosaic
sample reference
```

## 16.1 Authoritative comparison coordinate space

The stitched camera result is in:

```text
ProcessedSampleGlobalPixels
```

Therefore the authoritative comparison image must also be in:

```text
ProcessedSampleGlobalPixels
```

Preferred source:

```text
processed sample reference created by Tab 2
```

Add or use manifest fields such as:

```text
ProcessedSamplePath
SourceToProcessedTransform
PreprocessMode
```

If these fields do not yet exist:

1. Use `SourceRasterPath` only when source and processed coordinate spaces are identical.
2. If a simple resize transform is fully known, reconstruct it explicitly.
3. For FitPad or CenterCrop, require the exact transform metadata.
4. If the mapping cannot be reconstructed, block authoritative overlay and log a warning.
5. Never silently resize the original sample and call it geometrically accurate.

## 16.2 Comparison products

Create:

```text
sample_reference_preview.png
stitched_preview.png
overlay_comparison.png
difference_comparison.png
edge_comparison.png
```

The full-resolution stitched output remains separate.

## 16.3 UI comparison modes

Support:

```text
Sample only
Stitched only
Alpha overlay
Absolute difference
Edge overlay
Blink comparison
```

Alpha slider must update the bounded preview without modifying source images.

## 16.4 Modality-aware metrics

Camera and Gerber sample have different visual modalities. Raw RGB MAE alone is not sufficient.

Report multiple metrics where applicable:

```text
edge overlap
binary mask IoU
normalized cross-correlation
distance-transform error
valid overlap ratio
```

State which metric is authoritative.

Do not report a single misleading percentage as “accuracy”.

## 16.5 Save comparison metadata

Record:

```text
sample reference path
stitched output path
coordinate-space transform
preview scale
comparison mode
metric values
valid mask area
timestamp
```

---

# 17. Async UI and cancellation

All expensive operations must run outside the UI thread:

```text
manifest validation when large
captured image validation
image decode
preprocessing
HALCON NCC
OpenCV ECC
neighbor matching
interpolation calculation
stitching
BigTIFF writing
comparison generation
report serialization
```

Use:

```text
async/await
CancellationTokenSource
IProgress<T>
```

Do not use:

```text
Application.DoEvents
Thread.Sleep
Task.Result on UI thread
Control access from workers
```

Manual dialog requests must be marshaled to the UI thread through a dedicated dispatcher/provider.

Restore UI state in `finally`.

Prevent:

- double Run
- changing manifest during a run
- changing captured folder during a run
- closing while unmanaged work is still active without cancellation/disposal

Cancellation must:

- stop before the next expensive stage
- not publish a completed report/output
- clean only the current application-owned temporary directory
- preserve previous successful outputs
- log the cancellation location

---

# 18. Output structure

Use:

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
   │  └─ edge_comparison.png
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

Generate diagnostics according to config to avoid excessive storage.

---

# 19. Processing report requirements

Each tile report must contain:

```text
ManifestVersion
OrderIndex
Row
Column
SamplePath
CapturedPath
Image dimensions
Channel/bit depth
Preprocessing candidates
Selected preprocessing variant
Direct methods attempted
HALCON NCC score
ECC correlation
Canonical transform matrix
Translation
Rotation
Scale
Overlap ratio
Direct alignment success
Recovery methods attempted
Neighbor anchor
Pair score
Final pose source
AlignmentSucceeded
IsFallbackPose
IsStitchable
Rejection reason
Fallback reason
Manual action
Processing time
Diagnostic paths
```

Run report must contain:

```text
run status
input paths
config snapshot
start/end time
counts by pose source
direct success ratio
recovery success ratio
fallback ratio
excluded count
stitching bounds
output dimensions
TIFF mode
output paths
comparison metrics
warnings
errors
```

---

# 20. Task plan

Complete tasks in order. Build and test after every task.

## Task 0 — Baseline audit and protection

Create:

```text
docs/tab3_implementation_baseline.md
```

Record:

- current UI call graph
- current alignment call graph
- current recovery call graph
- current stitching call graph
- fake/stub methods
- transform directions
- image ownership
- all Bitmap allocations
- all UI-thread violations
- current build status
- current reference files used

Do not change runtime behavior in Task 0.

## Task 1 — Update scope rules and canonical models

- Update repository guidance so Tab 3 is explicitly in scope.
- Consolidate duplicate `AlignStitchConfig` models.
- Add run-status and explicit state properties.
- Extend manifest only when required for comparison-coordinate correctness.
- Preserve backward compatibility through versioned readers.

## Task 2 — Manifest selection and input UI

Implement:

- Select Manifest button
- manifest path textbox
- derived folder/sample labels
- shared validation
- path relocation policy
- captured folder
- output folder
- Run enable/disable state

## Task 3 — ELog integration

Implement:

- log ListBox
- file logging
- level colors
- tile context
- lifecycle disposal
- processing log copy/export

Test cross-thread logging.

## Task 4 — PathCanvas refactor

Port selected reference behavior into production models.

Add Designer/resx.

Render:

- expected order
- traversal graph
- final states
- recovery edges

Test stable identity and all traversal modes.

## Task 5 — Image interop and validation

Implement:

- HObject/Mat/Bitmap interop
- pixel format validation
- no GetPixel/SetPixel production path
- deterministic ownership tests

## Task 6 — Real preprocessing

Implement every supported mode truthfully.

Add candidate generation and diagnostics.

## Task 7 — HALCON NCC

Implement real model creation/search/cache/disposal.

Add synthetic tests.

## Task 8 — Pyramid ECC

Implement real OpenCvSharp ECC pyramid refinement.

Add transform-direction and synthetic tests.

## Task 9 — Direct alignment policy

Combine NCC and ECC.

Implement rejection and thresholds.

Do not enable stitching yet unless direct result tests pass.

## Task 10 — Neighbor recovery

Implement actual captured-to-captured pair alignment.

Remove hard-coded acceptance.

Visualize accepted edges.

## Task 11 — Anchor interpolation and manual alignment

Implement real interpolation.

Complete manual overlay, difference, rotation, and UI-thread dispatch.

## Task 12 — Workflow outcome and report

Rebuild final states and report after all passes.

Separate alignment success from fallback stitchability.

## Task 13 — Production UI wiring

Replace simulated Run with real async workflow.

Remove `Application.DoEvents()`.

Update PathCanvas and diagnostics through progress snapshots.

## Task 14 — Stitching

Implement transformed bounds, warp masks, blending, preview, and cancellation.

## Task 15 — TIFF/BigTIFF

Implement real writer selection and streaming-oriented output.

Validate output.

## Task 16 — Sample comparison

Implement authoritative processed-sample overlay, difference products, metrics, and UI.

## Task 17 — Integration and regression tests

Run full scenarios and document results.

---

# 21. Required test scenarios

## Scenario A — Manifest selection

```text
Select valid sample_manifest.json
→ fields populated
→ tile paths resolved
→ Run remains disabled until captured folder is valid
```

Test copied/moved run folders.

## Scenario B — Mapping

```text
Natural sort: 0, 1, 2, 10
→ Captured K maps to Sample OrderIndex K
```

Shuffle JSON tile list and verify mapping remains correct.

## Scenario C — Direct alignment

Use known synthetic transforms.

Verify translation, rotation, scale, score, and direction.

## Scenario D — Different modalities

Test:

```text
binary sample
grayscale camera
illumination gradient
noise
inverted polarity
```

## Scenario E — Rejected direct match

Ensure failed direct alignment does not become `SampleAlignOk`.

## Scenario F — Neighbor recovery

Use an anchor/target pair with known overlap.

Verify real pair score and global composition.

## Scenario G — Interpolation

Remove one or more tiles between valid anchors.

Verify interpolated pose and source label.

## Scenario H — Expected-grid policy

Verify it is not counted as alignment success.

Test:

```text
disallow
allow with warning
manual confirmation
```

## Scenario I — Manual alignment

Test X/Y, rotation, alpha overlay, difference, nudge, reset, accept, skip, and cancel.

## Scenario J — PathCanvas

Verify arrows and states for all orders and recovery paths.

## Scenario K — Stitching

Test:

```text
translation
rotation
scale
negative global coordinates
excluded tile
overlap blending
```

Verify transformed bounds do not crop corners.

## Scenario L — BigTIFF

Use a simulated output size beyond Standard TIFF policy.

Verify real BigTIFF header/writer path and bounded memory behavior.

## Scenario M — Comparison overlay

Verify stitched and sample coordinate spaces match.

Ensure overlay is blocked when the source-to-processed transform is unknown.

## Scenario N — Cancellation

Cancel during:

```text
NCC
ECC
neighbor matching
stitching
TIFF writing
comparison generation
```

No completed report/output may be published.

## Scenario O — Logging

Verify:

- UI ListBox receives messages
- log file is written
- worker-thread messages are safe
- errors contain tile context
- logger is disposed

---

# 22. Build and quality gates

After every task:

1. Build `Debug|x64`.
2. Build `Release|x64`.
3. Run relevant tests.
4. Record warnings.
5. Record native-runtime tests that could not run.
6. Do not mark a task complete from static inspection.

Block completion if any remain:

```text
UI simulation
Application.DoEvents
fake NCC model
fake ECC correlation
hard-coded PairMatching success
expected-grid labeled as real recovery
report poses stale after second pass
manifest mapped by JSON list position
full-size Bitmap stitching
BigTIFF implemented by Bitmap.Save
rotation ignored in manual result
alpha slider with no overlay
PathCanvas ignoring TraversalGraph
output published after partial failure
```

---

# 23. Required documentation

Create or update:

```text
docs/tab3_implementation_baseline.md
docs/tab3_fix_task_results.md
docs/tab3_fix_changed_files.md
docs/tab3_fix_test_report.md
docs/tab3_transform_contract.md
docs/tab3_image_ownership.md
docs/tab3_manifest_resolution.md
docs/tab3_alignment_validation.md
docs/tab3_output_contract.md
```

## `tab3_fix_task_results.md`

| Task | Status | Files changed | Debug x64 | Release x64 | Tests | Notes |
|---|---|---|---|---|---|---|

## `tab3_fix_changed_files.md`

| File | Class/Method | Before | After | Reason |
|---|---|---|---|---|

## `tab3_image_ownership.md`

| Resource | Owner | Created by | Replaced by | Disposed by | UI/Worker |
|---|---|---|---|---|---|

## `tab3_transform_contract.md`

Document:

```text
coordinate spaces
matrix direction
HALCON conversion
OpenCV conversion
global composition
stitch canvas translation
sample comparison transform
```

---

# 24. Expected changed files

The exact set may vary, but expected production changes include:

```text
GerberViewer/Views/AlignStitchingControl.cs
GerberViewer/Views/AlignStitchingControl.Designer.cs
GerberViewer/Views/AlignStitchingControl.resx

GerberViewer/Views/ManualAlignmentDialog.cs
GerberViewer/Views/ManualAlignmentDialog.Designer.cs
GerberViewer/Views/ManualAlignmentDialog.resx

GerberViewer/Stitching/PathCanvasControl.cs
GerberViewer/Stitching/PathCanvasControl.Designer.cs
GerberViewer/Stitching/PathCanvasControl.resx

GerberViewer/Workflow/Models/WorkflowContext.cs

GerberStitching.Core/Models/SampleManifest.cs
GerberStitching.Core/Models/WorkflowModels.cs
GerberStitching.Core/Arrangement/CapturedImageLoader.cs
GerberStitching.Core/Alignment/*
GerberStitching.Core/Stitching/*
GerberStitching.Core/Imaging/ImageInterop/*
GerberStitching.Core/Reporting/*
GerberStitching.Core/Comparison/*
GerberStitching.Core/Utils/TiffBigWriter.cs

GerberViewer/GerberViewer.csproj
GerberStitching.Core/GerberStitching.Core.csproj
```

Do not edit `EWindowControl`.

Do not edit files under `reference/`.

Only edit `Elog_1_0` when a general compatibility defect prevents safe use, and document the reason. Prefer using its existing API unchanged.

---

# 25. Final response format

At completion, report:

1. Root causes fixed.
2. Exact UI changes.
3. Manifest selection and path-resolution behavior.
4. ELog initialization and log locations.
5. PathCanvas changes adopted from reference.
6. Canonical transform direction.
7. HALCON NCC implementation details.
8. OpenCV Pyramid ECC implementation details.
9. Direct alignment acceptance policy.
10. Neighbor and interpolation recovery implementation.
11. Manual alignment behavior.
12. Stitching and blending behavior.
13. TIFF/BigTIFF behavior.
14. Sample overlay/difference comparison.
15. Exact files and methods changed.
16. Debug x64 result.
17. Release x64 result.
18. Tests passed.
19. Tests not run and why.
20. Remaining risks.

Do not state that Tab 3 is complete unless:

```text
camera images are actually aligned to sample tiles
false-success paths are removed
final poses are internally consistent
stitching uses those final poses
stitched output is compared in the correct sample coordinate space
logs and reports describe the true processing result
```
