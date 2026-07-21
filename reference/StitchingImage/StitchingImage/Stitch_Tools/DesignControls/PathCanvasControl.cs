using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using StitchingImage.Stitch_Tools.RobotManager;

namespace StitchingImage.Stitch_Tools.DesignControls
{
    public partial class PathCanvasControl : UserControl
    {
        // [Codex] [Change time: 260324] [Limit label rendering for large graphs to reduce paint latency]
        private const int LabelDisplayNodeThreshold = 1500;
        private TraversalBatchResult _traversalData;
        private ArrangeBatchResult _arrangeData;
        private readonly Font _labelFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        private readonly Font _axisFont = new Font("Segoe UI", 8f, FontStyle.Regular);
        private bool _hideAxes;
        private bool _showArrange = true;
        private bool _showTraversal = true;

        public PathCanvasControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint, true);

            // [Codex] [Change time: 260323] [Move drawing into the dedicated canvas panel and wire graph visibility toggles]
            panelCanvas.Paint += PanelCanvas_Paint;
            chkShowArrange.CheckedChanged += (s, e) =>
            {
                _showArrange = chkShowArrange.Checked;
                UpdateStatusText();
                InvalidateCanvas();
            };
            chkShowTraversal.CheckedChanged += (s, e) =>
            {
                _showTraversal = chkShowTraversal.Checked;
                UpdateStatusText();
                InvalidateCanvas();
            };

