using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using GerberViewer.Stitching.Alignment;
using GerberViewer.Stitching.Matching;
using GerberViewer.Stitching.Models;
using GerberViewer.Stitching.Transforms;
using OpenCvSharp;

namespace GerberStitching.Tests.Matching
{
    public static class DirectMatcherPipelineTests
    {
        public static void RunAll()
        {
            DirectSuccess();
            NccSuccessAndEccSuccessUsesEcc();
            NccSuccessAndEccFailUsesNccOnlyWhenAllowed();
            NccFailAndEccExpectedInitialization();
            AllMethodsFailRejectsDirect();
            InvertedPolarityCandidateCanBeSelected();
            PartialOverlapRejectsDirect();
            OrderIndexMappingAndGlobalPoseComposition();
            DirectFailureTriggersRecoveryInsteadOfExpectedGridSuccess();
        }

        private static void DirectSuccess()
        {
            var factory = new ScriptedMatcherFactory(
                new MatchResult[] { Success("NCC", 0.9, Transform2D.Translation(-2, -3)) },
                new MatchResult[] { Success("ECC", 0.95, Transform2D.Translation(-2.5, -3.5)) });
            var result = RunAligner(factory, PolarityMode.AsIs, true, true);
            AssertTrue(result.Success, "Direct pipeline should succeed.");
            AssertEqual("NCC+ECC", result.PipelineStage, "Direct success must be NCC+ECC.");
            AssertNear(0.9, result.NccScore, 1e-9, "NCC score must be retained.");
            AssertNear(0.95, result.EccCorrelation, 1e-9, "ECC correlation must be retained.");
            AssertNear(-2.5, result.TranslationX, 1e-9, "ECC transform must be selected.");
        }

        private static void NccSuccessAndEccSuccessUsesEcc()
        {
            DirectSuccess();
        }

        private static void NccSuccessAndEccFailUsesNccOnlyWhenAllowed()
        {
            var factory = new ScriptedMatcherFactory(
                new MatchResult[] { Success("NCC", 0.88, Transform2D.Translation(-4, -1)) },
                new MatchResult[] { Failed("ECC", MatchFailureReason.RuntimeFailure, "forced ecc fail") });
            var result = RunAligner(factory, PolarityMode.AsIs, true, false);
            AssertTrue(result.Success, "NCC-only fallback should succeed when policy allows it.");
            AssertEqual("NCC_ONLY_AFTER_ECC_FAIL", result.PipelineStage, "Pipeline stage must record NCC-only fallback.");
            AssertEqual("NccOnlyAcceptedAfterEccFailure", result.Warning, "Fallback warning must be explicit.");
            AssertNear(0.88, result.NccScore, 1e-9, "NCC score must be retained on fallback.");
            AssertTrue(double.IsNaN(result.EccCorrelation), "Failed ECC result must not be stored as a fake successful correlation.");
        }

        private static void NccFailAndEccExpectedInitialization()
        {
            var factory = new ScriptedMatcherFactory(
                new MatchResult[] { Failed("NCC", MatchFailureReason.CorrelationBelowThreshold, "forced ncc fail") },
                new MatchResult[] { Success("ECC", 0.91, Transform2D.Translation(-1, -2)) });
            var result = RunAligner(factory, PolarityMode.AsIs, false, true);
            AssertTrue(result.Success, "ECC from expected initialization should succeed when NCC fails and policy allows it.");
            AssertEqual("ECC_FROM_EXPECTED_AFTER_NCC_FAIL", result.PipelineStage, "Pipeline stage must record ECC-from-expected.");
            AssertEqual("EccAcceptedFromExpectedAfterNccFailure", result.Warning, "Expected-initialization warning must be explicit.");
            AssertNear(0.91, result.EccCorrelation, 1e-9, "Actual ECC correlation must be retained.");
        }

        private static void AllMethodsFailRejectsDirect()
        {
            var factory = new ScriptedMatcherFactory(
                new MatchResult[] { Failed("NCC", MatchFailureReason.CorrelationBelowThreshold, "forced ncc fail") },
                new MatchResult[] { Failed("ECC", MatchFailureReason.RuntimeFailure, "forced ecc fail") });
            var result = RunAligner(factory, PolarityMode.AsIs, false, true);
            AssertFalse(result.Success, "All methods failing must reject direct alignment.");
            AssertTrue(result.CapturedToSampleTransform != null, "Rejected result may carry identity for serialization but must not be marked success.");
        }

        private static void InvertedPolarityCandidateCanBeSelected()
        {
            var factory = new VariantAwareMatcherFactory();
            var result = RunAligner(factory, PolarityMode.Auto, false, true);
            AssertTrue(result.Success, "Auto polarity should try an inverted captured candidate.");
            AssertTrue(result.PreprocessingVariant.IndexOf("invert-captured", StringComparison.OrdinalIgnoreCase) >= 0, "Selected preprocessing variant must record inverted captured polarity.");
        }

