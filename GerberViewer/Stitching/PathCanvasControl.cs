using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GerberViewer.Stitching.Models;
using GerberViewer.Stitching.RobotManager;

namespace GerberViewer.Stitching.DesignControls
{
    public sealed class PathCanvasNode { public int NodeId { get; set; } public int OrderIndex { get; set; } public int Row { get; set; } public int Column { get; set; } public double X { get; set; } public double Y { get; set; } public OrderNodeState State { get; set; } }
    public sealed class PathCanvasEdge { public int FromNodeId { get; set; } public int ToNodeId { get; set; } public string Layer { get; set; } }
    public sealed class PathCanvasSnapshot { public IList<PathCanvasNode> Nodes { get; private set; } public IList<PathCanvasEdge> Edges { get; private set; } public PathCanvasSnapshot(IEnumerable<PathCanvasNode> nodes, IEnumerable<PathCanvasEdge> edges) { Nodes = (nodes ?? Enumerable.Empty<PathCanvasNode>()).OrderBy(n => n.OrderIndex).ToList(); Edges = (edges ?? Enumerable.Empty<PathCanvasEdge>()).ToList(); } }

    public sealed class PathCanvasControl : UserControl
    {
        private readonly BufferedPanel _canvas = new BufferedPanel();
        private readonly CheckBox _chkExpected = new CheckBox();
        private readonly Label _legend = new Label();
        private PathCanvasSnapshot _snapshot = new PathCanvasSnapshot(null, null);
        private readonly Dictionary<OrderNodeState, Color> _stateColors = new Dictionary<OrderNodeState, Color>
        {
            { OrderNodeState.Pending, Color.LightGray }, { OrderNodeState.Processing, Color.DodgerBlue },
            { OrderNodeState.SampleAlignOk, Color.ForestGreen }, { OrderNodeState.NeighborAlignOk, Color.SeaGreen },
            { OrderNodeState.AnchorAdjusted, Color.DarkCyan }, { OrderNodeState.Interpolated, Color.MediumPurple },
            { OrderNodeState.ExpectedGridOffset, Color.Goldenrod }, { OrderNodeState.ExpectedOffset, Color.Goldenrod },
            { OrderNodeState.Manual, Color.Orange }, { OrderNodeState.Failed, Color.Firebrick }, { OrderNodeState.Excluded, Color.DimGray }
        };

        public PathCanvasControl()
        {
            _legend.Dock = DockStyle.Top; _legend.Height = 24; _legend.Text = "Expected Sample Coordinates | Final Global Coordinates | Virtual Row/Column";
            _chkExpected.Dock = DockStyle.Top; _chkExpected.Height = 24; _chkExpected.Text = "Expected order arrows"; _chkExpected.Checked = true; _chkExpected.CheckedChanged += delegate { _canvas.Invalidate(); };
            _canvas.Dock = DockStyle.Fill; _canvas.BackColor = Color.White; _canvas.Paint += Canvas_Paint;
            Controls.Add(_canvas); Controls.Add(_chkExpected); Controls.Add(_legend);
        }

        public void SetCapturedImages(IEnumerable<CapturedImageInfo> images)
        {
            var nodes = (images ?? Enumerable.Empty<CapturedImageInfo>()).Select(x => new PathCanvasNode { NodeId = x.OrderIndex, OrderIndex = x.OrderIndex, Row = x.Row, Column = x.Column, X = x.RobotX, Y = x.RobotY, State = x.State }).ToList();
            var edges = nodes.OrderBy(n => n.OrderIndex).Zip(nodes.OrderBy(n => n.OrderIndex).Skip(1), (a, b) => new PathCanvasEdge { FromNodeId = a.NodeId, ToNodeId = b.NodeId, Layer = "Expected order" }).ToList();
            SetSnapshot(new PathCanvasSnapshot(nodes, edges));
        }

        public void SetSnapshot(PathCanvasSnapshot snapshot) { _snapshot = snapshot ?? new PathCanvasSnapshot(null, null); _canvas.Invalidate(); }

        public void SetData(ArrangeBatchResult arrange, TraversalGraph traversal)
        {
            var items = arrange != null ? arrange.Components.SelectMany(c => c.Items ?? new ImageInfo[0]) : Enumerable.Empty<ImageInfo>();
            var nodes = items.Select(x => new PathCanvasNode { NodeId = x.PositionId.HasValue ? x.PositionId.Value : x.ImageId, OrderIndex = x.PositionId.HasValue ? x.PositionId.Value : x.ImageId, Row = x.Row, Column = x.Column, X = x.XRobot, Y = x.YRobot, State = OrderNodeState.Pending }).ToList();
            SetSnapshot(new PathCanvasSnapshot(nodes, nodes.OrderBy(n => n.OrderIndex).Zip(nodes.OrderBy(n => n.OrderIndex).Skip(1), (a, b) => new PathCanvasEdge { FromNodeId = a.NodeId, ToNodeId = b.NodeId, Layer = "Traversal graph" })));
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(_canvas.BackColor);
            if (_snapshot.Nodes.Count == 0) { e.Graphics.DrawString("No order data", Font, Brushes.Gray, 12, 12); return; }
            var points = BuildScreenPoints();
            if (_chkExpected.Checked) DrawEdges(e.Graphics, points);
            foreach (var node in _snapshot.Nodes) DrawNode(e.Graphics, node, points[node.NodeId]);
        }

        private Dictionary<int, PointF> BuildScreenPoints()
        {
            var maxRow = Math.Max(0, _snapshot.Nodes.Max(x => x.Row)); var maxCol = Math.Max(0, _snapshot.Nodes.Max(x => x.Column));
            var cellW = Math.Max(42f, (_canvas.Width - 40f) / (maxCol + 1)); var cellH = Math.Max(42f, (_canvas.Height - 40f) / (maxRow + 1));
            return _snapshot.Nodes.ToDictionary(n => n.NodeId, n => new PointF(20f + n.Column * cellW + cellW / 2f, 20f + n.Row * cellH + cellH / 2f));
        }
        private void DrawEdges(Graphics g, Dictionary<int, PointF> points) { using (var pen = new Pen(Color.Gray, 1.5f)) foreach (var edge in _snapshot.Edges) if (points.ContainsKey(edge.FromNodeId) && points.ContainsKey(edge.ToNodeId)) g.DrawLine(pen, points[edge.FromNodeId], points[edge.ToNodeId]); }
        private void DrawNode(Graphics g, PathCanvasNode node, PointF p) { var color = _stateColors.ContainsKey(node.State) ? _stateColors[node.State] : Color.LightGray; using (var b = new SolidBrush(color)) g.FillEllipse(b, p.X - 16, p.Y - 16, 32, 32); g.DrawEllipse(Pens.Black, p.X - 16, p.Y - 16, 32, 32); g.DrawString(node.OrderIndex.ToString(), Font, Brushes.Black, p.X - 8, p.Y - 7); }
        internal sealed class BufferedPanel : Panel { public BufferedPanel() { DoubleBuffered = true; ResizeRedraw = true; } }
    }
}
