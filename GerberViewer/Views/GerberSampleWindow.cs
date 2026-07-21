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
                    AddRectangleOutline(rect, ToRgba(overlay.Item2));
                    AddLabelContours(overlay.Item3, rect.Left + 6, rect.Top + 8);
                }
            }
            if (_overlayRegions.Count > 0) ShowRegions(_overlayRegions, _overlayColors, false);
            else if (SourceHobject != null && SourceHobject.IsInitialized()) SetShowImage(true);
            EnableMouseWheelZoom = true;
            WinOperate = 1;
        }

        private void AddRectangleOutline(Rectangle rect, int[] color)
        {
            HObject contour = null;
            var top = rect.Top;
            var left = rect.Left;
            var bottom = Math.Max(rect.Top, rect.Bottom - 1);
            var right = Math.Max(rect.Left, rect.Right - 1);
            HOperatorSet.GenContourPolygonXld(out contour, new HTuple(new double[] { top, bottom, bottom, top, top }), new HTuple(new double[] { left, left, right, right, left }));
            _overlayRegions.Add(contour);
            _overlayColors.Add(color);
        }

        private void AddLabelContours(string text, int x, int y)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var cursor = x;
            foreach (var ch in text)
            {
                if (char.IsDigit(ch)) AddDigitContours(ch - '0', cursor, y, 9, ToRgba("yellow"));
                cursor += 8;
            }
        }

        private void AddDigitContours(int digit, int x, int y, int size, int[] color)
        {
            var segments = DigitSegments(digit);
            var w = Math.Max(4, size / 2);
            var h = Math.Max(7, size);
            if (segments[0]) AddLine(y, x, y, x + w, color);
            if (segments[1]) AddLine(y, x + w, y + h / 2, x + w, color);
            if (segments[2]) AddLine(y + h / 2, x + w, y + h, x + w, color);
            if (segments[3]) AddLine(y + h, x, y + h, x + w, color);
            if (segments[4]) AddLine(y + h / 2, x, y + h, x, color);
            if (segments[5]) AddLine(y, x, y + h / 2, x, color);
            if (segments[6]) AddLine(y + h / 2, x, y + h / 2, x + w, color);
        }

        private void AddLine(int row1, int col1, int row2, int col2, int[] color)
        {
            HObject contour = null;
            HOperatorSet.GenContourPolygonXld(out contour, new HTuple(new double[] { row1, row2 }), new HTuple(new double[] { col1, col2 }));
            _overlayRegions.Add(contour);
            _overlayColors.Add(color);
        }

        private static bool[] DigitSegments(int digit)
        {
            switch (digit)
            {
                case 0: return new[] { true, true, true, true, true, true, false };
                case 1: return new[] { false, true, true, false, false, false, false };
                case 2: return new[] { true, true, false, true, true, false, true };
                case 3: return new[] { true, true, true, true, false, false, true };
                case 4: return new[] { false, true, true, false, false, true, true };
                case 5: return new[] { true, false, true, true, false, true, true };
                case 6: return new[] { true, false, true, true, true, true, true };
                case 7: return new[] { true, true, true, false, false, false, false };
                case 8: return new[] { true, true, true, true, true, true, true };
                case 9: return new[] { true, true, true, true, false, true, true };
                default: return new[] { false, false, false, false, false, false, false };
            }
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
