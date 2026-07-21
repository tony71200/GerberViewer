using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StitchingImage.Stitch_Tools.RobotManager
{
// [Codex] [Change time: 260319] [Align ordering comments and helper names with Zigzag business terminology]
    // (Connected-components + cluster rows by Y + new zigzag law)
    // =======================================================
    public static class RobotOrderer
    {
        //private static ImageInfo[,] transform(ImageInfo[,] imageMatrix)
        //{
        //    // TODO: Transform matrix from Array [r][c] to Array[c][r]
        //    if (imageMatrix == null) return null;


        //}
        public static OrderedGroupResult BuildOrdersForGroup(int groupId, ImageInfo[] images, OrderOptions opt)
        {
            if (images == null) throw new ArgumentNullException(nameof(images));
            if (images.Length == 0)
            {
                return new OrderedGroupResult
                {
                    GroupId = groupId,
                    Components = new OrderComponent[0],
                    EstimateDistance = 0.0,
                };
            }

            // [Codex] [Change time: 260318] [Prefer available metadata and only enter manual row/column fallback when both metadata sources are missing]

            var hasCoordinateMetadata = images.All(i => !double.IsNaN(i.XRobot) && !double.IsNaN(i.YRobot));
            var hasPositionMetadata = images.All(i => i.PositionId.HasValue);
            var useManualFallback = !hasCoordinateMetadata && !hasPositionMetadata;
            var effectiveClusterOrder = hasPositionMetadata
                ? ClusterOrderMode.Position
                : (hasCoordinateMetadata ? ClusterOrderMode.Coordinates : opt.ClusterOrder);

            bool horizontalMove = (opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Right);

            double dX = 1.0;
            double dY = 1.0;
            double rowTol = 1.0;
            double stepPrimary = 1.0;
            double connectThrMax = 1.0;
            double primaryTol = 0.1;

            List<ImageInfo[]> comps;
            if (useManualFallback)
            {
                // [Codex] [Change time: 260318] [Manual row/column fallback avoids metadata-based clustering and uses ImageId sequence only]
//                // Fly-node mode intentionally avoids coordinate-based clustering/step estimation.
//                comps = new List<ImageInfo[]> { images.OrderBy(p => p.ImageId).ToArray() };
                comps = new List<ImageInfo[]> { images.OrderBy(p => p.ImageId).ToArray() };
            }
            else
            {
                var estimated = EstimateTypicalStep(images);
                dX = estimated.Item1;
                dY = estimated.Item2;
                if (dX <= 0) dX = 1.0;
                if (dY <= 0) dY = 1.0;

                rowTol = horizontalMove
                    ? Math.Max(1e-9, opt.RowFactor * dY)
                    : Math.Max(1e-9, opt.RowFactor * dX);

                stepPrimary = horizontalMove ? dX : dY;
                connectThrMax = Math.Max(1e-9, opt.GapFactor * stepPrimary);
                primaryTol = Math.Max(1e-9, 0.3 * dX);

                if (effectiveClusterOrder == ClusterOrderMode.Position)
                {
                    comps = BuildComponentsByPosition(images, opt);
                }
                else
                {
                    comps = ConnectedComponents(images, connectThrMax, primaryTol, horizontalMove);
                    comps.Sort((a, b) => CompareComponents(a, b, opt));
                }
            }

            var components = new List<OrderComponent>();
            for (int ci = 0; ci < comps.Count; ci++)
            {
                var pts = comps[ci];
                // [Codex] [Change time: 260318] [Split manual row and manual column fallback into explicit business-level branches]
//                var rows = shouldUseFlyNode
//                    ? BuildRowsByFlyNodeTraditional(pts, opt)
//                    : (opt.ClusterOrder == ClusterOrderMode.Position
//                        ? BuildRowsByPosition(pts, opt, shouldUseFlyNode: false)
//                        : BuildRowsByCoordinates(pts, rowTol, opt));
                var rows = useManualFallback
                    ? (effectiveClusterOrder == ClusterOrderMode.ManualColumn
                        ? BuildManualColumns(pts, opt)
                        : BuildManualRows(pts, opt))
                    : (effectiveClusterOrder == ClusterOrderMode.Position
                        ? BuildRowsByPosition(pts, opt)
                        : BuildRowsByCoordinates(pts, rowTol, opt));

//                var graph = shouldUseFlyNode
//                    ? BuildGraphFlyNode(rows, opt)
//                    : BuildGraph(rows, opt);
                // [Codex] [Change time: 260320] [Remove old graph builders after ImageGrid parity verification and use only ImageGrid graph construction]
//                var imageGrid = ToImageGrid(rows, opt);
//                var graphNew = BuildGraphFromGrid(imageGrid, opt);
//
//                var graph = useManualFallback
//                    ? BuildGraphManual(rows, opt)
//                    : BuildGraph(rows, opt);
//
//                ValidateGraphRefactor(graph, graphNew, groupId, ci);
                var graph = BuildGraphFromGrid(ToImageGrid(rows, opt), opt);
                var specialGapEdges = BuildSpecialGapEdges(graph, pts, dX);
                components.Add(new OrderComponent
                {
                    ComponentIndex = ci,
                    Points = pts,
                    Bounds = Bounds2D.FromPoints(pts),
                    Graph = graph,
                    EstimateDistanceX = dX,
                    EstimateDistanceY = dY,
                    SpecialGapEdges = specialGapEdges,
                });
            }
                
            return new OrderedGroupResult
            {
                GroupId = groupId,
                Components = components.ToArray(),
                EstimateDistance = stepPrimary
                
            };
        }

        private static HashSet<(int AId, int BId)> BuildSpecialGapEdges(OrderGraph graph, ImageInfo[] points, double estimateX)
        {
            var result = new HashSet<(int AId, int BId)>();
            if (graph?.LinksById == null || points == null || points.Length == 0 || estimateX <= 0)
                return result;

            var byId = points.ToDictionary(p => p.ImageId);
            foreach (var kv in graph.LinksById)
            {
                var fromId = kv.Key;
                if (!byId.TryGetValue(fromId, out var from))
                    continue;

                if (kv.Value.HNext.HasValue && byId.TryGetValue(kv.Value.HNext.Value, out var toH))
                    TryAddSpecial(result, from, toH, estimateX);

                if (kv.Value.VNext.HasValue && byId.TryGetValue(kv.Value.VNext.Value, out var toV))
                    TryAddSpecial(result, from, toV, estimateX);
            }

            return result;
        }

        private static void TryAddSpecial(HashSet<(int AId, int BId)> result, ImageInfo from, ImageInfo to, double estimateX)
        {
            var dx = Math.Abs(to.XRobot - from.XRobot);
            if (dx < 2 * estimateX)
                result.Add((from.ImageId, to.ImageId));
        }

        #region Logic in Order
        private static (double, double) EstimateTypicalStep(ImageInfo[] pts)
        {
            if (pts.Length < 2) return (0.0,0.0);

            var nearestX = new double[pts.Length];
            var nearestY = new double[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                double bestX = double.PositiveInfinity;
                double bestY = double.PositiveInfinity;
                var xi = pts[i].XRobot;
                var yi = pts[i].YRobot;

                for (int j = 0; j < pts.Length; j++)
                {
                    if (i == j) continue;
                    var dx = xi - pts[j].XRobot;
                    var dy = yi - pts[j].YRobot;
                    var d = Math.Sqrt(dx * dx + dy * dy);
                    if ((Math.Abs(dx) > Math.Abs(dy)) && d < bestX)
                    {
                        bestX = d;
                    }
                    else if ((Math.Abs(dx) < Math.Abs(dy)) && (d < bestY))
                    {
                        bestY = d;
                    }
                }
                
                nearestX[i] = (double.IsNaN(bestX) ||double.IsInfinity(bestX)) ? 0 : bestX;
                nearestY[i] = (double.IsNaN(bestY) ||double.IsInfinity(bestY)) ? 0 : bestY;
            }
            Array.Sort(nearestX);
            Array.Sort(nearestY);
            return (Median(nearestX), Median(nearestY));
        }

        private static double Median(double[] sorted)
        {
            if (sorted.Length == 0) return 0;
            int n = sorted.Length;
            if ((n & 1) == 1) return sorted[n / 2];
            return (sorted[(n / 2) - 1] + sorted[n / 2]) / 2.0;
        }

        private static int CompareComponents(ImageInfo[] a, ImageInfo[] b, OrderOptions opt)
        {
            var ba = Bounds2D.FromPoints(a);
            var bb = Bounds2D.FromPoints(b);

            bool horizontalMove = (opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Right);

            if (horizontalMove)
            {
                bool startTop = (opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.TopRight);
                double ya = startTop ? ba.MaxY : ba.MinY;
                double yb = startTop ? bb.MaxY : bb.MinY;
                int cmpY = startTop ? yb.CompareTo(ya) : ya.CompareTo(yb);
                if (cmpY != 0) return cmpY;

                bool startLeft = (opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.BottomLeft);
                double xa = startLeft ? ba.MinX : ba.MaxX;
                double xb = startLeft ? bb.MinX : bb.MaxX;
                return startLeft ? xa.CompareTo(xb) : xb.CompareTo(xa);
            }

            bool startLeftPrimary = (opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.BottomLeft);
            double xaPrimary = startLeftPrimary ? ba.MinX : ba.MaxX;
            double xbPrimary = startLeftPrimary ? bb.MinX : bb.MaxX;
            int cmpX = startLeftPrimary ? xaPrimary.CompareTo(xbPrimary) : xbPrimary.CompareTo(xaPrimary);
            if (cmpX != 0) return cmpX;

            bool startTopSecondary = (opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.TopRight);
            double yaSecondary = startTopSecondary ? ba.MaxY : ba.MinY;
            double ybSecondary = startTopSecondary ? bb.MaxY : bb.MinY;
            return startTopSecondary ? ybSecondary.CompareTo(yaSecondary) : yaSecondary.CompareTo(ybSecondary);
        }

        private static List<ImageInfo[]> ConnectedComponentsGrid(ImageInfo[] pts,
            double thrMax,
            double thrMin,
            bool horizontalMove,
            double secondaryTol)
        {
            if (pts == null) throw new ArgumentNullException(nameof(pts));
            if (pts.Length == 0) return new List<ImageInfo[]>();
            if (thrMax <= 0) throw new ArgumentOutOfRangeException(nameof(thrMax));
            if (thrMin < 0) throw new ArgumentOutOfRangeException(nameof(thrMin));
            if (secondaryTol < 0) throw new ArgumentOutOfRangeException(nameof(secondaryTol));

            int n = pts.Length;
            var visited = new bool[n];
            var comps = new List<ImageInfo[]>();

            double cell = thrMax;

            var buckets = new Dictionary<long, List<int>>(n * 2);
            for (int i = 0; i < n; i++)
            {
                var (gx, gy) = GridCoord(pts[i].XRobot, pts[i].YRobot, cell);
                long key = Pack(gx, gy);
                if (!buckets.TryGetValue(key, out var list))
                    buckets[key] = list = new List<int>(8);
                list.Add(i);
            }

            for (int i = 0; i < n; i++)
            {
                if (visited[i]) continue;

                var q = new Queue<int>();
                var idxs = new List<int>();

                visited[i] = true;
                q.Enqueue(i);

                while (q.Count > 0)
                {
                    int u = q.Dequeue();
                    idxs.Add(u);

                    var xu = pts[u].XRobot;
                    var yu = pts[u].YRobot;

                    var (ugx, ugy) = GridCoord(xu, yu, cell);

                    // search neighbor cells (3x3)
                    for (int dxCell = -1; dxCell <= 1; dxCell++)
                    {
                        for (int dyCell = -1; dyCell <= 1; dyCell++)
                        {
                            long key = Pack(ugx + dxCell, ugy + dyCell);
                            if (!buckets.TryGetValue(key, out var cand)) continue;

                            for (int ci = 0; ci < cand.Count; ci++)
                            {
                                int v = cand[ci];
                                if (visited[v]) continue;
                                if (v == u) continue;

                                var xv = pts[v].XRobot;
                                var yv = pts[v].YRobot;

                                var dx = xu - xv;
                                var dy = yu - yv;

                                var primary = horizontalMove ? Math.Abs(dx) : Math.Abs(dy);
                                var secondary = horizontalMove ? Math.Abs(dy) : Math.Abs(dx);

                                // IMPORTANT: lock to row/col to prevent bridging
                                if (secondary > secondaryTol) continue;

                                // primary must be within thresholds (too small OR too big => split)
                                if (primary < thrMin || primary > thrMax) continue;

                                // optional: ensure Euclidean not crazy (kept, but not required if secondaryTol is tight)
                                var d2 = dx * dx + dy * dy;
                                if (d2 > thrMax * thrMax) continue;

                                visited[v] = true;
                                q.Enqueue(v);
                            }
                        }
                    }
                }
                comps.Add(idxs.Select(k => pts[k]).ToArray());
            }
            return comps;
        }

        private static (int gx, int gy) GridCoord(double x, double y, double cell)
        {
            int gx = (int)Math.Floor(x / cell);
            int gy = (int)Math.Floor(y / cell);
            return (gx, gy);
        }

        // Pack two ints into one long key
        private static long Pack(int a, int b)
        {
            unchecked
            {
                return ((long)a << 32) ^ (uint)b;
            }
        }

        private static List<ImageInfo[]> ConnectedComponents(ImageInfo[] pts, double thrMax, double thrMin, bool horizontalMove)
        {
            int n = pts.Length;
            var visited = new bool[n];
            var comps = new List<ImageInfo[]>();

            for (int i = 0; i < n; i++)
            {
                if (visited[i]) continue;

                var q = new Queue<int>();
                var idxs = new List<int>();

                visited[i] = true;
                q.Enqueue(i);

                while (q.Count > 0)
                {
                    int u = q.Dequeue();
                    idxs.Add(u);

                    var xu = pts[u].XRobot;
                    var yu = pts[u].YRobot;

                    for (int v = 0; v < n; v++)
                    {
                        if (visited[v]) continue;
                        var dx = xu - pts[v].XRobot;
                        var dy = yu - pts[v].YRobot;
                        var d = Math.Sqrt(dx * dx + dy * dy);
                        var primaryDelta = horizontalMove ? Math.Abs(dx) : Math.Abs(dy);
                        if (d < thrMax && Math.Abs(dx) < thrMax && Math.Abs(dx) > thrMin)
                        {
                            visited[v] = true;
                            q.Enqueue(v);
                        }
                    }
                }

                comps.Add(idxs.Select(k => pts[k]).ToArray());
            }

            return comps;
        }

        private static OrderedRow[] BuildRowsByCoordinates(ImageInfo[] pts, double rowTol, OrderOptions opt)
        {
            var rows = ClusterRowsBySecondaryAxis(pts, rowTol, opt);
            bool horizontalMove = (opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Right);

            foreach (var row in rows)
            {
                row.Sort((a, b) => GetPrimaryAxisValue(a, horizontalMove)
                    .CompareTo(GetPrimaryAxisValue(b, horizontalMove)));
            }

// [Codex] [Change time: 260319] [Rename helper usage to Zigzag terminology while keeping enum symbol OrderMode.Snake]
//            return ApplySnakeDirection(rows, opt);
            return ApplyZigzagDirection(rows, opt);
        }

        private sealed class PositionRow
        {
            public int RowIndex { get; set; }
            public int Direction { get; set; }
            public List<ImageInfo> Items { get; } = new List<ImageInfo>();
        }

        // [Codex] [Change time: 260318] [Position-based grouping now relies only on real PositionId metadata]
//        private static List<ImageInfo[]> BuildComponentsByPosition(ImageInfo[] pts, OrderOptions opt, bool shouldUseFlyNode)
//        {
//            var rows = BuildPositionRows(pts, opt, shouldUseFlyNode);
//            var orderBuckets = new Dictionary<int, List<PositionRow>>();
        private static List<ImageInfo[]> BuildComponentsByPosition(ImageInfo[] pts, OrderOptions opt)
        {
            var rows = BuildPositionRows(pts);
            var orderBuckets = new Dictionary<int, List<PositionRow>>();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowParity = i % 2;
                var orderIndex = row.Direction >= 0 ? rowParity : 1 - rowParity;
                if (!orderBuckets.TryGetValue(orderIndex, out var bucket))
                {
                    bucket = new List<PositionRow>();
                    orderBuckets[orderIndex] = bucket;
                }
                bucket.Add(row);
            }

            var comps = new List<ImageInfo[]>();
            foreach (var orderIndex in orderBuckets.Keys.OrderBy(x => x))
            {
                var points = orderBuckets[orderIndex]
                    .OrderBy(r => r.RowIndex)
                    .SelectMany(r => r.Items)
                    .ToArray();
                comps.Add(points);
            }

            return comps;
        }

        // [Codex] [Change time: 260318] [Position-based rows should not borrow manual interval semantics]
//        private static OrderedRow[] BuildRowsByPosition(ImageInfo[] pts, OrderOptions opt, bool shouldUseFlyNode)
//        {
//            var rows = BuildPositionRows(pts, opt, shouldUseFlyNode);
        private static OrderedRow[] BuildRowsByPosition(ImageInfo[] pts, OrderOptions opt)
        {
            var rows = BuildPositionRows(pts);
            return rows.Select((r, idx) => new OrderedRow
            {
                RowIndex = idx,
                Sequence = r.Items.ToArray()
            }).ToArray();
        }

        // [Codex] [Change time: 260318] [Replace FlyNode fallback with explicit manual row and manual column builders]
//        private static OrderedRow[] BuildRowsByFlyNodeTraditional(ImageInfo[] pts, OrderOptions opt)
//        {
//            var sorted = pts.OrderBy(p => p.ImageId).ToArray();
//            var interval = Math.Max(1, opt?.FlyNodeInterval ?? 11);
//            var horizontalMove = opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Right;
//            var startTop = opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.TopRight;
//            var startLeft = opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.BottomLeft;
//
//            var lanes = new List<List<ImageInfo>>();
//            for (int i = 0; i < sorted.Length; i += interval)
//                lanes.Add(sorted.Skip(i).Take(interval).ToList());
//
//            IEnumerable<List<ImageInfo>> orderedLanes = horizontalMove
//                ? (startTop ? lanes : lanes.AsEnumerable().Reverse())
//                : (startLeft ? lanes : lanes.AsEnumerable().Reverse());
//
//            bool firstLaneForward = horizontalMove
//                ? (opt.RobotMovement == RobotMovement.Right)
//                : (opt.RobotMovement == RobotMovement.Down);
//
//            var travelRows = new List<List<ImageInfo>>();
//            int laneIdx = 0;
//            foreach (var lane in orderedLanes)
//            {
//                var seq = lane.ToList();
//                var laneForward = (laneIdx % 2 == 0) ? firstLaneForward : !firstLaneForward;
//                if (!laneForward)
//                    seq.Reverse();
//
//                travelRows.Add(seq);
//                laneIdx++;
//            }
//
//            return travelRows.Select((seq, idx) => new OrderedRow
//            {
//                RowIndex = idx,
//                Sequence = seq.ToArray()
//            }).ToArray();
//        }
        private static OrderedRow[] BuildManualRows(ImageInfo[] pts, OrderOptions opt)
        {
            var sorted = pts.OrderBy(p => p.ImageId).ToArray();
            var interval = Math.Max(1, opt?.NodeInterval ?? 11);
            var rowGroups = ChunkByInterval(sorted, interval);
            return BuildManualLanes(rowGroups, opt, horizontalLayout: true);
        }

        private static OrderedRow[] BuildManualColumns(ImageInfo[] pts, OrderOptions opt)
        {
            var sorted = pts.OrderBy(p => p.ImageId).ToArray();
            var interval = Math.Max(1, opt?.NodeInterval ?? 11);
            var columnGroups = ChunkByInterval(sorted, interval);
            return BuildManualLanes(columnGroups, opt, horizontalLayout: false);
        }

        private static List<List<ImageInfo>> ChunkByInterval(ImageInfo[] sorted, int interval)
        {
            var groups = new List<List<ImageInfo>>();
            for (int i = 0; i < sorted.Length; i += interval)
                groups.Add(sorted.Skip(i).Take(interval).ToList());
            return groups;
        }

        private static OrderedRow[] BuildManualLanes(List<List<ImageInfo>> lanes, OrderOptions opt, bool horizontalLayout)
        {
            var horizontalMove = opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Right;
            var startTop = opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.TopRight;
            var startLeft = opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.BottomLeft;

            IEnumerable<List<ImageInfo>> orderedLanes = horizontalLayout
                ? (startTop ? lanes : lanes.AsEnumerable().Reverse())
                : (startLeft ? lanes : lanes.AsEnumerable().Reverse());

            bool firstLaneForward;
            if (horizontalLayout)
                firstLaneForward = horizontalMove ? (opt.RobotMovement == RobotMovement.Right) : (opt.RobotMovement == RobotMovement.Down);
            else
                firstLaneForward = horizontalMove ? startTop : (opt.RobotMovement == RobotMovement.Down);

            var arranged = new List<OrderedRow>();
            int laneIdx = 0;
            foreach (var lane in orderedLanes)
            {
                var seq = lane.ToList();
                var laneForward = (laneIdx % 2 == 0) ? firstLaneForward : !firstLaneForward;
                if (!laneForward)
                    seq.Reverse();

                arranged.Add(new OrderedRow
                {
                    RowIndex = laneIdx,
                    Sequence = seq.ToArray()
                });
                laneIdx++;
            }

            return arranged.ToArray();
        }

        // [Codex] [Change time: 260318] [Position ordering should use PositionId directly instead of synthetic FlyNode positions]
//        private static int GetEffectivePositionId(ImageInfo p, int index, int flyNodeInterval, bool useFlyNode)
//        {
//            if (!useFlyNode && p.PositionId.HasValue)
//                return p.PositionId.Value;
//
//            var interval = Math.Max(1, flyNodeInterval);
//            var row = index / interval;
//            var col = index % interval;
//            return (row % 2 == 0) ? col : (interval - 1 - col);
//        }
        private static int GetEffectivePositionId(ImageInfo p)
            => p.PositionId ?? 0;

        // [Codex] [Change time: 260318] [Position rows no longer depend on manual fallback flags]
//        private static List<PositionRow> BuildPositionRows(ImageInfo[] pts, OrderOptions opt, bool shouldUseFlyNode)
//        {
//            var sorted = pts.OrderBy(p => p.ImageId).ToArray();
//            var flyNodeInterval = Math.Max(1, opt?.FlyNodeInterval ?? 11);
        private static List<PositionRow> BuildPositionRows(ImageInfo[] pts)
        {
            var sorted = pts.OrderBy(p => p.ImageId).ToArray();
            var rows = new List<PositionRow>();
            PositionRow current = null;
            ImageInfo prev = null;
            int? direction = null;

            for (int idx = 0; idx < sorted.Length; idx++)
            {
                var p = sorted[idx];
                if (current == null)
                {
                    current = new PositionRow { RowIndex = rows.Count };
                    rows.Add(current);
                    current.Items.Add(p);
                    prev = p;
                    continue;
                }

                var prevPos = GetEffectivePositionId(prev);
                var currPos = GetEffectivePositionId(p);
                var delta = currPos - prevPos;
                var step = Math.Sign(delta);
                var isStep = Math.Abs(delta) == 1;
                if (direction == null && isStep)
                    direction = step;

                var sameDirection = direction == null || (isStep && step == direction.Value);

                if (!isStep || !sameDirection)
                {
                    current.Direction = direction ?? 1;
                    current = new PositionRow { RowIndex = rows.Count };
                    rows.Add(current);
                    direction = null;
                }

                current.Items.Add(p);
                prev = p;
            }

            if (current != null && current.Direction == 0)
                current.Direction = direction ?? 1;

            return rows;
        }

        private static List<List<ImageInfo>> ClusterRowsBySecondaryAxis(ImageInfo[] pts, double rowTol, OrderOptions opt)
        {
            bool horizontalMove = (opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Right);
            bool startTop = (opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.TopRight);
            bool startLeft = (opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.BottomLeft);

            IEnumerable<ImageInfo> sorted;
            if (horizontalMove)
            {
                sorted = startTop
                    ? pts.OrderByDescending(p => p.YRobot).ThenBy(p => p.XRobot)
                    : pts.OrderBy(p => p.YRobot).ThenBy(p => p.XRobot);
            }
            else
            {
                sorted = startLeft
                    ? pts.OrderBy(p => p.XRobot).ThenByDescending(p => p.YRobot)
                    : pts.OrderByDescending(p => p.XRobot).ThenByDescending(p => p.YRobot);
            }

            var rows = new List<List<ImageInfo>>();
            foreach (var p in sorted)
            {
                if (rows.Count == 0)
                {
                    rows.Add(new List<ImageInfo> { p });
                    continue;
                }

                var last = rows[rows.Count - 1];
                var center = last.Average(x => GetSecondaryAxisValue(x, horizontalMove));
                if (Math.Abs(GetSecondaryAxisValue(p, horizontalMove) - center) <= rowTol)
                {
                    last.Add(p);
                }
                else
                {
                    rows.Add(new List<ImageInfo> { p });
                }
            }

            return rows;
        }

// [Codex] [Change time: 260319] [Rename helper to Zigzag terminology without changing traversal behavior]
//        private static OrderedRow[] ApplySnakeDirection(List<List<ImageInfo>> rows, OrderOptions opt)
        private static OrderedRow[] ApplyZigzagDirection(List<List<ImageInfo>> rows, OrderOptions opt)
        {
            bool forwardAscending = opt.RobotMovement == RobotMovement.Right || opt.RobotMovement == RobotMovement.Up;

            for (int r = 0; r < rows.Count; r++)
            {
                bool rowAscending = forwardAscending;
                if (r % 2 == 1) rowAscending = !rowAscending;

                if (!rowAscending)
                    rows[r].Reverse();
            }

            return rows.Select((r, idx) => new OrderedRow
            {
                RowIndex = idx,
                Sequence = r.ToArray()
            }).ToArray();
        }

        private static double GetPrimaryAxisValue(ImageInfo info, bool horizontalMove)
            => horizontalMove ? info.XRobot : info.YRobot;

        private static double GetSecondaryAxisValue(ImageInfo info, bool horizontalMove)
            => horizontalMove ? info.YRobot : info.XRobot;

        private static bool IsVerticalMovement(OrderOptions opt)
            => opt.RobotMovement == RobotMovement.Up || opt.RobotMovement == RobotMovement.Down;

        #region ImageGrid
        // [Codex] [Change time: 260320] [Add ImageGrid builders — not yet wired]
        /// <summary>
        /// Converts an OrderedRow[] produced by any row builder into an ImageGrid.
        /// Preserves ragged rows by padding missing physical cells with null.
        /// </summary>
        private static ImageGrid ToImageGrid(OrderedRow[] rows, OrderOptions opt)
        {
            if (rows == null || rows.Length == 0)
                return new ImageGrid(new ImageInfo[0, 0], Array.Empty<bool>(), 0);

            int rowCount = rows.Length;
            int colCount = rows.Max(r => r?.Sequence?.Length ?? 0);

            bool horizontalMove = opt.RobotMovement == RobotMovement.Left
                               || opt.RobotMovement == RobotMovement.Right;
            bool spineLeft = opt.StartCorner == StartCorner.TopLeft
                          || opt.StartCorner == StartCorner.BottomLeft;

            int spineCol = spineLeft ? 0 : Math.Max(0, colCount - 1);

            var grid = new ImageInfo[rowCount, colCount];
            var rowForward = new bool[rowCount];

            for (int r = 0; r < rowCount; r++)
            {
                var seq = rows[r]?.Sequence ?? Array.Empty<ImageInfo>();

                // [Codex] [Change time: 260320] [Fix NaN direction inference — NaN<=NaN is always false, breaking Branch spine for manual mode]
                //bool isForward = true;
                //if (seq.Length >= 2)
                //{
                //    double first = horizontalMove ? seq[0].XRobot : seq[0].YRobot;
                //    double last = horizontalMove ? seq[seq.Length - 1].XRobot : seq[seq.Length - 1].YRobot;
                //    isForward = first <= last;
                //}
                bool isForward = true;
                if (seq.Length >= 2)
                {
                    double first = horizontalMove ? seq[0].XRobot : seq[0].YRobot;
                    double last = horizontalMove ? seq[seq.Length - 1].XRobot : seq[seq.Length - 1].YRobot;
                    if (double.IsNaN(first) || double.IsNaN(last))
                        isForward = seq[0].ImageId <= seq[seq.Length - 1].ImageId;
                    else
                        isForward = first <= last;
                }

                rowForward[r] = isForward;

                for (int c = 0; c < seq.Length; c++)
                {
                    int physCol = isForward ? c : (seq.Length - 1 - c);
                    if (physCol >= 0 && physCol < colCount)
                        grid[r, physCol] = seq[c];
                }
            }

            return new ImageGrid(grid, rowForward, spineCol);
        }

        // [Codex] [Change time: 260320] [Build graph directly from physical ImageGrid after verification parity confirmation]
        private static OrderGraph BuildGraphFromGrid(ImageGrid ig, OrderOptions opt)
        {
            if (ig == null) throw new ArgumentNullException(nameof(ig));

            var links = new Dictionary<int, NodeLinks>();

            NodeLinks GetOrAdd(ImageInfo image)
            {
                if (image == null)
                    return null;

                if (!links.TryGetValue(image.ImageId, out var nodeLinks))
                {
                    nodeLinks = new NodeLinks { ImageId = image.ImageId };
                    links[image.ImageId] = nodeLinks;
                }

                return nodeLinks;
            }

            for (int r = 0; r < ig.Rows; r++)
            {
                var traversal = ig.GetRowSequence(r);
                for (int i = 0; i < traversal.Length; i++)
                    GetOrAdd(traversal[i]);
            }

            int horizontalRowLimit = opt.Mode == OrderMode.BranchDown ? Math.Min(1, ig.Rows) : ig.Rows;
            for (int r = 0; r < horizontalRowLimit; r++)
            {
                var traversal = ig.GetRowSequence(r).Where(x => x != null).ToArray();
                for (int i = 0; i < traversal.Length - 1; i++)
                    GetOrAdd(traversal[i]).HNext = traversal[i + 1].ImageId;
            }

            for (int r = 0; r < ig.Rows - 1; r++)
            {
                if (opt.Mode == OrderMode.Zigzag)
                {
                    var currentTraversal = ig.GetRowSequence(r);
                    var nextTraversal = ig.GetRowSequence(r + 1);
                    var tail = currentTraversal.LastOrDefault(x => x != null);
                    var head = nextTraversal.FirstOrDefault(x => x != null);
                    if (tail != null && head != null)
                        GetOrAdd(tail).VNext = head.ImageId;
                }
                else if (opt.Mode == OrderMode.Branch)
                {
                    var from = ig.At(r, ig.SpineCol);
                    var to = ig.At(r + 1, ig.SpineCol);
                    if (from != null && to != null)
                        GetOrAdd(from).VNext = to.ImageId;
                }
                else if (opt.Mode == OrderMode.BranchDown)
                {
                    for (int c = 0; c < ig.Cols; c++)
                    {
                        var from = ig.At(r, c);
                        var to = ig.At(r + 1, c);
                        if (from != null && to != null)
                            GetOrAdd(from).VNext = to.ImageId;
                    }
                }
            }

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
                Mode = opt.Mode,
                Rows = orderedRows,
                LinksById = links,
                RobotMovement = opt.RobotMovement
            };
        }

        // [Codex] [Change time: 260320] [Expand ImageGrid verification to cover manual and position illustration matrices across all corners/movements]
        internal static IReadOnlyList<string> VerifyImageGridGraphParity()
        {
            var failures = new List<string>();
            var verificationCases = new List<GraphVerificationCase>();

            foreach (var layoutCase in BuildIllustrationVerificationCases())
            {
                verificationCases.Add(layoutCase.Case);
                failures.AddRange(VerifyMetadataManualParity(
                    $"{layoutCase.Case.Name} metadata/manual parity",
                    layoutCase.MetadataImages,
                    layoutCase.MetadataOptions,
                    layoutCase.ManualImages,
                    layoutCase.ManualOptions));
            }

            foreach (var verificationCase in verificationCases)
                failures.AddRange(VerifyGraphCase(verificationCase));

            return failures;
        }

        // [Codex] [Change time: 260320] [Generate exhaustive illustration-derived verification cases for Manual and Position layouts]
        private static IEnumerable<IllustrationVerificationLayout> BuildIllustrationVerificationCases()
        {
            const int manualRows = 4;
            const int manualCols = 5;

            var manualCombos = new[]
            {
                new { Name = "Manual TopLeft Right", StartCorner = StartCorner.TopLeft, RobotMovement = RobotMovement.Right, ClusterOrder = ClusterOrderMode.ManualRow, NodeInterval = manualCols },
                new { Name = "Manual TopLeft Down", StartCorner = StartCorner.TopLeft, RobotMovement = RobotMovement.Down, ClusterOrder = ClusterOrderMode.ManualColumn, NodeInterval = manualRows },
                new { Name = "Manual TopRight Left", StartCorner = StartCorner.TopRight, RobotMovement = RobotMovement.Left, ClusterOrder = ClusterOrderMode.ManualRow, NodeInterval = manualCols },
                new { Name = "Manual TopRight Down", StartCorner = StartCorner.TopRight, RobotMovement = RobotMovement.Down, ClusterOrder = ClusterOrderMode.ManualColumn, NodeInterval = manualRows },
                new { Name = "Manual BottomLeft Right", StartCorner = StartCorner.BottomLeft, RobotMovement = RobotMovement.Right, ClusterOrder = ClusterOrderMode.ManualRow, NodeInterval = manualCols },
                new { Name = "Manual BottomLeft Up", StartCorner = StartCorner.BottomLeft, RobotMovement = RobotMovement.Up, ClusterOrder = ClusterOrderMode.ManualColumn, NodeInterval = manualRows },
                new { Name = "Manual BottomRight Left", StartCorner = StartCorner.BottomRight, RobotMovement = RobotMovement.Left, ClusterOrder = ClusterOrderMode.ManualRow, NodeInterval = manualCols },
                new { Name = "Manual BottomRight Up", StartCorner = StartCorner.BottomRight, RobotMovement = RobotMovement.Up, ClusterOrder = ClusterOrderMode.ManualColumn, NodeInterval = manualRows }
            };

            var positionCombos = new[]
            {
                new { Name = "Position TopLeft Right", StartCorner = StartCorner.TopLeft, RobotMovement = RobotMovement.Right },
                new { Name = "Position TopLeft Down", StartCorner = StartCorner.TopLeft, RobotMovement = RobotMovement.Down },
                new { Name = "Position TopRight Left", StartCorner = StartCorner.TopRight, RobotMovement = RobotMovement.Left },
                new { Name = "Position TopRight Down", StartCorner = StartCorner.TopRight, RobotMovement = RobotMovement.Down },
                new { Name = "Position BottomLeft Right", StartCorner = StartCorner.BottomLeft, RobotMovement = RobotMovement.Right },
                new { Name = "Position BottomLeft Up", StartCorner = StartCorner.BottomLeft, RobotMovement = RobotMovement.Up },
                new { Name = "Position BottomRight Left", StartCorner = StartCorner.BottomRight, RobotMovement = RobotMovement.Left },
                new { Name = "Position BottomRight Up", StartCorner = StartCorner.BottomRight, RobotMovement = RobotMovement.Up }
            };

            var modes = new[] { OrderMode.Zigzag, OrderMode.Branch, OrderMode.BranchDown };
            int groupId = 600;

            foreach (var combo in manualCombos)
            {
                foreach (var mode in modes)
                {
                    var metadataOptions = new OrderOptions
                    {
                        Mode = mode,
                        StartCorner = combo.StartCorner,
                        RobotMovement = combo.RobotMovement,
                        ClusterOrder = ClusterOrderMode.Coordinates,
                        NodeInterval = combo.NodeInterval
                    };
                    var metadataModel = CreateCoordinateVerificationModel(groupId++, manualRows, manualCols, metadataOptions);
                    var manualOptions = new OrderOptions
                    {
                        Mode = mode,
                        StartCorner = combo.StartCorner,
                        RobotMovement = combo.RobotMovement,
                        ClusterOrder = combo.ClusterOrder,
                        NodeInterval = combo.NodeInterval
                    };
                    var manualModel = CreateManualVerificationModel(groupId++, manualRows, manualCols, manualOptions);
                    yield return new IllustrationVerificationLayout(
                        CreateGraphVerificationCase($"{combo.Name} {mode}", metadataModel, metadataOptions),
                        metadataModel.Images,
                        metadataOptions,
                        manualModel.Images,
                        manualOptions);
                }
            }

            foreach (var combo in positionCombos)
            {
                foreach (var mode in modes)
                {
                    var metadataOptions = new OrderOptions
                    {
                        Mode = mode,
                        StartCorner = combo.StartCorner,
                        RobotMovement = combo.RobotMovement,
                        ClusterOrder = ClusterOrderMode.Position,
                        NodeInterval = manualCols
                    };
                    var metadataModel = CreatePositionVerificationModel(groupId++, manualRows, manualCols, metadataOptions);
                    var manualOptions = new OrderOptions
                    {
                        Mode = mode,
                        StartCorner = combo.StartCorner,
                        RobotMovement = combo.RobotMovement,
                        ClusterOrder = (combo.RobotMovement == RobotMovement.Left || combo.RobotMovement == RobotMovement.Right)
                            ? ClusterOrderMode.ManualRow
                            : ClusterOrderMode.ManualColumn,
                        NodeInterval = (combo.RobotMovement == RobotMovement.Left || combo.RobotMovement == RobotMovement.Right) ? manualCols : manualRows
                    };
                    var manualModel = CreateManualVerificationModel(groupId++, manualRows, manualCols, manualOptions);
                    yield return new IllustrationVerificationLayout(
                        CreateGraphVerificationCase($"{combo.Name} {mode}", metadataModel, metadataOptions),
                        metadataModel.Images,
                        metadataOptions,
                        manualModel.Images,
                        manualOptions);
                }
            }
        }

        private static GraphVerificationCase CreateGraphVerificationCase(string name, VerificationGraphModel model, OrderOptions options)
        {
            var expectedRows = model.TraversalRows.Select(row => row.ToArray()).ToArray();
            var expectedFlatten = BuildExpectedFlatten(model, options.Mode).ToArray();
            var expectedLinks = BuildExpectedLinks(model, options.Mode).ToArray();
            return new GraphVerificationCase(name, model.Images, options, expectedRows, expectedFlatten, expectedLinks);
        }

        private static VerificationGraphModel CreateCoordinateVerificationModel(int groupId, int rowCount, int colCount, OrderOptions opt)
        {
            var physicalCells = CreatePhysicalCellGrid(rowCount, colCount);
            var physicalGrid = CreatePhysicalLanes(physicalCells, opt);
            var images = new List<ImageInfo>();
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    int imageId = physicalCells[r][c];
                    images.Add(new ImageInfo(
                        $"coord-{groupId}-{imageId}.bmp",
                        groupId,
                        imageId,
                        null,
                        c * 10.0,
                        (rowCount - 1 - r) * 10.0));
                }
            }

            var rowForward = BuildRowForward(physicalGrid.Length, opt);
            var traversalRows = BuildTraversalRows(physicalGrid, rowForward);
            return new VerificationGraphModel(images.ToArray(), physicalGrid, rowForward, traversalRows)
            {
                Options = opt
            };
        }

        private static VerificationGraphModel CreateManualVerificationModel(int groupId, int rowCount, int colCount, OrderOptions opt)
        {
            var images = Enumerable.Range(1, rowCount * colCount)
                .Select(i => new ImageInfo($"manual-{groupId}-{i}.bmp", groupId, i, null, double.NaN, double.NaN))
                .ToArray();

            var physicalGrid = new int[rowCount][];
            var traversalRows = new List<int[]>();
            bool horizontalLayout = opt.ClusterOrder == ClusterOrderMode.ManualRow;
            bool horizontalMove = opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Right;
            bool startTop = opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.TopRight;
            bool startLeft = opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.BottomLeft;
            bool firstLaneForward = horizontalLayout
                ? (horizontalMove ? opt.RobotMovement == RobotMovement.Right : opt.RobotMovement == RobotMovement.Down)
                : (horizontalMove ? startTop : opt.RobotMovement == RobotMovement.Down);

            var baseLanes = new List<List<int>>();
            if (horizontalLayout)
            {
                for (int r = 0; r < rowCount; r++)
                    baseLanes.Add(Enumerable.Range((r * colCount) + 1, colCount).ToList());
                if (!startTop)
                    baseLanes.Reverse();
            }
            else
            {
                for (int c = 0; c < colCount; c++)
                    baseLanes.Add(Enumerable.Range(0, rowCount).Select(r => (r * colCount) + c + 1).ToList());
                if (!startLeft)
                    baseLanes.Reverse();
            }

            var rowForward = new bool[baseLanes.Count];
            for (int laneIndex = 0; laneIndex < baseLanes.Count -1; laneIndex++)
            {
                var traversal = baseLanes[laneIndex].ToList();
                bool laneForward = (laneIndex % 2 == 0) ? firstLaneForward : !firstLaneForward;
                if (!laneForward)
                    traversal.Reverse();
                rowForward[laneIndex] = laneForward;
                traversalRows.Add(traversal.ToArray());
                physicalGrid[laneIndex] = laneForward ? traversal.ToArray() : traversal.AsEnumerable().Reverse().ToArray();
            }

            return new VerificationGraphModel(images, physicalGrid, rowForward, traversalRows)
            {
                Options = opt
            };
        }

        private static VerificationGraphModel CreatePositionVerificationModel(int groupId, int rowCount, int colCount, OrderOptions opt)
        {
            var physicalCells = CreatePhysicalCellGrid(rowCount, colCount);
            var physicalGrid = CreatePhysicalLanes(physicalCells, opt);
            var rowForward = BuildRowForward(physicalGrid.Length, opt);
            var traversalRows = BuildTraversalRows(physicalGrid, rowForward);
            var coordinatesById = CreateCoordinatesById(physicalCells);
            var images = new List<ImageInfo>();
            int imageId = 1;

            for (int lane = 0; lane < physicalGrid.Length; lane++)
            {
                var physicalLane = physicalGrid[lane] ?? Array.Empty<int>();
                for (int i = 0; i < physicalLane.Length; i++)
                {
                    int physicalIndex = rowForward[lane] ? i : (physicalLane.Length - 1 - i);
                    int physicalId = physicalLane[physicalIndex];
                    var coord = coordinatesById[physicalId];
                    images.Add(new ImageInfo(
                        $"position-{groupId}-{imageId}.bmp",
                        groupId,
                        imageId,
                        physicalIndex,
                        coord.X,
                        coord.Y));
                    physicalLane[physicalIndex] = imageId;
                    imageId++;
                }
            }

            traversalRows = BuildTraversalRows(physicalGrid, rowForward);
            return new VerificationGraphModel(images.ToArray(), physicalGrid, rowForward, traversalRows)
            {
                Options = opt
            };
        }


        private static int[][] CreatePhysicalCellGrid(int rowCount, int colCount)
        {
            var cells = new int[rowCount][];
            int imageId = 1;
            for (int r = 0; r < rowCount; r++)
            {
                cells[r] = new int[colCount];
                for (int c = 0; c < colCount; c++)
                    cells[r][c] = imageId++;
            }
            return cells;
        }

        private static int[][] CreatePhysicalLanes(int[][] physicalCells, OrderOptions opt)
        {
            bool horizontalMove = opt.RobotMovement == RobotMovement.Left || opt.RobotMovement == RobotMovement.Right;
            bool startTop = opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.TopRight;
            bool startLeft = opt.StartCorner == StartCorner.TopLeft || opt.StartCorner == StartCorner.BottomLeft;

            if (horizontalMove)
            {
                var lanes = physicalCells.Select(row => row.ToArray()).ToList();
                if (!startTop)
                    lanes.Reverse();
                return lanes.ToArray();
            }

            int rowCount = physicalCells.Length;
            int colCount = rowCount == 0 ? 0 : physicalCells[0].Length;
            var columns = new List<int[]>();
            for (int c = 0; c < colCount; c++)
                columns.Add(Enumerable.Range(0, rowCount).Select(r => physicalCells[rowCount - 1 - r][c]).ToArray());
            if (!startLeft)
                columns.Reverse();
            return columns.ToArray();
        }

        private static Dictionary<int, (double X, double Y)> CreateCoordinatesById(int[][] physicalCells)
        {
            var result = new Dictionary<int, (double X, double Y)>();
            int rowCount = physicalCells.Length;
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < physicalCells[r].Length; c++)
                    result[physicalCells[r][c]] = (c * 10.0, (rowCount - 1 - r) * 10.0);
            }
            return result;
        }

        private static bool[] BuildRowForward(int rowCount, OrderOptions opt)
        {
            bool forwardAscending = opt.RobotMovement == RobotMovement.Right || opt.RobotMovement == RobotMovement.Up;
            var rowForward = new bool[rowCount];
            for (int r = 0; r < rowCount; r++)
                rowForward[r] = (r % 2 == 0) ? forwardAscending : !forwardAscending;
            return rowForward;
        }

        private static List<int[]> BuildTraversalRows(int[][] physicalGrid, bool[] rowForward)
        {
            var rows = new List<int[]>();
            for (int r = 0; r < physicalGrid.Length; r++)
            {
                var physicalRow = physicalGrid[r] ?? Array.Empty<int>();
                rows.Add(rowForward[r] ? physicalRow.ToArray() : physicalRow.Reverse().ToArray());
            }
            return rows;
        }

        private static IEnumerable<(int From, int To)> BuildExpectedFlatten(VerificationGraphModel model, OrderMode mode)
        {
            var rows = model.TraversalRows;
            for (int r = 0; r < rows.Count; r++)
            {
                var seq = rows[r] ?? Array.Empty<int>();
                if (mode != OrderMode.BranchDown || r == 0)
                {
                    for (int i = 0; i < seq.Length - 1; i++)
                        yield return (seq[i], seq[i + 1]);
                }

                if (r >= rows.Count - 1)
                    continue;

                var next = rows[r + 1] ?? Array.Empty<int>();
                if (seq.Length == 0 || next.Length == 0)
                    continue;

                if (mode == OrderMode.Zigzag)
                    yield return (seq[seq.Length - 1], next[0]);
                else if (mode == OrderMode.Branch)
                    yield return (seq[0], next[0]);
                else
                {
                    for (int c = 0; c < Math.Min(seq.Length, next.Length); c++)
                        yield return (seq[c], next[c]);
                }
            }
        }

        private static IEnumerable<(int Id, int? HNext, int? VNext)> BuildExpectedLinks(VerificationGraphModel model, OrderMode mode)
        {
            var rows = model.TraversalRows;
            var links = new Dictionary<int, NodeLinks>();

            NodeLinks GetOrAdd(int id)
            {
                if (!links.TryGetValue(id, out var node))
                {
                    node = new NodeLinks { ImageId = id };
                    links[id] = node;
                }
                return node;
            }

            foreach (var row in rows)
                foreach (var id in row ?? Array.Empty<int>())
                    GetOrAdd(id);

            int horizontalRowLimit = mode == OrderMode.BranchDown ? Math.Min(1, rows.Count) : rows.Count;
            for (int r = 0; r < horizontalRowLimit; r++)
            {
                var seq = rows[r] ?? Array.Empty<int>();
                for (int i = 0; i < seq.Length - 1; i++)
                    GetOrAdd(seq[i]).HNext = seq[i + 1];
            }

            int spineCol = model.PhysicalGrid.Length == 0 ? 0 : ((model.Options?.StartCorner == StartCorner.TopLeft || model.Options?.StartCorner == StartCorner.BottomLeft) ? 0 : model.PhysicalGrid[0].Length - 1);
            for (int r = 0; r < rows.Count - 1; r++)
            {
                if (mode == OrderMode.Zigzag)
                {
                    var seq = rows[r] ?? Array.Empty<int>();
                    var next = rows[r + 1] ?? Array.Empty<int>();
                    if (seq.Length > 0 && next.Length > 0)
                        GetOrAdd(seq[seq.Length - 1]).VNext = next[0];
                }
                else if (mode == OrderMode.Branch)
                {
                    var from = model.PhysicalGrid[r][spineCol];
                    var to = model.PhysicalGrid[r + 1][spineCol];
                    GetOrAdd(from).VNext = to;
                }
                else
                {
                    for (int c = 0; c < model.PhysicalGrid[r].Length; c++)
                        GetOrAdd(model.PhysicalGrid[r][c]).VNext = model.PhysicalGrid[r + 1][c];
                }
            }

            return links.OrderBy(kv => kv.Key).Select(kv => (kv.Key, kv.Value.HNext, kv.Value.VNext));
        }

        private sealed class VerificationGraphModel
        {
            public VerificationGraphModel(ImageInfo[] images, int[][] physicalGrid, bool[] rowForward, List<int[]> traversalRows)
            {
                Images = images;
                PhysicalGrid = physicalGrid;
                RowForward = rowForward;
                TraversalRows = traversalRows;
            }

            public ImageInfo[] Images { get; }
            public int[][] PhysicalGrid { get; }
            public bool[] RowForward { get; }
            public List<int[]> TraversalRows { get; }
            public OrderOptions Options { get; set; }
        }

        private sealed class IllustrationVerificationLayout
        {
            public IllustrationVerificationLayout(GraphVerificationCase graphCase, ImageInfo[] metadataImages, OrderOptions metadataOptions, ImageInfo[] manualImages, OrderOptions manualOptions)
            {
                Case = graphCase;
                MetadataImages = metadataImages;
                MetadataOptions = metadataOptions;
                ManualImages = manualImages;
                ManualOptions = manualOptions;
            }

            public GraphVerificationCase Case { get; }
            public ImageInfo[] MetadataImages { get; }
            public OrderOptions MetadataOptions { get; }
            public ImageInfo[] ManualImages { get; }
            public OrderOptions ManualOptions { get; }
        }
        // [Codex] [Change time: 260320] [Compare expected Flatten, Rows, and LinksById outputs for each verification case]
        private static IEnumerable<string> VerifyGraphCase(GraphVerificationCase verificationCase)
        {
            var result = BuildOrdersForGroup(verificationCase.Images[0].GroupId, verificationCase.Images, verificationCase.Options);
            var graph = result.Components.Single().Graph;
            var failures = new List<string>();

            if (!HaveEquivalentRows(graph.Rows, verificationCase.ExpectedRows))
                failures.Add($"{verificationCase.Name}: Rows mismatch. Actual={FormatRows(graph.Rows)} Expected={FormatRows(verificationCase.ExpectedRows)}");

            var flatten = OrderFlattener.Flatten(graph);
            if (!HaveEquivalentFlatten(flatten, verificationCase.ExpectedFlatten))
                failures.Add($"{verificationCase.Name}: Flatten mismatch. Actual={FormatFlatten(flatten)} Expected={FormatFlatten(verificationCase.ExpectedFlatten)}");

            if (!HaveEquivalentLinks(graph.LinksById, verificationCase.ExpectedLinks))
                failures.Add($"{verificationCase.Name}: Links mismatch. Actual={FormatLinks(graph.LinksById)} Expected={FormatLinks(verificationCase.ExpectedLinks)}");

            return failures;
        }

        // [Codex] [Change time: 260320] [Verify metadata-driven and manual fallback flows emit the same graph semantics for matched Bottom/... to Up cases]
        private static IEnumerable<string> VerifyMetadataManualParity(string caseName, ImageInfo[] metadataImages, OrderOptions metadataOptions, ImageInfo[] manualImages, OrderOptions manualOptions)
        {
            var metadataGraph = BuildOrdersForGroup(metadataImages[0].GroupId, metadataImages, metadataOptions).Components.Single().Graph;
            var manualGraph = BuildOrdersForGroup(manualImages[0].GroupId, manualImages, manualOptions).Components.Single().Graph;
            var failures = new List<string>();

            if (!HaveEquivalentRows(metadataGraph.Rows, manualGraph.Rows))
                failures.Add($"{caseName}: Rows mismatch. Metadata={FormatRows(metadataGraph.Rows)} Manual={FormatRows(manualGraph.Rows)}");

            var metadataFlatten = OrderFlattener.Flatten(metadataGraph);
            var manualFlatten = OrderFlattener.Flatten(manualGraph);
            if (!HaveEquivalentFlatten(metadataFlatten, manualFlatten))
                failures.Add($"{caseName}: Flatten mismatch. Metadata={FormatFlatten(metadataFlatten)} Manual={FormatFlatten(manualFlatten)}");

            if (!HaveEquivalentLinks(metadataGraph.LinksById, manualGraph.LinksById))
                failures.Add($"{caseName}: Links mismatch. Metadata={FormatLinks(metadataGraph.LinksById)} Manual={FormatLinks(manualGraph.LinksById)}");

            return failures;
        }

        // [Codex] [Change time: 260320] [Support ImageGrid verification pass with concise structural comparisons and formatting helpers]
        private static bool HaveEquivalentRows(OrderedRow[] rows, IReadOnlyList<int[]> expectedRows)
            => HaveEquivalentRows(rows, expectedRows.Select(r => r.ToArray()).ToArray());

        private static bool HaveEquivalentRows(OrderedRow[] leftRows, OrderedRow[] rightRows)
            => HaveEquivalentRows(leftRows, rightRows?.Select(r => (r?.Sequence ?? Array.Empty<ImageInfo>()).Select(i => i.ImageId).ToArray()).ToArray());

        private static bool HaveEquivalentRows(OrderedRow[] rows, int[][] expectedRows)
        {
            var actual = rows ?? Array.Empty<OrderedRow>();
            expectedRows = expectedRows ?? Array.Empty<int[]>();
            if (actual.Length != expectedRows.Length)
                return false;

            for (int i = 0; i < actual.Length; i++)
            {
                var actualIds = (actual[i]?.Sequence ?? Array.Empty<ImageInfo>()).Select(img => img.ImageId).ToArray();
                var expectedIds = expectedRows[i] ?? Array.Empty<int>();
                if (!actualIds.SequenceEqual(expectedIds))
                    return false;
            }

            return true;
        }

        private static bool HaveEquivalentFlatten((List<int> From, List<int> To) actual, IReadOnlyList<(int From, int To)> expected)
            => actual.From.SequenceEqual(expected.Select(e => e.From))
                && actual.To.SequenceEqual(expected.Select(e => e.To));

        private static bool HaveEquivalentFlatten((List<int> From, List<int> To) left, (List<int> From, List<int> To) right)
            => left.From.SequenceEqual(right.From) && left.To.SequenceEqual(right.To);

        private static bool HaveEquivalentLinks(Dictionary<int, NodeLinks> actual, IReadOnlyList<(int Id, int? HNext, int? VNext)> expected)
        {
            actual = actual ?? new Dictionary<int, NodeLinks>();
            expected = expected ?? Array.Empty<(int Id, int? HNext, int? VNext)>();
            if (actual.Count != expected.Count)
                return false;

            foreach (var entry in expected)
            {
                if (!actual.TryGetValue(entry.Id, out var nodeLinks))
                    return false;
                if (nodeLinks.HNext != entry.HNext || nodeLinks.VNext != entry.VNext)
                    return false;
            }

            return true;
        }

        private static bool HaveEquivalentLinks(Dictionary<int, NodeLinks> left, Dictionary<int, NodeLinks> right)
        {
            left = left ?? new Dictionary<int, NodeLinks>();
            right = right ?? new Dictionary<int, NodeLinks>();

            if (left.Count != right.Count)
                return false;

            foreach (var kv in left)
            {
                if (!right.TryGetValue(kv.Key, out var other))
                    return false;

                var current = kv.Value;
                if ((current?.HNext != other?.HNext) || (current?.VNext != other?.VNext))
                    return false;
            }

            return true;
        }

        private static string FormatRows(OrderedRow[] rows)
            => FormatRows((rows ?? Array.Empty<OrderedRow>()).Select(r => (r?.Sequence ?? Array.Empty<ImageInfo>()).Select(i => i.ImageId).ToArray()));

        private static string FormatRows(IEnumerable<int[]> rows)
            => string.Join(" | ", rows.Select(r => $"[{string.Join(",", r ?? Array.Empty<int>())}]"));

        private static string FormatFlatten((List<int> From, List<int> To) flatten)
            => string.Join(", ", flatten.From.Zip(flatten.To, (from, to) => $"{from}->{to}"));

        private static string FormatFlatten(IReadOnlyList<(int From, int To)> flatten)
            => string.Join(", ", flatten.Select(edge => $"{edge.From}->{edge.To}"));

        private static string FormatLinks(Dictionary<int, NodeLinks> links)
            => string.Join(", ", (links ?? new Dictionary<int, NodeLinks>()).OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}(H:{FormatNullable(kv.Value.HNext)},V:{FormatNullable(kv.Value.VNext)})"));

        private static string FormatLinks(IReadOnlyList<(int Id, int? HNext, int? VNext)> links)
            => string.Join(", ", (links ?? Array.Empty<(int Id, int? HNext, int? VNext)>()).OrderBy(link => link.Id).Select(link => $"{link.Id}(H:{FormatNullable(link.HNext)},V:{FormatNullable(link.VNext)})"));

        private static string FormatNullable(int? value) => value.HasValue ? value.Value.ToString() : "-";

        private static ImageInfo[] CreateManualImages(int groupId, int count)
            => Enumerable.Range(1, count)
                .Select(i => new ImageInfo($"manual-{groupId}-{i}.bmp", groupId, i, null, double.NaN, double.NaN))
                .ToArray();

        private static ImageInfo[] CreateGridImages(int groupId, int[,] imageIdsByColumnThenRow, double[,] coordinatesByImageId, bool withPositionIds)
        {
            var images = new List<ImageInfo>();
            int columns = imageIdsByColumnThenRow.GetLength(0);
            int rows = imageIdsByColumnThenRow.GetLength(1);

            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    int imageId = imageIdsByColumnThenRow[c, r];
                    int coordIndex = imageId - 1;
                    images.Add(new ImageInfo(
                        $"grid-{groupId}-{imageId}.bmp",
                        groupId,
                        imageId,
                        withPositionIds ? coordIndex : (int?)null,
                        coordinatesByImageId[coordIndex, 0],
                        coordinatesByImageId[coordIndex, 1]));
                }
            }

            return images.ToArray();
        }

        internal sealed class GraphVerificationCase
        {
            public GraphVerificationCase(string name, ImageInfo[] images, OrderOptions options, int[][] expectedRows, IReadOnlyList<(int From, int To)> expectedFlatten, IReadOnlyList<(int Id, int? HNext, int? VNext)> expectedLinks)
            {
                Name = name;
                Images = images;
                Options = options;
                ExpectedRows = expectedRows;
                ExpectedFlatten = expectedFlatten;
                ExpectedLinks = expectedLinks;
            }

            public string Name { get; }
            public ImageInfo[] Images { get; }
            public OrderOptions Options { get; }
            public int[][] ExpectedRows { get; }
            public IReadOnlyList<(int From, int To)> ExpectedFlatten { get; }
            public IReadOnlyList<(int Id, int? HNext, int? VNext)> ExpectedLinks { get; }
        }

