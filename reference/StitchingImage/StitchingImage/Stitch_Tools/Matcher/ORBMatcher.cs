// =======================================================
// File: StitchingImage/Stitch_Tools/Matcher/ORBMatcher.cs
// Target: .NET Framework 4.8
// NuGet: OpenCvSharp4, OpenCvSharp4.runtime.win
// =======================================================
using System;
using System.Linq;
using OpenCvSharp.Features2D;
using OpenCvSharp;
using StitchingImage.Stitch_Tools.RobotManager;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    public sealed class ORBMatcher : FeatureMatcher
    {
        public ORBMatcher(StitchingConfig cfg) : base(cfg)
        {
        }

        protected override Feature2D CreateDetector() => ORB.Create(_cfg.OrbNFeatures);
        protected override NormTypes DescriptorNorm => NormTypes.Hamming;
        protected override string StageName => "orb";
    }

    public static class PairTest
    {
        public static void TestOneEdge(OrderComponent comp, StitchingConfig cfg)
        {
            if (comp == null) throw new ArgumentNullException(nameof(comp));
            if (comp.Graph == null || comp.Graph.LinksById == null) throw new ArgumentException("Component has no graph.");

            var byId = comp.Points.ToDictionary(p => p.ImageId);

            int fromId = -1, toId = -1;
            Direction? directionHint = null;

            foreach (var kv in comp.Graph.LinksById)
            {
                if (kv.Value.HNext.HasValue)
                {
                    fromId = kv.Key;
                    toId = kv.Value.HNext.Value;
                    directionHint = Direction.Horizontal;
                    break;
                }

                // [GPT-5.2-Codex] [Change time: 260319] [Allow edge tests to preserve vertical graph orientation instead of relying only on robot deltas]
                if (kv.Value.VNext.HasValue)
                {
                    fromId = kv.Key;
                    toId = kv.Value.VNext.Value;
                    directionHint = Direction.Vertical;
                    break;
                }
            }
            if (fromId < 0)
                throw new InvalidOperationException("No HNext/VNext edge found.");

            var a = byId[fromId];
            var b = byId[toId];

            var dx = b.XRobot - a.XRobot;
            var dy = b.YRobot - a.YRobot;

            using (var matcher = new ORBMatcher(cfg))
            {
                // var res = matcher.MatchPair(a.FilePath, b.FilePath, dx, dy, comp.EstimateDistanceX, comp.EstimateDistanceY);
                var res = matcher.MatchPair(a.FilePath, b.FilePath, dx, dy, comp.EstimateDistanceX, comp.EstimateDistanceY, directionHint);
                try
                {
                    var ev = res.Eval;

                    System.Diagnostics.Debug.WriteLine(
                        $"EDGE {fromId}->{toId} ok={ev.IsMatch} reason={ev.Reason} kpA={ev.NKpA} kpB={ev.NKpB} matches={ev.NMatches} inliers={ev.NInliers} rmse={ev.Rmse:0.###} overlap={ev.OverlapRatio:0.###} accuracy={ev.Accuracy:0.###}");
                }
                finally
                {
                    res.HFullBToA?.Dispose();
                    res.MRigidBToA?.Dispose();
                }
            }
        }
    }
}