        private static void PartialOverlapRejectsDirect()
        {
            var factory = new ScriptedMatcherFactory(
                new MatchResult[] { Failed("NCC", MatchFailureReason.GeometryRejected, "partial overlap") },
                new MatchResult[] { Failed("ECC", MatchFailureReason.GeometryRejected, "partial overlap") });
            var result = RunAligner(factory, PolarityMode.AsIs, false, true);
            AssertFalse(result.Success, "Partial overlap below policy must reject direct alignment.");
        }

        private static void OrderIndexMappingAndGlobalPoseComposition()
        {
            var root = Path.Combine(Path.GetTempPath(), "gv_direct_pipeline_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var sample0 = Path.Combine(root, "sample0.png");
                var cap0 = Path.Combine(root, "cap0.png");
                WriteBitmap(sample0);
                WriteBitmap(cap0);
                var manifest = new SampleManifest
                {
                    ManifestVersion = SampleManifest.CurrentVersion,
                    RootDirectory = root,
                    SourceRasterPath = sample0,
                    SourceWidth = 200,
                    SourceHeight = 200,
                    ProcessedWidth = 200,
                    ProcessedHeight = 200,
                    Tiles = new List<SampleTileInfo> { new SampleTileInfo { OrderIndex = 0, Row = 0, Column = 0, ExpectedPath = sample0, ExpectedX = 40, ExpectedY = 50, Width = 64, Height = 64 } }
                };
                var captured = new List<CapturedImageInfo> { new CapturedImageInfo { OrderIndex = 0, Row = 0, Column = 0, FilePath = cap0 } };
                var aligner = new StaticSampleAligner(Homography.FromPose(-3, -4, 0, 1), 0.8, 0.9);
                var service = new AlignStitchWorkflowService(() => aligner);
                var result = service.RunAsync(new AlignStitchConfig(), manifest, captured, null, CancellationToken.None).GetAwaiter().GetResult();
                AssertTrue(result.Report.Succeeded, "Workflow should succeed with verified direct alignment.");
                AssertEqual(0, result.States[0].OrderIndex, "OrderIndex mapping must be preserved.");
                AssertNear(37, result.States[0].GlobalPose[0, 2], 1e-9, "Global pose X must be Translation(tile.ExpectedX) x CapturedToSample.");
                AssertNear(46, result.States[0].GlobalPose[1, 2], 1e-9, "Global pose Y must be Translation(tile.ExpectedY) x CapturedToSample.");
                AssertNear(0.8, result.Report.TileReports[0].NccScore, 1e-9, "Report must retain actual NCC score.");
                AssertNear(0.9, result.Report.TileReports[0].EccCorrelation, 1e-9, "Report must retain actual ECC correlation.");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }


        private static void DirectFailureTriggersRecoveryInsteadOfExpectedGridSuccess()
        {
            var root = Path.Combine(Path.GetTempPath(), "gv_direct_fail_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var sample0 = Path.Combine(root, "sample0.png");
                var cap0 = Path.Combine(root, "cap0.png");
                WriteBitmap(sample0);
                WriteBitmap(cap0);
                var manifest = new SampleManifest
                {
                    ManifestVersion = SampleManifest.CurrentVersion,
                    RootDirectory = root,
                    SourceRasterPath = sample0,
                    SourceWidth = 200,
                    SourceHeight = 200,
                    ProcessedWidth = 200,
                    ProcessedHeight = 200,
                    Tiles = new List<SampleTileInfo> { new SampleTileInfo { OrderIndex = 0, Row = 0, Column = 0, ExpectedPath = sample0, ExpectedX = 40, ExpectedY = 50, Width = 64, Height = 64 } }
                };
                var captured = new List<CapturedImageInfo> { new CapturedImageInfo { OrderIndex = 0, Row = 0, Column = 0, FilePath = cap0 } };
                var service = new AlignStitchWorkflowService(() => new FailingSampleAligner());
                var result = service.RunAsync(new AlignStitchConfig(), manifest, captured, null, CancellationToken.None).GetAwaiter().GetResult();
                AssertFalse(result.Report.Succeeded, "Failed direct alignment must not publish success.");
                AssertFalse(result.States[0].AlignmentSucceeded, "Failed direct alignment must remain failed after unavailable recovery.");
                AssertTrue(result.States[0].Source == PoseSource.Failed, "Expected-grid fallback must not be used as false success.");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static SampleAlignmentResult RunAligner(IMatcherFactory factory, PolarityMode polarity, bool allowNccOnly, bool allowExpectedEcc)
        {
            using (var sample = new Bitmap(32, 32, PixelFormat.Format24bppRgb))
            using (var captured = new Bitmap(32, 32, PixelFormat.Format24bppRgb))
            using (var aligner = new NccThenPyramidEccSampleAligner(new MatcherPipeline(factory)))
            {
                using (var g = Graphics.FromImage(sample)) g.Clear(Color.White);
                using (var g = Graphics.FromImage(captured)) g.Clear(Color.White);
                return aligner.Align(new SampleAlignmentContext
                {
                    SampleTileId = "0",
                    SampleImage = sample,
                    CapturedImage = captured,
                    ExpectedCapturedToSampleTransform = Homography.Identity(),
                    Options = new SampleAlignmentOptions { AllowNccOnlyAcceptance = allowNccOnly, AllowEccFromExpectedWhenNccFails = allowExpectedEcc, Preprocessing = new PreprocessingOptions { Polarity = polarity, Threshold = ThresholdMode.None, EdgePreparation = EdgePreparationMode.None, ContrastNormalization = ContrastNormalizationMode.None } }
                });
            }
        }

        private static MatchResult Success(string name, double score, Transform2D transform)
        {
            return new MatchResult { Success = true, MatcherName = name, MovingToReferenceTransform = transform, TranslationX = transform[0, 2], TranslationY = transform[1, 2], RawScore = score, NormalizedConfidence = score, OverlapRatio = 1, FailureReason = MatchFailureReason.None };
        }

        private static MatchResult Failed(string name, MatchFailureReason reason, string message)
        {
            return MatchResult.Failed(name, reason, message);
        }

        private static void WriteBitmap(string path)
        {
            using (var bitmap = new Bitmap(64, 64, PixelFormat.Format24bppRgb))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.FillRectangle(Brushes.Black, 10, 8, 20, 12);
                bitmap.Save(path, ImageFormat.Png);
            }
        }

        private static void AssertTrue(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static void AssertFalse(bool value, string message) { if (value) throw new InvalidOperationException(message); }
        private static void AssertEqual(string expected, string actual, string message) { if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual); }
        private static void AssertEqual(int expected, int actual, string message) { if (expected != actual) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual); }
        private static void AssertNear(double expected, double actual, double tolerance, string message) { if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual); }

