# Codex Prompt — Fix and Complete Tab 2 Implementation

## 1. Role

You are modifying a C# 7.3 WinForms solution targeting .NET Framework 4.8 with HALCON and OpenCvSharp4.

Repository:

```text
GerberViewer
```

Branch:

```text
2026-07-21_review-documentation-for-current-flow
```

Read these sources before editing:

```text
AGENT.md
GerberViewer/Views/CreateGerberSampleControl.cs
GerberViewer/Views/CreateGerberSampleControl.Designer.cs
GerberViewer/Views/GerberSampleWindow.cs
GerberViewer/Workflow/Models/WorkflowContext.cs
GerberStitching.Core/Configuration
GerberStitching.Core/Imaging
GerberStitching.Core/RobotManager
GerberStitching.Core/Arrangement
GerberStitching.Core/Validation
docs/
```

Also inspect existing project references for HALCON, OpenCvSharp4, and System.Drawing.

`AGENT.md` is mandatory and overrides any shortcut that conflicts with it.

---

## 2. Scope

Focus only on:

```text
Tab 2 — Create Gerber Sample
```

Do not implement Tab 3 alignment or stitching.

Tab 3 may be touched only when required to compile against a corrected shared manifest model. Do not alter Tab 3 runtime behavior.

Preserve Tab 1 behavior.

Do not modify the `EWindowControl` project. Extend it through:

```text
GerberViewer/Views/GerberSampleWindow.cs
```

Do not delete external, migrated, legacy, reference, sample, ZIP, or third-party source files.

---

## 3. Main objective

Replace the current split and inconsistent Tab 2 pipeline with one canonical, thread-safe pipeline:

```text
Open external image
→ decode once to owned HObject
→ preprocess once to owned processed HObject
→ calculate one physical grid
→ calculate one traversal/order mapping
→ create one PreparedSampleRun
→ preview and generator consume the same run
→ crop tiles from the same processed HObject
→ save and verify tiles
→ publish one canonical sample_manifest.json
```

The implementation must prevent:

- preview rectangles differing from generated tile rectangles
- preview `OrderIndex` differing from generated filenames/order
- reopening a large TIFF through `Bitmap`
- UI freezes
- partial manifests being published
- accidental recursive deletion of the selected output folder
- loss of zoom/pan behavior in `GerberSampleWindow`

---

## 4. Non-negotiable constraints

### 4.1 `EWindowControl` is read-only

Do not edit files under:

```text
EWindowControl/
```

Any Tab 2-specific behavior must be implemented in:

```text
GerberSampleWindow
```

using inheritance, composition, or adapter services.

### 4.2 HALCON `HObject` is canonical

Tab 2 must use `HObject` as the canonical image type for:

- original sample
- processed sample
- preview
- crop source
- generated tile source

Do not convert the full large image to `Bitmap` for normal processing.

### 4.3 No repeated image read

The same source file must not be decoded independently by preview and generator.

Remove this class of flow:

```text
HALCON ReadImage for preview
→ ImageRead.ReadBitmap for generator
```

Generator must accept an already prepared source/run object.

### 4.4 All expensive image work is off the UI thread

Perform these operations in a worker:

- image decoding
- image-size/channel validation
- preprocessing
- HALCON/OpenCV/Bitmap conversion
- grid/traversal calculation
- crop
- encode/write
- file readback verification
- manifest write/readback validation

Use:

```text
Task.Run
CancellationToken
IProgress<T>
```

Do not use:

```text
Application.DoEvents()
Thread.Sleep()
Control.Invoke inside deep processing services
Task.Result on UI thread
```

### 4.5 No destructive cleanup without warning

Never call:

```csharp
Directory.Delete(config.OutputDirectory, true);
```

on a path selected by the user.

If existing data may be overwritten or removed:

1. detect it before starting
2. show the exact path
3. explain the impact
4. require explicit confirmation
5. default to cancel

Cleanup may recursively delete only a run-specific temporary directory created by this program.

---

# 5. Required architecture

## 5.1 Add a canonical sample session/run model

Create a disposable model similar to:

