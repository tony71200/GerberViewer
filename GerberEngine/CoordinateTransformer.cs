// GerberEngine/CoordinateTransformer.cs
// FR-008, FR-009: mm <-> pixel theo DPI; anh xa he toa do Gerber (Y len, goc bat ky)
// sang he toa do anh (goc top-left, Y xuong), co le (margin) de anh khong bi cat lem.
using System;
using System.Drawing;

namespace GerberEngine
{
    public sealed class CoordinateTransformer
    {
        private readonly RectangleD _boundsMm;   // bbox noi dung (mm, he Gerber)
        private readonly double _marginMm;
        private readonly double _scale;          // px per mm

        public int Dpi { get; private set; }
        public int PixelWidth { get; private set; }
        public int PixelHeight { get; private set; }
        public RectangleD ContentBoundsMm { get { return _boundsMm; } }

        public CoordinateTransformer(RectangleD boundsMm, int dpi, double marginMm)
        {
            if (boundsMm.IsEmpty) throw new ArgumentException("Bounding box rong - khong co noi dung de render.");
            if (dpi <= 0) throw new ArgumentOutOfRangeException("dpi");
            _boundsMm = boundsMm;
            _marginMm = marginMm;
            Dpi = dpi;
            _scale = dpi / 25.4;
            PixelWidth = Math.Max(1, (int)Math.Ceiling((boundsMm.Width + 2 * marginMm) * _scale));
            PixelHeight = Math.Max(1, (int)Math.Ceiling((boundsMm.Height + 2 * marginMm) * _scale));
        }

        /// <summary>Quy doi do dai mm sang pixel.</summary>
        public float MmToPx(double mm) { return (float)(mm * _scale); }

        /// <summary>Diem Gerber (mm, Y len) -> diem anh (px, Y xuong, goc top-left).</summary>
        public PointF ToPixel(PointD mm)
        {
            float x = (float)((mm.X - _boundsMm.MinX + _marginMm) * _scale);
            float y = (float)((_boundsMm.MaxY - mm.Y + _marginMm) * _scale);
            return new PointF(x, y);
        }

        /// <summary>Nguoc lai: diem anh (px) -> toa do Gerber (mm). Dung hien thi toa do chuot (FR-009).</summary>
        public PointD ToMm(float px, float py)
        {
            double x = px / _scale + _boundsMm.MinX - _marginMm;
            double y = _boundsMm.MaxY + _marginMm - py / _scale;
            return new PointD(x, y);
        }
    }
}
