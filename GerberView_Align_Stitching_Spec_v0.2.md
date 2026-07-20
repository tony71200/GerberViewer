# GERBERVIEW – CREATE GERBER SAMPLE, ALIGNMENT AND STITCHING SPECIFICATION

**Document version:** 0.2 – Confirmed Requirements Draft  
**Target solution:** `GerberViewer.sln`  
**Target framework:** .NET Framework 4.8 / WinForms  
**Reference sources:** `GerberView.zip`, `StitchingImage.zip`

---

## 1. Purpose

Extend the current Gerber Viewer into a three-stage workflow:

1. **Read Gerber** – preserve all current Gerber loading, rendering, layer management and export functions.
2. **Create Gerber Sample** – divide a rendered Gerber sample image into ordered, overlapping tiles used as alignment references.
3. **Align and Stitching** – load captured images, arrange them according to the configured order, align each captured image to its corresponding Gerber sample tile, recover failed alignments, and compose the final stitched image.

The implementation must keep UI code separate from processing code. Long-running crop, alignment and stitching operations must run outside the UI thread and report progress to the UI.

---

## 2. Core design decisions

### 2.1 Do not expand `MainForm.cs` into a monolithic file

Each main tab must be implemented as an independent `UserControl`:

- `ReadGerberControl`
- `CreateGerberSampleControl`
- `AlignStitchingControl`

`MainForm` only owns the root `TabControl`, shared status information and workflow coordination.

### 2.2 Separate sample alignment from neighbor stitching

The existing `StitchingImage` matchers are primarily designed for matching overlapping edges between neighboring camera images. The new workflow needs two different operations:

- **Sample-to-Capture Alignment:** align a captured image against its corresponding Gerber sample tile and calculate an absolute pose.
- **Capture-to-Capture Alignment:** align neighboring captured images when sample alignment fails or when an additional refinement is required.

These operations must not be treated as the same pipeline.

### 2.3 Use absolute Gerber sample positions as global anchors

A successfully aligned captured image receives a global transform derived from:

```text
GlobalPoseCaptured = GlobalPoseSampleTile × LocalCapturedToSampleTransform
```

This prevents the accumulated drift that occurs when every global pose is calculated only by chaining neighbor-to-neighbor transforms.

### 2.4 Never silently accept a rejected match

A match is successful only when:

```csharp
pairResult?.Eval?.IsMatch == true
```

Checking only `pairResult.Eval != null` is prohibited because the current `StitchingImage` source can create an evaluation object whose `IsMatch` is false.

### 2.5 Keep fallback results traceable

Every image pose must contain a source classification:

- `SampleAlignment`
- `NeighborAlignment`
- `AnchorAdjusted`
- `Interpolated`
- `ExpectedGridOffset`
- `Manual`
- `Excluded`
- `Failed`

Fallback results must be visually distinguished and written to the processing report.

### 2.6 Confirmed runtime policies

The following decisions are mandatory for version 1:

- `Open Sample` accepts an external raster file only: PNG, BMP, TIFF or BigTIFF when the reader supports it.
- Sample geometry is generated from `Rows`, `Columns` and `Overlap`; overlap defaults to `60 px`.
- Overlap supports both `Pixel` and `Percent`, with `Pixel` as the default and preferred unit.
- Captured image `OrderIndex K` maps to sample tile `OrderIndex K`.
- Primary sample alignment uses OpenCvSharp Pyramid ECC and HALCON NCC.
- Recovery order is fixed: neighbor alignment → anchor interpolation/adjustment → expected-grid pose → manual adjustment.
- Final output is TIFF or BigTIFF.
- HALCON is mandatory. A missing DLL, incompatible runtime, failed self-test or invalid license blocks application startup.

---

## 3. Proposed solution structure

```text
GerberViewer.sln
├─ GerberViewer
│  ├─ MainForm.cs
│  ├─ MainForm.Designer.cs
│  ├─ Views
│  │  ├─ ReadGerberControl.cs
│  │  ├─ ReadGerberControl.Designer.cs
│  │  ├─ CreateGerberSampleControl.cs
│  │  ├─ CreateGerberSampleControl.Designer.cs
│  │  ├─ AlignStitchingControl.cs
│  │  └─ AlignStitchingControl.Designer.cs
│  └─ Workflow
│     ├─ Models
│     ├─ Services
│     ├─ Progress
│     └─ Validation
├─ GerberEngine
├─ EWindowControl
└─ GerberStitching.Core               [recommended new class-library project]
   ├─ Alignment
   ├─ Arrangement
   ├─ Configuration
   ├─ Imaging
   ├─ Stitching
   └─ Reporting
```

A separate `GerberStitching.Core` project is recommended so OpenCV, HALCON, BigTIFF and stitching logic do not become coupled to WinForms controls.

All migrated classes from `StitchingImage` must use the target namespace, for example:

```csharp
GerberViewer.Stitching.Alignment
GerberViewer.Stitching.Arrangement
GerberViewer.Stitching.Imaging
GerberViewer.Stitching.Configuration
```

Do not retain the original `StitchingImage.*` namespace inside the target solution.

---

## 4. Main window layout

### 4.1 Root control

Add a root `TabControl` named `mainTabControl` with these pages in this order:

1. `Read Gerber`
2. `Create Gerber Sample`
3. `Align and Stitching`

