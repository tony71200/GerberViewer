# Tab 3 Implementation Baseline

Updated: 2026-07-21 UTC

Scope: Task 0 from `docs/Codex_Prompt_Fix_Tab3_implement.md`. This document records the current Tab 3 state before continuing with the next ordered implementation task. No runtime behavior is changed by this baseline.

## Current UI call graph

```text
MainForm.Designer
  -> tabAlignStitching
  -> AlignStitchingControl
     -> InitializeComponent
     -> InitializeLogger
     -> alignConfigGrid.SelectedObject = _config
     -> btnSelectManifest_Click
        -> LoadManifest
           -> SampleManifestSerializer.Read
           -> SampleManifestValidator.Validate
           -> RenderManifestInfo
           -> LoadCapturedImages
     -> btnOpenImageFolder_Click
        -> LoadCapturedImages
           -> CapturedImageLoader.Load
           -> orderPathCanvas.SetCapturedImages
     -> btnSelectOutputFolder_Click
        -> UpdateRunState
     -> btnRunAlignStitch_Click
        -> create <OutputRoot>/AlignStitch_<runId>/.creating
        -> AlignStitchWorkflowService.RunAsync
        -> Progress<WorkflowProgress>
        -> orderPathCanvas.SetCapturedImages
     -> btnCancelAlignStitch_Click
        -> CancellationTokenSource.Cancel
```

UI risks observed:

- `AlignStitchingControl.Designer.cs` contains many controls on compressed single lines, which increases designer merge/formatting risk.
- `picStitchedImage` and `picComparison` are `PictureBox` placeholders; full-resolution preview limits are not implemented there.
- `ManualAlignmentDialog` loads sample/captured images into `PictureBox` with `new Bitmap(...)` and does not dispose the previous displayed images before replacement.

## Current alignment call graph

```text
AlignStitchWorkflowService.RunAsync
  -> Task.Run(RunCore)
     -> ValidateInputs
        -> SampleManifestValidator.Validate(manifest, requireFiles: true)
        -> captured count and file existence checks
     -> tileByOrder = manifest.Tiles.ToDictionary(OrderIndex)
     -> for each captured ordered by OrderIndex
        -> SolveDirect
           -> LoadBitmap(sample tile ExpectedPath)
           -> LoadBitmap(captured FilePath)
           -> SampleAlignmentContext
           -> ISampleAligner.Align
              -> NccThenPyramidEccSampleAligner.Align
                 -> HalconNccSampleAligner.Align
                    -> ModalityAwarePreprocessor.Preprocess
                    -> GetOrCreateNccModel
                    -> FindBestTranslation
                    -> BuildResult
                 -> PyramidEccSampleAligner.Align
                    -> ModalityAwarePreprocessor.Preprocess
                    -> HalconNccSampleAligner.Ncc as eccProxyScore
                    -> ValidateGeometry
           -> Translation(ExpectedX, ExpectedY) x CapturedToSampleTransform
        -> Recover when direct alignment fails
```

Alignment gaps observed:

- HALCON NCC is not a real HALCON NCC model/search implementation yet; `NccModelHandle` only records sample dimensions.
- Pyramid ECC is not a real OpenCvSharp ECC implementation yet; it still uses an NCC-style proxy score.
- Preprocessing is still based on `Bitmap`, `float[,]`, `GetPixel`, and `SetPixel` paths.

## Current recovery call graph

```text
SolveDirect failed
  -> Recover
     -> log neighbor recovery unavailable
     -> log anchor interpolation unavailable
     -> optional IManualAlignmentProvider.RequestManualAlignment
        -> ManualAlignmentDialog.RequestManualAlignment, when provider is supplied
     -> Failed / Excluded / Manual
```

Recovery gaps observed:

- Captured-to-captured neighbor matching is blocked, not implemented.
- Anchor interpolation is blocked, not implemented.
- Expected-grid fallback is blocked in workflow success, not implemented as a confirmed/manual fallback.
- Manual alignment dialog exists but is not wired as the default provider in `AlignStitchingControl`.

## Current stitching call graph

```text
GlobalTransformStitcher.StitchFromGlobalTransforms
  -> NormalizeTiffPath
  -> poses.Where(HasValidPose).ToDictionary(Row:Column)
  -> calculate min/max bounds from X/Y and image Width/Height
  -> create full Bitmap canvas
  -> Graphics.DrawImageUnscaled per captured image
  -> optional preview clone/resize
  -> canvas.Save(output, ImageFormat.Tiff)
```

