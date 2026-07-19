# GERBER VIEWER IMPLEMENTATION PLAN
## DPI-Independent SVG Preview, Vector Zoom, Distance Measurement, and Angle Measurement

**Target platform:** C# 7.3, WinForms, .NET Framework 4.8, x64  
**Target specification:** `Spec_CSharpWinform_EN_v1.2.md`  
**Date:** 2026-07-19

---

## 1. Objective

Refactor the current Gerber Viewer so that:

1. The interactive viewer is rendered from DPI-independent vector geometry.
2. Zoom and pan operate on an SVG viewport/camera and do not regenerate a DPI-based preview bitmap.
3. `Export DPI` is used only when exporting PNG or another raster output.
4. Distance and angle measurements are calculated from Gerber world coordinates in millimeters, not from screen pixels.
5. Existing Gerber parsing, aperture, polarity, layer, and PNG export behavior remains functional during the migration.

The implementation shall follow this separation:

```text
Gerber file
    ↓
Parser + graphics state
    ↓
GerberScene in millimeters
    ├── GerberSvgRenderer → WebView2 vector preview
    ├── Measurement overlay → world-coordinate SVG overlay
    └── GerberRasterExportRenderer → PNG at Export DPI
```

---

## 2. Current-State Audit Result

The current specification already describes the correct target architecture, but the implementation may remain DPI-dependent if any of the following patterns still exist:

- The preview calls a renderer that accepts `Dpi`.
- `cmbDpi_SelectedIndexChanged` triggers `RenderPreview`, `RefreshPreview`, or bitmap regeneration.
- `OnPaint` displays a bitmap created using `mm / 25.4 * dpi`.
- The same `RenderOptions` class is used by both preview and PNG export.
- A public generic method such as `RenderCombinedBitmap(...)` is reused by the viewer.
- The fallback preview reads the Export DPI ComboBox.
- Background preview code accesses `cmbDpi` directly, creating both architectural coupling and cross-thread risk.

Version 1.2 of the specification closes these gaps by:

- Replacing generic DPI bitmap APIs with export-only APIs.
- Defining a viewport-size bitmap fallback that has no DPI property.
- Prohibiting the Export DPI event from refreshing the preview.
- Adding explicit distance and angle requirements in world millimeters.
- Adding architectural and acceptance tests for DPI independence.

---

## 3. Non-Negotiable Architecture Rules

### 3.1. DPI ownership

`Dpi` may exist only in raster-export code:

```text
Allowed:
RasterExportOptions.Dpi
GerberRasterExportRenderer
ExportLayerPng
ExportCombinedPng
PNG dimension and memory validation

Forbidden:
SvgRenderOptions.Dpi
ViewportState.Dpi
GerberPreviewHost.Dpi
MeasurementDocument.Dpi
PreviewRenderCoordinator reading cmbDpi
World-to-screen camera calculation using Export DPI
```

Monitor DPI and `devicePixelRatio` are permitted only for WinForms scaling, browser rasterization, text readability, and screen-pixel tolerances. They must not change Gerber geometry or measurement values.

### 3.2. Single geometric source

`GerberScene` shall be the only geometric source for both preview and export. It shall store all coordinates, widths, aperture dimensions, bounds, and measurement snap points in millimeters.

### 3.3. Preview rendering

The primary viewer shall use SVG hosted by WebView2. It shall not display a bitmap created at 150, 300, 600, or 1200 DPI.

### 3.4. Export rendering

PNG export shall render directly from `GerberScene` using `RasterExportOptions.Dpi`. It shall not capture the WebView2 surface.

---

## 4. Implementation Phases

## Phase 0 — Baseline and DPI-Coupling Audit

### Goal

Identify every place where preview behavior currently depends on DPI before changing code.

### Tasks

1. Search the solution for:

```text
cmbDpi
Dpi
RenderPreview
RenderCombined
RenderLayer
RenderToBitmap
Bitmap
DrawImage
mm / 25.4
25.4 *
Graphics.FromImage
OnPaint
MouseWheel
```

2. Classify each occurrence as one of:

- `Export-only` — valid.
- `Monitor/UI scaling` — valid but must be named clearly.
- `Preview dependency` — must be removed.
- `Unknown` — inspect call flow.

3. Trace the existing preview flow from file load to canvas paint.
4. Record which methods are called by `cmbDpi_SelectedIndexChanged`.
5. Record whether a background task reads WinForms controls directly.
6. Capture baseline screenshots and timings for:
   - initial file load;
   - Fit-to-view;
   - ten wheel-zoom operations;
   - 150-DPI and 1200-DPI preview behavior;
   - one PNG export at each supported DPI.

### Deliverable

