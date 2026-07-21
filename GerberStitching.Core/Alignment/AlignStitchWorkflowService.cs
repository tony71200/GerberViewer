using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GerberViewer.Stitching.Models;

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

        public AlignStitchWorkflowService(Func<ISampleAligner> alignerFactory, IManualAlignmentProvider manualProvider = null)
        {
            _alignerFactory = alignerFactory ?? (() => new NccThenPyramidEccSampleAligner());
            _manualProvider = manualProvider;
        }

        public Task<AlignStitchWorkflowResult> RunAsync(AlignStitchConfig config, SampleManifest manifest, IList<CapturedImageInfo> captured, IProgress<WorkflowProgress> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => RunCore(config ?? new AlignStitchConfig(), manifest, captured, progress, cancellationToken), cancellationToken);
        }

        private AlignStitchWorkflowResult RunCore(AlignStitchConfig config, SampleManifest manifest, IList<CapturedImageInfo> captured, IProgress<WorkflowProgress> progress, CancellationToken ct)
        {
            ValidateInputs(manifest, captured);
            var report = ProcessingReport.Create(config, manifest);
            report.Messages.Add("Transform contract: CapturedToSampleTransform maps CapturedImageLocalPixels to SampleTileLocalPixels; GlobalPose maps CapturedImageLocalPixels to ProcessedSampleGlobalPixels.");
            var solved = new Dictionary<int, TileWorkflowState>();
            var tileByOrder = manifest.Tiles.ToDictionary(t => t.OrderIndex);
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
                    if (!state.HasValidPose)
                        state = Recover(config, tile, cap, solved, ordered, report, ct);
                    solved[cap.OrderIndex] = state;
                    report.Poses.Add(state.ToPose());
                }
            }

            var alignedCount = solved.Values.Count(v => v.Source == PoseSource.SampleAlignment || v.Source == PoseSource.Manual);
            report.Succeeded = alignedCount == ordered.Count && ordered.Count > 0;
            if (!report.Succeeded)
                report.Warnings.Add("Production output blocked: not every captured image has a verified sample/manual alignment pose.");
            return new AlignStitchWorkflowResult { Report = report, States = solved.Values.OrderBy(v => v.OrderIndex).ToList() };
        }

        private static void ValidateInputs(SampleManifest manifest, IList<CapturedImageInfo> captured)
        {
            var validation = SampleManifestValidator.Validate(manifest, true);
            if (!validation.IsValid) throw new InvalidOperationException("Invalid sample manifest: " + string.Join("; ", validation.Errors));
            var tileByOrder = manifest.Tiles.ToDictionary(t => t.OrderIndex);
            if (captured == null || captured.Count != manifest.Tiles.Count) throw new InvalidOperationException("Captured image count must equal manifest tile count.");
            foreach (var c in captured)
            {
                if (!tileByOrder.ContainsKey(c.OrderIndex)) throw new InvalidOperationException("Captured image OrderIndex has no manifest tile: " + c.OrderIndex);
                if (string.IsNullOrWhiteSpace(c.FilePath) || !File.Exists(c.FilePath)) throw new FileNotFoundException("Captured image missing for OrderIndex " + c.OrderIndex, c.FilePath);
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
                    var global = Multiply(Translation(tile.ExpectedX, tile.ExpectedY), r.CapturedToSampleTransform);
                    return TileWorkflowState.From(cap, global, PoseSource.SampleAlignment, r, null);
                }
                return TileWorkflowState.From(cap, null, PoseSource.Failed, r, r.RejectionReason);
            }
        }

        private TileWorkflowState Recover(AlignStitchConfig config, SampleTileInfo tile, CapturedImageInfo cap, Dictionary<int, TileWorkflowState> solved, IList<CapturedImageInfo> ordered, ProcessingReport report, CancellationToken ct)
        {
            report.Warnings.Add(TilePrefix(cap) + "Neighbor recovery unavailable: production camera-to-camera matcher is not implemented; expected-grid substitution is blocked.");
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
            return TileWorkflowState.From(cap, null, PoseSource.Failed, null, "No verified direct alignment and no implemented recovery/manual pose.");
        }

        private static Bitmap LoadBitmap(string p) { return new Bitmap(p); }
        private static SampleAlignmentOptions ToOptions(AlignStitchConfig c) { return new SampleAlignmentOptions { MinOverlapRatio = c.MinOverlapRatio, MaxAbsRotationDeg = c.MaxAbsRotationDeg, AllowNccOnlyAcceptance = c.AllowNccOnlyAcceptance, NccMinScore = c.NccMinScore, EccMinCorrelation = c.EccMinCorrelation, MaxTranslationPixels = c.MaxTranslationPixels, MinScale = c.MinScale, MaxScale = c.MaxScale, AllowEccFromExpectedWhenNccFails = c.AllowEccFromExpectedWhenNccFails }; }
        public static double[,] Translation(double x, double y) { return new[,] { { 1d, 0d, x }, { 0d, 1d, y }, { 0d, 0d, 1d } }; }
        public static double[,] Multiply(double[,] a, double[,] b) { var r = new double[3,3]; for (int y=0;y<3;y++) for(int x=0;x<3;x++) for(int k=0;k<3;k++) r[y,x]+=a[y,k]*b[k,x]; return r; }
        private static void AddReport(ProcessingReport report, CapturedImageInfo cap, SampleAlignmentResult r, string fallback) { report.TileReports.Add(ProcessingTileReport.From(cap, r, fallback)); if (r != null && !string.IsNullOrEmpty(r.Warning)) report.Warnings.Add(TilePrefix(cap) + r.Warning); }
        private static string TilePrefix(CapturedImageInfo cap) { return "OrderIndex " + cap.OrderIndex + " Row " + cap.Row + " Column " + cap.Column + ": "; }
    }
}