```csharp
public sealed class PreparedSampleRun : IDisposable
{
    public HObject SourceImage { get; }
    public HObject ProcessedImage { get; }
    public int SourceWidth { get; }
    public int SourceHeight { get; }
    public int ProcessedWidth { get; }
    public int ProcessedHeight { get; }
    public GerberSampleConfig ConfigSnapshot { get; }
    public SampleGridLayout Layout { get; }
    public IReadOnlyList<SampleTileLayout> TilesByOrder { get; }
    public ImagePreprocessMetadata PreprocessMetadata { get; }
}
```

Requirements:

- constructor arguments must be validated
- ownership must be explicit
- dispose owned HALCON images exactly once
- do not expose mutable collections
- do not read WinForms controls
- use a config snapshot captured on the UI thread

The exact class name may differ, but the responsibility must remain.

## 5.2 Add a preparation service

Recommended API:

```csharp
public interface ISamplePreparationService
{
    PreparedSampleRun Prepare(
        HObject sourceImage,
        GerberSampleConfig config,
        CancellationToken cancellationToken);
}
```

Responsibilities:

1. validate source image
2. clone/copy source according to ownership policy
3. preprocess once
4. get processed dimensions from HALCON
5. calculate physical tile rectangles
6. calculate traversal/order exactly once
7. return one prepared run used by both preview and generator

Do not preprocess again inside the generator.

---

# 6. Mandatory image interop service

Create a centralized conversion layer for:

```text
HObject
Bitmap
OpenCvSharp.Mat
```

Recommended location:

```text
GerberStitching.Core/Imaging/ImageInterop/
```

Recommended API:

```csharp
public interface IImageInteropService
{
    Bitmap ToBitmapCopy(HObject source);
    HObject ToHObjectCopy(Bitmap source);
    Mat ToMatCopy(HObject source);
    HObject ToHObjectCopy(Mat source);
    Mat ToMatCopy(Bitmap source);
    Bitmap ToBitmapCopy(Mat source);
}
```

Implementation requirements:

- C# 7.3 compatible
- support byte grayscale
- support 3-channel color
- preserve dimensions
- state channel order explicitly
- convert RGB ↔ BGR deliberately
- reject unsupported channel counts or pixel types
- never return an object backed by temporary/disposed memory
- every returned object is owned by the caller
- every temporary `Bitmap`, `Mat`, `HObject`, `HImage`, stream, or pinned buffer is disposed/released
- conversion tests must compare dimensions, channels, and representative pixel values

Do not place conversion code inside button event handlers.

Use conversion only when a downstream library requires it. The canonical Tab 2 processing path remains `HObject`.

---

# 7. Fix the manifest contract

## 7.1 Root cause

The current Tab 2 writer and Tab 3 reader use incompatible JSON shapes.

Tab 2 currently writes fields similar to:

```text
tiles[].file
tiles[].x
tiles[].y
tiles[].orderIndex
```

The shared reader expects fields similar to:

```text
RootDirectory
tiles[].ExpectedPath
tiles[].ExpectedX
tiles[].ExpectedY
```

Fix this by creating exactly one shared model.

## 7.2 Canonical contract

Create a shared contract similar to:

```csharp
[DataContract]
public sealed class SampleManifest
{
    [DataMember(Order = 1)]
    public int ManifestVersion { get; set; }

    [DataMember(Order = 2)]
    public string RootDirectory { get; set; }

    [DataMember(Order = 3)]
    public string SourceRasterPath { get; set; }

    [DataMember(Order = 4)]
    public int SourceWidth { get; set; }

    [DataMember(Order = 5)]
    public int SourceHeight { get; set; }

    [DataMember(Order = 6)]
    public int ProcessedWidth { get; set; }

    [DataMember(Order = 7)]
    public int ProcessedHeight { get; set; }

    [DataMember(Order = 8)]
    public string CropOrder { get; set; }

    [DataMember(Order = 9)]
    public string StartOrder { get; set; }

    [DataMember(Order = 10)]
    public List<SampleTileInfo> Tiles { get; set; }
}

[DataContract]
public sealed class SampleTileInfo
{
    [DataMember(Order = 1)]
    public int OrderIndex { get; set; }

    [DataMember(Order = 2)]
    public int Row { get; set; }

    [DataMember(Order = 3)]
    public int Column { get; set; }

    [DataMember(Order = 4)]
    public string ExpectedPath { get; set; }

    [DataMember(Order = 5)]
    public int ExpectedX { get; set; }

    [DataMember(Order = 6)]
    public int ExpectedY { get; set; }

    [DataMember(Order = 7)]
    public int Width { get; set; }

    [DataMember(Order = 8)]
    public int Height { get; set; }
}
```

