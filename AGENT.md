# AGENT.md — GerberViewer Repository Rules

## 1. Purpose

This file defines mandatory rules for every Codex task performed in this repository.

The current implementation priority is:

1. Complete and stabilize **Tab 2 — Create Gerber Sample**.
2. Preserve the existing behavior of **Tab 1 — Read Gerber**.
3. Do not implement or refactor **Tab 3 — Align and Stitching** unless a shared model or contract must be adjusted so Tab 2 can generate valid data for future Tab 3 use.

These rules are authoritative for all later tasks unless the user explicitly replaces them.

---

## 2. Repository scope

The solution contains these major projects:

- `GerberViewer`
- `GerberEngine`
- `GerberStitching.Core`
- `EWindowControl`

Tab 2 code should remain separated into:

- WinForms UI and derived controls in `GerberViewer`
- Image, geometry, manifest, preprocessing, traversal, crop, and output services in `GerberStitching.Core`

Do not move processing logic into `.Designer.cs`.

---

## 3. Mandatory rule: do not modify `EWindowControl`

### 3.1 Base project is read-only

The project below is treated as external/shared source:

```text
EWindowControl/
```

Codex must not directly edit, delete, rename, reformat, or regenerate files in this project.

This includes, but is not limited to:

- `EWindowControl.cs`
- `PrviateFunc.cs`
- `ERoiList.cs`
- `PubliceStructure.cs`
- `EWindowControl.Designer.cs`
- project files, resources, and embedded images

### 3.2 Required extension method

When Tab 2 needs behavior not exposed by the base control, create or update a derived control inside `GerberViewer`, for example:

```csharp
public sealed class GerberSampleWindow : EWindowControl.EWindowControl
{
}
```

Allowed approaches:

- inheritance
- composition
- adapter classes
- extension services outside the `EWindowControl` project
- event subscription through existing public APIs
- wrapper methods in `GerberSampleWindow`

Forbidden approaches:

- changing private fields in `EWindowControl`
- patching the base project to expose one-off Tab 2 APIs
- copying the whole base class into another file
- reflection into private members unless the user explicitly approves it
- changing the base control merely to make one Tab 2 test pass

### 3.3 Preserve inherited interaction behavior

`GerberSampleWindow` must preserve or restore:

- mouse-wheel zoom
- drag/pan
- fit-to-view
- double-click fit when enabled
- image-coordinate mouse tracking
- overlay alignment during zoom and pan

Do not replace these behaviors with a separate PictureBox-only viewer.

---

## 4. Mandatory rule: preserve the Tab 2 ↔ Tab 3 bridge

Tab 3 implementation is currently out of scope, but Tab 2 output must remain consumable by the future Tab 3 pipeline.

### 4.1 Single canonical manifest contract

There must be exactly one canonical typed contract for:

```text
sample_manifest.json
```

The contract must live in a shared non-UI location, preferably:

```text
GerberStitching.Core/Configuration
```

or:

```text
GerberStitching.Core/Models
```

Do not define separate manifest DTOs in Tab 2 and Tab 3.

### 4.2 Required tile identity

Every tile must retain these stable identifiers:

```text
OrderIndex
Row
Column
ExpectedPath
ExpectedX
ExpectedY
Width
Height
```

Mapping policy:

```text
Captured image OrderIndex K ↔ Sample tile OrderIndex K
```

`OrderIndex` must be unique, deterministic, zero-based unless the canonical contract explicitly states otherwise, and independent from file enumeration order.

### 4.3 Contract compatibility rule

When changing the manifest:

- add a `ManifestVersion`
- keep field names stable
- document coordinate space and transform direction
- store crop positions in processed-source pixel coordinates
- use a deterministic path policy
- validate the manifest by deserializing it before publication
- reject incomplete manifests
- never publish a manifest on cancel or partial failure
- update all shared readers/writers/tests in the same task

Do not silently introduce a second JSON shape.

### 4.4 Tab 3 scope boundary

Allowed during Tab 2 work:

- adding or correcting shared manifest models
- adding validation used by both tabs
- updating shared documentation
- preserving future `OrderIndex K ↔ K` behavior

Not allowed unless separately requested:

- implementing Tab 3 UI
- implementing alignment
- implementing neighbor recovery
- implementing stitching
- changing Tab 3 workflow behavior beyond the minimum needed to compile against the shared contract

---

## 5. Mandatory rule: do not delete external or reference source

