using System;
using System.Threading;
using GerberViewer.Stitching.Matching;
using OpenCvSharp;

namespace GerberStitching.Tests.Matching
{
    public static class PharseCorrMatcherTests
    {
        private const double TranslationTolerancePixels = 1.0;
        private const double NoisyTranslationTolerancePixels = 2.0;

        public static void RunAll()
        {
            TranslationX();
            TranslationY();
            NegativeTranslation();
            Noise();
            PartialOverlapRoi();
            InvalidRoi();
            LowTextureRejection();
            Cancellation();
            TransformDirectionIsMovingToReference();
        }

        private static void TranslationX()
        {
            AssertTranslation(8.0, 0.0, TranslationTolerancePixels, false, null, null, "Positive X shift must produce MovingToReference negative X translation.");
        }

        private static void TranslationY()
        {
            AssertTranslation(0.0, 6.0, TranslationTolerancePixels, false, null, null, "Positive Y shift must produce MovingToReference negative Y translation.");
        }

        private static void NegativeTranslation()
        {
            AssertTranslation(-7.0, -5.0, TranslationTolerancePixels, false, null, null, "Negative moving shift must produce positive MovingToReference translation.");
        }

        private static void Noise()
        {
            AssertTranslation(5.0, -4.0, NoisyTranslationTolerancePixels, true, null, null, "Noisy shifted image must remain within phase-correlation tolerance.");
        }

        private static void PartialOverlapRoi()
        {
            var referenceRoi = new Rect(8, 8, 96, 80);
            var movingRoi = new Rect(8, 8, 96, 80);
            AssertTranslation(4.0, 3.0, TranslationTolerancePixels, false, referenceRoi, movingRoi, "Partial overlap ROI must estimate MovingToReference translation.");
        }

        private static void InvalidRoi()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(96, 80))
            using (var moving = Shift(reference, 5.0, 0.0, false))
            using (var matcher = new PharseCorrMatcher())
            {
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, ReferenceRoi = new Rect(90, 0, 32, 32), Purpose = MatchPurpose.SyntheticTest }, CancellationToken.None);
                AssertFalse(result.Success, "Invalid ROI must be rejected.");
                AssertEqual(MatchFailureReason.InvalidRoi, result.FailureReason, "Invalid ROI failure reason must be stable.");
            }
        }

        private static void LowTextureRejection()
        {
            using (var reference = new Mat(64, 64, MatType.CV_8UC1, Scalar.All(12)))
            using (var moving = new Mat(64, 64, MatType.CV_8UC1, Scalar.All(12)))
            using (var matcher = new PharseCorrMatcher())
            {
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, Purpose = MatchPurpose.SyntheticTest }, CancellationToken.None);
                AssertFalse(result.Success, "Low-texture image must be rejected.");
                AssertEqual(MatchFailureReason.LowTexture, result.FailureReason, "Low texture failure reason must be stable.");
            }
        }

        private static void Cancellation()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(96, 80))
            using (var moving = Shift(reference, 3.0, 2.0, false))
            using (var matcher = new PharseCorrMatcher())
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, Purpose = MatchPurpose.SyntheticTest }, cts.Token);
                AssertFalse(result.Success, "Cancelled request must not succeed.");
                AssertEqual(MatchFailureReason.Cancelled, result.FailureReason, "Cancelled request must report Cancelled.");
            }
        }

        private static void TransformDirectionIsMovingToReference()
        {
            var movingShiftX = 9.0;
            var movingShiftY = -3.0;
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(128, 96))
            using (var moving = Shift(reference, movingShiftX, movingShiftY, false))
            using (var matcher = new PharseCorrMatcher())
            {
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, Purpose = MatchPurpose.SyntheticTest }, CancellationToken.None);
                AssertTrue(result.Success, "Direction test match must succeed.");
                AssertNear(-movingShiftX, result.MovingToReferenceTransform[0, 2], TranslationTolerancePixels, "MovingToReferenceTransform X must invert the generated moving image shift.");
                AssertNear(-movingShiftY, result.MovingToReferenceTransform[1, 2], TranslationTolerancePixels, "MovingToReferenceTransform Y must invert the generated moving image shift.");
                AssertNear(0.0, result.RotationDeg, 1e-12, "PharseCorrMatcher must not claim rotation.");
                AssertNear(1.0, result.Scale, 1e-12, "PharseCorrMatcher must not claim scale.");
            }
        }

        private static void AssertTranslation(double shiftX, double shiftY, double tolerance, bool addNoise, Rect? referenceRoi, Rect? movingRoi, string message)
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(128, 96))
            using (var moving = Shift(reference, shiftX, shiftY, addNoise))
            using (var matcher = new PharseCorrMatcher())
            {
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, ReferenceRoi = referenceRoi, MovingRoi = movingRoi, Purpose = MatchPurpose.SyntheticTest, Options = new MatcherOptions { PhaseMinResponse = addNoise ? 0.05 : 0.10, MinTextureStdDev = 1.0 } }, CancellationToken.None);
                AssertTrue(result.Success, message + " Result: " + result.FailureReason + " " + result.FailureMessage);
                AssertNear(-shiftX, result.TranslationX, tolerance, message + " TranslationX mismatch.");
                AssertNear(-shiftY, result.TranslationY, tolerance, message + " TranslationY mismatch.");
                AssertNear(0.0, result.RotationDeg, 1e-12, "Phase correlation must report translation only.");
                AssertNear(1.0, result.Scale, 1e-12, "Phase correlation scale must remain 1.");
                TransformAssert.AreEqual(new[,] { { 1d, 0d, result.TranslationX }, { 0d, 1d, result.TranslationY }, { 0d, 0d, 1d } }, result.MovingToReferenceTransform.ToArray(), 1e-12, "MovingToReferenceTransform must match translation fields.");
            }
        }

        private static Mat Shift(Mat source, double shiftX, double shiftY, bool addNoise)
        {
            var warp = new Mat(2, 3, MatType.CV_64FC1);
            warp.Set<double>(0, 0, 1d); warp.Set<double>(0, 1, 0d); warp.Set<double>(0, 2, shiftX);
            warp.Set<double>(1, 0, 0d); warp.Set<double>(1, 1, 1d); warp.Set<double>(1, 2, shiftY);
            var shifted = new Mat();
            Cv2.WarpAffine(source, shifted, warp, source.Size(), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));
            warp.Dispose();
            if (!addNoise) return shifted;
            var noise = new Mat(source.Rows, source.Cols, MatType.CV_8UC1);
            Cv2.Randn(noise, Scalar.All(0), Scalar.All(4));
            Cv2.Add(shifted, noise, shifted);
            noise.Dispose();
            return shifted;
        }

        private static void AssertTrue(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void AssertFalse(bool value, string message)
        {
            if (value) throw new InvalidOperationException(message);
        }

        private static void AssertNear(double expected, double actual, double tolerance, string message)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual + "; Tolerance: " + tolerance);
        }

        private static void AssertEqual(MatchFailureReason expected, MatchFailureReason actual, string message)
        {
            if (expected != actual) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual);
        }
    }
}
