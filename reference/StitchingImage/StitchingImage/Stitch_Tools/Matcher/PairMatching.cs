// =======================================================
// File: StitchingImage/Stitch_Tools/PairMatching.cs
// Target: .NET Framework 4.8
// NuGet: OpenCvSharp4, OpenCvSharp4.runtime.win
// =======================================================

using System;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
    // public enum Direction { Horizontal, Vertical }
    // public enum Method { Orb, Sift, Brisk, Manual, CoarseFine, PhaseCorr, OrbPharse }
    public enum Direction { Horizontal, Vertical }
    // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
    // public enum Method { Orb, Sift, Brisk, Manual, CoarseFine, PhaseCorr, OrbPharse }
    // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
    // public enum Method { Orb, Sift, Brisk, Manual, CoarseFine, PhaseCorr, OrbPharse, EccMatcher2, PyramidPhaseMatcher }
    public enum Method { Orb, Sift, Brisk, Manual, CoarseFine, PhaseCorr, OrbPharse, EccMatcher, EccMatcher2, PyramidPhaseMatcher }
    public enum EdgeSide { Left, Right, Top, Bottom }
    public enum MatchSideSelectionMode { Feature, PhaseCorr }

    public sealed class PairEval
    {
        public bool IsMatch { get; set; }
        public int NKpA { get; set; }
        public int NKpB { get; set; }
        public int NMatches { get; set; }
        public int NInliers { get; set; }
        public double InlierRatio { get; set; }
        public double Rmse { get; set; }
        public double OverlapRatio { get; set; }
        public double Accuracy { get; set; }
        public string Reason { get; set; } = "ok";
    }

    public sealed class PairResult
    {
        public Mat HFullBToA { get; set; }   // 3x3 CV_64F  (caller disposes)
        public Mat MRigidBToA { get; set; }  // 2x3 CV_64F  (caller disposes)
        public double DThetaRad { get; set; }
        public double Tx { get; set; }
        public double Ty { get; set; }
        public PairEval Eval { get; set; }
    }

    public sealed class MatchDebugInfo : IDisposable
    {
        public Mat RoiA { get; set; }
        public Mat RoiB { get; set; }
        public KeyPoint[] KeypointsA { get; set; }
        public KeyPoint[] KeypointsB { get; set; }
        public DMatch[] RawMatches { get; set; }
        public DMatch[] FilteredMatches { get; set; }
        public DMatch[] SelectedMatches { get; set; }
        public Direction Direction { get; set; }
        public string Stage { get; set; }

        public DMatch[] GetDisplayMatches()
        {
            if (SelectedMatches != null && SelectedMatches.Length > 0)
                return SelectedMatches;
            if (FilteredMatches != null && FilteredMatches.Length > 0)
                return FilteredMatches;
            return RawMatches ?? Array.Empty<DMatch>();
        }

        public void Dispose()
        {
            RoiA?.Dispose();
            RoiB?.Dispose();
        }
    }

    public sealed class MatchPairDebugResult
    {
        public PairResult Result { get; set; }
        public MatchDebugInfo DebugInfo { get; set; }
    }

    public sealed class PairMatchLayout
    {
        public Direction Direction { get; set; }
        public EdgeSide ASide { get; set; }
        public EdgeSide BSide { get; set; }
    }

    public abstract class PairMatching : IDisposable
    {
        protected readonly StitchingConfig _cfg;
        protected Feature2D _detector;

        protected PairMatching(StitchingConfig cfg)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        }

        public abstract PairResult MatchPair(string imgAPath, string  imgBPath, 
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null);

        public virtual MatchPairDebugResult MatchPairWithDebug(string imgAPath, string imgBPath,
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null)
        {
            return new MatchPairDebugResult
            {
                Result = MatchPair(imgAPath, imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint),
                DebugInfo = null
            };
        }

        public static PairMatching CreateMatcher(StitchingConfig cfg)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            switch (cfg.Method)
            {
                case Method.Manual:
                    return new ManualMatcher(cfg);
                case Method.Brisk:
                    return new BriskMatcher(cfg);
                case Method.PhaseCorr:
                    return new PhaseCorrMatcher(cfg);
                // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                // case Method.OrbPharse:
                //     return new ORBPharseMatcher(cfg);
                // case Method.CoarseFine:
                //     return new CoarseFineMatcher(cfg);
                // case Method.Sift:
                //     return new SiftMatcher(cfg);
                case Method.OrbPharse:
                    return new ORBPharseMatcher(cfg);
                // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                // case Method.EccMatcher2:
                //     return new EccMatcher2(cfg);
                case Method.EccMatcher:
                    return new EccMatcher(cfg);
                case Method.EccMatcher2:
                    return new EccMatcher2(cfg);
                case Method.PyramidPhaseMatcher:
                    return new PyramidPhaseMatcher(cfg);
                case Method.CoarseFine:
                    return new CoarseFineMatcher(cfg);
                case Method.Sift:
                    return new SiftMatcher(cfg);
                case Method.Orb:
                default:
                    return new ORBMatcher(cfg);
            }
        }

        public virtual void Dispose()
        {
            _detector?.Dispose();
            _detector = null;
        }

        protected bool IsRobotDirectionConsistent(Direction direction, 
            double dxRobot, double dyRobot, double tx, double ty)
        {
            if (double.IsNaN(dxRobot) || double.IsNaN(dyRobot))
                return true;
            if (direction == Direction.Horizontal)
            {
                if (Math.Abs(tx) < _cfg.MinAbsTranslationForRobotCheck || Math.Abs(dxRobot) < 1e-9)
                    return true;
                
                return Math.Sign(tx) == Math.Sign(dxRobot);
            }

            if (Math.Abs(ty) < _cfg.MinAbsTranslationForRobotCheck || Math.Abs(dyRobot) < 1e-9)
                return true;
            return Math.Sign(ty) == -Math.Sign(dyRobot);
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        // protected (EdgeSide ASide, EdgeSide BSide) DetermineFeatureMatchSides(Direction direction, double dxRobot, double dyRobot)
        // {
        //     if (direction == Direction.Horizontal)
        //         return dxRobot <= 0
        //             ? (EdgeSide.Right, EdgeSide.Left)
        //             : (EdgeSide.Left, EdgeSide.Right);
        //
        //     return dyRobot >= 0
        //         ? (EdgeSide.Top, EdgeSide.Bottom)
        //         : (EdgeSide.Bottom, EdgeSide.Top);
        // }
        //
        // // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        // protected (EdgeSide ASide, EdgeSide BSide) DeterminePhaseCorrSides(Direction direction, double dxRobot, double dyRobot)
        // {
        //     if (direction == Direction.Horizontal)
        //     {
        //         if (double.IsNaN(dxRobot) || dxRobot >= 0)
        //             return (EdgeSide.Right, EdgeSide.Left);
        //
        //         return (EdgeSide.Left, EdgeSide.Right);
        //     }
        //
        //     if (double.IsNaN(dyRobot) || dyRobot < 0)
        //         return (EdgeSide.Bottom, EdgeSide.Top);
        //
        //     return (EdgeSide.Top, EdgeSide.Bottom);
        // }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        protected PairMatchLayout ResolvePairMatchLayout(
            Direction? directionHint,
            double dxRobot,
            double dyRobot,
            MatchSideSelectionMode selectionMode,
            EdgeSide? aSideHint = null,
            EdgeSide? bSideHint = null)
        {
            var direction = ResolveMatchDirection(directionHint, dxRobot, dyRobot);
            var resolvedSides = ResolveMatchSides(direction, dxRobot, dyRobot, selectionMode);

            return new PairMatchLayout
            {
                Direction = direction,
                ASide = aSideHint ?? resolvedSides.ASide,
                BSide = bSideHint ?? resolvedSides.BSide
            };
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        // [GPT-5.2-Codex] [Change time: 260319] [Use graph-provided direction hints first and only infer orientation from robot deltas when the graph has no hint]
        protected Direction ResolveMatchDirection(Direction? directionHint, double dxRobot, double dyRobot)
        {
            return directionHint ?? DirectionFromRobotDelta(dyRobot, dxRobot);
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        protected (EdgeSide ASide, EdgeSide BSide) ResolveMatchSides(
            Direction direction,
            double dxRobot,
            double dyRobot,
            MatchSideSelectionMode selectionMode)
        {
            switch (selectionMode)
            {
                case MatchSideSelectionMode.Feature:
                    if (direction == Direction.Horizontal)
                        return dxRobot <= 0
                            ? (EdgeSide.Right, EdgeSide.Left)
                            : (EdgeSide.Left, EdgeSide.Right);

                    return dyRobot >= 0
                        ? (EdgeSide.Top, EdgeSide.Bottom)
                        : (EdgeSide.Bottom, EdgeSide.Top);

                case MatchSideSelectionMode.PhaseCorr:
                    if (direction == Direction.Horizontal)
                    {
                        if (double.IsNaN(dxRobot) || dxRobot >= 0)
                            return (EdgeSide.Right, EdgeSide.Left);

                        return (EdgeSide.Left, EdgeSide.Right);
                    }

                    if (double.IsNaN(dyRobot) || dyRobot < 0)
                        return (EdgeSide.Bottom, EdgeSide.Top);

                    return (EdgeSide.Top, EdgeSide.Bottom);

                default:
                    throw new ArgumentOutOfRangeException(nameof(selectionMode), selectionMode, null);
            }
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        protected static Direction DirectionFromRobotDelta(double dy, double dx)
            => (Math.Abs(dx) >= Math.Abs(dy)) ? Direction.Horizontal : Direction.Vertical;

        protected double AdjustOverlapFraction(Direction direction, double dxRobot, double dyRobot, double estimateDistX, double estimateDistY)
        {
            var baseFrac = _cfg.RoiMatchFraction;
            if (estimateDistX <= 0 || estimateDistY <=0)
                return baseFrac;

            var actual = direction == Direction.Horizontal ? Math.Abs(dxRobot) : Math.Abs(dyRobot);
            var estimateDistance = direction == Direction.Horizontal ? estimateDistX : estimateDistY;
            if (actual <= 1e-9)
                return baseFrac;

            var scale = estimateDistance / actual;
            scale = Math.Max(0.35, Math.Min(3.0, scale));

            var adjusted = baseFrac * scale;
            return Math.Max(0.05, Math.Min(0.9, adjusted));
        }

        protected PairEval EvaluatePair(
            Direction direction,
            double dxRobot,
            double dyRobot,
            int nKpA,
            int nKpB,
            int nMatches,
            int nInliers,
            double inlierRatio,
            double rmse,
            double overlap,
            double dtheta,
            double tx,
            double ty)
        {
            var ev = new PairEval
            {
                NKpA = nKpA,
                NKpB = nKpB,
                NMatches = nMatches,
                NInliers = nInliers,
                InlierRatio = inlierRatio,
                Rmse = rmse,
                OverlapRatio = overlap,
                Accuracy = CalculateFeatureAccuracy(inlierRatio, rmse, overlap),
                IsMatch = true,
                Reason = "ok"
            };

            if (nInliers < _cfg.MinInliers) { ev.IsMatch = false; ev.Reason = "too_few_inliers"; }
            else if (inlierRatio < _cfg.MinInlierRatio) { ev.IsMatch = false; ev.Reason = "low_inlier_ratio"; }
            else if (rmse > _cfg.MaxRmse) { ev.IsMatch = false; ev.Reason = "high_rmse"; }
            else if (overlap < _cfg.MinOverlapRatio) { ev.IsMatch = false; ev.Reason = "low_overlap"; }
            else if (Math.Abs(dtheta) > DegreesToRadians(_cfg.MaxAbsRotationDeg)) { ev.IsMatch = false; ev.Reason = "rotation_too_large"; }
            else if (_cfg.EnforceRobotDirection && !IsRobotDirectionConsistent(direction, dxRobot, dyRobot, tx, ty))
            {
                ev.IsMatch = false;
                ev.Reason = "robot_direction_mismatch";
            }

            return ev;
        }

        protected Mat ReadForWork(string path, double targetMegapix, out Size originalSize, out double workScale)
        {
            originalSize = EstimateOriginalSize(path);
            if (originalSize.Width <= 0 || originalSize.Height <= 0)
                throw new InvalidOperationException("Cannot estimate image size.");

            var area = (double)originalSize.Width * originalSize.Height;
            var targetPix = Math.Max(1.0, targetMegapix * 1_000_000.0);
            var desiredScale = Math.Sqrt(targetPix / Math.Max(1.0, area));
            desiredScale = Math.Max(1e-6, Math.Min(1.0, desiredScale));

            var (mode, factor) = ImageRead.SelectReducedMode(desiredScale);

            using (var tmp = ImageRead.ReadImage(path, mode))
            {
                if (tmp.Empty())
                    throw new System.IO.FileNotFoundException("Cannot read image", path);

                if (factor == 1)
                    originalSize = new Size(tmp.Cols, tmp.Rows);

                using (var adjusted = new Mat())
                {
                    Cv2.ConvertScaleAbs(tmp, adjusted, _cfg.ConvertAlpha, _cfg.ConvertBeta);

                    var effectiveScaleFromOrig = 1.0 / factor;
                    var residualScale = desiredScale / effectiveScaleFromOrig;
                    residualScale = Math.Max(1e-6, Math.Min(1.0, residualScale));

                    Mat work = Math.Abs(residualScale - 1.0) < 1e-9
                        ? adjusted.Clone()
                        : ResizeByScale(adjusted, residualScale);

                    workScale = effectiveScaleFromOrig * residualScale;
                    return work;
                }
            }
        }

        protected static Size EstimateOriginalSize(string path)
        {
            using (var m8 = ImageRead.ReadImage(path, ImreadModes.ReducedGrayscale8))
            {
                if (!m8.Empty()) return new Size(m8.Cols * 8, m8.Rows * 8);
            }
            using (var m4 = ImageRead.ReadImage(path, ImreadModes.ReducedGrayscale4))
            {
                if (!m4.Empty()) return new Size(m4.Cols * 4, m4.Rows * 4);
            }
            using (var m2 = ImageRead.ReadImage(path, ImreadModes.ReducedGrayscale2))
            {
                if (!m2.Empty()) return new Size(m2.Cols * 2, m2.Rows * 2);
            }
            using (var g = ImageRead.ReadImage(path, ImreadModes.Grayscale))
            {
                if (!g.Empty()) return new Size(g.Cols, g.Rows);
            }
            return new Size(0, 0);
        }

        protected static Mat ResizeByScale(Mat img, double scale)
        {
            var nh = Math.Max(1, (int)Math.Round(img.Rows * scale));
            var nw = Math.Max(1, (int)Math.Round(img.Cols * scale));
            var dst = new Mat();
            Cv2.Resize(img, dst, new Size(nw, nh), 0, 0, InterpolationFlags.Area);
            return dst;
        }

        protected static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;

        protected static double Clamp01(double value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        private double CalculateFeatureAccuracy(double inlierRatio, double rmse, double overlap)
        {
            var inlierScore = Clamp01(inlierRatio);
            var overlapScore = Clamp01(overlap);
            var rmseScore = _cfg.MaxRmse > 1e-9
                ? Clamp01(1.0 - (rmse / _cfg.MaxRmse))
                : 0.0;

            return Clamp01((0.5 * inlierScore) + (0.3 * overlapScore) + (0.2 * rmseScore));
        }

        public static string ToHomoString(PairResult result)
        {
            if (result == null)
                return "tx=?\tty=?\ttheta=?";
            return $"tx:{result.Tx:0.###}\tty:{result.Ty:0.###}\ttheta:{result.DThetaRad:0.####}";
        }

        public static string ToEvalString(PairResult result)
        {
            if (result?.Eval == null)
                return "match=unknown";

            var ev = result.Eval;
            return $"match={(ev.IsMatch ? "ok" : "fail")}\treason={ev.Reason}\tinliers={ev.NInliers}/{Math.Max(1, ev.NMatches)}\tratio={ev.InlierRatio:0.###}\trmse={ev.Rmse:0.###}\toverlap={ev.OverlapRatio:0.###}\taccuracy={ev.Accuracy:0.###}";
        }
    }
}