Stitching gaps observed:

- Current stitcher uses row/column pose keys, not `OrderIndex`.
- It places images by translation only and does not warp rotation/scale/homography.
- It creates a full in-memory `Bitmap` canvas, which is unsafe for huge mosaics.
- BigTIFF selection is calculated but output is still saved through `Bitmap.Save`, not a streaming BigTIFF writer.
- `AlignStitchingControl` does not call `GlobalTransformStitcher` yet after workflow success.

## Fake/stub methods and blocked placeholders

| File | Method/Pattern | Baseline status |
|---|---|---|
| `GerberStitching.Core/Alignment/SampleAligners.cs` | `NccModelHandle` | Explicit placeholder only; not a HALCON model handle. |
| `GerberStitching.Core/Alignment/SampleAligners.cs` | `FindBestTranslation(float[,], float[,])` | Managed brute-force NCC-like translation search; not HALCON NCC. |
| `GerberStitching.Core/Alignment/SampleAligners.cs` | `PyramidEccSampleAligner.Align` | Uses `eccProxyScore`; not OpenCvSharp ECC. |
| `GerberStitching.Core/Alignment/AlignStitchWorkflowService.cs` | `Recover` | Blocks neighbor/interpolation with warnings; no real recovery implementation. |
| `GerberStitching.Core/Stitching/GlobalTransformStitcher.cs` | `StitchFromGlobalTransforms` | Translation-only bitmap compositor; not production homography stitching. |
| `GerberStitching.Core/Utils/TiffBigWriter.cs` | `SaveTiffGray8Async`, `SaveTiffRgb24Async` | Uses `Bitmap.SetPixel` and `Bitmap.Save`; not streaming BigTIFF. |
| `GerberViewer/Views/ManualAlignmentDialog.cs` | Manual overlay | Basic dual PictureBox dialog; not a complete overlay/difference confirmation tool. |

## Transform directions

Current canonical alignment direction recorded in code:

```text
CapturedToSampleTransform:
CapturedImageLocalPixels -> SampleTileLocalPixels

GlobalPose:
CapturedImageLocalPixels -> ProcessedSampleGlobalPixels

Global composition:
Translation(SampleTileInfo.ExpectedX, SampleTileInfo.ExpectedY) x CapturedToSampleTransform
```

Potential transform risks:

- No dedicated immutable `Transform2D` wrapper exists yet.
- No adapter-boundary tests prove HALCON/OpenCV transform direction.
- `GlobalTransformStitcher` currently consumes only translation values from `TileWorkflowState.GlobalPose` and ignores rotation/scale/homography.

## Image ownership baseline

| Resource | Current owner | Created by | Disposal / risk |
|---|---|---|---|
| Manifest object | `AlignStitchingControl` / workflow service | `SampleManifestSerializer.Read` | Managed object; no disposal needed. |
| Captured-image metadata | `AlignStitchingControl` | `CapturedImageLoader.Load` | Managed list; cleared on load failures. |
| Sample/captured bitmaps in direct alignment | `AlignStitchWorkflowService.SolveDirect` | `new Bitmap(path)` through `LoadBitmap` | Disposed by `using`; OK for method scope, but not HALCON canonical. |
| Preprocessor diagnostics | `PreprocessedAlignmentImages` | `ModalityAwarePreprocessor.ToBitmap` | Disposed by `PreprocessedAlignmentImages.Dispose` unless transferred to result dictionary. |
| Manual dialog preview images | `ManualAlignmentDialog` | `new Bitmap(path)` | Risk: assigned to PictureBox without explicit previous-image disposal. |
| Stitch canvas | `GlobalTransformStitcher` | `new Bitmap(width,height,Format32bppArgb)` | Disposed by `using`; high memory risk. |
| Stitch preview | `GlobalTransformStitcher.MakePreview` | `Clone` or resized `Bitmap` | Ownership passed to progress consumer; disposal contract unclear. |
| Tab 3 logger | `AlignStitchingControl` | `new Elog_1_0.Elog()` | Disposed in control `Dispose`; verify designer override remains valid after future edits. |
| Cancellation token source | `AlignStitchingControl` | `new CancellationTokenSource()` | Disposed in `finally` and control dispose. |

