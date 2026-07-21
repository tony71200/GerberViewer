using System;
using System.Drawing;
using System.Threading;

namespace PCM_Inspection_Demo.Matcher
{
    public sealed class PharseCorrMatcher : IMatcher
    {
        public override string MatcherName => "PharseCorrMatcher";

        public override MatchResult Run(
            Bitmap srcImage, Rectangle srcRoi,
            Bitmap dstImage, Rectangle dstRoi,
            CancellationToken token)
        {
            // Convention: sampleImage is reference, testImage is moving image.
            // Output transform is T(test -> reference).
            if (srcImage == null || dstImage == null)
                return Fail("Missing input image.");
            if (srcRoi.Width < 8 || srcRoi.Height < 8 || dstRoi.Width < 8 || dstRoi.Height < 8)
                return Fail("ROI is too small.");

            var sampleGray = CropGray(srcImage, srcRoi);
            var testGray = CropGray(dstImage, dstRoi);
            token.ThrowIfCancellationRequested();

            const int targetW = 256;
            const int targetH = 256;
            sampleGray = Resize(sampleGray, targetW, targetH);
            testGray = Resize(testGray, targetW, targetH);

            var bestScore = double.MinValue;
            double bestDx = 0, bestDy = 0, bestAngle = 0;

            for (var angle = -12.0; angle <= 12.0; angle += 1.0)
            {
                token.ThrowIfCancellationRequested();
                var rotated = Rotate(sampleGray, angle);
                var score = FindBestShift(rotated, testGray, 36, out var dx, out var dy);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDx = dx;
                    bestDy = dy;
                    bestAngle = angle;
                }
            }

            var scaleX = srcRoi.Width / (double)targetW;
            var scaleY = srcRoi.Height / (double)targetH;

            return new MatchResult
            {
                Success = true,
                Dx = bestDx * scaleX,
                Dy = bestDy * scaleY,
                AngleDeg = bestAngle,
                Confidence = Math.Max(0d, Math.Min(1d, bestScore)),
                Message = "Done"
            };
        }

        private static double[,] CropGray(Bitmap bmp, Rectangle roi)
        {
            var safe = Rectangle.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height), roi);
            var data = new double[safe.Height, safe.Width];
            for (var y = 0; y < safe.Height; y++)
            {
                for (var x = 0; x < safe.Width; x++)
                {
                    var c = bmp.GetPixel(safe.X + x, safe.Y + y);
                    data[y, x] = 0.114 * c.B + 0.587 * c.G + 0.299 * c.R;
                }
            }
            return data;
        }

        private static double[,] Resize(double[,] src, int dstW, int dstH)
        {
            var srcH = src.GetLength(0);
            var srcW = src.GetLength(1);
            var dst = new double[dstH, dstW];
            for (var y = 0; y < dstH; y++)
            {
                var sy = y * (srcH - 1d) / Math.Max(1, dstH - 1);
                var y0 = (int)Math.Floor(sy);
                var y1 = Math.Min(srcH - 1, y0 + 1);
                var fy = sy - y0;
                for (var x = 0; x < dstW; x++)
                {
                    var sx = x * (srcW - 1d) / Math.Max(1, dstW - 1);
                    var x0 = (int)Math.Floor(sx);
                    var x1 = Math.Min(srcW - 1, x0 + 1);
                    var fx = sx - x0;
                    var v00 = src[y0, x0];
                    var v10 = src[y0, x1];
                    var v01 = src[y1, x0];
                    var v11 = src[y1, x1];
                    dst[y, x] = v00 * (1 - fx) * (1 - fy) + v10 * fx * (1 - fy) + v01 * (1 - fx) * fy + v11 * fx * fy;
                }
            }
            Normalize(dst);
            return dst;
        }

        private static void Normalize(double[,] img)
        {
            var h = img.GetLength(0);
            var w = img.GetLength(1);
            var sum = 0d;
            var sum2 = 0d;
            var n = (double)(h * w);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                sum += img[y, x];
                sum2 += img[y, x] * img[y, x];
            }

            var mean = sum / Math.Max(1d, n);
            var var = Math.Max(1e-9, sum2 / Math.Max(1d, n) - mean * mean);
            var std = Math.Sqrt(var);

            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                img[y, x] = (img[y, x] - mean) / std;
        }

        private static double[,] Rotate(double[,] src, double angleDeg)
        {
            var h = src.GetLength(0);
            var w = src.GetLength(1);
            var dst = new double[h, w];
            var rad = angleDeg * Math.PI / 180.0;
            var cos = Math.Cos(rad);
            var sin = Math.Sin(rad);
            var cx = (w - 1) / 2.0;
            var cy = (h - 1) / 2.0;

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var tx = x - cx;
                    var ty = y - cy;
                    var sx = cos * tx + sin * ty + cx;
                    var sy = -sin * tx + cos * ty + cy;
                    dst[y, x] = SampleBilinear(src, sx, sy);
                }
            }
            Normalize(dst);
            return dst;
        }

        private static double SampleBilinear(double[,] src, double x, double y)
        {
            var h = src.GetLength(0);
            var w = src.GetLength(1);
            if (x < 0 || y < 0 || x >= w - 1 || y >= h - 1) return 0;

            var x0 = (int)x;
            var y0 = (int)y;
            var x1 = x0 + 1;
            var y1 = y0 + 1;
            var fx = x - x0;
            var fy = y - y0;

            var v00 = src[y0, x0];
            var v10 = src[y0, x1];
            var v01 = src[y1, x0];
            var v11 = src[y1, x1];
            return v00 * (1 - fx) * (1 - fy) + v10 * fx * (1 - fy) + v01 * (1 - fx) * fy + v11 * fx * fy;
        }

        private static double FindBestShift(double[,] src, double[,] dst, int maxShift, out int bestDx, out int bestDy)
        {
            var h = src.GetLength(0);
            var w = src.GetLength(1);
            var best = double.MinValue;
            bestDx = 0;
            bestDy = 0;

            for (var dy = -maxShift; dy <= maxShift; dy++)
            {
                for (var dx = -maxShift; dx <= maxShift; dx++)
                {
                    var score = CorrAtShift(src, dst, dx, dy, w, h);
                    if (score > best)
                    {
                        best = score;
                        bestDx = dx;
                        bestDy = dy;
                    }
                }
            }

            return (best + 1.0) / 2.0;
        }

        private static double CorrAtShift(double[,] a, double[,] b, int dx, int dy, int w, int h)
        {
            var x0 = Math.Max(0, -dx);
            var y0 = Math.Max(0, -dy);
            var x1 = Math.Min(w, w - dx);
            var y1 = Math.Min(h, h - dy);
            if (x1 <= x0 || y1 <= y0) return -1;

            var sum = 0d;
            var sumA2 = 0d;
            var sumB2 = 0d;
            for (var y = y0; y < y1; y++)
            {
                for (var x = x0; x < x1; x++)
                {
                    var av = a[y, x];
                    var bv = b[y + dy, x + dx];
                    sum += av * bv;
                    sumA2 += av * av;
                    sumB2 += bv * bv;
                }
            }
            var den = Math.Sqrt(sumA2 * sumB2);
            return den < 1e-9 ? -1 : sum / den;
        }
    }
}
