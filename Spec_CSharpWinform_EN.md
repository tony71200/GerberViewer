# TECHNICAL SPECIFICATION
## GERBER VIEWER & PNG CONVERTER — C# / WINFORMS (.NET FRAMEWORK 4.8)

**File name:** `Spec_CSharpWinform_EN.md`  
**Version:** 1.1  
**Date:** 2026-07-18  
**Supersedes:** `Spec_CSharpWinform.md` version 1.0  
**UI/UX reference:** functionality comparable to `https://onlinegerberviewer.com/`  
**Architecture reference:** the parser → plotter → SVG renderer concept used by `@tracespace/renderer`; the JavaScript package itself is not used inside the C# core.

---

## 1. OBJECTIVES (BR — Business Requirements)

| ID | Description |
|---|---|
| BR-001 | The Windows desktop application shall open, inspect, configure, and convert Gerber files (RS-274X and Gerber X2) to high-quality PNG images. |
| BR-002 | The application shall provide an experience comparable to Online Gerber Viewer: multi-file drag-and-drop, layer visibility and color controls, zoom/pan, cursor coordinates in millimeters/inches, and realistic rendering. |
| BR-003 | The `GerberEngine` processing core shall be separated from the UI and packaged as a class library with a stable public API reusable by console tools, services, and other applications. |
| BR-004 | The interactive preview shall use vector geometry and SVG and shall be independent of PNG export DPI. Users shall be able to zoom deeply without pixelation or blur caused by a fixed-resolution preview bitmap. |
| BR-005 | Zoom shall respond immediately through viewport transformation, after which the display engine shall re-rasterize vector content for the current screen resolution. Gerber files shall not be reparsed on every zoom or pan operation. |

---

## 2. MANDATORY TECHNOLOGIES AND CONSTRAINTS

- Language: **C# 7.3**, compatible with .NET Framework 4.8. Do not use `record`, target-typed `new`, switch expressions, or syntax requiring C# 8 or later.
- UI: **Windows Forms (.NET Framework 4.8)**.
- The parser, graphics state, plotter, geometry model, and polarity logic shall be implemented inside `GerberEngine`.
- Rendering shall use a mandatory **dual-rendering architecture**:
  1. An **SVG vector renderer** for the interactive preview.
  2. A **GDI+ raster renderer** for PNG export at a selected DPI.
- `@tracespace/renderer` is an architectural reference only. The application shall not require Node.js and shall not use JavaScript to parse Gerber data.
- `GerberEngine.dll` shall not reference `System.Windows.Forms`, WebView2, or any UI control.
- The UI may use **Microsoft Edge WebView2** as a local SVG display host. WebView2 shall only display internally generated SVG/HTML and shall not parse Gerber files.
- If the WebView2 Runtime is unavailable, the application shall show a clear diagnostic and may provide a bitmap preview fallback. PNG export shall remain operational.
- No third-party Gerber parser or Gerber renderer shall be used in the core. UI dependencies such as WebView2 shall be isolated in the `GerberViewer` project.

---

## 3. FUNCTIONAL REQUIREMENTS (FR)

The word **“shall”** indicates a mandatory requirement. Every FR shall trace back to a BR and forward to a source file in Section 9.

### 3.1. Formats and multi-layer support

- **FR-001** (BR-001): The system shall parse RS-274X commands including `FS`, `MO`, `AD`, `AM`, `D01/D02/D03`, `G01/G02/G03`, `G36/G37`, `LPD/LPC`, and `M02`.
- **FR-002** (BR-001): The system shall read the Gerber X2 `TF.FileFunction` attribute for automatic layer classification. If unavailable, it shall use extensions such as `.gtl`, `.gbl`, `.gts`, `.gbs`, `.gto`, `.gbo`, `.gko`, and `.gm1`, together with file-name keywords.
- **FR-003** (BR-002): Users shall load multiple files through multi-select or drag-and-drop. Each file shall be managed as an independent layer with a name, type, visibility state, color, and drawing order.
- **FR-004** (BR-002): Users shall toggle visibility, change color, remove layers, and reorder layers without reparsing unchanged files.

### 3.2. Graphics core and apertures

