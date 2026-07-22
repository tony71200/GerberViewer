using System;
using System.Threading;
using GerberViewer.Stitching.Imaging.ImageInterop;
using GerberViewer.Stitching.Matching;
using GerberViewer.Stitching.Transforms;
using HalconDotNet;
using OpenCvSharp;

namespace GerberStitching.Tests.Matching
{
    public static class NCC_HalconMatcherTests
    {
        private const double TranslationTolerancePixels = 2.0;
        private const double RotationToleranceDeg = 1.0;

        public static void RunAll()
        {
            string skipReason;
            if (!TryGetHalconRuntimeSkipReason(out skipReason))
            {
                Console.WriteLine("SKIP HALCON NCC tests: " + skipReason);
                return;
            }

            ModelCreateFindClear();
            Translation();
            SmallRotation();
            PolarityCandidate();
            ScoreThreshold();
            NoMatchRejection();
            CacheReuse();
            CacheDisposal();
            Cancellation();
            TransformDirectionIsMovingToReference();
        }

        private static bool TryGetHalconRuntimeSkipReason(out string reason)
        {
            HObject hObject = null;
            HTuple width = null;
            HTuple height = null;
            HTuple modelId = null;
            try
            {
                using (var mat = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(64, 48))
                {
                    var interop = new ImageInteropService();
                    hObject = interop.ToHObjectCopy(mat, InteropPixelFormat.Mono8);
                    HOperatorSet.GetImageSize(hObject, out width, out height);
                    HOperatorSet.CreateNccModel(hObject, 1, 0.0, 0.0, 0.0, "use_polarity", out modelId);
                    HOperatorSet.ClearNccModel(modelId);
                }

                reason = null;
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
            finally
            {
                if (hObject != null) hObject.Dispose();
                if (width != null) width.Dispose();
                if (height != null) height.Dispose();
                if (modelId != null) modelId.Dispose();
            }
        }

        private static void ModelCreateFindClear()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(96, 72))
            using (var moving = Warp(reference, 4.0, 3.0, 0.0, false, false))
            {
                var matcher = new NCC_HalconMatcher();
                var result = matcher.Match(Request(reference, moving, "model-lifecycle", Options(0.5)), CancellationToken.None);
                AssertTrue(result.Success, "HALCON NCC create/find must succeed before clear. " + result.FailureMessage);
                AssertEqual("create_ncc_model", result.Diagnostics["HalconCreateOperator"], "NCC matcher must use create_ncc_model.");
                AssertEqual("find_ncc_model", result.Diagnostics["HalconFindOperator"], "NCC matcher must use find_ncc_model.");
                AssertEqual("clear_ncc_model", result.Diagnostics["HalconClearOperator"], "NCC matcher must clear individual models.");
                AssertEqual(1, matcher.CachedModelCount, "Successful match must cache one real HALCON model handle.");
                matcher.Dispose();
                AssertEqual(0, matcher.CachedModelCount, "Dispose must clear cached HALCON model handles exactly once.");
                matcher.Dispose();
            }
        }

        private static void Translation()
        {
            AssertNcc(6.0, -4.0, 0.0, false, false, Options(0.45), "HALCON NCC translation must be recovered.");
        }

        private static void SmallRotation()
        {
            var options = Options(0.35);
            options.NccAngleStartRad = -8.0 * Math.PI / 180.0;
            options.NccAngleExtentRad = 16.0 * Math.PI / 180.0;
            options.NccAngleStepRad = 0.5 * Math.PI / 180.0;
            AssertNcc(2.0, 3.0, 3.0, false, false, options, "HALCON NCC small rotation must be recovered.");
        }

        private static void PolarityCandidate()
        {
            var options = Options(0.20);
            options.NccMetric = "ignore_global_polarity";
            options.PreprocessingVariant = "inverted-polarity-candidate";
            AssertNcc(3.0, 2.0, 0.0, true, false, options, "HALCON NCC must support an inverted-polarity candidate when metric allows it.");
        }

