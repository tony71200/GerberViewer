using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
                    AddLabelTextRegions(overlay.Item3, rect.Left + lineWidth * 3, rect.Top + lineWidth * 4, labelSize, Math.Max(1, lineWidth), ToRgba("black"), ToRgba("white"));
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

        private void AddLabelTextRegions(string text, int x, int y, int size, int borderWidth, int[] borderColor, int[] textColor)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            using (var font = new Font(FontFamily.GenericSansSerif, Math.Max(6, size), FontStyle.Bold, GraphicsUnit.Pixel))
            using (var probe = new Bitmap(1, 1))
            using (var probeGraphics = Graphics.FromImage(probe))
            {
                var measured = Size.Ceiling(probeGraphics.MeasureString(text, font));
                var pad = Math.Max(2, borderWidth * 2);
                var width = Math.Max(1, measured.Width + pad * 2);
                var height = Math.Max(1, measured.Height + pad * 2);
                using (var borderMask = new Bitmap(width, height))
                using (var textMask = new Bitmap(width, height))
                {
                    DrawTextMask(borderMask, text, font, pad, pad, borderWidth, true);
                    DrawTextMask(textMask, text, font, pad, pad, 0, false);
                    AddMaskRegion(borderMask, x - pad, y - pad, borderColor);
                    AddMaskRegion(textMask, x - pad, y - pad, textColor);
                }
            }
        }

        private static void DrawTextMask(Bitmap bitmap, string text, Font font, int x, int y, int borderWidth, bool drawBorder)
        {
            using (var graphics = Graphics.FromImage(bitmap))
            using (var path = new GraphicsPath())
            using (var brush = new SolidBrush(Color.White))
            {
                graphics.Clear(Color.Black);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                path.AddString(text, font.FontFamily, (int)font.Style, font.Size, new Point(x, y), StringFormat.GenericDefault);
                if (drawBorder)
                {
                    using (var pen = new Pen(Color.White, Math.Max(1, borderWidth * 2)))
                    {
                        pen.LineJoin = LineJoin.Round;
                        graphics.DrawPath(pen, path);
                    }
                }
                else
                {
                    graphics.FillPath(brush, path);
                }
            }
        }

        private void AddMaskRegion(Bitmap bitmap, int offsetX, int offsetY, int[] color)
        {
            var rows = new List<int>();
            var starts = new List<int>();
            var ends = new List<int>();
            for (int row = 0; row < bitmap.Height; row++)
            {
                int runStart = -1;
                for (int col = 0; col < bitmap.Width; col++)
                {
                    var active = bitmap.GetPixel(col, row).R > 16;
                    if (active && runStart < 0) runStart = col;
                    if ((!active || col + 1 == bitmap.Width) && runStart >= 0)
                    {
                        var runEnd = active && col + 1 == bitmap.Width ? col : col - 1;
                        rows.Add(offsetY + row);
                        starts.Add(offsetX + runStart);
                        ends.Add(offsetX + runEnd);
                        runStart = -1;
                    }
                }
            }
            if (rows.Count == 0) return;
            HObject region = null;
            HOperatorSet.GenRegionRuns(out region, new HTuple(rows.ToArray()), new HTuple(starts.ToArray()), new HTuple(ends.ToArray()));
            _overlayRegions.Add(region);
            _overlayColors.Add(color);
        }

        private static int[] ToRgba(string color)
        {
            if (string.Equals(color, "green", StringComparison.OrdinalIgnoreCase)) return new[] { 0, 200, 0, 150 };
            if (string.Equals(color, "yellow", StringComparison.OrdinalIgnoreCase)) return new[] { 255, 220, 0, 150 };
            if (string.Equals(color, "white", StringComparison.OrdinalIgnoreCase)) return new[] { 255, 255, 255, 220 };
            if (string.Equals(color, "black", StringComparison.OrdinalIgnoreCase)) return new[] { 0, 0, 0, 220 };
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
