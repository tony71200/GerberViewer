using System;
using System.Globalization;
using System.Text;
using System.Drawing;

namespace GerberEngine
{
    public sealed class GerberSvgRenderer
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public string RenderCombinedSvg(GerberScene scene, SvgRenderOptions options)
        {
            if (scene == null) throw new ArgumentNullException("scene");
            if (options == null) options = new SvgRenderOptions();
            RectangleD b = scene.GetCombinedBoundsMm();
            if (b.IsEmpty) return EmptySvg();
            b.Inflate(options.MarginMm);
            string bg = string.IsNullOrEmpty(options.BackgroundCss) ? ColorToCss(GerberRasterExportRenderer.RealisticBackground) : options.BackgroundCss;
            var sb = new StringBuilder();
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" viewBox=\"")
              .Append(F(b.MinX)).Append(' ').Append(F(-b.MaxY)).Append(' ')
              .Append(F(b.Width)).Append(' ').Append(F(b.Height)).Append("\">");
            sb.Append("<rect x=\"").Append(F(b.MinX)).Append("\" y=\"").Append(F(-b.MaxY)).Append("\" width=\"")
              .Append(F(b.Width)).Append("\" height=\"").Append(F(b.Height)).Append("\" fill=\"").Append(E(bg)).Append("\"/>");
            foreach (GerberSceneLayer layer in scene.Layers)
            {
                if (!layer.Visible) continue;
                string color = options.Mode == ColorMode.BinaryMask ? "#ffffff" : ColorToCss(layer.DisplayColor);
                sb.Append("<g id=\"").Append(E(layer.Id)).Append("\" fill=\"").Append(color)
                  .Append("\" stroke=\"").Append(color).Append("\">");
                foreach (GerberPrimitive prim in layer.Primitives) AppendPrimitive(sb, prim, color);
                sb.Append("</g>");
            }
            sb.Append("</svg>");
            return sb.ToString();
        }

        private static void AppendPrimitive(StringBuilder sb, GerberPrimitive prim, string color)
        {
            var s = prim as StrokePrimitive;
            if (s != null) { AppendStroke(sb, s); return; }
            var a = prim as ArcPrimitive;
            if (a != null) { AppendArc(sb, a); return; }
            var f = prim as FlashPrimitive;
            if (f != null) { AppendFlash(sb, f); return; }
            var r = prim as RegionPrimitive;
            if (r != null) { AppendRegion(sb, r); return; }
        }

        private static void AppendStroke(StringBuilder sb, StrokePrimitive s)
        {
            double w = Math.Max(0.001, s.Aperture.StrokeDiameter);
            sb.Append("<line x1=\"").Append(F(s.Start.X)).Append("\" y1=\"").Append(F(-s.Start.Y))
              .Append("\" x2=\"").Append(F(s.End.X)).Append("\" y2=\"").Append(F(-s.End.Y))
              .Append("\" stroke-width=\"").Append(F(w)).Append("\" stroke-linecap=\"round\" stroke-linejoin=\"round\" fill=\"none\"/>");
        }

        private static void AppendArc(StringBuilder sb, ArcPrimitive a)
        {
            double r = a.Radius;
            int sweep = a.Clockwise ? 1 : 0;
            sb.Append("<path d=\"M ").Append(F(a.Start.X)).Append(' ').Append(F(-a.Start.Y)).Append(" A ")
              .Append(F(r)).Append(' ').Append(F(r)).Append(" 0 0 ").Append(sweep).Append(' ')
              .Append(F(a.End.X)).Append(' ').Append(F(-a.End.Y)).Append("\" stroke-width=\"")
              .Append(F(Math.Max(0.001, a.Aperture.StrokeDiameter))).Append("\" stroke-linecap=\"round\" fill=\"none\"/>");
        }

        private static void AppendFlash(StringBuilder sb, FlashPrimitive f)
        {
            double[] p = f.Aperture.Parameters;
            if (f.Aperture.Shape == ApertureShape.Rectangle && p.Length > 1)
            {
                sb.Append("<rect x=\"").Append(F(f.Position.X - p[0] / 2)).Append("\" y=\"").Append(F(-f.Position.Y - p[1] / 2))
                  .Append("\" width=\"").Append(F(p[0])).Append("\" height=\"").Append(F(p[1])).Append("\"/>");
            }
            else
            {
                double d = p.Length > 0 ? p[0] : 0.1;
                sb.Append("<circle cx=\"").Append(F(f.Position.X)).Append("\" cy=\"").Append(F(-f.Position.Y))
                  .Append("\" r=\"").Append(F(d / 2)).Append("\"/>");
            }
        }

        private static void AppendRegion(StringBuilder sb, RegionPrimitive r)
        {
            sb.Append("<path fill-rule=\"evenodd\" d=\"");
            foreach (RegionContour c in r.Contours)
            {
                PointD cur = c.Start;
                sb.Append("M ").Append(F(cur.X)).Append(' ').Append(F(-cur.Y)).Append(' ');
                foreach (RegionSegment seg in c.Segments)
                {
                    if (seg.IsArc)
                    {
                        double dx = cur.X - seg.Center.X, dy = cur.Y - seg.Center.Y;
                        double rad = Math.Sqrt(dx * dx + dy * dy);
                        sb.Append("A ").Append(F(rad)).Append(' ').Append(F(rad)).Append(" 0 0 ")
                          .Append(seg.Clockwise ? 1 : 0).Append(' ').Append(F(seg.End.X)).Append(' ').Append(F(-seg.End.Y)).Append(' ');
                    }
                    else sb.Append("L ").Append(F(seg.End.X)).Append(' ').Append(F(-seg.End.Y)).Append(' ');
                    cur = seg.End;
                }
                sb.Append("Z ");
            }
            sb.Append("\"/>");
        }

        private static string EmptySvg() { return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"></svg>"; }
        private static string F(double v) { return v.ToString("0.########", Inv); }
        private static string ColorToCss(Color c) { return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2"); }
        private static string E(string s) { return (s ?? "").Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;"); }
    }
}
