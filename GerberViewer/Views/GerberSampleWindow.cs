using System;
using System.Collections.Generic;
using System.Drawing;
using HalconDotNet;

namespace GerberViewer.Views
{
    public sealed class GerberSampleWindow : EWindowControl.EWindowControl
    {
        private readonly List<HObject> _overlayRegions = new List<HObject>();
        private readonly List<int[]> _overlayColors = new List<int[]>();

        public GerberSampleWindow()
        {
            EnableMouseWheelZoom = true;
            WinOperate = 1;
        }

        public void SetSourceImage(HObject image, bool fit)
        {
            ClearOverlayRegions();
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
            EnableMouseWheelZoom = true;
            WinOperate = 1;
            if (fit) FitImage();
        }

        public void RenderImageOverlay(IEnumerable<Tuple<Rectangle, string, string>> overlays)
        {
            ClearOverlayRegions();
            if (overlays != null)
            {
                foreach (var overlay in overlays)
                {
                    if (overlay == null) continue;
                    var rect = overlay.Item1;
                    HObject region = null;
                    HOperatorSet.GenRectangle1(out region, rect.Top, rect.Left, Math.Max(rect.Top, rect.Bottom - 1), Math.Max(rect.Left, rect.Right - 1));
                    _overlayRegions.Add(region);
                    _overlayColors.Add(ToRgba(overlay.Item2));
                }
            }
            if (_overlayRegions.Count > 0) ShowRegions(_overlayRegions, _overlayColors, false);
            else if (SourceHobject != null && SourceHobject.IsInitialized()) SetShowImage(true);
            EnableMouseWheelZoom = true;
            WinOperate = 1;
        }

        private static int[] ToRgba(string color)
        {
            if (string.Equals(color, "green", StringComparison.OrdinalIgnoreCase)) return new[] { 0, 200, 0, 150 };
            if (string.Equals(color, "yellow", StringComparison.OrdinalIgnoreCase)) return new[] { 255, 220, 0, 150 };
            return new[] { 255, 0, 0, 150 };
        }

        private void ClearOverlayRegions()
        {
            foreach (var region in _overlayRegions) if (region != null && region.IsInitialized()) region.Dispose();
            _overlayRegions.Clear();
            _overlayColors.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ClearOverlayRegions();
            base.Dispose(disposing);
        }
    }
}
