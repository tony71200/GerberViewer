using System;
using System.Collections.Generic;
using GerberViewer.Stitching.RobotManager;
using GerberViewer.Stitching.Alignment;

namespace GerberViewer.Stitching.Models
{
    public enum PoseSource { SampleAlignment, NeighborAlignment, AnchorAdjusted, Interpolated, ExpectedGridOffset, Manual, Excluded, Failed }
    public enum AlignStitchRunStatus { NotStarted, Completed, CompletedWithFallback, CompletedWithExcludedTiles, Cancelled, Failed }

    public sealed class GerberSampleConfig { public int Rows { get; set; } public int Columns { get; set; } public double TileWidth { get; set; } public double TileHeight { get; set; } public StartOrder StartOrder { get; set; } = StartOrder.TopLeftRight; }
    public sealed class AlignStitchConfig { public string InputManifestPath { get; set; } public string CapturedFolderPath { get; set; } public string OutputPath { get; set; } public SampleAlignmentMethod AlignmentMethod { get; set; } = SampleAlignmentMethod.NccThenPyramidEcc; public double NccMinScore { get; set; } = 0.70; public double EccMinCorrelation { get; set; } = 0.80; public double MaxTranslationPixels { get; set; } = 500.0; public double MaxAbsRotationDeg { get; set; } = 8.0; public double MinScale { get; set; } = 0.95; public double MaxScale { get; set; } = 1.05; public double MinOverlapRatio { get; set; } = 0.01; public bool AllowNccOnlyAcceptance { get; set; } public bool AllowEccFromExpectedWhenNccFails { get; set; } = true; public bool EnableNeighborRecovery { get; set; } = true; public bool EnableAnchorInterpolation { get; set; } = true; public bool AllowExpectedGridFallback { get; set; } public bool RequireManualConfirmationForExpectedGrid { get; set; } = true; public int PreviewUpdateInterval { get; set; } = 4; public double MaxPreviewMegapixels { get; set; } = 32.0; public TiffMode TiffMode { get; set; } = TiffMode.Auto; public int BigTiffTileWidth { get; set; } = 512; public int BigTiffTileHeight { get; set; } = 512; }
    public sealed class GerberWorkflowConfig { public GerberSampleConfig Sample { get; set; } = new GerberSampleConfig(); public AlignStitchConfig Alignment { get; set; } = new AlignStitchConfig(); }
    public enum OrderNodeState { Pending, Processing, SampleAlignOk, NeighborAlignOk, AnchorAdjusted, Interpolated, ExpectedGridOffset, Manual, Failed, Excluded, ExpectedOffset }
    public sealed class CapturedImageInfo { public string FilePath { get; set; } public int Row { get; set; } public int Column { get; set; } public int OrderIndex { get; set; } public int Width { get; set; } public int Height { get; set; } public string NaturalSortKey { get; set; } public string SourceMetadata { get; set; } public double RobotX { get; set; } public double RobotY { get; set; } public DateTime CapturedUtc { get; set; } public OrderNodeState State { get; set; } = OrderNodeState.Pending; }
    public sealed class StitchImagePose { public int OrderIndex { get; set; } public int Row { get; set; } public int Column { get; set; } public double X { get; set; } public double Y { get; set; } public double RotationDeg { get; set; } public PoseSource Source { get; set; } }
    public sealed class ProcessingReport { public bool Succeeded { get; set; } public AlignStitchRunStatus RunStatus { get; set; } = AlignStitchRunStatus.NotStarted; public AlignStitchConfig InputConfig { get; set; } public SampleManifest InputManifest { get; set; } public IList<string> Messages { get; set; } = new List<string>(); public IList<string> Warnings { get; set; } = new List<string>(); public IList<StitchImagePose> Poses { get; set; } = new List<StitchImagePose>(); public IList<ProcessingTileReport> TileReports { get; set; } = new List<ProcessingTileReport>(); public IList<RecoveryEdgeReport> RecoveryEdges { get; set; } = new List<RecoveryEdgeReport>(); public string FinalOutputPath { get; set; } public static ProcessingReport Create(AlignStitchConfig config, SampleManifest manifest) { return new ProcessingReport { InputConfig = config, InputManifest = manifest }; } }
}

