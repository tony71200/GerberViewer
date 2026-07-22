using OpenCvSharp;
using GerberViewer.Stitching.Transforms;

namespace GerberViewer.Stitching.Matching
{
    public enum MatchPurpose
    {
        CapturedToSample,
        TargetCapturedToAnchorCaptured,
        ManualPreview,
        SyntheticTest
    }

    public sealed class MatchRequest
    {
        public Mat ReferenceImage { get; set; }
        public Mat MovingImage { get; set; }
        public Mat ReferenceMask { get; set; }
        public Mat MovingMask { get; set; }
        public Rect? ReferenceRoi { get; set; }
        public Rect? MovingRoi { get; set; }
        public Transform2D InitialMovingToReferenceTransform { get; set; }
        public MatcherOptions Options { get; set; } = new MatcherOptions();
        public MatchPurpose Purpose { get; set; }
        public int? OrderIndex { get; set; }
        public string SampleTileId { get; set; }
        public string Context { get; set; }
    }
}
