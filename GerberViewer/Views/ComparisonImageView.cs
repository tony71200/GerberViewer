using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GerberViewer.Views
{
    public partial class ComparisonImageView : UserControl
    {
        private Bitmap _image;
        private float _zoom = 1f;
        private PointF _offset = new PointF(0, 0);
        private bool _panning;
        private Point _lastMouse;

        public ComparisonImageView()
        {
            InitializeComponent();
            DoubleBuffered = true;
            BackColor = Color.FromArgb(32, 32, 32);
            TabStop = true;
        }

        public event EventHandler ViewChanged;
        public event EventHandler<ComparisonImageMouseEventArgs> ImageMouseMove;

        public float Zoom { get { return _zoom; } }
        public PointF ViewOffset { get { return _offset; } }
        public Size ImageSize { get { return _image == null ? Size.Empty : _image.Size; } }

        public void SetImage(Bitmap image, bool cloneImage, bool preserveView)
        {
            DisposeImage();
            _image = image == null ? null : (cloneImage ? (Bitmap)image.Clone() : image);
            if (!preserveView) FitToWindow(false);
            else Invalidate();
        }

        public void ClearImage()
        {
            DisposeImage();
            _zoom = 1f;
            _offset = PointF.Empty;
            Invalidate();
        }

        public void FitToWindow() { FitToWindow(true); }
        public void ResetView() { FitToWindow(true); }

        public void SetView(float zoom, PointF offset, bool raiseEvent)
        {
            _zoom = Math.Max(0.02f, Math.Min(64f, zoom));
            _offset = offset;
            Invalidate();
            if (raiseEvent) OnViewChanged();
        }

        public PointF ScreenToImage(Point point)
        {
            return new PointF((point.X - _offset.X) / Math.Max(1e-6f, _zoom), (point.Y - _offset.Y) / Math.Max(1e-6f, _zoom));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) DisposeImage();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);
            if (_image == null)
            {
                TextRenderer.DrawText(e.Graphics, "No comparison image", Font, ClientRectangle, Color.Silver, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TranslateTransform(_offset.X, _offset.Y);
            e.Graphics.ScaleTransform(_zoom, _zoom);
            e.Graphics.DrawImage(_image, 0, 0, _image.Width, _image.Height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_image != null) FitToWindow(true);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_image == null) return;
            var oldZoom = _zoom;
            var factor = e.Delta > 0 ? 1.1f : 0.9f;
            _zoom = Math.Max(0.02f, Math.Min(64f, _zoom * factor));
            var dx = e.X - _offset.X;
            var dy = e.Y - _offset.Y;
            var ratio = _zoom / Math.Max(1e-6f, oldZoom);
            _offset = new PointF(e.X - dx * ratio, e.Y - dy * ratio);
            Invalidate();
            OnViewChanged();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _panning = true;
            _lastMouse = e.Location;
            Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var imagePoint = ScreenToImage(e.Location);
            ImageMouseMove?.Invoke(this, new ComparisonImageMouseEventArgs(imagePoint, e.Location));
            if (!_panning) return;
            _offset = new PointF(_offset.X + e.X - _lastMouse.X, _offset.Y + e.Y - _lastMouse.Y);
            _lastMouse = e.Location;
            Invalidate();
            OnViewChanged();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _panning = false;
        }

        private void FitToWindow(bool raiseEvent)
        {
            if (_image == null || Width <= 0 || Height <= 0) return;
            var zx = Width / (float)_image.Width;
            var zy = Height / (float)_image.Height;
            _zoom = Math.Max(0.02f, Math.Min(zx, zy));
            var drawW = _image.Width * _zoom;
            var drawH = _image.Height * _zoom;
            _offset = new PointF((Width - drawW) / 2f, (Height - drawH) / 2f);
            Invalidate();
            if (raiseEvent) OnViewChanged();
        }

        private void OnViewChanged() { ViewChanged?.Invoke(this, EventArgs.Empty); }
        private void DisposeImage() { if (_image != null) { _image.Dispose(); _image = null; } }
    }

    public sealed class ComparisonImageMouseEventArgs : EventArgs
    {
        public ComparisonImageMouseEventArgs(PointF imagePoint, Point screenPoint) { ImagePoint = imagePoint; ScreenPoint = screenPoint; }
        public PointF ImagePoint { get; private set; }
        public Point ScreenPoint { get; private set; }
    }
}
