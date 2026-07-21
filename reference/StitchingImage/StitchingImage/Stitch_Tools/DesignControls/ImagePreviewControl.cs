using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StitchingImage.Stitch_Tools.DesignControls
{
    public class ImagePreviewControl : Control
    {
        private Bitmap _image;
        private float _scale = 1f;
        private PointF _offset = new PointF(0, 0);
        private bool _panning;
        private bool _measuring;
        private Point _lastMouse;
        private PointF? _measureStartImage;
        private PointF? _measureEndImage;

        public ImagePreviewControl()
        {
            DoubleBuffered = true;
            TabStop = true;
        }

        public bool ManualMode { get; set; }

        public double ManualScaleToFull {  get; set; } = 1.0f;

        public event EventHandler<ManualMeasureEventArgs> ManualMeasureChanged;

        public void SetImage(Bitmap image, bool preserveView = true)
        {
            var hadImage = _image != null;
            _image?.Dispose();
            _image = image;
            _measureStartImage = null;
            _measureEndImage = null;
            if (!preserveView || !hadImage)
                FitToWindow();
            Invalidate();
        }

        public void ResetView()
        {
            FitToWindow();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image?.Dispose();
                _image = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);
            if (_image == null)
                return;

            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.TranslateTransform(_offset.X, _offset.Y);
            e.Graphics.ScaleTransform(_scale, _scale);
            e.Graphics.DrawImage(_image, 0, 0, _image.Width, _image.Height);

            if (_measureStartImage.HasValue && _measureEndImage.HasValue)
            {
                var p1 = _measureStartImage.Value;
                var p2 = _measureEndImage.Value;
                using (var pen = new Pen(Color.LimeGreen, 2f / Math.Max(0.01f, _scale)))
                using (var brush = new SolidBrush(Color.OrangeRed))
                {
                    e.Graphics.DrawLine(pen, p1, p2);
                    var r = 4f / Math.Max(0.01f, _scale);
                    e.Graphics.FillEllipse(brush, p1.X - r, p1.Y - r, r * 2, r * 2);
                    e.Graphics.FillEllipse(brush, p2.X - r, p2.Y - r, r * 3, r * 3);
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_image == null)
                return;

            var oldScale = _scale;
            var delta = e.Delta > 0 ? 1.1f : 0.9f;
            _scale = Math.Max(0.1f, Math.Min(10f, _scale * delta));

            var mousePos = e.Location;
            var dx = mousePos.X - _offset.X;
            var dy = mousePos.Y - _offset.Y;
            var scaleRatio = _scale / oldScale;
            _offset = new PointF(mousePos.X - dx * scaleRatio, mousePos.Y - dy * scaleRatio);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (ManualMode && e.Button == MouseButtons.Right)
            {
                _measuring = true;
                _measureStartImage = ScreenToImage(e.Location);
                _measureEndImage = _measureStartImage;
                //RaiseManualMeasureChanged();
                Invalidate();
                Focus();
                return;
            }
            
            if (e.Button == MouseButtons.Left || (ManualMode && e.Button == MouseButtons.Left))
            {
                _panning = true;
                _lastMouse = e.Location;
                Focus();
            } 
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_measuring)
            {
                _measureEndImage = ScreenToImage(e.Location);
                //RaiseManualMeasureChanged();
                Invalidate();
                return;
            }

            if (_panning)
            {
                var dx = e.Location.X - _lastMouse.X;
                var dy = e.Location.Y - _lastMouse.Y;
                _offset = new PointF(_offset.X + dx, _offset.Y + dy);
                _lastMouse = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _panning = false;
            if (ManualMode && e.Button == MouseButtons.Right)
            {
                _measuring = false;
                RaiseManualMeasureChanged();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            FitToWindow();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.R)
            {
                ResetView();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.Q)
            {
                _measureStartImage = null;
                _measureEndImage = null;
                Invalidate();
            }
        }

        private void FitToWindow()
        {
            if (_image == null || Width <= 0 || Height <= 0)
                return;

            var scaleX = Width / (float)_image.Width;
            var scaleY = Height / (float)_image.Height;
            _scale = Math.Min(scaleX, scaleY);
            if (_scale <= 0)
                _scale = 1f;

            var drawW = _image.Width * _scale;
            var drawH = _image.Height * _scale;
            _offset = new PointF((Width - drawW) / 2f, (Height - drawH) / 2f);
        }

        private PointF ScreenToImage(Point point)
        {
            var x = (point.X - _offset.X) / Math.Max(1e-6f, _scale);
            var y = (point.Y - _offset.Y) / Math.Max(1e-6f, _scale);
            return new PointF(x, y);
        }

        private void RaiseManualMeasureChanged()
        {
            if (!_measureStartImage.HasValue || !_measureEndImage.HasValue)
                return;

            var start = _measureStartImage.Value;
            var end = _measureEndImage.Value;
            var dx = (end.X - start.X) * ManualScaleToFull;
            var dy = (end.Y - start.Y) * ManualScaleToFull;
            ManualMeasureChanged?.Invoke(this, new ManualMeasureEventArgs(start, end, dx, dy));
        }

        public sealed class ManualMeasureEventArgs : EventArgs
        {
            public PointF Start { get; }
            public PointF End { get; }
            public double Dx { get; }
            public double Dy { get; }
            public ManualMeasureEventArgs(PointF start,  PointF end, double dx, double dy)
            {
                Start = start;
                End = end;
                Dx = dx;
                Dy = dy;
            }
        }
    }
}
