# Tab 2 Changed Files

| File | Class/method | Purpose | Before | After |
|---|---|---|---|---|
| `GerberStitching.Core/Configuration/GerberSampleConfig.cs` | `GerberSampleConfig` | Stabilize JSON persistence | Config was a plain POCO including runtime-only `PadColor` | Persisted fields are marked with data-contract attributes and `PadColor` is ignored. |
| `GerberStitching.Core/Configuration/AppPaths.cs` | `AppPaths.SampleConfigPath` | Centralize fixed config path | No fixed path helper | Uses `<AppBase>/Config/gerber_sample_config.json`. |
| `GerberStitching.Core/Configuration/SampleConfigStore.cs` | `SampleConfigStore` | Move config persistence out of UI | Load Config did not read JSON | Load/create/save fixed-path JSON with temp file/readback and invalid backup. |
| `GerberStitching.Core/Imaging/SampleGridGeometry.cs` | `SampleGeometryCalculator.Calculate` | Share deterministic grid with UI | Geometry was private to crop generator | UI can calculate tile rectangles and order indices immediately. |
| `GerberViewer/Views/CreateGerberSampleControl.Designer.cs` | `InitializeComponent` | Add fixed Save Config UI | No Save Config button; absolute positions | Table layout with Open, Path, Load, Save, Create, Cancel, Progress, Status. |
| `GerberViewer/Views/CreateGerberSampleControl.cs` | Load/open/save/create handlers | Wire fixed flow | Path-only sample load, no config IO, no overlay | Startup/load/save use store; sample image is decoded/displayed; grid overlay appears; completed tiles turn green. |
| `GerberStitching.Core/Imaging/SampleTileGenerator.cs` | `Generate` catch block | Avoid swallowed exception | Empty catch around incomplete marker | Marker write failures are surfaced with context. |
| `GerberStitching.Core/GerberStitching.Core.csproj` | Compile items | Include new services | New files not compiled | New config store/path and geometry files included. |
| `Log.html` | Tab 2 rows | Update program log | Did not mention this pass | Documents config/store/UI/display/grid changes. |
| `Params.html` | Tab 2/Core rows | Update parameter docs | Listed old Load/Create behavior | Documents fixed config path, Save button, decoded sample image, and tile state. |

| `EWindowControl/EWindowControl.cs` | `SetSourceImage`, `RenderImageOverlay` | Support large TIFF/BigTIFF preview | Tab 2 had to convert selected rasters through `Bitmap` | Viewer accepts HALCON `HObject` source and draws overlays in image coordinates. |
| `GerberViewer/Views/CreateGerberSampleControl.cs` | `btnOpenSample_Click`, `ReplaceSampleImage`, `RenderGridOverlay` | Use HALCON input path | `ImageRead.ReadBitmap` failed on large TIFF with `Parameter is not valid` | `HOperatorSet.ReadImage` loads sample, `GetImageSize` drives geometry, overlays render without cloning a huge bitmap. |
