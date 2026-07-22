using System;
using System.Collections.Generic;
using GerberViewer.Stitching.Transforms;

namespace GerberViewer.Stitching.Matching
{
    public enum MatchFailureReason
    {
        None,
        InvalidInput,
        InvalidRoi,
        SizeMismatch,
        LowTexture,
        ResponseBelowThreshold,
        CorrelationBelowThreshold,
        GeometryRejected,
        NonFiniteTransform,
        Cancelled,
        RuntimeFailure
    }

    public sealed class MatchResult
    {
        public bool Success { get; set; }
        public Transform2D MovingToReferenceTransform { get; set; }
        public double TranslationX { get; set; }
        public double TranslationY { get; set; }
        public double RotationDeg { get; set; }
        public double Scale { get; set; } = 1.0;
        public double RawScore { get; set; }
        public double NormalizedConfidence { get; set; }
        public double OverlapRatio { get; set; }
        public string MatcherName { get; set; }
        public MatchFailureReason FailureReason { get; set; }
        public string FailureMessage { get; set; }
        public string Warning { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public IDictionary<string, string> Diagnostics { get; private set; } = new Dictionary<string, string>();

        public static MatchResult Failed(string matcherName, MatchFailureReason reason, string message)
        {
            return new MatchResult { MatcherName = matcherName, FailureReason = reason, FailureMessage = message, RawScore = double.NaN, NormalizedConfidence = 0d, OverlapRatio = 0d, Scale = 1d };
        }
    }
}
