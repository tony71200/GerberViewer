using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StitchingImage.Stitch_Tools.RobotManager
{
    public sealed class RobotArrange
    {
        /// <summary>
        /// Arrange images into 1..N components; each component has a 2D matrix and a traversal graph.
        /// Priority inside each component: Position > Coordinates > Manual.
        /// If Position + Coordinates: Position first, then refine row order by coordinates.
        ///
        /// customizeOptPerComponent: allows changing opt per component (index + items).
        /// </summary>
        /// 
        public ArrangeBatchResult Arrange (IReadOnlyList<ImageInfo> images,
            OrderOptions baseOpt, Func<int, IReadOnlyList<ImageInfo>, OrderOptions, OrderOptions> customizeOptPerComponent = null)
        {
            if (images == null) throw new ArgumentNullException (nameof (images));
            if (baseOpt == null) throw new ArgumentNullException (nameof (baseOpt));

            var hasPosition = images.Count > 0 && images.All(i => i.PositionId.HasValue);
            var hasCoords = images.Count > 0 && images.All(i => IsFinite(i.XRobot) && IsFinite(i.YRobot));

            var typical = hasCoords ? EstimatetypicalStep(images, baseOpt.InvertXOnParse) : (double.NaN, double.NaN);
            var result = new ArrangeBatchResult {
                Components = new List<ArrangeComponent>(),
                TypicalStep = typical
            };

            if (hasPosition)
            {
                // Build exactly 2 groups for Postion stream
                var groupMatrices = BuildPositionTwoGroupMatrices(images, baseOpt);

                for (int g = 0; g < groupMatrices.Count; g++)
                {
                    var matrix = groupMatrices[g];
                    // apply only orientation transforms here if you want (transpose/flip),
                    matrix = NormalizePositionMatrix(matrix, baseOpt);

                    //Flatten items from matrix for component Items
                    var flatItems = matrix.SelectMany(r => r).Where(x => x != null).ToList();

                    var opt = customizeOptPerComponent != null
                        ? (customizeOptPerComponent(g, flatItems, baseOpt) ?? baseOpt)
                        : baseOpt;

                    var graph = BuildArrangeGraph(matrix, opt.RobotMovement, opt.StartCorner);
                    var cellById = BuildCellIndex(matrix);
                    var path = BuildPathSegments(graph, cellById, out var startId);

                    result.Components.Add(new ArrangeComponent
                    {
                        Index = g,
                        Items = flatItems,
                        Matrix = matrix,
                        Graph = graph,
                        OptionsUsed = opt,
                        CellById = cellById,
                        Path = path,
                        StartId = startId
                    });
                }
                return result;

            }

            var components = SplitIntoComponents(images, baseOpt, hasPosition: false, hasCoords: hasCoords, typical: typical);


            for (int idx = 0; idx < components.Count; idx++)
            {
                var items = components[idx];
                var opt = customizeOptPerComponent != null
                    ? (customizeOptPerComponent(idx, items, baseOpt) ?? baseOpt)
                    : baseOpt;

                var matrix = BuildMatrixForComponent(items, opt, typical);
                var graph = BuildArrangeGraph(matrix, opt.RobotMovement, opt.StartCorner);

                var cellById = BuildCellIndex(matrix);
                var path = BuildPathSegments(graph, cellById, out var startId);

                result.Components.Add(new ArrangeComponent
                {
                    Index = idx,
                    Items = items,
                    Matrix = matrix,
                    Graph = graph,
                    OptionsUsed = opt,

                    CellById = cellById,
                    Path = path,
                    StartId = startId
                });
            }

            return result;

        }

        // ============================================================
        // POSITION
        // ============================================================
        private static List<List<ImageInfo>> BuildMatrixByPosition(IReadOnlyList<ImageInfo> images, OrderOptions opt)
        {
            var ordered = images.OrderBy(i => i.ImageId).ToList();

            var lines = new List<List<ImageInfo>>();
            var current = new List<ImageInfo> { ordered[0] };
            int? lastPos = ordered[0].PositionId;

            int? lastDelta = null;
            for (int idx = 1; idx < ordered.Count; idx++)
            {
                var cur = ordered[idx];
                var curPos = cur.PositionId;

                int delta = curPos.Value - lastPos.Value;
                bool okStep = delta == 1 || delta == -1;
                bool directionChanged = lastDelta.HasValue && delta != lastDelta.Value;

                if (!okStep || directionChanged)
                {
                    lines.Add(current);
                    current = new List<ImageInfo>();
                    lastDelta = null;
                }
                else
                {
                    lastDelta = delta;
                }

                current.Add(cur);
                lastPos = curPos;
            }
            lines.Add(current);

            foreach (var line in lines)
                SortLineByPosition(line, opt);

            ApplyStartCornerLineOrder(lines, opt);
            ApplySnake(lines, opt);

            return lines;
        }

        // =====================
        // ModiPos: NEW - normalize orientation for Position matrix
        // Goal: put ImageId start (smallest ImageId in component) into StartCorner, and align to RobotMovement.
        // Allowed transforms: transpose, reverse rows, reverse cols.
        // =====================
        private static List<List<ImageInfo>> NormalizePositionMatrix(List<List<ImageInfo>> matrix, OrderOptions opt)
        {
            // ModiPos: transpose if movement is vertical, as per your spec.
            if (IsVertical(opt.RobotMovement))
                matrix = TransposeJagged(matrix);

            // ModiPos: find "start" (smallest ImageId) cell in this component.
            var cellById = BuildCellIndex(matrix);
            if (cellById.Count == 0) return matrix;

            int startId = cellById.Keys.Min();
            var start = cellById[startId];

            // Determine desired corner for startId
            int desiredRow = (opt.StartCorner == StartCorner.BottomLeft || opt.StartCorner == StartCorner.BottomRight)
                ? (matrix.Count - 1)
                : 0;

            int desiredCol = (opt.StartCorner == StartCorner.TopRight || opt.StartCorner == StartCorner.BottomRight)
                ? (MaxCols(matrix) - 1)
                : 0;

            // If current start row not matching desired -> flip vertically
            if (start.Row != desiredRow && matrix.Count > 1)
                matrix.Reverse(); // flip rows

            // Rebuild index after flip
            cellById = BuildCellIndex(matrix);
            start = cellById[startId];

            // If current start col not matching desired -> flip horizontally (reverse each row)
            if (start.Col != desiredCol && MaxCols(matrix) > 1)
            {
                for (int r = 0; r < matrix.Count; r++)
                    matrix[r].Reverse();
            }

            return matrix;
        }

        private static int MaxCols(List<List<ImageInfo>> m)
        {
            int max = 0;
            for (int i = 0; i < m.Count; i++)
                if (m[i].Count > max) max = m[i].Count;
            return max;
        }

        private static void SortLineByPosition(List<ImageInfo> line, OrderOptions opt)
        {
            bool forward =
                opt.RobotMovement == RobotMovement.Right ||
                opt.RobotMovement == RobotMovement.Down;

            bool startAtRight =
                opt.StartCorner == StartCorner.TopRight ||
                opt.StartCorner == StartCorner.BottomRight;

            if (opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Up)
                forward = !forward;

            if (startAtRight)
                forward = !forward;

            line.Sort((a, b) =>
            {
                var va = a.PositionId.Value;
                var vb = b.PositionId.Value;
                return forward ? va.CompareTo(vb) : vb.CompareTo(va);
            });
        }

        private static List<List<List<ImageInfo>>> BuildPositionTwoGroupMatrices(
                IReadOnlyList<ImageInfo> images,
                OrderOptions opt)
        {
            // Step 1: build rows from ImageId stream, but order INSIDE row by PositionId ascending
            // so decreasing sweeps become reversed (matching your expected [19..15], [14..10], ...)
            var ordered = images.OrderBy(i => i.ImageId).ToList();
            var rows = BuildPositionRowsFromStream(ordered);

            // Step 2: group rows into stripes by (minPos,maxPos)
            var stripes = rows
                .Select(r => new
                {
                    Row = r,
                    MinPos = r.Min(x => x.PositionId.Value),
                    MaxPos = r.Max(x => x.PositionId.Value),
                    Dir = ComputeRowDir(r) // +1 inc, -1 dec
                })
                .GroupBy(x => (x.MinPos, x.MaxPos))
                .OrderBy(g => g.Key.MinPos)
                .ToList();

            // Step 3: distribute into 2 groups; swap when stripe direction toggles
            var g0 = new List<List<ImageInfo>>();
            var g1 = new List<List<ImageInfo>>();

            bool flip = false; // false: first row->g0, second->g1; true: swapped
            int? lastDir = null;

            foreach (var stripe in stripes)
            {
                // take rows in stripe in stream order (by min ImageId)
                var stripeRows = stripe
                    .OrderBy(x => x.Row.Min(p => p.ImageId))
                    .Select(x => x.Row)
                    .ToList();

                if (stripeRows.Count == 0) continue;

                int dir = stripe.First().Dir;
                if (lastDir.HasValue && dir != lastDir.Value)
                    flip = !flip; // ModiPos: swap assignment when direction changes

                lastDir = dir;

                for (int i = 0; i < stripeRows.Count; i++)
                {
                    var target0 = (!flip) ? (i % 2 == 0) : (i % 2 == 1);
                    if (target0) g0.Add(stripeRows[i]);
                    else g1.Add(stripeRows[i]);
                }
            }

            return new List<List<List<ImageInfo>>>{ g0, g1};
        }

        // =====================
        // ModiPos: NEW - build rows from stream; boundary when sign changes (+/-1) or jump
        // and sort inside each row by PositionId asc
        // =====================
        private static List<List<ImageInfo>> BuildPositionRowsFromStream(List<ImageInfo> ordered)
        {
            if (ordered.Count == 0) return new List<List<ImageInfo>>();

            var rows = new List<List<ImageInfo>>();
            var cur = new List<ImageInfo> { ordered[0] };

            int? lastSign = null;

            for (int i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var now = ordered[i];

                int delta = now.PositionId.Value - prev.PositionId.Value;
                bool ok = (delta == 1 || delta == -1);

                int sign = delta > 0 ? 1 : -1;

                bool newRow = false;
                if (!ok) newRow = true;
                else if (lastSign.HasValue && sign != lastSign.Value) newRow = true;

                if (newRow)
                {
                    // ModiPos: sort inside row by PositionId asc so dec sweep becomes reversed IDs
                    cur = cur.OrderBy(x => x.PositionId.Value).ThenBy(x => x.ImageId).ToList();
                    rows.Add(cur);
                    cur = new List<ImageInfo>();
                    lastSign = null;
                }

                cur.Add(now);
                if (ok) lastSign = sign;
            }
            cur = cur.OrderBy(x => x.PositionId.Value).ThenBy(x => x.ImageId).ToList();
            rows.Add(cur);
            return rows;
        }

        // ModiPos
        private static int ComputeRowDir(List<ImageInfo> row)
        {
            if (row.Count < 2) return 1;
            // After sorting by PositionId asc, direction is always "inc" in Position space,
            // but we need stripe direction in stream: use original ImageId trend vs PositionId.
            // Easiest: compare PositionId of smallest ImageId and largest ImageId in the row.
            var a = row.OrderBy(x => x.ImageId).First().PositionId.Value;
            var b = row.OrderByDescending(x => x.ImageId).First().PositionId.Value;
            return (b - a) >= 0 ? 1 : -1;
        }

        private static List<List<ImageInfo>> BuildMatrixForComponent(
            IReadOnlyList<ImageInfo> images,
            OrderOptions opt,
            (double StepX, double StepY) typical)
        {
            var hasPosition = images.All(i => i.PositionId.HasValue);
            var hasCoords = images.All(i => IsFinite(i.XRobot) && IsFinite(i.YRobot));

            if (hasPosition)
            {
                var matrix = BuildMatrixByPosition(images, opt);

                if (hasCoords)
                    RefineRowsByCoordinates(matrix, opt);

                // Position: you wanted transpose for Up/Down
                if (IsVertical(opt.RobotMovement))
                    matrix = TransposeJagged(matrix);

                return matrix;
            }

            if (hasCoords)
                return BuildRowsByCoordinates(images, opt, typical);

            // Manual: FIXED logic -> DO NOT transpose here.
            return BuildMatrixByManual(images, opt);
        }

        // ============================================================
        // MANUAL (FIXED)
        // - Build along movement axis (Right/Left => rows, Down/Up => columns)
        // - StartCorner decides which side is first
        // - Snake applied once, no extra reverse stacking, no transpose required
        // ============================================================
        private static List<List<ImageInfo>> BuildMatrixByManual(IReadOnlyList<ImageInfo> images, OrderOptions opt)
        {
            int interval = opt.NodeInterval <= 0 ? 1 : opt.NodeInterval;

            var ordered = images.OrderBy(i => i.ImageId).ToList();
            if (ordered.Count == 0) return new List<List<ImageInfo>>();

            int n = ordered.Count;
            int lineCount = (int)Math.Ceiling(n / (double)interval);

            bool horizontal = opt.RobotMovement == RobotMovement.Right || opt.RobotMovement == RobotMovement.Left;

            if (horizontal)
            {
                // Build rows directly
                var placed = new (int Row, List<ImageInfo> Cells)[lineCount];
                int idx = 0;

                for (int li = 0; li < lineCount; li++)
                {
                    int take = Math.Min(interval, n - idx);
                    var seg = ordered.GetRange(idx, take);
                    idx += take;

                    bool forward = opt.RobotMovement == RobotMovement.Right;
                    if ((li % 2) == 1) forward = !forward;

                    var rowCells = new ImageInfo[take];
                    if (forward)
                    {
                        for (int j = 0; j < take; j++) rowCells[j] = seg[j];
                    }
                    else
                    {
                        for (int j = 0; j < take; j++) rowCells[take - 1 - j] = seg[j];
                    }

                    int rowIndex = IsBottom(opt.StartCorner) ? (lineCount - 1 - li) : li;
                    placed[li] = (rowIndex, rowCells.ToList());
                }

                var rows = placed.OrderBy(x => x.Row).Select(x => x.Cells).ToList();

                // StartCorner right side means row is viewed from right to left for the "start" row.
                //if (IsRight(opt.StartCorner))
                //{
                //    foreach (var r in rows)
                //        r.Reverse();
                //}

                // Left movement already handled by "forward" start; keep this consistent:
                // if RobotMovement == Left and StartCorner is Left, 0 must start at top-left for TopLeft+Left? (you didn't request that case)
                return rows;
            }
            else
            {
                // Build columns directly, then render into rows for visualization
                var columns = new List<ImageInfo>[lineCount];
                int idx = 0;

                for (int li = 0; li < lineCount; li++)
                {
                    int take = Math.Min(interval, n - idx);
                    var seg = ordered.GetRange(idx, take);
                    idx += take;

                    bool forward = opt.RobotMovement == RobotMovement.Down;
                    if ((li % 2) == 1) forward = !forward;

                    var colCells = new ImageInfo[take];
                    if (forward)
                    {
                        for (int j = 0; j < take; j++) colCells[j] = seg[j];
                    }
                    else
                    {
                        for (int j = 0; j < take; j++) colCells[take - 1 - j] = seg[j];
                    }

                    int colIndex = IsRight(opt.StartCorner) ? (lineCount - 1 - li) : li;
                    columns[colIndex] = colCells.ToList();
                }

                // Render into rows: r=0..interval-1, c=0..lineCount-1
                var matrix = new List<List<ImageInfo>>();
                for (int r = 0; r < interval; r++)
                {
                    var row = new List<ImageInfo>();
                    for (int c = 0; c < lineCount; c++)
                    {
                        var col = columns[c] ?? new List<ImageInfo>();
                        row.Add(r < col.Count ? col[r] : null);
                    }
                    TrimTrailingNulls(row);
                    matrix.Add(row);
                }

                // For bottom start, flip row order to ensure 0 starts at bottom if needed
                //if (IsBottom(opt.StartCorner))
                //    matrix.Reverse();
                // Row/col ordering will be handled in traversal (EnumerateSnakeLines) based on StartCorner.


                return matrix;
            }
        }

        private static void TrimTrailingNulls(List<ImageInfo> row)
        {
            for (int k = row.Count - 1; k >= 0; k--)
            {
                if (row[k] != null) break;
                row.RemoveAt(k);
            }
        }

        private static bool IsBottom(StartCorner c) => c == StartCorner.BottomLeft || c == StartCorner.BottomRight;
        private static bool IsRight(StartCorner c) => c == StartCorner.TopRight || c == StartCorner.BottomRight;

        // ============================================================
        // POSITION + COORD refine
        // ============================================================
        private static void RefineRowsByCoordinates(List<List<ImageInfo>> matrix, OrderOptions opt)
        {
            bool moveHorizontal = opt.RobotMovement == RobotMovement.Right || opt.RobotMovement == RobotMovement.Left;

            foreach (var row in matrix)
            {
                row.Sort((a, b) =>
                {
                    double pa = PrimaryAxis(a, moveHorizontal, opt.InvertXOnParse);
                    double pb = PrimaryAxis(b, moveHorizontal, opt.InvertXOnParse);
                    return pa.CompareTo(pb);
                });
            }

            ApplySnake(matrix, opt);
        }

        // EstimateTypicalStep
        public static (double StepX, double StepY) EstimatetypicalStep(IReadOnlyList<ImageInfo> images, bool invertX)
        {
            if (images == null) throw new ArgumentNullException(nameof (images));
            if (images.Count < 2) return (double.NaN, double.NaN);

            var stepX = new List<double>();
            var stepY = new List<double>();

            for (int i = 0; i < images.Count; i++)
            {
                var a = images[i];
                double ax = invertX ? -a.XRobot : a.XRobot;
                double ay = a.YRobot;

                double bestDx = double.PositiveInfinity;
                double bestDy = double.PositiveInfinity;
                for (int j = 0; j < images.Count; j++)
                {
                    if (i == j) continue;
                    var b = images[j];
                    double bx = invertX ? -b.XRobot : b.XRobot;
                    double by = b.YRobot;

                    double dx = Math.Abs(ax - bx);
                    double dy = Math.Abs(ay - by);
                    if (dx < 1e-9 && dy < 1e-9) continue;
                    if (dx > dy ) bestDx = Math.Min(bestDx, dx);
                    else if (dy > dx) bestDy = Math.Min(bestDy, dy);
                }
                if (IsFinite(bestDx) && !double.IsInfinity(bestDx)) stepX.Add(bestDx);
                if (IsFinite(bestDy) && !double.IsInfinity(bestDy)) stepY.Add(bestDy);
            }
            stepX.Sort();
            stepY.Sort();
            return (Median(stepX), Median(stepY));
        }

        // ============================================================
        // KEEP: BuildRowsByCoordinates
        // ============================================================
        public static List<List<ImageInfo>> BuildRowsByCoordinates(
            IReadOnlyList<ImageInfo> images,
            OrderOptions opt,
            (double StepX, double StepY) typicalStep)
        {
            if (images == null) throw new ArgumentNullException(nameof(images));
            if (opt == null) throw new ArgumentNullException(nameof(opt));
            if (images.Count == 0) return new List<List<ImageInfo>>();

            bool moveHorizontal = opt.RobotMovement == RobotMovement.Right || opt.RobotMovement == RobotMovement.Left;

            double stepSecondary = moveHorizontal ? typicalStep.StepY : typicalStep.StepX;
            if (!IsFinite(stepSecondary) || stepSecondary <= 0) stepSecondary = 1.0;

            double rowTol = opt.RowFactor * stepSecondary;

            var points = images.ToList();

            points.Sort((a, b) =>
            {
                double sa = SecondaryAxis(a, moveHorizontal, opt.InvertXOnParse);
                double sb = SecondaryAxis(b, moveHorizontal, opt.InvertXOnParse);
                return sa.CompareTo(sb);
            });

            var rows = new List<List<ImageInfo>>();
            var cur = new List<ImageInfo>();
            double? curCenter = null;

            foreach (var p in points)
            {
                double sec = SecondaryAxis(p, moveHorizontal, opt.InvertXOnParse);

                if (cur.Count == 0)
                {
                    cur.Add(p);
                    curCenter = sec;
                    continue;
                }

                if (Math.Abs(sec - curCenter.Value) <= rowTol)
                {
                    cur.Add(p);
                    curCenter = (curCenter.Value * (cur.Count - 1) + sec) / cur.Count;
                }
                else
                {
                    rows.Add(cur);
                    cur = new List<ImageInfo> { p };
                    curCenter = sec;
                }
            }
            if (cur.Count > 0) rows.Add(cur);

            foreach (var r in rows)
            {
                r.Sort((a, b) =>
                {
                    double pa = PrimaryAxis(a, moveHorizontal, opt.InvertXOnParse);
                    double pb = PrimaryAxis(b, moveHorizontal, opt.InvertXOnParse);
                    return pa.CompareTo(pb);
                });
            }

            ApplyZigzagDirection(rows, opt.RobotMovement);
            ApplyStartCornerLineOrder(rows, opt);

            return rows;
        }

        // ============================================================
        // COMPONENTS: bucket+DSU (unchanged)
        // ============================================================
        private static List<IReadOnlyList<ImageInfo>> SplitIntoComponents(
            IReadOnlyList<ImageInfo> images,
            OrderOptions opt,
            bool hasPosition,
            bool hasCoords,
            (double StepX, double StepY) typical)
        {
            if (images.Count == 0) return new List<IReadOnlyList<ImageInfo>>();

            if (hasCoords)
            {
                //var thr = ComputeConnectThreshold(opt, typical);
                return ConnectedComponentsByBuckets(images, opt, typical);
            }

            if (hasPosition)
            {
                var sorted = images.OrderBy(i => i.PositionId.Value).ThenBy(i => i.ImageId).ToList();
                var list = new List<IReadOnlyList<ImageInfo>>();
                var cur = new List<ImageInfo> { sorted[0] };

                for (int i = 1; i < sorted.Count; i++)
                {
                    var gap = Math.Abs(sorted[i].PositionId.Value - sorted[i - 1].PositionId.Value);
                    if (gap > 1)
                    {
                        list.Add(cur);
                        cur = new List<ImageInfo>();
                    }
                    cur.Add(sorted[i]);
                }
                list.Add(cur);
                return list;
            }

            return new List<IReadOnlyList<ImageInfo>> { images.ToList() };
        }

        private static double ComputeConnectThreshold(OrderOptions opt, (double StepX, double StepY) typical)
        {
            bool moveHorizontal = opt.RobotMovement == RobotMovement.Right || opt.RobotMovement == RobotMovement.Left;
            double primary = moveHorizontal ? typical.StepX : typical.StepY;
            if (!IsFinite(primary) || primary <= 0) primary = 1.0;
            var thr = opt.GapFactor * primary;
            if (!IsFinite(thr) || thr <= 0) thr = primary * 2.0;
            return thr;
        }

        private static List<IReadOnlyList<ImageInfo>> ConnectedComponentsByBuckets(
            IReadOnlyList<ImageInfo> images,
            OrderOptions opt,
            (double StepX, double StepY) typical)
        {
            if (images == null) throw new ArgumentNullException(nameof(images));
            if (opt == null) throw new ArgumentNullException(nameof(opt));

            var pts = images.ToList();
            int n = pts.Count;
            if (n == 0) return new List<IReadOnlyList<ImageInfo>>();

            bool invertX = opt.InvertXOnParse;
            bool horizontalMove = opt.RobotMovement == RobotMovement.Right || opt.RobotMovement == RobotMovement.Left;
            double primaryStep = horizontalMove ? typical.StepX : typical.StepY;
            double secondaryStep = horizontalMove ? typical.StepY : typical.StepX;

            if (!IsFinite(primaryStep) || primaryStep <= 0) primaryStep = 1.0;
            if (!IsFinite(secondaryStep) || secondaryStep <= 0) secondaryStep = 1.0;

            // along row/col
            double primaryMax = opt.GapFactor * primaryStep;
            if (!IsFinite(primaryMax) || primaryMax <= 0) primaryMax = primaryStep * 2.0;

            // lock to same row/col band to prevent diagonal bridging
            double secondaryTol = opt.RowFactor * secondaryStep;
            if (!IsFinite(secondaryTol) || secondaryTol < 0) secondaryTol = secondaryStep * 0.6;

            // optional: avoid union for overlapping/near-duplicates
            double primaryMin = Math.Max(0.0, primaryStep * 0.25);
            var dsu = new Dsu(n);

            double cell = Math.Max(primaryMax, secondaryTol);
            if (!IsFinite(cell) || cell <= 0) cell = 1.0;

            var buckets = new Dictionary<long, List<int>>(n * 2);

            for (int i = 0; i < n; i++)
            {
                var p = pts[i];
                double x = invertX ? -p.XRobot : p.XRobot;
                double y = p.YRobot;

                int bx = (int)Math.Floor(x / cell);
                int by = (int)Math.Floor(y / cell);

                long key = Pack(bx, by);
                if (!buckets.TryGetValue(key, out var list))
                    buckets[key] = list = new List<int>(8);
                list.Add(i);
            }

            double primaryMax2 = primaryMax * primaryMax;

            foreach (var kv in buckets)
            {
                Unpack(kv.Key, out int cx, out int cy);

                for (int dxCell = -1; dxCell <= 1; dxCell++)
                {
                    for (int dyCell = -1; dyCell <= 1; dyCell++)
                    {
                        long nk = Pack(cx + dxCell, cy + dyCell);
                        if (!buckets.TryGetValue(nk, out var neigh)) continue;

                        var aList = kv.Value;
                        for (int ia = 0; ia < aList.Count; ia++)
                        {
                            int i = aList[ia];
                            var pi = pts[i];
                            double ix = invertX ? -pi.XRobot : pi.XRobot;
                            double iy = pi.YRobot;

                            for (int jb = 0; jb < neigh.Count; jb++)
                            {
                                int j = neigh[jb];
                                if (j <= i) continue;

                                var pj = pts[j];
                                double jx = invertX ? -pj.XRobot : pj.XRobot;
                                double jy = pj.YRobot;

                                double dx = jx - ix;
                                double dy = jy - iy;
                                double primary = horizontalMove ? Math.Abs(dx) : Math.Abs(dy);
                                double secondary = horizontalMove ? Math.Abs(dy) : Math.Abs(dx);

                                if (secondary > secondaryTol) continue;
                                if (primary < primaryMin || primary > primaryMax) continue;
                                double dist2 = dx * dx + dy * dy;
                                if (dist2 > primaryMax2) continue;
                                dsu.Union(i, j);
                            }
                        }
                    }
                }
            }

            var groups = new Dictionary<int, List<ImageInfo>>();
            for (int i = 0; i < n; i++)
            {
                int root = dsu.Find(i);
                if (!groups.TryGetValue(root, out var list))
                    groups[root] = list = new List<ImageInfo>();
                list.Add(pts[i]);
            }

            return groups.Values
                .OrderBy(g => g.Min(p => p.ImageId))
                .Select(g => (IReadOnlyList<ImageInfo>)g)
                .ToList();
        }

        private sealed class Dsu
        {
            private readonly int[] _p;
            private readonly byte[] _r;

            public Dsu(int n)
            {
                _p = new int[n];
                _r = new byte[n];
                for (int i = 0; i < n; i++) _p[i] = i;
            }

            public int Find(int x)
            {
                while (_p[x] != x)
                {
                    _p[x] = _p[_p[x]];
                    x = _p[x];
                }
                return x;
            }

            public void Union(int a, int b)
            {
                int ra = Find(a);
                int rb = Find(b);
                if (ra == rb) return;

                if (_r[ra] < _r[rb]) _p[ra] = rb;
                else if (_r[ra] > _r[rb]) _p[rb] = ra;
                else { _p[rb] = ra; _r[ra]++; }
            }
        }

        private static long Pack(int x, int y)
        {
            unchecked { return ((long)x << 32) ^ (uint)y; }
        }

        private static void Unpack(long key, out int x, out int y)
        {
            unchecked
            {
                x = (int)(key >> 32);
                y = (int)key;
            }
        }

        // ============================================================
        // Graph + drawing outputs
        // ============================================================
        private static ArrangeGraph BuildArrangeGraph(List<List<ImageInfo>> matrix, RobotMovement movement, StartCorner startCorner)
        {
            var g = new ArrangeGraph();
            if (matrix == null || matrix.Count == 0) return g;

            var traversal = FlattenSnakeTraversal(matrix, movement, startCorner);
            if (traversal.Count == 0) return g;

            // Next/Prev (global)
            for (int i = 0; i < traversal.Count; i++)
            {
                int id = traversal[i];
                var links = g.GetOrCreate(id);

                links.Prev = (i > 0) ? (int?)traversal[i - 1] : null;
                links.Next = (i + 1 < traversal.Count) ? (int?)traversal[i + 1] : null;
            }

            // Optional: LineNext / InterLineNext for debugging draw
            // We'll build by lines used in traversal generation.
            var lines = EnumerateSnakeLines(matrix, movement, startCorner).ToList();
            for (int li = 0; li < lines.Count; li++)
            {
                var line = lines[li];
                for (int k = 0; k < line.Count; k++)
                {
                    var id = line[k];
                    var links = g.GetOrCreate(id);
                    links.LineNext = (k + 1 < line.Count) ? (int?)line[k + 1] : null;
                }

                if (li + 1 < lines.Count && line.Count > 0 && lines[li + 1].Count > 0)
                {
                    int last = line[line.Count - 1];
                    int firstNext = lines[li + 1][0];
                    g.GetOrCreate(last).InterLineNext = firstNext;
                }
            }

            return g;
        }

        private static List<int> FlattenSnakeTraversal(List<List<ImageInfo>> matrix, 
            RobotMovement movement,
            StartCorner startCorner)
        {
            var traversal = new List<int>();
            foreach (var line in EnumerateSnakeLines(matrix, movement, startCorner))
                traversal.AddRange(line);
            return traversal;
        }

        /// <summary>
        /// Generates the traversal as a list of "lines".
        /// - Right/Left: each line is a row; direction alternates per row (snake).
        /// - Down/Up: each line is a column; direction alternates per column (snake).
        /// Assumes matrix has already been normalized (start corner/flip/transpose as you designed).
        /// </summary>
        private static IEnumerable<List<int>> EnumerateSnakeLines(List<List<ImageInfo>> matrix, RobotMovement movement, StartCorner startCorner)
        {
            bool horizontal = movement == RobotMovement.Right || movement == RobotMovement.Left;
            bool startAtBottom = startCorner == StartCorner.BottomLeft || startCorner == StartCorner.BottomRight;
            bool startAtRight = startCorner == StartCorner.TopRight || startCorner == StartCorner.BottomRight;

            if (horizontal)
            {

                //bool row0Forward = movement == RobotMovement.Right;

                //for (int r = 0; r < matrix.Count; r++)
                //{
                //    var row = matrix[r];
                //    if (row == null || row.Count == 0) continue;

                //    var ids = row.Where(x => x != null).Select(x => x.ImageId).ToList();
                //    if (ids.Count == 0) continue;

                //    bool forward = (r % 2 == 0) ? row0Forward : !row0Forward;
                //    if (!forward) ids.Reverse();

                //    yield return ids;
                //}
                // row indices in correct start order
                IEnumerable<int> rowIdxs = startAtBottom
                    ? Enumerable.Range(0, matrix.Count).Reverse()
                    : Enumerable.Range(0, matrix.Count);

                bool line0Forward = (movement == RobotMovement.Right); // within-row direction of first traversed row

                int li = 0;
                foreach (int r in rowIdxs)
                {
                    var row = matrix[r];
                    if (row == null || row.Count == 0) { li++; continue; }

                    var ids = row.Where(x => x != null).Select(x => x.ImageId).ToList();
                    if (ids.Count == 0) { li++; continue; }

                    bool forward = (li % 2 == 0) ? line0Forward : !line0Forward;
                    if (!forward) ids.Reverse();

                    yield return ids;
                    li++;
                }
            }
            else
            {
                int maxCols = 0;
                for (int r = 0; r < matrix.Count; r++)
                    if (matrix[r] != null && matrix[r].Count > maxCols) maxCols = matrix[r].Count;

                //bool col0Forward = movement == RobotMovement.Down;

                //for (int c = 0; c < maxCols; c++)
                //{
                //    var ids = new List<int>();
                //    for (int r = 0; r < matrix.Count; r++)
                //    {
                //        if (matrix[r] == null) continue;
                //        if (c >= matrix[r].Count) continue;
                //        var v = matrix[r][c];
                //        if (v == null) continue;
                //        ids.Add(v.ImageId);
                //    }

                //    if (ids.Count == 0) continue;

                //    bool forward = (c % 2 == 0) ? col0Forward : !col0Forward;
                //    if (!forward) ids.Reverse();

                //    yield return ids;
                //}

                IEnumerable<int> colIdxs = startAtRight
            ? Enumerable.Range(0, maxCols).Reverse()
            : Enumerable.Range(0, maxCols);

                bool line0Forward = (movement == RobotMovement.Down); // within-col direction of first traversed col

                int li = 0;
                foreach (int c in colIdxs)
                {
                    var ids = new List<int>();
                    for (int r = 0; r < matrix.Count; r++)
                    {
                        if (matrix[r] == null) continue;
                        if (c >= matrix[r].Count) continue;
                        var v = matrix[r][c];
                        if (v == null) continue;
                        ids.Add(v.ImageId);
                    }

                    if (ids.Count == 0) { li++; continue; }

                    bool forward = (li % 2 == 0) ? line0Forward : !line0Forward;
                    if (!forward) ids.Reverse();

                    yield return ids;
                    li++;
                }
            }
        }

        private static ImageInfo FirstNonNull(List<ImageInfo> row)
        {
            for (int i = 0; i < row.Count; i++) if (row[i] != null) return row[i];
            return null;
        }

        private static ImageInfo LastNonNull(List<ImageInfo> row)
        {
            for (int i = row.Count - 1; i >= 0; i--) if (row[i] != null) return row[i];
            return null;
        }

        private static Dictionary<int, GridCell> BuildCellIndex(List<List<ImageInfo>> matrix)
        {
            var map = new Dictionary<int, GridCell>();
            if (matrix == null) return map;

            for (int r = 0; r < matrix.Count; r++)
            {
                var row = matrix[r];
                for (int c = 0; c < row.Count; c++)
                {
                    var p = row[c];
                    if (p == null) continue;
                    map[p.ImageId] = new GridCell(r, c);
                }
            }
            return map;
        }

        private static List<PathSegment> BuildPathSegments(
            ArrangeGraph graph,
            Dictionary<int, GridCell> cellById,
            out int? startId)
        {
            startId = null;
            var segs = new List<PathSegment>();
            // [Codex] [Change time: 260323] [Replace obsolete ArrangeGraph.ById alias with LinksById]
            if (graph == null || graph.LinksById.Count == 0) return segs;

            // start = node that is never pointed-to (or smallest id fallback)
            int start = graph.LinksById.Values.Where(x => !x.Prev.HasValue).Select(x => x.ImageId)
                .DefaultIfEmpty(graph.LinksById.Keys.Min())
                .Min();

            startId = start;

            int cur = start;
            var visited = new HashSet<int>();

            while (true)
            {
                if (!visited.Add(cur)) break;
                if (!graph.LinksById.TryGetValue(cur, out var links)) break;
                if (!links.Next.HasValue) break;

                int nxt = links.Next.Value;

                var fromCell = cellById.TryGetValue(cur, out var fc) ? fc : new GridCell(-1, -1);
                var toCell = cellById.TryGetValue(nxt, out var tc) ? tc : new GridCell(-1, -1);

                segs.Add(new PathSegment
                {
                    FromId = cur,
                    ToId = nxt,
                    FromCell = fromCell,
                    ToCell = toCell,
                    Direction = ComputeDirection(fromCell, toCell)
                });

                cur = nxt;
            }

            return segs;
        }

        public static LinkDirection ComputeDirection(GridCell a, GridCell b)
        {
            if (a.Row < 0 || b.Row < 0) return LinkDirection.Jump;

            int dr = b.Row - a.Row;
            int dc = b.Col - a.Col;

            if (dr == 0 && dc == 1) return LinkDirection.Right;
            if (dr == 0 && dc == -1) return LinkDirection.Left;
            if (dr == 1 && dc == 0) return LinkDirection.Down;
            if (dr == -1 && dc == 0) return LinkDirection.Up;

            return LinkDirection.Jump;
        }



        // ============================================================
        // orientation helpers (unchanged for position/coords)
        // ============================================================
        private static void ApplyStartCornerLineOrder(List<List<ImageInfo>> lines, OrderOptions opt)
        {
            bool startAtBottom =
                opt.StartCorner == StartCorner.BottomLeft ||
                opt.StartCorner == StartCorner.BottomRight;

            if (startAtBottom) lines.Reverse();
        }

        private static void ApplySnake(List<List<ImageInfo>> lines, OrderOptions opt)
        {
            bool row0Forward = opt.RobotMovement == RobotMovement.Right || opt.RobotMovement == RobotMovement.Down;

            bool startAtRight =
                opt.StartCorner == StartCorner.TopRight ||
                opt.StartCorner == StartCorner.BottomRight;

            if (startAtRight) row0Forward = !row0Forward;
            if (opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Up)
                row0Forward = !row0Forward;

            for (int r = 0; r < lines.Count; r++)
            {
                bool forward = (r % 2 == 0) ? row0Forward : !row0Forward;
                if (!forward) lines[r].Reverse();
            }
        }

        private static void ApplyZigzagDirection(List<List<ImageInfo>> rows, RobotMovement movement)
        {
            bool row0Forward = movement == RobotMovement.Right || movement == RobotMovement.Down;
            for (int r = 0; r < rows.Count; r++)
            {
                bool forward = (r % 2 == 0) ? row0Forward : !row0Forward;
                if (!forward) rows[r].Reverse();
            }
        }

        private static List<List<ImageInfo>> TransposeJagged(List<List<ImageInfo>> m)
        {
            int rows = m.Count;
            int cols = m.Count == 0 ? 0 : m.Max(r => r.Count);

            var padded = new ImageInfo[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    padded[r, c] = (c < m[r].Count) ? m[r][c] : null;

            var t = new List<List<ImageInfo>>();
            for (int c = 0; c < cols; c++)
            {
                var line = new List<ImageInfo>();
                for (int r = 0; r < rows; r++)
                    if (padded[r, c] != null) line.Add(padded[r, c]);
                t.Add(line);
            }
            return t;
        }

        // Utils
        private static bool IsVertical(RobotMovement m) => m == RobotMovement.Up || m == RobotMovement.Down;
        private static bool IsFinite(double value) => !(double.IsNaN(value) || double.IsInfinity(value));
        private static double Median(List<double> xs)
        {
            if (xs  == null || xs.Count == 0) return double.NaN;
            int n = xs.Count;
            if (n % 2 == 1) return xs[n / 2];
            return (xs[n /2 -1 ] + xs[n / 2]) / 2.0;
        }

        private static double PrimaryAxis(ImageInfo p, bool moveHorizontal, bool invertX)
        {
            if (moveHorizontal) return invertX ? -p.XRobot : p.XRobot;
            return p.YRobot;
        }

        private static double SecondaryAxis(ImageInfo p, bool moveHorizontal, bool invertX)
        {
            if (moveHorizontal) return p.YRobot;
            return invertX ? -p.XRobot : p.XRobot;
        }
    }

    public static class DebugVisualizeArrange
    {
        /// <summary>
        /// Print all components: matrix + startId + traversal (Prev/Next) in console-friendly format.
        /// </summary>
        /// 
        public static void PrintArrangeResult(ArrangeBatchResult result)
        {
            if (result == null)
            {
                Console.WriteLine("ArrangeResult: <null>");
                return;
            }

            Console.WriteLine($"TypicalStep: ({result.TypicalStep.StepX:0.###}, {result.TypicalStep.StepY:0.###})");
            Console.WriteLine($"Components: {result.Components?.Count ?? 0}");
            Console.WriteLine();

            if (result.Components == null) return;

            foreach (var comp in result.Components)
            {
                Console.WriteLine($"=== Component #{comp.Index} ===");
                Console.WriteLine($"StartId: {(comp.StartId.HasValue ? comp.StartId.Value.ToString() : "<null>")}");
                Console.WriteLine($"Items: {comp.Items?.Count ?? 0}");
                Console.WriteLine($"Options: StartCorner={comp.OptionsUsed?.StartCorner}, Movement={comp.OptionsUsed?.RobotMovement}, Mode={comp.OptionsUsed?.Mode}");
                Console.WriteLine();

                PrintMatrix(comp.Matrix);

                //Console.WriteLine();
                //PrintSnakeLinks(comp); // Prev/Next + arrows
                //Console.WriteLine();
            }
        }

        /// <summary>
        /// Print a matrix as fixed-width cells. Null -> ".".
        /// </summary>
        public static void PrintMatrix(List<List<ImageInfo>> matrix, int cellWidth = 3)
        {
            if (matrix == null)
            {
                Console.WriteLine("<matrix=null>");
                return;
            }

            int maxCols = matrix.Count == 0 ? 0 : matrix.Max(r => r?.Count ?? 0);

            Console.WriteLine($"Matrix: rows={matrix.Count}, cols(max)={maxCols}");
            for (int r = 0; r < matrix.Count; r++)
            {
                var row = matrix[r] ?? new List<ImageInfo>();
                var sb = new StringBuilder();
                sb.Append($"[{r:00}] ");

                for (int c = 0; c < maxCols; c++)
                {
                    if (c >= row.Count || row[c] == null)
                    {
                        sb.Append("".PadLeft(cellWidth - 1)).Append('.').Append(' ');
                        continue;
                    }

                    sb.Append(row[c].ImageId.ToString().PadLeft(cellWidth)).Append(' ');
                }

                Console.WriteLine(sb.ToString().TrimEnd());
            }
        }

        /// <summary>
        /// Print traversal with arrows based on comp.Path (FromCell->ToCell).
        /// Also prints Prev/Next for each node (from comp.Graph).
        /// </summary>
        public static void PrintSnakeLinks(ArrangeComponent comp)
        {
            if (comp == null)
            {
                Console.WriteLine("<component=null>");
                return;
            }

            if (comp.Path == null || comp.Path.Count == 0)
            {
                Console.WriteLine("Path: <empty>");
                return;
            }

            Console.WriteLine("Traversal (id arrows):");
            // render as: 0 → 1 → 2 ... with line breaks when direction changes a lot
            var sb = new StringBuilder();
            sb.Append(comp.Path[0].FromId);

            for (int i = 0; i < comp.Path.Count; i++)
            {
                var seg = comp.Path[i];
                sb.Append(' ').Append(ToArrow(seg.Direction)).Append(' ').Append(seg.ToId);
            }
            Console.WriteLine(sb.ToString());

            Console.WriteLine();
            Console.WriteLine("Segments (FromId FromCell -> ToId ToCell, Dir):");
            foreach (var seg in comp.Path)
            {
                Console.WriteLine($"{seg.FromId,3} {seg.FromCell} {ToArrow(seg.Direction)} {seg.ToId,3} {seg.ToCell}  ({seg.Direction})");
            }

            Console.WriteLine();
            Console.WriteLine("Prev/Next table:");
            if (comp.Graph == null || comp.Graph.LinksById.Count == 0)
            {
                Console.WriteLine("<graph empty>");
                return;
            }

            foreach (var id in comp.Graph.LinksById.Keys.OrderBy(x => x))
            {
                var links = comp.Graph.LinksById[id];
                Console.WriteLine($"{id,3}: Prev={(links.Prev.HasValue ? links.Prev.Value.ToString() : "null"),4}  Next={(links.Next.HasValue ? links.Next.Value.ToString() : "null"),4}");
            }
        }

        private static string ToArrow(LinkDirection d)
        {
            switch (d)
            {
                case LinkDirection.Left: return "←";
                case LinkDirection.Right: return "→";
                case LinkDirection.Up: return "↑";
                case LinkDirection.Down: return "↓";
                case LinkDirection.Jump: return "⤴";
                default: return "·";
            }
        }
    }
}
