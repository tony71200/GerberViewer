using System;
using System.Reflection;
using GerberViewer.Stitching.Matching;
using GerberViewer.Stitching.Transforms;

namespace GerberViewer.Stitching.Alignment
{
    public sealed class NeighborAcceptanceResult
    {
        public bool IsMatch { get; set; }
        public string Reason { get; set; }
    }

    public static class NeighborMatchAcceptance
    {
        public static NeighborAcceptanceResult Validate(MatchResult coarse, MatchResult refined, Transform2D targetToAnchor, double expectedTranslationX, double expectedTranslationY, MatcherOptions options, double overlapRatio)
        {
            options = options ?? new MatcherOptions();
            var result = refined != null && refined.Success ? refined : coarse;
            if (result == null || !result.Success) return Reject("No successful neighbor matcher result.");
            if (targetToAnchor == null) return Reject("TargetToAnchorTransform is missing.");
            if (overlapRatio < options.MinOverlapRatio) return Reject("OverlapRatio below MinOverlapRatio.");
            var matrix = targetToAnchor.ToArray();
            for (int r = 0; r < 3; r++) for (int c = 0; c < 3; c++) if (double.IsNaN(matrix[r, c]) || double.IsInfinity(matrix[r, c])) return Reject("Pair transform contains non-finite values.");
            if (coarse == null || !coarse.Success || coarse.RawScore < options.PhaseMinResponse) return Reject("Phase correlation score below threshold.");
            if (refined != null && refined.Success && refined.RawScore < options.MinCorrelation) return Reject("ECC correlation below threshold.");
            var rotationDeg = Math.Atan2(matrix[1, 0], matrix[0, 0]) * 180.0 / Math.PI;
            if (Math.Abs(rotationDeg) > options.MaxAbsRotationDeg) return Reject("Pair rotation exceeds MaxAbsRotationDeg.");
            var dx = matrix[0, 2] - expectedTranslationX;
            var dy = matrix[1, 2] - expectedTranslationY;
            if (Math.Sqrt(dx * dx + dy * dy) > options.MaxTranslationPixels) return Reject("Pair translation deviation exceeds MaxTranslationPixels.");
            return new NeighborAcceptanceResult { IsMatch = true, Reason = "Accepted image-based neighbor match." };
        }

        public static bool IsAccepted(object pairResult)
        {
            if (pairResult == null) return false;
            var eval = pairResult.GetType().GetProperty("Eval", BindingFlags.Instance | BindingFlags.Public)?.GetValue(pairResult, null);
            if (eval == null) return false;
            var isMatch = eval.GetType().GetProperty("IsMatch", BindingFlags.Instance | BindingFlags.Public)?.GetValue(eval, null);
            return isMatch is bool && (bool)isMatch;
        }

        private static NeighborAcceptanceResult Reject(string reason)
        {
            return new NeighborAcceptanceResult { IsMatch = false, Reason = reason };
        }
    }
}
