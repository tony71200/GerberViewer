# AGENTS.md

<!-- [Codex] [Change time: 260320] [Removed temporary Snake terminology note after enum/doc cleanup]
> Temporary terminology note: UI/business term **Zigzag** currently maps to the public enum/code symbol `OrderMode.Zigzag`. This document uses **Zigzag** for business meaning unless it references the literal symbol.
-->

> UI clarification note: in `MainForm`, **Layout** means how rows/columns are arranged before graph links are written, while **Traversal** means which stitch graph flow (`Zigzag`, `Branch`, `Branch Down`) is used after that layout already exists.

## Project Overview
- **Project:** `PLP` / `StitchingImage`
- **Stack:** C#, Windows Forms, .NET Framework 4.8
- **Solution:** `PLP.sln`
- **Main executable project:** `StitchingImage/StitchingImage.csproj`
- **Primary responsibility of this folder:** build image ordering graphs/components for robot-captured tiles before matching and stitching
- **Why this file exists:** keep the ordering algorithm note next to `RobotOrderer.cs` so maintainers can read code and algorithm rules side-by-side

## Scope
- This note is for files under `StitchingImage/Stitch_Tools/RobotManager/`
- Read this file before changing:
  - `RobotOrderer.cs`
  - `Domain.cs`
  - `OrderStitchRunner.cs`
  - `StitchingImage.cs`

## Key Files
- `Domain.cs` — domain models such as `ImageInfo`, `OrderOptions`, `OrderGraph`, `OrderedRow`, and enums used by the orderer
- `RobotOrderer.cs` — builds components, rows, and stitch graphs from image metadata
- `OrderStitchRunner.cs` — consumes the generated graph/order for matching and stitching execution
- `StitchingImage.cs` — final image composition and save pipeline

## Environment Notes
- Target framework is **.NET Framework 4.8**
- The project currently references local/packaged dependencies such as **HALCON** and **OpenCvSharp**
- Some build/run flows require Windows and local native dependencies; do not assume they are available in every environment

## Commands
- `msbuild PLP.sln /t:Build /p:Configuration=Debug` — build the solution in Debug mode
- `msbuild PLP.sln /t:Build /p:Configuration=Release` — build the solution in Release mode
- `nuget restore PLP.sln` — restore NuGet packages when package references are missing
- `git diff -- StitchingImage/Stitch_Tools/RobotManager/RobotOrderer.cs` — review algorithm changes
- `git diff -- StitchingImage/Stitch_Tools/RobotManager/RobotOrderer.md` — review documentation changes in this file

## Code Style For This Area
- Keep business meaning explicit: prefer `Position`, `Coordinates`, `ManualRow`, `ManualColumn` terminology over vague fallback names
- When changing ordering logic, keep row construction and graph construction clearly separated in code and in comments
- Avoid introducing hidden metadata fallbacks; if a mode depends on coordinates or `PositionId`, state that directly in code/comments
- Preserve the existing enum vocabulary: `StartCorner`, `RobotMovement`, `ClusterOrderMode`, `OrderMode`
- Prefer small helper methods when separating business cases instead of packing more conditions into `BuildOrdersForGroup()`

## Algorithm Overview

### `BuildOrdersForGroup()` flow
1. Validate input
   - `images == null` throws
   - empty input returns an empty `OrderedGroupResult`
2. Detect usable metadata
   - `hasCoordinateMetadata`: every image has valid `XRobot` and `YRobot`
   - `hasPositionMetadata`: every image has `PositionId`
   - `useManualFallback = !hasCoordinateMetadata && !hasPositionMetadata`
3. Choose the effective clustering source
   - if all images have `PositionId` -> force `ClusterOrderMode.Position`
   - else if all images have coordinates -> force `ClusterOrderMode.Coordinates`
   - else keep `opt.ClusterOrder` and enter manual fallback flow
4. Build components
   - manual fallback: one component, sorted by `ImageId`
   - position mode: `BuildComponentsByPosition()`
   - coordinate mode: `EstimateTypicalStep()` + `ConnectedComponents()` + `CompareComponents()`
5. Build rows/lanes inside each component
   - `BuildRowsByPosition()` for position mode
   - `BuildRowsByCoordinates()` for coordinate mode
   - `BuildManualRows()` or `BuildManualColumns()` for manual fallback