// [Codex] [Change time: 260320] [Retire legacy graph builders after ImageGrid verification]
//        private static OrderGraph BuildGraphManual(OrderedRow[] rows, OrderOptions opt)
//        {
//        }
//
//        private static OrderGraph BuildGraph(OrderedRow[] rows, OrderOptions opt)
//        {
//        }
//
//        private static OrderedRow[] BuildStitchColumns(OrderedRow[] rows, OrderOptions opt)
//        {
//        }
//
//        private static OrderedRow[] BuildStitchRows(OrderedRow[] rows, OrderOptions opt)
//        {
//        }
        #endregion
        #endregion
    }

    #region class OrderFlattener
    public static class OrderFlattener
    {
        /// <summary>
        /// Flatten an OrderGraph into ordered edge lists (From, To).
// [Codex] [Change time: 260319] [Clarify Flatten docs with Zigzag business wording]
//        /// Snake:
//        ///   - horizontal edges per row
//        ///   - vertical edges Tail(row) -> Head(nextRow)
        /// Zigzag business mode (<see cref="OrderMode.Zigzag"/>):
        ///   - horizontal edges per row
        ///   - vertical edges Tail(row) -> Head(nextRow)
        /// Branch:
        ///   - horizontal edges per row
        ///   - vertical edges Head(row) -> Head(nextRow)
        /// BranchDown:
        ///   - horizontal edges per row
        ///   - vertical edges link each column node to the next row
        /// </summary>
        /// 
        public static (List<int> From, List<int> To) Flatten(OrderGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            var from = new List<int>(256);
            var to = new List<int>(256);

            var rows = graph.Rows ?? Array.Empty<OrderedRow>();
            for (int r = 0; r < rows.Length; r++)
            {
                var seq = rows[r].Sequence ?? Array.Empty<ImageInfo>();
                if (seq.Length == 0) continue;

                // Horizontal edges in-row
                if (graph.Mode != OrderMode.BranchDown || r == 0)
                {
                    for (int i = 0; i < seq.Length - 1; i++)
                    {
                        from.Add(seq[i].ImageId);
                        to.Add(seq[i + 1].ImageId);
                    }
                }

                // Vertical edge to next row (if any)
                if (r < rows.Length - 1)
                {
                    var nextSeq = rows[r + 1].Sequence ?? Array.Empty<ImageInfo>();
                    if (nextSeq.Length == 0) continue;

                    var headNext = rows[r + 1].Head;
                    if (headNext == null) continue;

                    if (graph.Mode == OrderMode.Branch)
                    {
                        var head = rows[r].Head;
                        if (head == null) continue;

                        from.Add(head.ImageId);
                        to.Add(headNext.ImageId);
                    }
                    else if (graph.Mode == OrderMode.BranchDown)
                    {
                        var count = Math.Min(seq.Length, nextSeq.Length);
                        for (int i = 0; i < count; i++)
                        {
                            from.Add(seq[i].ImageId);
                            to.Add(nextSeq[i].ImageId);
                        }
                    }
// [Codex] [Change time: 260319] [Clarify business wording in fallback branch]
//                    else // Snake
                    else // Zigzag business mode (OrderMode.Zigzag)
                    {
                        var tail = rows[r].Tail;
                        if (tail == null) continue;

                        from.Add(tail.ImageId);
                        to.Add(headNext.ImageId);
                    }
                }
            }

            return (from, to);
        }

        /// <summary>
        /// If you want explicit mode selection (ignoring graph.Mode).
        /// </summary>
        public static (List<int> From, List<int> To) Flatten(OrderGraph graph, OrderMode modeOverride)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var original = graph.Mode;
            graph.Mode = modeOverride;
            try { return Flatten(graph); }
            finally { graph.Mode = original; }
        }
    }
    #endregion
}