The existing status strip may remain at the bottom of `MainForm`, but status text must indicate the active workflow and processing state.

### 4.2 Shared workflow state

`MainForm` or a dedicated `WorkflowContext` must retain:

- Current external sample raster path.
- Current sample preprocessing metadata and processed dimensions.
- Current sample configuration.
- Current sample manifest path.
- Current sample output directory.
- Current alignment/stitching configuration.
- Last stitched output path.

Tab controls must communicate through typed events or `WorkflowContext`; they must not access each other’s private UI controls.

---

## 5. Tab 1 – Read Gerber

### 5.1 Functional requirement

Move all existing Gerber Viewer controls and behavior into the `Read Gerber` tab without regression:

- Open multiple Gerber files.
- Drag and drop Gerber files.
- Show and hide layers.
- Change layer colors.
- Reorder and remove layers.
- Render preview.
- Zoom, pan and fit.
- Display board dimensions and cursor coordinates.
- Export selected layer.
- Export combined Gerber image.

### 5.2 Refactor requirement

The current Gerber toolbar, layer list and `GerberCanvas` must be moved into `ReadGerberControl`. Existing event handlers should be moved with the controls instead of forwarding every event through `MainForm`.

### 5.3 Workflow isolation

For version 1, `Create Gerber Sample` does **not** consume the current render from the `Read Gerber` tab. Its source must be selected through `Open Sample` as an external raster file.

The existing Gerber rendering API may remain reusable internally, but direct tab-to-tab raster transfer is outside the required version-1 workflow. This avoids hidden coupling between the current render DPI/state and the sample-generation configuration.

---

## 6. Tab 2 – Create Gerber Sample

## 6.1 Purpose

Create ordered, overlapping reference tiles from an external raster representation of the Gerber sample. The generated tiles and manifest are later used to map and align captured images in Tab 3.

## 6.2 UI layout

### Top command area

| Control | Name | Requirement |
|---|---|---|
| Button | `btnOpenSample` | Open an external raster sample: `.png`, `.bmp`, `.tif` or `.tiff`. It must not import the active Gerber render in version 1. |
| TextBox | `txtSamplePath` | Read-only absolute path of the selected raster sample. |
| Button | `btnLoadSampleConfig` | Load a sample configuration file. |
| Button | `btnCreateSample` | Validate configuration and start tile generation. |
| Button | `btnCancelCreateSample` | Cancel an active crop operation. Recommended. |
| ProgressBar | `prgCreateSample` | Show completed tile count. |
| Label | `lblCreateSampleStatus` | Show current tile, output path and status. |

### Main area

Use a horizontal `SplitContainer`:

- **Left panel:** `EWindowControl` named `sampleWindow`.
- **Right panel:** typed configuration table named `sampleConfigGrid`.

The configuration table must not rely only on free-form string cells. Enum values should use combo-box cells, Boolean values should use checkbox cells, and numeric values must be range validated.

## 6.3 Sample configuration

Define `GerberSampleConfig` with at least these properties:

| Property | Type | Default | Description |
|---|---:|---:|---|
| `CropOrder` | enum | `Zigzag` | Traversal strategy migrated from `StitchingImage`: `Zigzag`, `Branch`, `BranchDown`. UI label may show `Snake Order` for `Zigzag`, but the code symbol must remain unambiguous. |
| `StartOrder` | enum | `TopLeftRight` | Only four supported presets: `TopLeftRight`, `TopLeftDown`, `BottomRightLeft`, `BottomRightUp`. |
| `InvertImage` | bool | `false` | Invert sample pixel intensity before tile generation. This is not coordinate inversion. |
| `Rows` | int | required | Number of physical grid rows. Minimum 1. |
| `Columns` | int | required | Number of physical grid columns. Minimum 1. |
| `OverlapValue` | double | `60` | Common overlap value for X and Y unless separate-axis support is later enabled. |
| `OverlapUnit` | enum | `Pixel` | `Pixel` or `Percent`. Pixel is the preferred and default unit. |
| `PreprocessMode` | enum | `None` | `None`, `Resize`, `FitPad`, or `CenterCrop`. Geometry is calculated on the preprocessed source. |
| `PreprocessWidth` | int | `0` | Target source width. `0` means use original width. Required when preprocessing changes dimensions. |
| `PreprocessHeight` | int | `0` | Target source height. `0` means use original height. Required when preprocessing changes dimensions. |
| `KeepAspectRatio` | bool | `true` | Prevent unintended geometric distortion during source preprocessing. |
| `OutputDirectory` | string | required | Sample tile output directory. |
| `OutputFormat` | enum | `Png` | Tile format: `Png`, `Bmp`, or `Tiff`. PNG is recommended for binary reference tiles. |
| `TileNamePattern` | string | `Sample_R{row:00}_C{col:00}_O{order:000}` | Deterministic tile naming. |
| `DrawOrderLabels` | bool | `true` | Display tile order numbers in the preview overlay. |
| `SaveOverlayPreview` | bool | `true` | Save a preview containing crop rectangles, order labels and traversal arrows. |

`StartOrder` maps to the existing arrangement concepts as follows:

| StartOrder preset | StartCorner | RobotMovement |
|---|---|---|
| `TopLeftRight` | `TopLeft` | `Right` |
| `TopLeftDown` | `TopLeft` | `Down` |
| `BottomRightLeft` | `BottomRight` | `Left` |
| `BottomRightUp` | `BottomRight` | `Up` |

