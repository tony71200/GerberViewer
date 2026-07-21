# ImageGrid_Refactor.md

> **Scope:** This document describes a targeted refactor to introduce `ImageGrid` — a 2D matrix
> structure — as the normalized intermediate between row-building and graph-building stages in
> `RobotOrderer.cs`. Read this alongside `RobotOrderer.md` and `Domain.cs` before making any changes.

---

## Problem Statement

### Root cause

`BuildGraph()` and `BuildGraphManual()` currently operate on `OrderedRow[]` — a 1-dimensional list
of rows. This creates three concrete inconsistencies when building graphs for `Branch` and
`BranchDown` modes:

**Inconsistency 1 — Hidden row reversal**
`ApplyZigzagDirection()` reverses odd rows in-place before `BuildGraph()` is called. `BuildGraph()`
then calls `BuildStitchRows()` / `BuildStitchColumns()` which may reverse rows *again* to align
heads with the spine corner. There is no flag indicating whether a row has already been reversed,
so the two normalizations can conflict silently.

**Inconsistency 2 — BranchDown column alignment breaks after zigzag**
`BranchDown` requires `node[r][i] → node[r+1][i]` to link the same physical column across rows.
After `ApplyZigzagDirection()` reverses odd rows, `seq[i]` in row 0 and `seq[i]` in row 1 no
longer refer to the same physical column. The current code uses `Math.Min(seq.Length, nextSeq.Length)`
and pairs by index — silently linking wrong nodes.

**Inconsistency 3 — Manual mode skips normalization entirely**
`BuildGraphManual()` does not call `BuildStitchRows()` / `BuildStitchColumns()`. As a result,
`Branch` and `BranchDown` behave differently between metadata-driven mode and manual fallback mode,
even when the user configures identical `OrderMode` and `StartCorner` settings.

### Why `OrderedRow[]` is insufficient

`OrderedRow[]` encodes traversal order, not physical position. Once a row is reversed for zigzag
traversal, the physical column index `col` is no longer recoverable from the sequence index `i`
without knowing the original row direction. `BuildGraph` must then guess or re-derive the physical
layout — and the three inconsistencies above are the result.

---

## Proposed Solution: `ImageGrid`

Introduce `ImageGrid` as a stable 2D matrix of `ImageInfo` indexed by physical `(row, col)`.
`ImageGrid` encodes physical layout and is never mutated after construction. All three graph modes
(`Zigzag`, `Branch`, `BranchDown`) read from `ImageGrid` directly using unambiguous index math.
Manual fallback and metadata-driven row builders must both converge into the same `ImageGrid`
representation before graph construction so they emit identical `OrderGraph` semantics for the
same `OrderMode`, `StartCorner`, and `RobotMovement` inputs.

### Key invariant

> `Grid[r, c]` always refers to the physically correct tile at row `r`, column `c`, regardless of
> traversal direction. Traversal direction is stored separately in `RowForward[r]`.

## Target graph semantics (must be preserved)

These rules define the intended behavior of the new grid-backed graph builder. The implementation
must satisfy them identically whether rows come from metadata-driven clustering or the manual
fallback chunking path.

1. **Zigzag `VNext` uses traversal endpoints, not physical heads.**
   `VNext = tail(row r) -> head(row r+1)`, where `head(row r+1)` is taken from the traversal order
   returned by the row direction for row `r+1`. If `RowForward[r+1] == false`, then the head is the
   physically last tile in that row because `GetRowSequence(r+1)` is reversed.
2. **Branch uses the physical spine column.**
   `Branch` vertical links must always use `SpineCol` on the physical grid:
   `Grid[r, SpineCol] -> Grid[r+1, SpineCol]`. The traversal head of a row is not a substitute for
   the spine unless it happens to lie on `SpineCol`.
3. **BranchDown uses physical column alignment only.**
   `BranchDown` vertical links are `Grid[r,c] -> Grid[r+1,c]` for each physical column `c`. They
   must never be computed by traversal index because zigzag or spine-oriented row traversal can
   reorder the per-row visit sequence.
4. **Manual fallback and metadata-driven modes converge before graph build.**
   After either path creates `OrderedRow[]`, both must be converted into the same physical-grid +
   row-direction model so `BuildGraphFromGrid()` applies one shared set of semantics. There must be
   no manual-only or metadata-only graph interpretation rules.

---

## New Type: `ImageGrid` (add to `Domain.cs`)

