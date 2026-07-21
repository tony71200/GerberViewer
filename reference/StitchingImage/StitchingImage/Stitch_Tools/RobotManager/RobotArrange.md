# RobotArrange Guide (Agent Rules)

**Audience:** coding agents (CodeX / Claude / Copilot) who might modify the arranging logic.  
**Non‑negotiable:** any change **must preserve the invariants and test cases** listed here.

---

## 1) High-level purpose

`RobotArrange` converts an unordered `List<ImageInfo>` into:
1) A **2D layout matrix** (`List<List<ImageInfo>>`) per connected component / lane-group, and  
2) A **deterministic traversal graph** (`ArrangeGraph` with `Prev/Next`) plus a **drawable path** (`List<PathSegment>`).

This is used to:
- determine **how the robot (or stitch pipeline) should traverse images**, and
- provide a stable data model for **UI visualization** (grid + arrows). fileciteturn4file2turn4file8

---

## 2) Related files

### Core files
- `Domain.cs`
  - Defines `ImageInfo`, `OrderOptions`, enums, and output DTOs (`ArrangeBatchResult`, `ArrangeComponent`, `ArrangeGraph`, `PathSegment`, etc.). fileciteturn4file4turn4file8
- `RobotArrange.cs`
  - Main arranging implementation (`RobotArrange.Arrange`) and internal algorithms (Position/Coordinates/Manual + graph traversal). fileciteturn4file2

### Debug-only helper (in the same file)
- `DebugVisualizeArrange`
  - Console printing of matrices and optionally traversal debug. fileciteturn4file3turn4file5

### Historical / reference (optional)
- `RobotOrderer.cs` contains older parity/verification combos you can reuse to build tests. fileciteturn4file1

---

## 3) Input model and configuration

### ImageInfo
Each image is represented by:
- `ImageId` (unique within a group/run)
- Optional `PositionId` (robot-provided order index)
- Optional `XRobot`, `YRobot` (robot coordinate metadata)
- `GroupId`, `FilePath` fileciteturn4file0turn4file4

### OrderOptions (key fields)
- `StartCorner`: TopLeft / TopRight / BottomLeft / BottomRight
- `RobotMovement`: Left / Right / Up / Down
- `ClusterOrder`: Coordinates / Position / ManualRow / ManualColumn (legacy and business choice)
- `FlyNodeInterval`: chunk size used by Manual fallback
- `GapFactor`, `RowFactor`, `InvertXOnParse` for coordinate clustering fileciteturn4file4

---

## 4) Rules to arrange `List<ImageInfo>` (priority + modes)

### 4.1 Metadata priority selection (must not change)
Inside `RobotArrange.Arrange`:
1) **Position mode** if **all** images have `PositionId`
2) else **Coordinates mode** if **all** images have valid `XRobot` & `YRobot`
3) else **Manual fallback** (uses `FlyNodeInterval`) fileciteturn4file2

### 4.2 Position mode rule (special: TWO lane-groups)
If Position metadata exists (`PositionId` is present for all images):
- The algorithm intentionally creates **exactly two lane-group matrices**:
  - `BuildPositionTwoGroupMatrices(images, opt)` returns `[G0, G1]`
  - Rows are first derived from the `ImageId` stream (robot capture order), then grouped into **stripes** by `(minPos, maxPos)`.
  - When stripe direction toggles (increasing vs decreasing), the assignment flips (`flip = !flip`) to keep lanes consistent. fileciteturn4file2turn4file0

**Important invariants (Position):**
- Output components count must be **2** for a pure Position dataset, unless you explicitly redesign the business meaning (do not do silently).
- Row creation must treat `PositionId` deltas `±1` as continuous movement; sign changes indicate row-turns. fileciteturn4file2
- `NormalizePositionMatrix` may transpose/flip the final matrix to respect `StartCorner` and movement direction. It must not reorder items in a way that breaks stripe/lane logic. fileciteturn4file2

### 4.3 Coordinates mode rule
If coordinates exist for all images:
- Use `EstimatetypicalStep` (median nearest-neighbor step) to derive clustering scale.
- Cluster by the secondary axis (`RowFactor * typicalStepSecondary`) to form rows (or columns).
- Sort within each row by primary axis and apply zigzag direction.
- Apply `StartCorner` row ordering. fileciteturn4file2

**Coordinate invariant:**
- `InvertXOnParse` must be applied consistently for primary/secondary axis comparisons. fileciteturn4file4turn4file6

### 4.4 Manual fallback rule
If neither Position nor Coordinates are available:
- Use `FlyNodeInterval` to chunk `ImageId`-sorted images into **lines**.
- If movement is horizontal (Right/Left), lines are **rows**.
- If movement is vertical (Up/Down), lines are **columns** (then rendered as rows for visualization).
- Do **not** stack extra reversals; only one place should decide direction (avoid “double-reverse” bugs). fileciteturn4file2

---

## 5) Outputs: what they mean and how to use them

`ArrangeBatchResult`
- `TypicalStep`: `(StepX, StepY)` (only meaningful if coordinates exist)
- `Components`: list of `ArrangeComponent` fileciteturn4file8turn4file2

`ArrangeComponent`
- `Items`: flattened `ImageInfo` list contained in this component
- `Matrix`: `List<List<ImageInfo>>` layout (may be jagged; trailing cells may be missing)
- `OptionsUsed`: resolved options for this component
- `Graph`: `ArrangeGraph` containing `Prev/Next` + optional `LineNext/InterLineNext`
- `CellById`: map `ImageId -> (Row, Col)` for drawing
- `Path`: ordered `PathSegment` list for rendering arrows
- `StartId`: traversal start node id (the first node with `Prev == null`) fileciteturn4file8turn4file2