The UI must not expose unsupported combinations such as `TopRight + Left` in version 1.

Recommended read-only derived values:

- `OriginalSourceWidth`, `OriginalSourceHeight`
- `ProcessedSourceWidth`, `ProcessedSourceHeight`
- `TileWidth`, `TileHeight`
- `StepX`, `StepY`
- `ExpectedTileCount = Rows × Columns`
- resolved `StartCorner` and `RobotMovement`

### Important distinction

`InvertImage` changes the pixel intensity of the generated sample tiles. `InvertXOnParse`, inherited conceptually from `StitchingImage`, changes parsed robot coordinates. These settings must remain separate.

## 6.4 Preprocessing and crop geometry

### 6.4.1 Geometry coordinate space

The selected raster is preprocessed first. All crop rectangles, tile origins and manifest coordinates are calculated in the **processed-source pixel coordinate system**.

```text
External raster
→ optional resize / fit-pad / center-crop
→ optional intensity inversion
→ processed source
→ calculate Rows × Columns crop geometry
→ crop tiles
```

Non-uniform resize is prohibited when `KeepAspectRatio = true`. If preprocessing changes scale or adds padding, the manifest must store the source-to-processed transform so coordinates remain auditable.

### 6.4.2 Pixel overlap

For source width `W`, columns `C`, and overlap `Ox` pixels:

```text
tileWidth = (W + (C - 1) × Ox) / C
stepX     = tileWidth - Ox
```

For source height `H`, rows `R`, and overlap `Oy` pixels:

```text
tileHeight = (H + (R - 1) × Oy) / R
stepY      = tileHeight - Oy
```

Version 1 uses the common configured value:

```text
Ox = Oy = OverlapValue
```

The default is `60 px`.

### 6.4.3 Percent overlap

When overlap is a ratio of tile size, convert the configured percentage `P` to `p = P / 100`:

```text
tileWidth  = W / [1 + (C - 1) × (1 - p)]
tileHeight = H / [1 + (R - 1) × (1 - p)]
stepX      = tileWidth  × (1 - p)
stepY      = tileHeight × (1 - p)
```

Valid percent range is `0 ≤ P < 100`.

### 6.4.4 Rounding and boundary policy

- Geometry calculations must use `double`.
- Integer crop boundaries must use one deterministic policy throughout the application.
- Recommended: calculate boundary arrays and round boundaries, not each tile width independently.
- First boundary must be `0`; final boundary must equal processed source width/height.
- Every tile must remain inside source bounds.
- Adjacent measured overlap may differ by at most one pixel due to rounding.
- Reject pixel overlap when it produces `tileWidth <= overlap` or `tileHeight <= overlap`.

## 6.5 Tile ordering

The physical crop rectangles must first be generated as a stable `[row, column]` matrix. Traversal order is applied afterward and must never mutate physical row/column identity.

Each tile must retain:

- physical `Row`
- physical `Column`
- sequential `OrderIndex`
- crop rectangle
- expected neighbors
- traversal predecessor and successor

Use `OrderMode`, `StartCorner`, `RobotMovement`, `RobotArrange` and `TraversalGraph` concepts from `StitchingImage`. Do not use the legacy `RobotOrderer` path.

`StartOrder` must be resolved through the fixed preset table in section 6.3. For vertical presets (`TopLeftDown`, `BottomRightUp`), traversal proceeds primarily by columns; for horizontal presets it proceeds primarily by rows.

The same resolved order must be written to `sample_manifest.json` and later reused unchanged by the captured-image workflow.

## 6.6 Live preview behavior

Before cropping begins:

- Display the full sample image.
- Draw all crop rectangles.
- Draw order labels and traversal arrows.

During cropping:

- Pending tile: neutral outline.
- Current tile: yellow or another high-contrast outline.
- Completed tile: green outline.
- Failed tile: red outline.
- Auto-scroll or zoom is optional; the full layout should remain visible by default.

UI updates must use `IProgress<SampleCropProgress>` or equivalent marshaling. The crop loop must not directly access `EWindowControl` from a worker thread.

## 6.7 Output files

A successful run must create:

```text
<SampleOutputDirectory>/
├─ tiles/
│  ├─ Sample_R00_C00_O000.png
│  ├─ Sample_R00_C01_O001.png
│  └─ ...
├─ sample_manifest.json
├─ sample_config.json
└─ sample_overlay.png
```

## 6.8 Sample manifest

`sample_manifest.json` is mandatory. It is the contract between Tab 2 and Tab 3.

Each entry must include:

```json
{
  "tileId": 0,
  "orderIndex": 0,
  "row": 0,
  "column": 0,
  "filePath": "tiles/Sample_R00_C00_O000.png",
  "cropX": 0,
  "cropY": 0,
  "cropWidth": 2048,
  "cropHeight": 2048,
  "globalOriginX": 0.0,
  "globalOriginY": 0.0,
  "predecessorTileId": null,
  "horizontalNeighborIds": [1],
  "verticalNeighborIds": [4]
}
```

Manifest-level metadata must include:

- external source image path
- source image hash
- source DPI when present in raster metadata
- original source dimensions
- processed source dimensions
- source-to-processed transform, including scale and padding/crop offsets
- rows and columns
- overlap definition
- crop order
- `StartOrder` preset and resolved start corner/movement
- image inversion status
- creation timestamp
- application/spec version

