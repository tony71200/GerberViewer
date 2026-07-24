using OpenCvSharp;
using System;
using System.Drawing;
using System.Threading;


namespace PCM_Inspection_Demo.Matcher
{
    /// <summary>
    /// Enhanced Correlation Coefficient (ECC) Matcher.
    /// Solves: dx, dy, rotation (θ) — and depending on MotionType, scale and shear can be added.
    ///
    /// Advantages: Very accurate, robust against noise, readily available in OpenCV.
    /// Disadvantages: Slower than phase correlation; requires an approximate starting point if the variation is large.
    ///
    /// Suggested combination: Use FourierMellinMatcher to estimate (θ, scale) first,
    // then use ECC Matcher for precise fine-tuning.
    /// </summary>
    public sealed class EccMatcher : IMatcher
    {
        /// <summary>
        /// Type of motion to solve:
        /// Translation → (dx, dy)
        /// Euclidean → (dx, dy, θ) ← default
        /// Affine → (dx, dy, θ, scale, shear)
        /// Homography → full perspective
        /// </summary>
        /// 
        public MotionTypes MotionType { get; set; } = MotionTypes.Euclidean;

        /// <summary>Maximum number of ECC loops.</summary>
        /// 
        public int MaxIterations { get; set; } = 200;
        /// <summary>Convergence threshold (epsilon).</summary>
        /// 
        public double Epsilon { get; set; } = 1e-5;
        /// <summary>
        // Initial warp matrix (optional).
        // null → use identity (assuming small variation).
        // Estimates from FourierMellinMatcher can be passed in for speed.
        // </summary>
        public Mat InitialWarpMatrix { get; set; } = null;

        public override string MatcherName => "ECC (Enhanced Correlation Coefficient)";

        public override MatchResult Run(
            Bitmap srcImage, Rectangle srcRoi,
            Bitmap dstImage, Rectangle dstRoi,
            CancellationToken token)
        {
            // Convention: srcImage is reference, dstImage is moving image.
            // Output transform is T(test -> reference).
            if (!MatcherHelper.IsRoiValid(srcRoi, minSize: 32) ||
                !MatcherHelper.IsRoiValid(dstRoi, minSize: 32))
                return Fail("ROI is too small (ECC needs a minimum of 32x32).");
            Mat srcGray = null, dstGray = null, warpMatrix = null;

            try
            {
                token.ThrowIfCancellationRequested();
                using(Mat srcRoiImage = MatcherHelper.BitmapRoiToMat(srcImage, srcRoi))
                using(Mat dstRoiImage = MatcherHelper.BitmapRoiToMat(dstImage, dstRoi))
                {
                    srcGray = new Mat();
                    dstGray = new Mat();
                    Cv2.CvtColor(srcRoiImage, srcGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.CvtColor(dstRoiImage, dstGray, ColorConversionCodes.BGR2GRAY);

                    token.ThrowIfCancellationRequested();

                    // 2. Initialize the warp matrix
                    bool isHomography = MotionType == MotionTypes.Homography;

                    if (InitialWarpMatrix != null)
                    {
                        warpMatrix = InitialWarpMatrix.Clone();
                    }
                    else
                    {
                        if (isHomography)
                        {
                            warpMatrix = Mat.Eye(3, 3, MatType.CV_32F);
                        }
                        else
                            warpMatrix = Mat.Eye(2, 3, MatType.CV_32F);
                    }

                    // 3. Run ECC
                    var criteria = new TermCriteria(
                        CriteriaTypes.Count | CriteriaTypes.Eps,
                        MaxIterations,
                        Epsilon);

                    double eccScore = Cv2.FindTransformECC(
                        srcGray, dstGray,
                        warpMatrix,
                        MotionType,
                        criteria);

                    token.ThrowIfCancellationRequested();

                    // 4. Decoding the warp matrix
                    var decoded = DecodeWarpMatrix(warpMatrix, MotionType);
                    return new MatchResult
                    {
                        Success = true,
                        Dx = decoded.dx,
                        Dy = decoded.dy,
                        AngleDeg = decoded.angleDeg,
                        Confidence = NormalizeEccScore(eccScore),
                        Message = $"ECC={eccScore:F6} | MotionType={MotionType} " +
                                 $"| Scale=({decoded.scaleX:F4},{decoded.scaleY:F4})"
                    };
                }
            }
            catch (OperationCanceledException)
            {
                return Fail("Cancelled by CancellationToken.");
            }
            catch (OpenCVException ex)
            {
                // ECC usually throws an error when it doesn't converge
                return Fail($"ECC doesn't converge: {ex.Message} " +
                    "— Try increasing MaxIterations or provide a closer InitialWarpMatrix.");
            }
            catch (Exception ex)
            {
                return Fail($"Internal error: { ex.Message}");
            }
            finally
            {
                srcGray?.Dispose();
                dstGray?.Dispose();
                warpMatrix?.Dispose();
            }
        }

        /// <summary>
        /// Decode the warp matrix into geometric parameters.
        /// </summary>
        /// 
        public static (double dx, double dy, double angleDeg, double scaleX, double scaleY)
            DecodeWarpMatrix(Mat W, MotionTypes motionType)
        {
            switch (motionType)
            {
                case MotionTypes.Translation:
                    {
                        // [1 0 tx]
                        // [0 1 ty]
                        double tx = W.At<float>(0, 2);
                        double ty = W.At<float>(1, 2);
                        return (tx, ty, 0.0, 1.0, 1.0);
                    }

                case MotionTypes.Euclidean:
                    {
                        // [cos θ  -sin θ  tx]
                        // [sin θ   cos θ  ty]
                        float cos = W.At<float>(0, 0);
                        float sin = W.At<float>(1, 0);
                        float tx = W.At<float>(0, 2);
                        float ty = W.At<float>(1, 2);

                        double angle = Math.Atan2(sin, cos) * 180.0 / Math.PI;
                        double scale = Math.Sqrt(cos * cos + sin * sin);
                        return (tx, ty, angle, scale, scale);
                    }

                case MotionTypes.Affine:
                    {
                        // [a  b  tx]
                        // [c  d  ty]
                        float a = W.At<float>(0, 0);
                        float b = W.At<float>(0, 1);
                        float c = W.At<float>(1, 0);
                        float d = W.At<float>(1, 1);
                        float tx = W.At<float>(0, 2);
                        float ty = W.At<float>(1, 2);

                        // SVD for separating gear + scale
                        double scaleX = Math.Sqrt(a * a + c * c);
                        double scaleY = Math.Sqrt(b * b + d * d);
                        double angle = Math.Atan2(c, a) * 180.0 / Math.PI;
                        return (tx, ty, angle, scaleX, scaleY);
                    }

                case MotionTypes.Homography:
                    {
                        // The translation is taken from column 3 (approximate only for small homography).
                        float tx = W.At<float>(0, 2);
                        float ty = W.At<float>(1, 2);
                        float a = W.At<float>(0, 0);
                        float c = W.At<float>(1, 0);
                        double angle = Math.Atan2(c, a) * 180.0 / Math.PI;
                        double scale = Math.Sqrt(a * a + c * c);
                        return (tx, ty, angle, scale, scale);
                    }

                default:
                    return (0, 0, 0, 1, 1);
            }
        }

        // <summary>
        /// ECC score is in [-1, 1]. Map to [0, 1] for Confidence.
        /// </summary>
        /// 
        private static double NormalizeEccScore(double ecc)
            => Math.Max(0.0, Math.Min(1.0, (ecc + 1.0) / 2.0));
    }
}