Add required preprocessing metadata or geometry fields when needed, but do not create another competing DTO.

Rules:

- `ManifestVersion` starts at `1`
- `OrderIndex` is mandatory and unique
- `ExpectedX` and `ExpectedY` are in processed-source pixels
- `ExpectedPath` uses one documented deterministic policy
- manifest contains exactly `Rows × Columns` tiles
- sort serialized tiles by `OrderIndex`
- deserialize and validate after writing
- do not publish manifest on cancel or any tile failure
- update shared readers and validators only as needed to compile against this contract
- do not implement Tab 3 behavior

## 7.3 Manifest validator

Add validation that reports all errors, including:

- null manifest
- unsupported version
- missing root directory
- empty tile list
- duplicate `OrderIndex`
- duplicate `(Row, Column)`
- missing order indices
- negative coordinates
- non-positive width or height
- tile outside processed image
- missing path
- missing or unreadable file after generation
- tile count mismatch
- inconsistent processed dimensions

---

# 8. Fix preview and generated-tile mismatch

## 8.1 Root causes to remove

Current code can calculate preview using the displayed source dimensions, while the generator:

1. reads the image again
2. preprocesses it
3. calculates crop rectangles from processed dimensions

The current code also has two order implementations:

```text
SampleGeometryCalculator
RobotArrange / TraversalGraph
```

This can produce different `OrderIndex` values, particularly for:

- `Branch`
- `BranchDown`
- vertical traversal
- `TopLeftDown`
- `BottomRightUp`

## 8.2 Required fix

Create one layout pipeline:

```text
Processed image dimensions
→ physical row/column rectangles
→ traversal service
→ assign one OrderIndex
→ immutable SampleGridLayout
```

Both preview and generator must use the same `SampleGridLayout` instance from `PreparedSampleRun`.

Do not recalculate layout inside `SampleTileGenerator`.

## 8.3 Physical geometry rules

The physical matrix must be created before traversal.

Every tile must contain:

```text
Row
Column
OrderIndex
X
Y
Width
Height
Rectangle
Predecessor
Successor
```

At minimum, assert:

- tile count equals `Rows × Columns`
- every `(Row, Column)` is unique
- every `OrderIndex` is unique
- order indices form a contiguous sequence
- all rectangles remain within processed image bounds
- preview rectangle equals generator crop rectangle
- manifest rectangle equals generator crop rectangle
- filenames use the same row/column/order values
- changing `StartOrder` changes order only, not physical rectangles
- changing `CropOrder` does not alter physical rectangles

## 8.4 Consolidate ordering

Select one authoritative traversal implementation.

Preferred solution:

- retain reusable `RobotArrange/TraversalGraph` domain logic
- expose it through one `ISampleTraversalService`
- make `SampleGeometryCalculator` responsible only for physical rectangles, or make it delegate ordering to the traversal service
- remove duplicate order assignment

Do not delete the old external/reference implementation. Deprecate or stop calling duplicate repository-owned code only after references are documented.

---

# 9. Remove Bitmap reread from generator

Refactor `SampleTileGenerator` so it does not accept only a source file path and reopen the file.

Recommended API:

```csharp
public Task<SampleGenerationResult> GenerateAsync(
    PreparedSampleRun preparedRun,
    string outputRoot,
    IProgress<SampleCropProgress> progress,
    CancellationToken cancellationToken);
```

or an equivalent API with explicit ownership.

Generator requirements:

