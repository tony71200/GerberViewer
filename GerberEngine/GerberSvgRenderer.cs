// GerberEngine/GerberSvgRenderer.cs
// Pure SVG renderer for GerberScene data. All geometry is emitted in millimeters.
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Xml;

namespace GerberEngine
{
    /// <summary>
    /// Options for renderer-independent SVG generation.
    /// Colors are SVG/CSS color strings; geometry and viewBox units are millimeters.
    /// </summary>
    public sealed class SvgRenderOptions
    {
        public double MarginMm = 2.0;
        public ColorMode Mode = ColorMode.Realistic;
        public bool InvertBinary = false;
        public bool IncludeBackground = true;
        public string BackgroundColor = null;
        public string BinaryForegroundColor = null;
        public string BinaryBackgroundColor = null;
    }

    /// <summary>
    /// Renders GerberScene to an SVG string using only scene-domain millimeter geometry.
    /// </summary>
    public sealed class GerberSvgRenderer
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private const int ProgressPrimitiveBatchSize = 10000;

        public string Render(GerberScene scene, SvgRenderOptions options)
        {
            return Render(scene, options, CancellationToken.None, null);
        }

        public string Render(GerberScene scene, SvgRenderOptions options, Action<string> reportProgress)
        {
            return Render(scene, options, CancellationToken.None, reportProgress);
        }

        public string Render(GerberScene scene, SvgRenderOptions options, CancellationToken cancellationToken)
        {
            return Render(scene, options, cancellationToken, null);
        }

        public string Render(GerberScene scene, SvgRenderOptions options, CancellationToken cancellationToken, Action<string> reportProgress)
        {
            if (scene == null) throw new ArgumentNullException("scene");
            if (options == null) options = new SvgRenderOptions();

            cancellationToken.ThrowIfCancellationRequested();
            Report(reportProgress, "Calculating bounds");
            RectangleD bounds = GetBoundsMm(scene, cancellationToken, reportProgress);
            cancellationToken.ThrowIfCancellationRequested();
            if (bounds.IsEmpty)
            {
                bounds = new RectangleD { MinX = 0, MinY = 0, MaxX = 1, MaxY = 1 };
            }

            double margin = Math.Max(0, options.MarginMm);
            bounds.Inflate(margin);
            double width = Math.Max(bounds.Width, 0.001);
            double height = Math.Max(bounds.Height, 0.001);

            var sb = new StringBuilder(16384);
            var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement("svg", "http://www.w3.org/2000/svg");
                writer.WriteAttributeString("version", "1.1");
                writer.WriteAttributeString("width", Format(width) + "mm");
                writer.WriteAttributeString("height", Format(height) + "mm");
                writer.WriteAttributeString("viewBox", Format(bounds.MinX) + " " + Format(-bounds.MaxY) + " " + Format(width) + " " + Format(height));
                writer.WriteAttributeString("xmlns", "xlink", null, "http://www.w3.org/1999/xlink");

                Report(reportProgress, "Generating SVG definitions");

                if (options.IncludeBackground)
                {
                    writer.WriteStartElement("rect");
                    writer.WriteAttributeString("x", Format(bounds.MinX));
                    writer.WriteAttributeString("y", Format(-bounds.MaxY));
                    writer.WriteAttributeString("width", Format(width));
                    writer.WriteAttributeString("height", Format(height));
                    writer.WriteAttributeString("fill", ResolveBackground(options));
                    writer.WriteEndElement();
                }

                Report(reportProgress, "Generating layer geometry");
                writer.WriteStartElement("g");
                writer.WriteAttributeString("transform", "scale(1,-1)");
                int layerIndex = 0;
                foreach (GerberSceneLayer layer in scene.Layers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Report(reportProgress, "Generating SVG layer " + (layerIndex + 1).ToString(Invariant));
                    string color = ResolveLayerColor(layer, layerIndex, options);
                    WriteLayer(writer, layer, color, layerIndex, cancellationToken, reportProgress);
                    layerIndex++;
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            return sb.ToString();
        }

        private static void Report(Action<string> reportProgress, string stage)
        {
            if (reportProgress != null) reportProgress(stage);
        }

        private static RectangleD GetBoundsMm(GerberScene scene, CancellationToken cancellationToken, Action<string> reportProgress)
        {
            RectangleD bounds = RectangleD.Empty;
            int layerIndex = 0;
            foreach (GerberSceneLayer layer in scene.Layers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Report(reportProgress, "Calculating bounds for layer " + (layerIndex + 1).ToString(Invariant));
                int primitiveIndex = 0;
                foreach (GerberScenePrimitive primitive in layer.Primitives)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (primitiveIndex > 0 && primitiveIndex % ProgressPrimitiveBatchSize == 0) Report(reportProgress, "Calculated bounds for " + primitiveIndex.ToString(Invariant) + " primitives");
                    bounds.Expand(primitive.GetBoundsMm());
                    primitiveIndex++;
                }
                layerIndex++;
            }
            return bounds;
        }

