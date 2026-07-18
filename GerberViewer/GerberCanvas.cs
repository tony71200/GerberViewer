// GerberViewer/GerberCanvas.cs
// Canvas double-buffered tuy bien (Spec 5.1.3, 5.2.2, FR-016):
// - CHI DrawImage bitmap cache trong OnPaint (khong re-render engine moi lan paint)
// - Zoom lan chuot neo tai con tro, pan bang keo chuot, Fit-to-view
// Logic nam o day; .Designer.cs chi khai bao instance (khong kéo-thả logic vao Designer).
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GerberViewer
{
    public sealed class GerberCanvas : Control
    {
        private Bitmap _image;
        private float _zoom = 1f;
        private PointF _offset = PointF.Empty;   // vi tri goc anh trong toa do control
        private Point _lastMouse;
        private bool _panning;

        private const float MinZoom = 0.02f, MaxZoom = 64f;

        /// <summary>Con tro dang o pixel nao cua ANH (null = ngoai anh). Dung cho StatusStrip (FR-009).</summary>
        public event EventHandler<PointF?> ImageCursorMoved;

        public GerberCanvas()
        {
            // Bat buoc de khong flicker (Spec 5.1.3) va nhan MouseWheel (5.1.6)
            SetStyle(ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.Selectable, true);
            BackColor = Color.FromArgb(30, 30, 30);
        }

        /// <summary>Gan bitmap cache moi. Canvas SO HUU bitmap va dispose ban cu (Spec 5.1.7).</summary>
        public void SetImage(Bitmap bmp, bool fit)
        {
            Bitmap old = _image;
            _image = bmp;
            if (old != null && !ReferenceEquals(old, bmp)) old.Dispose();
            if (fit) FitToView(); else Invalidate();
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_image == null)
            {
                TextRenderer.DrawText(e.Graphics,
                    "Keo-tha file Gerber vao day hoac dung nut Open",
                    Font, ClientRectangle, Color.Gray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            Graphics g = e.Graphics;
            // Phong to > 2x dung NearestNeighbor de soi net; thu nho dung Bilinear
            g.InterpolationMode = _zoom >= 2f ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.Half;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing && _image != null) { _image.Dispose(); _image = null; }
            base.Dispose(disposing);
        }
    }
}
