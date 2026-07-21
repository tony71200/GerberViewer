# TraversalGraph Guide (Agent Rules)

**Audience:** coding agents (CodeX / Claude / Copilot) who might modify traversal/graph logic.  
**Non‑negotiable:** any change **must preserve the invariants and test cases** listed here.

---

## 1) High-level purpose

`TraversalGraph` builds a **directed traversal graph** from an already-built **Matrix** (`List<List<ImageInfo>>`).
It is intentionally **independent** of the arranging rules in `RobotArrange`.

Given:
- `matrix`: 2D layout produced elsewhere (e.g., `RobotArrange.Arrange(...).Components[i].Matrix`)
- `OrderOptions.Mode`: `Zigzag | Branch | BranchDown`

`TraversalGraph` outputs:
- Per-node links (`LinksById[ImageId]`) similar to `ArrangeGraph`, but using `HNext/VNext` rules defined here.
- A drawable `List<PathSegment>` derived from `Flatten()` edges.
- Optional linear `Prev/Next` chain for `Zigzag` mode only.

---

## 2) Related files

### Inputs / shared domain
- `Domain.cs`
  - `ImageInfo`, `OrderOptions`, `OrderMode`
  - `GridCell`, `PathSegment`, `LinkDirection` (if shared in your project)
- `RobotArrange.cs`
  - Produces `Matrix`, `CellById`, `Path` etc. (TraversalGraph consumes only the Matrix)

### This module
- `TraversalGraph.cs`
  - Implements the rules in Section 4
- `TraversalGraph.md`
  - This contract/document

---

## 3) Scope and non-scope

### In scope
- Construct graph edges **only** from the provided `matrix` and `OrderOptions.Mode`.
- Provide `Flatten()` output (edges) and `PathSegments` (drawable).
- Provide a console debug print to visualize edges and directions.

### Explicitly NOT in scope
- **No** re-arranging/grouping/clustering of images.
- **No** use of `StartCorner`, `RobotMovement`, `PositionId`, or robot coordinates.
- **No** attempt to “fix” matrix orientation.
- The matrix is treated as authoritative layout.

---

## 4) Graph rules (must not change)

> The phrase “head” means the first non-null cell of a row/column in natural index order.

### 4.1 Common matrix assumptions
- Matrix is a jagged 2D list: `matrix[row][col]`.
- Cells may be `null` (missing). Nulls are ignored (skipped) when building edges.
- “Natural order” means:
  - row index increases downward: `row=0..R-1`
  - column index increases rightward: `col=0..C-1`

### 4.2 Mode: Zigzag
**Goal:** snake along rows, always starting from the matrix “top-left cell” (`matrix[0][0]`) and moving **to the right** on the first traversed row.

Rules:
1) For each row (top to bottom):
   - Filter non-null cells.
   - Row0 direction is **Left → Right**.
   - Alternate direction each row (`row1` reversed, `row2` forward, ...).
2) Create `HNext` inside each row following the row’s effective direction.
3) Create `VNext` to connect the end of row `r` to the start of row `r+1` (using the effective directions).
4) Create a linear chain `Prev/Next` across the entire traversal path (unique start/end).

**Invariants (Zigzag):**
- Exactly one start node with `Prev == null` and one end node with `Next == null`.
- For a matrix with `N` non-null nodes: chain length is `N` and `Flatten()` returns `N-1` edges.
- `HNext` + `VNext` links must be consistent with the `Prev/Next` chain.

### 4.3 Mode: Branch
**Goal:** traverse each row horizontally (left→right) and connect the **heads of rows** (top→bottom).

Rules:
1) For each row:
   - Filter non-null cells in **left→right** order.
   - Create `HNext` edges for each adjacent pair.
2) For row heads:
   - Let `head(r)` be the first non-null in row `r`.
   - Create `VNext`: `head(0) -> head(1) -> head(2) ...`

**Invariants (Branch):**
- `HNext` edges are always left→right (never reversed).
- `VNext` exists only on row-head nodes (the “spine”).
- There is no requirement for a single Hamiltonian chain; `Prev/Next` may be absent.

### 4.4 Mode: BranchDown
**Goal:** traverse each column vertically (top→bottom) and connect the **heads of columns** (left→right).

Rules:
1) For each column:
   - Filter non-null cells in **top→bottom** order.
   - Create `VNext` edges for each adjacent pair.
