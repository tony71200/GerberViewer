using System;
using System.Collections.Generic;
using System.Drawing;
using GerberViewer.Stitching.DesignControls;
using GerberViewer.Stitching.Models;

namespace GerberStitching.Tests.UI
{
    public static class PathCanvasControlTests
    {
        public static void RunAll()
        {
            SmokeGrid(1, 1, "1x1");
            SmokeGrid(2, 2, "2x2");
            SmokeGrid(4, 4, "4x4");
            SmokeGrid(8, 8, "8x8");
            SmokeZigzag();
            SmokeBranch();
            SmokeBranchDown();
            SmokeResize();
            SmokeRecoveryEdges();
            SmokeMixedStates();
        }

        private static void SmokeGrid(int rows, int cols, string name)
        {
            using (var control = new PathCanvasControl())
            {
                control.SetSnapshot(BuildSnapshot(rows, cols, false, false, false));
                AssertEqual(rows * cols, control.Snapshot.Nodes.Count, name + " node count.");
                AssertTrue(AllNodeIdsAreOrderIndex(control.Snapshot), name + " must keep NodeId = OrderIndex.");
                Render(control, 320, 240);
            }
        }

        private static void SmokeZigzag()
        {
            using (var control = new PathCanvasControl())
            {
                control.SetSnapshot(BuildSnapshot(3, 4, true, false, false));
                Render(control, 360, 240);
            }
        }

        private static void SmokeBranch()
        {
            using (var control = new PathCanvasControl())
            {
                control.SetSnapshot(BuildSnapshot(3, 3, false, true, false));
                AssertTrue(control.Snapshot.Edges.Count > 0, "Branch snapshot must have edges.");
                Render(control, 360, 240);
            }
        }

        private static void SmokeBranchDown()
        {
            using (var control = new PathCanvasControl())
            {
                control.SetSnapshot(BuildSnapshot(3, 3, false, false, true));
                Render(control, 360, 240);
            }
        }

        private static void SmokeResize()
        {
            using (var control = new PathCanvasControl())
            {
                control.SetSnapshot(BuildSnapshot(4, 4, false, false, false));
                Render(control, 180, 120);
                Render(control, 640, 480);
            }
        }

        private static void SmokeRecoveryEdges()
        {
            using (var control = new PathCanvasControl())
            {
                control.SetSnapshot(BuildSnapshot(2, 2, false, false, false));
                control.SetRecoveryEdges(new[] { new RecoveryEdgeReport { AnchorOrderIndex = 0, TargetOrderIndex = 1, Direction = "right", Matcher = "PharseCorrMatcher+EccMatcher", Reason = "accepted" } });
                AssertTrue(ContainsLayer(control.Snapshot, "Neighbor recovery"), "Recovery edge layer must be present.");
                Render(control, 320, 240);
            }
        }

        private static void SmokeMixedStates()
        {
            using (var control = new PathCanvasControl())
            {
                var states = new[]
                {
                    State(0, 0, 0, PoseSource.SampleAlignment),
                    State(1, 0, 1, PoseSource.NeighborAlignment),
                    State(2, 1, 0, PoseSource.Manual),
                    State(3, 1, 1, PoseSource.Failed)
                };
                control.SetFinalStates(states, new[] { new RecoveryEdgeReport { AnchorOrderIndex = 0, TargetOrderIndex = 1, Direction = "right", Matcher = "PharseCorrMatcher", Reason = "accepted" } });
                control.SetSelectedOrderIndex(1);
                AssertEqual(1, control.SelectedOrderIndex.Value, "Selected OrderIndex must synchronize into canvas.");
                Render(control, 320, 240);
            }
        }

        private static PathCanvasSnapshot BuildSnapshot(int rows, int cols, bool zigzag, bool branch, bool branchDown)
        {
            var nodes = new List<PathCanvasNode>();
            var edges = new List<PathCanvasEdge>();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var order = zigzag && r % 2 == 1 ? r * cols + (cols - 1 - c) : r * cols + c;
                    nodes.Add(new PathCanvasNode(order, r, c, c * 100, r * 100, OrderNodeState.Pending));
                }
            }
            nodes.Sort((a, b) => a.OrderIndex.CompareTo(b.OrderIndex));
            for (int i = 0; i < nodes.Count - 1; i++) edges.Add(new PathCanvasEdge(nodes[i].NodeId, nodes[i + 1].NodeId, "Expected order", null, null));
            if (branch && nodes.Count > cols) edges.Add(new PathCanvasEdge(nodes[0].NodeId, nodes[cols].NodeId, "Traversal graph", null, "branch"));
            if (branchDown && nodes.Count > cols + 1) edges.Add(new PathCanvasEdge(nodes[1].NodeId, nodes[cols + 1].NodeId, "Interpolation anchors", null, "branch down"));
            return new PathCanvasSnapshot(nodes, edges);
        }

        private static TileWorkflowState State(int order, int row, int col, PoseSource source)
        {
            return TileWorkflowState.From(new CapturedImageInfo { OrderIndex = order, Row = row, Column = col }, new[,] { { 1d, 0d, col * 100d }, { 0d, 1d, row * 100d }, { 0d, 0d, 1d } }, source, null, null);
        }

        private static void Render(PathCanvasControl control, int width, int height)
        {
            control.Size = new Size(width, height);
            using (var bitmap = new Bitmap(width, height)) control.DrawToBitmap(bitmap, new Rectangle(0, 0, width, height));
        }

        private static bool AllNodeIdsAreOrderIndex(PathCanvasSnapshot snapshot)
        {
            foreach (var node in snapshot.Nodes) if (node.NodeId != node.OrderIndex) return false;
            return true;
        }

        private static bool ContainsLayer(PathCanvasSnapshot snapshot, string layer)
        {
            foreach (var edge in snapshot.Edges) if (string.Equals(edge.Layer, layer, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void AssertTrue(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static void AssertEqual(int expected, int actual, string message) { if (expected != actual) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual); }
    }
}