- **FR-005** (BR-001): The engine shall correctly construct the four standard aperture types: Circle, Rectangle, Obround, and Polygon, for both `D03` flashes and `D01` strokes.
- **FR-006** (BR-001): Strokes using circular apertures shall have round caps and joins. Strokes using non-circular apertures shall be represented using aperture geometry and shall not be reduced by default to a line with `Pen.Width`.
- **FR-007** (BR-001): The engine shall support Aperture Macro primitives 1, 4, 5, 20, and 21 at minimum. Unsupported primitives shall degrade safely to bounding geometry and produce a warning rather than failing the complete render.
- **FR-008** (BR-004): The parser/plotter shall produce a renderer-independent intermediate model named `GerberScene`, containing millimeter-based primitives, layers, polarity, apertures, and bounding boxes. `GerberScene` shall contain no pixels or DPI values.

### 3.3. Coordinates, units, and transforms

- **FR-009** (BR-001): The engine shall detect MM/IN units and coordinate format from `FS`/`MO`; all internal coordinates and dimensions shall be normalized to **millimeters represented by `double`**.
- **FR-010** (BR-004): `CoordinateTransformer` shall separate two transform spaces:
  1. **World/vector transform:** millimeters ↔ SVG user units/viewBox, without DPI.
  2. **Export raster transform:** millimeters → pixels using `px = mm / 25.4 × dpi`.
- **FR-011** (BR-002): The UI shall display cursor coordinates in millimeters and inches by inversely transforming screen coordinates into world millimeters.
- **FR-012** (BR-004): Changing Export DPI shall not change preview zoom, camera position, sharpness, or logical SVG size.
- **FR-013** (BR-005): Zoom shall be anchored at the cursor. The world point below the cursor shall remain unchanged before and after the zoom within display tolerance.

### 3.4. SVG vector preview

- **FR-014** (BR-004): `GerberSvgRenderer` shall convert `GerberScene` into valid SVG with a `viewBox` derived from combined bounds and configurable margin.
- **FR-015** (BR-004): SVG shall preserve geometry as vectors using `path`, `circle`, `rect`, `polygon`, `use`, `defs`, `mask`, or equivalent elements. The complete preview shall not be embedded as a base64 bitmap.
- **FR-016** (BR-004): Every Gerber layer shall be represented by a separate SVG group with a stable identifier so that the UI can change visibility, color, and opacity without reparsing the file.
- **FR-017** (BR-004): `LPC` polarity shall be applied within the owning layer through an SVG mask or equivalent composition. Clear geometry in one layer shall not cut through layers below it.
- **FR-018** (BR-004): `G36/G37` regions, including contours containing `G02/G03` arcs, shall be emitted as closed paths with a consistent and tested fill rule (`nonzero` or `evenodd` according to the selected geometry model).
- **FR-019** (BR-004): The SVG renderer shall reuse repeated geometry through `<defs>/<use>` when doing so reduces DOM size. `<use>` is not mandatory when a high instance count causes slower browser rendering.
- **FR-020** (BR-004): Generated SVG shall be self-contained and shall not reference external fonts, scripts, images, or network resources.

### 3.5. Vector zoom and re-rasterization

- **FR-021** (BR-005): Zoom and pan shall operate through camera/viewport transformations on the existing SVG. They shall not call the parser or rebuild `GerberScene` for every mouse event.
- **FR-022** (BR-005): During continuous wheel or drag input, the UI shall apply the transform immediately. Browser/WebView2 shall re-rasterize the SVG for the current zoom and `devicePixelRatio`.
- **FR-023** (BR-005): After zoom/pan input stops for a configurable debounce interval of 80–200 ms, the UI may perform **progressive refinement**, including exact viewport updates, culling, or regeneration of visible SVG geometry when required.
- **FR-024** (BR-005): For large scenes, the system shall support **viewport culling** based on bounding boxes and a spatial index. Only geometry intersecting an expanded viewport needs to be included at full detail.
- **FR-025** (BR-005): The system may use Level of Detail:
  - at low zoom, omit primitives below a sub-pixel threshold or merge geometry;
  - at high zoom, display complete primitives.
  LOD shall not change world dimensions or PNG export results.
- **FR-026** (BR-005): While progressive refinement is running, the canvas shall retain the current frame to avoid a white flash. Refined content shall replace the previous content atomically.
- **FR-027** (BR-005): Fit-to-view shall calculate the camera from combined millimeter bounds and shall not create a full-board bitmap.

### 3.6. PNG output configuration

