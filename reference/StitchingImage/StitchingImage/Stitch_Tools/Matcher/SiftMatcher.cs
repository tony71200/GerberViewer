using System;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    public sealed class SiftMatcher : FeatureMatcher
    {
        public SiftMatcher(StitchingConfig cfg) : base(cfg)
        {
        }

        protected override Feature2D CreateDetector()
        {
            try
            {
                return SIFT.Create();
            }
            catch (Exception ex)
            {
                Logger.Error("SIFT detector initialization failed.", ex);
                throw;
            }
        }

        protected override NormTypes DescriptorNorm => NormTypes.L2;
        protected override string StageName => "sift";
    }
}