        private sealed class ScriptedMatcherFactory : IMatcherFactory
        {
            private readonly Queue<MatchResult> _ncc;
            private readonly Queue<MatchResult> _ecc;
            public ScriptedMatcherFactory(IEnumerable<MatchResult> ncc, IEnumerable<MatchResult> ecc) { _ncc = new Queue<MatchResult>(ncc); _ecc = new Queue<MatchResult>(ecc); }
            public IMatcher CreateNccMatcher() { return new ScriptedMatcher(_ncc.Count > 0 ? _ncc.Dequeue() : Failed("NCC", MatchFailureReason.RuntimeFailure, "No scripted NCC result")); }
            public IMatcher CreateEccMatcher() { return new ScriptedMatcher(_ecc.Count > 0 ? _ecc.Dequeue() : Failed("ECC", MatchFailureReason.RuntimeFailure, "No scripted ECC result")); }
        }

        private sealed class VariantAwareMatcherFactory : IMatcherFactory
        {
            public IMatcher CreateNccMatcher() { return new VariantAwareMatcher(true); }
            public IMatcher CreateEccMatcher() { return new VariantAwareMatcher(false); }
        }

        private sealed class VariantAwareMatcher : IMatcher
        {
            private readonly bool _ncc;
            public VariantAwareMatcher(bool ncc) { _ncc = ncc; }
            public string MatcherName { get { return _ncc ? "NCC" : "ECC"; } }
            public MatchResult Match(MatchRequest request, CancellationToken cancellationToken)
            {
                var inverted = request.Options != null && request.Options.PreprocessingVariant != null && request.Options.PreprocessingVariant.IndexOf("invert-captured", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!inverted) return Failed(MatcherName, MatchFailureReason.CorrelationBelowThreshold, "Variant rejected");
                return Success(MatcherName, _ncc ? 0.82 : 0.9, Transform2D.Translation(-1, -1));
            }
            public void Dispose() { }
        }

        private sealed class ScriptedMatcher : IMatcher
        {
            private readonly MatchResult _result;
            public ScriptedMatcher(MatchResult result) { _result = result; }
            public string MatcherName { get { return _result.MatcherName; } }
            public MatchResult Match(MatchRequest request, CancellationToken cancellationToken) { return _result; }
            public void Dispose() { }
        }

        private sealed class FailingSampleAligner : ISampleAligner
        {
            public SampleAlignmentResult Align(SampleAlignmentContext context) { return SampleAlignmentResult.Rejected(SampleAlignmentMethod.NccThenPyramidEcc, "forced direct failure"); }
            public void Dispose() { }
        }

        private sealed class StaticSampleAligner : ISampleAligner
        {
            private readonly double[,] _pose;
            private readonly double _ncc;
            private readonly double _ecc;
            public StaticSampleAligner(double[,] pose, double ncc, double ecc) { _pose = pose; _ncc = ncc; _ecc = ecc; }
            public SampleAlignmentResult Align(SampleAlignmentContext context) { return new SampleAlignmentResult { Success = true, Method = SampleAlignmentMethod.NccThenPyramidEcc, CapturedToSampleTransform = _pose, NccScore = _ncc, EccCorrelation = _ecc, TranslationX = _pose[0, 2], TranslationY = _pose[1, 2], Scale = 1, RotationDeg = 0, OverlapRatio = 1, PipelineStage = "NCC+ECC" }; }
            public void Dispose() { }
        }
    }
}
