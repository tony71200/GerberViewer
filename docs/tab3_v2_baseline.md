# Tab 3 v2 Baseline — Scope and Risk Audit

Updated: 2026-07-22 00:22 UTC

## 1. Branch and working tree baseline

- Required branch from `AGENT.md` and the v2 prompt: `2026-07-21_use-agent.md-as-program-title`.
- Actual branch observed before changes: `work`.
- Working tree before changes: no modified files were reported by `git status --short`.
- This task is documentation/scope-only. Runtime production implementation was not changed.

## 2. Scope confirmation

Tab 3 — Align and Stitching is in scope for implementation. The active repository guidance in `AGENT.md` states that Tab 3 is the current priority and that any older documentation claiming Tab 3 is outside scope is superseded. The v2 prompt also states that Tab 3 is the main implementation target.

## 3. Required files reviewed

Production files reviewed for this baseline:

- `GerberViewer/Views/AlignStitchingControl.cs`
- `GerberViewer/Views/AlignStitchingControl.Designer.cs`
- `GerberViewer/Workflow/Models/WorkflowContext.cs`
- `GerberStitching.Core/Alignment/AlignStitchWorkflowService.cs`
- `GerberStitching.Core/Alignment/SampleAligners.cs`
- `GerberStitching.Core/Alignment/SampleAlignmentModels.cs`
- `GerberStitching.Core/Alignment/ModalityAwarePreprocessor.cs`
- `GerberStitching.Core/Alignment/NeighborMatchAcceptance.cs`
- `GerberStitching.Core/Arrangement/CapturedImageLoader.cs`
- `GerberStitching.Core/Models/SampleManifest.cs`
- `GerberStitching.Core/Models/WorkflowModels.cs`
- `GerberStitching.Core/Stitching/GlobalTransformStitcher.cs`
- `GerberStitching.Core/Imaging/PreparedSampleRun.cs`
- `GerberStitching.Core/Imaging/SampleTileGenerator.cs`
- `GerberViewer/Views/CreateGerberSampleControl.cs`
- `GerberViewer/Views/GerberSampleWindow.cs`
- `GerberViewer/Stitching/PathCanvasControl.cs`

Reference/documentation reviewed:

- `AGENT.md`
- `docs/Codex_Prompt_Fix_Tab3_implement_v2.md`
- `docs/Codex_Prompt_Fix_Tab3_implement.md`
- `docs/Tab3_Align_Stitching_Flow.md`
- `docs/tab3_implementation_baseline.md`
- `docs/tab3_fix_task_results.md`
- `docs/tab2_manifest_contract.md`
- `docs/tab2_image_ownership.md`
- `reference/StitchingImage/StitchingImage/Stitch_Tools/DesignControls/OffsetPreviewControl.cs`
- `reference/StitchingImage/StitchingImage/Stitch_Tools/DesignControls/OffsetPreviewControl.Designer.cs`

## 4. Current call graph

### 4.1 `AlignStitchingControl`

```text
AlignStitchingControl..ctor
  -> InitializeComponent
  -> InitializeLogger
  -> alignConfigGrid.SelectedObject = GerberViewer.Stitching.Models.AlignStitchConfig
  -> UpdateRunState

WorkflowContext.set
  -> unsubscribe old Changed only when old context is non-null
  -> assign new context only inside old-context branch
  -> subscribe new Changed only inside old-context branch
  Risk: first assignment from null does nothing.

btnSelectManifest_Click
  -> LoadManifest
    -> ClearManifestState
    -> SampleManifestSerializer.Read
    -> SampleManifestValidator.Validate
    -> RenderManifestInfo
    -> LoadCapturedImages
    -> UpdateRunState

btnOpenImageFolder_Click
  -> set captured folder
  -> LoadCapturedImages

LoadCapturedImages
  -> CapturedImageLoader.Load(folder, manifestPath)
  -> bind list box
  -> orderPathCanvas.SetCapturedImages
  -> UpdateRunState

btnRunAlignStitch_Click
  -> create .creating run directory
  -> new AlignStitchWorkflowService(null, null)
  -> RunAsync(config, manifest, captured, progress, token)
  -> update PathCanvas node states
  -> block publication if report.Succeeded is false
```

### 4.2 Direct alignment