## 6.9 Create Sample processing flow

```text
Open external raster sample
→ validate image and config
→ preprocess source to configured dimensions
→ calculate physical tile matrix
→ calculate traversal graph
→ render complete overlay
→ create output directory
→ crop tile 0..N-1 in traversal order
→ update live overlay after every tile
→ save tile and manifest entry
→ save config, manifest and overlay
→ publish manifest to WorkflowContext
```

Partial output must be removed or marked incomplete if the operation is cancelled or fails.

---

## 7. Tab 3 – Align and Stitching

## 7.1 Purpose

Load captured images, order them consistently with the sample manifest, align them with Gerber sample tiles, recover images that cannot be aligned directly, and compose the final stitched image.

## 7.2 UI layout

### Left command and input area

| Control | Name | Requirement |
|---|---|---|
| Button | `btnOpenImageFolder` | Select the captured-image folder. |
| TextBox | `txtImageFolder` | Read-only selected folder path. |
| ListBox | `lstCapturedImages` | Show all accepted image filenames in parsed/order sequence. |
| Label | `lblImageCount` | Show loaded, parsed, rejected and expected counts. |
| Button | `btnRunAlignStitch` | Start the workflow. |
| Button | `btnCancelAlignStitch` | Cancel processing. Recommended. |
| ProgressBar | `prgAlignStitch` | Overall progress. |

Each list item should expose status through text or owner drawing:

```text
[Pending]  CameraImage0001...
[Align OK] CameraImage0002...
[Fallback] CameraImage0003...
[Failed]   CameraImage0004...
```

### Center result area

Use an internal `TabControl` named `resultTabControl`:

1. **Order View**
   - Reuse or adapt `PathCanvasControl` from `StitchingImage`.
   - Show tile nodes, row/column, order index and traversal links.
   - Highlight the current image and current matching edge.
   - Distinguish success, fallback and failure states.

2. **Stitched Image**
   - Use `EWindowControl` to display the current stitched preview and final output.
   - Update incrementally according to `PreviewUpdateInterval` rather than after every pixel operation.

A small current-pair diagnostic panel is recommended for showing:

- sample tile
- captured image
- warped captured image
- overlap or difference view
- match score and rejection reason

## 7.3 Right configuration area

Use a typed configuration grid. Split basic and advanced parameters.

### Basic parameters

| Property | Type | Default | Description |
|---|---:|---:|---|
| `SampleManifestPath` | string | current workflow manifest | Manifest created in Tab 2. |
| `OrderingSource` | enum | `ManifestOrder` | Fixed to manifest order in version 1. |
| `AlignMethod` | enum | `NccThenPyramidEcc` | `NccThenPyramidEcc`, `PyramidEcc`, or `HalconNcc`. |
| `ExpectedOverlapValue` | double | from manifest | Read-only by default; can be overridden only in advanced mode. |
| `ExpectedOverlapUnit` | enum | from manifest | `Pixel` or `Percent`. |
| `ExpectedImageWidth` | int | required/auto-detect | Expected captured-image width in pixels. |
| `ExpectedImageHeight` | int | required/auto-detect | Expected captured-image height in pixels. |
| `SamplePreprocessProfile` | enum | `GerberBinary` | Preprocessing profile for binary Gerber tiles. |
| `CapturedPreprocessProfile` | enum | `CameraGray` | Preprocessing profile for grayscale camera images. |
| `OutputPath` | string | required | Final `.tif` or `.tiff` path. |
| `TiffMode` | enum | `Auto` | `Auto`, `StandardTiff`, or `BigTiff`; `Auto` selects BigTIFF when needed. |
| `PreviewScale` | double | auto | Downscale factor used only for UI preview. It must not change full-resolution output. |

The following order values are read from the sample manifest and shown as read-only:

- `CropOrder`
- `StartOrder`
- `Rows`
- `Columns`
- expected tile count

### Required alignment validation parameters

These values are based on `StitchingConfig` from `StitchingImage`:

- `MinInliers`
- `MinInlierRatio`
- `MaxRmse`
- `MinOverlapRatio`
- `MaxAbsRotationDeg`
- `RansacThreshold`
- `RansacConfidence`
- `RansacMaxIterations`
- `RoiMatchFraction`
- `RoiMinPixels`
- `PhaseCorrelationMinResponse`


Method-specific required parameters:

- `NccMinScore`
- `NccNumLevels`
- `NccAngleStartDeg`
- `NccAngleExtentDeg`
- `NccMinContrast`
- `EccMotionType` (`Translation`, `Euclidean`, or `Affine`; default `Euclidean`)
- `EccPyramidLevels`
- `EccIterationsPerLevel`
- `EccEpsilon`
- `EccMinCorrelation`
- `MaxTranslationDeviationFromExpectedPx`

### Fallback and adjustment parameters

| Property | Type | Description |
|---|---:|---|
| `EnableNeighborFallback` | bool | Try captured-image neighbor matching after sample alignment fails. |
| `EnableInterpolationFallback` | bool | Interpolate poses between valid anchors. |
| `EnableExpectedGridFallback` | bool | Use expected offset based on dimensions and overlap as the final automatic fallback. |
| `EnableManualFallback` | bool | Must be `true` in version 1; opens the manual review workflow for unresolved images. |
| `AcceptFallbackForOutput` | bool | Allow fallback images to enter the final stitched output. |
| `MaxConsecutiveFallbacks` | int | Limit uncontrolled propagation through a long failed sequence. |
| `RefineSuccessfulPose` | bool | Apply ECC/phase refinement after initial alignment. |
| `PreviewUpdateInterval` | int | Update stitched preview every N images. |

