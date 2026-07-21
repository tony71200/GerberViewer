# Tab 2 Fix Report

This report was generated during the Tab 2 canonical pipeline task on 2026-07-21.

## Status

Implemented core corrective slice for Tasks 0, 1, 3, 5, 6, 7, 8, 9, 10, and 11:

- Baseline audit identified split preview/generator paths, full-image Bitmap reread in `SampleTileGenerator`, recursive deletion of the selected output directory, manifest shape mismatch, UI-thread decode/preparation, and reflection in `GerberSampleWindow`.
- Added one shared `SampleManifest` / `SampleTileInfo` contract with `ManifestVersion = 1` and validation/readback serializer.
- Added `PreparedSampleRun` and `SamplePreparationService` to own copied HALCON source/processed images and one layout.
- Refactored generation to consume `PreparedSampleRun`, crop from HALCON `ProcessedImage`, verify each saved tile, and publish a manifest only after validation.
- Replaced selected-root deletion with `.creating_<runId>` temp folders, `.gerber_sample_run` marker, and final `GerberSample_<runId>` publication.
- Updated preview/create UI to prepare in background and reuse the prepared run.
- Updated `GerberSampleWindow` to use inherited public APIs instead of reflection, with wheel zoom and pan enabled.

## Build/test note

The Linux container does not provide `msbuild`, `xbuild`, `dotnet`, or HALCON/OpenCvSharp native test tooling, so Debug/Release builds and HALCON runtime tests could not be executed here. `git diff --check` passed.
