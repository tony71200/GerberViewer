using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GerberViewer.Stitching.Models;
using GerberViewer.Stitching.RobotManager;

namespace GerberViewer.Stitching.DesignControls
{
    public sealed class PathCanvasNode
    {
        public PathCanvasNode(int orderIndex, int row, int column, double x, double y, OrderNodeState state)
        {
            NodeId = orderIndex;
            OrderIndex = orderIndex;
            Row = row;
            Column = column;
            X = x;
            Y = y;
            State = state;
        }
        public int NodeId { get; private set; }
        public int OrderIndex { get; private set; }
        public int Row { get; private set; }
        public int Column { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public OrderNodeState State { get; private set; }
    }

    public sealed class PathCanvasEdge
    {
        public PathCanvasEdge(int fromNodeId, int toNodeId, string layer, string matcher, string reason)
        {
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            Layer = layer ?? string.Empty;
            Matcher = matcher;
            Reason = reason;
        }
        public int FromNodeId { get; private set; }
        public int ToNodeId { get; private set; }
        public string Layer { get; private set; }
        public string Reason { get; private set; }
        public string Matcher { get; private set; }
    }

    public sealed class PathCanvasSnapshot
    {
        public IList<PathCanvasNode> Nodes { get; private set; }
        public IList<PathCanvasEdge> Edges { get; private set; }
        public PathCanvasSnapshot(IEnumerable<PathCanvasNode> nodes, IEnumerable<PathCanvasEdge> edges)
        {
            Nodes = (nodes ?? Enumerable.Empty<PathCanvasNode>()).OrderBy(n => n.OrderIndex).ToList().AsReadOnly();
            Edges = (edges ?? Enumerable.Empty<PathCanvasEdge>()).ToList().AsReadOnly();
        }
        public static PathCanvasSnapshot Empty { get { return new PathCanvasSnapshot(null, null); } }
    }

    public sealed partial class PathCanvasControl : UserControl
    {
        private PathCanvasSnapshot _snapshot = PathCanvasSnapshot.Empty;
        private readonly Dictionary<OrderNodeState, Color> _stateColors = new Dictionary<OrderNodeState, Color>
        {
            { OrderNodeState.Pending, Color.LightGray }, { OrderNodeState.Processing, Color.DodgerBlue },
            { OrderNodeState.SampleAlignOk, Color.ForestGreen }, { OrderNodeState.NeighborAlignOk, Color.SeaGreen },
            { OrderNodeState.AnchorAdjusted, Color.DarkCyan }, { OrderNodeState.Interpolated, Color.MediumPurple },
            { OrderNodeState.ExpectedGridOffset, Color.Goldenrod }, { OrderNodeState.ExpectedOffset, Color.Goldenrod },
            { OrderNodeState.Manual, Color.Orange }, { OrderNodeState.Failed, Color.Firebrick }, { OrderNodeState.Excluded, Color.DimGray }
        };
        private int? _selectedOrderIndex;

        public event EventHandler<PathCanvasNodeSelectedEventArgs> NodeSelected;

        public PathCanvasControl()
        {
            InitializeComponent();
        }

        public PathCanvasSnapshot Snapshot { get { return _snapshot; } }
        public int? SelectedOrderIndex { get { return _selectedOrderIndex; } }

        public void SetCapturedImages(IEnumerable<CapturedImageInfo> images)
        {
            var nodes = (images ?? Enumerable.Empty<CapturedImageInfo>()).Select(x => new PathCanvasNode(x.OrderIndex, x.Row, x.Column, x.RobotX, x.RobotY, x.State)).ToList();
            var edges = nodes.OrderBy(n => n.OrderIndex).Zip(nodes.OrderBy(n => n.OrderIndex).Skip(1), (a, b) => new PathCanvasEdge(a.NodeId, b.NodeId, "Expected order", null, null)).ToList();
            SetSnapshot(new PathCanvasSnapshot(nodes, edges));
        }

        public void SetSnapshot(PathCanvasSnapshot snapshot)
        {
            _snapshot = snapshot ?? PathCanvasSnapshot.Empty;
            if (_selectedOrderIndex.HasValue && !_snapshot.Nodes.Any(n => n.OrderIndex == _selectedOrderIndex.Value)) _selectedOrderIndex = null;
            canvasPanel.Invalidate();
        }

        public void SetRecoveryEdges(IEnumerable<RecoveryEdgeReport> recoveryEdges)
        {
            var edges = (recoveryEdges ?? Enumerable.Empty<RecoveryEdgeReport>()).Select(x => new PathCanvasEdge(x.AnchorOrderIndex, x.TargetOrderIndex, "Neighbor recovery", x.Matcher, x.Reason)).ToList();
            SetSnapshot(new PathCanvasSnapshot(_snapshot.Nodes, _snapshot.Edges.Where(e => e.Layer != "Neighbor recovery").Concat(edges)));
        }

        public void SetFinalStates(IEnumerable<TileWorkflowState> states, IEnumerable<RecoveryEdgeReport> recoveryEdges)
        {
            var stateList = (states ?? Enumerable.Empty<TileWorkflowState>()).ToList();
            var nodes = stateList.Select(x => new PathCanvasNode(x.OrderIndex, x.Row, x.Column, x.GlobalPose == null ? 0 : x.GlobalPose[0, 2], x.GlobalPose == null ? 0 : x.GlobalPose[1, 2], ToNodeState(x.Source))).ToList();
            var expectedEdges = nodes.OrderBy(n => n.OrderIndex).Zip(nodes.OrderBy(n => n.OrderIndex).Skip(1), (a, b) => new PathCanvasEdge(a.NodeId, b.NodeId, "Expected order", null, null));
            var recovery = (recoveryEdges ?? Enumerable.Empty<RecoveryEdgeReport>()).Select(x => new PathCanvasEdge(x.AnchorOrderIndex, x.TargetOrderIndex, "Neighbor recovery", x.Matcher, x.Reason));
            SetSnapshot(new PathCanvasSnapshot(nodes, expectedEdges.Concat(recovery)));
        }

