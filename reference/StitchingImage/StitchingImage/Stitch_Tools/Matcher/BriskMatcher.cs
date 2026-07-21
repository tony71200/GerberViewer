using OpenCvSharp;
using OpenCvSharp.Features2D;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    public sealed class BriskMatcher : FeatureMatcher
    {
        public BriskMatcher(StitchingConfig cfg) : base(cfg)
        {
        }

        protected override Feature2D CreateDetector() => BRISK.Create();
        protected override NormTypes DescriptorNorm => NormTypes.Hamming;
        protected override string StageName => "brisk";
    }
}
