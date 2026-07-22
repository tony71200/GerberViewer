using System;
using System.Threading;
using GerberViewer.Stitching.Matching;
using GerberViewer.Stitching.Transforms;
using OpenCvSharp;

namespace GerberStitching.Tests.Matching
{
    public static class EccMatcherTests
    {
        private const double TranslationTolerancePixels = 1.5;
        private const double RotationToleranceDeg = 0.3;

        public static void RunAll()
        {
            Translation();
            Rotation();
            TranslationAndRotation();
            InitialTransform();
            IlluminationGradient();
            Noise();
            InsufficientOverlap();
            NonConvergence();
            Cancellation();
            TransformDirectionIsMovingToReference();
        }

        private static void Translation()
        {
            AssertEcc(7.0, -5.0, 0.0, false, false, null, "ECC translation must be recovered.");
        }

        private static void Rotation()
        {
            AssertEcc(0.0, 0.0, 4.0, false, false, null, "ECC rotation must be recovered.");
        }

        private static void TranslationAndRotation()
        {
            AssertEcc(6.0, 4.0, -3.5, false, false, null, "ECC translation + rotation must be recovered.");
        }

        private static void InitialTransform()
        {
            var referenceToMoving = ReferenceToMoving(9.0, -6.0, 3.0, new Size(128, 96));
            var initialMovingToReference = new Transform2D(referenceToMoving).Invert();
            AssertEcc(9.0, -6.0, 3.0, false, false, initialMovingToReference, "ECC must use an initial MovingToReference transform.");
        }

        private static void IlluminationGradient()
        {
            AssertEcc(4.0, 3.0, 2.0, true, false, null, "ECC must tolerate an illumination gradient.");
        }

        private static void Noise()
        {
            AssertEcc(-5.0, 6.0, -2.0, false, true, null, "ECC must tolerate modest noise.");
        }

