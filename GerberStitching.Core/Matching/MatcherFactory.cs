using System;

namespace GerberViewer.Stitching.Matching
{
    public interface IMatcherFactory
    {
        IMatcher CreateNccMatcher();
        IMatcher CreateEccMatcher();
    }

    public sealed class MatcherFactory : IMatcherFactory
    {
        public IMatcher CreateNccMatcher()
        {
            return new NCC_HalconMatcher();
        }

        public IMatcher CreateEccMatcher()
        {
            return new EccMatcher();
        }
    }
}