        private static void WriteLayer(XmlWriter writer, GerberSceneLayer layer, string color, int layerIndex, CancellationToken cancellationToken, Action<string> reportProgress)
        {
            string maskId = "gerber-layer-mask-" + layerIndex.ToString(Invariant);
            writer.WriteStartElement("defs");
            writer.WriteStartElement("mask");
            writer.WriteAttributeString("id", maskId);
            writer.WriteAttributeString("maskUnits", "userSpaceOnUse");
            writer.WriteStartElement("g");
            int primitiveIndex = 0;
            foreach (GerberScenePrimitive primitive in layer.Primitives)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (primitiveIndex > 0 && primitiveIndex % ProgressPrimitiveBatchSize == 0) Report(reportProgress, "Generated " + primitiveIndex.ToString(Invariant) + " SVG mask primitives");
                WritePrimitive(writer, primitive, primitive.Polarity == GerberPolarity.Clear ? "black" : "white", cancellationToken);
                primitiveIndex++;
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("g");
            writer.WriteAttributeString("id", "gerber-layer-" + layerIndex.ToString(Invariant));
            writer.WriteAttributeString("data-layer-index", layerIndex.ToString(Invariant));
            writer.WriteAttributeString("mask", "url(#" + maskId + ")");
            primitiveIndex = 0;
            foreach (GerberScenePrimitive primitive in layer.Primitives)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (primitiveIndex > 0 && primitiveIndex % ProgressPrimitiveBatchSize == 0) Report(reportProgress, "Generated " + primitiveIndex.ToString(Invariant) + " SVG primitives");
                if (primitive.Polarity == GerberPolarity.Dark) WritePrimitive(writer, primitive, color, cancellationToken);
                primitiveIndex++;
            }
            writer.WriteEndElement();
        }

        private static void WritePrimitive(XmlWriter writer, GerberScenePrimitive primitive, string color, CancellationToken cancellationToken)
        {
            var stroke = primitive as StrokePrimitive;
            if (stroke != null) { WriteStroke(writer, stroke, color); return; }
            var arc = primitive as ArcPrimitive;
            if (arc != null) { WriteArc(writer, arc, color); return; }
            var flash = primitive as FlashPrimitive;
            if (flash != null) { WriteFlash(writer, flash, color, cancellationToken); return; }
            var region = primitive as RegionPrimitive;
            if (region != null) { WriteRegion(writer, region, color, cancellationToken); return; }
        }

        private static void WriteStroke(XmlWriter writer, StrokePrimitive stroke, string color)
        {
            writer.WriteStartElement("line");
            writer.WriteAttributeString("x1", Format(stroke.Start.X));
            writer.WriteAttributeString("y1", Format(stroke.Start.Y));
            writer.WriteAttributeString("x2", Format(stroke.End.X));
            writer.WriteAttributeString("y2", Format(stroke.End.Y));
            writer.WriteAttributeString("stroke", color);
            writer.WriteAttributeString("stroke-width", Format(Math.Max(stroke.Aperture.StrokeDiameter, 0.001)));
            writer.WriteAttributeString("stroke-linecap", stroke.Aperture.Shape == ApertureShape.Rectangle ? "square" : "round");
            writer.WriteAttributeString("stroke-linejoin", "round");
            writer.WriteAttributeString("fill", "none");
            writer.WriteEndElement();
        }

        private static void WriteArc(XmlWriter writer, ArcPrimitive arc, string color)
        {
            writer.WriteStartElement("path");
            writer.WriteAttributeString("d", ArcPath(arc.Start, arc.End, arc.Center, arc.Clockwise));
            writer.WriteAttributeString("stroke", color);
            writer.WriteAttributeString("stroke-width", Format(Math.Max(arc.Aperture.StrokeDiameter, 0.001)));
            writer.WriteAttributeString("stroke-linecap", arc.Aperture.Shape == ApertureShape.Rectangle ? "square" : "round");
            writer.WriteAttributeString("stroke-linejoin", "round");
            writer.WriteAttributeString("fill", "none");
            writer.WriteEndElement();
        }

        private static void WriteFlash(XmlWriter writer, FlashPrimitive flash, string color, CancellationToken cancellationToken)
        {
            double[] p = flash.Aperture.Parameters;
            switch (flash.Aperture.Shape)
            {
                case ApertureShape.Circle:
                    WriteCircle(writer, flash.Position, p.Length > 0 ? p[0] / 2 : 0, color); break;
                case ApertureShape.Rectangle:
                    WriteRect(writer, flash.Position, p, color); break;
                case ApertureShape.Obround:
                    WriteObround(writer, flash.Position, p, color); break;
                case ApertureShape.Polygon:
                    WritePolygon(writer, flash.Position, p, color, cancellationToken); break;
                default:
                    WriteCircle(writer, flash.Position, flash.GetBoundsMm().Width / 2, color); break;
            }
        }

        private static void WriteCircle(XmlWriter writer, PointD center, double radius, string color)
        {
            writer.WriteStartElement("circle");
            writer.WriteAttributeString("cx", Format(center.X));
            writer.WriteAttributeString("cy", Format(center.Y));
            writer.WriteAttributeString("r", Format(Math.Max(radius, 0.001)));
            writer.WriteAttributeString("fill", color);
            writer.WriteEndElement();
        }

        private static void WriteRect(XmlWriter writer, PointD center, double[] p, string color)
        {
            double w = p.Length > 0 ? p[0] : 0.001, h = p.Length > 1 ? p[1] : w;
            writer.WriteStartElement("rect");
            writer.WriteAttributeString("x", Format(center.X - w / 2));
            writer.WriteAttributeString("y", Format(center.Y - h / 2));
            writer.WriteAttributeString("width", Format(Math.Max(w, 0.001)));
            writer.WriteAttributeString("height", Format(Math.Max(h, 0.001)));
            writer.WriteAttributeString("fill", color);
            writer.WriteEndElement();
        }

        private static void WriteObround(XmlWriter writer, PointD center, double[] p, string color)
        {
            double w = p.Length > 0 ? p[0] : 0.001, h = p.Length > 1 ? p[1] : w;
            double r = Math.Min(w, h) / 2;
            writer.WriteStartElement("rect");
            writer.WriteAttributeString("x", Format(center.X - w / 2));
            writer.WriteAttributeString("y", Format(center.Y - h / 2));
            writer.WriteAttributeString("width", Format(Math.Max(w, 0.001)));
            writer.WriteAttributeString("height", Format(Math.Max(h, 0.001)));
            writer.WriteAttributeString("rx", Format(Math.Max(r, 0.001)));
            writer.WriteAttributeString("ry", Format(Math.Max(r, 0.001)));
            writer.WriteAttributeString("fill", color);
            writer.WriteEndElement();
        }

        private static void WritePolygon(XmlWriter writer, PointD center, double[] p, string color, CancellationToken cancellationToken)
        {
            double diameter = p.Length > 0 ? p[0] : 0.001;
            int n = p.Length > 1 ? Math.Max(3, (int)p[1]) : 3;
            double rotDeg = p.Length > 2 ? p[2] : 0;
            var points = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double ang = rotDeg * Math.PI / 180 + 2 * Math.PI * i / n;
                if (i > 0) points.Append(' ');
                points.Append(Format(center.X + diameter / 2 * Math.Cos(ang))).Append(',').Append(Format(center.Y + diameter / 2 * Math.Sin(ang)));
            }
            writer.WriteStartElement("polygon");
            writer.WriteAttributeString("points", points.ToString());
            writer.WriteAttributeString("fill", color);
            writer.WriteEndElement();
        }

        private static void WriteRegion(XmlWriter writer, RegionPrimitive region, string color, CancellationToken cancellationToken)
        {
            var d = new StringBuilder();
            foreach (RegionContour contour in region.Contours)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PointD cur = contour.Start;
                d.Append("M ").Append(Format(cur.X)).Append(' ').Append(Format(cur.Y));
                foreach (RegionSegment segment in contour.Segments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    d.Append(' ');
                    if (segment.IsArc) d.Append(ArcCommand(cur, segment.End, segment.Center, segment.Clockwise));
                    else d.Append("L ").Append(Format(segment.End.X)).Append(' ').Append(Format(segment.End.Y));
                    cur = segment.End;
                }
                d.Append(" Z ");
            }
            writer.WriteStartElement("path");
            writer.WriteAttributeString("d", d.ToString());
            writer.WriteAttributeString("fill", color);
            writer.WriteAttributeString("fill-rule", "nonzero");
            writer.WriteEndElement();
        }

        private static string ArcPath(PointD start, PointD end, PointD center, bool clockwise)
        {
            return "M " + Format(start.X) + " " + Format(start.Y) + " " + ArcCommand(start, end, center, clockwise);
        }

        private static string ArcCommand(PointD start, PointD end, PointD center, bool clockwise)
        {
            double radius = Math.Sqrt((start.X - center.X) * (start.X - center.X) + (start.Y - center.Y) * (start.Y - center.Y));
            bool fullCircle = Math.Abs(start.X - end.X) < 1e-9 && Math.Abs(start.Y - end.Y) < 1e-9;
            if (fullCircle)
            {
                PointD mid = new PointD(center.X - (start.X - center.X), center.Y - (start.Y - center.Y));
                return SvgArc(start, mid, center, radius, clockwise) + " " + SvgArc(mid, end, center, radius, clockwise);
            }
            return SvgArc(start, end, center, radius, clockwise);
        }

        private static string SvgArc(PointD start, PointD end, PointD center, double radius, bool clockwise)
        {
            double a1 = Math.Atan2(start.Y - center.Y, start.X - center.X);
            double a2 = Math.Atan2(end.Y - center.Y, end.X - center.X);
            double sweep = clockwise ? NormalizeRadians(a1 - a2) : NormalizeRadians(a2 - a1);
            int largeArc = sweep > Math.PI ? 1 : 0;
            int sweepFlag = clockwise ? 0 : 1;
            return "A " + Format(radius) + " " + Format(radius) + " 0 " + largeArc.ToString(Invariant) + " " + sweepFlag.ToString(Invariant) + " " + Format(end.X) + " " + Format(end.Y);
        }

        private static double NormalizeRadians(double value)
        {
            value = value % (2 * Math.PI);
            if (value <= 0) value += 2 * Math.PI;
            return value;
        }

        private static string ResolveBackground(SvgRenderOptions options)
        {
            if (!string.IsNullOrEmpty(options.BackgroundColor)) return options.BackgroundColor;
            if (options.Mode == ColorMode.BinaryMask)
            {
                if (!string.IsNullOrEmpty(options.BinaryBackgroundColor)) return options.BinaryBackgroundColor;
                return options.InvertBinary ? "white" : "black";
            }
            return "#0A2D19";
        }

        private static string ResolveLayerColor(GerberSceneLayer layer, int index, SvgRenderOptions options)
        {
            if (options.Mode == ColorMode.BinaryMask)
            {
                if (!string.IsNullOrEmpty(options.BinaryForegroundColor)) return options.BinaryForegroundColor;
                return options.InvertBinary ? "black" : "white";
            }
            GerberLayer gerberLayer = layer as GerberLayer;
            if (gerberLayer != null) return ColorToSvg(gerberLayer.DisplayColor);
            switch (layer.Type)
            {
                case LayerType.TopCopper:
                case LayerType.BottomCopper:
                case LayerType.InnerCopper: return "#E0B252";
                case LayerType.TopSolderMask:
                case LayerType.BottomSolderMask: return "rgba(0,102,51,0.59)";
                case LayerType.TopSilkscreen:
                case LayerType.BottomSilkscreen: return "#F0F0F0";
                case LayerType.BoardOutline: return "#C8C83C";
                case LayerType.Drill: return "#282828";
                default: return index % 2 == 0 ? "gold" : "#D0D0D0";
            }
        }

        private static string ColorToSvg(System.Drawing.Color color)
        {
            return "#" + color.R.ToString("X2", Invariant) + color.G.ToString("X2", Invariant) + color.B.ToString("X2", Invariant);
        }

        private static string Format(double value)
        {
            return value.ToString("0.##########", Invariant);
        }
    }
}