`ArrangeGraph`
- `ById[imageId].Prev`: previous node in traversal, or null at start
- `ById[imageId].Next`: next node in traversal, or null at end
- `LineNext`: next node in the current row/column traversal line
- `InterLineNext`: link between lines (end-of-line to next line head) fileciteturn4file7turn4file9

`PathSegment`
- `FromId`, `ToId`: consecutive traversal nodes
- `FromCell`, `ToCell`: grid cells used for drawing
- `Direction`: computed arrow direction (Left/Right/Up/Down) or `Jump` if not adjacent fileciteturn4file8turn4file2

### UI usage contract (must not break)
If an agent modifies traversal rules:
- `CellById` + `Path` must remain valid and consistent (every segment endpoints must exist in `CellById`).
- `Path.Count == Items.Count - 1` for a connected traversal (no missing nodes).
- `StartId` must match the first segment’s `FromId` when `Path.Count > 0`. fileciteturn4file2turn4file8

---

## 6) Debug-only code (must not affect production behavior)

`DebugVisualizeArrange` is a **debug helper** used to print matrices and (optionally) traversal.  
Rules:
- Do not add side effects (no mutation) into debug printers.
- Debug printing may be expanded, but must never change `RobotArrange` logic. fileciteturn4file3turn4file5

---

## 7) Mandatory test cases (logic regression suite)

Every modification must run the following checks. If any check changes, the agent must:
1) explain why, and
2) update expected outputs intentionally.

### 7.1 Manual fallback regression (no PositionId, no X/Y)

**Dataset A:** `ImageId = 0..54`, `FlyNodeInterval = 11`.  
Run 8 cases:
- (TopLeft, Right)
- (TopLeft, Down)
- (TopRight, Left)
- (TopRight, Down)
- (BottomLeft, Right)
- (BottomLeft, Up)
- (BottomRight, Left)
- (BottomRight, Up)

**Expected invariants:**
- `StartId == 0` for all 8 cases (start corner is where `ImageId=0` must appear).
- For Up/Down, traversal must be **column-based**: majority of `PathSegment.Direction` must be `Up/Down`, not `Left/Right`.
- `Path.Count == 54` and all `ImageId` appear exactly once in the traversal.  
(Use `DebugVisualizeArrange.PrintArrangeResult` to inspect matrices quickly.) fileciteturn4file3turn4file2

**Dataset B (large):** `ImageId = 0..314` (315 items), `FlyNodeInterval = 21`.  
Run the same 8 cases. Expected invariants identical (especially for `TopRight+Left` and `BottomRight+Left`—these historically exposed “double-reverse” bugs). fileciteturn4file2

### 7.2 Position mode regression (two-lane grouping)

**Dataset C:** the canonical 2-group example:
```
0-0, 1-1, 2-2, 3-3, 4-4,
5-0, 6-1, 7-2, 8-3, 9-4,
10-9, 11-8, 12-7, 13-6, 14-5,
15-9, 16-8, 17-7, 18-6, 19-5,
20-10, 21-11, 22-12, 23-13, 24-14,
25-10, 26-11, 27-12, 28-13, 29-14
```
Run at least `(TopLeft, Right)` and verify **two components** are produced and lane grouping is stable:

Expected shape for `(TopLeft, Right)` (G0/G1):
- G0 rows: `[0 1 2 3 4]`, `[19 18 17 16 15]`, `[20 21 22 23 24]`
- G1 rows: `[5 6 7 8 9]`, `[14 13 12 11 10]`, `[25 26 27 28 29]`

Then run all 8 cases and verify invariants:
- `Components.Count == 2`
- `Items` across both components cover exactly 30 unique ids
- `StartId` aligns with the chosen `StartCorner` after `NormalizePositionMatrix`. fileciteturn4file2turn4file0

### 7.3 Coordinates mode regression (basic)
Use any small synthetic grid with valid `(XRobot, YRobot)` (e.g., 4x5):
- Verify `TypicalStep` is finite and stable across runs.
- Verify no node duplication and `Path.Count == Items.Count - 1`.
- Verify changing `InvertXOnParse` flips horizontal ordering as expected. fileciteturn4file2turn4file4

### 7.4 Graph invariants (all modes)
For every component:
- Exactly one start node: `count(Prev==null) == 1`
- Exactly one end node: `count(Next==null) == 1`
- Traversal is a simple chain: following `Next` visits every `ImageId` exactly once.
- `CellById.Count == Items.Count`
- Every `PathSegment` references valid cells (no `(-1,-1)`), except if you explicitly allow off-grid nodes. fileciteturn4file7turn4file2

---

## 8) “Do not break” checklist for agents

Before committing changes, ensure:
- [ ] Priority selection remains **Position > Coordinates > Manual**. fileciteturn4file2
- [ ] Position mode still returns **two lane groups** for Position datasets. fileciteturn4file0turn4file2
- [ ] Manual mode does **not** introduce extra reversals (no “double reverse”). fileciteturn4file2
- [ ] `BuildArrangeGraph` uses movement semantics:
  - Right/Left: snake across rows
  - Up/Down: snake across columns fileciteturn4file9
- [ ] All mandatory tests in Section 7 pass.
- [ ] If expected matrices change, update this document and the golden outputs intentionally.

---

## 9) Agent-friendly execution notes

- Prefer adding **unit tests** (NUnit/xUnit) that run Section 7 programmatically.
- Use `DebugVisualizeArrange.PrintArrangeResult(...)` only for manual inspection; do not depend on console output in CI. fileciteturn4file3