## 7.4 Input loading, natural sorting and fixed mapping

### Supported input

At minimum:

- `.bmp`
- `.png`
- `.jpg` / `.jpeg`
- `.tif` / `.tiff`

### Loading flow

```text
Open Folder
→ enumerate supported files
→ natural-sort filenames
→ validate image readability and dimensions
→ compare count with sample manifest tile count
→ assign OrderIndex 0..N-1
→ map image K to sample tile K
→ display the resolved matrix and traversal in Order View
```

### Mandatory mapping rule

```text
CapturedImages[OrderIndex K] ↔ SampleManifest.Tiles[OrderIndex K]
```

Version 1 must not remap by robot coordinates, `PositionId`, filename metadata, nearest content or list selection. Natural filename sorting occurs **before** assigning `OrderIndex`.

When image count differs from `Rows × Columns`:

- More images than expected: block execution and show the extra filenames.
- Fewer images than expected: block automatic run unless the missing-index workflow is explicitly enabled in a future version.
- Duplicate natural-sort keys: block execution and request deterministic filenames.

## 7.5 Sample-to-capture alignment

Create a dedicated interface instead of forcing binary-sample alignment through the edge-only neighbor matcher API:

```csharp
public interface ISampleAligner
{
    SampleAlignmentResult Align(
        string sampleTilePath,
        string capturedImagePath,
        SampleAlignmentContext context,
        CancellationToken cancellationToken);
}
```

`SampleAlignmentResult` must include:

- success state
- transform from captured image to sample tile
- NCC score and/or ECC correlation
- rotation, translation and scale
- overlap ratio
- method and preprocessing variant used
- rejection reason
- diagnostic/debug images when enabled

### 7.5.1 Modality-aware preprocessing

The Gerber sample is binary while captured images are grayscale with illumination variation, background, noise and material texture. The aligner must support this configurable pipeline:

```text
Load
→ grayscale conversion
→ normalize contrast
→ optional polarity inversion
→ optional threshold
→ edge extraction
→ Gerber-content mask
→ size normalization
→ alignment
```

Required preprocessing operations:

- contrast normalization: min-max, CLAHE or configurable equivalent
- sample polarity: as-generated or inverted
- captured polarity: as-is, inverted, or auto-try-both
- threshold: fixed, Otsu or adaptive
- edge extraction: Sobel/Canny for OpenCV and equivalent HALCON region/contour preparation
- mask: exclude empty Gerber background and invalid padded pixels

Preprocessing parameters and the selected variant must be written to the report. The application must not overwrite original input images.

### 7.5.2 OpenCvSharp Pyramid ECC mode

Implement a full-tile Pyramid ECC aligner using OpenCvSharp:

1. Convert sample and captured representations to compatible single-channel floating-point images.
2. Build coarse-to-fine image pyramids.
3. Initialize the coarsest level from expected tile pose or an optional NCC result.
4. Run `Cv2.FindTransformECC` at each level.
5. Scale the transform to the next finer level.
6. Validate final correlation, translation, rotation, overlap and transform finiteness.

Default motion model is `Euclidean`. `Affine` may be exposed in advanced settings; unconstrained homography is not the default because it can deform the physical grid.

The existing edge-oriented `EccMatcher2` can provide implementation ideas, but it must not be reused unchanged because it crops border ROIs for neighbor matching.

### 7.5.3 HALCON NCC mode

Implement an NCC aligner using HALCON operators conceptually equivalent to:

```text
create_ncc_model
→ find_ncc_model
→ generate transform from row/column/angle
→ validate score and pose bounds
```

Requirements:

- HALCON NCC is mandatory and must use the same preprocessed representation and valid-content mask policy.
- Model creation must be cached per sample tile for the duration of a run, or persisted using a versioned model-cache strategy.
- All NCC model handles and HALCON objects must be disposed deterministically.
- A result below `NccMinScore` is a failed alignment, not a fallback success.

### 7.5.4 Default hybrid mode

Default `AlignMethod = NccThenPyramidEcc`:

```text
HALCON NCC coarse localization
→ convert NCC pose to OpenCV transform
→ Pyramid ECC refinement
→ final validation
```

Acceptance policy:

- If NCC and ECC pass: accept refined ECC pose as `SampleAlignment`.
- If NCC passes but ECC fails: accept NCC only when its score and geometric validation pass; record `NccOnlyAccepted` warning.
- If NCC fails: optionally run Pyramid ECC from expected-grid initialization.
- If all direct sample methods fail: enter the fixed recovery pipeline in section 7.7.

## 7.6 Global pose calculation

For every successfully aligned tile:

```text
SampleGlobalTranslation = Translation(cropX, cropY)
CapturedGlobalPose = SampleGlobalTranslation × CapturedToSampleTransform
```

The transform direction must be documented and standardized. The existing matcher convention is generally `B → A`; adapters must not mix `Sample → Captured` and `Captured → Sample` matrices.

All transforms should be represented internally as `3 × 3 CV_64F` homography matrices. Rigid `2 × 3` matrices may be stored as diagnostics but should be converted before global composition.

