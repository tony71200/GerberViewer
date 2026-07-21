using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace StitchingImage.Stitch_Tools.RobotManager
{
    public sealed class TraversalGraph
    {
        /// <summary>
        /// TraversalGraph builds a directed graph from an already-built Matrix (List<List<ImageInfo>>),
        /// using ONLY OrderOptions.Mode rules (independent from RobotArrange rules).
        ///
        /// Modes:
        /// - Zigzag: snake rows, ALWAYS start at Matrix[0][0] and go RIGHT on row0.
        /// - Branch: connect each row horizontally LEFT->RIGHT, plus connect HEADs of rows (row0 head -> row1 head -> ...).
        /// - BranchDown: connect each column vertically TOP->BOTTOM, plus connect HEADs of columns (col0 head -> col1 head -> ...).
        ///
        /// Outputs:
        /// - LinksById: per node links (HNext/VNext and optional Prev/Next for Zigzag chain).
        /// - CellById: ImageId -> GridCell for drawing.
        /// - PathSegments: segments created from Flatten() edges.
        /// </summary>
        [Obsolete("Use Link")]
        public sealed class Links : Link { }
        public OrderMode Mode { get; }
        public IReadOnlyList<IReadOnlyList<ImageInfo>> Matrix {  get; }
        public Dictionary<int, Link> LinksById { get; } = new Dictionary<int, Link>();

        /// <summary>For UI draw: ImageId -> (Row, Col) in the given Matrix.</summary>
        /// 
        public Dictionary<int, GridCell> CellById { get; } = new Dictionary<int, GridCell>();
        /// <summary>For UI draw: segments derived from Flatten().</summary>
        /// 
        public List<PathSegment> PathSegments { get; } = new List<PathSegment>();

        private TraversalGraph(OrderMode mode, List<List<ImageInfo>> matrix)
        {
            Mode = mode;
            Matrix = matrix.Select(r => (IReadOnlyList<ImageInfo>)r).ToList();
            BuildCellIndex(matrix);
        }

        public static TraversalGraph Build(List<List<ImageInfo>> matrix, OrderOptions opt)
        {
            if (matrix == null) throw new ArgumentNullException(nameof(matrix));
            if (opt == null) throw new ArgumentNullException(nameof(opt));
            var g = new TraversalGraph(opt.Mode, matrix);

            switch(opt.Mode)
            {
                case OrderMode.Zigzag:
                    g.BuildZigzag(matrix); break;
                case OrderMode.Branch:
                    g.BuildBranch(matrix); break;
                case OrderMode.BranchDown:
                    g.BuildBranchDown(matrix); break;
                default:
                    throw new NotSupportedException($"Unsupport mode {opt.Mode}");
            }

            // Build drawable segments from Flatten edges
            var (fromList, toList) = g.Flatten(matrix);
            g.PathSegments.Clear();
            for (int i = 0; i < fromList.Count; i++)
            {
                var a = fromList[i];
                var b = toList[i];
                var fromCell = g.CellById.TryGetValue(a.ImageId, out var fc) ? fc : new GridCell(-1, -1);
                var toCell = g.CellById.TryGetValue(b.ImageId, out var tc) ? tc : new GridCell(-1, -1);

                g.PathSegments.Add(new PathSegment
                {
                    FromId = a.ImageId,
                    ToId = b.ImageId,
                    FromCell = fromCell,
                    ToCell = toCell,
                    Direction = ComputeDirection(fromCell, toCell)
                });
            }
            return g;
        }

        // [Codex] [Change time: 260323] [Build traversal runtime results directly from RobotArrange output so MainForm can stop depending on RobotOrderer]
        public static TraversalBatchResult BuildBatch(int groupId, ArrangeBatchResult arrange, OrderOptions opt)
        {
            if (arrange == null) throw new ArgumentNullException(nameof(arrange));
            if (opt == null) throw new ArgumentNullException(nameof(opt));

            var components = new List<TraversalComponent>();
            var allPoints = arrange.Components?
                .SelectMany(comp => comp?.Items ?? Array.Empty<ImageInfo>())
                .Where(p => p != null)
                .ToArray() ?? Array.Empty<ImageInfo>();

            var estimated = EstimateTypicalStep(allPoints);
            var dX = arrange.TypicalStep.StepX > 0 ? arrange.TypicalStep.StepX : estimated.Item1;
            var dY = arrange.TypicalStep.StepY > 0 ? arrange.TypicalStep.StepY : estimated.Item2;
            if (dX <= 0) dX = 1.0;
            if (dY <= 0) dY = 1.0;
            var stepPrimary = (opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Right) ? dX : dY;

            foreach (var arrangeComponent in arrange.Components ?? new List<ArrangeComponent>())
            {
                var points = (arrangeComponent?.Items ?? Array.Empty<ImageInfo>()).Where(p => p != null).ToArray();
                var graph = Build(arrangeComponent?.Matrix ?? new List<List<ImageInfo>>(), opt);
                components.Add(new TraversalComponent
                {
                    ComponentIndex = arrangeComponent?.Index ?? components.Count,
                    Points = points,
                    Bounds = Bounds2D.FromPoints(points),
                    Graph = graph,
                    EstimateDistanceX = dX,
                    EstimateDistanceY = dY,
                    SpecialGapEdges = BuildSpecialGapEdges(graph, points, dX),
                    ArrangeComponent = arrangeComponent
                });
            }

            return new TraversalBatchResult
            {
                GroupId = groupId,
                Components = components.ToArray(),
                EstimateDistance = stepPrimary
            };
        }

        public static IEnumerable<(int AId, int BId, EdgeDir Direction)> EnumerateEdges(TraversalGraph graph)
        {
            if (graph?.LinksById == null) yield break;

            foreach (var kv in graph.LinksById)
            {
                var a = kv.Key;
                var link = kv.Value;

                if (link?.HNext.HasValue == true)
                    yield return (a, link.HNext.Value, EdgeDir.Horizontal);

                if (link?.VNext.HasValue == true)
                    yield return (a, link.VNext.Value, EdgeDir.Vertical);
            }
        }

        public static int? GuessRootId(TraversalGraph graph)
        {
            if (graph == null) return null;

            var start = graph.LinksById.Values.FirstOrDefault(link => !link.Prev.HasValue);
            if (start != null) return start.ImageId;

            foreach (var row in graph.Matrix ?? Array.Empty<IReadOnlyList<ImageInfo>>())
            {
                var head = row?.FirstOrDefault(p => p != null);
                if (head != null) return head.ImageId;
            }

            if (graph.LinksById.Count == 0) return null;
            return graph.LinksById.Keys.Min();
        }

        /// <summary>
        /// Flatten returns two parallel lists: From[i] -> To[i] edges.
        /// - Zigzag: returns the single traversal chain edges (N-1 edges).
        /// - Branch: returns all HNext edges (row traversal) + VNext edges (row-head chain).
        /// - BranchDown: returns all VNext edges (col traversal) + HNext edges (col-head chain).
        /// </summary>
        public (List<ImageInfo> From, List<ImageInfo> To) Flatten(List<List<ImageInfo>> matrix)
        {
            var idToInfo = BuildIdToInfo(matrix);

            var from = new List<ImageInfo>();
            var to = new List<ImageInfo>();

            if (Mode == OrderMode.Zigzag)
            {
                // Chain via Next pointers
                var startId = FindZigzagStartId();
                if (!startId.HasValue) return (from, to);

                int cur = startId.Value;
                var visited = new HashSet<int>();
                while (true)
                {
                    if (!visited.Add(cur)) break;
                    if (!LinksById.TryGetValue(cur, out var links)) break;
                    if (!links.Next.HasValue) break;

                    int nxt = links.Next.Value;
                    if (idToInfo.TryGetValue(cur, out var a) && idToInfo.TryGetValue(nxt, out var b))
                    {
                        from.Add(a);
                        to.Add(b);
                    }
                    cur = nxt;
                }

                return (from, to);
            }

            // Branch / BranchDown: return all edges (unique)
            var seen = new HashSet<(int A, int B)>();

            foreach (var kv in LinksById)
            {
                var aId = kv.Key;
                var links = kv.Value;

                if (links.HNext.HasValue)
                {
                    var e = (aId, links.HNext.Value);
                    if (seen.Add(e) && idToInfo.TryGetValue(e.Item1, out var a) && idToInfo.TryGetValue(e.Item2, out var b))
                    {
                        from.Add(a);
                        to.Add(b);
                    }
                }

                if (links.VNext.HasValue)
                {
                    var e = (aId, links.VNext.Value);
                    if (seen.Add(e) && idToInfo.TryGetValue(e.Item1, out var a) && idToInfo.TryGetValue(e.Item2, out var b))
                    {
                        from.Add(a);
                        to.Add(b);
                    }
                }
            }

            return (from, to);
        }

        /// <summary>Debug: print edges (FromId -> ToId) with arrow directions based on grid cells.</summary>
        public void DebugPrintEdges()
        {
            Console.WriteLine($"TraversalGraph Mode={Mode}");
            Console.WriteLine($"Nodes={LinksById.Count}, Cells={CellById.Count}");

            var (from, to) = Flatten(Matrix.Select(r => r.ToList()).ToList());
            for (int i = 0; i < from.Count; i++)
            {
                var a = from[i];
                var b = to[i];

                var fc = CellById.TryGetValue(a.ImageId, out var c1) ? c1 : new GridCell(-1, -1);
                var tc = CellById.TryGetValue(b.ImageId, out var c2) ? c2 : new GridCell(-1, -1);
                var dir = ComputeDirection(fc, tc);

                Console.WriteLine($"{a.ImageId,4} {fc} {Arrow(dir)} {b.ImageId,4} {tc}  ({dir})");
            }
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

        #region Mode Builder
        private void BuildZigzag(List<List<ImageInfo>> matrix)
        {
            // Zigzag: start at matrix[0][0], go RIGHT on row0, alternate per row.
            var rows = NormalizeRows(matrix);
            if (rows.Count == 0) return;

            var traversal = new List<int>();
            for (int r = 0; r < rows.Count; r++)
            {
                var ids = rows[r].Select(x => x.ImageId).ToList();
                bool forward = (r % 2 == 0); // row0 forward (right)
                if (!forward) ids.Reverse();
                traversal.AddRange(ids);

                // HNext within this directional row
                for (int i = 0; i + 1 < ids.Count; i++)
                    GetOrCreate(ids[i]).HNext = ids[i + 1];
            }

            // VNext: tail(row r) -> head(row r+1) (classic zigzag)
            for (int r = 0; r + 1 < rows.Count; r++)
            {
                var aRow = rows[r];
                var bRow = rows[r + 1];

                var aIds = aRow.Select(x => x.ImageId).ToList();
                var bIds = bRow.Select(x => x.ImageId).ToList();

                bool aForward = (r % 2 == 0);
                bool bForward = ((r + 1) % 2 == 0);

                if (!aForward) aIds.Reverse();
                if (!bForward) bIds.Reverse();

                int tail = aIds[aIds.Count - 1];
                int head = bIds[0];
                GetOrCreate(tail).VNext = head;
            }

            // Prev/Next (linear chain)
            for (int i = 0; i < traversal.Count; i++)
            {
                var links = GetOrCreate(traversal[i]);
                links.Prev = (i > 0) ? (int?)traversal[i - 1] : null;
                links.Next = (i + 1 < traversal.Count) ? (int?)traversal[i + 1] : null;
            }
        }

        private void BuildBranch(List<List<ImageInfo>> matrix)
        {
            // Branch: start at matrix[0][0], connect each row LEFT->RIGHT,
            // and connect row heads (first non-null in each row) via VNext.
            var rows = NormalizeRows(matrix);
            if (rows.Count == 0) return;

            // HNext for each row (always left->right)
            foreach (var row in rows)
            {
                var ids = row.Select(x => x.ImageId).ToList();
                for (int i = 0; i + 1 < ids.Count; i++)
                    GetOrCreate(ids[i]).HNext = ids[i + 1];
            }

            // VNext for heads of rows
            var heads = rows.Select(r => r[0].ImageId).ToList();
            for (int i = 0; i + 1 < heads.Count; i++)
                GetOrCreate(heads[i]).VNext = heads[i + 1];
        }

        private void BuildBranchDown(List<List<ImageInfo>> matrix)
        {
            // BranchDown: start at matrix[0][0], connect each column TOP->BOTTOM (VNext),
            // and connect column heads (top cell of each column) via HNext.
            int maxCols = matrix.Count == 0 ? 0 : matrix.Max(r => r?.Count ?? 0);
            if (maxCols == 0) return;

            // Build columns (top->bottom)
            var colHeads = new List<int>();

            for (int c = 0; c < maxCols; c++)
            {
                var col = new List<ImageInfo>();
                for (int r = 0; r < matrix.Count; r++)
                {
                    if (matrix[r] == null) continue;
                    if (c >= matrix[r].Count) continue;
                    var v = matrix[r][c];
                    if (v == null) continue;
                    col.Add(v);
                }

                if (col.Count == 0) continue;
                colHeads.Add(col[0].ImageId);

                // VNext within column
                for (int i = 0; i + 1 < col.Count; i++)
                    GetOrCreate(col[i].ImageId).VNext = col[i + 1].ImageId;
            }

            // HNext across column heads (left->right)
            for (int i = 0; i + 1 < colHeads.Count; i++)
                GetOrCreate(colHeads[i]).HNext = colHeads[i + 1];
        }
        #endregion

        #region Helpers
        // private Links GetOrCreate(int imageId)
        private Link GetOrCreate(int imageId)
        {
            if (!LinksById.TryGetValue(imageId, out var links))
            {
                // CHANGE:
                // links = new Links { ImageId = imageId };
                links = new Link { ImageId = imageId };
                LinksById[imageId] = links;
            }
            return links;
        }

        private void BuildCellIndex(List<List<ImageInfo>> matrix)
        {
            CellById.Clear();
            for (int r = 0; r < matrix.Count; r++)
            {
                var row = matrix[r];
                if (row == null) continue;
                for (int c = 0; c < row.Count; c++)
                {
                    var v = row[c];
                    if (v == null) continue;
                    CellById[v.ImageId] = new GridCell(r, c);
                }
            }
        }

        private static Dictionary<int, ImageInfo> BuildIdToInfo(List<List<ImageInfo>> matrix)
        {
            var map = new Dictionary<int, ImageInfo>();
            for (int r = 0; r < matrix.Count; r++)
            {
                var row = matrix[r];
                if (row == null) continue;

                for (int c = 0; c < row.Count; c++)
                {
                    var v = row[c];
                    if (v == null) continue;
                    map[v.ImageId] = v;
                }
            }
            return map;
        }

        private static List<List<ImageInfo>> NormalizeRows(List<List<ImageInfo>> matrix)
        {
            // Remove nulls, drop empty rows.
            var rows = new List<List<ImageInfo>>();
            foreach (var row in matrix)
            {
                if (row == null) continue;
                var cleaned = row.Where(x => x != null).ToList();
                if (cleaned.Count > 0) rows.Add(cleaned);
            }
            return rows;
        }

        private static HashSet<(int AId, int BId)> BuildSpecialGapEdges(TraversalGraph graph, ImageInfo[] points, double estimateX)
        {
            var result = new HashSet<(int AId, int BId)>();
            if (graph?.LinksById == null || points == null || points.Length == 0 || estimateX <= 0)
                return result;

            var byId = points.ToDictionary(p => p.ImageId);
            foreach (var edge in EnumerateEdges(graph))
            {
                if (!byId.TryGetValue(edge.AId, out var from) || !byId.TryGetValue(edge.BId, out var to))
                    continue;

                var dx = Math.Abs(to.XRobot - from.XRobot);
                if (dx < 2 * estimateX)
                    result.Add((from.ImageId, to.ImageId));
            }

            return result;
        }

        private static (double, double) EstimateTypicalStep(ImageInfo[] pts)
        {
            if (pts == null || pts.Length < 2) return (0.0, 0.0);

            var nearestX = new double[pts.Length];
            var nearestY = new double[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                double bestX = double.PositiveInfinity;
                double bestY = double.PositiveInfinity;
                for (int j = 0; j < pts.Length; j++)
                {
                    if (i == j) continue;
                    var dx = Math.Abs(pts[j].XRobot - pts[i].XRobot);
                    var dy = Math.Abs(pts[j].YRobot - pts[i].YRobot);
                    if (dx > 1e-9 && dx < bestX) bestX = dx;
                    if (dy > 1e-9 && dy < bestY) bestY = dy;
                }
                nearestX[i] = double.IsPositiveInfinity(bestX) ? 0.0 : bestX;
                nearestY[i] = double.IsPositiveInfinity(bestY) ? 0.0 : bestY;
            }

            Array.Sort(nearestX);
            Array.Sort(nearestY);
            return (MedianOfPositive(nearestX), MedianOfPositive(nearestY));
        }

        private static double MedianOfPositive(double[] values)
        {
            var positives = values?.Where(v => v > 1e-9).ToArray() ?? Array.Empty<double>();
            if (positives.Length == 0) return 0.0;
            Array.Sort(positives);
            int mid = positives.Length / 2;
            return positives.Length % 2 == 1 ? positives[mid] : 0.5 * (positives[mid - 1] + positives[mid]);
        }

        private int? FindZigzagStartId()
        {
            // Zigzag chain start: node with Prev == null (if present), else smallest id
            var candidates = LinksById.Values.Where(x => !x.Prev.HasValue).Select(x => x.ImageId).ToList();
            if (candidates.Count == 1) return candidates[0];
            if (LinksById.Count == 0) return null;
            return LinksById.Keys.Min();
        }

        //private static LinkDirection ComputeDirection(GridCell a, GridCell b)
        //{
        //    if (a.Row < 0 || b.Row < 0) return LinkDirection.Jump;

        //    int dr = b.Row - a.Row;
        //    int dc = b.Col - a.Col;

        //    if (dr == 0 && dc == 1) return LinkDirection.Right;
        //    if (dr == 0 && dc == -1) return LinkDirection.Left;
        //    if (dr == 1 && dc == 0) return LinkDirection.Down;
        //    if (dr == -1 && dc == 0) return LinkDirection.Up;

        //    return LinkDirection.Jump;
        //}

        private static string Arrow(LinkDirection d)
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
        #endregion
    }
}