- **FR-028** (BR-001): Export DPI shall be selectable as 150, 300, 600, or 1200. The UI label shall be **Export DPI** and shall not imply that this value controls preview DPI.
- **FR-029** (BR-001): Two color modes shall be provided:
  1. **Binary Mask:** white geometry on a black background, with inversion support.
  2. **Realistic:** PCB-style colors for copper, solder mask, silkscreen, and background.
- **FR-030** (BR-001): Users shall export selected layers individually or all visible layers as a composite PNG.
- **FR-031** (BR-001): PNG export shall use `GerberRasterRenderer` directly from `GerberScene`. It shall not capture a WebView2 screenshot or rasterize from the current canvas size.
- **FR-032** (BR-001): Export shall include the complete bounds and margin without clipping and shall validate bitmap dimensions and memory requirements before allocation.
- **FR-033** (BR-001): SVG preview and PNG export shall use the same `GerberScene`. Positional and dimensional differences between renderers shall remain within the test tolerance.

### 3.7. WinForms UI/UX

- **FR-034** (BR-002): The layout shall contain a top ToolStrip, a SplitContainer, a left layer-management panel, a right SVG preview, and a bottom StatusStrip.
- **FR-035** (BR-002): The ToolStrip shall include at minimum Open, Export DPI, Color Mode, Refresh Preview, Export Selected, Export Combined, and Fit.
- **FR-036** (BR-002): The canvas shall support cursor-anchored wheel zoom, drag pan, Fit-to-view, and zoom-factor display.
- **FR-037** (BR-002): Layer visibility, color, and opacity changes shall update the preview without reparsing.
- **FR-038** (BR-002): Parsing, `GerberScene` construction, expensive SVG generation, and PNG export shall run in the background using `Task.Run` or an equivalent worker. Control updates shall use `Invoke/BeginInvoke`.
- **FR-039** (BR-002): The UI shall display distinct states: Parsing, Building scene, Generating SVG, Refining viewport, Exporting PNG, and Ready.
- **FR-040** (BR-002): The SVG preview host shall block external navigation and popups and shall display only local or in-memory content generated by the application.

---

## 4. NON-FUNCTIONAL REQUIREMENTS (NFR)

- **NFR-001 — Parse/scene performance:** A layer containing up to 50,000 primitives shall be parsed and converted to `GerberScene` within 5 seconds on the reference office computer.
- **NFR-002 — PNG performance:** A layer containing up to 50,000 primitives on a board no larger than 100 × 100 mm shall export at 600 DPI within 10 seconds on the reference computer.
- **NFR-003 — Zoom responsiveness:** For a loaded scene of up to 50,000 primitives, camera-transform feedback after wheel/drag input shall begin within 50 ms. A refined frame shall be available within 300 ms when a complete scene rebuild is not required.
- **NFR-004 — DPI independence:** For the same camera and viewport, changing Export DPI between 150 and 1200 shall not change the SVG viewBox or world-to-screen transform.
- **NFR-005 — Build:** The application shall be built for x64.
- **NFR-006 — Robustness:** A malformed Gerber file shall return line-based warnings, render the valid remainder, and not crash.
- **NFR-007 — Core isolation:** `GerberEngine.dll` shall not reference `System.Windows.Forms`, WebView2, or the UI project.
- **NFR-008 — Determinism:** Identical input and render options shall produce geometrically equivalent SVG and a PNG with identical pixel dimensions.
- **NFR-009 — Memory:** Vector preview shall not allocate a full-board bitmap at Export DPI. Large bitmaps shall be allocated only during export and disposed after saving.
- **NFR-010 — Offline operation:** Parsing, preview, and export shall work without Internet access once the WebView2 Runtime is available.
- **NFR-011 — Security:** Generated SVG/HTML shall not contain unnecessary scripts, remote URLs, `foreignObject`, or active content derived from Gerber input.
- **NFR-012 — Compatibility:** The application shall support Windows display scaling at 100%, 125%, and 150%, including multiple monitors, using `PerMonitorV2`.

---

## 5. MANDATORY RENDERING DESIGN

### 5.1. Shared pipeline

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

Mandatory principles:

1. The parser shall not call drawing APIs.
2. A renderer shall not read raw Gerber text directly.
3. `GerberScene` shall be the single geometric source for SVG and PNG.
4. Preview DPI and Export DPI shall be separate concepts.
5. Camera operations shall not modify world geometry.

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

