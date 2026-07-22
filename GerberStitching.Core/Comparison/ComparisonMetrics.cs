namespace GerberViewer.Stitching.Comparison
{
    public sealed class ComparisonMetrics
    {
        public double ValidOverlapRatio { get; set; }
        public double NormalizedCrossCorrelation { get; set; }
        public double BinaryMaskIoU { get; set; }
        public double EdgeOverlap { get; set; }
        public double DistanceTransformError { get; set; }
    }
}