- consume `PreparedSampleRun.ProcessedImage`
- consume `PreparedSampleRun.Layout`
- crop using HALCON image coordinates
- save each tile using a supported writer
- convert an individual tile only when required by the selected output encoder
- do not convert the full processed source to `Bitmap`
- do not recalculate preprocessing
- do not recalculate traversal
- verify each file after save
- mark Completed only after verification
- write the manifest only after all tiles succeed

For TIFF/BigTIFF input, no full-image `Bitmap` constructor may be called.

---

# 10. Safe output transaction and user warning

## 10.1 UI preflight

Before starting generation, perform a UI-thread preflight:

- resolve absolute output root
- reject source file directory when unsafe
- inspect whether target run/final directory exists
- inspect whether files would be overwritten
- show an explicit confirmation dialog when destructive replacement is possible
- display the exact path
- default the dialog to Cancel

## 10.2 Run directory

Use:

```text
<OutputRoot>/.creating_<runId>/
```

The run directory must include an ownership marker, for example:

```text
.gerber_sample_run
```

Only a directory containing the current run marker may be recursively cleaned automatically.

## 10.3 Final publication

Preferred flow:

```text
create temp run folder
→ generate and verify all tiles
→ write config snapshot
→ write canonical manifest
→ read and validate manifest
→ write overlay preview
→ move temp folder to final run folder
→ update WorkflowContext
```

Never delete or clear the selected output root.

If the final destination already exists, require an explicit strategy:

- create a new unique run folder
- replace an application-owned prior run after confirmation
- cancel

Do not silently merge stale files into a new manifest.

---

# 11. Threading and UI state

## 11.1 Open Sample

`btnOpenSample_Click` must:

1. show file dialog on UI thread
2. validate selected path
3. disable conflicting controls
4. start decode/preparation in background
5. report status through `Progress<T>` or returned result
6. replace the owned sample run on UI thread
7. set the image in `GerberSampleWindow`
8. render overlay from the same layout
9. fit image when appropriate
10. restore controls in `finally`

Opening a large TIFF must not freeze the form.

## 11.2 Refresh Preview

Refresh must:

1. read and validate config on UI thread
2. capture an immutable config snapshot
3. reuse the already decoded source `HObject`
4. run preprocessing/layout in background
5. atomically replace the prior `PreparedSampleRun`
6. redraw overlay on UI thread

Do not reopen the source file merely because Rows, Columns, overlap, or order changed.

Optimization is allowed:

- if only traversal fields changed, reuse the processed image and recalculate only ordering
- if only overlay style changed, reuse image and layout

Do not implement the optimization at the cost of correctness.

## 11.3 Create Sample

Create must:

- prevent double-run
- use one `CancellationTokenSource`
- disable Open/Load/Save/Refresh/Create
- enable Cancel
- process in background
- update each tile state through `IProgress<SampleCropProgress>`
- restore UI state in `finally`
- retain completed green tiles after success
- retain accurate failed state after failure
- leave unprocessed tiles pending after cancellation

---

# 12. Restore zoom, pan, and fit in `GerberSampleWindow`

Do not edit `EWindowControl`.

Update only:

```text
GerberViewer/Views/GerberSampleWindow.cs
```

Requirements:

- enable inherited mouse-wheel zoom
- preserve inherited drag/pan
- preserve inherited fit-to-view
- optionally enable inherited double-click fit
- do not swallow base mouse events
- do not add a transparent overlay WinForms control that captures mouse input
- render grid in image coordinates
- grid must remain aligned while zooming and panning
- replacing source image must not permanently disable `HMoveContent` or wheel zoom
- overlay refresh must not reset zoom unless the user requests Fit or a new source is loaded
- loading a new image may Fit once
- changing tile state must redraw overlay without forcing Fit

Use existing public base APIs such as the applicable:

```text
EnableMouseWheelZoom
EnableDoubleClickZoom
WinOperate
FitImage
EWinldowShowChanged
SourceHobject
SetShowImage
```

Choose the safe subset supported by the current code.

Do not use reflection to reach private `HWindow` unless the user explicitly approves it.

Add manual tests:

- wheel zoom in/out at cursor
- drag/pan after zoom
- Fit
- refresh preview without losing current zoom
- tile state update without changing camera
- replace source image and zoom again
- overlay remains aligned at high zoom

---

# 13. Task plan

Complete tasks in order. Build and test after each task.

## Task 0 — Baseline and protection audit

Files:

- `AGENT.md`
- current Tab 2 source
- current manifest writer/reader
- current image readers
- current traversal services

Output:

```text
docs/tab2_implementation_baseline.md
```

Record:

- current decode call graph
- current preprocess call graph
- current layout/order call graph
- every full-image Bitmap conversion
- every recursive directory delete
- manifest DTOs and writers/readers
- current `HObject` ownership
- current UI-thread violations
- baseline build results

Do not change behavior in this task.

## Task 1 — Canonical manifest contract

Implement:

- one shared `SampleManifest`
- one shared `SampleTileInfo`
- version field
- validator
- serializer/readback validation
- tests

Update Tab 2 writer.

Only minimally update shared readers needed to compile. Do not implement Tab 3.

## Task 2 — Image interop layer

Implement centralized conversions among:

```text
HObject
Bitmap
Mat
```

Add ownership and pixel tests.

Do not route normal full-size Tab 2 TIFF processing through Bitmap.

## Task 3 — Canonical source ownership

Refactor Tab 2 so selected sample is decoded once into owned `HObject`.

Create a source/session holder with deterministic disposal.

Test:

- PNG
- BMP
- TIFF grayscale
- TIFF color
- large TIFF when available
- replace source 20 times
- close/dispose control
- no file lock
- no invalid/disposed source in viewer

## Task 4 — Single preprocessing service

Move all preprocessing into one service that consumes canonical `HObject` and returns owned processed `HObject` plus metadata.

Test each mode:

- None
- Resize
- FitPad
- CenterCrop
- invert on/off
- keep aspect ratio
- invalid target size
- cancellation

## Task 5 — Single geometry and traversal pipeline

Create physical rectangles once and apply traversal once.

Eliminate conflicting `OrderIndex` assignment.

Test matrix:

```text
1×1
2×2
4×4
8×8
```

Orders:

```text
Zigzag
Branch
BranchDown
```

Start orders:

```text
TopLeftRight
TopLeftDown
BottomRightLeft
BottomRightUp
```

Assert preview/generator/manifest equality for every tile.

## Task 6 — PreparedSampleRun

Create the immutable/disposable prepared run.

Wire preview to use:

```text
ProcessedImage
Layout
TilesByOrder
```

Do not generate tiles yet.

Test refresh behavior and disposal.

## Task 7 — GerberSampleWindow interaction recovery

Restore inherited zoom, pan, fit, and overlay alignment without editing `EWindowControl`.

Complete manual interaction tests.

## Task 8 — Generator consumes prepared run

Remove file reread and independent preprocessing/layout from generator.

Crop from prepared HALCON image.

Verify output tile after save.

Test no full-size Bitmap is allocated for large TIFF source.

## Task 9 — Safe output transaction

Remove recursive deletion of user-selected output root.

Implement preflight warning, run folder, marker, temp publication, and failure cleanup.

Test:

- empty root
- non-empty root
- existing final run
- user cancels warning
- failure at tile K
- cancellation at tile K
- unwritable directory
- previous successful run remains intact

## Task 10 — Async UI integration

Make Open, Refresh, and Create responsive and cancellable.

Remove any `Application.DoEvents()` in Tab 2 flow.

Test double-click, cancellation, exception, and control restoration.

## Task 11 — Manifest publication and workflow context

Publish manifest only after all tile files and JSON validation pass.

Update:

```text
WorkflowContext.ManifestPath
WorkflowContext.OutputDirectory
WorkflowContext.SampleConfig
```

only on complete success.

## Task 12 — Integration and regression test

Run complete Tab 2 scenarios:

### Scenario A — Open and preview

```text
Open large TIFF
→ UI remains responsive
→ image displayed
→ red grid and OrderIndex displayed
→ wheel zoom works
→ pan works
→ Fit works
```

