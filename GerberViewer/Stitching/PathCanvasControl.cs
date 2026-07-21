using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GerberViewer.Stitching.Models;
using GerberViewer.Stitching.RobotManager;

namespace GerberViewer.Stitching.DesignControls
{
    public sealed class PathCanvasControl : UserControl
    {
        private readonly Panel _canvas = new Panel();
        private readonly Label _legend = new Label();
        private IList<CapturedImageInfo> _capturedImages = new List<CapturedImageInfo>();
        private readonly Dictionary<OrderNodeState, Color> _stateColors = new Dictionary<OrderNodeState, Color>
        {
            { OrderNodeState.Pending, Color.LightGray }, { OrderNodeState.Processing, Color.DodgerBlue },
            { OrderNodeState.SampleAlignOk, Color.ForestGreen }, { OrderNodeState.NeighborAlignOk, Color.SeaGreen },
            { OrderNodeState.Interpolated, Color.MediumPurple }, { OrderNodeState.ExpectedOffset, Color.Goldenrod },
            { OrderNodeState.Failed, Color.Firebrick }, { OrderNodeState.Excluded, Color.DimGray }
        };

        public PathCanvasControl()
        {
            _legend.Dock = DockStyle.Top;
            _legend.Height = 36;
            _legend.Text = "Pending | Processing | Sample Align OK | Neighbor Align OK | Interpolated | Expected Offset | Failed | Excluded";
            _canvas.Dock = DockStyle.Fill;
            _canvas.BackColor = Color.White;
            _canvas.Paint += Canvas_Paint;
            Controls.Add(_canvas);
            Controls.Add(_legend);
        }

        public void SetCapturedImages(IEnumerable<CapturedImageInfo> images)
        {
            _capturedImages = (images ?? Enumerable.Empty<CapturedImageInfo>()).OrderBy(x => x.OrderIndex).ToList();
            _canvas.Invalidate();
        }

        public void SetData(ArrangeBatchResult arrange, TraversalGraph traversal)
        {
            var items = arrange != null ? arrange.Components.SelectMany(c => c.Items ?? new ImageInfo[0]) : Enumerable.Empty<ImageInfo>();
            _capturedImages = items.Select((x, i) => new CapturedImageInfo { FilePath = x.FilePath, Row = x.Row, Column = x.Column, OrderIndex = i, RobotX = x.XRobot, RobotY = x.YRobot, State = OrderNodeState.Pending }).ToList();
            _canvas.Invalidate();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(_canvas.BackColor);
            if (_capturedImages.Count == 0) { e.Graphics.DrawString("No order data", Font, Brushes.Gray, 12, 12); return; }
            var maxRow = Math.Max(0, _capturedImages.Max(x => x.Row));
            var maxCol = Math.Max(0, _capturedImages.Max(x => x.Column));
            var cellW = Math.Max(48f, (_canvas.Width - 40f) / (maxCol + 1));
            var cellH = Math.Max(48f, (_canvas.Height - 40f) / (maxRow + 1));
            foreach (var img in _capturedImages.OrderBy(x => x.OrderIndex))
            {
                var x = 20f + img.Column * cellW + cellW / 2f;
                var y = 20f + img.Row * cellH + cellH / 2f;
                var color = _stateColors.ContainsKey(img.State) ? _stateColors[img.State] : Color.LightGray;
                using (var b = new SolidBrush(color)) e.Graphics.FillEllipse(b, x - 16, y - 16, 32, 32);
                e.Graphics.DrawEllipse(Pens.Black, x - 16, y - 16, 32, 32);
                e.Graphics.DrawString((img.OrderIndex + 1).ToString(), Font, Brushes.Black, x - 8, y - 7);
            }
        }
    }
}