6. Convert rows into a physical `ImageGrid` and build the graph once
   - `ToImageGrid()` converts metadata-driven and manual rows into the same physical grid model
   - `BuildGraphFromGrid()` writes `Rows` and `LinksById` for `Zigzag`, `Branch`, and `BranchDown`
7. Compute `SpecialGapEdges`

## Data Cases

### Case 1 — Has `PositionId` and has `XRobot/YRobot`
- Effective behavior: prefer **position ordering**
- Expected path:
  - `effectiveClusterOrder = Position`
  - `BuildComponentsByPosition()`
  - `BuildRowsByPosition()`
  - `ToImageGrid()` -> `BuildGraphFromGrid()`
- Note:
  - coordinates may still affect downstream behavior such as bounds and special-gap detection even though graph construction now uses the shared `ImageGrid` path

### Case 2 — Missing `PositionId`, but still has coordinates
- Effective behavior: use **coordinate ordering**
- Expected path:
  - `effectiveClusterOrder = Coordinates`
  - `ConnectedComponents()`
  - `BuildRowsByCoordinates()`
  - `ToImageGrid()` -> `BuildGraphFromGrid()`
- Use this when coordinates reflect the real physical tile layout

### Case 3 — Has `PositionId`, but missing coordinates
- Effective behavior: still prefer **position ordering**
- Expected path:
  - `effectiveClusterOrder = Position`
  - `BuildComponentsByPosition()`
  - `BuildRowsByPosition()`
  - `ToImageGrid()` -> `BuildGraphFromGrid()`
- Caution:
  - this mode still has some downstream coordinate coupling
  - `EstimateTypicalStep()` and `BuildSpecialGapEdges()` can become unstable when coordinates are absent or `NaN`

### Case 4 — Missing both `PositionId` and coordinates
- Effective behavior: **manual fallback**
- Expected path:
  - `useManualFallback = true`
  - one component sorted by `ImageId`
  - `BuildManualRows()` or `BuildManualColumns()` based on `opt.ClusterOrder`
  - `ToImageGrid()` -> `BuildGraphFromGrid()`
- Use this only when real metadata cannot be trusted or does not exist

## Distinguish Two Concepts

### zigzag layout ordering
This is the layout-building stage that decides how rows/columns are discovered and oriented before graph links are written.

It includes:
- component detection
- row/column grouping
- lane direction handling based on the active layout source

Typical entry points:
- `BuildRowsByCoordinates()` -> uses `ApplyZigzagDirection()`
- `BuildRowsByPosition()` -> preserves `BuildPositionRows()` run direction from `PositionId`
- `BuildManualRows()` / `BuildManualColumns()` -> use `BuildManualLanes()`

### stitch traversal mode
This is the graph-building stage controlled by `OrderMode`. It decides how already-built rows/columns are connected for stitching traversal.

It includes:
- `OrderMode.Zigzag` = UI/business term `Zigzag`
- `OrderMode.Branch`
- `OrderMode.BranchDown`

Important:
- layout ordering and stitch traversal mode are related but different concepts
- this task keeps the public enum/code symbol `OrderMode.Zigzag` unchanged to avoid wide refactors

## When To Use Each Ordering Mode

### zigzag layout ordering: Coordinate ordering
Use when:
- all images have valid coordinates
- `PositionId` is incomplete or unavailable
- layout should follow physical robot positions

Core functions:
- `EstimateTypicalStep()`
- `ConnectedComponents()`
- `ClusterRowsBySecondaryAxis()`
- `ApplyZigzagDirection()`

Checks in `RobotOrderer.cs`:
- coordinate rows are sorted by primary axis in `BuildRowsByCoordinates()`
- alternating zigzag direction is applied only here via `ApplyZigzagDirection()`

### zigzag layout ordering: Position ordering
Use when:
- all images have `PositionId`
- `PositionId` encodes scan order more reliably than physical coordinates

Core functions:
- `BuildPositionRows()`
- `BuildComponentsByPosition()`
- `BuildRowsByPosition()`

