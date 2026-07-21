using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    public sealed class CoarseFineMatcher: PairMatching
    {
        public CoarseFineMatcher(StitchingConfig cfg) : base(cfg) { }

        public override PairResult MatchPair(string imgAPath, string imgBPath, double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null)
        {
            var coarsecfg = _cfg.Clone();
            coarsecfg.Method = Method.Orb;
            coarsecfg.WorkMegapix = _cfg.CoarseWorkMegapix > 0 ? _cfg.CoarseWorkMegapix : _cfg.WorkMegapix;
            coarsecfg.EnforceRobotDirection = false;
            coarsecfg.PreferPerpOffsetConstraint = false;

            var finecfg = _cfg.Clone();
            finecfg.Method = Method.Orb;
            finecfg.WorkMegapix = _cfg.FineWorkMegapix > 0 ? _cfg.FineWorkMegapix : _cfg.WorkMegapix;
            finecfg.EnforceRobotDirection = true;
            finecfg.PreferPerpOffsetConstraint = true;

            PairResult coarseRst;
            using (var coarse = new ORBMatcher(coarsecfg))
            {
                coarseRst = coarse.MatchPair(imgAPath, imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint);
            }
            if (coarseRst?.Eval == null || coarseRst.Eval.IsMatch) return coarseRst;

            PairResult fineRst;
            using (var fine = new ORBMatcher(finecfg))
            {
                fineRst = fine.MatchPair(imgAPath,imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint);
            }
            if (fineRst?.Eval == null || !fineRst.Eval.IsMatch)
            {
                DisposePairResult(coarseRst);
                return fineRst;
            }
            DisposePairResult(fineRst);
            return coarseRst;
        }

        public override MatchPairDebugResult MatchPairWithDebug(string imgAPath, string imgBPath, double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null)
        {
            var coarsecfg = _cfg.Clone();
            coarsecfg.Method = Method.Orb;
            coarsecfg.WorkMegapix = _cfg.CoarseWorkMegapix > 0 ? _cfg.CoarseWorkMegapix : _cfg.WorkMegapix;
            coarsecfg.EnforceRobotDirection = false;
            coarsecfg.PreferPerpOffsetConstraint = false;

            var finecfg = _cfg.Clone();
            finecfg.Method = Method.Orb;
            finecfg.WorkMegapix = _cfg.FineWorkMegapix > 0 ? _cfg.FineWorkMegapix : _cfg.WorkMegapix;
            finecfg.EnforceRobotDirection = true;
            finecfg.PreferPerpOffsetConstraint = true;

            MatchPairDebugResult coarseRst;
            using (var coarse = new ORBMatcher(coarsecfg))
            {
                coarseRst = coarse.MatchPairWithDebug(imgAPath, imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint);
                if (coarseRst?.DebugInfo != null)
                    coarseRst.DebugInfo.Stage = "coarse";
            }
            if (coarseRst?.Result?.Eval == null || coarseRst.Result.Eval.IsMatch) return coarseRst;

            MatchPairDebugResult fineRst;
            using (var fine = new ORBMatcher(finecfg))
            {
                fineRst = fine.MatchPairWithDebug(imgAPath, imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint);
                if (fineRst?.DebugInfo != null)
                    fineRst.DebugInfo.Stage = "fine";
            }
            if (fineRst?.Result?.Eval == null || !fineRst.Result.Eval.IsMatch)
            {
                DisposeMatchDebug(coarseRst);
                return fineRst;
            }

            DisposeMatchDebug(fineRst);
            return coarseRst;
        }

        private static void DisposePairResult(PairResult result)
        {
            if (result == null) return;
            result.HFullBToA?.Dispose();
            result.MRigidBToA?.Dispose();
        }

        private static void DisposeMatchDebug(MatchPairDebugResult result)
        {
            if (result == null) return;
            DisposePairResult(result.Result);
            result.DebugInfo?.Dispose();
        }
    }
}
