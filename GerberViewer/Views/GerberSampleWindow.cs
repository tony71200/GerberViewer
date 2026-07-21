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
            var zoom = Math.Max(0.01d, CurrentZoom);
            var lineWidth = Math.Max(1, (int)Math.Round(2d / zoom));
            var labelSize = Math.Max(4, (int)Math.Round(14d / zoom));
            if (overlays != null)
            {
                foreach (var overlay in overlays)
                {
                    if (overlay == null) continue;
                    var rect = overlay.Item1;
                    AddRectangleOutline(rect, lineWidth, ToRgba(overlay.Item2));
                    AddLabelContours(overlay.Item3, rect.Left + lineWidth * 3, rect.Top + lineWidth * 4, labelSize, lineWidth);
                }
            }
            if (_overlayRegions.Count > 0) ShowRegions(_overlayRegions, _overlayColors, false);
            else if (SourceHobject != null && SourceHobject.IsInitialized()) SetShowImage(true);
            EnableMouseWheelZoom = true;
            WinOperate = 1;
        }

        private void AddRectangleOutline(Rectangle rect, int lineWidth, int[] color)
        {
            var top = rect.Top;
            var left = rect.Left;
            var bottom = Math.Max(rect.Top + 1, rect.Bottom);
            var right = Math.Max(rect.Left + 1, rect.Right);
            AddFilledRect(top, left, Math.Min(bottom, top + lineWidth), right, color);
            AddFilledRect(Math.Max(top, bottom - lineWidth), left, bottom, right, color);
            AddFilledRect(top, left, bottom, Math.Min(right, left + lineWidth), color);
            AddFilledRect(top, Math.Max(left, right - lineWidth), bottom, right, color);
        }

        private void AddLabelContours(string text, int x, int y, int size, int lineWidth)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var cursor = x;
            foreach (var ch in text)
            {
                if (char.IsDigit(ch)) AddDigitContours(ch - '0', cursor, y, size, lineWidth, ToRgba("yellow"));
                cursor += Math.Max(1, size / 2) + lineWidth * 2;
            }
        }

        private void AddDigitContours(int digit, int x, int y, int size, int lineWidth, int[] color)
        {
            var segments = DigitSegments(digit);
            var w = Math.Max(4, size / 2);
            var h = Math.Max(7, size);
            var half = h / 2;
            if (segments[0]) AddSegment(y, x, y, x + w, lineWidth, color);
            if (segments[1]) AddSegment(y, x + w, y + half, x + w, lineWidth, color);
            if (segments[2]) AddSegment(y + half, x + w, y + h, x + w, lineWidth, color);
            if (segments[3]) AddSegment(y + h, x, y + h, x + w, lineWidth, color);
            if (segments[4]) AddSegment(y + half, x, y + h, x, lineWidth, color);
            if (segments[5]) AddSegment(y, x, y + half, x, lineWidth, color);
            if (segments[6]) AddSegment(y + half, x, y + half, x + w, lineWidth, color);
        }

        private void AddSegment(int row1, int col1, int row2, int col2, int lineWidth, int[] color)
        {
            if (row1 == row2)
            {
                var top = row1 - lineWidth / 2;
                AddFilledRect(top, Math.Min(col1, col2), top + lineWidth, Math.Max(col1, col2) + lineWidth, color);
            }
            else
            {
                var left = col1 - lineWidth / 2;
                AddFilledRect(Math.Min(row1, row2), left, Math.Max(row1, row2) + lineWidth, left + lineWidth, color);
            }
        }

        private void AddFilledRect(int top, int left, int bottom, int right, int[] color)
        {
            HObject region = null;
            HOperatorSet.GenRectangle1(out region, top, left, Math.Max(top, bottom - 1), Math.Max(left, right - 1));
            _overlayRegions.Add(region);
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