        public void SetData(ArrangeBatchResult arrange, TraversalGraph traversal)
        {
            var items = arrange != null ? arrange.Components.SelectMany(c => c.Items ?? new ImageInfo[0]) : Enumerable.Empty<ImageInfo>();
            var nodes = items.Select(x => new PathCanvasNode(x.PositionId.HasValue ? x.PositionId.Value : x.ImageId, x.Row, x.Column, x.XRobot, x.YRobot, OrderNodeState.Pending)).ToList();
            SetSnapshot(new PathCanvasSnapshot(nodes, nodes.OrderBy(n => n.OrderIndex).Zip(nodes.OrderBy(n => n.OrderIndex).Skip(1), (a, b) => new PathCanvasEdge(a.NodeId, b.NodeId, "Traversal graph", null, null))));
        }

        public void SetSelectedOrderIndex(int? orderIndex)
        {
            _selectedOrderIndex = orderIndex;
            canvasPanel.Invalidate();
        }

        private void chkExpectedOrder_CheckedChanged(object sender, EventArgs e)
        {
            canvasPanel.Invalidate();
        }

        private void canvasPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(canvasPanel.BackColor);
            if (_snapshot.Nodes.Count == 0) { e.Graphics.DrawString("No order data", Font, Brushes.Gray, 12, 12); return; }
            var points = BuildScreenPoints();
            DrawEdges(e.Graphics, points);
            foreach (var node in _snapshot.Nodes) DrawNode(e.Graphics, node, points[node.NodeId]);
        }

        private void canvasPanel_MouseClick(object sender, MouseEventArgs e)
        {
            var hit = HitTest(e.Location);
            if (hit == null) return;
            _selectedOrderIndex = hit.OrderIndex;
            canvasPanel.Invalidate();
            var handler = NodeSelected;
            if (handler != null) handler(this, new PathCanvasNodeSelectedEventArgs(hit));
        }

        private PathCanvasNode HitTest(Point point)
        {
            if (_snapshot.Nodes.Count == 0) return null;
            var points = BuildScreenPoints();
            foreach (var node in _snapshot.Nodes)
            {
                var p = points[node.NodeId];
                var dx = point.X - p.X;
                var dy = point.Y - p.Y;
                if (dx * dx + dy * dy <= 20 * 20) return node;
            }
            return null;
        }

        private Dictionary<int, PointF> BuildScreenPoints()
        {
            var maxRow = Math.Max(0, _snapshot.Nodes.Max(x => x.Row)); var maxCol = Math.Max(0, _snapshot.Nodes.Max(x => x.Column));
            var cellW = Math.Max(42f, (canvasPanel.Width - 40f) / (maxCol + 1)); var cellH = Math.Max(42f, (canvasPanel.Height - 40f) / (maxRow + 1));
            return _snapshot.Nodes.ToDictionary(n => n.NodeId, n => new PointF(20f + n.Column * cellW + cellW / 2f, 20f + n.Row * cellH + cellH / 2f));
        }

        private void DrawEdges(Graphics g, Dictionary<int, PointF> points)
        {
            foreach (var edge in _snapshot.Edges)
            {
                if (!points.ContainsKey(edge.FromNodeId) || !points.ContainsKey(edge.ToNodeId)) continue;
                if (edge.Layer == "Expected order" && !chkExpectedOrder.Checked) continue;
                var color = edge.Layer == "Neighbor recovery" ? Color.SeaGreen : (edge.Layer == "Interpolation anchors" ? Color.MediumPurple : Color.Gray);
                var width = edge.Layer == "Neighbor recovery" ? 2.5f : 1.5f;
                using (var pen = new Pen(color, width)) g.DrawLine(pen, points[edge.FromNodeId], points[edge.ToNodeId]);
            }
        }

        private void DrawNode(Graphics g, PathCanvasNode node, PointF p)
        {
            var color = _stateColors.ContainsKey(node.State) ? _stateColors[node.State] : Color.LightGray;
            using (var b = new SolidBrush(color)) g.FillEllipse(b, p.X - 16, p.Y - 16, 32, 32);
            g.DrawEllipse(_selectedOrderIndex.HasValue && _selectedOrderIndex.Value == node.OrderIndex ? Pens.Red : Pens.Black, p.X - 16, p.Y - 16, 32, 32);
            g.DrawString(node.OrderIndex.ToString(), Font, Brushes.Black, p.X - 8, p.Y - 7);
        }

        private static OrderNodeState ToNodeState(PoseSource source)
        {
            if (source == PoseSource.SampleAlignment) return OrderNodeState.SampleAlignOk;
            if (source == PoseSource.NeighborAlignment) return OrderNodeState.NeighborAlignOk;
            if (source == PoseSource.AnchorAdjusted) return OrderNodeState.AnchorAdjusted;
            if (source == PoseSource.Interpolated) return OrderNodeState.Interpolated;
            if (source == PoseSource.Manual) return OrderNodeState.Manual;
            if (source == PoseSource.Excluded) return OrderNodeState.Excluded;
            if (source == PoseSource.ExpectedGridOffset) return OrderNodeState.ExpectedGridOffset;
            return OrderNodeState.Failed;
        }
    }

    internal sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    public sealed class PathCanvasNodeSelectedEventArgs : EventArgs
    {
        public PathCanvasNodeSelectedEventArgs(PathCanvasNode node) { Node = node; }
        public PathCanvasNode Node { get; private set; }
    }
}
