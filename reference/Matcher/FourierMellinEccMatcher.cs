using System;
using System.Drawing;
using System.Threading;
using OpenCvSharp;

namespace PCM_Inspection_Demo.Matcher
{
    /// <summary>
    /// Combined Matcher: Fourier-Mellin (rough estimation) → ECC (refinement).
    ///
    /// 2-stage pipeline:
    /// Stage 1 — FourierMellinMatcher:
    /// Fast estimation of (θ, scale) and (dx, dy) approximates.
    /// Compensates test image to closely match sample image.
    ///
    /// Stage 2 — EccMatcher (Euclidean):
    /// Precisely refines (dx, dy, θ) on compensated image.
    /// Obtains InitialWarpMatrix from Stage 1 results for faster convergence.
    ///
    /// Final result = synthesis of 2 stages.
    /// This is the recommended pipeline for production problems.
    /// </summary>
    internal class FourierMellinEccMatcher : IMatcher
    {
        public double MinRotScaleConfidence { get; set; } = 0.05;
        public double MinTranslationConfidence { get; set; } = 0.03;

        public MotionTypes EccMotionType { get; set; } = MotionTypes.Euclidean;
        public int EccMaxIterations { get; set; } = 100;
        public double EccEpsilon { get; set; } = 1e-5;

        /// <summary>
        /// If ECC Stage 2 does not converge, it will still return the Stage 1 result.
        /// </summary>
        /// 
        public bool FallbackToStage1 { get; set; } = true;
        /// Base Matcher
        /// 
        public override string MatcherName => "Fourier-Mellin + ECC (2-Stage)";

        public override MatchResult Run(
            Bitmap srcImage, Rectangle srcRoi,
            Bitmap dstImage, Rectangle dstRoi,
            CancellationToken token)
        {
            // Convention: sampleImage is reference, testImage is moving image.
            // Output transform is T(test -> reference).
            if (!MatcherHelper.IsRoiValid(srcRoi, minSize: 32) ||
                !MatcherHelper.IsRoiValid(dstRoi, minSize: 32))
                return Fail("ROI is too small (minimum 32×32 for ECC).");

            var fmMatcher = new FourierMellinMatcher
            {
                MinRotScaleConfidence = MinRotScaleConfidence,
                MinTranslationConfidence = MinTranslationConfidence
            };

            MatchResult stage1 = fmMatcher.Run(srcImage, srcRoi, dstImage, dstRoi, token);
            if (!stage1.Success)
                return Fail($"Stage 1 (Fourier-Mellin) failed: {stage1.Message}");

            token.ThrowIfCancellationRequested();

            // ── Stage 2: Fine-tuning ECC ──
            // Create initial warp matrix from the results of Stage 1
            MatchResult stage2;
            using (Mat initialWarp = BuildEuclideanWarp(stage1.Dx, stage1.Dy, stage1.AngleDeg))
            {
                var eccMatcher = new EccMatcher
                {
                    MotionType = EccMotionType,
                    MaxIterations = EccMaxIterations,
                    Epsilon = EccEpsilon,
                    InitialWarpMatrix = initialWarp,
                };
                stage2 = eccMatcher.Run(srcImage, srcRoi, dstImage, dstRoi, token);
            }

            if (!stage2.Success)
            {
                if (FallbackToStage1)
                {
                    return new MatchResult
                    {
                        Success = stage1.Success,
                        Dx = stage1.Dx,
                        Dy = stage1.Dy,
                        AngleDeg = stage1.AngleDeg,
                        Confidence = stage1.Confidence * 0.8, // penalty because ECC fail
                        Message = $"[Fallback→Stage1] ECC thất bại: {stage2.Message} | Stage1: {stage1.Message}"
                    };
                }
                return Fail($"[Stage2-ECC] {stage2.Message}");
            }

            double combinedConf = Math.Sqrt(stage1.Confidence * stage2.Confidence);

            return new MatchResult
            {
                Success     = true,
                Dx          = stage2.Dx,
                Dy          = stage2.Dy,
                AngleDeg    = stage2.AngleDeg,
                Confidence  = combinedConf,
                Message     =   $"[2-Stage OK] Stage1=({stage1.Dx:F2},{stage1.Dy:F2},θ={stage1.AngleDeg:F2}°) " +
                                $"| Stage2=({stage2.Dx:F2},{stage2.Dy:F2},θ={stage2.AngleDeg:F2}°) " +
                                $"| Conf={combinedConf:F4}"
            };
        }

        /// <summary>
        // Create a Euclidean [2×3] warp matrix from (dx, dy, θ).
        // Use as InitialWarpMatrix for ECC.
        // </summary>
        private static Mat BuildEuclideanWarp(double dx, double dy, double angleDeg)
        {
            double  rad = angleDeg * Math.PI / 180.0;
            float cos = (float)Math.Cos(rad);
            float sin = (float)Math.Sin(rad);

            //  [ cos  -sin  tx ]
            //  [ sin   cos  ty ]
            Mat W = Mat.Eye(2, 3, MatType.CV_32F);
            W.Set<float>(0, 0, cos);
            W.Set<float>(0, 1, -sin);
            W.Set<float>(0, 2, (float)dx);
            W.Set<float>(1, 0, sin);
            W.Set<float>(1, 1, cos);
            W.Set<float>(1, 2, (float)dy);
            return W;
        }
    }
}
