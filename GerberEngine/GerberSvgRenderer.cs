using System;
using System.Globalization;
using System.Text;
using System.Security;
using System.Drawing;

namespace GerberEngine
{
    public sealed class GerberSvgRenderer
    {
        public string Render(GerberScene scene, SvgRenderOptions options)
        {
            RectangleD view = scene.BoundsMm;
            view.Inflate(options.MarginMm);
            StringBuilder sb = new StringBuilder();
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" viewBox=\"");
            sb.Append(F(view.MinX)).Append(' ').Append(F(-view.MaxY)).Append(' ').Append(F(view.Width)).Append(' ').Append(F(view.Height)).Append("\">");
            sb.Append("<rect x=\"").Append(F(view.MinX)).Append("\" y=\"").Append(F(-view.MaxY)).Append("\" width=\"").Append(F(view.Width)).Append("\" height=\"").Append(F(view.Height)).Append("\" fill=\"").Append(SecurityElement.Escape(options.BackgroundCss)).Append("\"/>");
            int layerIndex = 0;
            foreach (GerberSceneLayer layer in scene.Layers)
            {
                if (!layer.Visible) { layerIndex++; continue; }
                string color = ToCss(options.Mode == ColorMode.BinaryMask ? Color.White : layer.DisplayColor);
                sb.Append("<g id=\"layer-").Append(layerIndex).Append("-\").Append(SecurityElement.Escape(SafeId(layer.FileName))).Append("\" fill=\"").Append(color).Append("\" stroke=\"").Append(color).Append("\">");
                foreach (GerberPrimitive primitive in layer.Primitives) AppendPrimitive(sb, primitive);
                sb.Append("</g>");
                layerIndex++;
            }
            sb.Append("</svg>");
            return sb.ToString();
        }

        private static void AppendPrimitive(StringBuilder sb, GerberPrimitive primitive)
        {
            StrokePrimitive stroke = primitive as StrokePrimitive;
            if (stroke != null)
            {
                double width = Math.Max(0.001, stroke.Aperture.StrokeDiameter);
                sb.Append("<line x1=\"").Append(F(stroke.Start.X)).Append("\" y1=\"").Append(F(-stroke.Start.Y)).Append("\" x2=\"").Append(F(stroke.End.X)).Append("\" y2=\"").Append(F(-stroke.End.Y)).Append("\" stroke-width=\"").Append(F(width)).Append("\" stroke-linecap=\"round\"/>");
                return;
            }

            ArcPrimitive arc = primitive as ArcPrimitive;
            if (arc != null)
            {
                double r = arc.Radius;
                int sweep = arc.Clockwise ? 1 : 0;
                sb.Append("<path d=\"M ").Append(F(arc.Start.X)).Append(' ').Append(F(-arc.Start.Y)).Append(" A ").Append(F(r)).Append(' ').Append(F(r)).Append(" 0 0 ").Append(sweep).Append(' ').Append(F(arc.End.X)).Append(' ').Append(F(-arc.End.Y)).Append("\" fill=\"none\" stroke-width=\"").Append(F(Math.Max(0.001, arc.Aperture.StrokeDiameter))).Append("\" stroke-linecap=\"round\"/>");
                return;
            }

            FlashPrimitive flash = primitive as FlashPrimitive;
            if (flash != null) { AppendFlash(sb, flash); return; }

            RegionPrimitive region = primitive as RegionPrimitive;
            if (region != null) { AppendRegion(sb, region); return; }
        }

        private static void AppendFlash(StringBuilder sb, FlashPrimitive flash)
        {
            double[] p = flash.Aperture.Parameters;
            if (flash.Aperture.Shape == ApertureShape.Rectangle && p.Length > 1)
            {
                sb.Append("<rect x=\"").Append(F(flash.Position.X - p[0] / 2)).Append("\" y=\"").Append(F(-flash.Position.Y - p[1] / 2)).Append("\" width=\"").Append(F(p[0])).Append("\" height=\"").Append(F(p[1])).Append("\"/>");
                return;
            }
            double d = p.Length > 0 ? p[0] : 0.1;
            sb.Append("<circle cx=\"").Append(F(flash.Position.X)).Append("\" cy=\"").Append(F(-flash.Position.Y)).Append("\" r=\"").Append(F(d / 2)).Append("\"/>");
        }

        private static void AppendRegion(StringBuilder sb, RegionPrimitive region)
        {
            sb.Append("<path fill-rule=\"evenodd\" d=\"");
            foreach (RegionContour contour in region.Contours)
            {
                sb.Append('M').Append(F(contour.Start.X)).Append(' ').Append(F(-contour.Start.Y)).Append(' ');
                foreach (RegionSegment segment in contour.Segments)
                {
                    if (segment.IsArc)
                    {
                        double dx = segment.End.X - segment.Center.X;
                        double dy = segment.End.Y - segment.Center.Y;
                        double r = Math.Sqrt(dx * dx + dy * dy);
                        sb.Append('A').Append(F(r)).Append(' ').Append(F(r)).Append(" 0 0 ").Append(segment.Clockwise ? "1 " : "0 ").Append(F(segment.End.X)).Append(' ').Append(F(-segment.End.Y)).Append(' ');
                    }
                    else sb.Append('L').Append(F(segment.End.X)).Append(' ').Append(F(-segment.End.Y)).Append(' ');
                }
                sb.Append("Z ");
            }
            sb.Append("\"/>");
        }

        private static string F(double value) { return value.ToString("0.######", CultureInfo.InvariantCulture); }
        private static string ToCss(Color c) { return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2"); }
        private static string SafeId(string value) { return string.IsNullOrEmpty(value) ? "layer" : value.Replace(' ', '-').Replace('.', '-'); }
    }
}
