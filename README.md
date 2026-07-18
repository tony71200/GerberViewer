# Gerber Viewer

A Windows Forms (.NET Framework 4.8) Gerber viewer and PNG converter with a reusable `GerberEngine` class library.

## What changed for the Online Gerber Viewer-style spec

This branch implements the currently available WinForms/GDI+ path while moving the UI closer to the reference at <https://onlinegerberviewer.com/>:

- Dark CAD-style workspace with grid, gold Gerber artwork colors, dark layer sidebar, top toolbar, and bottom coordinate/status strip.
- Multi-file open and drag-and-drop loading.
- Layer visibility, per-layer color changes, remove, and reorder actions without reparsing unchanged layers.
- Cursor-anchored wheel zoom, drag pan, fit-to-view, zoom percentage, and cursor coordinates in millimeters/inches.
- Export DPI is explicitly separated from preview rendering. Preview rendering uses a capped internal DPI for responsiveness, while PNG export uses the selected 150/300/600/1200 DPI.
- Large-scene status warnings are shown when a loaded set reaches the 50,000-primitive performance threshold.
- All changed functions and variables are documented in `Log.html`.

> Note: `Spec_CSharpWinform_EN.md` defines a future mandatory SVG/WebView2 preview architecture. This branch improves the existing raster-backed WinForms preview and preserves PNG export. It does not yet add WebView2/SVG files such as `GerberPreviewHost.cs` or `GerberSvgRenderer.cs`.

## Projects

```text
GerberViewer.sln
├── GerberEngine/   # Parser, geometry model, coordinate transform, GDI+ rendering/export facade
└── GerberViewer/   # WinForms UI, layer management, preview canvas, export workflows
```

## Requirements

- Windows
- Visual Studio 2019/2022 or MSBuild with .NET Framework 4.8 targeting pack
- x64 build configuration is recommended for large PNG exports

## Build

```powershell
msbuild GerberViewer.sln /p:Configuration=Release /p:Platform=x64
```

## Run

Open `GerberViewer.sln` in Visual Studio and start the `GerberViewer` project, or run the built executable from:

```text
GerberViewer\bin\x64\Release\GerberViewer.exe
```

## Basic usage

1. Click **Open** or drag one or more Gerber files onto the window.
2. Use the left **Layers** list to toggle visibility. Double-click a layer, or use the context menu, to change its color.
3. Use the mouse wheel to zoom at the cursor, drag to pan, or click **Fit**.
4. Select **Export DPI** and **Color Mode**.
5. Export checked layers with **Export Selected** or all visible layers with **Export Combined**.

## Performance notes

- Preview rendering intentionally uses a fixed internal preview DPI so changing **Export DPI** does not allocate a high-DPI preview bitmap.
- PNG export validates bitmap dimensions before allocation through `GerberRenderer.MaxPixels`.
- For very large boards or 1200 DPI exports, use x64 builds and export individual layers if memory pressure is high.