Codex must not delete, overwrite, rename, or move code that originated outside the current implementation.

Protected examples include:

```text
reference/
Sources/
third_party/
vendor/
external/
legacy reference projects
ZIP archives
sample Gerber files
sample TIFF/PNG/BMP files
test datasets
HALCON reference code
OpenCV reference code
```

Also do not delete files merely because they appear unused.

Before removing any source file from the active solution:

1. Prove it is owned by this repository.
2. Search all project references and runtime loading paths.
3. Record the reason in the changed-files report.
4. Obtain explicit user approval when the file is external, migrated, legacy, or reference material.

Preferred handling for unused code:

- leave it unchanged
- exclude it from build only when justified
- move repository-owned obsolete code to an explicitly approved archive
- document it in `unusedLog.html` or a Markdown report

Never delete a ZIP or reference implementation as cleanup.

---

## 6. Canonical image policy for Tab 2

### 6.1 Canonical in-memory image

Tab 2 must use HALCON `HObject` as the canonical image representation for:

- opened sample image
- preprocessed sample image
- crop source
- preview source
- tile generation source

Do not use `Bitmap` as the master source for large TIFF or BigTIFF input.

### 6.2 No repeated decoding

A sample image must be decoded once per selected source/run unless an explicit cache invalidation requires a reload.

Forbidden flow:

```text
Open with HALCON for preview
→ reopen with Bitmap for preprocessing
→ reopen again for tile generation
```

Required flow:

```text
Read once
→ create owned canonical HObject
→ preprocess once
→ retain owned processed HObject
→ calculate layout from processed dimensions
→ preview and generator consume the same processed image and layout
```

### 6.3 Explicit interop layer

All conversion among these libraries must go through a named, tested interop service:

```text
HALCON HObject
System.Drawing.Bitmap
OpenCvSharp.Mat
```

Recommended location:

```text
GerberStitching.Core/Imaging/ImageInterop
```

Recommended API shape:

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

Rules:

- every conversion result must have explicit ownership
- method names must state whether they return a copy
- never return a `Bitmap` backed by a temporary HALCON pointer
- never return a `Mat` backed by disposed managed memory
- preserve channel order and bit depth
- document RGB/BGR conversion
- support grayscale and color paths
- reject unsupported pixel types with contextual errors
- dispose all temporary objects deterministically

Direct ad hoc conversion code in UI event handlers is forbidden.

---

## 7. Single source of truth for preprocessing, grid, and order

Tab 2 must have one prepared run model, for example:

```csharp
public sealed class PreparedSampleRun : IDisposable
{
    public HObject ProcessedImage { get; }
    public int ProcessedWidth { get; }
    public int ProcessedHeight { get; }
    public SampleGridLayout Layout { get; }
    public IReadOnlyList<SampleTileLayout> TilesByOrder { get; }
    public GerberSampleConfig ConfigSnapshot { get; }
}
```

Both preview and generator must consume the same:

- processed image
- processed dimensions
- overlap calculation
- physical rectangles
- traversal result
- `OrderIndex`
- row and column mapping

Do not maintain independent ordering implementations for preview and generation.

Required architecture:

```text
Config snapshot
+ canonical source HObject
→ preprocess service
→ processed HObject
→ one geometry/traversal service
→ PreparedSampleRun
   ├─ preview overlay
   └─ tile generator
```

`SampleGeometryCalculator` and `RobotArrange/TraversalGraph` must not independently assign conflicting `OrderIndex` values. Consolidate ordering behind one service or make one delegate to the other.

---

## 8. Threading and UI responsiveness

All expensive image work must execute outside the UI thread:

- image decode
- image validation
- preprocessing
- format conversion
- grid/traversal calculation for large layouts
- tile crop
- tile encode/write
- output verification
- manifest serialization and validation
- overlay preparation when computationally expensive

Required mechanisms:

```text
Task.Run
CancellationToken
IProgress<T>
immutable/config snapshots
UI update through captured SynchronizationContext or Progress<T>
```

Forbidden mechanisms:

- `Application.DoEvents()`
- `Thread.Sleep()` to hide race conditions
- accessing WinForms controls from worker threads
- reading `PropertyGrid` or ComboBox values inside a worker
- calling `sampleWindow` from a worker thread
- blocking `.Result` or `.Wait()` on the UI thread
- starting a second run while one is active

UI controls must be restored in `finally`.

---

## 9. Safe output policy