        private static void InsufficientOverlap()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(128, 96))
            using (var moving = Warp(reference, 90.0, 70.0, 0.0, false, false))
            using (var matcher = new EccMatcher())
            {
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, Purpose = MatchPurpose.SyntheticTest, Options = Options(EccMotionModel.Euclidean, minCorrelation: 0.95) }, CancellationToken.None);
                AssertFalse(result.Success, "Insufficient overlap must not be reported as success.");
            }
        }

        private static void NonConvergence()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(128, 96))
            using (var moving = Warp(reference, 20.0, 20.0, 12.0, false, false))
            using (var matcher = new EccMatcher())
            {
                var options = Options(EccMotionModel.Euclidean, minCorrelation: 0.9999);
                options.MaxIterations = 1;
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, Purpose = MatchPurpose.SyntheticTest, Options = options }, CancellationToken.None);
                AssertFalse(result.Success, "Non-converged ECC must not be marked success.");
            }
        }

        private static void Cancellation()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(128, 96))
            using (var moving = Warp(reference, 4.0, 2.0, 1.0, false, false))
            using (var matcher = new EccMatcher())
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, Purpose = MatchPurpose.SyntheticTest, Options = Options(EccMotionModel.Euclidean, minCorrelation: 0.5) }, cts.Token);
                AssertFalse(result.Success, "Cancelled ECC must not succeed.");
                AssertEqual(MatchFailureReason.Cancelled, result.FailureReason, "Cancelled ECC must report Cancelled.");
            }
        }

        private static void TransformDirectionIsMovingToReference()
        {
            var shiftX = 8.0;
            var shiftY = -3.0;
            var angleDeg = 2.5;
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(128, 96))
            using (var moving = Warp(reference, shiftX, shiftY, angleDeg, false, false))
            using (var matcher = new EccMatcher())
            {
                var expected = new Transform2D(ReferenceToMoving(shiftX, shiftY, angleDeg, reference.Size())).Invert();
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, Purpose = MatchPurpose.SyntheticTest, Options = Options(EccMotionModel.Euclidean, minCorrelation: 0.5) }, CancellationToken.None);
                AssertTrue(result.Success, "ECC direction test must succeed. " + result.FailureReason + " " + result.FailureMessage);
                TransformAssert.AreEqual(expected.ToArray(), result.MovingToReferenceTransform.ToArray(), TranslationTolerancePixels, "ECC MovingToReferenceTransform must map moving image coordinates back to reference image coordinates.");
            }
        }

        private static void AssertEcc(double shiftX, double shiftY, double angleDeg, bool gradient, bool noise, Transform2D initialMovingToReference, string message)
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(128, 96))
            using (var moving = Warp(reference, shiftX, shiftY, angleDeg, gradient, noise))
            using (var matcher = new EccMatcher())
            {
                var expected = new Transform2D(ReferenceToMoving(shiftX, shiftY, angleDeg, reference.Size())).Invert();
                var result = matcher.Match(new MatchRequest { ReferenceImage = reference, MovingImage = moving, InitialMovingToReferenceTransform = initialMovingToReference, Purpose = MatchPurpose.SyntheticTest, Options = Options(EccMotionModel.Euclidean, minCorrelation: noise || gradient ? 0.45 : 0.60) }, CancellationToken.None);
                AssertTrue(result.Success, message + " Result: " + result.FailureReason + " " + result.FailureMessage);
                AssertNear(expected[0, 2], result.TranslationX, TranslationTolerancePixels, message + " TranslationX mismatch.");
                AssertNear(expected[1, 2], result.TranslationY, TranslationTolerancePixels, message + " TranslationY mismatch.");
                AssertNear(Math.Atan2(expected[1, 0], expected[0, 0]) * 180.0 / Math.PI, result.RotationDeg, RotationToleranceDeg, message + " Rotation mismatch.");
                AssertTrue(result.RawScore > 0.0, "ECC must return a positive actual correlation.");
            }
        }

        private static MatcherOptions Options(EccMotionModel motionModel, double minCorrelation)
        {
            return new MatcherOptions { EccMotionModel = motionModel, PyramidLevels = 3, MaxIterations = 80, Epsilon = 1e-5, MinCorrelation = minCorrelation, MinTextureStdDev = 1.0, MaxAbsRotationDeg = 20.0, MinScale = 0.95, MaxScale = 1.05 };
        }

        private static Mat Warp(Mat reference, double shiftX, double shiftY, double angleDeg, bool gradient, bool noise)
        {
            var matrix = ReferenceToMoving(shiftX, shiftY, angleDeg, reference.Size());
            using (var warp = ToWarpMat(matrix))
            {
                var moving = new Mat();
                Cv2.WarpAffine(reference, moving, warp, reference.Size(), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));
                if (gradient) ApplyGradient(moving);
                if (noise) ApplyNoise(moving);
                return moving;
            }
        }

        private static double[,] ReferenceToMoving(double shiftX, double shiftY, double angleDeg, Size size)
        {
            var centerX = (size.Width - 1) / 2.0;
            var centerY = (size.Height - 1) / 2.0;
            var angle = angleDeg * Math.PI / 180.0;
            var c = Math.Cos(angle);
            var s = Math.Sin(angle);
            var tx = centerX - c * centerX + s * centerY + shiftX;
            var ty = centerY - s * centerX - c * centerY + shiftY;
            return new[,] { { c, -s, tx }, { s, c, ty }, { 0d, 0d, 1d } };
        }

        private static Mat ToWarpMat(double[,] matrix)
        {
            var warp = new Mat(2, 3, MatType.CV_64FC1);
            warp.Set<double>(0, 0, matrix[0, 0]); warp.Set<double>(0, 1, matrix[0, 1]); warp.Set<double>(0, 2, matrix[0, 2]);
            warp.Set<double>(1, 0, matrix[1, 0]); warp.Set<double>(1, 1, matrix[1, 1]); warp.Set<double>(1, 2, matrix[1, 2]);
            return warp;
        }

        private static void ApplyGradient(Mat image)
        {
            for (int y = 0; y < image.Rows; y++)
            {
                using (var row = image.Row(y))
                    Cv2.Add(row, Scalar.All(y * 24.0 / Math.Max(1, image.Rows - 1)), row);
            }
        }

        private static void ApplyNoise(Mat image)
        {
            using (var noise = new Mat(image.Rows, image.Cols, MatType.CV_8UC1))
            {
                Cv2.Randn(noise, Scalar.All(0), Scalar.All(5));
                Cv2.Add(image, noise, image);
            }
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