```csharp
/// <summary>
/// A 2D physical layout of ImageInfo tiles, indexed by [row, col].
/// Grid[r, c] is the tile at physical row r and column c — never reordered.
/// Traversal direction per row is stored in RowForward[].
/// </summary>
public sealed class ImageGrid
{
    /// <summary>Physical tile matrix. Grid[r, c] is stable and never reversed.</summary>
    public ImageInfo[,] Grid { get; }

    /// <summary>Number of rows in the grid.</summary>
    public int Rows { get; }

    /// <summary>Number of columns in the grid.</summary>
    public int Cols { get; }

    /// <summary>
    /// Traversal direction for each row.
    /// RowForward[r] = true  → traverse col 0..Cols-1 (left to right)
    /// RowForward[r] = false → traverse col Cols-1..0 (right to left)
    /// For Zigzag: even rows match RobotMovement.Right/Up, odd rows are flipped.
    /// For Branch / BranchDown: all rows use the spine-side direction.
    /// </summary>
    public bool[] RowForward { get; }

    /// <summary>
    /// Column index that serves as the spine for Branch mode.
    /// SpineCol = 0 when StartCorner is TopLeft or BottomLeft.
    /// SpineCol = Cols-1 when StartCorner is TopRight or BottomRight.
    /// Unused by Zigzag and BranchDown.
    /// </summary>
    public int SpineCol { get; }

    public ImageGrid(ImageInfo[,] grid, bool[] rowForward, int spineCol)
    {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (rowForward == null) throw new ArgumentNullException(nameof(rowForward));
        Grid = grid;
        Rows = grid.GetLength(0);
        Cols = grid.GetLength(1);
        RowForward = rowForward;
        SpineCol = spineCol;
    }

    /// <summary>
    /// Returns the ImageInfo at physical position (r, c). Returns null if out of bounds.
    /// </summary>
    public ImageInfo At(int r, int c)
    {
        if (r < 0 || r >= Rows || c < 0 || c >= Cols) return null;
        return Grid[r, c];
    }

    /// <summary>
    /// Returns the traversal sequence for row r, respecting RowForward[r].
    /// This is the order the robot actually visits tiles in row r.
    /// </summary>
    public ImageInfo[] GetRowSequence(int r)
    {
        var seq = new ImageInfo[Cols];
        for (int c = 0; c < Cols; c++)
            seq[c] = RowForward[r] ? Grid[r, c] : Grid[r, Cols - 1 - c];
        return seq;
    }
}
```

---

## New Method: `ToImageGrid()` (add to `RobotOrderer.cs`, private)

This is the bridge between existing row builders and the new graph builder. It converts
`OrderedRow[]` — which already has traversal direction baked in — into an `ImageGrid` where
`Grid[r, c]` is always the physical column-ascending position.

```csharp
/// <summary>
/// Converts an OrderedRow[] produced by any row builder into an ImageGrid.
/// Assumes all rows have the same length (pad with null if ragged).
/// </summary>
private static ImageGrid ToImageGrid(OrderedRow[] rows, OrderOptions opt)
{
    if (rows == null || rows.Length == 0)
        return new ImageGrid(new ImageInfo[0, 0], Array.Empty<bool>(), 0);

    int rowCount = rows.Length;
    int colCount = rows.Max(r => r.Sequence?.Length ?? 0);

    bool horizontalMove = opt.RobotMovement == RobotMovement.Left
                       || opt.RobotMovement == RobotMovement.Right;
    bool spineLeft = opt.StartCorner == StartCorner.TopLeft
                  || opt.StartCorner == StartCorner.BottomLeft;

    // SpineCol: the column index that Branch mode uses as the vertical connection spine
    int spineCol = spineLeft ? 0 : colCount - 1;

    var grid = new ImageInfo[rowCount, colCount];
    var rowForward = new bool[rowCount];

    for (int r = 0; r < rowCount; r++)
    {
        var seq = rows[r].Sequence ?? Array.Empty<ImageInfo>();

        // Detect whether this row is currently in ascending physical order.
        // A row is forward if its first element has a smaller primary-axis value
        // than its last element.
        bool isForward = true;
        if (seq.Length >= 2)
        {
            double first = horizontalMove ? seq[0].XRobot : seq[0].YRobot;
            double last  = horizontalMove ? seq[seq.Length - 1].XRobot
                                          : seq[seq.Length - 1].YRobot;
            isForward = first <= last;
        }

        rowForward[r] = isForward;

        // Fill grid in physical column order (ascending primary axis)
        for (int c = 0; c < seq.Length; c++)
        {
            int physCol = isForward ? c : (seq.Length - 1 - c);
            if (physCol < colCount)
                grid[r, physCol] = seq[c];
        }
    }

    return new ImageGrid(grid, rowForward, spineCol);
}
```

