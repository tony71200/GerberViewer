# Tab 2 Image Ownership

| Resource | Owner | Created by | Replaced by | Disposed by | UI/Worker |
|---|---|---|---|---|---|
| Selected decoded HObject | CreateGerberSampleControl | HALCON `ReadImage` in background open task | `ReplaceSampleImage` | `DisposeSampleSource` / next replacement | Worker create, UI owner |
| Prepared source copy | PreparedSampleRun | `SamplePreparationService.Prepare` via `CopyImage` | New prepared run | `PreparedSampleRun.Dispose` | Worker create, UI owner |
| Prepared processed HObject | PreparedSampleRun | `SamplePreparationService.Prepare` | New prepared run | `PreparedSampleRun.Dispose` | Worker create, UI owner/generator read |
| Viewer copy | GerberSampleWindow/base EWindowControl | `SetSourceImage` via `CopyImage` | Next `SetSourceImage` | Base `ClearImage` / dispose | UI |
| Crop tile HObject | SampleTileGenerator | HALCON `CropRectangle1` | Per tile | `using` scope | Worker |
| Output temp directory | SampleTileGenerator | `.creating_<runId>` with marker | Per run | Marker-guarded cleanup on failure/cancel | Worker |