2) For column heads:
   - Let `head(c)` be the top-most non-null in column `c`.
   - Create `HNext`: `head(0) -> head(1) -> head(2) ...`

**Invariants (BranchDown):**
- `VNext` edges are always top→bottom (never reversed).
- `HNext` exists only on column-head nodes (the “spine”).
- There is no requirement for a single Hamiltonian chain; `Prev/Next` may be absent.

---

## 5) Outputs and usage contract

### 5.1 LinksById
`LinksById[imageId]` contains:
- `HNext`: horizontal successor (mode-dependent)
- `VNext`: vertical successor (mode-dependent)
- `Prev/Next`: only guaranteed for `Zigzag`

**Agent rule:** do not overload meanings. `HNext` and `VNext` must follow Section 4 semantics exactly.

### 5.2 Flatten()
`Flatten(matrix)` returns two parallel lists:
- `From[i] -> To[i]` edges

Ordering rules:
- Zigzag: returns edges of the single traversal chain (in traversal order).
- Branch / BranchDown: returns the union of all `HNext` and `VNext` edges (duplicates removed).

### 5.3 PathSegments (drawable)
`PathSegments` is built from `Flatten()`:
- Each segment stores `(FromId, ToId)` and `(FromCell, ToCell)` based on `CellById`.
- `Direction` is derived by cell adjacency:
  - Right/Left/Up/Down for adjacent cells
  - Jump for non-adjacent or missing cells

**UI contract:**
- Every endpoint in `PathSegments` must exist in `CellById`.
- If a segment is `Jump`, the UI must draw a special connector (dashed line) or handle it explicitly.

---

## 6) Debug-only helpers

`TraversalGraph.DebugPrintEdges()` prints:
- `FromId (Row,Col) -> ToId (Row,Col) (Direction)` for the current mode.

Rules:
- Debug methods must be pure read-only (no state mutation).
- Debug output format can evolve, but must not change algorithm behavior.

---

## 7) Mandatory regression tests (logic suite)

Every modification must run these tests.
If expected results change, the agent must:
1) explain why, and
2) update golden outputs intentionally.

### 7.1 Zigzag golden test (simple)
Matrix:
```
[ 0 1 2 ]
[ 5 4 3 ]
```
Expected traversal (Prev/Next chain):
`0 -> 1 -> 2 -> 3 -> 4 -> 5`

Expected edges:
- Row0: `0->1, 1->2`
- VNext: `2->3`
- Row1 reversed: `3->4, 4->5`

### 7.2 Branch golden test (simple)
Same matrix:
```
[ 0 1 2 ]
[ 5 4 3 ]
```
Expected:
- HNext row0: `0->1, 1->2`
- HNext row1: `5->4, 4->3` (Branch uses matrix order left->right, so this row is `5->4->3`)
- VNext heads: `0->5`

### 7.3 BranchDown golden test (simple)
Same matrix:
- VNext col0: `0->5`
- VNext col1: `1->4`
- VNext col2: `2->3`
- HNext column heads: `0->1->2`

### 7.4 Null cell handling
Matrix:
```
[ 0 . 2 ]
[ . 4 . ]
```
Expected:
- All modes must skip nulls and never emit edges from/to missing nodes.
- `CellById` must contain only `{0,2,4}`.

### 7.5 Integration sanity (RobotArrange output)
Take any `ArrangeComponent.Matrix` from `RobotArrange` and build `TraversalGraph` with all three modes:
- Zigzag must always build a single chain over non-null nodes.
- Branch / BranchDown must build consistent spines (row-head / col-head).

---

## 8) “Do not break” checklist for agents

Before committing changes, ensure:
- [ ] TraversalGraph is independent from RobotArrange rules (no StartCorner/Movement logic).
- [ ] Zigzag row0 always goes **right** from `matrix[0][0]`.
- [ ] Branch connects row heads using `VNext` and rows using `HNext` left→right.
- [ ] BranchDown connects column heads using `HNext` and columns using `VNext` top→bottom.
- [ ] Zigzag has exactly one start and one end (Prev/Next chain).
- [ ] All mandatory tests in Section 7 pass.

---

## 9) Agent execution notes

- Prefer unit tests (xUnit/NUnit) for Section 7.
- Use `DebugPrintEdges()` only for inspection; do not rely on console output in CI.