## Bitmap allocations and pixel-loop hotspots

Known Tab 3 / stitching-related allocations and hotspots:

| File | Baseline allocation / hotspot |
|---|---|
| `GerberViewer/Views/ManualAlignmentDialog.cs` | `_sample`/`_captured` PictureBoxes and `new Bitmap` for sample/captured preview. |
| `GerberViewer/Views/AlignStitchingControl.Designer.cs` | `picStitchedImage` and `picComparison` PictureBoxes for bounded placeholders. |
| `GerberStitching.Core/Arrangement/CapturedImageLoader.cs` | `new Bitmap(item.FilePath)` for image readability and dimensions. |
| `GerberStitching.Core/Alignment/AlignStitchWorkflowService.cs` | `LoadBitmap` uses `new Bitmap(path)` for sample and captured images. |
| `GerberStitching.Core/Alignment/ModalityAwarePreprocessor.cs` | `new Bitmap`, `GetPixel`, diagnostic `SetPixel`. |
| `GerberStitching.Core/Stitching/GlobalTransformStitcher.cs` | full canvas `new Bitmap`, per-image `new Bitmap`, preview clone/resize. |
| `GerberStitching.Core/Utils/TiffBigWriter.cs` | `new Bitmap`, nested `SetPixel`, `Bitmap.Save`. |
| `GerberStitching.Core/Utils/ImageRead.cs` | `new Bitmap(filename)` and format normalization bitmap. |

## UI-thread violations and responsiveness risks

- `Application.DoEvents()` no longer appears in the production Tab 3 files after the previous patch, based on repository search.
- `btnRunAlignStitch_Click` starts `RunAsync`, which wraps `RunCore` in `Task.Run`; UI updates are routed through `Progress<WorkflowProgress>`.
- `ManualAlignmentDialog.RequestManualAlignment` is synchronous and shows a modal dialog; provider dispatch and UI-thread ownership still need explicit handling before production use.
- `CapturedImageLoader.Load` performs `Bitmap` readability checks synchronously from the UI call path when `LoadCapturedImages` is called.
- Manifest reading/validation and derived-info rendering are synchronous in `LoadManifest`.
- `GlobalTransformStitcher.StitchFromGlobalTransforms` is synchronous and must be called from a worker if wired later.

## Current build status

Commands attempted on 2026-07-21 UTC:

```bash
msbuild GerberViewer.sln /p:Configuration=Debug /p:Platform=x64 /t:Build
msbuild GerberViewer.sln /p:Configuration=Release /p:Platform=x64 /t:Build
```

Both commands failed in this container before project evaluation because `msbuild` is not installed:

```text
/bin/bash: line 1: msbuild: command not found
```

Therefore Debug x64 and Release x64 build status is: not verified in this environment.

## Current reference files used/read for this baseline

Production and docs inspected through shell/ripgrep during baseline creation:

- `AGENT.md`
- `docs/Codex_Prompt_Fix_Tab3_implement.md`
- `GerberViewer/Views/AlignStitchingControl.cs`
- `GerberViewer/Views/AlignStitchingControl.Designer.cs`
- `GerberViewer/Views/ManualAlignmentDialog.cs`
- `GerberViewer/Stitching/PathCanvasControl.cs`
- `GerberViewer/Workflow/Models/WorkflowContext.cs`
- `GerberStitching.Core/Alignment/AlignStitchWorkflowService.cs`
- `GerberStitching.Core/Alignment/SampleAligners.cs`
- `GerberStitching.Core/Alignment/ModalityAwarePreprocessor.cs`
- `GerberStitching.Core/Arrangement/CapturedImageLoader.cs`
- `GerberStitching.Core/Stitching/GlobalTransformStitcher.cs`
- `GerberStitching.Core/Utils/TiffBigWriter.cs`
- `GerberStitching.Core/Utils/ImageRead.cs`
- `docs/Tab3_Align_Stitching_Flow.md`
- `GerberView_Align_Stitching_Spec_v0.2.md`

Reference-source policy:

- No files under `reference/` were edited.
- Reference files remain read-only for future implementation tasks.

## Task 0 result

Task 0 is now complete as a documentation-only baseline audit. Runtime behavior has not been intentionally changed by this task.
