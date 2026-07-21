// =======================================================
// File: StitchingImage/Stitch_Tools/Matcher/FeatureMatcher.cs
// Target: .NET Framework 4.8
// NuGet: OpenCvSharp4, OpenCvSharp4.runtime.win
// =======================================================
using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    public abstract class FeatureMatcher : PairMatching
    {
        protected FeatureMatcher(StitchingConfig cfg) : base(cfg)
        {
            _detector = CreateDetector();
        }

        protected abstract Feature2D CreateDetector();
        protected abstract NormTypes DescriptorNorm { get; }
        protected abstract string StageName { get; }

        public override PairResult MatchPair(string imgAPath, string imgBPath,
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null)
        {
            if (string.IsNullOrWhiteSpace(imgAPath)) throw new ArgumentNullException(nameof(imgAPath));
            if (string.IsNullOrWhiteSpace(imgBPath)) throw new ArgumentNullException(nameof(imgBPath));

            // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
            // var direction = directionHint ?? DirectionFromRobotDelta(dyRobot, dxRobot);
            var layout = ResolvePairMatchLayout(directionHint, dxRobot, dyRobot, MatchSideSelectionMode.Feature);
            var direction = layout.Direction;
            var roiFrac = AdjustOverlapFraction(direction, dxRobot, dyRobot, estimateDistX, estimateDistY);

            Size aOrig, bOrig;
            double scaleAWork, scaleBWork;

            using (var imgAWork = ReadForWork(imgAPath, _cfg.WorkMegapix, out aOrig, out scaleAWork))
            using (var imgBWork = ReadForWork(imgBPath, _cfg.WorkMegapix, out bOrig, out scaleBWork))
            {
                // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
                // string aSide, bSide;
                // if (direction == Direction.Horizontal)
                // {
                //     if (dxRobot <= 0) { aSide = "right"; bSide = "left"; }
                //     else { aSide = "left"; bSide = "right"; }
                // }
                // else
                // {
                //     if (dyRobot >= 0) { aSide = "top"; bSide = "bottom"; }
                //     else { aSide = "bottom"; bSide = "top"; }
                // }
                // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
                // var (aSide, bSide) = DetermineFeatureMatchSides(direction, dxRobot, dyRobot);
                var aSide = layout.ASide;
                var bSide = layout.BSide;

                int ax, ay, bx, by;
                Rect aRoiRect, bRoiRect;
                using (var aRoi = ExtractEdgeRoi(imgAWork, aSide, roiFrac, _cfg.RoiMinPx, out ax, out ay, out aRoiRect))
                using (var bRoi = ExtractEdgeRoi(imgBWork, bSide, roiFrac, _cfg.RoiMinPx, out bx, out by, out bRoiRect))
                {
                    KeyPoint[] kpA, kpB;

                    using (var desA = new Mat())
                    using (var desB = new Mat())
                    {
                        DetectAndCompute(_detector, aRoi, out kpA, desA);
                        DetectAndCompute(_detector, bRoi, out kpB, desB);

                        var matches = MatchKnn2(desA, desB, DescriptorNorm, _cfg.RatioTest, _cfg.AllowCloseSecondBest, _cfg.CloseDiff);
                        if (matches.Count < 4)
                            return Fail("too_few_matches", kpA.Length, kpB.Length, matches.Count);

                        var filter = FilterMatchesByDirection(matches, kpA, kpB, ax, ay, bx, by, direction, scaleAWork);

                        var best = filter.Count > 4
                            ? filter.Take(Math.Max(4, _cfg.MaxHomoMatches)).ToArray()
                            : matches.Take(Math.Max(4, _cfg.MaxHomoMatches)).ToArray();

                        var dstWork = new Point2f[best.Length]; // A coords (work)
                        var srcWork = new Point2f[best.Length]; // B coords (work)

                        for (int i = 0; i < best.Length; i++)
                        {
                            var m = best[i];
                            var pa = kpA[m.QueryIdx].Pt;
                            var pb = kpB[m.TrainIdx].Pt;

                            dstWork[i] = new Point2f(pa.X + ax, pa.Y + ay);
                            srcWork[i] = new Point2f(pb.X + bx, pb.Y + by);
                        }

                        var ransacThrWork = _cfg.RansacThresh / Math.Max(1e-9, scaleAWork);
                        // FindHomography: use Nx2 mats to avoid 1xN ambiguity
                        using (var srcMat = CreatePointMatNx2(srcWork))
                        using (var dstMat = CreatePointMatNx2(dstWork))
                        using (var inlierMask = new Mat())
                        using (var hWork = Cv2.FindHomography(
                                   srcMat,
                                   dstMat,
                                   HomographyMethods.Ransac,
                                   ransacThrWork,
                                   inlierMask,
                                   _cfg.RansacMaxIters,
                                   _cfg.RansacConf))
                        {
                            if (hWork.Empty() || inlierMask.Empty())
                                return Fail("find_homography_failed", kpA.Length, kpB.Length, matches.Count);

                            using (var hWork64 = EnsureMat64F(hWork))
                            {
                                NormalizeHomographyInPlace(hWork64);

                                // ECC refinement removed: FindTransformECC is very sensitive to low-texture/low-overlap
                                // regions and can throw when the warp degenerates or the ROI has insufficient signal.
                                var hWorkForFull = hWork64;
                                var hFull = HomographyFullFromScaled(hWorkForFull, scaleAWork, scaleBWork);
                                //TryEstimateSeamMask(imgAWork, imgBWork, hWorkForFull);

                                var nInliers = CountInliersAnyShape(inlierMask);
                                var inlierRatio = nInliers / (double)Math.Max(1, inlierMask.Rows);

                                var srcFullPts = srcWork.Select(p => new Point2f(p.X / (float)scaleBWork, p.Y / (float)scaleBWork)).ToArray();
                                var dstFullPts = dstWork.Select(p => new Point2f(p.X / (float)scaleAWork, p.Y / (float)scaleAWork)).ToArray();

                                var rmse = ComputeRmse(srcFullPts, dstFullPts, hFull, inlierMask);
                                var overlap = OverlapRatio(aOrig, bOrig, hFull);

                                var (dtheta, tx, ty) = PoseFromHomography(hFull);
                                var ev = EvaluatePair(direction, dxRobot, dyRobot, kpA.Length, kpB.Length, matches.Count, nInliers, inlierRatio, rmse, overlap, dtheta, tx, ty);

                                Mat mRigid;
                                if (ev.IsMatch)
                                {
                                    mRigid = BuildRigid(dtheta, tx, ty);
                                }
                                else
                                {
                                    var rigid = RigidFromInliersAnyShape(srcFullPts, dstFullPts, inlierMask);
                                    mRigid = rigid.MRigid2x3;
                                    dtheta = rigid.DThetaRad;
                                    tx = rigid.Tx;
                                    ty = rigid.Ty;
                                }

                                if (!ev.IsMatch)
                                    Logger.Warning($"[MATCHING FAIL] {ev.Reason} tx={tx:0.###} ty={ty:0.###} theta={dtheta:0.####}");
                                Logger.Info($"[MATCHING] tx={tx:0.###} ty={ty:0.###} theta={dtheta:0.####}");
                                return new PairResult
                                {
                                    HFullBToA = hFull,   // caller disposes
                                    MRigidBToA = mRigid, // caller disposes
                                    DThetaRad = dtheta,
                                    Tx = tx,
                                    Ty = ty,
                                    Eval = ev
                                };
                            }
                        }
                    }
                }
            }
        }

        public override MatchPairDebugResult MatchPairWithDebug(string imgAPath, string imgBPath,
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null)
        {
            var result = MatchPair(imgAPath, imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint);
            MatchDebugInfo debug = null;
            try
            {
                debug = BuildDebugInfo(imgAPath, imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Debug match data skipped: {ex.Message}");
            }

            return new MatchPairDebugResult
            {
                Result = result,
                DebugInfo = debug
            };
        }

        private static Mat CreatePointMatNx2(Point2f[] pts)
        {
            var m = new Mat(pts.Length, 2, MatType.CV_32FC1);
            for (int i = 0; i < pts.Length; i++)
            {
                m.Set<float>(i, 0, pts[i].X);
                m.Set<float>(i, 1, pts[i].Y);
            }
            return m;
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        private static Mat ExtractEdgeRoi(
            Mat imgWork,
            EdgeSide side,
            double frac,
            int minPx,
            out int ox,
            out int oy,
            out Rect roiRect)
        {
            frac = Math.Max(0.0, Math.Min(1.0, frac));
            minPx = Math.Max(1, minPx);

            var h = imgWork.Rows;
            var w = imgWork.Cols;

            roiRect = EdgeRoiRect(h, w, side, frac, minPx);
            ox = roiRect.X;
            oy = roiRect.Y;

            return new Mat(imgWork, roiRect);
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        private static Rect EdgeRoiRect(int h, int w, EdgeSide side, double frac, int minPx)
        {
            switch (side)
            {
                case EdgeSide.Left:
                case EdgeSide.Right:
                {
                    var rw = (int)Math.Round(frac * w);
                    rw = Math.Max(minPx, Math.Min(w, rw));
                    var x = side == EdgeSide.Right ? 0 : (w - rw);
                    return new Rect(x, 0, rw, h);
                }
                case EdgeSide.Top:
                case EdgeSide.Bottom:
                {
                    var rh = (int)Math.Round(frac * h);
                    rh = Math.Max(minPx, Math.Min(h, rh));
                    var y = side == EdgeSide.Top ? 0 : (h - rh);
                    return new Rect(0, y, w, rh);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        private static void DetectAndCompute(Feature2D detector, Mat bgr, out KeyPoint[] kps, Mat des)
        {
            using (var gray = new Mat())
            {
                Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                detector.DetectAndCompute(gray, null, out kps, des);
            }
        }

        private static List<DMatch> MatchKnn2(
            Mat desA,
            Mat desB,
            NormTypes norm,
            double ratio,
            bool allowCloseSecondBest,
            double closeDiff)
        {
            if (desA.Empty() || desB.Empty())
                return new List<DMatch>();

            using (var matcher = new BFMatcher(norm, crossCheck: false))
            {
                var knn = matcher.KnnMatch(desA, desB, k: 2);
                var good = new List<DMatch>(knn.Length);

                foreach (var pair in knn)
                {
                    if (pair == null || pair.Length < 2) continue;
                    var m1 = pair[0];
                    var m2 = pair[1];

                    var okRatio = m1.Distance <= ratio * m2.Distance;
                    var okClose = allowCloseSecondBest && (Math.Abs(m1.Distance - m2.Distance) <= closeDiff);

                    if (okRatio || okClose) good.Add(m1);
                }

                good.Sort((x, y) => x.Distance.CompareTo(y.Distance));
                return good;
            }
        }

        private MatchDebugInfo BuildDebugInfo(string imgAPath, string imgBPath,
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint)
        {
            if (string.IsNullOrWhiteSpace(imgAPath)) throw new ArgumentNullException(nameof(imgAPath));
            if (string.IsNullOrWhiteSpace(imgBPath)) throw new ArgumentNullException(nameof(imgBPath));

            // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
            // var direction = directionHint ?? DirectionFromRobotDelta(dyRobot, dxRobot);
            var layout = ResolvePairMatchLayout(directionHint, dxRobot, dyRobot, MatchSideSelectionMode.Feature);
            var direction = layout.Direction;
            var roiFrac = AdjustOverlapFraction(direction, dxRobot, dyRobot, estimateDistX, estimateDistY);

            Size aOrig, bOrig;
            double scaleAWork, scaleBWork;

            using (var imgAWork = ReadForWork(imgAPath, _cfg.WorkMegapix, out aOrig, out scaleAWork))
            using (var imgBWork = ReadForWork(imgBPath, _cfg.WorkMegapix, out bOrig, out scaleBWork))
            {
                // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
                // string aSide, bSide;
                // if (direction == Direction.Horizontal)
                // {
                //     if (dxRobot <= 0) { aSide = "right"; bSide = "left"; }
                //     else { aSide = "left"; bSide = "right"; }
                // }
                // else
                // {
                //     if (dyRobot >= 0) { aSide = "top"; bSide = "bottom"; }
                //     else { aSide = "bottom"; bSide = "top"; }
                // }
                // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
                // var (aSide, bSide) = DetermineFeatureMatchSides(direction, dxRobot, dyRobot);
                var aSide = layout.ASide;
                var bSide = layout.BSide;

                int ax, ay, bx, by;
                Rect aRoiRect, bRoiRect;
                using (var aRoi = ExtractEdgeRoi(imgAWork, aSide, roiFrac, _cfg.RoiMinPx, out ax, out ay, out aRoiRect))
                using (var bRoi = ExtractEdgeRoi(imgBWork, bSide, roiFrac, _cfg.RoiMinPx, out bx, out by, out bRoiRect))
                using (var desA = new Mat())
                using (var desB = new Mat())
                {
                    DetectAndCompute(_detector, aRoi, out var kpA, desA);
                    DetectAndCompute(_detector, bRoi, out var kpB, desB);

                    var matches = MatchKnn2(desA, desB, DescriptorNorm, _cfg.RatioTest, _cfg.AllowCloseSecondBest, _cfg.CloseDiff);
                    var filtered = FilterMatchesByDirection(matches, kpA, kpB, ax, ay, bx, by, direction, scaleAWork);
                    var selectedPool = filtered.Count > 4 ? filtered : matches;
                    var selected = selectedPool.Take(Math.Max(1, Math.Max(4, _cfg.MaxHomoMatches))).ToArray();

                    return new MatchDebugInfo
                    {
                        RoiA = aRoi.Clone(),
                        RoiB = bRoi.Clone(),
                        KeypointsA = kpA,
                        KeypointsB = kpB,
                        RawMatches = matches.ToArray(),
                        FilteredMatches = filtered.ToArray(),
                        SelectedMatches = selected,
                        Direction = direction,
                        Stage = StageName
                    };
                }
            }
        }

        private List<DMatch> FilterMatchesByDirection(
            List<DMatch> matches,
            KeyPoint[] kpA,
            KeyPoint[] kpB,
            int ax,
            int ay,
            int bx,
            int by,
            Direction direction,
            double scaleAWork)
        {
            if (!_cfg.PreferPerpOffsetConstraint || matches == null || matches.Count == 0)
                return matches ?? new List<DMatch>();

            var thresholdFull = Math.Max(1.0, _cfg.MaxPerpOffsetPx);
            var thresholdWork = thresholdFull * Math.Max(1e-9, scaleAWork);

            List<DMatch> filtered = new List<DMatch>();
            foreach (var m in matches)
            {
                var pa = kpA[m.QueryIdx].Pt;
                var pb = kpB[m.TrainIdx].Pt;
                var axFull = pa.X + ax;
                var ayFull = pa.Y + ay;
                var bxFull = pb.X + bx;
                var byFull = pb.Y + by;

                var perpOffset = direction == Direction.Horizontal
                    ? Math.Abs(ayFull - byFull) : Math.Abs(axFull - bxFull);
                if (perpOffset < thresholdWork)
                    filtered.Add(m);
            }

            return filtered.Count > 0 ? filtered : matches;
        }

        private static Mat EnsureMat64F(Mat h)
        {
            if (h.Type() == MatType.CV_64FC1) return h.Clone();
            var dst = new Mat();
            h.ConvertTo(dst, MatType.CV_64FC1);
            return dst;
        }

        private static Mat TryRefineWithEcc(Mat aRoi, Mat bRoi, Mat hWork64)
        {
            try
            {
                using (var initWarp = BuildRigidFromHomography(hWork64))
                {
                    var criteria = new TermCriteria(CriteriaTypes.Count | CriteriaTypes.Eps, 30, 1e-6);
                    var refinedWarp = AlignmentRefinement.RefineWithEcc(aRoi, bRoi, initWarp, MotionTypes.Euclidean, criteria);
                    var refined = HomographyFromRigid(refinedWarp);
                    refinedWarp.Dispose();
                    return refined;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"ECC refine skipped: {ex.Message}");
                return null;
            }
        }

        private static void TryEstimateSeamMask(Mat imgAWork, Mat imgBWork, Mat hWork64)
        {
            try
            {
                using (var warpedB = new Mat())
                {
                    Cv2.WarpPerspective(imgBWork, warpedB, hWork64, new Size(imgAWork.Width, imgAWork.Height));
                    using (var mask = AlignmentRefinement.EstimateSeamMask(imgAWork, warpedB))
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Seam mask skipped: {ex.Message}");
            }
        }

        private static (double DThetaRad, double Tx, double Ty) PoseFromHomography(Mat hFull64)
        {
            var a = hFull64.At<double>(0, 0);
            var c = hFull64.At<double>(1, 0);
            var theta = Math.Atan2(c, a);
            var tx = hFull64.At<double>(0, 2);
            var ty = hFull64.At<double>(1, 2);
            return (theta, tx, ty);
        }

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

        private static Mat BuildRigidFromHomography(Mat hWork64)
        {
            var (theta, tx, ty) = PoseFromHomography(hWork64);
            return BuildRigid(theta, tx, ty);
        }

        private static Mat HomographyFromRigid(Mat rigid)
        {
            var h = Mat.Eye(3, 3, MatType.CV_64FC1).ToMat();
            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    h.Set(r, c, rigid.At<double>(r, c));
                }
            }
            return h;
        }

        private static void NormalizeHomographyInPlace(Mat h64)
        {
            var v = h64.At<double>(2, 2);
            if (Math.Abs(v) > 1e-12) h64 /= v;
        }

        private static Mat HomographyFullFromScaled(Mat hWork64, double scaleA, double scaleB)
        {
            var SaInv = Mat.Eye(3, 3, MatType.CV_64FC1).ToMat();
            var Sb = Mat.Eye(3, 3, MatType.CV_64FC1).ToMat();

            SaInv.Set<double>(0, 0, 1.0 / scaleA);
            SaInv.Set<double>(1, 1, 1.0 / scaleA);

            Sb.Set<double>(0, 0, scaleB);
            Sb.Set<double>(1, 1, scaleB);

            using (var tmp = (SaInv * hWork64).ToMat())
            {
                var hFull = (tmp * Sb).ToMat();
                SaInv.Dispose();
                Sb.Dispose();
                NormalizeHomographyInPlace(hFull);
                return hFull;
            }
        }

        private static double ComputeRmse(Point2f[] src, Point2f[] dst, Mat hFull64, Mat mask)
        {
            var ok = new List<int>(src.Length);
            for (int i = 0; i < mask.Rows; i++)
                if (mask.At<byte>(i, 0) != 0) ok.Add(i);

            if (ok.Count == 0) return double.PositiveInfinity;

            double sum = 0.0;
            for (int k = 0; k < ok.Count; k++)
            {
                int i = ok[k];
                var x = (double)src[i].X;
                var y = (double)src[i].Y;

                var X = hFull64.At<double>(0, 0) * x + hFull64.At<double>(0, 1) * y + hFull64.At<double>(0, 2);
                var Y = hFull64.At<double>(1, 0) * x + hFull64.At<double>(1, 1) * y + hFull64.At<double>(1, 2);
                var Z = hFull64.At<double>(2, 0) * x + hFull64.At<double>(2, 1) * y + hFull64.At<double>(2, 2);
                Z = Math.Max(1e-12, Z);

                var px = X / Z;
                var py = Y / Z;

                var dx = px - dst[i].X;
                var dy = py - dst[i].Y;

                sum += dx * dx + dy * dy;
            }

            return Math.Sqrt(sum / ok.Count);
        }

        private static double OverlapRatio(Size aSize, Size bSize, Mat hFull64)
        {
            if (aSize.Width <= 0 || aSize.Height <= 0 || bSize.Width <= 0 || bSize.Height <= 0)
                return 0.0;

            var wa = aSize.Width;
            var ha = aSize.Height;
            var wb = bSize.Width;
            var hb = bSize.Height;

            var corners = new[]
            {
                new Point2d(0, 0),
                new Point2d(wb, 0),
                new Point2d(wb, hb),
                new Point2d(0, hb)
            };

            var proj = corners.Select(c =>
            {
                var x = c.X;
                var y = c.Y;

                var X = hFull64.At<double>(0, 0) * x + hFull64.At<double>(0, 1) * y + hFull64.At<double>(0, 2);
                var Y = hFull64.At<double>(1, 0) * x + hFull64.At<double>(1, 1) * y + hFull64.At<double>(1, 2);
                var Z = hFull64.At<double>(2, 0) * x + hFull64.At<double>(2, 1) * y + hFull64.At<double>(2, 2);
                Z = Math.Max(1e-12, Z);

                return new Point2d(X / Z, Y / Z);
            }).ToArray();

            var minx = Math.Max(0.0, proj.Min(p => p.X));
            var maxx = Math.Min(wa, proj.Max(p => p.X));
            var miny = Math.Max(0.0, proj.Min(p => p.Y));
            var maxy = Math.Min(ha, proj.Max(p => p.Y));

            var iw = Math.Max(0.0, maxx - minx);
            var ih = Math.Max(0.0, maxy - miny);

            return (iw * ih) / Math.Max(1e-12, (double)wa * ha);
        }

        private static (Mat MRigid2x3, double DThetaRad, double Tx, double Ty) RigidFromInliersAnyShape(
            Point2f[] src, Point2f[] dst, Mat mask)
        {
            var srcIn = new List<Point2d>();
            var dstIn = new List<Point2d>();
            int n = Math.Min((int)mask.Total(), Math.Min(src.Length, dst.Length));

            for (int i = 0; i < n; i++)
            {
                if (!IsInlier(mask, i)) continue;
                srcIn.Add(new Point2d(src[i].X, src[i].Y));
                dstIn.Add(new Point2d(dst[i].X, dst[i].Y));
            }

            return RigidFromPointLists(srcIn, dstIn);
        }

        private static (Mat MRigid2x3, double DThetaRad, double Tx, double Ty) RigidFromPointLists(
            IList<Point2d> srcIn,
            IList<Point2d> dstIn)
        {
            if (srcIn == null) throw new ArgumentNullException(nameof(srcIn));
            if (dstIn == null) throw new ArgumentNullException(nameof(dstIn));
            if (srcIn.Count != dstIn.Count) throw new ArgumentException("src/dst size mismatch.");

            if (srcIn.Count < 2)
            {
                var m = new Mat(2, 3, MatType.CV_64FC1);
                m.Set<double>(0, 0, 1); m.Set<double>(0, 1, 0); m.Set<double>(0, 2, 0);
                m.Set<double>(1, 0, 0); m.Set<double>(1, 1, 1); m.Set<double>(1, 2, 0);
                return (m, 0.0, 0.0, 0.0);
            }

            var srcMean = new Point2d(srcIn.Average(p => p.X), srcIn.Average(p => p.Y));
            var dstMean = new Point2d(dstIn.Average(p => p.X), dstIn.Average(p => p.Y));

            double h00 = 0, h01 = 0, h10 = 0, h11 = 0;

            for (int i = 0; i < srcIn.Count; i++)
            {
                var xs = srcIn[i].X - srcMean.X;
                var ys = srcIn[i].Y - srcMean.Y;
                var xd = dstIn[i].X - dstMean.X;
                var yd = dstIn[i].Y - dstMean.Y;

                h00 += xs * xd; h01 += xs * yd;
                h10 += ys * xd; h11 += ys * yd;
            }

            using (var H = new Mat(2, 2, MatType.CV_64FC1))
            using (var W = new Mat())
            using (var U = new Mat())
            using (var Vt = new Mat())
            {
                H.Set<double>(0, 0, h00);
                H.Set<double>(0, 1, h01);
                H.Set<double>(1, 0, h10);
                H.Set<double>(1, 1, h11);

                Cv2.SVDecomp(H, W, U, Vt);

                using (var V = Vt.T().ToMat())
                using (var Ut = U.T().ToMat())
                using (var R = (V * Ut).ToMat())
                {
                    var det = Cv2.Determinant(R);
                    if (det < 0)
                    {
                        Vt.Set<double>(1, 0, -Vt.At<double>(1, 0));
                        Vt.Set<double>(1, 1, -Vt.At<double>(1, 1));

                        using (var V2 = Vt.T().ToMat())
                        using (var R2 = (V2 * Ut).ToMat())
                        {
                            return BuildRigidFromR(R2, srcMean, dstMean);
                        }
                    }

                    return BuildRigidFromR(R, srcMean, dstMean);
                }
            }
        }

        private static (Mat MRigid2x3, double DThetaRad, double Tx, double Ty) BuildRigidFromR(
            Mat R2x2,
            Point2d srcMean,
            Point2d dstMean)
        {
            var r00 = R2x2.At<double>(0, 0);
            var r01 = R2x2.At<double>(0, 1);
            var r10 = R2x2.At<double>(1, 0);
            var r11 = R2x2.At<double>(1, 1);

            var tx = dstMean.X - (r00 * srcMean.X + r01 * srcMean.Y);
            var ty = dstMean.Y - (r10 * srcMean.X + r11 * srcMean.Y);

            var m = new Mat(2, 3, MatType.CV_64FC1);
            m.Set<double>(0, 0, r00); m.Set<double>(0, 1, r01); m.Set<double>(0, 2, tx);
            m.Set<double>(1, 0, r10); m.Set<double>(1, 1, r11); m.Set<double>(1, 2, ty);

            var dtheta = Math.Atan2(r10, r00);
            return (m, dtheta, tx, ty);
        }

        private static PairResult Fail(string reason, int nKpA, int nKpB, int nMatches)
        {
            return new PairResult
            {
                HFullBToA = new Mat(),
                MRigidBToA = new Mat(),
                Eval = new PairEval
                {
                    IsMatch = false,
                    Reason = reason,
                    NKpA = nKpA,
                    NKpB = nKpB,
                    NMatches = nMatches,
                    NInliers = 0,
                    InlierRatio = 0,
                    Rmse = double.PositiveInfinity,
                    OverlapRatio = 0,
                    Accuracy = 0
                }
            };
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        // private static Direction DirectionFromRobotDelta(double dy, double dx)
        //     => (Math.Abs(dx) >= Math.Abs(dy)) ? Direction.Horizontal : Direction.Vertical;

        private static bool IsInlier(Mat mask, int i)
        {
            if (mask.Rows == 1 && mask.Cols > 1)
                return mask.At<byte>(0, i) != 0;

            return mask.At<byte>(i, 0) != 0;
        }

        private static int CountInliersAnyShape(Mat mask)
        {
            int n = 0;
            int total = (int)mask.Total();
            for (int i = 0; i < total; i++)
                if (IsInlier(mask, i)) n++;
            return n;
        }
    }
}
