// GerberViewer/GerberCanvas.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GerberViewer
{
    public enum CanvasInteractionMode { PanInspect, MeasureDistance, MeasureAngle }

    public sealed class CanvasMeasurementOverlay
    {
        public readonly List<PointF> ImagePoints = new List<PointF>();
        public string Label = "";
        public bool IsAngle;
    }

    public sealed class GerberCanvas : Control
    {
        private Bitmap _image;
        private float _zoom = 1f;
        private PointF _offset = PointF.Empty;
        private Point _lastMouse;
        private bool _panning;
        private readonly List<CanvasMeasurementOverlay> _measurements = new List<CanvasMeasurementOverlay>();
        private readonly List<PointF> _pendingMeasurement = new List<PointF>();
        private PointF? _liveImagePoint;

        private const float MinZoom = 0.02f, MaxZoom = 64f;
        private readonly Pen _majorGridPen = new Pen(Color.FromArgb(42, 48, 48));
        private readonly Pen _minorGridPen = new Pen(Color.FromArgb(25, 30, 30));

        public event EventHandler<PointF?> ImageCursorMoved;
        public event EventHandler<PointF> ImageClicked;

        public CanvasInteractionMode InteractionMode { get; set; }
        public bool HasImage { get { return _image != null; } }
        public float Zoom { get { return _zoom; } }

        public GerberCanvas()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            BackColor = Color.FromArgb(12, 14, 14);
        }

        public void SetImage(Bitmap bmp, bool fit)
        {
            Bitmap old = _image;
            _image = bmp;
            if (old != null && !ReferenceEquals(old, bmp)) old.Dispose();
            if (fit) FitToView(); else Invalidate();
        }

        public void SetMeasurements(IEnumerable<CanvasMeasurementOverlay> overlays)
        {
            _measurements.Clear();
            if (overlays != null) _measurements.AddRange(overlays);
            Invalidate();
        }

        public void SetPendingMeasurement(IEnumerable<PointF> points, PointF? livePoint)
        {
            _pendingMeasurement.Clear();
            if (points != null) _pendingMeasurement.AddRange(points);
            _liveImagePoint = livePoint;
            Invalidate();
        }

        public void ClearMeasurementOverlay()
        {
            _measurements.Clear();
            _pendingMeasurement.Clear();
            _liveImagePoint = null;
            Invalidate();
        }

        public void FitToView()
        {
            if (_image == null || Width <= 0 || Height <= 0) { Invalidate(); return; }
            float zx = (float)Width / _image.Width;
            float zy = (float)Height / _image.Height;
            _zoom = Clamp(Math.Min(zx, zy) * 0.95f);
            _offset = new PointF((Width - _image.Width * _zoom) / 2, (Height - _image.Height * _zoom) / 2);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_image != null) { _image.Dispose(); _image = null; }
                _majorGridPen.Dispose();
                _minorGridPen.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            DrawGrid(g);
            if (_image == null)
            {
                TextRenderer.DrawText(g, "Drop Gerber files here or click Open", Font, ClientRectangle,
                    Color.FromArgb(130, 140, 140), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            g.InterpolationMode = _zoom >= 2f ? InterpolationMode.HighQualityBicubic : InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(_image, new RectangleF(_offset.X, _offset.Y, _image.Width * _zoom, _image.Height * _zoom));
            DrawMeasurements(g);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_image == null) return;
            float factor = e.Delta > 0 ? 1.25f : 1f / 1.25f;
            float newZoom = Clamp(_zoom * factor);
            if (Math.Abs(newZoom - _zoom) < 1e-6) return;
            float ix = (e.X - _offset.X) / _zoom;
            float iy = (e.Y - _offset.Y) / _zoom;
            _zoom = newZoom;
            _offset = new PointF(e.X - ix * _zoom, e.Y - iy * _zoom);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right ||
                (e.Button == MouseButtons.Left && InteractionMode == CanvasInteractionMode.PanInspect))
            {
                _panning = true; _lastMouse = e.Location; Cursor = Cursors.Hand;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_panning)
            {
                _offset = new PointF(_offset.X + e.X - _lastMouse.X, _offset.Y + e.Y - _lastMouse.Y);
                _lastMouse = e.Location;
                Invalidate();
            }
            PointF? imagePoint = ClientToImage(e.Location);
            RaiseCursor(imagePoint);
            if (InteractionMode != CanvasInteractionMode.PanInspect) SetPendingMeasurement(_pendingMeasurement, imagePoint);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _panning = false; Cursor = Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || InteractionMode == CanvasInteractionMode.PanInspect) return;
            PointF? imagePoint = ClientToImage(e.Location);
            if (imagePoint.HasValue && ImageClicked != null) ImageClicked(this, imagePoint.Value);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            RaiseCursor(null);
            _liveImagePoint = null;
            Invalidate();
        }

        private PointF? ClientToImage(Point mouse)
        {
            if (_image == null) return null;
            float ix = (mouse.X - _offset.X) / _zoom;
            float iy = (mouse.Y - _offset.Y) / _zoom;
            if (ix < 0 || iy < 0 || ix >= _image.Width || iy >= _image.Height) return null;
            return new PointF(ix, iy);
        }

        private PointF ImageToClient(PointF imagePoint)
        {
            return new PointF(_offset.X + imagePoint.X * _zoom, _offset.Y + imagePoint.Y * _zoom);
        }

        private void DrawMeasurements(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen donePen = new Pen(Color.FromArgb(255, 255, 209, 102), 2f))
            using (Pen livePen = new Pen(Color.FromArgb(220, 86, 180, 255), 1.5f) { DashStyle = DashStyle.Dash })
            using (SolidBrush markerBrush = new SolidBrush(Color.FromArgb(255, 255, 209, 102)))
            using (SolidBrush labelBrush = new SolidBrush(Color.FromArgb(240, 245, 245, 245)))
            using (SolidBrush labelBack = new SolidBrush(Color.FromArgb(180, 20, 25, 25)))
            {
                foreach (CanvasMeasurementOverlay m in _measurements) DrawMeasurement(g, m.ImagePoints, m.Label, donePen, markerBrush, labelBrush, labelBack);
                if (_pendingMeasurement.Count > 0)
                {
                    List<PointF> live = new List<PointF>(_pendingMeasurement);
                    if (_liveImagePoint.HasValue) live.Add(_liveImagePoint.Value);
                    DrawMeasurement(g, live, "", livePen, markerBrush, labelBrush, labelBack);
                }
            }
        }

        private void DrawMeasurement(Graphics g, IList<PointF> pts, string label, Pen pen, Brush marker, Brush text, Brush back)
        {
            if (pts.Count < 1) return;
            PointF[] c = new PointF[pts.Count];
            for (int i = 0; i < pts.Count; i++) c[i] = ImageToClient(pts[i]);
            if (c.Length >= 2) g.DrawLines(pen, c);
            foreach (PointF p in c) g.FillEllipse(marker, p.X - 4, p.Y - 4, 8, 8);
            if (!string.IsNullOrEmpty(label))
            {
                PointF anchor = c[c.Length - 1];
                Size sz = TextRenderer.MeasureText(label, Font);
                RectangleF r = new RectangleF(anchor.X + 8, anchor.Y + 8, sz.Width + 8, sz.Height + 4);
                g.FillRectangle(back, r);
                g.DrawString(label, Font, text, r.X + 4, r.Y + 2);
            }
        }

        private void DrawGrid(Graphics g)
        {
            int minor = 16, major = minor * 5;
            for (int x = 0; x < Width; x += minor) g.DrawLine((x % major) == 0 ? _majorGridPen : _minorGridPen, x, 0, x, Height);
            for (int y = 0; y < Height; y += minor) g.DrawLine((y % major) == 0 ? _majorGridPen : _minorGridPen, 0, y, Width, y);
        }

        private void RaiseCursor(PointF? imagePoint)
        {
            EventHandler<PointF?> h = ImageCursorMoved;
            if (h != null) h(this, imagePoint);
        }

        private static float Clamp(float z) { return Math.Max(MinZoom, Math.Min(MaxZoom, z)); }
    }
}