        private static void ScoreThreshold()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(96, 72))
            using (var moving = Warp(reference, 2.0, 2.0, 0.0, false, false))
            using (var matcher = new NCC_HalconMatcher())
            {
                var options = Options(1.01);
                var result = matcher.Match(Request(reference, moving, "score-threshold", options), CancellationToken.None);
                AssertFalse(result.Success, "NCC score below threshold must not succeed.");
                AssertEqual(MatchFailureReason.CorrelationBelowThreshold, result.FailureReason, "NCC threshold rejection reason mismatch.");
            }
        }

        private static void NoMatchRejection()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(96, 72))
            using (var moving = new Mat(reference.Rows, reference.Cols, MatType.CV_8UC1, Scalar.All(0)))
            using (var matcher = new NCC_HalconMatcher())
            {
                var result = matcher.Match(Request(reference, moving, "no-match", Options(0.7)), CancellationToken.None);
                AssertFalse(result.Success, "No-match NCC result must be rejected.");
            }
        }

        private static void CacheReuse()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(96, 72))
            using (var moving = Warp(reference, 1.0, 2.0, 0.0, false, false))
            using (var matcher = new NCC_HalconMatcher())
            {
                var request = Request(reference, moving, "cache-reuse", Options(0.4));
                var first = matcher.Match(request, CancellationToken.None);
                var second = matcher.Match(request, CancellationToken.None);
                AssertTrue(first.Success, "First cache-reuse NCC match must succeed.");
                AssertTrue(second.Success, "Second cache-reuse NCC match must succeed.");
                AssertEqual(1, matcher.CachedModelCount, "Same sample/options must reuse one cached model.");
                AssertEqual("true", second.Diagnostics["CacheHit"], "Second match must report cache hit.");
            }
        }

        private static void CacheDisposal()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(96, 72))
            using (var moving = Warp(reference, 1.0, 2.0, 0.0, false, false))
            {
                var matcher = new NCC_HalconMatcher();
                var result = matcher.Match(Request(reference, moving, "cache-disposal", Options(0.4)), CancellationToken.None);
                AssertTrue(result.Success, "Cache-disposal NCC match must succeed before dispose.");
                matcher.Dispose();
                AssertEqual(0, matcher.CachedModelCount, "NCC matcher disposal must clear the cache.");
                matcher.Dispose();
                AssertEqual(0, matcher.CachedModelCount, "NCC matcher disposal must be idempotent.");
            }
        }

        private static void Cancellation()
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(96, 72))
            using (var moving = Warp(reference, 1.0, 2.0, 0.0, false, false))
            using (var matcher = new NCC_HalconMatcher())
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                var result = matcher.Match(Request(reference, moving, "cancel", Options(0.4)), cts.Token);
                AssertFalse(result.Success, "Cancelled NCC match must not succeed.");
                AssertEqual(MatchFailureReason.Cancelled, result.FailureReason, "Cancelled NCC match must report Cancelled.");
            }
        }

        private static void TransformDirectionIsMovingToReference()
        {
            AssertNcc(5.0, -3.0, 0.0, false, false, Options(0.45), "NCC MovingToReferenceTransform must map moving coordinates back to reference coordinates.");
        }

        private static void AssertNcc(double shiftX, double shiftY, double angleDeg, bool invertPolarity, bool noise, MatcherOptions options, string message)
        {
            using (var reference = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(96, 72))
            using (var moving = Warp(reference, shiftX, shiftY, angleDeg, invertPolarity, noise))
            using (var matcher = new NCC_HalconMatcher())
            {
                var expected = new Transform2D(ReferenceToMoving(shiftX, shiftY, angleDeg, reference.Size())).Invert();
                var result = matcher.Match(Request(reference, moving, "tile-" + Guid.NewGuid().ToString("N"), options), CancellationToken.None);
                AssertTrue(result.Success, message + " Result: " + result.FailureReason + " " + result.FailureMessage);
                AssertNear(expected[0, 2], result.TranslationX, TranslationTolerancePixels, message + " TranslationX mismatch.");
                AssertNear(expected[1, 2], result.TranslationY, TranslationTolerancePixels, message + " TranslationY mismatch.");
                AssertNear(Math.Atan2(expected[1, 0], expected[0, 0]) * 180.0 / Math.PI, result.RotationDeg, RotationToleranceDeg, message + " Rotation mismatch.");
                AssertTrue(result.RawScore >= options.NccMinScore, "NCC must return actual score that satisfies NccMinScore.");
                AssertEqual("MovingImage -> ReferenceImage", result.Diagnostics["TransformDirection"], "NCC transform direction diagnostic mismatch.");
                AssertTrue(result.Diagnostics["ModelOrigin"].IndexOf("row=0,column=0", StringComparison.OrdinalIgnoreCase) >= 0, "NCC diagnostics must document model-origin handling.");
            }
        }

        private static MatchRequest Request(Mat reference, Mat moving, string sampleTileId, MatcherOptions options)
        {
            return new MatchRequest
            {
                ReferenceImage = reference,
                MovingImage = moving,
                SampleTileId = sampleTileId,
                Purpose = MatchPurpose.SyntheticTest,
                Options = options
            };
        }

        private static MatcherOptions Options(double minScore)
        {
            return new MatcherOptions
            {
                PreprocessingVariant = "synthetic-mono8",
                NccNumLevels = 4,
                NccAngleStartRad = -5.0 * Math.PI / 180.0,
                NccAngleExtentRad = 10.0 * Math.PI / 180.0,
                NccAngleStepRad = 1.0 * Math.PI / 180.0,
                NccMetric = "use_polarity",
                NccMinScore = minScore,
                NccMaxMatches = 1,
                NccMaxOverlap = 0.5,
                NccSubPixel = "true",
                MaxAbsRotationDeg = 10.0,
                MinScale = 0.95,
                MaxScale = 1.05
            };
        }

        private static Mat Warp(Mat reference, double shiftX, double shiftY, double angleDeg, bool invertPolarity, bool noise)
        {
            var matrix = ReferenceToMoving(shiftX, shiftY, angleDeg, reference.Size());
            using (var warp = ToWarpMat(matrix))
            {
                var moving = new Mat();
                Cv2.WarpAffine(reference, moving, warp, reference.Size(), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));
                if (invertPolarity) Cv2.BitwiseNot(moving, moving);
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

        private static void ApplyNoise(Mat image)
        {
            using (var noise = new Mat(image.Rows, image.Cols, MatType.CV_8UC1))
            {
                Cv2.Randn(noise, Scalar.All(0), Scalar.All(4));
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

        private static void AssertEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual);
        }

        private static void AssertEqual(int expected, int actual, string message)
        {
            if (expected != actual) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual);
        }

        private static void AssertEqual(MatchFailureReason expected, MatchFailureReason actual, string message)
        {
            if (expected != actual) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual);
        }
    }
}
