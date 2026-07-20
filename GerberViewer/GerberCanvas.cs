// GerberViewer/GerberCanvas.cs
// Custom double-buffered canvas (Spec 5.1.3, 5.2.2, FR-016):
// - DrawImage bitmap cache only in OnPaint (does not re-render engine every time paint is used)

// - Zoom and pan with mouse cursor, pan with mouse drag, Fit-to-view
// Logic is here; .Designer.cs only declares the instance (does not drag-and-drop logic into Designer).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GerberViewer
{
    public sealed class GerberCanvas : Control
    {
        private Bitmap _image;
        private float _zoom = 1f;
        private PointF _offset = PointF.Empty;   // original image position in the control coordinates
        private Point _lastMouse;
        private bool _panning;

        private const float MinZoom = 0.02f, MaxZoom = 64f;
        private readonly Pen _majorGridPen = new Pen(Color.FromArgb(42, 48, 48));
        private readonly Pen _minorGridPen = new Pen(Color.FromArgb(25, 30, 30));
        /// <summary>
        /// The cursor is currently on which pixel of the image (null = outside the image). 
        /// Use for StatusStrip (FR-009).
        /// </summary>
        public event EventHandler<PointF?> ImageCursorMoved;

        public GerberCanvas()
        {
            // To disable flicker (Spec 5.1.3), press MouseWheel (5.1.6).
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.Selectable, true);
            BackColor = Color.FromArgb(12, 14, 14);
        }
        /// <summary>
        /// Live bitmap cache new. 
        /// Canvas bitmap and dispose of original (Spec 5.1.7).
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="fit"></param>
        public void SetImage(Bitmap bmp, bool fit)
        {
            Bitmap old = _image;
            _image = bmp;
            if (old != null && !ReferenceEquals(old, bmp)) old.Dispose();
            if (fit) FitToView(); else Invalidate();
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

        public bool HasImage { get { return _image != null; } }
        public float Zoom { get { return _zoom; } }

        public void FitToView()
        {
            if (_image == null || Width <= 0 || Height <= 0) { Invalidate(); return; }
            float zx = (float)Width / _image.Width;
            float zy = (float)Height / _image.Height;
            _zoom = Clamp(Math.Min(zx, zy) * 0.95f);
            _offset = new PointF((Width - _image.Width * _zoom) / 2,
                                 (Height - _image.Height * _zoom) / 2);
            Invalidate();
        }

        #region Drawing
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            DrawGrid(g);
            if (_image == null)
            {
                TextRenderer.DrawText(g,
                    "Drop Gerber files here or click Open",
                    Font, ClientRectangle, Color.FromArgb(130, 140, 140),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            // Keep preview smooth while zoom/pan transform is immediate; export DPI is independent.
            g.InterpolationMode = _zoom >= 2f ? InterpolationMode.HighQualityBicubic : InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(_image,
                new RectangleF(_offset.X, _offset.Y, _image.Width * _zoom, _image.Height * _zoom));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_image == null) return;
            float factor = e.Delta > 0 ? 1.25f : 1f / 1.25f;
            float newZoom = Clamp(_zoom * factor);
            if (Math.Abs(newZoom - _zoom) < 1e-6) return;

            // Neo diem anh duoi con tro: giu nguyen vi tri man hinh cua no
            float ix = (e.X - _offset.X) / _zoom;
            float iy = (e.Y - _offset.Y) / _zoom;
            _zoom = newZoom;
            _offset = new PointF(e.X - ix * _zoom, e.Y - iy * _zoom);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus(); // can focus de nhan wheel (Spec 5.1.6)
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
            {
                _panning = true;
                _lastMouse = e.Location;
                Cursor = Cursors.Hand;
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
            RaiseCursor(e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _panning = false;
            Cursor = Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            EventHandler<PointF?> h = ImageCursorMoved;
            if (h != null) h(this, null);
        }
        #endregion Drawing

        #region Private
        private void DrawGrid(Graphics g)
        {
            int minor = 16;
            int major = minor * 5;
            for (int x = 0; x < Width; x += minor) g.DrawLine((x % major) == 0 ? _majorGridPen : _minorGridPen, x, 0, x, Height);
            for (int y = 0; y < Height; y += minor) g.DrawLine((y % major) == 0 ? _majorGridPen : _minorGridPen, 0, y, Width, y);
        }

        private void RaiseCursor(Point mouse)
        {
            EventHandler<PointF?> h = ImageCursorMoved;
            if (h == null) return;
            if (_image == null) { h(this, null); return; }
            float ix = (mouse.X - _offset.X) / _zoom;
            float iy = (mouse.Y - _offset.Y) / _zoom;
            if (ix < 0 || iy < 0 || ix >= _image.Width || iy >= _image.Height) h(this, null);
            else h(this, new PointF(ix, iy));
        }

        private static float Clamp(float z)
        {
            return Math.Max(MinZoom, Math.Min(MaxZoom, z));
        }
        #endregion Private
    }
}