### Scenario B — Preprocess consistency

```text
Set Resize/FitPad/CenterCrop
→ refresh
→ preview layout uses processed dimensions
→ generated tile rectangles are identical
→ manifest rectangles are identical
```

### Scenario C — All traversal modes

Verify `OrderIndex` and filenames for every supported start order and crop order.

### Scenario D — Successful generation

```text
Generate
→ each completed tile turns green after verification
→ manifest has all tiles
→ manifest readback passes
→ WorkflowContext updated
```

### Scenario E — Failure

```text
writer fails at tile K
→ tile K Failed
→ remaining tiles not falsely completed
→ no complete manifest published
→ prior output unchanged
→ UI restored
```

### Scenario F — Cancellation

```text
cancel at tile K
→ completed tiles remain accurate
→ unprocessed tiles remain Pending
→ no complete manifest published
→ temp run cleaned safely
→ selected output root remains intact
```

---

# 14. Required tests

Add tests where the current solution supports them. At minimum create testable non-UI services.

## 14.1 Image tests

- HObject → Bitmap → HObject
- HObject → Mat → HObject
- Bitmap → Mat → Bitmap
- grayscale dimensions and pixel samples
- RGB/BGR channel correctness
- unsupported image type
- repeated conversion and disposal

## 14.2 Geometry tests

For every tile compare:

```text
PreviewRectangle
GeneratorCropRectangle
ManifestRectangle
```

They must be equal.

## 14.3 Manifest tests

- serialize/deserialize round trip
- deterministic order
- duplicate order rejection
- missing order rejection
- path validation
- bounds validation
- incomplete output rejection

## 14.4 Output safety tests

Use temporary folders.

Assert the test root and unrelated files are never deleted.

---

# 15. Build and quality gates

After every task:

1. Build `Debug|x64`.
2. Build `Release|x64`.
3. Run relevant tests.
4. Record warnings.
5. Record tests not runnable because of HALCON/native runtime.
6. Do not claim runtime success from static inspection.

Do not finish with:

- new cross-thread exceptions
- swallowed exceptions
- full-size Bitmap reread for TIFF
- duplicate layout/order logic
- `Directory.Delete(OutputDirectory, true)`
- manifest written before tile verification
- source disposed while viewer still uses it
- direct changes in `EWindowControl`

---

# 16. Required reports

Create or update:

```text
docs/tab2_implementation_baseline.md
docs/tab2_fix_task_results.md
docs/tab2_fix_test_report.md
docs/tab2_fix_changed_files.md
docs/tab2_manifest_contract.md
docs/tab2_image_ownership.md
```

`tab2_fix_task_results.md` must contain:

| Task | Status | Files changed | Debug x64 | Release x64 | Tests | Notes |
|---|---|---|---|---|---|---|

`tab2_fix_changed_files.md` must contain:

| File | Class/Method | Before | After | Reason |
|---|---|---|---|---|

`tab2_image_ownership.md` must document:

| Resource | Owner | Created by | Replaced by | Disposed by | UI/Worker |
|---|---|---|---|---|---|

`tab2_manifest_contract.md` must include:

- JSON example
- field definitions
- coordinate space
- path policy
- version policy
- validation rules
- Tab 2 writer location
- future Tab 3 reader location

---

# 17. Final response format

At completion, report:

1. Root causes fixed.
2. Exact manifest contract.
3. Exact canonical image ownership.
4. Exact interop conversion locations.
5. Exact service used by both preview and generator.
6. How duplicate order calculation was removed.
7. How output deletion risk was eliminated.
8. How user warning works.
9. How zoom/pan/Fit were restored.
10. Changed files and methods.
11. Build results.
12. Tests passed.
13. Tests not run and why.
14. Remaining risks.

Do not say Tab 2 is complete while any of these remain:

- preview and generator use different layouts
- generator reopens the source
- large TIFF passes through full-size Bitmap
- user output root can be recursively deleted
- manifest can be published after partial failure
- zoom/pan is broken
- UI image processing runs on the UI thread
- contract remains duplicated