Checks in `RobotOrderer.cs`:
- row breaks happen when `PositionId` no longer continues by step `+/-1`
- `BuildRowsByPosition()` does **not** call `ApplyZigzagDirection()`; it keeps the run direction produced from `PositionId`

### zigzag layout ordering: `Manual Row`
Use when:
- neither coordinates nor `PositionId` are available for the full group
- `FlyNodeInterval` should be interpreted as images-per-row
- the maintainer/user wants horizontal logical lanes based on `ImageId`

Core functions:
- `BuildManualRows()`
- `BuildManualLanes(..., horizontalLayout: true)`
- `ToImageGrid()` -> `BuildGraphFromGrid()`

Checks in `RobotOrderer.cs`:
- rows are chunks of `ImageId` ordered by `FlyNodeInterval`
- alternating lane direction is handled in `BuildManualLanes()`

### zigzag layout ordering: `Manual Column`
Use when:
- neither coordinates nor `PositionId` are available for the full group
- `FlyNodeInterval` should be interpreted as images-per-column
- the maintainer/user wants vertical logical lanes based on `ImageId`

Core functions:
- `BuildManualColumns()`
- `BuildManualLanes(..., horizontalLayout: false)`
- `ToImageGrid()` -> `BuildGraphFromGrid()`

Checks in `RobotOrderer.cs`:
- columns are chunks of `ImageId` ordered by `FlyNodeInterval`
- alternating lane direction is handled in `BuildManualLanes()`

## Option Relationships

### `ClusterOrderMode`
Controls which metadata source is used to understand the layout before graph building.
- `Coordinates` — derive rows/components from `XRobot/YRobot`
- `Position` — derive rows/components from `PositionId`
- `ManualRow` — derive rows from `ImageId` chunks
- `ManualColumn` — derive columns from `ImageId` chunks

Important:
- `BuildOrdersForGroup()` overrides this mode when full real metadata is available
- manual modes are only honored when both metadata sources are missing

### `StartCorner`
Controls which corner is treated as the layout origin.

It affects:
- component ordering in `CompareComponents()`
- row grouping/sorting in `ClusterRowsBySecondaryAxis()`
- lane ordering in `BuildManualLanes()`
- physical spine selection in `ToImageGrid()` / `BuildGraphFromGrid()`

### `RobotMovement`
Controls the primary travel axis and the forward direction of the first lane.

It affects:
- whether the move is horizontal (`Left/Right`) or vertical (`Up/Down`)
- step estimation thresholds and row tolerance
- row sorting in coordinate mode
- zigzag direction in `ApplyZigzagDirection()`
- manual lane direction in `BuildManualLanes()`
- how manual graph links are written to `HNext` / `VNext`

### `OrderMode`
Controls how already-built rows/columns are connected in the stitch graph.
- `OrderMode.Zigzag` (UI/business term `Zigzag`) — tail of current row/column connects to head of the next row/column, while the layout stage already alternates row direction for zigzag travel
- `Branch` — head of current row connects to head of next row
- `BranchDown` — corresponding nodes connect row-to-row by index

Important:
- `OrderMode` does **not** decide how rows are discovered during zigzag layout ordering
- `OrderMode` decides graph traversal topology after rows already exist

## ImageGrid Graph Rules

- `ToImageGrid()` is now the only bridge from row discovery to graph construction.
- `ImageGrid.Grid[r, c]` always represents the physical tile position and is never reversed in-place.
- `ImageGrid.RowForward[r]` stores traversal direction separately from physical placement.
- `BuildGraphFromGrid()` handles all graph modes:
  - `Zigzag`: tail of traversal row `r` links to head of traversal row `r+1`
  - `Branch`: physical spine column links downward
  - `BranchDown`: physical column `c` links to physical column `c` in the next row
- Metadata-driven and manual fallback flows now converge before graph construction, so `Rows`, `LinksById`, and `OrderFlattener.Flatten()` semantics match for equivalent layouts.

## Verification Examples

### `stitch traversal mode = Zigzag`
Expected for a `TopLeft` / `Right` 2x3 layout:
- row 0: `A -> B -> C`
- row 1: `F -> E -> D`
- transition: `C -> F`
- flattened expectation: `A->B, B->C, C->F, F->E, E->D`