`DpiDependencyAudit.md` containing file, method, line, current behavior, classification, and required action.

### Exit gate

Every DPI reference is classified, and the existing preview call graph is documented.

---

## Phase 1 — Separate Preview State from Export State

### Goal

Make it structurally impossible for the normal preview pipeline to consume Export DPI.

### Tasks

1. Replace the shared render-options class with:

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

public sealed class RasterExportOptions
{
    public int Dpi { get; set; }
    public ColorMode Mode { get; set; }
    public double MarginMm { get; set; }
    public Color Background { get; set; }
    public bool InvertBinary { get; set; }
}

public sealed class ViewportBitmapOptions
{
    public int ViewportWidthPx { get; set; }
    public int ViewportHeightPx { get; set; }
    public RectangleD WorldViewportMm { get; set; }
    public ColorMode Mode { get; set; }
    public Color Background { get; set; }
}
```

2. Rename the UI label from `DPI` to `Export DPI`.
3. Change the DPI ComboBox event so it only updates an export settings field:

```csharp
private void cmbExportDpi_SelectedIndexChanged(object sender, EventArgs e)
{
    _exportSettings.Dpi = ParseSelectedExportDpi();
    UpdateExportEstimate();

    // Do not call RefreshPreview, RenderPreviewAsync, or BuildScene here.
}
```

4. Read ComboBox values only on the UI thread. Pass an immutable copy of `RasterExportOptions` into the export task.
5. Remove any direct `cmbDpi` access from parser, preview, rendering workers, and measurement code.
6. Rename DPI-based renderer classes and methods to include `Export`, for example:

```text
GerberRasterRenderer          → GerberRasterExportRenderer
RenderCombinedBitmap          → remove or make internal export-only
RenderLayerBitmap             → remove or make internal export-only
RenderCombined                → ExportCombinedPng or RenderCombinedForExport
```

7. Add an architecture test that fails if `SvgRenderOptions` or `ViewportBitmapOptions` exposes a property named `Dpi`.

### Exit gate

Changing Export DPI from 150 to 1200 does not call any preview method, does not change the current camera, and does not allocate a preview bitmap.

---

## Phase 2 — Establish the DPI-Independent `GerberScene`

### Goal

Ensure parsing and plotting produce a renderer-neutral vector scene in millimeters.

### Tasks

1. Add or normalize these models:

```text
GerberScene
GerberSceneLayer
GerberPrimitive
StrokePrimitive
ArcStrokePrimitive
FlashPrimitive
RegionPrimitive
RectangleD
PointD
ApertureDefinition
LayerPolarity
```

2. Store all geometry in millimeters as `double`.
3. Keep arcs as arcs in the scene; flatten only inside a renderer when required.
4. Store bounding boxes per primitive, per layer, and for the combined scene.
5. Preserve ordered polarity operations or produce an equivalent layer-local composition model.
6. Build a spatial index for primitive bounds and snap candidates.
7. Remove any pixel or DPI field from scene models.
8. Add tests for:
   - MM and IN normalization;
   - coordinate-format parsing;
   - line, arc, flash, and region bounds;
   - LPD/LPC layer-local behavior;
   - scene equality across different Export DPI settings.

### Exit gate

The same Gerber files produce an identical `GerberScene` regardless of the selected Export DPI.

---

## Phase 3 — Implement the SVG Renderer

### Goal

Generate a self-contained SVG preview from `GerberScene` without raster image embedding.

### Tasks

1. Implement `GerberSvgRenderer`.
2. Generate the root SVG with a `viewBox` based on combined world bounds plus margin.
3. Create one stable `<g>` group per Gerber layer.
4. Render primitives as SVG vector elements:

```text
Line/arc stroke  → path
Circle flash     → circle or reusable definition
Rectangle flash  → rect/path
Obround flash    → path
Polygon flash    → polygon/path
Region           → closed path
Clear polarity   → layer-local mask/composition
```

5. Use `<defs>/<use>` only when it improves DOM size and performance.
6. Do not embed a full-board PNG or base64 bitmap.
7. Sanitize all identifiers and text derived from file names.
8. Keep Gerber-generated SVG script-free. Place trusted interaction JavaScript only in the fixed preview shell.
9. Validate SVG XML and compare its physical bounds against `GerberScene`.

### Exit gate

A representative Gerber package renders as SVG with correct layer order, dimensions, apertures, regions, and polarity.

---

## Phase 4 — Integrate WebView2 and Vector Camera Control

### Goal

Replace DPI-based bitmap zoom with vector zoom and browser re-rasterization.

### Tasks

1. Create `GerberPreviewHost` as a dedicated WinForms `UserControl`.
2. Encapsulate WebView2 initialization, retry, error state, navigation blocking, and disposal.
3. Load a fixed local preview shell containing:

```text
preview-shell.html
preview-shell.css
preview-shell.js
```

4. Insert or replace Gerber layer SVG groups without navigating to external content.
5. Implement camera state independently of Export DPI:

```csharp
public sealed class ViewportState
{
    public RectangleD WorldViewportMm { get; set; }
    public double ZoomFactor { get; set; }
    public double DevicePixelRatio { get; set; }
}
```

6. Implement cursor-anchored wheel zoom:
   - convert cursor client position to world point;
   - update zoom/viewBox;
   - preserve the same world point under the cursor.
7. Implement pan by middle-button, right-button, or `Space + drag`.
8. Implement Fit-to-view from combined world bounds.
9. During continuous input, update the camera immediately.
10. After an 80–200 ms debounce, run optional viewport culling or LOD refinement.
11. Keep the current frame visible until refined SVG content is ready.
12. Move screen-coordinate conversion into a dedicated camera/transform component. Do not call the export coordinate transformer.

### Exit gate

The user can zoom deeply without bitmap pixelation. Export DPI changes have no visible effect on the viewer.

---

## Phase 5 — Add Distance and Angle Measurement

### Goal

Provide measurement behavior comparable to a modern online Gerber viewer while maintaining world-coordinate accuracy.

### 5.1. Measurement modes

Add ToolStrip controls for:

```text
Pan/Inspect
Measure Distance
Measure Angle
Single/Continuous mode
Snap toggle
Measurement unit: mm / mil / inch
Clear Measurements
```

Only one main interaction mode may be active at a time.

### 5.2. Distance measurement

Use two world points `P1` and `P2`:

```text
dx = P2.Xmm - P1.Xmm
dy = P2.Ymm - P1.Ymm
distance = sqrt(dx * dx + dy * dy)
bearing = atan2(dy, dx) * 180 / PI
bearing = (bearing + 360) % 360
```

Display:

```text
Distance
ΔX
ΔY
Bearing from +X
Selected display unit
```

The live overlay shall update while the pointer moves after the first point is placed.

### 5.3. Three-point angle measurement

Use points `A → V → B`, where `V` is the vertex:

```text
u = A - V
v = B - V
cosTheta = dot(u, v) / (length(u) * length(v))
cosTheta = clamp(cosTheta, -1, 1)
angle = acos(cosTheta) * 180 / PI
```

Reject or keep the tool active when either vector length is below a small world epsilon.

### 5.4. Coordinate conversion

1. Capture client coordinates in the trusted preview shell.
2. Convert them to world coordinates using the inverse SVG screen transform or inverse camera matrix.
3. Correctly account for the Gerber +Y-up system and the browser +Y-down screen system.
4. Perform authoritative measurement calculations in `MeasurementMath` using world millimeters.
5. Never use screen-pixel distance as the engineering measurement.

### 5.5. Overlay behavior

1. Create a dedicated SVG overlay group above all Gerber layers.
2. Store measurement points in world millimeters.
3. Use non-scaling strokes for lines and markers.
4. Keep labels readable at all zoom levels.
5. Preserve completed and in-progress measurements during zoom, pan, Fit-to-view, layer changes, monitor scaling changes, and Export DPI changes.
6. Exclude measurements from PNG export by default.

### 5.6. Interaction behavior

Implement:

```text
Esc       → cancel in-progress measurement
Delete    → delete selected measurement
Ctrl+Z    → undo latest completed measurement
Clear     → remove all measurements
Wheel     → zoom while measuring
Space+drag / middle drag / right drag → pan while measuring
```

In Continuous mode, completing one measurement shall immediately prepare the next measurement while retaining previous overlays.

### 5.7. Snapping

1. Add optional grid snapping.
2. Add snap candidates for indexed endpoints, flash centers, and drill centers when available.
3. Define snap tolerance in CSS pixels, for example 8 px.
4. Convert tolerance to world millimeters using the current camera scale.
5. Show a visible snap indicator.
6. Never derive snapping tolerance from Export DPI.

### 5.8. WebView2 bridge

Use typed message contracts, for example:

```json
{
  "type": "measurementPointCommitted",
  "mode": "distance",
  "worldXmm": 12.34,
  "worldYmm": 56.78
}
```

Recommended responsibility split:

- JavaScript: pointer capture, camera interaction, immediate live overlay.
- C#: authoritative measurement model, unit conversion, persistence, undo/delete/clear, final validation.
- Bridge: committed points, selected IDs, camera changes, and overlay-document updates.

Do not send Export DPI through this bridge.

### Exit gate

Distance, ΔX, ΔY, bearing, and three-point angles remain numerically unchanged after zoom, pan, monitor scaling changes, and Export DPI changes.

---

## Phase 6 — Progressive Rendering, Culling, and LOD

### Goal

Keep vector interaction responsive for large Gerber scenes.

### Tasks

1. Benchmark full-scene SVG before adding complexity.
2. Enable viewport culling only after the scene exceeds a measured threshold.
3. Query an expanded viewport to avoid geometry appearing late at the edges.
4. Use screen-pixel tolerance only for visual LOD decisions.
5. Never alter world geometry or measurement values for LOD.
6. Do not cull active measurement overlays.
7. Use version/cancellation tokens so stale refinement results cannot overwrite a newer camera state.
8. Swap refined layer groups atomically.

### Exit gate

Zoom/pan feedback begins within 50 ms, and no white frame appears during refinement.

---

## Phase 7 — Preserve and Harden PNG Export

### Goal

Keep high-quality raster export while ensuring it is isolated from preview.

### Tasks

1. Implement or rename the renderer as `GerberRasterExportRenderer`.
2. Accept only `GerberScene` and `RasterExportOptions`.
3. Calculate export dimensions using:

```text
widthPx  = ceil(widthMm  / 25.4 * dpi)
heightPx = ceil(heightMm / 25.4 * dpi)
```

4. Validate maximum width, height, total pixels, stride, and estimated memory before allocation.
5. Render polarity within each layer before compositing layers.
6. Dispose all GDI+ resources deterministically.
7. Keep measurements excluded unless a future explicit `ExportWithAnnotations` option is introduced.
8. Run export on a background task using a copied options object.
9. Do not read WinForms controls from the export worker.

### Exit gate

PNG output remains correct at 150, 300, 600, and 1200 DPI, while the preview remains unchanged.

---

## Phase 8 — Tests, Migration Cleanup, and Acceptance

### Unit tests

Add tests for:

- world-unit normalization;
- SVG bounds and layer groups;
- polarity and regions;
- camera world/screen round-trip;
- cursor-anchored zoom;
- distance, ΔX, ΔY, and bearing;
- three-point included angle;
- zero-length angle vectors;
- unit conversion:
  - `1 inch = 25.4 mm`;
  - `1 mil = 0.0254 mm`;
- snap tolerance conversion;
- DPI invariance.

### Integration tests

1. Load a multi-layer Gerber job.
2. Fit-to-view and record camera state.
3. Change Export DPI from 150 to 1200.
4. Assert:
   - camera unchanged;
   - SVG/viewBox unchanged;
   - measurements unchanged;
   - no preview render triggered;
   - no export-size bitmap allocated.
5. Complete distance and angle measurements.
6. Zoom and pan while a measurement is in progress.
7. Export PNG and verify measurement overlays are absent.
8. Test Windows scaling at 100%, 125%, and 150%.

### Cleanup

Remove or deprecate:

- preview code paths that accept DPI;
- shared preview/export render options;
- DPI-based bitmap cache used as the primary viewer;
- preview workers reading `cmbDpi`;
- duplicate coordinate conversion logic;
- unused GDI+ preview resources.

### Final acceptance gate

All version 1.2 acceptance scenarios pass, and code search confirms that preview and measurement components do not reference `RasterExportOptions.Dpi` or the Export DPI ComboBox.

---

## 5. Recommended Commit Sequence

Keep the solution buildable after every commit:

1. `docs: add v1.2 DPI-independent preview and measurement requirements`
2. `refactor: split SVG preview options from raster export options`
3. `refactor: isolate export DPI and remove preview DPI access`
4. `refactor: normalize GerberScene to world millimeters`
5. `feat: add SVG renderer and layer-local polarity masks`
6. `feat: add WebView2 preview host and vector camera`
7. `feat: add distance measurement and live overlay`
8. `feat: add three-point angle measurement and snapping`
9. `perf: add viewport culling and progressive refinement`
10. `test: add DPI invariance and measurement acceptance tests`
11. `cleanup: remove legacy DPI bitmap preview path`

---

## 6. Definition of Done

The refactor is complete only when all statements below are true:

- The primary preview is SVG vector content.
- The preview has no image-DPI setting.
- `cmbExportDpi` changes export configuration only.
- No preview or measurement class accepts `RasterExportOptions`.
- No preview worker reads a WinForms DPI control.
- Zoom/pan changes only the camera and optional culling/LOD detail.
- Distance and angle calculations use world millimeters.
- Measurement overlays survive zoom, pan, layer changes, and Export DPI changes.
- PNG export alone uses DPI.
- PNG export does not capture WebView2.
- The bitmap fallback, if enabled, renders to viewport width/height pixels and does not use Export DPI.
- All architecture, unit, integration, and acceptance tests pass.
