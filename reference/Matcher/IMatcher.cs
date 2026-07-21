using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace PCM_Inspection_Demo.Matcher
{
    public abstract class IMatcher
    {
        // Global convention used by all matchers in this repository:
        // input order is (sample/reference, test), and output is T(test -> reference).
        public const string TransformDirection = "T(test -> reference)";

        public sealed class MatchResult
        {
            public bool Success { get; set; }
            // Translation in pixel, interpreted as transform T(test -> reference).
            // +Dx = move test to the right; +Dy = move test downward.
            public double Dx { get; set; }
            public double Dy { get; set; }
            // Positive angle is counter-clockwise, interpreted as T(test -> reference).
            public double AngleDeg { get; set; }
            public double Confidence { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        public abstract string MatcherName { get; }
        // Note: src Image is test Image, dest Image is reference Image.
        // Purpose: Find the best transform T(test -> reference) to align test to reference, and report confidence.
        public abstract MatchResult Run(Bitmap srcImage, Rectangle srcRoi, Bitmap dstImage, Rectangle dstRoi, CancellationToken token);

        public virtual Task<MatchResult> RunAsync(Bitmap srcImage, Rectangle srcRoi, Bitmap dstImage, Rectangle dstRoi, CancellationToken token)
        {
            return Task.Run(() => Run(srcImage, srcRoi, dstImage, dstRoi, token), token);
        }

        public override string ToString() => MatcherName;
        public virtual MatchResult Fail(string message) => new MatchResult { Success = false, Message = message };

        public static string FormatTransformForLog(MatchResult result)
        {
            if (result == null) return $"{TransformDirection} | <null result>";
            return $"{TransformDirection} | dx={result.Dx:0.###}, dy={result.Dy:0.###}, angle={result.AngleDeg:0.###}deg, conf={result.Confidence:0.###}";
        }
    }
}