            UpdateLegendText();
            UpdateStatusText();
        }

        public void SetHideAxesWhenNoCoordinates(bool hideAxes)
        {
            _hideAxes = hideAxes;
            InvalidateCanvas();
        }

        public void SetData(TraversalBatchResult data)
        {
            // [Codex] [Change time: 260323] [Keep traversal-only overload for backward compatibility]
            _arrangeData = null;
            _traversalData = data;
            UpdateStatusText();
            InvalidateCanvas();
        }

        public void SetData(ArrangeBatchResult arrange, TraversalBatchResult traversal)
        {
            // [Codex] [Change time: 260323] [Accept both ArrangeGraph and TraversalGraph sources for the refactored canvas]
            _arrangeData = arrange;
            _traversalData = traversal;
            UpdateStatusText();
            InvalidateCanvas();
        }

        private void InvalidateCanvas()
        {
            panelCanvas?.Invalidate();
        }

        private void PanelCanvas_Paint(object sender, PaintEventArgs e)
        {
            DrawCanvas(e.Graphics, panelCanvas.ClientRectangle);
        }

        private void DrawCanvas(Graphics g, Rectangle canvasBounds)
        {
            // [Codex] [Change time: 260324] [Use adaptive smoothing mode to reduce rendering cost for large node sets]
            // g.SmoothingMode = SmoothingMode.AntiAlias;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.Clear(panelCanvas.BackColor);

            var traversalComponents = _traversalData?.Components ?? Array.Empty<TraversalComponent>();
            var arrangeComponents = _arrangeData?.Components ?? new List<ArrangeComponent>();
            var allPoints = traversalComponents.SelectMany(c => c?.Points ?? Array.Empty<ImageInfo>())
                .Concat(arrangeComponents.SelectMany(c => c?.Items ?? Array.Empty<ImageInfo>()))
                .Where(p => p != null)
                .GroupBy(p => p.ImageId)
                .Select(gp => gp.First())
                .ToArray();

            if (allPoints.Length == 0)
            {
                using (var br = new SolidBrush(Color.FromArgb(160, Color.Black)))
                    g.DrawString("No data", _labelFont, br, 10, 10);
                return;
            }

            var useVirtualLayout = allPoints.Any(p => double.IsNaN(p.XRobot) || double.IsNaN(p.YRobot));
            var virtualCoords = useVirtualLayout ? BuildVirtualCoordinates(arrangeComponents, traversalComponents) : new Dictionary<int, PointF>();
            var bounds = useVirtualLayout ? ComputeVirtualBounds(allPoints, virtualCoords) : Bounds2D.FromPoints(allPoints);

            float padLeft = 58f;
            float padTop = 16f;
            float padRight = 20f;
            float padBottom = 42f;
            float plotW = Math.Max(1f, canvasBounds.Width - padLeft - padRight);
            float plotH = Math.Max(1f, canvasBounds.Height - padTop - padBottom);
            double spanX = Math.Max(1e-6, bounds.MaxX - bounds.MinX);
            double spanY = Math.Max(1e-6, bounds.MaxY - bounds.MinY);
            float scale = (float)Math.Min(plotW / spanX, plotH / spanY);
            float scaleX = useVirtualLayout ? (float)(plotW / spanX) : scale;
            float scaleY = useVirtualLayout ? (float)(plotH / spanY) : scale;

            Func<ImageInfo, PointF> map = p =>
            {
                var logical = useVirtualLayout ? GetVirtualPoint(p, virtualCoords) : new PointF((float)p.XRobot, (float)p.YRobot);
                var sx = useVirtualLayout ? scaleX : scale;
                var sy = useVirtualLayout ? scaleY : scale;
                float x = (float)((logical.X - bounds.MinX) * sx + padLeft);
                float y = (float)((bounds.MaxY - logical.Y) * sy + padTop);
                return new PointF(x, y);
            };
            // [Codex] [Change time: 260324] [Cache mapped screen points once per paint to avoid repeated map() calls in edge loops]
            var mappedPoints = allPoints.ToDictionary(p => p.ImageId, map);
            var shouldDrawLabels = allPoints.Length <= LabelDisplayNodeThreshold;

            if (!_hideAxes && !useVirtualLayout)
                DrawAxes(g, bounds, padLeft, padTop, plotW, plotH);

            DrawComponentBoxes(g, traversalComponents, map, useVirtualLayout, virtualCoords);
            // [Codex] [Change time: 260324] [Pass cached points and adaptive label flag]
            // DrawNodes(g, allPoints, map, useVirtualLayout);
            DrawNodes(g, allPoints, mappedPoints, useVirtualLayout, shouldDrawLabels);

            if (_showArrange)
                // [Codex] [Change time: 260324] [Use O(1) lookup dictionaries during arrange segment rendering]
                DrawArrangeSegments(g, arrangeComponents, mappedPoints, 6f);
            if (_showTraversal)
                // [Codex] [Change time: 260324] [Use cached mapped points during traversal segment rendering]
                DrawTraversalSegments(g, traversalComponents, mappedPoints, -6f);
        }

        private void DrawComponentBoxes(Graphics g, IEnumerable<TraversalComponent> components, Func<ImageInfo, PointF> map, bool useVirtualLayout, Dictionary<int, PointF> virtualCoords)
        {
            using (var penBox = new Pen(Color.FromArgb(180, Color.Gray), 1.6f))
            using (var brLabel = new SolidBrush(Color.FromArgb(220, Color.DimGray)))
            {
                foreach (var c in components ?? Array.Empty<TraversalComponent>())
                {
                    var points = c?.Points ?? Array.Empty<ImageInfo>();
                    if (points.Length == 0)
                        continue;

                    var componentBounds = useVirtualLayout ? ComputeVirtualBounds(points, virtualCoords) : c.Bounds;
                    var tl = map(new ImageInfo("", 0, int.MinValue, null, componentBounds.MinX, componentBounds.MaxY));
                    var br = map(new ImageInfo("", 0, int.MinValue + 1, null, componentBounds.MaxX, componentBounds.MinY));
                    var rect = RectangleF.FromLTRB(Math.Min(tl.X, br.X), Math.Min(tl.Y, br.Y), Math.Max(tl.X, br.X), Math.Max(tl.Y, br.Y));
                    g.DrawRectangle(penBox, rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawString($"order {c.ComponentIndex}", _axisFont, brLabel, rect.X + 4, rect.Y - 18);
                }
            }
        }

        // [Codex] [Change time: 260324] [Render labels conditionally and reuse mapped points]
        // private void DrawNodes(Graphics g, IEnumerable<ImageInfo> points, Func<ImageInfo, PointF> map, bool useVirtualLayout)
        private void DrawNodes(Graphics g, IEnumerable<ImageInfo> points, IReadOnlyDictionary<int, PointF> mappedPoints, bool useVirtualLayout, bool drawLabels)
        {
            using (var brPoint = new SolidBrush(Color.Black))
            using (var brText = new SolidBrush(Color.FromArgb(220, Color.Black)))
            {
                foreach (var p in points ?? Array.Empty<ImageInfo>())
                {
                    if (p == null || !mappedPoints.TryGetValue(p.ImageId, out var pt))
                        continue;
                    const float r = 4.2f;
                    g.FillEllipse(brPoint, pt.X - r, pt.Y - r, 2 * r, 2 * r);
                    if (drawLabels)
                    {
                        var label = useVirtualLayout || !p.PositionId.HasValue ? p.ImageId.ToString() : $"{p.ImageId}-{p.PositionId.Value}";
                        g.DrawString(label, _labelFont, brText, pt.X + 4, pt.Y - 18);
                    }
                }
            }
        }

        // [Codex] [Change time: 260324] [Avoid repeated linear FirstOrDefault scans when drawing arrange edges]
        // private void DrawArrangeSegments(Graphics g, IEnumerable<ArrangeComponent> components, Func<ImageInfo, PointF> map, float perpendicularOffset)
        private void DrawArrangeSegments(Graphics g, IEnumerable<ArrangeComponent> components, IReadOnlyDictionary<int, PointF> mappedPoints, float perpendicularOffset)
        {
            using (var arrangePen = CreateArrowPen(Color.ForestGreen, 2.2f))
            {
                foreach (var comp in components ?? new List<ArrangeComponent>())
                {
                    var itemById = (comp?.Items ?? new List<ImageInfo>())
                        .Where(x => x != null)
                        .GroupBy(x => x.ImageId)
                        .ToDictionary(gp => gp.Key, gp => gp.First());
                    foreach (var seg in comp?.Path ?? new List<PathSegment>())
                    {
                        if (!itemById.TryGetValue(seg.FromId, out var from) || !itemById.TryGetValue(seg.ToId, out var to))
                            continue;
                        if (!mappedPoints.TryGetValue(from.ImageId, out var fromPoint) || !mappedPoints.TryGetValue(to.ImageId, out var toPoint))
                            continue;
                        DrawArrowSegment(g, arrangePen, fromPoint, toPoint, perpendicularOffset);
                    }
                }
            }
        }

        // [Codex] [Change time: 260324] [Use cached mapped coordinates for traversal edge drawing]
        // private void DrawTraversalSegments(Graphics g, IEnumerable<TraversalComponent> components, Func<ImageInfo, PointF> map, float perpendicularOffset)
        private void DrawTraversalSegments(Graphics g, IEnumerable<TraversalComponent> components, IReadOnlyDictionary<int, PointF> mappedPoints, float perpendicularOffset)
        {
            using (var traversalPen = CreateArrowPen(Color.Red, 2.2f))
            {
                foreach (var comp in components ?? Array.Empty<TraversalComponent>())
                {
                    var byId = (comp?.Points ?? Array.Empty<ImageInfo>()).ToDictionary(p => p.ImageId);
                    foreach (var kv in comp?.Graph?.LinksById ?? new Dictionary<int, Link>())
                    {
                        if (!byId.TryGetValue(kv.Key, out var from))
                            continue;

                        if (!mappedPoints.TryGetValue(from.ImageId, out var fromPoint))
                            continue;
                        if (kv.Value?.HNext.HasValue == true
                            && byId.TryGetValue(kv.Value.HNext.Value, out var hNext)
                            && mappedPoints.TryGetValue(hNext.ImageId, out var hNextPoint))
                            DrawArrowSegment(g, traversalPen, fromPoint, hNextPoint, perpendicularOffset);
                        if (kv.Value?.VNext.HasValue == true
                            && byId.TryGetValue(kv.Value.VNext.Value, out var vNext)
                            && mappedPoints.TryGetValue(vNext.ImageId, out var vNextPoint))
                            DrawArrowSegment(g, traversalPen, fromPoint, vNextPoint, perpendicularOffset);
                    }
                }
            }
        }

        private static Pen CreateArrowPen(Color color, float width)
        {
            var pen = new Pen(Color.FromArgb(220, color), width)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                CustomEndCap = new AdjustableArrowCap(4, 6, true)
            };
            return pen;
        }

        private static void DrawArrowSegment(Graphics g, Pen pen, PointF from, PointF to, float perpendicularOffset)
        {
            var offset = Math.Abs(perpendicularOffset) > 0.001f
                ? ComputePerpendicularOffset(from, to, perpendicularOffset)
                : PointF.Empty;
            g.DrawLine(pen, OffsetPoint(from, offset), OffsetPoint(to, offset));
        }

        private static PointF ComputePerpendicularOffset(PointF from, PointF to, float distance)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f)
                return PointF.Empty;
            return new PointF(-dy / len * distance, dx / len * distance);
        }

        private static PointF OffsetPoint(PointF point, PointF offset)
            => new PointF(point.X + offset.X, point.Y + offset.Y);

        private static Dictionary<int, PointF> BuildVirtualCoordinates(IEnumerable<ArrangeComponent> arrangeComponents, IEnumerable<TraversalComponent> traversalComponents)
        {
            var result = new Dictionary<int, PointF>();
            float componentOffset = 0f;

            foreach (var comp in arrangeComponents ?? new List<ArrangeComponent>())
            {
                var matrix = comp?.Matrix ?? new List<List<ImageInfo>>();
                var maxCols = Math.Max(1, matrix.Count == 0 ? 1 : matrix.Max(r => r?.Count ?? 0));
                for (int r = 0; r < matrix.Count; r++)
                {
                    var row = matrix[r] ?? new List<ImageInfo>();
                    for (int c = 0; c < row.Count; c++)
                    {
                        var node = row[c];
                        if (node != null)
                            result[node.ImageId] = new PointF(componentOffset + c, -r);
                    }
                }
                componentOffset += maxCols + 2f;
            }

            foreach (var comp in traversalComponents ?? Array.Empty<TraversalComponent>())
            {
                foreach (var row in comp?.Graph?.Matrix ?? Array.Empty<IReadOnlyList<ImageInfo>>())
                {
                    foreach (var node in row ?? Array.Empty<ImageInfo>())
                    {
                        if (node != null && !result.ContainsKey(node.ImageId))
                            result[node.ImageId] = new PointF(componentOffset++, 0);
                    }
                }
            }

            return result;
        }

        private static PointF GetVirtualPoint(ImageInfo info, Dictionary<int, PointF> coords)
        {
            if (info != null && coords != null && coords.TryGetValue(info.ImageId, out var p))
                return p;
            return new PointF(0, 0);
        }

        private static Bounds2D ComputeVirtualBounds(IEnumerable<ImageInfo> points, Dictionary<int, PointF> coords)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            int used = 0;
            foreach (var p in points ?? Array.Empty<ImageInfo>())
            {
                var vp = GetVirtualPoint(p, coords);
                minX = Math.Min(minX, vp.X);
                minY = Math.Min(minY, vp.Y);
                maxX = Math.Max(maxX, vp.X);
                maxY = Math.Max(maxY, vp.Y);
                used++;
            }
            if (used == 0)
                return new Bounds2D { MinX = 0, MinY = 0, MaxX = 1, MaxY = 1 };
            if (Math.Abs(maxX - minX) < 1e-9) maxX = minX + 1;
            if (Math.Abs(maxY - minY) < 1e-9) maxY = minY + 1;
            return new Bounds2D { MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY };
        }

        private void DrawAxes(Graphics g, Bounds2D b, float padLeft, float padTop, float plotW, float plotH)
        {
            using (var axisPen = new Pen(Color.FromArgb(160, Color.Gray), 1f))
            using (var gridPen = new Pen(Color.FromArgb(45, Color.Gray), 1f))
            using (var br = new SolidBrush(Color.FromArgb(200, Color.Black)))
            {
                float x0 = padLeft;
                float y0 = padTop + plotH;
                g.DrawLine(axisPen, x0, padTop, x0, y0);
                g.DrawLine(axisPen, x0, y0, x0 + plotW, y0);

                int ticks = 6;
                for (int i = 0; i <= ticks; i++)
                {
                    float tx = x0 + (plotW * i / ticks);
                    float ty = padTop + (plotH * i / ticks);
                    g.DrawLine(axisPen, tx, y0, tx, y0 + 4);
                    g.DrawLine(axisPen, x0 - 4, ty, x0, ty);
                    if (i > 0 && i < ticks)
                    {
                        g.DrawLine(gridPen, tx, padTop, tx, y0);
                        g.DrawLine(gridPen, x0, ty, x0 + plotW, ty);
                    }

                    var xv = (b.MinX + (b.MaxX - b.MinX) * i / ticks).ToString("0.##");
                    var yv = (b.MaxY - (b.MaxY - b.MinY) * i / ticks).ToString("0.##");
                    var xsz = g.MeasureString(xv, _axisFont);
                    var ysz = g.MeasureString(yv, _axisFont);
                    g.DrawString(xv, _axisFont, br, tx - xsz.Width / 2f, y0 + 6);
                    g.DrawString(yv, _axisFont, br, x0 - 6 - ysz.Width, ty - ysz.Height / 2f);
                }

                g.DrawString("XRobot", _axisFont, br, x0 + plotW - 44, y0 + 24);
                g.DrawString("YRobot", _axisFont, br, 6, padTop);
            }
        }

        private void UpdateLegendText()
        {
            if (statusLabelLegend != null)
                statusLabelLegend.ToolTipText = "Legend: Green=ArrangeGraph, Red=TraversalGraph, Black=Node";
        }

        private void UpdateStatusText()
        {
            UpdateLegendText();
            if (statusLabelTraversal == null)
                return;

            var graph = _traversalData?.Components?.FirstOrDefault()?.Graph;
            var modeLabel = graph == null ? "-" : graph.Mode.ToString();
            statusLabelTraversal.Text = $"Gray=layout/ticks, Green=ArrangeGraph [{(_showArrange ? "ON" : "OFF")}], Red=TraversalGraph [{(_showTraversal ? "ON" : "OFF")}] (Mode={modeLabel})";
        }
    }
}
