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
            ImagePointMoved += GerberCanvas_ImagePointMoved;
            MouseLeave += GerberCanvas_MouseLeave;
        }

        /// <summary>
        /// Live bitmap cache new.
        /// Canvas bitmap and dispose of original (Spec 5.1.7).
        /// </summary>
        public void SetImage(Bitmap bmp, bool fit)
        {
            _image = bmp;
            SetSourceBitmap(bmp, fit);

            EnableMouseWheelZoom = bmp != null;
            WinOperate = bmp != null ? 1 : 0;

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ImagePointMoved -= GerberCanvas_ImagePointMoved;
                MouseLeave -= GerberCanvas_MouseLeave;
                if (_image != null) { _image.Dispose(); _image = null; }
            }
            base.Dispose(disposing);
        }

        public bool HasImage { get { return HasSourceImage; } }

        public float Zoom
        {
            get
            {
                return (float)CurrentZoom;
            }
        }

        public void FitToView()
        {
            if (!HasSourceImage) return;
            FitImage();
        }

        private void GerberCanvas_ImagePointMoved(object sender, PointF? imagePoint)
        {
            EventHandler<PointF?> h = ImageCursorMoved;
            if (h != null) h(this, imagePoint);
        }

        private void GerberCanvas_MouseLeave(object sender, EventArgs e)
        {
            EventHandler<PointF?> h = ImageCursorMoved;
            if (h != null) h(this, null);
        }
    }
}
