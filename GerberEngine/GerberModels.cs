// GerberEngine/GerberModels.cs
// Kieu du lieu loi cua GerberEngine. KHONG tham chieu System.Windows.Forms (NFR-004).
// Moi toa do/kich thuoc noi bo deu la MILIMET (FR-007).
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GerberEngine
{
    public enum GerberUnit { Millimeter, Inch }

    public enum GerberPolarity { Dark, Clear }          // LPD / LPC (FR-013)

    public enum ColorMode { BinaryMask, Realistic }     // FR-011

    public enum ApertureShape { Circle, Rectangle, Obround, Polygon, Macro }

    public enum LayerType
    {
        TopCopper, BottomCopper, InnerCopper,
        TopSolderMask, BottomSolderMask,
        TopSilkscreen, BottomSilkscreen,
        BoardOutline, Drill, Unknown
    }

    /// <summary>Diem 2D do chinh xac kep (mm).</summary>
    public struct PointD
    {
        public double X, Y;
        public PointD(double x, double y) { X = x; Y = y; }
        public override string ToString() { return "(" + X + ", " + Y + ")"; }
    }

    /// <summary>Bounding box mm (thay cho RectangleF de giu chinh xac).</summary>
    public struct RectangleD
    {
        public double MinX, MinY, MaxX, MaxY;
        public bool IsEmpty { get { return MaxX < MinX || MaxY < MinY; } }
        public double Width { get { return MaxX - MinX; } }
        public double Height { get { return MaxY - MinY; } }

        public static RectangleD Empty
        {
            get { return new RectangleD { MinX = double.MaxValue, MinY = double.MaxValue, MaxX = double.MinValue, MaxY = double.MinValue }; }
        }

        public void Expand(double x, double y)
        {
            if (x < MinX) MinX = x; if (x > MaxX) MaxX = x;
            if (y < MinY) MinY = y; if (y > MaxY) MaxY = y;
        }

        public void Expand(RectangleD other)
        {
            if (other.IsEmpty) return;
            Expand(other.MinX, other.MinY);
            Expand(other.MaxX, other.MaxY);
        }

        public void Inflate(double d)
        {
            MinX -= d; MinY -= d; MaxX += d; MaxY += d;
        }
    }

    /// <summary>Dinh nghia aperture macro (lenh AM) - luu block tho, evaluate khi flash (FR-006).</summary>
    public sealed class ApertureMacro
    {
        public string Name;
        public List<string> Blocks = new List<string>();  // moi phan tu la 1 primitive/variable line
    }

    /// <summary>Aperture (lenh AD). Parameters da quy doi ve mm.</summary>
    public sealed class Aperture
    {
        public int Code;                      // D10..D999
        public ApertureShape Shape;
        public double[] Parameters = new double[0]; // C: [dia,(hole)] R/O: [w,h,(hole)] P: [dia,vertices,(rot),(hole)]
        public ApertureMacro Macro;           // khi Shape == Macro
        public double[] MacroArgs = new double[0]; // GIU NGUYEN don vi file (than macro dung cung don vi)
        public double MacroUnitScale = 1.0;   // he so quy doi don vi file -> mm (1.0 hoac 25.4)

        public double StrokeDiameter
        {
            get
            {
                if (Shape == ApertureShape.Circle && Parameters.Length > 0) return Parameters[0];
                if ((Shape == ApertureShape.Rectangle || Shape == ApertureShape.Obround) && Parameters.Length > 1)
                    return Math.Min(Parameters[0], Parameters[1]);
                return Parameters.Length > 0 ? Parameters[0] : 0.1;
            }
        }
    }

    // ----- Primitive: ket qua parse, don vi mm, goc toa do Gerber (Y huong len) -----

    public abstract class GerberPrimitive
    {
        public GerberPolarity Polarity = GerberPolarity.Dark;
        public abstract RectangleD GetBoundsMm();
    }

    /// <summary>D03 - flash aperture tai mot diem.</summary>
    public sealed class FlashPrimitive : GerberPrimitive
    {
        public PointD Position;
        public Aperture Aperture;

        public override RectangleD GetBoundsMm()
        {
            double r = HalfExtent();
            RectangleD b = RectangleD.Empty;
            b.Expand(Position.X - r, Position.Y - r);
            b.Expand(Position.X + r, Position.Y + r);
            return b;
        }

        private double HalfExtent()
        {
            double[] p = Aperture.Parameters;
            switch (Aperture.Shape)
            {
                case ApertureShape.Circle: return p.Length > 0 ? p[0] / 2 : 0;
                case ApertureShape.Rectangle:
                case ApertureShape.Obround: return p.Length > 1 ? Math.Max(p[0], p[1]) / 2 : 0;
                case ApertureShape.Polygon: return p.Length > 0 ? p[0] / 2 : 0;
                default: return EstimateMacroRadius();
            }
        }

        private double EstimateMacroRadius()
        {
            // Suy giam an toan cho macro chua ho tro (FR-006)
            double r = 0;
            foreach (double a in Aperture.MacroArgs) r = Math.Max(r, Math.Abs(a) * Aperture.MacroUnitScale);
            return Math.Max(r, 1.0);
        }
    }

    /// <summary>D01 tuyen tinh (G01) - net ve bang aperture.</summary>
    public sealed class StrokePrimitive : GerberPrimitive
    {
        public PointD Start, End;
        public Aperture Aperture;

        public override RectangleD GetBoundsMm()
        {
            double r = Aperture.StrokeDiameter / 2;
            RectangleD b = RectangleD.Empty;
            b.Expand(Start.X, Start.Y); b.Expand(End.X, End.Y);
            b.Inflate(r);
            return b;
        }
    }

    /// <summary>D01 cung tron (G02/G03).</summary>
    public sealed class ArcPrimitive : GerberPrimitive
    {
        public PointD Start, End, Center;
        public bool Clockwise;                // G02 = CW (trong he toa do Gerber, Y huong len)
        public Aperture Aperture;

        public double Radius
        {
            get
            {
                double dx = Start.X - Center.X, dy = Start.Y - Center.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        public override RectangleD GetBoundsMm()
        {
            // Bao thu: bbox ca duong tron (du cho render; toi uu sau neu can)
            double r = Radius + Aperture.StrokeDiameter / 2;
            RectangleD b = RectangleD.Empty;
            b.Expand(Center.X - r, Center.Y - r);
            b.Expand(Center.X + r, Center.Y + r);
            return b;
        }
    }

    /// <summary>Mot doan trong contour cua region: line hoac arc toi diem End.</summary>
    public struct RegionSegment
    {
        public PointD End;
        public bool IsArc;
        public PointD Center;
        public bool Clockwise;
    }

    /// <summary>Mot contour kin cua region (G36..G37).</summary>
    public sealed class RegionContour
    {
        public PointD Start;
        public List<RegionSegment> Segments = new List<RegionSegment>();
    }

    /// <summary>Vung fill G36/G37 - polygon pour (FR-014).</summary>
    public sealed class RegionPrimitive : GerberPrimitive
    {
        public List<RegionContour> Contours = new List<RegionContour>();

        public override RectangleD GetBoundsMm()
        {
            RectangleD b = RectangleD.Empty;
            foreach (RegionContour c in Contours)
            {
                b.Expand(c.Start.X, c.Start.Y);
                foreach (RegionSegment s in c.Segments)
                {
                    if (s.IsArc)
                    {
                        double dx = s.End.X - s.Center.X, dy = s.End.Y - s.Center.Y;
                        double r = Math.Sqrt(dx * dx + dy * dy);
                        b.Expand(s.Center.X - r, s.Center.Y - r);
                        b.Expand(s.Center.X + r, s.Center.Y + r);
                    }
                    else b.Expand(s.End.X, s.End.Y);
                }
            }
            return b;
        }
    }

    /// <summary>Mot lop Gerber da parse (mot file = mot layer, FR-003).</summary>
    public sealed class GerberLayer
    {
        public string FilePath;
        public string FileName;
        public LayerType Type = LayerType.Unknown;
        public GerberUnit SourceUnit = GerberUnit.Millimeter;
        public List<GerberPrimitive> Primitives = new List<GerberPrimitive>();
        public List<string> Warnings = new List<string>();   // NFR-003
        public bool Visible = true;
        public Color DisplayColor = Color.Gold;              // mau tuy bien tung lop (FR-004)

        public RectangleD GetBoundsMm()
        {
            RectangleD b = RectangleD.Empty;
            foreach (GerberPrimitive p in Primitives) b.Expand(p.GetBoundsMm());
            return b;
        }
    }
}