### 9.1 Never delete the user-selected root

This is forbidden:

```csharp
Directory.Delete(userSelectedOutputDirectory, true);
```

The selected output path is a root location, not an application-owned disposable directory.

### 9.2 Application-owned run directory

Create a run-specific temporary directory:

```text
<OutputRoot>/.creating_<runId>/
```

or:

```text
<OutputRoot>/GerberSample_<runId>/.creating/
```

Only directories created by the current run may be recursively deleted automatically.

### 9.3 User warning

When an operation could overwrite, replace, clean, or remove existing files:

- detect the condition before starting the worker
- show the exact path
- explain which files may be replaced
- require explicit user confirmation
- default to cancel
- log the user decision

Never hide destructive behavior behind a normal Create button.

### 9.4 Transactional publication

On success:

```text
validate every tile
→ verify every saved file is readable
→ write config snapshot
→ write manifest
→ deserialize and validate manifest
→ write overlay preview if enabled
→ atomically publish/move final run directory
→ update WorkflowContext
```

On failure or cancellation:

- do not publish a complete manifest
- do not update `WorkflowContext.ManifestPath` as successful
- clean only the current application-owned temporary directory
- keep prior successful output intact

---

## 10. Resource ownership

Every class holding one of these resources must define ownership explicitly:

- `HObject`
- `HTuple`
- `HImage`
- `Bitmap`
- `Graphics`
- `Mat`
- `Stream`
- `CancellationTokenSource`

Rules:

- implement `IDisposable` where ownership spans method scope
- do not dispose an image still displayed by a control
- do not share one mutable/disposable image instance among unrelated owners
- use copy/clone only through documented APIs
- replace old source through a single method
- dispose prior owned source exactly once
- clear preview ownership when the control is disposed

Shallow copying HALCON handles into multiple owners without a contract is forbidden.

---

## 11. Error handling and logging

Do not use:

```csharp
catch { }
```

Do not catch an exception only to ignore it.

Every error must include relevant context:

- source file
- output folder
- tile `OrderIndex`
- row and column
- image dimensions
- preprocess mode
- pixel type/channel count
- operation name

Expected cancellation must be handled separately from failure.

A failed tile must remain failed and must not be shown as completed.

---

## 12. Designer and WinForms rules

`.Designer.cs` may contain only:

- control declarations
- property assignments
- layout
- event wiring
- standard `Dispose(bool)`

It must not contain:

- image loading
- HALCON operators
- OpenCV code
- JSON logic
- preprocessing
- traversal
- crop logic
- async worker logic
- manifest logic

Preserve Visual Studio Designer formatting and `.resx` relationships.

---

## 13. Required workflow for every Codex task

Before editing:

1. Read this file.
2. Read the current task prompt.
3. Inspect the current branch and relevant docs.
4. State the task scope and files expected to change.
5. Identify whether the task affects the manifest bridge.
6. Identify all owned disposable resources.
7. Record baseline build/test status.

During editing:

1. Keep the solution buildable after each logical task.
2. Do not perform unrelated cleanup.
3. Do not change public contracts without updating tests and docs.
4. Do not change `EWindowControl`.
5. Do not delete external/reference source.
6. Use C# 7.3-compatible syntax.

After editing:

1. Build x64 Debug.
2. Build x64 Release.
3. Run relevant unit/integration tests.
4. Report tests that could not run and why.
5. List every changed file and method.
6. Report resource ownership changes.
7. Report manifest schema changes.
8. Report any remaining risk.
9. Do not mark a task complete based only on static inspection when runtime behavior is required.

---

## 14. Definition of done for Tab 2 work

A Tab 2 task is complete only when all applicable statements are true:

- `EWindowControl` was not modified.
- Large TIFF/BigTIFF is not reopened through `Bitmap`.
- Tab 2 uses an owned canonical `HObject`.
- Preview and generator use the same processed image.
- Preview and generator use the same layout and `OrderIndex`.
- Zoom, pan, and fit still work in `GerberSampleWindow`.
- Image processing does not block the UI thread.
- Cancellation restores UI state.
- No user-selected root folder is recursively deleted.
- Destructive output behavior requires an explicit warning.
- Manifest uses the canonical shared contract.
- Manifest is not published after failure or cancellation.
- Every completed tile has a readable output file.
- All changed code is C# 7.3 / .NET Framework 4.8 compatible.
- Build and tests are reported honestly.
