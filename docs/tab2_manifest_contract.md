# Tab 2 Manifest Contract

## JSON example

```json
{
  "ManifestVersion": 1,
  "RootDirectory": "C:/Output/GerberSample_20260721_120000_000",
  "SourceRasterPath": "C:/Input/sample.tif",
  "SourceWidth": 10000,
  "SourceHeight": 8000,
  "ProcessedWidth": 10000,
  "ProcessedHeight": 8000,
  "CropOrder": "Zigzag",
  "StartOrder": "TopLeftRight",
  "Tiles": [
    { "OrderIndex": 0, "Row": 0, "Column": 0, "ExpectedPath": "C:/Output/GerberSample_.../tiles/Sample_R00_C00_O000.png", "ExpectedX": 0, "ExpectedY": 0, "Width": 5000, "Height": 4000 }
  ]
}
```

## Field definitions

`ExpectedX` and `ExpectedY` are crop origins in processed-source pixel coordinates. `ExpectedPath` is an absolute deterministic path under `RootDirectory/tiles` and includes row, column, and order values from the same prepared layout used by preview and generation.

## Version and validation policy

`ManifestVersion` starts at `1`. Validation rejects null manifests, unsupported versions, missing roots, empty tile lists, duplicate or missing order indices, duplicate row/column pairs, negative coordinates, non-positive sizes, out-of-bounds tiles, and missing/unreadable tile files after generation.

## Locations

Tab 2 writer: `GerberStitching.Core/Imaging/SampleTileGenerator.cs`.
Shared contract and validator: `GerberStitching.Core/Models/SampleManifest.cs`.
Future Tab 3 reader: `GerberStitching.Core/Arrangement/CapturedImageLoader.cs` and alignment services consume the shared model.
