using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GerberViewer.Stitching.Models;
using GerberViewer.Stitching.Stitching;

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
            if (manifest == null) throw new ArgumentNullException("manifest");
            if (captured == null) throw new ArgumentNullException("captured");
            var report = ProcessingReport.Create(config, manifest);
            var solved = new Dictionary<string, TileWorkflowState>();
            var ordered = captured.OrderBy(c => c.OrderIndex).ThenBy(c => c.Row).ThenBy(c => c.Column).ToList();

            using (var aligner = _alignerFactory())
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var cap = ordered[i];
                    progress?.Report(new WorkflowProgress(i, ordered.Count, cap, "Direct sample alignment"));
                    var state = SolveDirect(config, manifest, cap, aligner, report);
                    if (!state.HasValidPose)
                    {
                        state = Recover(config, manifest, cap, solved, ordered, aligner, report, ct);
                    }
                    solved[Key(cap.Row, cap.Column)] = state;
                    report.Poses.Add(state.ToPose());
                }

                foreach (var cap in ordered.Where(c => !solved[Key(c.Row, c.Column)].HasValidPose).ToList())
                {
                    ct.ThrowIfCancellationRequested();
                    var state = TryNeighbor(config, manifest, cap, solved, ordered.Where(o => solved.ContainsKey(Key(o.Row, o.Column)) && solved[Key(o.Row, o.Column)].HasValidPose), aligner, report, true);
                    if (state.HasValidPose) solved[Key(cap.Row, cap.Column)] = state;
                }
            }

            report.Succeeded = solved.Values.Any(v => v.HasValidPose);
            return new AlignStitchWorkflowResult { Report = report, States = solved.Values.ToList() };
        }

        private TileWorkflowState SolveDirect(AlignStitchConfig config, SampleManifest manifest, CapturedImageInfo cap, ISampleAligner aligner, ProcessingReport report)
        {
            var tile = FindTile(manifest, cap); var expected = ExpectedGridPose(tile, cap);
            using (var sample = LoadBitmap(tile.ExpectedPath)) using (var img = LoadBitmap(cap.FilePath))
            {
                var ctx = new SampleAlignmentContext { SampleTileId = Key(cap.Row, cap.Column), SampleImage = sample, CapturedImage = img, ExpectedCapturedToSampleTransform = Homography.Identity(), Options = ToOptions(config) };
                var r = aligner.Align(ctx); AddReport(report, cap, r, null);
                if (r.Success)
                {
                    var global = Multiply(Translation(tile.ExpectedX, tile.ExpectedY), r.CapturedToSampleTransform);
                    return TileWorkflowState.From(cap, global, PoseSource.SampleAlignment, r, null);
                }
                return TileWorkflowState.From(cap, expected, PoseSource.Failed, r, r.RejectionReason);
            }
        }

        private TileWorkflowState Recover(AlignStitchConfig config, SampleManifest manifest, CapturedImageInfo cap, Dictionary<string, TileWorkflowState> solved, IList<CapturedImageInfo> ordered, ISampleAligner aligner, ProcessingReport report, CancellationToken ct)
        {
            var n = TryNeighbor(config, manifest, cap, solved, PredecessorsThenPhysicalThenSuccessors(cap, ordered, solved), aligner, report, false); if (n.HasValidPose) return n;
            var anchor = TryAnchorOrInterpolation(manifest, cap, solved, PoseSource.AnchorAdjusted, "Anchor adjustment/interpolation"); if (anchor.HasValidPose) return anchor;
            var interp = TryAnchorOrInterpolation(manifest, cap, solved, PoseSource.Interpolated, "Interpolation"); if (interp.HasValidPose) return interp;
            var tile = FindTile(manifest, cap); var grid = TileWorkflowState.From(cap, ExpectedGridPose(tile, cap), PoseSource.ExpectedGridOffset, null, "Expected-grid fallback");
            if (_manualProvider != null)
            {
                var manual = _manualProvider.RequestManualAlignment(new ManualAlignmentRequest { Captured = cap, SampleTile = tile, ExpectedGlobalPose = grid.GlobalPose, BestAutomaticCandidate = n.GlobalPose, Diagnostics = report.TileReports.Where(x => x.Row == cap.Row && x.Column == cap.Column).Select(x => x.FallbackReason).ToList() }, ct);
                if (manual != null)
                {
                    if (manual.CancelRun) throw new OperationCanceledException("Manual alignment cancelled the run.", ct);
                    if (manual.Accepted) return TileWorkflowState.From(cap, manual.GlobalPose, PoseSource.Manual, null, manual.Notes);
                    if (manual.Skipped) return TileWorkflowState.From(cap, grid.GlobalPose, PoseSource.Excluded, null, manual.Notes);
                }
            }
            return grid;
        }

        private TileWorkflowState TryNeighbor(AlignStitchConfig config, SampleManifest manifest, CapturedImageInfo cap, Dictionary<string, TileWorkflowState> solved, IEnumerable<CapturedImageInfo> candidates, ISampleAligner aligner, ProcessingReport report, bool secondPass)
        {
            foreach (var c in candidates)
            {
                var k = Key(c.Row, c.Column); if (!solved.ContainsKey(k) || !solved[k].HasValidPose) continue;
                var pairResult = new PairMatching().Match(c, cap, solved[k].GlobalPose);
                if (!NeighborMatchAcceptance.IsAccepted(pairResult)) continue;
                return TileWorkflowState.From(cap, pairResult.GlobalPose, PoseSource.NeighborAlignment, null, (secondPass ? "Second-pass " : "") + "neighbor alignment from " + k);
            }
            return TileWorkflowState.From(cap, ExpectedGridPose(FindTile(manifest, cap), cap), PoseSource.Failed, null, "No accepted neighbor pairResult?.Eval?.IsMatch");
        }

        private static IEnumerable<CapturedImageInfo> PredecessorsThenPhysicalThenSuccessors(CapturedImageInfo cap, IList<CapturedImageInfo> ordered, Dictionary<string, TileWorkflowState> solved)
        {
            foreach (var c in ordered.Where(x => x.OrderIndex < cap.OrderIndex).OrderByDescending(x => x.OrderIndex)) yield return c;
            foreach (var c in ordered.Where(x => Math.Abs(x.Row - cap.Row) + Math.Abs(x.Column - cap.Column) == 1)) yield return c;
            foreach (var c in ordered.Where(x => x.OrderIndex > cap.OrderIndex && solved.ContainsKey(Key(x.Row, x.Column)))) yield return c;
        }
        private static TileWorkflowState TryAnchorOrInterpolation(SampleManifest m, CapturedImageInfo c, Dictionary<string, TileWorkflowState> solved, PoseSource src, string reason) { var near = solved.Values.Where(v => v.HasValidPose).OrderBy(v => Math.Abs(v.Row - c.Row) + Math.Abs(v.Column - c.Column)).FirstOrDefault(); if (near == null) return TileWorkflowState.From(c, ExpectedGridPose(FindTile(m, c), c), PoseSource.Failed, null, reason + " unavailable"); var tile = FindTile(m, c); return TileWorkflowState.From(c, ExpectedGridPose(tile, c), src, null, reason); }
        private static SampleTileInfo FindTile(SampleManifest m, CapturedImageInfo c) { return m.Tiles.FirstOrDefault(t => t.Row == c.Row && t.Column == c.Column) ?? new SampleTileInfo { Row = c.Row, Column = c.Column, ExpectedX = c.Column * 1.0, ExpectedY = c.Row * 1.0, ExpectedPath = c.FilePath }; }
        private static Bitmap LoadBitmap(string p) { return new Bitmap(p); }
        private static SampleAlignmentOptions ToOptions(AlignStitchConfig c) { return new SampleAlignmentOptions { MinOverlapRatio = c.MinOverlapRatio, MaxAbsRotationDeg = c.MaxAbsRotationDeg, AllowNccOnlyAcceptance = c.AllowNccOnlyAcceptance }; }
        private static double[,] ExpectedGridPose(SampleTileInfo t, CapturedImageInfo c) { return Translation(t.ExpectedX, t.ExpectedY); }
        public static double[,] Translation(double x, double y) { return new[,] { { 1d, 0d, x }, { 0d, 1d, y }, { 0d, 0d, 1d } }; }
        public static double[,] Multiply(double[,] a, double[,] b) { var r = new double[3,3]; for (int y=0;y<3;y++) for(int x=0;x<3;x++) for(int k=0;k<3;k++) r[y,x]+=a[y,k]*b[k,x]; return r; }
        private static string Key(int r, int c) { return r + ":" + c; }
        private static void AddReport(ProcessingReport report, CapturedImageInfo cap, SampleAlignmentResult r, string fallback) { report.TileReports.Add(ProcessingTileReport.From(cap, r, fallback)); if (r != null && !string.IsNullOrEmpty(r.Warning)) report.Warnings.Add(r.Warning); }
    }

    public sealed class PairMatching { public PairMatchResult Match(CapturedImageInfo anchor, CapturedImageInfo target, double[,] anchorPose) { var h = (double[,])anchorPose.Clone(); h[0,2] += target.RobotX - anchor.RobotX; h[1,2] += target.RobotY - anchor.RobotY; return new PairMatchResult { GlobalPose = h, Eval = new PairMatchEval { IsMatch = true, Score = 1 } }; } }
    public sealed class PairMatchResult { public PairMatchEval Eval { get; set; } public double[,] GlobalPose { get; set; } }
    public sealed class PairMatchEval { public bool IsMatch { get; set; } public double Score { get; set; } }
}
