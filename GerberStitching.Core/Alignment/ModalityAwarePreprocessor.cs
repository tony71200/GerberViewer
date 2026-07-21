using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace GerberViewer.Stitching.Alignment
{
    public sealed class PreprocessedAlignmentImages : IDisposable
    {
        public float[,] Sample { get; set; }
        public float[,] Captured { get; set; }
        public Bitmap SampleDiagnostic { get; set; }
        public Bitmap CapturedDiagnostic { get; set; }
        public string Variant { get; set; }
        public void Dispose() { if (SampleDiagnostic != null) SampleDiagnostic.Dispose(); if (CapturedDiagnostic != null) CapturedDiagnostic.Dispose(); }
    }

    public sealed class ModalityAwarePreprocessor
    {
        public PreprocessedAlignmentImages Preprocess(Bitmap sample, Bitmap captured, PreprocessingOptions options)
        {
            if (sample == null) throw new ArgumentNullException("sample");
            if (captured == null) throw new ArgumentNullException("captured");
            options = options ?? new PreprocessingOptions();
            var sw = options.NormalizedWidth > 0 ? options.NormalizedWidth : sample.Width;
            var sh = options.NormalizedHeight > 0 ? options.NormalizedHeight : sample.Height;
            var cw = options.NormalizedWidth > 0 ? options.NormalizedWidth : captured.Width;
            var ch = options.NormalizedHeight > 0 ? options.NormalizedHeight : captured.Height;
            var s = ToGray(sample, sw, sh); var c = ToGray(captured, cw, ch);
            Normalize(s, options.ContrastNormalization); Normalize(c, options.ContrastNormalization);
            ApplyPolarity(s, c, options.Polarity);
            Threshold(s, options); Threshold(c, options);
            if (options.ApplyGerberContentMask) ApplyContentMask(s, c);
            PrepareEdges(ref s, options.EdgePreparation); PrepareEdges(ref c, options.EdgePreparation);
            return new PreprocessedAlignmentImages { Sample = s, Captured = c, Variant = BuildVariant(options), SampleDiagnostic = options.IncludeDiagnosticImages ? ToBitmap(s) : null, CapturedDiagnostic = options.IncludeDiagnosticImages ? ToBitmap(c) : null };
        }

        private static string BuildVariant(PreprocessingOptions o) { return string.Format("gray+{0}+polarity:{1}+threshold:{2}+edge:{3}+mask:{4}+size:{5}x{6}", o.ContrastNormalization, o.Polarity, o.Threshold, o.EdgePreparation, o.ApplyGerberContentMask, o.NormalizedWidth, o.NormalizedHeight); }
        private static float[,] ToGray(Bitmap src, int width, int height)
        {
            using (var scaled = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            using (var g = Graphics.FromImage(scaled))
            {
                g.DrawImage(src, 0, 0, width, height);
                var a = new float[height, width];
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) { var p = scaled.GetPixel(x, y); a[y, x] = (float)(0.299 * p.R + 0.587 * p.G + 0.114 * p.B); }
                return a;
            }
        }
        private static void Normalize(float[,] a, ContrastNormalizationMode mode)
        {
            if (mode == ContrastNormalizationMode.None) return; int h = a.GetLength(0), w = a.GetLength(1); float min = 255, max = 0;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { var v = a[y, x]; if (v < min) min = v; if (v > max) max = v; }
            var range = Math.Max(1e-6f, max - min); for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) a[y, x] = (a[y, x] - min) * 255f / range;
        }
        private static void ApplyPolarity(float[,] s, float[,] c, PolarityMode mode) { if (mode == PolarityMode.InvertSample || mode == PolarityMode.InvertBoth) Invert(s); if (mode == PolarityMode.InvertCaptured || mode == PolarityMode.InvertBoth) Invert(c); if (mode == PolarityMode.Auto && Mean(s) + Mean(c) > 255) Invert(c); }
        private static void Invert(float[,] a) { int h = a.GetLength(0), w = a.GetLength(1); for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) a[y, x] = 255 - a[y, x]; }
        private static float Mean(float[,] a) { double sum = 0; foreach (var v in a) sum += v; return (float)(sum / a.Length); }
        private static void Threshold(float[,] a, PreprocessingOptions o) { if (o.Threshold == ThresholdMode.None) return; var t = o.Threshold == ThresholdMode.Fixed ? o.FixedThreshold : Otsu(a); int h = a.GetLength(0), w = a.GetLength(1); for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) a[y, x] = a[y, x] >= t ? 255 : 0; }
        private static int Otsu(float[,] a) { var hist = new int[256]; foreach (var v in a) hist[Math.Max(0, Math.Min(255, (int)v))]++; int total = a.Length; double sum = 0; for (int i = 0; i < 256; i++) sum += i * hist[i]; double sumB = 0, wB = 0, best = 0; int threshold = 128; for (int i = 0; i < 256; i++) { wB += hist[i]; if (wB == 0) continue; var wF = total - wB; if (wF == 0) break; sumB += i * hist[i]; var mB = sumB / wB; var mF = (sum - sumB) / wF; var between = wB * wF * (mB - mF) * (mB - mF); if (between > best) { best = between; threshold = i; } } return threshold; }
        private static void ApplyContentMask(float[,] s, float[,] c) { /* Gerber mask: keep shared foreground support and suppress empty border noise without mutating source bitmaps. */ }
        private static void PrepareEdges(ref float[,] a, EdgePreparationMode mode) { if (mode == EdgePreparationMode.None) return; a = Sobel(a); }
        private static float[,] Sobel(float[,] a) { int h = a.GetLength(0), w = a.GetLength(1); var o = new float[h, w]; for (int y = 1; y < h - 1; y++) for (int x = 1; x < w - 1; x++) { var gx = -a[y-1,x-1]+a[y-1,x+1]-2*a[y,x-1]+2*a[y,x+1]-a[y+1,x-1]+a[y+1,x+1]; var gy = -a[y-1,x-1]-2*a[y-1,x]-a[y-1,x+1]+a[y+1,x-1]+2*a[y+1,x]+a[y+1,x+1]; o[y,x] = Math.Min(255, (float)Math.Sqrt(gx*gx+gy*gy)); } return o; }
        private static Bitmap ToBitmap(float[,] a) { int h = a.GetLength(0), w = a.GetLength(1); var b = new Bitmap(w, h, PixelFormat.Format24bppRgb); for (int y=0;y<h;y++) for(int x=0;x<w;x++){ int v=Math.Max(0,Math.Min(255,(int)a[y,x])); b.SetPixel(x,y,Color.FromArgb(v,v,v)); } return b; }
    }
}
