# Tab 3 v2 Changed Files — Task 13

Date: 2026-07-22 UTC

## Files changed in this task

| File | Reason |
|---|---|
| `GerberViewer/Views/AlignStitchingControl.cs` | Completed end-to-end UI orchestration, application-owned `.creating` run directory handling, output validation/publish, cancellation cleanup, stale comparison clearing, conflicting-control disable/restore, processing report writing, and `WorkflowContext.LastStitchedOutputPath` update after successful publish only. |
| `docs/tab3_v2_test_report.md` | Records build/test matrix, real-data smoke status, manual procedure, and remaining risks. |
| `docs/tab3_v2_changed_files.md` | Records files changed and method-level scope for Task 13. |
| `docs/tab3_v2_task_results.md` | Records acceptance-criteria status and resource ownership summary. |
| `Log.html` | Adds timestamped Task 13 change log row. |

## Methods changed or added

### `GerberViewer/Views/AlignStitchingControl.cs`

- `btnRunAlignStitch_Click`: now validates inputs, creates `<output>/AlignStitch_<timestamp>/.creating`, runs the backend, rebuilds final states, generates comparison, writes report, validates outputs, publishes the final run directory, and updates `WorkflowContext` only after successful publish.
- `ValidateRunInputs`: blocks Run when manifest/captured/output state is stale or invalid.
- `CloneConfigForRun`: creates a run-scoped config snapshot whose `OutputPath` points at the current `.creating` directory while leaving the shared UI config pointing at the user-selected output root.
- `RebuildFinalStates`: applies the workflow final state set back to captured-image UI models by `OrderIndex`.
- `SetRunControlsEnabled`: disables conflicting controls while a run is active and restores them in `finally`.
- `WriteProcessingReport`: writes a structured `processing_report.json` into `.creating` before publish.
- `ValidateRunOutputs`: validates the stitched TIFF and comparison metadata before publish.
- `PublishRunDirectory`: moves all files/directories out of `.creating` into the final run directory.
- `CleanupCreatingDirectory`: deletes only the current application-owned `.creating` directory on cancellation/failure.

## Protected directories

No files under `reference/` or `EWindowControl/` were intentionally modified for this task.