---

## New Method: `BuildGraphFromGrid()` (add to `RobotOrderer.cs`, private)

This replaces `BuildGraph()` and `BuildGraphManual()`. It reads `ImageGrid` directly — no row
reversal, no re-normalization, no mode-specific branching on row direction.

```csharp
/// <summary>
/// Builds an OrderGraph from an ImageGrid.
/// All three OrderMode values read Grid[r, c] directly using unambiguous index math.
/// Replaces both BuildGraph() and BuildGraphManual().
/// </summary>
private static OrderGraph BuildGraphFromGrid(ImageGrid ig, OrderOptions opt)
{
    if (ig == null) throw new ArgumentNullException(nameof(ig));

    var links = new Dictionary<int, NodeLinks>();

    // Helper: get or create NodeLinks for a given ImageId
    NodeLinks GetOrAdd(int id)
    {
        if (!links.TryGetValue(id, out var nl))
        {
            nl = new NodeLinks { ImageId = id };
            links[id] = nl;
        }
        return nl;
    }

    // -----------------------------------------------------------------------
    // HNext: in-row traversal links
    // For BranchDown, horizontal links are only emitted for the first row
    // (matches existing behavior documented in RobotOrderer.md).
    // -----------------------------------------------------------------------
    int hRowLimit = (opt.Mode == OrderMode.BranchDown) ? 1 : ig.Rows;

    for (int r = 0; r < hRowLimit; r++)
    {
        var seq = ig.GetRowSequence(r);   // respects RowForward[r]
        for (int i = 0; i < seq.Length - 1; i++)
        {
            if (seq[i] == null || seq[i + 1] == null) continue;
            GetOrAdd(seq[i].ImageId).HNext = seq[i + 1].ImageId;
        }
        // Ensure tail node exists in links even if it has no HNext
        if (seq.Length > 0 && seq[seq.Length - 1] != null)
            GetOrAdd(seq[seq.Length - 1].ImageId);
    }

    // -----------------------------------------------------------------------
    // VNext: row-to-row links — behaviour differs per OrderMode
    // -----------------------------------------------------------------------
    for (int r = 0; r < ig.Rows - 1; r++)
    {
        if (opt.Mode == OrderMode.Zigzag)
        {
            // Tail of current traversal row → Head of next traversal row
            var currentSeq = ig.GetRowSequence(r);
            var nextSeq    = ig.GetRowSequence(r + 1);
            var tail = currentSeq.LastOrDefault(x => x != null);
            var head = nextSeq.FirstOrDefault(x => x != null);
            if (tail != null && head != null)
                GetOrAdd(tail.ImageId).VNext = head.ImageId;
        }
        else if (opt.Mode == OrderMode.Branch)
        {
            // Physical spine column: Grid[r, SpineCol] → Grid[r+1, SpineCol]
            var from = ig.At(r,     ig.SpineCol);
            var to   = ig.At(r + 1, ig.SpineCol);
            if (from != null && to != null)
                GetOrAdd(from.ImageId).VNext = to.ImageId;
        }
        else if (opt.Mode == OrderMode.BranchDown)
        {
            // Each physical column links downward: Grid[r, c] → Grid[r+1, c]
            for (int c = 0; c < ig.Cols; c++)
            {
                var from = ig.At(r,     c);
                var to   = ig.At(r + 1, c);
                if (from != null && to != null)
                    GetOrAdd(from.ImageId).VNext = to.ImageId;
            }
        }
    }

    // -----------------------------------------------------------------------
    // Reconstruct OrderedRow[] for OrderGraph.Rows
    // Rows carry the traversal-order sequences (respecting RowForward).
    // -----------------------------------------------------------------------
    var orderedRows = new OrderedRow[ig.Rows];
    for (int r = 0; r < ig.Rows; r++)
    {
        orderedRows[r] = new OrderedRow
        {
            RowIndex = r,
            Sequence = ig.GetRowSequence(r).Where(x => x != null).ToArray()
        };
    }

    return new OrderGraph
    {
        Mode         = opt.Mode,
        Rows         = orderedRows,
        LinksById    = links,
        RobotMovement = opt.RobotMovement
    };
}
```

---

## Migration Plan

Follow these steps in order. Do **not** delete old code until Step 4 is complete and verified.