## 7.7 Failed alignment recovery and manual adjustment

The recovery policy is fixed and must execute in this order for every direct sample-alignment failure:

```text
A. Neighbor alignment
→ B. Anchor adjustment / interpolation
→ C. Expected-grid pose
→ D. Manual adjustment
→ fail only when manual adjustment is cancelled or invalid
```

### Stage A – Neighbor alignment

Match the failed captured image against a directly adjacent captured image that already has a validated global pose. Existing `PairMatching` implementations from `StitchingImage` may be used here because this is captured-to-captured matching.

For neighbor A with known global pose and failed image B:

```text
GlobalPoseB = GlobalPoseA × TransformBToA
```

Only accept when:

```csharp
pairResult?.Eval?.IsMatch == true
```

Prefer neighbors in this order:

1. traversal predecessor with validated pose
2. horizontal/vertical physical neighbor with the best alignment quality
3. traversal successor if it has already been solved by a second pass

### Stage B – Anchor adjustment and interpolation

For a failed sequence bounded by validated anchors:

- calculate a candidate from each available anchor
- interpolate translation and rotation by physical grid distance
- adjust candidates to minimize disagreement with sample tile origins and expected grid step
- reject results exceeding configured rotation or translation deviation

Mark accepted results as `Interpolated` or `AnchorAdjusted`; never as direct alignment success.

### Stage C – Expected-grid pose

Calculate a deterministic pose from the corresponding sample tile origin, expected image dimensions and configured overlap. Mark it as `ExpectedGridOffset` and display a warning.

Expected-grid poses may be included in output only when `AcceptFallbackForOutput = true`.

### Stage D – Manual adjustment

Manual adjustment is required in version 1. Open `ManualAlignmentDialog` for unresolved images.

Required dialog features:

- sample tile view
- captured image view
- alpha-blended overlay/difference view
- translate X/Y using drag, numeric input and keyboard nudge
- rotate using numeric input and coarse/fine controls
- optional scale control, disabled by default
- reset to expected-grid pose
- reset to best automatic candidate
- display NCC/ECC diagnostics and previous rejection reasons
- `Accept`, `Skip`, and `Cancel Run`

An accepted manual transform is marked `Manual`. `Skip` marks the image `Excluded`; `Cancel Run` cancels the workflow. The application must not label an unconfirmed expected pose as manual success.

## 7.8 Stitching

### Recommended API

Add a new absolute-pose stitching method:

```csharp
StitchResult StitchFromGlobalTransforms(
    IReadOnlyList<StitchImagePose> images,
    StitchOutputConfig config,
    IProgress<StitchProgress> progress,
    CancellationToken cancellationToken);
```

Do not convert absolute sample alignment results into artificial graph edges merely to satisfy the existing `StitchingImage.Stitch` signature.

### Composition

The stitcher must:

1. Calculate transformed image bounds.
2. Calculate canvas translation and scale.
3. Enforce a maximum preview canvas megapixel limit.
4. Warp each image using its global pose.
5. Apply mask-based composition.
6. Optionally apply seam or blending logic.
7. Update the preview every configured N images.
8. Save full-resolution output independently from preview resolution.

### Output modes

- **Preview:** downscaled in-memory canvas for interactive display only.
- **Standard TIFF:** used when predicted file size and TIFF offsets remain within standard TIFF limits.
- **BigTIFF:** used when the predicted output may exceed standard TIFF limits.
- **Auto:** default; estimate the output before writing and select TIFF or BigTIFF deterministically.

The final production artifact must be `.tif` or `.tiff`. PNG is not a final-output option. HALCON and OpenCvSharp are both mandatory runtime dependencies: HALCON provides NCC and may participate in image composition; OpenCvSharp provides Pyramid ECC and warping. Both paths must use the same global-pose convention.

## 7.9 Live processing display

During alignment and stitching:

- Highlight the current image node in Order View.
- Highlight the current neighbor edge when fallback matching is running.
- Update node color based on result state.
- Display current sample and captured filenames.
- Display method, score, translation, rotation and reason.
- Refresh stitched preview at a controlled interval.

Required node states:

- Pending
- Processing
- Sample Align OK
- Neighbor Align OK
- Interpolated
- Expected Offset
- Failed
- Excluded

---

## 8. Configuration files

Use a versioned workflow configuration with separate sample and alignment sections:

```json
{
  "schemaVersion": "2.0",
  "sample": {
    "cropOrder": "Zigzag",
    "startOrder": "TopLeftRight",
    "invertImage": false,
    "rows": 4,
    "columns": 4,
    "overlapValue": 60.0,
    "overlapUnit": "Pixel",
    "preprocessMode": "None",
    "preprocessWidth": 0,
    "preprocessHeight": 0,
    "keepAspectRatio": true
  },
  "alignment": {
    "orderingSource": "ManifestOrder",
    "alignMethod": "NccThenPyramidEcc",
    "expectedImageWidth": 4096,
    "expectedImageHeight": 4096,
    "nccMinScore": 0.65,
    "eccMotionType": "Euclidean",
    "eccPyramidLevels": 4,
    "eccMinCorrelation": 0.80,
    "enableNeighborFallback": true,
    "enableInterpolationFallback": true,
    "enableExpectedGridFallback": true,
    "enableManualFallback": true,
    "acceptFallbackForOutput": true,
    "tiffMode": "Auto"
  }
}
```

