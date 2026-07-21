using System;
using System.Drawing;
using System.Threading;

namespace PCM_Inspection_Demo.Matcher
{
    public sealed class PharseCorrMatcher2 : IMatcher
    {
        private sealed class CacheEntry
        {
            public Bitmap BitmapRef;
            public Rectangle Roi;
            public int TargetSize;
            public double[,] Data;
        }

        private readonly object _cacheLock = new object();
        private CacheEntry _sampleCache;
        private CacheEntry _testCache;

        public override string MatcherName => "PharseCorrMatcher2";

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

            var levels = new[]
            {
                new SearchLevel(64, -12.0, 12.0, 2.0, 10),
                new SearchLevel(128, 0, 0, 0.5, 16),
                new SearchLevel(256, 0, 0, 0.2, 30)
            };

            double bestAngle = 0;
            double bestDx = 0;
            double bestDy = 0;
            double bestScore = -1;

            for (var i = 0; i < levels.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                var lvl = levels[i];

                var src = GetProcessed(srcImage, srcRoi, lvl.Size, true);
                var dst = GetProcessed(dstImage, dstRoi, lvl.Size, false);

                double aStart;
                double aEnd;
                if (i == 0)
                {
                    aStart = lvl.AngleStart;
                    aEnd = lvl.AngleEnd;
                }
                else if (i == 1)
                {
                    aStart = bestAngle - 2.0;
                    aEnd = bestAngle + 2.0;
                }
                else
                {
                    aStart = bestAngle - 0.8;
                    aEnd = bestAngle + 0.8;
                }

                var centerDx = i == 0 ? 0 : bestDx * (lvl.Size / (double)levels[i - 1].Size);
                var centerDy = i == 0 ? 0 : bestDy * (lvl.Size / (double)levels[i - 1].Size);

                for (var angle = aStart; angle <= aEnd; angle += lvl.AngleStep)
                {
                    token.ThrowIfCancellationRequested();
                    var rotated = Rotate(src, angle);
                    var score = FindBestShiftAround(rotated, dst, lvl.MaxShift, centerDx, centerDy, out var dx, out var dy);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAngle = angle;
                        bestDx = dx;
                        bestDy = dy;
                    }
                }
            }

            var scaleX = srcRoi.Width / 256d;
            var scaleY = srcRoi.Height / 256d;
            return new MatchResult
            {
                Success = true,
                Dx = bestDx * scaleX,
                Dy = bestDy * scaleY,
                AngleDeg = bestAngle,
                Confidence = Math.Max(0d, Math.Min(1d, bestScore)),
                Message = "Done (Pyramid/Coarse2Fine)"
            };
        }

        private static double[,] GradientAndMask(double[,] src)
        {
            var h = src.GetLength(0);
            var w = src.GetLength(1);
            var dst = new double[h, w];
            var sum = 0d;
            var n = 0;
            for (var y = 1; y < h - 1; y++)
            {
                for (var x = 1; x < w - 1; x++)
                {
                    var gx = -src[y - 1, x - 1] - 2 * src[y, x - 1] - src[y + 1, x - 1] + src[y - 1, x + 1] + 2 * src[y, x + 1] + src[y + 1, x + 1];
                    var gy = -src[y - 1, x - 1] - 2 * src[y - 1, x] - src[y - 1, x + 1] + src[y + 1, x - 1] + 2 * src[y + 1, x] + src[y + 1, x + 1];
                    var mag = Math.Sqrt(gx * gx + gy * gy);
                    dst[y, x] = mag;
                    sum += mag;
                    n++;
                }
            }

            var thr = n > 0 ? sum / n : 0;
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                if (dst[y, x] < thr) dst[y, x] = 0;

            Normalize(dst);
            return dst;
        }

        private double[,] GetProcessed(Bitmap bmp, Rectangle roi, int size, bool sample)
        {
            lock (_cacheLock)
            {
                var cache = sample ? _sampleCache : _testCache;
                if (cache != null && ReferenceEquals(cache.BitmapRef, bmp) && cache.Roi == roi && cache.TargetSize == size && cache.Data != null)
                    return cache.Data;

                var gray = CropGray(bmp, roi);
                var resized = Resize(gray, size, size);
                var filtered = GradientAndMask(resized);

                var updated = new CacheEntry { BitmapRef = bmp, Roi = roi, TargetSize = size, Data = filtered };
                if (sample) _sampleCache = updated;
                else _testCache = updated;
                return filtered;
            }
        }

        private static double[,] CropGray(Bitmap bmp, Rectangle roi)
        {
            var safe = Rectangle.Intersect(new Rectangle(0, 0, bmp.Width, bmp.Height), roi);
            var data = new double[safe.Height, safe.Width];
            for (var y = 0; y < safe.Height; y++)
            for (var x = 0; x < safe.Width; x++)
            {
                var c = bmp.GetPixel(safe.X + x, safe.Y + y);
                data[y, x] = 0.114 * c.B + 0.587 * c.G + 0.299 * c.R;
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
            for (var x = 0; x < w; x++)
            {
                var tx = x - cx;
                var ty = y - cy;
                var sx = cos * tx + sin * ty + cx;
                var sy = -sin * tx + cos * ty + cy;
                dst[y, x] = SampleBilinear(src, sx, sy);
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

        private static double FindBestShiftAround(double[,] src, double[,] dst, int maxShift, double centerDx, double centerDy, out int bestDx, out int bestDy)
        {
            var h = src.GetLength(0);
            var w = src.GetLength(1);
            var best = double.MinValue;
            bestDx = 0;
            bestDy = 0;
            var cdx = (int)Math.Round(centerDx);
            var cdy = (int)Math.Round(centerDy);

            for (var dy = cdy - maxShift; dy <= cdy + maxShift; dy++)
            for (var dx = cdx - maxShift; dx <= cdx + maxShift; dx++)
            {
                var score = CorrAtShift(src, dst, dx, dy, w, h);
                if (score > best)
                {
                    best = score;
                    bestDx = dx;
                    bestDy = dy;
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
            for (var x = x0; x < x1; x++)
            {
                var av = a[y, x];
                var bv = b[y + dy, x + dx];
                sum += av * bv;
                sumA2 += av * av;
                sumB2 += bv * bv;
            }
            var den = Math.Sqrt(sumA2 * sumB2);
            return den < 1e-9 ? -1 : sum / den;
        }

        private struct SearchLevel
        {
            public int Size;
            public double AngleStart;
            public double AngleEnd;
            public double AngleStep;
            public int MaxShift;
            public SearchLevel(int size, double angleStart, double angleEnd, double angleStep, int maxShift)
            {
                Size = size;
                AngleStart = angleStart;
                AngleEnd = angleEnd;
                AngleStep = angleStep;
                MaxShift = maxShift;
            }
        }
    }
}
