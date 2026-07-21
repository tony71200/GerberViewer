using System;
using OpenCvSharp;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    public sealed class ManualMatcher : PairMatching
    {
        public ManualMatcher(StitchingConfig cfg) : base(cfg) { }

        public override PairResult MatchPair(string imgAPath, string imgBPath, double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null)
        {
            // [GPT-5.2-Codex] [Change time: 260319] [Keep manual matcher aligned with graph direction hints before falling back to robot deltas]
            // var direction = directionHint ?? DirectionFromRobotDelta(dyRobot, dxRobot);
            var direction = ResolveMatchDirection(directionHint, dxRobot, dyRobot);
            var (tx, ty) = direction == Direction.Horizontal
                ? (_cfg.ManualOffsetHorizontalTx, _cfg.ManualOffsetHorizontalTy)
                : (_cfg.ManualOffsetVerticalTx, _cfg.ManualOffsetVerticalTy);

            var theta = 0.0;
            var mRigid = BuildRigid(theta, tx, ty);
            var hFull = ToHomography(mRigid);

            return new PairResult
            {
                HFullBToA = hFull,
                MRigidBToA = mRigid,
                DThetaRad = theta,
                Tx = tx,
                Ty = ty,
                Eval = new PairEval
                {
                    IsMatch = true,
                    Reason = "manual",
                    NKpA = 0,
                    NKpB = 0,
                    NMatches = 0,
                    NInliers = 0,
                    InlierRatio = 0,
                    Rmse = 0,
                    OverlapRatio = 0,
                    Accuracy = 1.0
                }
            };
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Retain original robot-delta helper as commented history while base class now owns the fallback]
//        private static Direction DirectionFromRobotDelta(double dy, double dx)
//            => (Math.Abs(dx) >= Math.Abs(dy)) ? Direction.Horizontal : Direction.Vertical;

        private static Mat BuildRigid(double thetaRad, double tx, double ty)
        {
            var cos = Math.Cos(thetaRad);
            var sin = Math.Sin(thetaRad);
            var m = new Mat(2, 3, MatType.CV_64FC1);
            m.Set<double>(0, 0, cos);
            m.Set<double>(0, 1, -sin);
            m.Set<double>(0, 2, tx);
            m.Set<double>(1, 0, sin);
            m.Set<double>(1, 1, cos);
            m.Set<double>(1, 2, ty);
            return m;
        }

        private static Mat ToHomography(Mat mRigid)
        {
            var h = Mat.Eye(3, 3, MatType.CV_64FC1).ToMat();
            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    h.Set(r, c, mRigid.At<double>(r, c));
                }
            }
            return h;
        }
    }
}