namespace GerberViewer.Stitching.Models
{
    public enum TiffMode { Auto, StandardTiff, BigTiff }
    public sealed class WorkflowProgress { public WorkflowProgress(int current, int total, CapturedImageInfo image, string stage) { Current = current; Total = total; Image = image; Stage = stage; } public int Current { get; private set; } public int Total { get; private set; } public CapturedImageInfo Image { get; private set; } public string Stage { get; private set; } }
    public sealed class AlignStitchWorkflowResult { public ProcessingReport Report { get; set; } public System.Collections.Generic.IList<TileWorkflowState> States { get; set; } = new System.Collections.Generic.List<TileWorkflowState>(); }
    public sealed class TileWorkflowState
    {
        public int OrderIndex { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public double[,] GlobalPose { get; set; }
        public PoseSource Source { get; set; }
        public string Reason { get; set; }
        public GerberViewer.Stitching.Alignment.SampleAlignmentResult Alignment { get; set; }
        public bool AlignmentSucceeded { get; set; }
        public bool IsFallbackPose { get; set; }
        public bool IsStitchable { get; set; }
        public bool HasValidPose { get { return GerberViewer.Stitching.Alignment.Homography.IsFinite(GlobalPose); } }
        public static TileWorkflowState From(CapturedImageInfo c, double[,] pose, PoseSource source, GerberViewer.Stitching.Alignment.SampleAlignmentResult result, string reason)
        {
            var hasPose = GerberViewer.Stitching.Alignment.Homography.IsFinite(pose);
            var alignmentSucceeded = hasPose && (source == PoseSource.SampleAlignment || source == PoseSource.NeighborAlignment || source == PoseSource.AnchorAdjusted || source == PoseSource.Manual);
            var isFallbackPose = hasPose && (source == PoseSource.ExpectedGridOffset || source == PoseSource.Interpolated);
            var isStitchable = alignmentSucceeded;
            return new TileWorkflowState { OrderIndex = c.OrderIndex, Row = c.Row, Column = c.Column, GlobalPose = pose, Source = source, Alignment = result, Reason = reason, AlignmentSucceeded = alignmentSucceeded, IsFallbackPose = isFallbackPose, IsStitchable = isStitchable };
        }
        public StitchImagePose ToPose() { return new StitchImagePose { OrderIndex = OrderIndex, Row = Row, Column = Column, X = GlobalPose == null ? 0 : GlobalPose[0, 2], Y = GlobalPose == null ? 0 : GlobalPose[1, 2], RotationDeg = GlobalPose == null ? 0 : System.Math.Atan2(GlobalPose[1, 0], GlobalPose[0, 0]) * 180 / System.Math.PI, Source = Source }; }
    }
    public sealed class ManualAlignmentRequest { public CapturedImageInfo Captured { get; set; } public SampleTileInfo SampleTile { get; set; } public double[,] ExpectedGlobalPose { get; set; } public double[,] BestAutomaticCandidate { get; set; } public System.Collections.Generic.IList<string> Diagnostics { get; set; } = new System.Collections.Generic.List<string>(); }
    public sealed class ManualAlignmentResult { public bool Accepted { get; set; } public bool Skipped { get; set; } public bool CancelRun { get; set; } public double[,] GlobalPose { get; set; } public string Notes { get; set; } }
    public sealed class RecoveryEdgeReport { public int AnchorOrderIndex { get; set; } public int TargetOrderIndex { get; set; } public string Direction { get; set; } public string Matcher { get; set; } public string Reason { get; set; } public double[,] TargetToAnchorTransform { get; set; } public double PhaseScore { get; set; } public double EccCorrelation { get; set; } public double OverlapRatio { get; set; } }
    public sealed class ProcessingTileReport { public int OrderIndex { get; set; } public int Row { get; set; } public int Column { get; set; } public PoseSource PoseSource { get; set; } public double NccScore { get; set; } public double EccCorrelation { get; set; } public string PipelineStage { get; set; } public string PreprocessingVariant { get; set; } public string NccFailureReason { get; set; } public string EccFailureReason { get; set; } public string FallbackReason { get; set; } public bool Manual { get; set; } public bool Excluded { get; set; } public string RejectionReason { get; set; } public static ProcessingTileReport From(CapturedImageInfo c, GerberViewer.Stitching.Alignment.SampleAlignmentResult r, string fallback) { return new ProcessingTileReport { OrderIndex = c.OrderIndex, Row = c.Row, Column = c.Column, NccScore = r == null ? double.NaN : r.NccScore, EccCorrelation = r == null ? double.NaN : r.EccCorrelation, PipelineStage = r == null ? null : r.PipelineStage, PreprocessingVariant = r == null ? null : r.PreprocessingVariant, NccFailureReason = r == null ? null : r.NccFailureReason, EccFailureReason = r == null ? null : r.EccFailureReason, FallbackReason = fallback, RejectionReason = r == null ? null : r.RejectionReason }; } }
    public partial class ProcessingReportExtensions { }
}
