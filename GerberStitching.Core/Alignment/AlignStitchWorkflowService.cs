using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GerberViewer.Stitching.Imaging.ImageInterop;
using GerberViewer.Stitching.Matching;
using GerberViewer.Stitching.Models;
using GerberViewer.Stitching.Stitching;
using GerberViewer.Stitching.Transforms;
using OpenCvSharp;

namespace GerberViewer.Stitching.Alignment
{
    public interface IManualAlignmentProvider
    {
        ManualAlignmentResult RequestManualAlignment(ManualAlignmentRequest request, CancellationToken cancellationToken);
    }

    public sealed class AlignStitchWorkflowService
    {
        private readonly Func<ISampleAligner> _alignerFactory;
        private readonly IManualAlignmentProvider _manualProvider;
        private readonly IMatcherFactory _matcherFactory;
        private readonly IImageInteropService _imageInterop = new ImageInteropService();

        public AlignStitchWorkflowService(Func<ISampleAligner> alignerFactory, IManualAlignmentProvider manualProvider = null, IMatcherFactory matcherFactory = null)
        {
            _alignerFactory = alignerFactory ?? (() => new HalconNccSampleAligner());
            _manualProvider = manualProvider;
            _matcherFactory = matcherFactory ?? new MatcherFactory();
        }

        public Task<AlignStitchWorkflowResult> RunAsync(
            AlignStitchConfig config, 
            SampleManifest manifest, 
            IList<CapturedImageInfo> captured, 
            IProgress<WorkflowProgress> progress, 
            CancellationToken cancellationToken)
        {
            return Task.Run(() => RunCore(
                config ?? new AlignStitchConfig(), 
                manifest, 
                captured, 
                progress, 
                cancellationToken
                ), cancellationToken);
        }

        private AlignStitchWorkflowResult RunCore(
            AlignStitchConfig config, 
            SampleManifest manifest, 
            IList<CapturedImageInfo> captured, 
            IProgress<WorkflowProgress> progress, 
            CancellationToken ct)
        {
            ValidateInputs(manifest, captured);
            var report = ProcessingReport.Create(config, manifest);
            report.Messages.Add("Stitching engine: " + config.StitchingEngine + " (OpenCV path remains available; HALCON ProjectiveMosaic uses HOperatorSet.GenProjectiveMosaic when selected).");
            report.Messages.Add("Transform contract: NCC_HalconMatcher returns MovingImage-to-ReferenceImage transforms. Direct alignment warps each captured MovingImage into its reference tile coordinate system, then composes the tile ExpectedX/ExpectedY offset for global stitching.");
            var solved = new Dictionary<int, TileWorkflowState>();
            var tileByOrder = manifest.Tiles.ToDictionary(t => t.OrderIndex);
            var capturedByOrder = captured.ToDictionary(c => c.OrderIndex);
            var ordered = captured.OrderBy(c => c.OrderIndex).ToList();

            using (var aligner = _alignerFactory())
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var cap = ordered[i];
                    progress?.Report(new WorkflowProgress(i, ordered.Count, cap, "Direct camera-to-sample alignment"));
                    var tile = tileByOrder[cap.OrderIndex];
                    var state = SolveDirect(config, tile, cap, aligner, report);
                    if (!state.AlignmentSucceeded)
                        state = Recover(config, tile, cap, solved, ordered, capturedByOrder, tileByOrder, report, ct, false);
                    solved[cap.OrderIndex] = state;
                }
            }

            foreach (var cap in ordered)
            {
                ct.ThrowIfCancellationRequested();
                TileWorkflowState state;
                if (solved.TryGetValue(cap.OrderIndex, out state) && !state.AlignmentSucceeded)
                    solved[cap.OrderIndex] = Recover(config, tileByOrder[cap.OrderIndex], cap, solved, ordered, capturedByOrder, tileByOrder, report, ct, true);
            }

            foreach (var state in solved.Values.OrderBy(v => v.OrderIndex)) 
                report.Poses.Add(state.ToPose());

