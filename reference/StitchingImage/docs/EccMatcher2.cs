using OpenCvSharp;
using System;
using System.Drawing;
using System.Threading;

namespace PCM_Inspection_Demo.Matcher
{
    /// <summary>
    /// ECC matcher variant optimized for large ROI:
    /// - Run ECC on a downscaled working image to reduce compute time.
    /// - Upscale translation parameters back to full resolution.
    /// - Optionally run a short refinement on full resolution.
    /// Convention: output is T(test -> reference).
    /// </summary>
    public sealed class EccMatcher2 : BaseMatcher
    {
        public MotionTypes MotionType { get; set; } = MotionTypes.Euclidean;
        public int MaxWorkingEdge { get; set; } = 640;
        public int CoarseIterations { get; set; } = 80;
        public int RefineIterations { get; set; } = 35;
        public double Epsilon { get; set; } = 1e-5;
        public bool EnableFullResolutionRefine { get; set; } = true;

        public override string MatcherName => "ECC-FAST (Large ROI)";

        public override MatchResult Run(
            Bitmap srcImage, Rectangle srcRoi,
            Bitmap dstImage, Rectangle dstRoi,
            CancellationToken token)
        {
            // Convention: sampleImage is reference, testImage is moving image.
            // Output transform is T(test -> reference).
            if (!MatcherHelper.IsRoiValid(srcRoi, minSize: 32) ||
                !MatcherHelper.IsRoiValid(dstRoi, minSize: 32))
                return Fail("ROI is too small (ECC-FAST needs a minimum of 32x32).");

            Mat srcGray = null;
            Mat dstGray = null;
            Mat srcWork = null;
            Mat dstWork = null;
            Mat warpWork = null;
            Mat warpFinal = null;
            try
            {
                token.ThrowIfCancellationRequested();
                using (Mat srcRoiImage = MatcherHelper.BitmapRoiToMat(srcImage, srcRoi))
                using (Mat dstRoiImage = MatcherHelper.BitmapRoiToMat(dstImage, dstRoi))
                {
                    srcGray = new Mat();
                    dstGray = new Mat();
                    Cv2.CvtColor(srcRoiImage, srcGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.CvtColor(dstRoiImage, dstGray, ColorConversionCodes.BGR2GRAY);
                    srcGray.ConvertTo(srcGray, MatType.CV_32F, 1.0 / 255.0);
                    dstGray.ConvertTo(dstGray, MatType.CV_32F, 1.0 / 255.0);
                }

                token.ThrowIfCancellationRequested();
                double scaleX;
                double scaleY;
                PrepareWorkingPair(srcGray, dstGray, out srcWork, out dstWork, out scaleX, out scaleY);

                warpWork = CreateIdentityWarp(MotionType);
                var coarseCriteria = new TermCriteria(CriteriaTypes.Count | CriteriaTypes.Eps, Math.Max(10, CoarseIterations), Epsilon);
                double coarseEcc = Cv2.FindTransformECC(srcWork, dstWork, warpWork, MotionType, coarseCriteria);

                warpFinal = warpWork.Clone();
                ScaleWarpTranslation(warpFinal, scaleX <= 0 ? 1.0 : (1.0 / scaleX), scaleY <= 0 ? 1.0 : (1.0 / scaleY), MotionType);

                double finalEcc = coarseEcc;
                if (EnableFullResolutionRefine && RefineIterations > 0 && (scaleX < 0.999 || scaleY < 0.999))
                {
                    token.ThrowIfCancellationRequested();
                    var refineCriteria = new TermCriteria(CriteriaTypes.Count | CriteriaTypes.Eps, Math.Max(5, RefineIterations), Epsilon);
                    finalEcc = Cv2.FindTransformECC(srcGray, dstGray, warpFinal, MotionType, refineCriteria);
                }

                var decoded = EccMatcher.DecodeWarpMatrix(warpFinal, MotionType);
                return new MatchResult
                {
                    Success = true,
                    Dx = decoded.dx,
                    Dy = decoded.dy,
                    AngleDeg = decoded.angleDeg,
                    Confidence = NormalizeEccScore(finalEcc),
                    Message = $"ECC-FAST={finalEcc:F6} | coarseScale=({scaleX:F3},{scaleY:F3}) | MotionType={MotionType}"
                };
            }
            catch (OperationCanceledException)
            {
                return Fail("Cancelled by CancellationToken.");
            }
            catch (OpenCVException ex)
            {
                return Fail($"ECC-FAST doesn't converge: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Fail($"Internal error: {ex.Message}");
            }
            finally
            {
                srcGray?.Dispose();
                dstGray?.Dispose();
                srcWork?.Dispose();
                dstWork?.Dispose();
                warpWork?.Dispose();
                warpFinal?.Dispose();
            }
        }

        private Mat CreateIdentityWarp(MotionTypes motionType)
        {
            return motionType == MotionTypes.Homography
                ? Mat.Eye(3, 3, MatType.CV_32F)
                : Mat.Eye(2, 3, MatType.CV_32F);
        }

        private void PrepareWorkingPair(Mat srcGray, Mat dstGray, out Mat srcWork, out Mat dstWork, out double scaleX, out double scaleY)
        {
            srcWork = srcGray.Clone();
            dstWork = new Mat();
            if (dstGray.Size() == srcGray.Size())
                dstGray.CopyTo(dstWork);
            else
                Cv2.Resize(dstGray, dstWork, srcGray.Size(), 0, 0, InterpolationFlags.Linear);

            var maxEdge = Math.Max(srcWork.Rows, srcWork.Cols);
            if (maxEdge <= MaxWorkingEdge)
            {
                scaleX = 1.0;
                scaleY = 1.0;
                return;
            }

            var uniformScale = MaxWorkingEdge / (double)Math.Max(1, maxEdge);
            var targetW = Math.Max(32, (int)Math.Round(srcWork.Cols * uniformScale));
            var targetH = Math.Max(32, (int)Math.Round(srcWork.Rows * uniformScale));
            var target = new OpenCvSharp.Size(targetW, targetH);

            var srcSmall = new Mat();
            var dstSmall = new Mat();
            Cv2.Resize(srcWork, srcSmall, target, 0, 0, InterpolationFlags.Area);
            Cv2.Resize(dstWork, dstSmall, target, 0, 0, InterpolationFlags.Area);

            srcWork.Dispose();
            dstWork.Dispose();
            srcWork = srcSmall;
            dstWork = dstSmall;
            scaleX = targetW / (double)Math.Max(1, srcGray.Cols);
            scaleY = targetH / (double)Math.Max(1, srcGray.Rows);
        }

        private static void ScaleWarpTranslation(Mat warp, double txScale, double tyScale, MotionTypes motionType)
        {
            if (warp == null) return;
            if (motionType == MotionTypes.Homography)
            {
                warp.Set(0, 2, (float)(warp.At<float>(0, 2) * txScale));
                warp.Set(1, 2, (float)(warp.At<float>(1, 2) * tyScale));
                return;
            }

            if (warp.Rows >= 2 && warp.Cols >= 3)
            {
                warp.Set(0, 2, (float)(warp.At<float>(0, 2) * txScale));
                warp.Set(1, 2, (float)(warp.At<float>(1, 2) * tyScale));
            }
        }

        private static double NormalizeEccScore(double ecc)
            => Math.Max(0.0, Math.Min(1.0, (ecc + 1.0) / 2.0));
    }
}
