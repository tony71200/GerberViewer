# Tab 2 Baseline Audit

## Call flow before this fix

1. `MainForm.Designer.cs` creates `createGerberSampleControl` inside `tabCreateGerberSample`.
2. `MainForm.cs` assigns the shared `WorkflowContext` to `createGerberSampleControl`.
3. `CreateGerberSampleControl` constructed a single in-memory `GerberSampleConfig` and bound it to `sampleConfigGrid`.
4. `btnOpenSample_Click` opened a raster dialog, stored only the selected path in `_sampleConfig`, `txtSamplePath`, and `WorkflowContext`.
5. `btnLoadSampleConfig_Click` did not read JSON; it simply rebound the in-memory `_sampleConfig`.
6. `btnCreateSample_Click` asked for an output folder and ran `SampleTileGenerator.GenerateAsync` with progress updates.
7. `sampleWindow` was initialized in the designer, but no source image was assigned by Tab 2.
8. Grid calculation happened privately inside `SampleTileGenerator.BuildTiles`, so the UI could not render the grid immediately after image load.
9. Overlay output was only saved as `sample_overlay.png` during generation; no red pending grid was displayed in the viewer.
10. Progress was based on completed count and status text only; individual tile state was not stored in the UI.
11. Manifest was written at the end of `SampleTileGenerator.Generate` and then published to `WorkflowContext` on success.

## Root causes found

- TIFF/PNG/BMP selection only updated the path and never decoded or assigned an image to `sampleWindow`.
- Load Config did not use the fixed `Config/gerber_sample_config.json` path and did not deserialize JSON.
- There was no Save Config button.
- Grid geometry was private to the crop generator instead of reusable by the UI.
- Pending/completed state per tile was missing from Tab 2, so the UI could not turn individual rectangles green.
- The generator had an empty catch while attempting to mark incomplete output.

## Baseline build

- Build command attempted: `msbuild GerberViewer.sln /p:Configuration=Debug /p:Platform=x64 /m`.
- Result in this container: not runnable because `msbuild` is not installed.
