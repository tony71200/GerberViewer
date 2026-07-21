using System;
using OpenCvSharp;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    /// <summary>
    /// [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
    /// ECC matcher ported from docs/EccMatcher.cs and adapted to PairMatching pipeline.
    /// </summary>
    public sealed class EccMatcher : PairMatching
    {
        public EccMatcher(StitchingConfig cfg) : base(cfg) { }

        public MotionTypes MotionType { get; set; } = MotionTypes.Euclidean;
        public int MaxIterations { get; set; } = 200;
        public double Epsilon { get; set; } = 1e-5;
        public Mat InitialWarpMatrix { get; set; }

        public override PairResult MatchPair(string imgAPath, string imgBPath,
            double dxRobot, double dyRobot, double estimateDistX, double estimateDistY, Direction? directionHint = null)
        {
            if (string.IsNullOrWhiteSpace(imgAPath)) throw new ArgumentNullException(nameof(imgAPath));
            if (string.IsNullOrWhiteSpace(imgBPath)) throw new ArgumentNullException(nameof(imgBPath));

            var layout = ResolvePairMatchLayout(directionHint, dxRobot, dyRobot, MatchSideSelectionMode.PhaseCorr);
            var direction = layout.Direction;

            using (var imgAWork = ReadForWork(imgAPath, _cfg.WorkMegapix, out var aOrig, out var scaleAWork))
            using (var imgBWork = ReadForWork(imgBPath, _cfg.WorkMegapix, out var bOrig, out var scaleBWork))
            {
                var fractionW = NormalizeFraction(_cfg.PhaseCorrFractionW);
                var fractionH = NormalizeFraction(_cfg.PhaseCorrFractionH);

                int ax, ay, bx, by;
                using (var aRoi = CropBorderRoi(imgAWork, layout.ASide, fractionW, fractionH, _cfg.RoiMinPx, out ax, out ay))
                using (var bRoi = CropBorderRoi(imgBWork, layout.BSide, fractionW, fractionH, _cfg.RoiMinPx, out bx, out by))
                using (var aGray = EnsureGray32F(aRoi))
                using (var bGrayRaw = EnsureGray32F(bRoi))
                using (var bGray = new Mat())
                using (var warp = CreateWarp(MotionType, InitialWarpMatrix))
                {
                    if (aGray.Empty() || bGrayRaw.Empty())
                        return Fail("empty_roi");

                    if (aGray.Size() != bGrayRaw.Size())
                        Cv2.Resize(bGrayRaw, bGray, aGray.Size(), 0, 0, InterpolationFlags.Linear);
                    else
                        bGrayRaw.CopyTo(bGray);

                    var criteria = new TermCriteria(CriteriaTypes.Count | CriteriaTypes.Eps, Math.Max(10, MaxIterations), Epsilon);
                    double eccScore;
                    try
                    {
                        eccScore = Cv2.FindTransformECC(aGray, bGray, warp, MotionType, criteria);
                    }
                    catch (OpenCVException ex)
                    {
                        return Fail("ecc_fail:" + ex.Message);
                    }

                    // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                    // ECC confidence guard aligned with PhaseCorr minimum response to reduce false-positive matches.
                    var normalizedEcc = NormalizeEccScore(eccScore);
                    if (normalizedEcc < Math.Max(0.0, Math.Min(1.0, _cfg.PhaseCorrMinResponse)))
                        return Fail($"ecc_low_confidence:{normalizedEcc:0.###}");

                    var decoded = DecodeWarpMatrix(warp, MotionType);

                    var scaleRatio = scaleAWork / Math.Max(1e-9, scaleBWork);
                    var txWork = (ax - (bx * scaleRatio)) + decoded.dx;
                    var tyWork = (ay - (by * scaleRatio)) + decoded.dy;

                    var tx = txWork / Math.Max(1e-9, scaleAWork);
                    var ty = tyWork / Math.Max(1e-9, scaleAWork);

                    var hFull = Mat.Eye(3, 3, MatType.CV_64F).ToMat();
                    hFull.Set(0, 2, tx);
                    hFull.Set(1, 2, ty);

                    var mRigid = BuildRigid(decoded.angleDeg * Math.PI / 180.0, tx, ty);
                    var overlap = EstimateOverlap(aOrig, bOrig, tx, ty);
                    var ev = new PairEval
                    {
                        IsMatch = true,
                        Reason = "ok",
                        NKpA = 0,
                        NKpB = 0,
                        NMatches = 1,
                        NInliers = 1,
                        InlierRatio = 1.0,
                        Rmse = 0.0,
                        OverlapRatio = overlap,
                        Accuracy = Clamp01((0.8 * normalizedEcc) + (0.2 * overlap))
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
                        DThetaRad = decoded.angleDeg * Math.PI / 180.0,
                        Tx = tx,
                        Ty = ty,
                        Eval = ev
                    };
                }
            }
        }

        public static (double dx, double dy, double angleDeg, double scaleX, double scaleY) DecodeWarpMatrix(Mat w, MotionTypes motionType)
        {
            switch (motionType)
            {
                case MotionTypes.Translation:
                    return (w.At<float>(0, 2), w.At<float>(1, 2), 0.0, 1.0, 1.0);
                case MotionTypes.Euclidean:
                    {
                        var cos = w.At<float>(0, 0);
                        var sin = w.At<float>(1, 0);
                        var angle = Math.Atan2(sin, cos) * 180.0 / Math.PI;
                        var scale = Math.Sqrt(cos * cos + sin * sin);
                        return (w.At<float>(0, 2), w.At<float>(1, 2), angle, scale, scale);
                    }
                case MotionTypes.Affine:
                    {
                        var a = w.At<float>(0, 0);
                        var b = w.At<float>(0, 1);
                        var c = w.At<float>(1, 0);
                        var d = w.At<float>(1, 1);
                        var scaleX = Math.Sqrt(a * a + c * c);
                        var scaleY = Math.Sqrt(b * b + d * d);
                        var angle = Math.Atan2(c, a) * 180.0 / Math.PI;
                        return (w.At<float>(0, 2), w.At<float>(1, 2), angle, scaleX, scaleY);
                    }
                case MotionTypes.Homography:
                    {
                        var a = w.At<float>(0, 0);
                        var c = w.At<float>(1, 0);
                        var angle = Math.Atan2(c, a) * 180.0 / Math.PI;
                        var scale = Math.Sqrt(a * a + c * c);
                        return (w.At<float>(0, 2), w.At<float>(1, 2), angle, scale, scale);
                    }
                default:
                    return (0, 0, 0, 1, 1);
            }
        }

        private static Mat CreateWarp(MotionTypes motionType, Mat initial)
        {
            if (initial != null)
                return initial.Clone();

            return motionType == MotionTypes.Homography
                ? Mat.Eye(3, 3, MatType.CV_32F)
                : Mat.Eye(2, 3, MatType.CV_32F);
        }

        private static double NormalizeFraction(double value) => Math.Max(0.005, Math.Min(1.0, value));

        private static Mat CropBorderRoi(Mat img, EdgeSide side, double fractionWPercent, double fractionHPercent, int minPx, out int ox, out int oy)
        {
            var h = img.Rows;
            var w = img.Cols;
            var cropW = Math.Max(minPx, (int)(w * fractionWPercent));
            var cropH = Math.Max(minPx, (int)(h * fractionHPercent));
            cropW = Math.Min(w, cropW);
            cropH = Math.Min(h, cropH);

            var x = 0;
            var y = 0;
            switch (side)
            {
                case EdgeSide.Left: x = 0; y = 0; cropH = h; break;
                case EdgeSide.Right: x = w - cropW; y = 0; cropH = h; break;
                case EdgeSide.Top: x = 0; y = 0; cropW = w; break;
                case EdgeSide.Bottom: x = 0; y = h - cropH; cropW = w; break;
                default: throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }

            ox = x;
            oy = y;
            return new Mat(img, new Rect(x, y, cropW, cropH));
        }

        private static Mat EnsureGray32F(Mat src)
        {
            var gray = new Mat();
            if (src.Channels() == 1) src.CopyTo(gray);
            else Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            var result = new Mat();
            gray.ConvertTo(result, MatType.CV_32F);
            gray.Dispose();
            return result;
        }

        private static double EstimateOverlap(Size aSize, Size bSize, double tx, double ty)
        {
            var left = Math.Max(0.0, tx);
            var right = Math.Min(aSize.Width, tx + bSize.Width);
            var top = Math.Max(0.0, ty);
            var bottom = Math.Min(aSize.Height, ty + bSize.Height);
            var iw = Math.Max(0.0, right - left);
            var ih = Math.Max(0.0, bottom - top);
            return (iw * ih) / Math.Max(1e-12, (double)aSize.Width * aSize.Height);
        }

        private Mat BuildRigid(double thetaRad, double tx, double ty)
        {
            var c = Math.Cos(thetaRad);
            var s = Math.Sin(thetaRad);
            //Mat m = Mat.Zeros(2, 3, MatType.CV_64F).ToMat();
            Mat m = new Mat(2, 3, MatType.CV_64F, Scalar.All(0));

            //m.At<double>(0, 0) = c;
            //m.At<double>(0, 1) = -s;
            //m.At<double>(1, 0) = s;
            //m.At<double>(1, 1) = c;
            //m.At<double>(0, 2) = tx;
            //m.At<double>(1, 2) = ty;
            m.Set<double>(0, 0, c);
            m.Set<double>(0, 1, -s);
            m.Set<double>(1, 0, s);
            m.Set<double>(1, 1, c);
            m.Set<double>(0, 2, tx);
            m.Set<double>(1, 2, ty);
            return m;
        }

        private static PairResult Fail(string reason)
        {
            return new PairResult
            {
                HFullBToA = new Mat(),
                MRigidBToA = new Mat(),
                DThetaRad = 0,
                Tx = 0,
                Ty = 0,
                Eval = new PairEval { IsMatch = false, Reason = reason }
            };
        }

        private static double NormalizeEccScore(double ecc)
            => Math.Max(0.0, Math.Min(1.0, (ecc + 1.0) / 2.0));
    }
}