- SVG coordinates may directly use millimeters or a fixed user-unit scale, but physical measurements shall be preserved.
- `viewBox` defines the displayed world region.
- `devicePixelRatio` and monitor DPI affect only final rasterization and shall not change geometry.
- SVG shall not be regenerated because the user changes Export DPI.
- Partial SVG regeneration is allowed when viewport culling or LOD requires a different visible detail set.

### 5.3. Progressive vector rendering

The system shall divide interaction into two phases:

1. **Interactive transform**
   - Apply the matrix/viewBox change immediately.
   - Prioritize input responsiveness.
   - Retain current content during interaction.

2. **Refinement**
   - Run after debounce.
   - Query the spatial index for an expanded viewport.
   - Add or replace detailed geometry.
   - Allow browser/WebView2 to re-rasterize vectors at the current resolution.
   - Swap documents or layer groups without producing a white frame.

Official terminology in code and documentation:

- Vector zoom
- Browser re-rasterization
- Progressive refinement
- Viewport culling
- Level of Detail (LOD)
- Spatial index

Do not describe this mechanism as “loading a higher-DPI image” unless a future implementation actually uses a multi-resolution image tile pyramid.

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

DPI shall be used only in this pipeline and equivalent bitmap-export features.

---

## 6. WINFORMS 4.8 LIMITATIONS AND CONSTRAINTS

### 6.1. Platform limitations

1. **WinForms has no native retained-mode vector canvas:** SVG shall be hosted through WebView2 or an equivalent adapter. The vector preview shall not be forced into a fixed-DPI bitmap.
2. **WebView2 is a UI dependency:** no WebView2 type shall leak into `GerberEngine`.
3. **GDI+ remains CPU-only:** it shall be used for PNG export and bitmap fallback only.
4. **Bitmap limits:** a bitmap uses approximately 4 bytes per pixel. A 300 × 300 mm board at 1200 DPI is approximately 14,173 × 14,173 pixels and approximately 800 MB before overhead; allocation shall be validated.
5. **Single UI thread:** all WinForms/WebView2 updates shall return to the UI thread.
6. **Ownership:** SVG strings may be passed as immutable data; `Bitmap`, `Graphics`, `Pen`, `Brush`, and `GraphicsPath` shall be disposed.
7. **Monitor DPI:** use `PerMonitorV2` and `AutoScaleMode.Dpi`. Monitor DPI is not Export DPI.
8. **Mouse/keyboard focus:** the preview host shall receive wheel and keyboard shortcuts reliably.
9. **WebView2 initialization:** the application shall expose initializing/error/retry states and shall not call CoreWebView2 APIs before initialization completes.
10. **Navigation security:** every navigation not belonging to generated application content shall be cancelled.

### 6.2. UI structure in `.Designer.cs`

1. `MainForm.Designer.cs` shall contain only `InitializeComponent()`, control fields, and `Dispose(bool)`.
2. Parser logic, asynchronous workflows, SVG generation, and WebView2 business event logic shall not be placed in Designer files.
3. `GerberPreviewHost` shall be a separate UserControl/class encapsulating WebView2, initialization, navigation blocking, and a `SetSvgAsync` API.
4. Events may be wired in the Designer, but handler bodies shall remain in `MainForm.cs` or an appropriate controller/service.
5. Initialization shall use `SuspendLayout/ResumeLayout`.
6. SplitContainer, StatusStrip, and ToolStrip z-order shall be controlled explicitly.
7. The layer list may use a `ListView` with checkboxes and owner drawing; MVVM is not required.

---

## 7. SCOPE LIMITATIONS

- Excellon/NC Drill support is not mandatory unless drill data is supplied as Gerber.
- The application is read/view/export only; it does not edit or write Gerber.
- A very large SVG DOM may render slowly. Viewport culling and LOD become mandatory when performance thresholds are exceeded.
- High-DPI GDI+ export remains constrained by RAM and pixel count.
- Arcs shall remain vector arcs in `GerberScene`; they may be flattened only when a renderer requires it, using tolerance derived from world or screen scale.
- SVG preview is not CAM/fabrication proof; a dedicated reference test suite is required.
- Bitmap fallback may become blurry at deep zoom and is a fallback mode, not the primary preview path.

---

