# RobotOrderer Refactor — Complete & Verify

## Goal Description

The ImageGrid refactor (Steps 1–4 from [ImageGrid_Refactor.md](file:///h:/005_Project/2601_StitchingProj/PLP/ImageGrid_Refactor.md)) is already fully implemented. The remaining work is:

1. **Verify correctness** — Add exhaustive test cases matching all 6 illustration images (M1, M2, P1–P4) to the existing [VerifyImageGridGraphParity()](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/RobotOrderer.cs#847-997) method. This covers 8 Manual combinations × 3 modes = 24 Manual cases + 8 Position corner/movement × 3 modes = 24 Position cases.
Note that: Some notes regarding the image:
1.1. The blue arrow indicates the image arrangement direction (depending on StartCorner and direction, Position if any). The red arrow indicates the stitching direction (traversal).
1.2. In P*.png, the symbols (0-0) follow the format <ImageId>-<PositionId>
1.3 The image in M*.png, P*.png are just for reference, the actual image may be different. The important thing is the graph structure.
2. **Fix bugs** — Correct any graph generation mismatches discovered during verification.
3. **Update PathCanvasControl** — Adjust node label rendering to match the illustration format (show `traversalIndex` for Manual mode; show `ImageId-PositionId` for Position mode).
4. **Clean up stale references** — Remove all `OrderMode.Snake` documentation references since the enum is now `OrderMode.Zigzag`.

---

## Proposed Changes

### Verification & Algorithm

#### [MODIFY] [RobotOrderer.cs](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/RobotOrderer.cs)
- Expand [VerifyImageGridGraphParity()](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/RobotOrderer.cs#847-997) with test cases derived from illustrations:
  - **M1 cases** — Manual TopLeft→Right (Col=5), TopLeft→Down (Row=4) for Zigzag/Branch/BranchDown
  - **M2 cases** — Manual BottomLeft→Right (Col=5), BottomLeft→Up (Row=4) for Zigzag/Branch/BranchDown
  - **P1 cases** — Position TopLeft→Right Zigzag/Branch/BranchDown
  - **P2 cases** — Position TopLeft→Down Zigzag/Branch/BranchDown
  - **P3 cases** — Position BottomLeft→Right Zigzag/Branch/BranchDown
  - **P4 cases** — Position BottomLeft→Up / BottomLeft→Down Zigzag/Branch/BranchDown
- Fix any graph bugs uncovered by the new test cases
- Implement for other cases if any
[Important] The code can be completely modified as long as it meets the stated requirements. External parts related to RobotOrderer.cs can be changed accordingly.
---

### UI Visualization

#### [MODIFY] [PathCanvasControl.cs](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/DesignControls/PathCanvasControl.cs)
- In [DrawComponentNodes()](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/DesignControls/PathCanvasControl.cs#227-255), adjust the label format:
  - For virtual layout (manual mode): show the 0-based traversal index only
  - For non-virtual with [PositionId](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/RobotOrderer.cs#571-584): show `ImageId-PositionId` (matching illustration `X-Y` format)

---

### Documentation Cleanup

#### [MODIFY] [RobotOrderer.md](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/RobotOrderer.md)
- Replace all `OrderMode.Snake` references with `OrderMode.Zigzag`
- Remove the "Temporary terminology note" at the top

#### [MODIFY] [Domain.cs](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/Domain.cs)
- Update XML `<summary>` doc comments that reference `OrderMode.Snake`

---

## Verification Plan

### Automated Tests

Run the internal graph verification suite by invoking [VerifyImageGridGraphParity()](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/RobotManager/RobotOrderer.cs#847-997) at application startup (in DEBUG) and checking it returns zero failures.

**How to run:**
1. Add a call to `RobotOrderer.VerifyImageGridGraphParity()` in [MainForm_Load](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/MainForm.cs#578-585) under `#if DEBUG`
2. If the returned list has any entries, display them via `Logger.Warning()` or `MessageBox`
3. Build in Debug: `msbuild PLP.sln /t:Build /p:Configuration=Debug`
4. Run the application — verification passes silently if no failures are logged

### Manual Verification
1. Open the project in Visual Studio
2. Build (`Debug` configuration) and run
3. Load a test image folder
4. For each ScanDirection × OrderMode combination, compare the [PathCanvasControl](file:///h:/005_Project/2601_StitchingProj/PLP/StitchingImage/Stitch_Tools/DesignControls/PathCanvasControl.cs#21-29) visualization to the corresponding illustration (M1, M2, P1–P4)
5. Verify node labels show the correct format