```text
AlignStitchWorkflowService.RunAsync
  -> Task.Run(RunCore)
RunCore
  -> ValidateInputs
  -> ProcessingReport.Create
  -> for captured ordered by OrderIndex
     -> SolveDirect
        -> LoadBitmap(tile.ExpectedPath)
        -> LoadBitmap(cap.FilePath)
        -> SampleAlignmentContext
        -> ISampleAligner.Align (default NccThenPyramidEccSampleAligner)
           -> HalconNccSampleAligner.Align
              -> ModalityAwarePreprocessor.Preprocess
              -> GetOrCreateNccModel placeholder
              -> FindBestTranslation managed NCC scan
              -> BuildResult
           -> PyramidEccSampleAligner.Align
              -> ModalityAwarePreprocessor.Preprocess
              -> HalconNccSampleAligner.Ncc proxy score at initial transform
              -> BuildResult
              -> ValidateGeometry
        -> Translation(tile.ExpectedX, tile.ExpectedY) x CapturedToSampleTransform
        -> TileWorkflowState.From(... PoseSource.SampleAlignment ...)
```

### 4.3 Recovery

```text
RunCore
  -> if SolveDirect returns !HasValidPose
     -> Recover
        -> add warnings that neighbor recovery is unavailable
        -> add warnings that anchor interpolation is unavailable
        -> optional IManualAlignmentProvider.RequestManualAlignment
        -> accepted manual pose -> PoseSource.Manual
        -> skipped manual pose -> PoseSource.Excluded
        -> otherwise PoseSource.Failed
```

Current recovery does not run image-based neighbor matching.

### 4.4 Stitching

```text
GlobalTransformStitcher.StitchFromGlobalTransforms
  -> poses.Where(p => p.HasValidPose).ToDictionary(row:column)
  -> join captured images by row:column
  -> CalculateBounds using translation plus width/height only
  -> allocate full Bitmap canvas
  -> for each item
     -> new Bitmap(captured path)
     -> Graphics.TranslateTransform / RotateTransform / ScaleTransform
     -> DrawImageUnscaled
     -> optional MakePreview clone/resize
  -> canvas.Save(output, ImageFormat.Tiff)
```

Current stitcher is not invoked by `AlignStitchingControl.btnRunAlignStitch_Click` after alignment.

### 4.5 Comparison

```text
AlignStitchingControl.Designer
  -> resultTabControl
     -> tabComparison
        -> picComparison PictureBox Dock=Fill
```

No production comparison service/control is currently wired. `tabComparison` is a placeholder `PictureBox` and does not produce authoritative comparison outputs.

## 5. Placeholder or false-success paths still present

- `HalconNccSampleAligner` name implies HALCON NCC, but current implementation uses managed float-array NCC scanning and a width/height-only `NccModelHandle` placeholder, not `create_ncc_model/find_ncc_model/clear_ncc_model`.
- `PyramidEccSampleAligner` name implies ECC, but current implementation does not call `Cv2.FindTransformECC`; it reuses an NCC proxy score at the initial transform.
- No `IMatcher`, `MatchRequest`, `MatchResult`, `MatcherOptions`, `MatcherFactory`, `EccMatcher`, `PharseCorrMatcher`, or `NCC_HalconMatcher` production contract exists under `GerberStitching.Core/Matching/`.
- Direct alignment currently performs translation-only managed NCC and proxy ECC; rotation/scale are not genuinely estimated.
- Neighbor recovery is explicitly unavailable and returns failed/manual/excluded only; it does not run `Cv2.PhaseCorrelate` or pair ECC.
- `EnableNeighborRecovery`, `EnableAnchorInterpolation`, `AllowExpectedGridFallback`, and related config fields are exposed but are not implemented as production paths.
- `TileWorkflowState.HasValidPose` is still the single gate; canonical `AlignmentSucceeded`, `IsFallbackPose`, and `IsStitchable` are not present.
- Stitching maps by `Row:Column`, not canonical `OrderIndex`.
- Stitching bounds ignore transformed corners for rotation/scale.
- Stitching uses full-canvas `Bitmap` and `Graphics.DrawImageUnscaled`, not OpenCV/HALCON warp with valid masks/blending.
- `TiffMode.BigTiff` can be selected, but output still uses `Bitmap.Save(... ImageFormat.Tiff)`.
- `tabComparison` is only a `PictureBox`; no comparison metrics, coordinate-authoritative check, or saved comparison artifacts exist.
- `WorkflowContext` setter fails on first assignment when `_workflowContext` is null.

## 6. Duplicate models

