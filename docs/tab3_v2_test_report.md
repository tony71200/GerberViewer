# Tab 3 v2 End-to-End Test Report

Date: 2026-07-22 UTC
Task: Task 13 — end-to-end workflow integration and practical verification
Branch observed in this environment: `work`

## Scope verified by static inspection

The Tab 3 UI now orchestrates the production backend in the required stage order:

1. validate loaded manifest before Run,
2. validate captured folder/image count before Run,
3. create an application-owned `<output>/AlignStitch_<timestamp>/.creating` directory,
4. run direct alignment through `AlignStitchWorkflowService`,
5. run workflow recovery inside `AlignStitchWorkflowService`,
6. rebuild final UI states from the returned `TileWorkflowState` collection,
7. stitch using the same final state set,
8. generate comparison output with `SampleComparisonService`,
9. bind comparison result to the comparison tab,
10. write `processing_report.json`,
11. reopen and validate stitched TIFF plus comparison metadata,
12. publish by moving files from `.creating` to the final run directory,
13. update `WorkflowContext.LastStitchedOutputPath` only after publish succeeds.

Cancellation behavior is guarded between major stages. If cancellation is requested before publish, the current `.creating` directory is deleted, completed output is not published, and `LastStitchedOutputPath` is not updated.

## Build matrix attempted

| Check | Command | Result | Evidence / reason |
|---|---|---|---|
| Debug x64 build | `msbuild GerberViewer.sln /p:Configuration=Debug /p:Platform=x64 /m` | Not run successfully | `msbuild` is not installed in this Linux container. |
| Release x64 build | `msbuild GerberViewer.sln /p:Configuration=Release /p:Platform=x64 /m` | Not run successfully | `msbuild` is not installed in this Linux container. |
| Lightweight test harness | `./GerberStitching.Tests/bin/x64/Debug/GerberStitching.Tests.exe` | Not run | The executable cannot exist until the Debug x64 build succeeds. |
| Static whitespace check | `git diff --check` | Passed | No whitespace errors were reported. |
| Project XML parse | Python `xml.etree.ElementTree` parse of project/resx files | Passed | Project and resx XML parsed successfully. |

## Required test matrix status

| Required test | Status | Reason / notes |
|---|---|---|
| manifest v1 | Not run in this container | Requires building/running the .NET Framework test harness. |
| manifest v2 | Not run in this container | Requires building/running the .NET Framework test harness. |
| natural sort 1,2,10 | Not run in this container | Covered by loader/test sources, but not executed here. |
| count mismatch | Not run in this container | Covered by validation path, but not executed here. |
| duplicate OrderIndex | Not run in this container | Manifest validator path exists, but not executed here. |
| direct alignment | Not run in this container | Requires OpenCvSharp/HALCON-capable Windows x64 runtime. |
| neighbor recovery | Not run in this container | Requires built test harness/runtime images. |
| rotation stitching | Not run in this container | Requires OpenCvSharp native runtime and build. |
| output reopen | Static path reviewed; runtime not run | `GlobalTransformStitcher` and UI publish validation both reopen output before publish/update. |
| comparison modes | Not run in this container | UI test source exists, but WinForms runtime tests were not executed here. |
| cancellation | Static path reviewed; runtime not run | `.creating` cleanup and no-context-update path added. |
| Form close | Not run in this container | Requires Windows WinForms UI smoke environment. |
| real-data smoke | Not run | No real manifest/captured folder and no HALCON license/runtime were available in this container. |
| Tab 1 smoke | Not run | Requires Windows WinForms runtime/manual UI. |
| Tab 2 smoke | Not run | Requires Windows WinForms runtime/manual UI. |

## Exact manual real-data smoke procedure

Run this on a Windows x64 machine with Visual Studio/MSBuild, OpenCvSharp native runtime, and HALCON runtime/license installed:

1. Build `GerberViewer.sln` Debug x64.
2. Build `GerberViewer.sln` Release x64.
3. Start `GerberViewer` Debug x64.
4. Tab 1 smoke: open a known Gerber/raster input and verify existing preview/load behavior still works.
5. Tab 2 smoke: generate a sample manifest with at least 2x2 tiles and confirm `sample_manifest.json`, tile images, processed sample metadata, and manifest v2 fields are written.
6. Tab 3: load the generated/known real `sample_manifest.json`.
7. Select a captured image folder with at least four real camera tiles matching the manifest order.
8. Select an empty output root directory.
9. Run Align/Stitch.
10. Verify controls are disabled during Run and double Run is ignored.
11. Confirm progress updates appear without cross-thread exceptions.
12. If a manual review dialog is triggered, confirm it opens on the UI thread.
13. Confirm direct alignment and recovery diagnostics are populated.
14. Confirm `stitched.tif` exists only after publish in `<output>/AlignStitch_<timestamp>/`.
15. Reopen `stitched.tif` in an external image viewer.
16. Confirm `processing_report.json` exists in the published run directory.
17. Confirm `comparison/comparison_metadata.json` and comparison preview PNGs exist.
18. Verify the comparison tab binds the result and all comparison modes switch without `ObjectDisposedException`.
19. Start a second run and cancel during alignment or stitching; confirm only that run's `.creating` directory is removed, no final output is published, and `LastStitchedOutputPath` still points to the previous successful run.
20. Close the form during/after a run and confirm no cross-thread, timer, or disposed-bitmap exceptions.

## Remaining risks

- This environment cannot prove actual matcher/stitch/comparison runtime success because the solution targets .NET Framework/WinForms and requires Windows x64 runtime components.
- HALCON-dependent NCC smoke tests remain license/runtime gated.
- Real-data smoke evidence is still required from a machine with a real manifest and captured folder.