### Step 1 — Add `ImageGrid` to `Domain.cs`

- Add the `ImageGrid` class exactly as specified above.
- No existing code changes. This step is additive only.
- Tag the addition: `// [Codex] [Change time: XXXXXX] [Add ImageGrid 2D layout type]`

### Step 2 — Add `ToImageGrid()` and `BuildGraphFromGrid()` to `RobotOrderer.cs`

- Add both private methods in a new `#region ImageGrid` block below the existing `#region Logic in Order`.
- No existing call sites change. Both methods are unreachable until Step 3.
- Tag the addition: `// [Codex] [Change time: XXXXXX] [Add ImageGrid builders — not yet wired]`

### Step 3 — Wire `BuildGraphFromGrid()` into `BuildOrdersForGroup()` behind a flag

Replace the graph-building call inside the component loop with a parallel path:

```csharp
// [Codex] [Change time: XXXXXX] [Wire ImageGrid path for validation — runs alongside old path]
var igrid  = ToImageGrid(rows, opt);
var graphNew = BuildGraphFromGrid(igrid, opt);

// Keep old path active until verified:
var graph = useManualFallback
    ? BuildGraphManual(rows, opt)
    : BuildGraph(rows, opt);

// TODO: after validation, replace the two lines above with:
// var graph = graphNew;
```

Run the verification examples from `RobotOrderer.md` against both `graph` and `graphNew` and
confirm they produce identical `OrderFlattener.Flatten()` output.

### Step 4 — Remove old path

Once all verification examples pass:

1. Delete `BuildGraph()`, `BuildGraphManual()`, `BuildStitchRows()`, `BuildStitchColumns()`.
2. Replace the wiring block from Step 3 with `var graph = BuildGraphFromGrid(ToImageGrid(rows, opt), opt);`.
3. Tag: `// [Codex] [Change time: XXXXXX] [Remove old graph builders — ImageGrid path verified]`
4. Update `RobotOrderer.md`:
   - Remove the four inconsistency entries under `## Current Inconsistencies To Watch`.
   - Update `## Algorithm Overview` step 6 to reference `BuildGraphFromGrid()`.
   - Update `## Doc-to-Code Recheck Checklist` to remove `BuildStitchRows/Columns` references.

---

## Verification Examples

Use the same inputs as `RobotOrderer.md`. Expected `Flatten()` output must not change.

## Compact verification matrix

Use this matrix as a quick doc-to-code check while wiring the new path. It captures the requested
reference cases and the semantic rule each case is intended to prove.

| StartCorner | RobotMovement | Mode | Row direction expectation | Vertical-link expectation to verify |
|-------------|---------------|------|---------------------------|-------------------------------------|
| `TopLeft` | `Right` | `Zigzag` | Row 0 traverses left→right; row 1 traverses right→left. | `VNext = tail(row 0) -> head(row 1)` where `head(row 1)` is the first tile of the reversed traversal order. |
| `TopLeft` | `Right` | `Branch` | Rows traverse from the left-side spine outward. | `VNext = Grid[r, SpineCol=0] -> Grid[r+1, SpineCol=0]`. |
| `TopLeft` | `Right` | `BranchDown` | Row traversal may vary, but vertical links ignore traversal index. | `Grid[r,c] -> Grid[r+1,c]` for each physical column `c`. |
| `BottomLeft` | `Up` | `Zigzag` | Column-major traversal keeps the bottom row as the start-side physical edge; alternating columns reverse traversal order. | `VNext = tail(column-like row r) -> head(column-like row r+1)` using the directed traversal order, not physical top/bottom alone. |
| `BottomRight` | `Up` | `Branch` / `BranchDown` | Spine and column matching are evaluated on the physical grid with the rightmost physical column as the start-side anchor. | `Branch` uses `SpineCol = Cols-1`; `BranchDown` still links by physical column `c`, independent of traversal reversal. |

### Zigzag — `TopLeft` / `Right`, 2×3

```
Grid (physical):
  [0,0]=A(0,10)  [0,1]=B(10,10)  [0,2]=C(20,10)
  [1,0]=D(0,0)   [1,1]=E(10,0)   [1,2]=F(20,0)

RowForward: [true, false]   (row 0 →, row 1 ←)
SpineCol: 0

HNext row 0: A→B, B→C
HNext row 1: F→E, E→D      (GetRowSequence(1) = [F,E,D])
VNext Zigzag: tail(row 0)=C → head(row 1)=F

Flatten: A→B, B→C, C→F, F→E, E→D   ✓
```