### `stitch traversal mode = Branch`
Expected for a `TopLeft` / `Right` 2x3 layout with left spine:
- row 0: `A -> B -> C`
- row 1 should normalize to `D -> E -> F`
- vertical branch: `A -> D`
- flattened expectation: `A->B, B->C, A->D, D->E, E->F`

### `stitch traversal mode = BranchDown`
Expected for a `TopLeft` / `Right` 2x3 layout:
- row 0: `A -> B -> C`
- row 1: `D -> E -> F`
- vertical edges: `A->D, B->E, C->F`
- current implementation note: horizontal edges are only emitted for the first row in `BranchDown`

### `ClusterOrderMode = Coordinates`
Example data:
- top row: `A(0,10) B(10,10) C(20,10)`
- bottom row: `D(0,0) E(10,0) F(20,0)`
- options: `StartCorner = TopLeft`, `RobotMovement = Right`

Expected:
- row clustering yields `[A,B,C]` and `[D,E,F]`
- zigzag mode then yields `[A,B,C]` and `[F,E,D]`

### `ClusterOrderMode = Position`
Example ordered by `ImageId` with `PositionId` values:
- `A:0, B:1, C:2, D:2, E:1, F:0`

Expected:
- one increasing run `0,1,2`
- then a new row when the direction flips to `2,1,0`
- resulting rows: `[A,B,C]` and `[D,E,F]`

### zigzag layout ordering: `Manual Row`
Example:
- no coordinates
- no `PositionId`
- `ImageId = 1,2,3,4,5,6`
- `FlyNodeInterval = 3`

Expected:
- chunks: `[1,2,3]`, `[4,5,6]`
- lane direction depends on `StartCorner` and `RobotMovement`
- common zigzag-like result for `TopLeft` / `Right`: `[1,2,3]`, `[6,5,4]`

### zigzag layout ordering: `Manual Column`
Example:
- no coordinates
- no `PositionId`
- `ImageId = 1,2,3,4,5,6`
- `FlyNodeInterval = 2`

Expected:
- chunks interpreted as logical columns: `[1,2]`, `[3,4]`, `[5,6]`
- first column and in-column direction depend on `StartCorner`, `RobotMovement`, and lane parity

## Doc-to-Code Recheck Checklist
- Re-open `RobotOrderer.cs` after updating this document.
- Verify `BuildOrdersForGroup()` still chooses between `Coordinates`, `Position`, and manual fallback exactly as described here.
- Verify which builder actually applies alternating direction:
  - coordinate mode -> `ApplyZigzagDirection()`
  - position mode -> `BuildPositionRows()` run direction
  - manual mode -> `BuildManualLanes()`
- Verify stitch traversal remains in `BuildGraphFromGrid()` and not in the row-discovery stage.
- Verify `OrderFlattener.Flatten()` still matches the `OrderMode` examples in this document.

## Testing Guidance
- For logic changes, validate all three layers when possible:
  - `component.Graph.Rows`
  - `component.Graph.LinksById`
  - `OrderFlattener.Flatten(component.Graph)`
- Do not validate by row layout alone; flatten order is the real stitch traversal
- When updating fallback logic, test at least the four metadata-availability cases listed above
- When updating `Branch` or `BranchDown`, compare metadata-driven and manual flows explicitly after both pass through `ToImageGrid()`

## Always Do
- Keep this file aligned with the behavior of `RobotOrderer.cs`
- Update examples when changing row reversal or graph traversal semantics
- Re-check both row-level order and flattened graph order after algorithm edits
- Prefer documenting any forced mode override in `BuildOrdersForGroup()`

## Ask First
- Before changing enum meaning exposed to UI/business settings
- Before changing `FlyNodeInterval` semantics for manual modes
- Before removing coordinate coupling from position mode if downstream consumers still depend on current behavior
- Before changing `BranchDown` traversal semantics without confirming expected stitch output

## Never Do
- Do not assume `opt.ClusterOrder` will be honored when full coordinates or full `PositionId` metadata exists
- Do not treat `OrderedRow.Sequence` as the final stitch order without checking graph normalization and flattening
- Do not mix manual fallback rules into position/coordinate modes unless the code explicitly requires it
- Do not update `RobotOrderer.cs` without checking the impact on `OrderFlattener.Flatten()` consumers
