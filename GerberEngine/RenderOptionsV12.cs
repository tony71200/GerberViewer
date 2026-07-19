using System.Drawing;

namespace GerberEngine
{
    public sealed class SvgRenderOptions
    {
        public double MarginMm = 2.0;
        public ColorMode Mode = ColorMode.Realistic;
        public string BackgroundCss = "#0a2d19";
    }

    public sealed class RasterExportOptions
    {
        public int Dpi = 600;
        public ColorMode Mode = ColorMode.Realistic;
        public double MarginMm = 2.0;
        public Color Background = GerberRenderer.RealisticBackground;
        public bool InvertBinary = false;
    }
}
