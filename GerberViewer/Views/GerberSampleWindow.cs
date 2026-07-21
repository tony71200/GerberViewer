using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using HalconDotNet;

namespace GerberViewer.Views
{
    public sealed class GerberSampleWindow : EWindowControl.EWindowControl
    {
        private HWindow _halconWindow;

        public void SetSourceImage(HObject image, bool fit)
        {
            if (image == null || !image.IsInitialized())
            {
                ClearImage();
                return;
            }

            HObject copied = null;
            HOperatorSet.CopyImage(image, out copied);
            ClearImage();
            SourceHobject = copied;
            SetShowImage(true);
            if (fit) FitImage();
        }

        public void RenderImageOverlay(IEnumerable<Tuple<Rectangle, string, string>> overlays)
        {
            var window = GetHalconWindow();
            if (SourceHobject != null && SourceHobject.IsInitialized())
            {
                window.ClearWindow();
                window.DispObj(SourceHobject);
            }
            if (overlays == null) return;

            foreach (var overlay in overlays)
            {
                if (overlay == null) continue;
                var rect = overlay.Item1;
                var color = string.IsNullOrWhiteSpace(overlay.Item2) ? "red" : overlay.Item2;
                HObject region = null;
                try
                {
                    window.SetColor(color);
                    window.SetLineWidth(2);
                    window.SetDraw("margin");
                    HOperatorSet.GenRectangle1(out region, rect.Top, rect.Left, rect.Bottom, rect.Right);
                    window.DispObj(region);
                    if (!string.IsNullOrWhiteSpace(overlay.Item3))
                    {
                        HOperatorSet.DispText(window, overlay.Item3, "image", rect.Top + 3, rect.Left + 3, "yellow", "box", "true");
                    }
                }
                finally
                {
                    if (region != null && region.IsInitialized()) region.Dispose();
                    window.SetDraw("fill");
                }
            }
        }

        private HWindow GetHalconWindow()
        {
            if (_halconWindow != null) return _halconWindow;
            var field = typeof(EWindowControl.EWindowControl).GetField("hWindow", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(typeof(EWindowControl.EWindowControl).FullName, "hWindow");
            _halconWindow = field.GetValue(this) as HWindow;
            if (_halconWindow == null) throw new InvalidOperationException("EWindowControl HALCON window is not initialized.");
            return _halconWindow;
        }
    }
}
