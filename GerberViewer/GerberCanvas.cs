// Gerber canvas adapter around EWindowControl.
// Keeps the public API used by MainForm while delegating image display,
// zoom, and pan behavior to EWindowControl instead of custom GDI+ painting.
using System;
using System.Drawing;
using System.Windows.Forms;
using EWindowControl;

namespace GerberViewer
{
    public sealed class GerberCanvas : EWindowControl.EWindowControl
    {
        private Bitmap _image;

        /// <summary>
        /// The cursor is currently on which pixel of the image (null = outside the image).
        /// Use for StatusStrip (FR-009).
        /// </summary>
        public event EventHandler<PointF?> ImageCursorMoved;

        public GerberCanvas()
        {
            BackColor = Color.FromArgb(12, 14, 14);
            EnableInfo = false;
            EnableInfoFromUser = true;
            EnableDoubleClickZoom = true;
            EnableMouseWheelZoom = true;
            WinOperate = 1;
            EMouseMoveInfo += GerberCanvas_EMouseMoveInfo;
            MouseLeave += GerberCanvas_MouseLeave;
        }

        /// <summary>
        /// Live bitmap cache new.
        /// Canvas bitmap and dispose of original (Spec 5.1.7).
        /// </summary>
        public void SetImage(Bitmap bmp, bool fit)
        {
            Bitmap old = _image;
            _image = bmp;
            SourceBitmap = bmp;

            if (old != null && !ReferenceEquals(old, bmp)) old.Dispose();

            EnableMouseWheelZoom = bmp != null;
            WinOperate = bmp != null ? 1 : 0;

            if (fit && bmp != null) FitImage();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                EMouseMoveInfo -= GerberCanvas_EMouseMoveInfo;
                MouseLeave -= GerberCanvas_MouseLeave;
                if (_image != null) { _image.Dispose(); _image = null; }
            }
            base.Dispose(disposing);
        }

        public bool HasImage { get { return _image != null; } }

        public float Zoom
        {
            get
            {
                if (_image == null) return 1f;

                double y1, x1, y2, x2;
                GetWinShowSize(out y1, out x1, out y2, out x2);
                double visibleWidth = x2 - x1 + 1;
                if (visibleWidth <= 0) return 1f;

                return (float)(Width / visibleWidth);
            }
        }

        public void FitToView()
        {
            if (_image == null) return;
            FitImage();
        }

        private void GerberCanvas_EMouseMoveInfo(EMouseEventArgs mousePoint, ref string userInfo)
        {
            EventHandler<PointF?> h = ImageCursorMoved;
            if (h == null) return;

            if (_image == null)
            {
                h(this, null);
                return;
            }

            Point p = mousePoint.Coordinate_Image;
            if (p.X < 0 || p.Y < 0 || p.X >= _image.Width || p.Y >= _image.Height) h(this, null);
            else h(this, new PointF(p.X, p.Y));
        }

        private void GerberCanvas_MouseLeave(object sender, EventArgs e)
        {
            EventHandler<PointF?> h = ImageCursorMoved;
            if (h != null) h(this, null);
        }
    }
}
