using System;
using System.Collections.Generic;
using System.Drawing;
using GerberViewer.Stitching.Imaging.ImageInterop;
using OpenCvSharp;

namespace GerberViewer.Stitching.Alignment
{
    public sealed class PreprocessedAlignmentImages : IDisposable
    {
        public Mat Sample { get; set; }
        public Mat Captured { get; set; }
        public Bitmap SampleDiagnostic { get; set; }
        public Bitmap CapturedDiagnostic { get; set; }
        public string Variant { get; set; }

        public void Dispose()
        {
            if (Sample != null) Sample.Dispose();
            if (Captured != null) Captured.Dispose();
            if (SampleDiagnostic != null) SampleDiagnostic.Dispose();
            if (CapturedDiagnostic != null) CapturedDiagnostic.Dispose();
        }
    }

    public sealed class ModalityAwarePreprocessor
    {
        private readonly IImageInteropService _imageInterop;

        public ModalityAwarePreprocessor() : this(new ImageInteropService())
        {
        }

        public ModalityAwarePreprocessor(IImageInteropService imageInterop)
        {
            if (imageInterop == null) throw new ArgumentNullException("imageInterop");
            _imageInterop = imageInterop;
        }

        public IList<PreprocessedAlignmentImages> PreprocessCandidates(Bitmap sample, Bitmap captured, PreprocessingOptions options)
        {
            if (sample == null) throw new ArgumentNullException("sample");
            if (captured == null) throw new ArgumentNullException("captured");
            options = options ?? new PreprocessingOptions();
            var candidates = new List<PreprocessedAlignmentImages>();
            if (options.Polarity == PolarityMode.Auto)
            {
                candidates.Add(CreateCandidate(sample, captured, options, PolarityMode.AsIs, "polarity:auto/as-is"));
                candidates.Add(CreateCandidate(sample, captured, options, PolarityMode.InvertCaptured, "polarity:auto/invert-captured"));
                return candidates;
            }

            candidates.Add(CreateCandidate(sample, captured, options, options.Polarity, "polarity:" + options.Polarity));
            return candidates;
        }

        private PreprocessedAlignmentImages CreateCandidate(Bitmap sample, Bitmap captured, PreprocessingOptions options, PolarityMode polarity, string polarityVariant)
        {
            Mat sampleMat = null;
            Mat capturedMat = null;
            try
            {
                sampleMat = _imageInterop.ToMatCopy(sample, InteropPixelFormat.Mono8);
                capturedMat = _imageInterop.ToMatCopy(captured, InteropPixelFormat.Mono8);
                ResizeIfRequested(ref sampleMat, options.NormalizedWidth, options.NormalizedHeight);
                ResizeIfRequested(ref capturedMat, options.NormalizedWidth, options.NormalizedHeight);
                Normalize(sampleMat, options.ContrastNormalization);
                Normalize(capturedMat, options.ContrastNormalization);
                ApplyPolarity(sampleMat, capturedMat, polarity);
                Threshold(sampleMat, options);
                Threshold(capturedMat, options);
                if (options.ApplyGerberContentMask) ApplyContentMask(sampleMat, capturedMat);
                PrepareEdges(ref sampleMat, options.EdgePreparation);
                PrepareEdges(ref capturedMat, options.EdgePreparation);

                var result = new PreprocessedAlignmentImages
                {
                    Sample = sampleMat,
                    Captured = capturedMat,
                    Variant = BuildVariant(options, polarityVariant)
                };
                sampleMat = null;
                capturedMat = null;
                if (options.IncludeDiagnosticImages)
                {
                    result.SampleDiagnostic = _imageInterop.ToBitmapCopy(result.Sample);
                    result.CapturedDiagnostic = _imageInterop.ToBitmapCopy(result.Captured);
                }
                return result;
            }
            finally
            {
                if (sampleMat != null) sampleMat.Dispose();
                if (capturedMat != null) capturedMat.Dispose();
            }
        }

        private static string BuildVariant(PreprocessingOptions o, string polarityVariant)
        {
            return string.Format("opencv-gray+{0}+{1}+threshold:{2}+edge:{3}+mask:{4}+size:{5}x{6}", o.ContrastNormalization, polarityVariant, o.Threshold, o.EdgePreparation, o.ApplyGerberContentMask, o.NormalizedWidth, o.NormalizedHeight);
        }

        private static void ResizeIfRequested(ref Mat image, int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            if (image.Cols == width && image.Rows == height) return;
            var resized = new Mat();
            Cv2.Resize(image, resized, new OpenCvSharp.Size(width, height), 0, 0, InterpolationFlags.Area);
            image.Dispose();
            image = resized;
        }

        private static void Normalize(Mat image, ContrastNormalizationMode mode)
        {
            if (mode == ContrastNormalizationMode.None) return;
            Cv2.Normalize(image, image, 0, 255, NormTypes.MinMax);
        }

        private static void ApplyPolarity(Mat sample, Mat captured, PolarityMode mode)
        {
            if (mode == PolarityMode.InvertSample || mode == PolarityMode.InvertBoth) Cv2.BitwiseNot(sample, sample);
            if (mode == PolarityMode.InvertCaptured || mode == PolarityMode.InvertBoth) Cv2.BitwiseNot(captured, captured);
        }

        private static void Threshold(Mat image, PreprocessingOptions options)
        {
            if (options.Threshold == ThresholdMode.None) return;
            if (options.Threshold == ThresholdMode.Fixed)
            {
                Cv2.Threshold(image, image, options.FixedThreshold, 255, ThresholdTypes.Binary);
                return;
            }
            if (options.Threshold == ThresholdMode.Adaptive)
            {
                var blockSize = Math.Max(3, options.AdaptiveRadius | 1);
                using (var adaptive = new Mat())
                {
                    Cv2.AdaptiveThreshold(image, adaptive, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, blockSize, 2);
                    adaptive.CopyTo(image);
                }
                return;
            }
            Cv2.Threshold(image, image, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
        }

        private static void ApplyContentMask(Mat sample, Mat captured)
        {
            using (var sampleMask = new Mat())
            using (var capturedMask = new Mat())
            {
                Cv2.Threshold(sample, sampleMask, 0, 255, ThresholdTypes.Binary);
                Cv2.Threshold(captured, capturedMask, 0, 255, ThresholdTypes.Binary);
                Cv2.BitwiseAnd(sample, sampleMask, sample);
                Cv2.BitwiseAnd(captured, capturedMask, captured);
            }
        }

        private static void PrepareEdges(ref Mat image, EdgePreparationMode mode)
        {
            if (mode == EdgePreparationMode.None) return;
            if (mode == EdgePreparationMode.Canny)
            {
                var canny = new Mat();
                Cv2.Canny(image, canny, 50, 150);
                image.Dispose();
                image = canny;
                return;
            }

            var gradX = new Mat();
            var gradY = new Mat();
            var absX = new Mat();
            var absY = new Mat();
            var sobel = new Mat();
            try
            {
                Cv2.Sobel(image, gradX, MatType.CV_16SC1, 1, 0);
                Cv2.Sobel(image, gradY, MatType.CV_16SC1, 0, 1);
                Cv2.ConvertScaleAbs(gradX, absX);
                Cv2.ConvertScaleAbs(gradY, absY);
                Cv2.AddWeighted(absX, 0.5, absY, 0.5, 0, sobel);
                image.Dispose();
                image = sobel;
                sobel = null;
            }
            finally
            {
                gradX.Dispose();
                gradY.Dispose();
                absX.Dispose();
                absY.Dispose();
                if (sobel != null) sobel.Dispose();
            }
        }
    }
}