## 8. SOLUTION ARCHITECTURE AND API

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

### 8.1. Facade API

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

`SvgRenderOptions` shall not contain an export DPI property.

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

`DevicePixelRatio` shall only support LOD or screen-tolerance calculations and shall not alter world geometry.

---

## 9. TRACEABILITY

| Requirement | Implementation file | Suggested test |
|---|---|---|
| FR-001–FR-003, FR-009 | `GerberTokenizer.cs`, `GerberParser.cs` | Parse KiCad/Altium samples and malformed files |
| FR-005–FR-008 | `GerberSceneBuilder.cs`, `ApertureMacroProcessor.cs`, model classes | Compare primitives and bounds with references |
| FR-010–FR-013 | `CoordinateTransformer.cs`, `ViewportController.cs` | World/screen round-trip; zoom anchoring |
| FR-014–FR-020 | `GerberSvgRenderer.cs` | Validate XML/SVG; compare bounds; test polarity masks |
| FR-021–FR-027 | `GerberPreviewHost.cs`, `ViewportController.cs`, `PreviewRenderCoordinator.cs`, spatial index | Wheel/pan stress test; latency; no white flash |
| FR-028–FR-033 | `GerberRasterRenderer.cs`, `GerberEngineFacade.cs` | Export 4 DPI values × 2 color modes |
| FR-034–FR-040 | `MainForm.*`, `GerberPreviewHost.*` | UI test; thread test; navigation blocking |
| NFR-003, NFR-004 | Preview benchmarks and viewport tests | Measure latency; verify DPI independence |
| NFR-006 | Parser diagnostics | Inject malformed commands |
| NFR-007 | `GerberEngine.csproj` | Verify project references |
| NFR-009 | Memory test | Verify no Export-DPI preview bitmap |
| NFR-011 | SVG security test | Reject external URLs and active content |

No orphan requirement is allowed. Every FR/NFR shall trace to BR-001 through BR-005.

---

## 10. ACCEPTANCE CRITERIA

### 10.1. Gherkin scenarios

```gherkin
Scenario: SVG preview is independent of Export DPI
  Given the user loaded a Gerber layer and selected Fit-to-view
  And Export DPI is 150
  When the user changes Export DPI to 1200
  Then the SVG viewBox and camera remain unchanged
  And the logical board size in the preview remains unchanged
  And no 1200-DPI preview bitmap is created

Scenario: Vector zoom remains sharp
  Given the SVG preview displays the board
  When the user zooms twenty times into the same area
  Then the world point below the cursor remains stable
  And the browser re-rasterizes vector content for the new zoom
  And an enlarged screenshot is not used as the final preview

Scenario: Progressive refinement does not blank the canvas
  Given a large board requires viewport culling
  When the user pans rapidly to a new area
  Then the current frame is transformed immediately
  And refinement runs after debounce
  And new detailed geometry replaces the old content without a white frame

Scenario: SVG and PNG use the same geometry
  Given a layer contains flashes, lines, arcs, regions, LPD, and LPC
  When the system creates an SVG preview and a 600-DPI PNG
  Then their physical bounding boxes are equivalent
  And clear polarity affects only its owning layer

Scenario: Load and composite multiple layers
  Given the user drags .gtl, .gts, and .gto files into the application
  When all three layers are visible
  Then the SVG preview composites them in the correct order and color
  And the StatusStrip shows board dimensions in millimeters
```

### 10.2. Checklist

- [ ] The ToolStrip includes Open, Export DPI, Color Mode, Refresh Preview, Export Selected, Export Combined, and Fit.
- [ ] The primary preview is SVG vector content, not a bitmap generated at Export DPI.
- [ ] Changing Export DPI does not modify the preview.
- [ ] Wheel zoom is cursor-anchored and pan does not flicker.
- [ ] After deep zoom, lines and apertures are sharply re-rasterized by the browser.
- [ ] Layer visibility/color/opacity updates do not trigger reparsing.
- [ ] LPC does not cut through lower layers in either SVG or PNG.
- [ ] PNG export does not use a WebView2 screenshot.
- [ ] Large boards use viewport culling/LOD or show a clear threshold warning.
- [ ] The build target is x64 and the application uses `PerMonitorV2`.
- [ ] `GerberEngine.dll` references neither WinForms nor WebView2.
- [ ] SVG contains no external URLs or active content.
