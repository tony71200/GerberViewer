# Tab 3 v2 Task Results — Task 13

Date: 2026-07-22 UTC

## Acceptance criteria status

| Criterion | Status | Evidence / note |
|---|---|---|
| Workflow UI nối với backend thật | Partially satisfied | `AlignStitchingControl` invokes `AlignStitchWorkflowService`, then comparison service, and binds the result. Runtime execution was not possible in this container. |
| Alignment, recovery, stitching and comparison use same final-state set | Satisfied by code path | Final states are rebuilt from `AlignStitchWorkflowResult.States`; stitching is produced by the workflow using those states; comparison consumes the resulting stitched TIFF. |
| Output only publishes after validation | Satisfied by code path | `processing_report.json`, stitched TIFF reopen, and comparison metadata validation occur before `.creating` contents are published. |
| Cancellation safe | Satisfied by code path; runtime not executed | Cancellation is checked before publish; cancellation deletes only the current `.creating` directory and does not update `LastStitchedOutputPath`. |
| Debug x64 build | Not verified | `msbuild` is unavailable in this environment. |
| Release x64 build | Not verified | `msbuild` is unavailable in this environment. |
| Synthetic tests pass | Not verified here | Test harness executable cannot be produced without MSBuild. |
| Real-data smoke evidence | Not available | No real dataset/HALCON license/runtime is available here; exact manual procedure is in `docs/tab3_v2_test_report.md`. |
| Tab 1 and Tab 2 regression smoke | Not verified here | Requires Windows WinForms runtime/manual smoke. |
| Report includes files/methods/build/test/resource ownership/risks | Satisfied | See this file plus `docs/tab3_v2_changed_files.md` and `docs/tab3_v2_test_report.md`. |

## Resource ownership

- `CancellationTokenSource`: owned by `AlignStitchingControl` for the active run and disposed in `finally`.
- `.creating` directory: owned only by the current run; cleanup deletes only this directory on cancellation/failure.
- Matcher/HALCON/OpenCV resources: owned by Core matchers/stitch/comparison services and disposed by their implementations; Task 13 did not move matcher ownership into UI.
- Bitmap comparison previews: owned by `SampleComparisonControl` after cloning at the UI boundary and disposed by clear/replace/dispose.
- Stitched TIFF: published only after reopen validation and comparison metadata validation.

## Remaining risks

- Build and synthetic tests still require Windows x64 MSBuild/.NET Framework runtime.
- Real-data smoke still requires a real manifest/captured folder with four or more tiles.
- HALCON NCC runtime smoke still requires HALCON runtime and license.
- Tab 1/Tab 2 smoke must be executed manually on Windows; no source under their protected external dependencies was changed in this task.