- Duplicate `AlignStitchConfig` exists in:
  - `GerberStitching.Core/Models/WorkflowModels.cs` as the expanded Tab 3 production config.
  - `GerberViewer/Workflow/Models/WorkflowContext.cs` as a small UI workflow config with `InputManifestPath`, `OutputDirectory`, and `OverlapPercent`.
- Duplicate/ambiguous sample config models exist in:
  - `GerberStitching.Core/Configuration/GerberSampleConfig.cs`.
  - `GerberStitching.Core/Models/WorkflowModels.cs` (`GerberSampleConfig`).
  - `GerberViewer/Workflow/Models/WorkflowContext.cs` (`SampleGerberConfig`).

## 7. Resource ownership baseline

### 7.1 `Bitmap`

- `AlignStitchWorkflowService.SolveDirect` owns `new Bitmap(tile.ExpectedPath)` and `new Bitmap(cap.FilePath)` inside `using` scopes.
- `SampleAlignmentContext` borrows those `Bitmap` instances for the duration of `ISampleAligner.Align`.
- `ModalityAwarePreprocessor.Preprocess` borrows input `Bitmap`, creates float arrays, and can create diagnostic `Bitmap` instances; returned `PreprocessedAlignmentImages` owns diagnostics until transferred to `SampleAlignmentResult.DiagnosticImages`.
- `GlobalTransformStitcher` owns captured `Bitmap` instances in per-image `using` scopes and owns a full output `Bitmap` canvas in a `using` scope; preview `Bitmap` clones are handed to the progress consumer.
- `ManualAlignmentDialog` assigns `new Bitmap(...)` directly to `PictureBox.Image`; explicit disposal ownership is not documented in the current baseline.
- `GerberCanvas` stores a `Bitmap` reference and forwards it to `EWindowControl.SetSourceBitmap`; ownership needs verification before further edits.

### 7.2 `Mat`

- No current production Tab 3 path owns or disposes OpenCvSharp `Mat` instances.
- The required OpenCV matcher/image interop layer is not implemented yet.

### 7.3 `HObject`

- Tab 2 preparation owns HALCON `HObject` source/processed images in `PreparedSampleRun`, which implements disposal.
- `SampleTileGenerator` crops per-tile `HObject` values and disposes them within method scope.
- `CreateGerberSampleControl` owns `_sampleSourceImage` and replaces/disposes old images via `ReplaceSampleImage`.
- `GerberSampleWindow` copies source `HObject` for display and owns overlay regions in `_overlayRegions` until cleared/disposed.
- Current Tab 3 alignment/stitching does not use HALCON `HObject` for matcher input.

### 7.4 `HTuple`

- `PreparedSampleRun.GetSize` creates width/height `HTuple` values for HALCON size queries and disposes them in method scope.
- `CreateGerberSampleControl.ReplaceSampleImage` creates width/height `HTuple` values for display sizing and disposes them in method scope.
- Current Tab 3 matcher path does not own HALCON `HTuple` transform/model tuples.

### 7.5 HALCON model handles

- No real HALCON NCC model handle is currently owned.
- `HalconNccSampleAligner.NccModelHandle` is a managed placeholder storing width/height only; it is kept in a dictionary and cleared on dispose.
- Required lifecycle `create_ncc_model -> find_ncc_model -> clear_ncc_model` is not implemented.

## 8. Baseline build result

Command attempted:

```text
msbuild GerberViewer.sln /p:Configuration=Debug /p:Platform=x64 /m
```

Result: not run to compilation. The environment is Linux and `msbuild` is not installed (`/bin/bash: line 1: msbuild: command not found`). No Debug x64 compile result can be claimed from this container.

## 9. Runtime tests not executed

- WinForms smoke test not executed: container has no Windows desktop UI session.
- HALCON runtime smoke test not executed: no verified HALCON 25.05 runtime/license binding was available through the attempted build path.
- Real-data Tab 3 end-to-end test not executed: no dataset was selected/provided in this non-interactive task.

## 10. Next-task risks

- Fix `WorkflowContext` setter before relying on Tab-to-Tab state.
- Consolidate duplicate `AlignStitchConfig` before adding more properties.
- Introduce canonical matcher contracts before replacing managed NCC/proxy ECC.
- Establish explicit image interop ownership for `Bitmap`, `Mat`, `HObject`, `HTuple`, and HALCON model handles.
- Replace `HasValidPose`-only gating with `AlignmentSucceeded`, `IsFallbackPose`, and `IsStitchable`.
- Implement comparison service/control before claiming `tabComparison` complete.