            var alignedCount = solved.Values.Count(v => v.AlignmentSucceeded);
            var stitchableCount = solved.Values.Count(v => v.IsStitchable);
            var fallbackCount = solved.Values.Count(v => v.IsFallbackPose);
            var excludedCount = solved.Values.Count(v => v.Source == PoseSource.Excluded);
            report.Succeeded = alignedCount == ordered.Count && stitchableCount == ordered.Count && ordered.Count > 0;
            if (report.Succeeded)
                report.RunStatus = fallbackCount > 0 ? AlignStitchRunStatus.CompletedWithFallback : AlignStitchRunStatus.Completed;
            else if (excludedCount > 0 && alignedCount + excludedCount == ordered.Count)
                report.RunStatus = AlignStitchRunStatus.CompletedWithExcludedTiles;
            else
                report.RunStatus = AlignStitchRunStatus.Failed;
            if (!report.Succeeded)
                report.Warnings.Add("Production output blocked: not every captured image has a verified stitchable alignment pose.");
            else if (!string.IsNullOrWhiteSpace(config.OutputPath))
            {
                var stitchedPath = Path.Combine(config.OutputPath, "stitched.tiff");
                report.FinalOutputPath = new GlobalTransformStitcher().StitchFromGlobalTransforms(
                    ordered, 
                    solved.Values.OrderBy(v => v.OrderIndex).ToList(), 
                    new StitchFromGlobalTransformsOptions 
                    { 
                        OutputPath = stitchedPath, 
                        PreviewUpdateInterval = config.PreviewUpdateInterval, 
                        MaxPreviewMegapixels = config.MaxPreviewMegapixels, 
                        TiffMode = config.TiffMode, 
                        EnableBlending = false, 
                        ForceGray8Output = true, 
                        BlendMode = StitchBlendMode.NoBlend, 
                        StitchingEngine = config.StitchingEngine 
                    }, null, ct);
            }
            return new AlignStitchWorkflowResult { Report = report, States = solved.Values.OrderBy(v => v.OrderIndex).ToList() };
        }

        private static void ValidateInputs(SampleManifest manifest, IList<CapturedImageInfo> captured)
        {
            var validation = SampleManifestValidator.Validate(manifest, true);
            if (!validation.IsValid) throw new InvalidOperationException("Invalid sample manifest: " + string.Join("; ", validation.Errors));
            var tileByOrder = manifest.Tiles.ToDictionary(t => t.OrderIndex);
            if (captured == null || captured.Count != manifest.Tiles.Count) 
                throw new InvalidOperationException("Captured image count must equal manifest tile count.");
            foreach (var c in captured)
            {
                if (!tileByOrder.ContainsKey(c.OrderIndex)) 
                    throw new InvalidOperationException("Captured image OrderIndex has no manifest tile: " + c.OrderIndex);
                if (string.IsNullOrWhiteSpace(c.FilePath) || !File.Exists(c.FilePath)) 
                    throw new FileNotFoundException("Captured image missing for OrderIndex " + c.OrderIndex, c.FilePath);
            }
        }

        private TileWorkflowState SolveDirect(AlignStitchConfig config, SampleTileInfo tile, CapturedImageInfo cap, ISampleAligner aligner, ProcessingReport report)
        {
            using (var sample = LoadBitmap(tile.ExpectedPath))
            using (var img = LoadBitmap(cap.FilePath))
            {
                var ctx = new SampleAlignmentContext
                {
                    SampleTileId = cap.OrderIndex.ToString(),
                    SampleImage = sample,
                    CapturedImage = img,
                    ExpectedCapturedToSampleTransform = Homography.Identity(),
                    Options = ToOptions(config)
                };
                var r = aligner.Align(ctx);
                AddReport(report, cap, r, null);
                if (r.Success)
                {
                    var global = Multiply(Translation(tile.ExpectedX, tile.ExpectedY), 
                        r.CapturedToSampleTransform);
                    return TileWorkflowState.From(cap, global, PoseSource.SampleAlignment, r, null);
                }
                return TileWorkflowState.From(cap, null, PoseSource.Failed, r, r.RejectionReason);
            }
        }

        private TileWorkflowState Recover(
            AlignStitchConfig config, 
            SampleTileInfo tile, 
            CapturedImageInfo cap, 
            Dictionary<int, TileWorkflowState> solved, 
            IList<CapturedImageInfo> ordered, 
            IDictionary<int, CapturedImageInfo> capturedByOrder, 
            IDictionary<int, SampleTileInfo> tileByOrder, 
            ProcessingReport report, 
            CancellationToken ct, 
            bool includeSuccessor)
        {
            if (config.EnableNeighborRecovery)
            {
                foreach (var candidate in BuildRecoveryCandidates(cap, solved, ordered, includeSuccessor))
                {
                    ct.ThrowIfCancellationRequested();
                    var recovered = TryRecoverFromAnchor(config, tile, cap, candidate, capturedByOrder, tileByOrder, report, ct);
                    if (recovered != null) return recovered;
                }
                report.Warnings.Add(TilePrefix(cap) + "Neighbor recovery attempted but no image-based candidate passed acceptance.");
            }
            else report.Warnings.Add(TilePrefix(cap) + "Neighbor recovery disabled; expected-grid substitution is blocked.");

            report.Warnings.Add(TilePrefix(cap) + "Anchor interpolation unavailable: no implemented transform model; expected-grid substitution is blocked.");
            var expected = Translation(tile.ExpectedX, tile.ExpectedY);
            if (_manualProvider != null)
            {
                var manual = _manualProvider.RequestManualAlignment(new ManualAlignmentRequest { Captured = cap, SampleTile = tile, ExpectedGlobalPose = expected, Diagnostics = report.TileReports.Where(x => x.OrderIndex == cap.OrderIndex).Select(x => x.RejectionReason ?? x.FallbackReason).ToList() }, ct);
                if (manual != null)
                {
                    if (manual.CancelRun) throw new OperationCanceledException("Manual alignment cancelled the run.", ct);
                    if (manual.Accepted) return TileWorkflowState.From(cap, manual.GlobalPose, PoseSource.Manual, null, manual.Notes);
                    if (manual.Skipped) return TileWorkflowState.From(cap, null, PoseSource.Excluded, null, manual.Notes);
                }
            }
            return TileWorkflowState.From(cap, null, PoseSource.Failed, null, "No verified direct alignment and no accepted image-based recovery/manual pose.");
        }

        private TileWorkflowState TryRecoverFromAnchor(
            AlignStitchConfig config, 
            SampleTileInfo targetTile, 
            CapturedImageInfo target, 
            RecoveryCandidate candidate, 
            IDictionary<int, CapturedImageInfo> capturedByOrder, 
            IDictionary<int, SampleTileInfo> tileByOrder, 
            ProcessingReport report, 
            CancellationToken ct)
        {
            CapturedImageInfo anchorCap;
            SampleTileInfo anchorTile;
            if (!capturedByOrder.TryGetValue(candidate.Anchor.OrderIndex, out anchorCap) || !tileByOrder.TryGetValue(candidate.Anchor.OrderIndex, out anchorTile)) return null;
            var direction = DirectionBetween(anchorTile, targetTile);
            if (direction == null) return null;
            using (var anchorBitmap = LoadBitmap(anchorCap.FilePath))
            using (var targetBitmap = LoadBitmap(target.FilePath))
            using (var anchorMat = _imageInterop.ToMatCopy(anchorBitmap, InteropPixelFormat.Mono8))
            using (var targetMat = _imageInterop.ToMatCopy(targetBitmap, InteropPixelFormat.Mono8))
            using (var anchorRoi = CropCopy(anchorMat, AnchorRoi(anchorMat, direction)))
            using (var targetRoi = CropCopy(targetMat, TargetRoi(targetMat, direction)))
            {
                var overlap = MatcherGeometryValidator.CalculateSameSizeOverlap(anchorRoi, targetRoi);
                var options = ToMatcherOptions(config);
                options.MinOverlapRatio = config.MinOverlapRatio;
                MatchResult phase;
                using (var phaseMatcher = new PharseCorrMatcher())
                    phase = phaseMatcher.Match(new MatchRequest 
                    { 
                        ReferenceImage = anchorRoi, 
                        MovingImage = targetRoi, 
                        Options = options, 
                        Purpose = MatchPurpose.TargetCapturedToAnchorCaptured, 
                        SampleTileId = target.OrderIndex.ToString(), 
                        OrderIndex = target.OrderIndex 
                    }, ct);
                if (!phase.Success) 
                { 
                    AddRecoveryEdge(
                        report, 
                        candidate.Anchor.OrderIndex, 
                        target.OrderIndex, 
                        direction, 
                        "PharseCorrMatcher", 
                        phase.FailureReason + ": " + phase.FailureMessage, 
                        null, 
                        phase, 
                        null, 
                        overlap); 
                    return null; 
                }

                var roiToFull = RoiTransformToFull(
                    phase.MovingToReferenceTransform, 
                    AnchorRoi(anchorMat, direction), 
                    TargetRoi(targetMat, direction));
                MatchResult ecc = null;
                using (var eccMatcher = _matcherFactory.CreateEccMatcher())
                    ecc = eccMatcher.Match(
                        new MatchRequest 
                        { 
                            ReferenceImage = anchorMat, 
                            MovingImage = targetMat, 
                            InitialMovingToReferenceTransform = roiToFull, 
                            Options = options, 
                            Purpose = MatchPurpose.TargetCapturedToAnchorCaptured, 
                            SampleTileId = target.OrderIndex.ToString(), 
                            OrderIndex = target.OrderIndex 
                        }, ct);
                var targetToAnchor = ecc != null && ecc.Success ? ecc.MovingToReferenceTransform : roiToFull;
                var expectedTx = anchorTile.ExpectedX - targetTile.ExpectedX;
                var expectedTy = anchorTile.ExpectedY - targetTile.ExpectedY;
                var acceptance = NeighborMatchAcceptance.Validate(
                    phase, 
                    ecc, 
                    targetToAnchor, 
                    expectedTx, 
                    expectedTy, 
                    options, 
                    overlap);
                var matcherName = ecc != null && ecc.Success ? "PharseCorrMatcher+EccMatcher" : "PharseCorrMatcher";
                AddRecoveryEdge(
                    report, 
                    candidate.Anchor.OrderIndex, 
                    target.OrderIndex, 
                    direction, 
                    matcherName, 
                    acceptance.Reason, 
                    targetToAnchor.ToArray(), 
                    phase, 
                    ecc, 
                    overlap
                    );
                if (!acceptance.IsMatch) return null;
                var global = new Transform2D(candidate.Anchor.GlobalPose).Multiply(targetToAnchor).ToArray();
                var alignment = new SampleAlignmentResult 
                { 
                    Success = true, 
                    Method = SampleAlignmentMethod.HalconNcc, 
                    CapturedToSampleTransform = targetToAnchor.ToArray(), 
                    NccScore = phase.RawScore, 
                    EccCorrelation = ecc == null ? double.NaN : ecc.RawScore, 
                    PipelineStage = matcherName, 
                    PreprocessingVariant = "neighbor-overlap-" + direction, 
                    TranslationX = targetToAnchor[0, 2], 
                    TranslationY = targetToAnchor[1, 2], 
                    RotationDeg = Math.Atan2(targetToAnchor[1, 0], targetToAnchor[0, 0]) * 180 / Math.PI, 
                    Scale = 1, 
                    OverlapRatio = overlap 
                };
                AddReport(report, target, alignment, "Recovered from anchor OrderIndex " + candidate.Anchor.OrderIndex + " via " + direction);
                return TileWorkflowState.From(target, global, PoseSource.NeighborAlignment, alignment, acceptance.Reason);
            }
        }

        private static IList<RecoveryCandidate> BuildRecoveryCandidates(
            CapturedImageInfo target, 
            Dictionary<int, TileWorkflowState> solved, 
            IList<CapturedImageInfo> ordered, 
            bool includeSuccessor)
        {
            var result = new List<RecoveryCandidate>();
            TileWorkflowState anchor;
            if (solved.TryGetValue(target.OrderIndex - 1, out anchor) && anchor.IsStitchable) 
                result.Add(new RecoveryCandidate(anchor, "traversal-predecessor"));
            foreach (var s in solved.Values.Where(x => x.IsStitchable && x.OrderIndex != target.OrderIndex && (Math.Abs(x.Row - target.Row) + Math.Abs(x.Column - target.Column) == 1)).OrderBy(x => Math.Abs(x.Row - target.Row) + Math.Abs(x.Column - target.Column)).ThenBy(x => x.OrderIndex))
                if (!result.Any(r => r.Anchor.OrderIndex == s.OrderIndex)) 
                    result.Add(new RecoveryCandidate(s, "solved-grid-neighbor"));
            if (includeSuccessor && solved.TryGetValue(target.OrderIndex + 1, out anchor) && anchor.IsStitchable && !result.Any(r => r.Anchor.OrderIndex == anchor.OrderIndex)) 
                result.Add(new RecoveryCandidate(anchor, "traversal-successor-second-pass"));
            return result;
        }

        private static string DirectionBetween(SampleTileInfo anchor, SampleTileInfo target)
        {
            if (target.Column == anchor.Column + 1 && target.Row == anchor.Row) return "right";
            if (target.Column == anchor.Column - 1 && target.Row == anchor.Row) return "left";
            if (target.Row == anchor.Row + 1 && target.Column == anchor.Column) return "bottom";
            if (target.Row == anchor.Row - 1 && target.Column == anchor.Column) return "top";
            return null;
        }

        private static Rect AnchorRoi(Mat anchor, string direction)
        {
            var w = Math.Max(8, anchor.Cols / 2); var h = Math.Max(8, anchor.Rows / 2);
            if (direction == "right") return new Rect(anchor.Cols - w, 0, w, anchor.Rows);
            if (direction == "left") return new Rect(0, 0, w, anchor.Rows);
            if (direction == "bottom") return new Rect(0, anchor.Rows - h, anchor.Cols, h);
            return new Rect(0, 0, anchor.Cols, h);
        }

        private static Rect TargetRoi(Mat target, string direction)
        {
            var w = Math.Max(8, target.Cols / 2); var h = Math.Max(8, target.Rows / 2);
            if (direction == "right") return new Rect(0, 0, w, target.Rows);
            if (direction == "left") return new Rect(target.Cols - w, 0, w, target.Rows);
            if (direction == "bottom") return new Rect(0, 0, target.Cols, h);
            return new Rect(0, target.Rows - h, target.Cols, h);
        }

        private static Transform2D RoiTransformToFull(Transform2D roiTargetToAnchor, Rect anchorRoi, Rect targetRoi)
        {
            return Transform2D.Translation(anchorRoi.X, anchorRoi.Y).Multiply(roiTargetToAnchor).Multiply(Transform2D.Translation(-targetRoi.X, -targetRoi.Y));
        }

        private static Mat CropCopy(Mat image, Rect roi)
        {
            using (var view = new Mat(image, roi)) return view.Clone();
        }

        private static void AddRecoveryEdge(ProcessingReport report, int anchorOrder, int targetOrder, string direction, string matcher, string reason, double[,] transform, MatchResult phase, MatchResult ecc, double overlap)
        {
            report.RecoveryEdges.Add(new RecoveryEdgeReport { AnchorOrderIndex = anchorOrder, TargetOrderIndex = targetOrder, Direction = direction, Matcher = matcher, Reason = reason, TargetToAnchorTransform = transform, PhaseScore = phase == null ? double.NaN : phase.RawScore, EccCorrelation = ecc == null ? double.NaN : ecc.RawScore, OverlapRatio = overlap });
        }

        private static Bitmap LoadBitmap(string p) { return new Bitmap(p); }
        private static SampleAlignmentOptions ToOptions(AlignStitchConfig c) 
        { 
            return new SampleAlignmentOptions 
            { 
                MinOverlapRatio = c.MinOverlapRatio, 
                MaxAbsRotationDeg = c.MaxAbsRotationDeg, 
                AllowNccOnlyAcceptance = c.AllowNccOnlyAcceptance, 
                NccMinScore = c.NccMinScore, 
                EccMinCorrelation = c.EccMinCorrelation, 
                MaxTranslationPixels = c.MaxTranslationPixels, 
                MinScale = c.MinScale, 
                MaxScale = c.MaxScale, 
                AllowEccFromExpectedWhenNccFails = c.AllowEccFromExpectedWhenNccFails 
            }; 
        }
        private static MatcherOptions ToMatcherOptions(AlignStitchConfig c) 
        { 
            return new MatcherOptions 
            { 
                PhaseMinResponse = 0.15, 
                MinCorrelation = c.EccMinCorrelation, 
                MinOverlapRatio = c.MinOverlapRatio, 
                MaxAbsRotationDeg = c.MaxAbsRotationDeg, 
                MaxTranslationPixels = c.MaxTranslationPixels, 
                MinScale = c.MinScale, 
                MaxScale = c.MaxScale, 
                PyramidLevels = 3, 
                MaxIterations = 80, 
                Epsilon = 1e-5 
            }; 
        }
        public static double[,] Translation(double x, double y) 
        { 
            return new[,] { { 1d, 0d, x }, { 0d, 1d, y }, { 0d, 0d, 1d } }; 
        }
        public static double[,] Multiply(double[,] a, double[,] b) 
        { 
            var r = new double[3, 3]; 
            for (int y = 0; y < 3; y++) 
                for (int x = 0; x < 3; x++) 
                    for (int k = 0; k < 3; k++) 
                        r[y, x] += a[y, k] * b[k, x]; 
            return r; 
        }
        private static void AddReport(
            ProcessingReport report, 
            CapturedImageInfo cap, 
            SampleAlignmentResult r, 
            string fallback
            ) 
        { 
            report.TileReports.Add(ProcessingTileReport.From(cap, r, fallback)); 
            if (r != null && !string.IsNullOrEmpty(r.Warning)) 
                report.Warnings.Add(TilePrefix(cap) + r.Warning); 
        }
        private static string TilePrefix(CapturedImageInfo cap) { return "OrderIndex " + cap.OrderIndex + " Row " + cap.Row + " Column " + cap.Column + ": "; }

        private sealed class RecoveryCandidate
        {
            public RecoveryCandidate(TileWorkflowState anchor, string priority) { Anchor = anchor; Priority = priority; }
            public TileWorkflowState Anchor { get; private set; }
            public string Priority { get; private set; }
        }
    }
}
