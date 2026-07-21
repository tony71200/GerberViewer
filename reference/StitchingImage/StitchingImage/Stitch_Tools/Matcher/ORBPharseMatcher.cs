// =======================================================
// File: StitchingImage/Stitch_Tools/ORBPharseMatcher.cs
// Target: .NET Framework 4.8
// NuGet: OpenCvSharp4, OpenCvSharp4.runtime.win
// =======================================================
using System;
using OpenCvSharp;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    /// <summary>
    /// ORB-based matcher with a PhaseCorrelate refinement on the overlap ROI.
    /// </summary>
    public sealed class ORBPharseMatcher : PairMatching
    {
        public ORBPharseMatcher(StitchingConfig cfg) : base(cfg) { }

        public override PairResult MatchPair(string imgAPath, string imgBPath,
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null)
        {
            using (var orb = new ORBMatcher(_cfg))
            {
                var baseResult = orb.MatchPair(imgAPath, imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint);
                if (baseResult?.Eval == null || !baseResult.Eval.IsMatch)
                    return baseResult;

                if (baseResult.HFullBToA == null || baseResult.HFullBToA.Empty())
                    return baseResult;

                double scaleAWork;
                double scaleBWork;
                using (var imgAWork = ReadForWork(imgAPath, _cfg.WorkMegapix, out _, out scaleAWork))
                using (var imgBWork = ReadForWork(imgBPath, _cfg.WorkMegapix, out _, out scaleBWork))
                using (var hWork = ScaleHomographyToWork(baseResult.HFullBToA, scaleAWork, scaleBWork))
                using (var warpedB = new Mat())
                {
                    Cv2.WarpPerspective(imgBWork, warpedB, hWork, new Size(imgAWork.Width, imgAWork.Height));

                    if (!TryGetOverlapRect(warpedB, out var overlapRect))
                        return baseResult;

                    using (var overlapA = new Mat(imgAWork, overlapRect))
                    using (var overlapB = new Mat(warpedB, overlapRect))
                    {
                        if (!TryPhaseCorrelate(overlapA, overlapB, out var shift, out var response))
                            return baseResult;

                        if (response < _cfg.PhaseCorrMinResponse)
                            return baseResult;

                        var dx = -shift.X;
                        var dy = -shift.Y;

                        hWork.Set(0, 2, hWork.At<double>(0, 2) + dx);
                        hWork.Set(1, 2, hWork.At<double>(1, 2) + dy);

                        using (var hFull = ScaleHomographyToFull(hWork, scaleAWork, scaleBWork))
                        {
                            var (dtheta, tx, ty) = PoseFromHomography(hFull);
                            var mRigid = BuildRigid(dtheta, tx, ty);

                            baseResult.HFullBToA?.Dispose();
                            baseResult.MRigidBToA?.Dispose();

                            baseResult.HFullBToA = hFull.Clone();
                            baseResult.MRigidBToA = mRigid;
                            baseResult.DThetaRad = dtheta;
                            baseResult.Tx = tx;
                            baseResult.Ty = ty;

                            if (baseResult.Eval != null)
                            {
                                baseResult.Eval.OverlapRatio = Math.Max(baseResult.Eval.OverlapRatio, Math.Min(1.0, response));
                                // [GPT-5.2-Codex] [Change time: 260319] [Preserve graph/run-graph direction hints during robot-direction validation]
                                // directionHint ?? DirectionFromRobotDelta(dyRobot, dxRobot)
                                var direction = ResolveMatchDirection(directionHint, dxRobot, dyRobot);
                                if (_cfg.EnforceRobotDirection && !IsRobotDirectionConsistent(
                                        direction, dxRobot, dyRobot, tx, ty))
                                {
                                    baseResult.Eval.IsMatch = false;
                                    baseResult.Eval.Reason = "robot_direction_mismatch";
                                }
                            }

                            return baseResult;
                        }
                    }
                }
            }
        }

        private static bool TryPhaseCorrelate(Mat a, Mat b, out Point2d shift, out double response)
        {
            shift = new Point2d(0, 0);
            response = 0;

            using (var aGray = EnsureGray32F(a))
            using (var bGrayBase = EnsureGray32F(b))
            using (var bGray = new Mat())
            using (var hann = new Mat())
            {
                if (aGray.Empty() || bGrayBase.Empty())
                    return false;

                if (aGray.Size() != bGrayBase.Size())
                    Cv2.Resize(bGrayBase, bGray, aGray.Size(), 0, 0, InterpolationFlags.Linear);
                else
                    bGrayBase.CopyTo(bGray);

                Cv2.CreateHanningWindow(hann, aGray.Size(), MatType.CV_32F);
                shift = Cv2.PhaseCorrelate(aGray, bGray, hann, out response);
                return true;
            }
        }

        private static bool TryGetOverlapRect(Mat warpedB, out Rect rect)
        {
            rect = new Rect();
            using (var gray = EnsureGray32F(warpedB))
            using (var gray8 = new Mat())
            using (var mask = new Mat())
            {
                gray.ConvertTo(gray8, MatType.CV_8U, 255.0);
                Cv2.Threshold(gray8, mask, 0, 255, ThresholdTypes.Binary);
                using (var points = new Mat())
                {
                    Cv2.FindNonZero(mask, points); // Fix for CS7036: Provide the required 'idx' parameter
                    if (points.Empty()) // Fix for CS0815: Check if points is empty instead of assigning void
                        return false;

                    rect = Cv2.BoundingRect(points);
                    return rect.Width > 1 && rect.Height > 1;
                }
            }
        }

        private static Mat ScaleHomographyToWork(Mat hFull, double scaleA, double scaleB)
        {
            using (var sa = Mat.Eye(3, 3, MatType.CV_64F).ToMat())
            using (var sbInv = Mat.Eye(3, 3, MatType.CV_64F).ToMat())
            using (var temp = new Mat())
            {
                sa.Set(0, 0, scaleA);
                sa.Set(1, 1, scaleA);
                sbInv.Set(0, 0, 1.0 / Math.Max(1e-9, scaleB));
                sbInv.Set(1, 1, 1.0 / Math.Max(1e-9, scaleB));

                Cv2.Gemm(sa, hFull, 1.0, new Mat(), 0.0, temp);
                var hWork = new Mat();
                Cv2.Gemm(temp, sbInv, 1.0, new Mat(), 0.0, hWork);
                return hWork;
            }
        }

        private static Mat ScaleHomographyToFull(Mat hWork, double scaleA, double scaleB)
        {
            using (var saInv = Mat.Eye(3, 3, MatType.CV_64F).ToMat())
            using (var sb = Mat.Eye(3, 3, MatType.CV_64F).ToMat())
            using (var temp = new Mat())
            {
                saInv.Set(0, 0, 1.0 / Math.Max(1e-9, scaleA));
                saInv.Set(1, 1, 1.0 / Math.Max(1e-9, scaleA));
                sb.Set(0, 0, scaleB);
                sb.Set(1, 1, scaleB);

                Cv2.Gemm(saInv, hWork, 1.0, new Mat(), 0.0, temp);
                var hFull = new Mat();
                Cv2.Gemm(temp, sb, 1.0, new Mat(), 0.0, hFull);
                return hFull;
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

        private static Mat EnsureGray32F(Mat src)
        {
            var gray = new Mat();
            if (src.Channels() == 1)
                gray = src.Clone();
            else
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            if (gray.Type() != MatType.CV_32F)
            {
                var tmp = new Mat();
                gray.ConvertTo(tmp, MatType.CV_32F, 1.0 / 255.0);
                gray.Dispose();
                gray = tmp;
            }
            return gray;
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Retain original robot-delta helper as commented history while PairMatching now provides the fallback]
//        private static Direction DirectionFromRobotDelta(double dy, double dx)
//            => (Math.Abs(dx) >= Math.Abs(dy)) ? Direction.Horizontal : Direction.Vertical;

    }
}
