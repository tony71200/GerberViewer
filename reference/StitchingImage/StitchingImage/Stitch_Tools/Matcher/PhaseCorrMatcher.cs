// =======================================================
// File: StitchingImage/Stitch_Tools/PhaseCorrMatcher.cs
// Target: .NET Framework 4.8
// NuGet: OpenCvSharp4, OpenCvSharp4.runtime.win
// =======================================================
using System;
using System.Diagnostics.Eventing.Reader;
using System.Security.RightsManagement;
using System.Text;
using OpenCvSharp;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    public sealed class PhaseCorrMatcher : PairMatching
    {
        public PhaseCorrMatcher(StitchingConfig cfg) : base(cfg) { }

        public override PairResult MatchPair(string imgAPath, string imgBPath,
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null)
        {
            return MatchPairInternal(imgAPath, imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint, false);
        }

        public PairResult MatchPairWithSpecialGap(string imgAPath, string imgBPath,
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint, bool isSpecialGap)
        {
            return MatchPairInternal(imgAPath, imgBPath, dxRobot, dyRobot, estimateDistX, estimateDistY, directionHint, isSpecialGap);
        }

        private PairResult MatchPairInternal(string imgAPath, string imgBPath,
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint, bool isSpecialGap)
        {
            if (string.IsNullOrWhiteSpace(imgAPath)) throw new ArgumentNullException(nameof(imgAPath));
            if (string.IsNullOrWhiteSpace(imgBPath)) throw new ArgumentNullException(nameof(imgBPath));

            // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
            // var direction = directionHint ?? DirectionFromRobotDelta(dyRobot, dxRobot);
            var layout = ResolvePairMatchLayout(directionHint, dxRobot, dyRobot, MatchSideSelectionMode.PhaseCorr);
            var direction = layout.Direction;

            double scaleAWork, scaleBWork;

            using (var imgAWork = ReadForWork(imgAPath, _cfg.WorkMegapix, out var aOrig, out scaleAWork))
            using (var imgBWork = ReadForWork(imgBPath, _cfg.WorkMegapix, out var bOrig, out scaleBWork))
            {
                // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
                // string aSide, bSide;
                // if (direction == Direction.Horizontal)
                // {
                //     if (double.IsNaN(dxRobot)) { aSide = "right"; bSide = "left"; }
                //     else
                //     {
                //         if (dxRobot >= 0) { aSide = "right"; bSide = "left"; }
                //         else { aSide = "left"; bSide = "right"; }
                //     }
                // }
                // else
                // {
                //     if (double.IsNaN(dyRobot)) { aSide = "bottom"; bSide = "top"; }
                //     else
                //     {
                //         if (dyRobot >= 0) { aSide = "top"; bSide = "bottom"; }
                //         else { aSide = "bottom"; bSide = "top"; }
                //     }
                // }
                // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
                // var (aSide, bSide) = DeterminePhaseCorrSides(direction, dxRobot, dyRobot);
                var aSide = layout.ASide;
                var bSide = layout.BSide;

                var fractionW = NormalizeFraction(_cfg.PhaseCorrFractionW);
                var fractionH = NormalizeFraction(_cfg.PhaseCorrFractionH);
                var result = TryMatchPhaseCorr(imgAWork, imgBWork, aSide, bSide, fractionW, fractionH,
                    scaleAWork, scaleBWork, aOrig, bOrig, direction, dxRobot, dyRobot, out var failReason);

                if (result == null && isSpecialGap && _cfg.PhaseCorrFractionSpecial > 0)
                {
                    var specialFractionW = NormalizeFraction(_cfg.PhaseCorrFractionSpecial);
                    result = TryMatchPhaseCorr(imgAWork, imgBWork, aSide, bSide, specialFractionW, fractionH,
                        scaleAWork, scaleBWork, aOrig, bOrig, direction, dxRobot, dyRobot, out failReason);
                }

                return result ?? Fail(failReason);
            }
        }

        private PairResult Fail(string reason)
        {
            return new PairResult
            {
                HFullBToA = new Mat(),
                MRigidBToA = new Mat(),
                DThetaRad = 0.0,
                Tx = 0.0,
                Ty = 0.0,
                Eval = new PairEval
                {
                    IsMatch = false,
                    Reason = reason,
                    NKpA = 0,
                    NKpB = 0,
                    NMatches = 0,
                    NInliers = 0,
                    InlierRatio = 0,
                    Rmse = double.PositiveInfinity,
                    OverlapRatio = 0,
                    Accuracy = 0
                }
            };
        }

        private static double OverlapRatio(Size aSize, Size bSize, double tx, double ty)
        {
            if (aSize.Width <= 0 || aSize.Height <= 0 || bSize.Width <= 0 || bSize.Height <= 0)
                return 0.0;

            var left = Math.Max(0.0, tx);
            var right = Math.Min(aSize.Width, tx + bSize.Width);
            var top = Math.Max(0.0, ty);
            var bottom = Math.Min(aSize.Height, ty + bSize.Height);

            var iw = Math.Max(0.0, right - left);
            var ih = Math.Max(0.0, bottom - top);
            return (iw * ih) / Math.Max(1e-12, (double)aSize.Width * aSize.Height);
        }

        private PairResult TryMatchPhaseCorr(
            Mat imgAWork,
            Mat imgBWork,
            EdgeSide aSide,
            EdgeSide bSide,
            double fractionW,
            double fractionH,
            double scaleAWork,
            double scaleBWork,
            Size aOrig,
            Size bOrig,
            Direction direction,
            double dxRobot,
            double dyRobot,
            out string failReason)
        {
            failReason = "empty_roi";
            var isMatch = true;

            int ax, ay, bx, by;
            using (var aRoi = CropBorderRoi2(imgAWork, aSide, fractionW, fractionH, _cfg.RoiMinPx, out ax, out ay))
            using (var bRoi = CropBorderRoi2(imgBWork, bSide, fractionW, fractionH, _cfg.RoiMinPx, out bx, out by))
            using (var aGray = EnsureGray32F(aRoi))
            using (var bGrayBase = EnsureGray32F(bRoi))
            using (var bGray = new Mat())
            using (var hann = new Mat())
            {
                if (aGray.Empty() || bGrayBase.Empty())
                {
                    failReason = "empty_roi";
                    isMatch = false;
                    return null;
                }

                if (aGray.Size() != bGrayBase.Size())
                    Cv2.Resize(bGrayBase, bGray, aGray.Size(), 0, 0, InterpolationFlags.Linear);
                else
                    bGrayBase.CopyTo(bGray);

                Cv2.CreateHanningWindow(hann, aGray.Size(), MatType.CV_32F);
                double response;
                var shift = Cv2.PhaseCorrelate(aGray, bGray, hann, out response);

                if (response < _cfg.PhaseCorrMinResponse)
                {
                    failReason = $"phasecorr_low_response:{response:0.###}";
                    switch (aSide)
                    {
                        case EdgeSide.Right:
                            shift.X = -aRoi.Width;
                            shift.Y = 0;
                            break;
                        case EdgeSide.Left:
                            shift.X = aRoi.Width;
                            shift.Y = 0;
                            break;
                        case EdgeSide.Bottom:
                            shift.X = 0;
                            shift.Y = -aRoi.Height;
                            break;
                        case EdgeSide.Top:
                            shift.X = 0;
                            shift.Y = aRoi.Height;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(aSide), aSide, null);
                    }
                    isMatch &= false;
                    
                }
                else
                {
                    failReason = "ok";
                    isMatch = true;
                }

                var dx = -shift.X;
                var dy = -shift.Y;

                var scaleRatio = scaleAWork / Math.Max(1e-9, scaleBWork);
                var bxScaled = bx * scaleRatio;
                var byScaled = by * scaleRatio;

                var txWork = (ax - bxScaled) + dx;
                var tyWork = (ay - byScaled) + dy;

                var tx = txWork / Math.Max(1e-9, scaleAWork);
                var ty = tyWork / Math.Max(1e-9, scaleAWork);

                var hFull = Mat.Eye(3, 3, MatType.CV_64F).ToMat();
                hFull.Set(0, 2, tx);
                hFull.Set(1, 2, ty);

                var mRigid = Mat.Eye(2, 3, MatType.CV_64F).ToMat();
                mRigid.Set(0, 2, tx);
                mRigid.Set(1, 2, ty);

                var overlap = OverlapRatio(aOrig, bOrig, tx, ty);
                var accuracy = Clamp01((0.8 * response) + (0.2 * (overlap)));
                Logger.Info($"[Pharr] tx: {tx} | ty: {ty} | response: {response}");
                var ev = new PairEval
                {
                    IsMatch = isMatch,
                    Reason = failReason,
                    NKpA = 0,
                    NKpB = 0,
                    NMatches = 0,
                    NInliers = 0,
                    InlierRatio = 1.0,
                    Rmse = 0.0,
                    OverlapRatio = overlap,
                    Accuracy = accuracy
                };

                if (_cfg.EnforceRobotDirection && !IsRobotDirectionConsistent(direction, dxRobot, dyRobot, tx, ty))
                {
                    ev.IsMatch = false;
                    ev.Reason = "robot_direction_mismatch";
                }

                return new PairResult
                {
                    HFullBToA = hFull,
                    MRigidBToA = mRigid,
                    DThetaRad = 0.0,
                    Tx = tx,
                    Ty = ty,
                    Eval = ev
                };
            }
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        private static Mat CropBorderRoi2(Mat img, EdgeSide side, double fractionWPercent, double fractionHPercent, int minPx, out int ox, out int oy)
        {
            fractionWPercent = Math.Max(0.005, fractionWPercent);
            fractionHPercent = Math.Max(0.005, fractionHPercent);
            minPx = Math.Max(1, minPx);

            var h = img.Rows;
            var w = img.Cols;
            var cropW = Math.Max(minPx, (int)(w * fractionWPercent));
            var cropH = Math.Max(minPx, (int)(h * fractionHPercent));
            cropW = (int)Math.Min(w, cropW);
            cropH = (int)Math.Min(h, cropH);

            int x = 0;
            int y = 0;

            switch (side)
            {
                case EdgeSide.Left:
                    x = 0;
                    y = 0;
                    cropH = h;
                    break;
                case EdgeSide.Right:
                    x = (int)(w - cropW);
                    y = 0;
                    cropH = h;
                    break;
                case EdgeSide.Top:
                    x = 0;
                    y = 0;
                    cropW = w;
                    break;
                case EdgeSide.Bottom:
                    x = 0;
                    y = (int)(h - cropH);
                    cropW = w;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }

            ox = x;
            oy = y;
            
            return new Mat(img, new Rect(x, y, cropW, cropH));
        }

        private static double NormalizeFraction(double value)
        {
            if (value <= 0)
                return 0;
            return value > 1.0 ? value / 100d : value;
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        private static Mat CropBorderRoi(Mat img, EdgeSide side, int fractionW, int fractionH, int minPx, out int ox, out int oy)
        {
            fractionW = Math.Max(1, fractionW);
            fractionH = Math.Max(1, fractionH);
            minPx = Math.Max(1, minPx);

            var h = img.Rows;
            var w = img.Cols;
            var cropW = Math.Max(minPx, w / fractionW);
            var cropH = Math.Max(minPx, h / fractionH);
            cropW = Math.Min(w, cropW);
            cropH = Math.Min(h, cropH);

            int x = 0;
            int y = 0;

            switch (side)
            {
                case EdgeSide.Left:
                    x = 0;
                    y = 0;
                    break;
                case EdgeSide.Right:
                    x = w - cropW;
                    y = 0;
                    break;
                case EdgeSide.Top:
                    x = 0;
                    y = 0;
                    break;
                case EdgeSide.Bottom:
                    x = 0;
                    y = h - cropH;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }

            ox = x;
            oy = y;
            return new Mat(img, new Rect(x, y, cropW, cropH));
        }

        // [GPT-5.2-Codex] [Change time: 260319] [Purpose of change]
        // private static Direction DirectionFromRobotDelta(double dy, double dx)
        //     => (Math.Abs(dx) >= Math.Abs(dy)) ? Direction.Horizontal : Direction.Vertical;

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
    }
}