The numeric score values above are initial configuration examples, not production-validated thresholds. Final defaults must be established using representative Gerber/camera datasets.

Config loading must be versioned. Unknown fields should be ignored with a warning; invalid required fields must block execution.

---

## 9. Required models

At minimum, define:

- `GerberWorkflowConfig`
- `GerberSampleConfig`
- `AlignStitchConfig`
- `SampleManifest`
- `SampleTileInfo`
- `CapturedImageInfo`
- `SampleAlignmentResult`
- `StitchImagePose`
- `WorkflowProgress`
- `ProcessingReport`

`CapturedImageInfo` should retain source metadata from `ImageInfo` where applicable:

- file path
- group ID
- image ID
- position ID
- robot X and Y
- row, column and order index

---

## 10. Source reuse from StitchingImage

### Reuse or adapt

- `RobotManager/Domain.cs`
- `RobotManager/RobotArrange.cs`
- `RobotManager/TraversalGraph.cs`
- selected parts of `RobotManager/StitchingImage.cs`
- `Matcher/PairMatching.cs`
- matcher implementations
- `Utils/AlignmentRefinement.cs`
- `Utils/ImageRead.cs`
- `Utils/StitchingConfig.cs`
- `Utils/TiffBigWriter.cs`
- `DesignControls/PathCanvasControl.cs`
- concepts from `DesignControls/ConfigGridControl.cs`

### Do not migrate unless independently required

- `Connection/TcpConnectionManager.cs`
- protocol sample UI
- `MainForm.cs` and `MainForm.Designer.cs` from `StitchingImage`
- `RobotOrderer.cs` legacy path
- AH01 handshake logic
- unrelated dialogs and system configuration watchers

### Required corrections during migration

- Rename namespaces to the target namespace.
- Remove legacy and duplicate code paths.
- Use `Eval.IsMatch` as the match success condition.
- Standardize matrix direction and documentation.
- Replace hard-coded fallback offsets with values derived from expected image dimensions and overlap, or explicit config values.
- Dispose all `Bitmap`, `Mat`, `HObject`, `HTuple` and temporary images deterministically.

---

## 11. Platform and dependency requirements

### 11.1 Build target

Use `x64` for all projects that reference OpenCV native runtime or HALCON XL libraries.

Set:

```text
PlatformTarget = x64
Prefer32Bit = false
```

### 11.2 Mandatory HALCON startup validation

The solution must use one consistent HALCON installation/version and one reference source. Do not mix the DLL bundled by `EWindowControl` with a different DLL copied from `StitchingImage`.

Before `Application.Run(...)`, execute a blocking `HalconRuntimeValidator` that checks:

1. process architecture is x64
2. `halcondotnetxl.dll` and required native runtime DLLs can be resolved
3. managed and native HALCON versions are compatible
4. the installed HALCON version matches the application-supported major/minor version
5. a basic operator self-test succeeds
6. HALCON license is valid
7. NCC operators can be initialized

Recommended self-test operations include retrieving HALCON system version information and creating/disposing an empty iconic object. A small NCC smoke test may be added to installation diagnostics.

Failure policy is mandatory:

```text
HALCON validation failed
→ show blocking diagnostic dialog
→ include expected version, detected version/path, HALCON error code and remediation
→ write startup log
→ exit application
```

The current `StitchingImage` behavior that offers “continue with OpenCV” must **not** be migrated. `HalconLibraryManager.AllowHalcon = false` is not a valid production state for this application.

### 11.3 NuGet dependencies

At minimum, the target stitching core may require:

- `OpenCvSharp4`
- `OpenCvSharp4.runtime.win`
- `OpenCvSharp4.Extensions`
- `TiffLibrary`
- `JpegLibrary`, only if still required by the migrated image writer

Remove WPF-only packages unless a migrated class actually requires them.

---

## 12. Threading, cancellation and UI safety

All crop, alignment and stitching loops must run in `Task.Run` or an equivalent worker service.

Rules:

- Never read or write WinForms controls from the worker thread.
- Capture UI values before starting the task.
- Use `CancellationToken` for both workflows.
- Use `IProgress<T>` to update status, overlays and progress bars.
- Disable conflicting controls while running.
- Restore the UI state in `finally`.
- Prevent two runs from executing concurrently.

---

## 13. Logging and processing report

Each run must create a machine-readable report and a readable log.

For each image, record:

- sample tile ID
- captured image path
- row, column and order
- method attempted
- method selected
- success state
- score and `PairEval` values
- transform matrix
- translation and rotation
- pose source
- fallback reason
- processing time

Recommended output:

```text
processing_report.json
processing_log.txt
```

The UI should provide a command to open the output folder after completion.

---

## 14. Error handling

Block execution for:

- missing sample image
- missing or invalid manifest
- zero rows or columns
- invalid overlap
- tile count mismatch
- unreadable captured image
- unsupported image dimensions when strict validation is enabled
- no valid alignment anchors after automatic passes and manual review is cancelled or unavailable
- invalid transform matrix
- canvas size exceeding the configured safe limit
- output path not writable

The user-facing error must include the affected file or tile and a suggested corrective action.

---

## 15. Implementation phases

### Phase 1 – UI refactor without behavior change

- Add root TabControl.
- Move current viewer into `ReadGerberControl`.
- Confirm existing Gerber functions still work.

