namespace GerberViewer.Stitching.Matching
{
    public enum EccMotionModel
    {
        Translation,
        Euclidean,
        Affine
    }

    public sealed class MatcherOptions
    {
        public double PhaseMinResponse { get; set; } = 0.20;
        public double MinTextureStdDev { get; set; } = 2.0;
        public double MinOverlapRatio { get; set; } = 0.10;
        public double MaxTranslationPixels { get; set; } = 10000.0;
        public EccMotionModel EccMotionModel { get; set; } = EccMotionModel.Euclidean;
        public int PyramidLevels { get; set; } = 3;
        public int MaxIterations { get; set; } = 100;
        public double Epsilon { get; set; } = 1e-5;
        public double MinCorrelation { get; set; } = 0.70;
        public double MaxAbsRotationDeg { get; set; } = 15.0;
        public double MinScale { get; set; } = 0.90;
        public double MaxScale { get; set; } = 1.10;
        public string PreprocessingVariant { get; set; } = "default";
        public int NccNumLevels { get; set; } = 4;
        public double NccAngleStartRad { get; set; } = -0.08726646259971647;
        public double NccAngleExtentRad { get; set; } = 0.17453292519943295;
        public double NccAngleStepRad { get; set; } = 0.017453292519943295;
        public string NccMetric { get; set; } = "use_polarity";
        public double NccMinScore { get; set; } = 0.70;
        public int NccMaxMatches { get; set; } = 1;
        public double NccMaxOverlap { get; set; } = 0.5;
        public string NccSubPixel { get; set; } = "true";
    }
}