### Branch — `TopLeft` / `Right`, 2×3

```
Grid (physical):
  [0,0]=A  [0,1]=B  [0,2]=C
  [1,0]=D  [1,1]=E  [1,2]=F

RowForward: [true, true]    (both rows →, spine is left)
SpineCol: 0

HNext row 0: A→B, B→C
HNext row 1: D→E, E→F
VNext Branch: Grid[0,0]=A → Grid[1,0]=D

Flatten: A→B, B→C, A→D, D→E, E→F   ✓
```

### BranchDown — `TopLeft` / `Right`, 2×3

```
Grid (physical):
  [0,0]=A  [0,1]=B  [0,2]=C
  [1,0]=D  [1,1]=E  [1,2]=F

RowForward: [true, true]

HNext row 0 only: A→B, B→C
VNext BranchDown: A→D, B→E, C→F

Flatten: A→B, B→C, A→D, B→E, C→F   ✓
```

### Manual fallback — `FlyNodeInterval=3`, `TopLeft` / `Right`, 6 images

```
ImageId sort: [1,2,3,4,5,6]
Chunks: [1,2,3], [4,5,6]

After ToImageGrid:
  Grid[0,0]=1  Grid[0,1]=2  Grid[0,2]=3
  Grid[1,0]=4  Grid[1,1]=5  Grid[1,2]=6
RowForward: [true, false]

Zigzag Flatten: 1→2, 2→3, 3→6, 6→5, 5→4   ✓
```

---

## Invariants `BuildGraphFromGrid()` Must Preserve

These are the correctness rules. Any change to `BuildGraphFromGrid()` must not violate them.

1. **Grid is read-only.** `BuildGraphFromGrid()` must never modify `ig.Grid` or `ig.RowForward`.
2. **HNext only within a row.** No `HNext` link may cross row boundaries.
3. **VNext only between adjacent rows.** `VNext` always goes from row `r` to row `r+1`, never `r+2` or back.
4. **BranchDown VNext uses physical column index.** `Grid[r,c] → Grid[r+1,c]` for the same `c`. Never index-by-traversal-order.
5. **Branch VNext uses SpineCol.** `Grid[r, SpineCol] → Grid[r+1, SpineCol]`. `SpineCol` must be set correctly before `BuildGraphFromGrid()` is called.
6. **Null safety.** Any `null` entry in `Grid` (ragged rows) must be skipped silently — never throw, never add a null ImageId to links.
7. **LinksById is consistent with Rows.** Every ImageId that appears in `OrderGraph.Rows[r].Sequence` must have a corresponding entry in `LinksById`, even if `HNext` and `VNext` are both null.

---

## Files To Change

| File | Change |
|------|--------|
| `Domain.cs` | Add `ImageGrid` class (Step 1) |
| `RobotOrderer.cs` | Add `ToImageGrid()`, `BuildGraphFromGrid()` (Step 2); wire (Step 3); remove old builders (Step 4) |
| `RobotOrderer.md` | Update algorithm overview and inconsistencies section after Step 4 |

Do **not** change `OrderStitchRunner.cs` or `StitchingImage.cs` — they consume `OrderGraph` which
is unchanged in shape. Only the internal construction path changes.

---

## Code Style Rules (inherit from `RobotOrderer.md`)

- Tag every change with `// [Codex] [Change time: XXXXXX] [reason]`
- Keep `ToImageGrid()` and `BuildGraphFromGrid()` in their own `#region ImageGrid`
- Do not add mode-specific logic to `ToImageGrid()` — it must only convert, not decide
- Do not add layout logic to `BuildGraphFromGrid()` — it must only build links from an already-correct grid
- Preserve all existing enum symbols: `OrderMode`, `StartCorner`, `RobotMovement`, `ClusterOrderMode`

---

## Ask First

- Before changing `ToImageGrid()` to handle ragged rows differently (current: pad with null)
- Before changing the `SpineCol` formula if `StartCorner` semantics change upstream
- Before adding a new `OrderMode` value — `BuildGraphFromGrid()` must explicitly handle it or throw

## Never Do

- Do not reverse any row inside `BuildGraphFromGrid()` — direction is already encoded in `RowForward`
- Do not call `BuildStitchRows()` or `BuildStitchColumns()` from `BuildGraphFromGrid()` — those are the methods being replaced
- Do not skip Step 3 (parallel validation) and go directly to Step 4 — the old and new paths must be confirmed equivalent first
- Do not modify `ImageGrid.Grid` after construction — treat it as immutable