### Phase 2 – Sample model and crop generation

- Implement typed sample config.
- Implement crop geometry and traversal.
- Implement live crop overlay.
- Save tiles, manifest and overlay.

### Phase 3 – Stitching core migration

- Migrate arrangement, traversal, required matchers and image utilities.
- Normalize namespace and dependencies.
- Add tests for order and transform direction.

### Phase 4 – Sample-to-capture alignment

- Add modality-aware preprocessing.
- Implement HALCON NCC full-tile alignment.
- Implement OpenCvSharp Pyramid ECC full-tile alignment.
- Implement default NCC → Pyramid ECC hybrid mode.
- Generate absolute global poses.
- Add diagnostic result display and threshold reporting.

### Phase 5 – Failed alignment recovery

- Neighbor fallback using captured-to-captured matchers.
- Anchor adjustment and interpolation.
- Expected-grid fallback.
- Implement `ManualAlignmentDialog`.
- Pose-source reporting and exclusion behavior.

### Phase 6 – Absolute-pose stitching

- Add `StitchFromGlobalTransforms`.
- Add incremental preview.
- Add full-resolution TIFF/BigTIFF output.

### Phase 7 – Startup validation, benchmark and performance

- Mandatory HALCON startup/version/license tests.
- Cancellation and UI-thread tests.
- Memory and disposal tests for `Mat`, `Bitmap`, `HObject`, `HTuple` and NCC model handles.
- Large-image TIFF/BigTIFF tests.
- Real Gerber-binary versus camera-grayscale alignment benchmark.
- Calibrate NCC/ECC thresholds from production-like data.
- Regression test for Read Gerber.

---

## 16. Acceptance criteria

### Startup and dependencies

- Application does not open the main form when HALCON DLLs, native runtime, compatible version, NCC capability or license validation fails.
- Startup error dialog identifies the detected path/version and HALCON error code when available.
- The solution uses one HALCON DLL set and runs as x64.

### Read Gerber

- All existing functions work after being moved into the tab.
- Preview and export results are unchanged for the same input and settings.

### Create Gerber Sample

- Generates exactly `Rows × Columns` valid tiles.
- Every tile is inside source bounds.
- Crop overlap matches configured pixel or percent values within one-pixel rounding tolerance; default configuration is 60 px.
- Traversal order matches one of the four supported `StartOrder` presets and the selected `CropOrder`.
- Live overlay marks the currently processed tile without freezing the UI.
- Manifest and tile files are internally consistent.

### Align and Stitching

- Loaded images are natural-sorted deterministically, assigned sequential `OrderIndex`, and mapped one-to-one to sample tiles with the same index.
- Captured-to-sample mapping is visible and auditable.
- A match with `Eval.IsMatch == false` is never marked as successful.
- Successful sample alignments produce absolute poses tied to tile crop origins.
- Failed alignments follow the mandatory A→B→C→D recovery sequence.
- Every fallback is clearly marked in UI and report.
- Final stitched preview updates during processing.
- Full-resolution TIFF/BigTIFF output is independent of preview resolution.
- Cancellation releases all image resources and leaves no locked output files.

---

## 17. Out of scope for the initial implementation

Unless explicitly requested:

- AH01 TCP handshake and remote commands
- automatic camera acquisition
- editing Gerber vector content
- manual seam painting
- photometric calibration between camera frames
- GPU acceleration
- multi-board batch processing

---

## 18. Confirmed requirements and remaining clarification points

### 18.1 Confirmed

1. Sample input is an external PNG/BMP/TIFF raster.
2. Tile count and geometry derive from Rows, Columns and Overlap after configured source preprocessing.
3. Overlap supports pixels and percent; default/preferred value is `60 px`.
4. Supported start presets are `TopLeftRight`, `TopLeftDown`, `BottomRightLeft`, and `BottomRightUp`.
5. Captured image K maps to sample tile K by `OrderIndex`.
6. Sample is binary; captured image is grayscale with real illumination, background, noise and material texture.
7. Direct alignment uses OpenCvSharp Pyramid ECC and HALCON NCC with modality-aware preprocessing.
8. Recovery sequence is neighbor alignment → interpolation/adjustment → expected-grid pose → manual adjustment.
9. Final output is TIFF/BigTIFF.
10. HALCON is mandatory and validated before the main UI opens.

### 18.2 Three implementation details still requiring confirmation

1. **Meaning of “preprocess according to the supplied size”:** should `PreprocessWidth/Height` resize the entire source before calculating crop rectangles, or should each tile be cropped first and then resized to the expected camera-image dimensions? This spec currently assumes **preprocess the full source first**, because resizing tiles afterward requires an additional per-tile scale transform in the manifest.
2. **Manual fallback timing:** should the run pause immediately whenever Stage D is reached, or should all automatic processing finish first and then open a review queue containing unresolved images? A review queue is usually more efficient for batch processing.
3. **Final TIFF pixel format and blending:** should output remain 8-bit grayscale, become RGB, or preserve source bit depth; and should overlap use overwrite, average/feather blending, or seam optimization? These choices materially affect memory, BigTIFF size and visual continuity.

Until these points are confirmed, implementation should use these provisional defaults:

- preprocess the entire source before crop geometry
- collect unresolved images and open the manual review queue after automatic passes
- output 8-bit grayscale TIFF/BigTIFF with feather blending in overlap regions
