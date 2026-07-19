using System;
using System.Drawing;

namespace GerberEngine
{
    public sealed class SvgRenderOptions
    {
        public double MarginMm = 2.0;
        public ColorMode Mode = ColorMode.Realistic;
        public string BackgroundCss;
        public RectangleD? ViewportMm;
        public double LodScreenTolerancePx = 0.5;
        public bool EnableViewportCulling;
        public bool ReuseDefinitions = true;
    }

    public sealed class RasterExportOptions
    {
        public int Dpi = 600;
        public ColorMode Mode = ColorMode.Realistic;
        public double MarginMm = 2.0;
        public bool InvertBinary;
        public Color? BackgroundOverride;

        public Color ResolveBackground()
        {
            if (BackgroundOverride.HasValue) return BackgroundOverride.Value;
            if (Mode == ColorMode.BinaryMask) return InvertBinary ? Color.White : Color.Black;
            return GerberRasterExportRenderer.RealisticBackground;
        }

        public Color ResolveForeground(GerberLayer layer)
        {
            if (Mode == ColorMode.BinaryMask) return InvertBinary ? Color.Black : Color.White;
            return layer.DisplayColor;
        }
    }

    public sealed class ViewportBitmapOptions
    {
        public int ViewportWidthPx;
        public int ViewportHeightPx;
        public RectangleD WorldViewportMm;
        public ColorMode Mode = ColorMode.Realistic;
        public Color Background = GerberRasterExportRenderer.RealisticBackground;
    }
}
