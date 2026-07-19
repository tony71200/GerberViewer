# DPI Dependency Audit

Date: 2026-07-19

## Summary

The implementation still uses a WinForms/GDI+ bitmap preview fallback, but the preview no longer reads or derives scale from the Export DPI ComboBox. Export DPI is now isolated in `RasterExportOptions` and export-only facade methods.

## Keyword audit

| Keyword / area | File | Classification | Action |
| --- | --- | --- | --- |
| `Dpi` in `RasterExportOptions` | `GerberEngine/GerberEngine.cs` | Export-only | Kept as PNG export configuration. |
| `Dpi` in `CoordinateTransformer` | `GerberEngine/CoordinateTransformer.cs` | Export-only metadata for DPI transformer; `0` for viewport fallback | Added `FromViewport(...)` constructor path so preview can use control pixel size without Export DPI. |
| `RenderPreviewAsync` | `GerberViewer/MainForm.cs` | Preview workflow | Changed to build `ViewportBitmapOptions`, not `RasterExportOptions`; it does not read `tscDpi`. |
| `tscDpi` / Export DPI ComboBox | `GerberViewer/MainForm.cs`, `GerberViewer/MainForm.Designer.cs` | Export-only UI setting | Read only in `BuildExportOptions()`, which is used by PNG export commands. No change handler refreshes preview. |
| `Bitmap`, `DrawImage`, `OnPaint` | `GerberViewer/GerberCanvas.cs` | Viewport bitmap fallback | Still used for current fallback preview, but bitmap dimensions come from the control viewport rather than Export DPI. |
| `Graphics.FromImage`, `DrawImage`, renderer bitmap allocation | `GerberEngine/GerberRenderer.cs` | Rasterization | Used by export and viewport fallback renderer paths. Allocation guard remains in renderer. |
| `mm / 25.4` | `GerberEngine/CoordinateTransformer.cs`, `GerberViewer/MainForm.cs` | Export transform / coordinate display | DPI conversion remains for export; inch display divides millimeters by 25.4 and is not used for preview scaling. |
| `MouseWheel` | `GerberViewer/GerberCanvas.cs` | UI camera | Zoom changes control transform only; it does not re-render through Export DPI. |

## Preview call graph after change

```text
LoadFiles / layer visibility / color / ordering
    -> RenderPreviewAsync(fit)
    -> BuildPreviewOptions()
       - ViewportWidthPx = canvas.ClientSize.Width
       - ViewportHeightPx = canvas.ClientSize.Height
       - WorldViewportMm = combined bounds + margin
       - no Export DPI read
    -> worker thread
       -> GerberEngineFacade.CreateViewportTransformer(...)
       -> GerberEngineFacade.RenderViewportBitmap(...)
    -> canvas.SetImage(...)
```

## Export call graph after change

```text
Export selected / export combined
    -> BuildExportOptions()
       - Dpi = tscDpi selected value
    -> ExportLayerPng / ExportCombinedPng
    -> RenderLayerForExport / RenderCombinedForExport
    -> CoordinateTransformer(bounds, exportDpi, margin)
    -> PNG SavePng(..., dpi)
```

## `q168.gbr` load-check target

The repository includes `q168.gbr`. The intended validation is to parse it with `GerberEngineFacade.LoadLayer("q168.gbr")`, confirm non-empty bounds, then render a viewport bitmap using `ViewportBitmapOptions`. This environment does not include MSBuild, xbuild, dotnet, csc, or mcs, so the check is documented but cannot be executed here.
