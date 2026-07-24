using System;
using System.Drawing;
using System.Threading;
using OpenCvSharp;

namespace PCM_Inspection_Demo.Matcher
{
    /// <summary> 
    /// Fourier-Mellin Transform Matcher. 
    /// Solve: dx, dy, rotation (θ), scale (s). 
    /// 
    /// Pipeline: 
    /// 1. FFT → Log-Magnitude Spectrum 
    /// 2. Log-Polar Transform → Phase Correlation → (θ, scale) 
    /// 3. Compensate rotation + scale on the test image 
    /// 4. Pure Phase Correlation → (dx, dy) 
    /// </summary>
    public sealed class FourierMellinMatcher : IMatcher
    {
        // ─── Configuration ───
        /// <summary>
        /// Minimum confidence threshold for the Log-Polar phase correlation step.
        /// Below this threshold → rotation/scale is considered undetectable.
        /// </summary>
        /// 
        public double MinRotScaleConfidence { get; set; } = 0.05;

        /// <summary>
        /// Minimum confidence threshold for the translation phase correlation step.
        /// </summary>
        /// 
        public double MinTranslationConfidence { get; set; } = 0.05;

        // ─── Implementation ───
        public override string MatcherName => "Fourier-Mellin";

        public override MatchResult Run(
            Bitmap srcImage, Rectangle srcRoi,
            Bitmap dstImage, Rectangle dstRoi,
            CancellationToken token)
        {
            // Convention: sampleImage is reference, testImage is moving image.
            // Output transform is T(test -> reference).
            // 1. Validate ROI
            if (!MatcherHelper.IsRoiValid(srcRoi) || !MatcherHelper.IsRoiValid(dstRoi))
                return Fail("ROI is too small (minimum 16×16).");
            Mat srcFloat = null, dstFloat = null,
                specSrc = null, specDst = null,
                lpSrc = null, lpDst = null,
                compensated = null, hanningWindow = null;

            try
            {
                token.ThrowIfCancellationRequested();
                // 2. Get ROI → float grayscale
                srcFloat = MatcherHelper.BitmapRoiToFloatGray(srcImage, srcRoi);
                dstFloat = MatcherHelper.BitmapRoiToFloatGray(dstImage, dstRoi);

                token.ThrowIfCancellationRequested();
                // 3. Magnitude Spectrum
                specSrc = MatcherHelper.GetMagnitudeSpectrum(srcFloat);
                specDst = MatcherHelper.GetMagnitudeSpectrum(dstFloat);

                token.ThrowIfCancellationRequested();

                // 4. Log-Polar Transform
                // M: scaling factor to map the logarithmic radius to the image width
                double radius = specSrc.Cols / 2.0;
                double M = specSrc.Cols / Math.Log(radius);
                var center = new Point2f(specSrc.Cols / 2f, specSrc.Rows / 2f);
                var flags = InterpolationFlags.Linear | InterpolationFlags.WarpFillOutliers;

                lpSrc = new Mat();
                lpDst = new Mat();
                Cv2.LogPolar(specSrc, lpSrc, center, M, flags);
                Cv2.LogPolar(specDst, lpDst, center, M, flags);

                token.ThrowIfCancellationRequested();
                hanningWindow = new Mat();
                Cv2.CreateHanningWindow(hanningWindow, lpSrc.Size(), MatType.CV_32F);

                // 5. Phase Correlation on Log-Polar → (Δθ, Δscale)
                if (lpSrc.Size() != lpDst.Size())
                {
                    var resized = new Mat();
                    Cv2.Resize(lpDst, resized, lpSrc.Size(), 0, 0, InterpolationFlags.Linear);
                    lpDst.Dispose();
                    lpDst = resized;
                }

                Point2d rotScaleShift = Cv2.PhaseCorrelate(lpSrc, lpDst, hanningWindow, out double rsConf);
                hanningWindow?.Dispose();
                hanningWindow = null;
                if (rsConf < MinRotScaleConfidence)
                {
                    return Fail($"Confidence Log-Polar is too low ({rsConf:F4}). " +
                                    "Images may be too different or ROI is not suitable.");
                }
                // rotScaleShift.Y → angle (fraction of image height = 360°) 
                // rotScaleShift.X → log(scale) scaled by M
                double angleDeg = rotScaleShift.Y * 360.0 / lpSrc.Rows;
                double scale = Math.Exp(rotScaleShift.X / M);

                // Set reasonable scale limits (avoid out-of-range).
                scale = Math.Max(0.1, Math.Min(scale, 10.0));
                token.ThrowIfCancellationRequested();

                // 6. Compare rotation and scale to test image
                compensated = MatcherHelper.CompensateTransform(dstFloat, angleDeg, scale, srcFloat.Size());

                token.ThrowIfCancellationRequested();
                if (srcFloat.Size() != compensated.Size())
                {
                    var resized = new Mat();
                    Cv2.Resize(compensated, resized, srcFloat.Size(), 0, 0, InterpolationFlags.Linear);
                    compensated.Dispose();
                    compensated = resized;
                }
                hanningWindow = new Mat();
                Cv2.CreateHanningWindow(hanningWindow, srcFloat.Size(), MatType.CV_32F);
                // 7. Phase Correlation on compensated image → (dx, dy)
                Point2d translation = Cv2.PhaseCorrelate(srcFloat, compensated, hanningWindow, out double trConf);

                if (trConf < MinTranslationConfidence)
                    return Fail($"Confidence translation is too low ({trConf:F4}) after compensation.");

                // Overall Confidence: Geometric average of 2 steps
                double combinedConfidence = Math.Sqrt(rsConf * trConf);
                return new MatchResult
                {
                    Success = true,
                    Dx = translation.X,
                    Dy = translation.Y,
                    AngleDeg = angleDeg,
                    Confidence = combinedConfidence,
                    Message = $"Scale={scale:F4} | RotConf={rsConf:F4} | TransConf={trConf:F4}"
                };
            }
            catch (OperationCanceledException)
            {
                return Fail("Cancellation has been cancelled by CancellationToken.");
            }
            catch (Exception ex)
            {
                return Fail($"Error: {ex.Message}");
            }
            finally
            {
                srcFloat?.Dispose();
                dstFloat?.Dispose();
                specSrc?.Dispose();
                specDst?.Dispose();
                lpSrc?.Dispose();
                lpDst?.Dispose();
                compensated?.Dispose();
                hanningWindow?.Dispose();
            }
        }
    }
}
