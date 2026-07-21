using System;
using OpenCvSharp;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.Matcher
{
    /// <summary>
    /// [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
    /// Ported from docs/EccMatcher2.cs and adapted to PairMatching/FeatureMatcher pipeline.
    /// Runs ECC on downscaled overlap ROI then maps translation back to full image coordinates.
    /// </summary>
    public sealed class EccMatcher2 : PairMatching
    {
        public EccMatcher2(StitchingConfig cfg) : base(cfg) { }

        public MotionTypes MotionType { get; set; } = MotionTypes.Euclidean;
        public int MaxWorkingEdge { get; set; } = 640;
        public int CoarseIterations { get; set; } = 80;
        public int RefineIterations { get; set; } = 35;
        public double Epsilon { get; set; } = 1e-5;
        public bool EnableFullResolutionRefine { get; set; } = true;

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
                {
                    if (aGray.Empty() || bGrayRaw.Empty())
                        return Fail("empty_roi");

                    if (aGray.Size() != bGrayRaw.Size())
                        Cv2.Resize(bGrayRaw, bGray, aGray.Size(), 0, 0, InterpolationFlags.Linear);
                    else
                        bGrayRaw.CopyTo(bGray);

                    var eccResult = RunEcc(aGray, bGray);
                    if (!eccResult.success)
                        return Fail(eccResult.reason);
                    // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                    // ECC confidence guard aligned with PhaseCorr minimum response to reduce false-positive matches.
                    if (eccResult.confidence < Math.Max(0.0, Math.Min(1.0, _cfg.PhaseCorrMinResponse)))
                        return Fail($"ecc_low_confidence:{eccResult.confidence:0.###}");

                    var scaleRatio = scaleAWork / Math.Max(1e-9, scaleBWork);
                    var txWork = (ax - (bx * scaleRatio)) + eccResult.dx;
                    var tyWork = (ay - (by * scaleRatio)) + eccResult.dy;

                    var tx = txWork / Math.Max(1e-9, scaleAWork);
                    var ty = tyWork / Math.Max(1e-9, scaleAWork);

                    var hFull = Mat.Eye(3, 3, MatType.CV_64F).ToMat();
                    hFull.Set(0, 2, tx);
                    hFull.Set(1, 2, ty);

                    var mRigid = Mat.Eye(2, 3, MatType.CV_64F).ToMat();
                    mRigid.Set(0, 2, tx);
                    mRigid.Set(1, 2, ty);

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
                        Accuracy = Clamp01((0.8 * eccResult.confidence) + (0.2 * overlap))
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
        }

        private (bool success, double dx, double dy, double confidence, string reason) RunEcc(Mat aGray, Mat bGray)
        {
            Mat srcWork = null, dstWork = null, warpWork = null, warpFinal = null;
            try
            {
                double scaleX, scaleY;
                PrepareWorkingPair(aGray, bGray, out srcWork, out dstWork, out scaleX, out scaleY);

                warpWork = CreateIdentityWarp(MotionType);
                var coarseCriteria = new TermCriteria(CriteriaTypes.Count | CriteriaTypes.Eps, Math.Max(10, CoarseIterations), Epsilon);
                var coarseEcc = Cv2.FindTransformECC(srcWork, dstWork, warpWork, MotionType, coarseCriteria);

                warpFinal = warpWork.Clone();
                ScaleWarpTranslation(warpFinal, 1.0 / Math.Max(1e-9, scaleX), 1.0 / Math.Max(1e-9, scaleY));

                var finalEcc = coarseEcc;
                if (EnableFullResolutionRefine && RefineIterations > 0 && (scaleX < 0.999 || scaleY < 0.999))
                {
                    var refineCriteria = new TermCriteria(CriteriaTypes.Count | CriteriaTypes.Eps, Math.Max(5, RefineIterations), Epsilon);
                    finalEcc = Cv2.FindTransformECC(aGray, bGray, warpFinal, MotionType, refineCriteria);
                }

                var decoded = DecodeWarpMatrix(warpFinal);
                return (true, decoded.dx, decoded.dy, NormalizeEccScore(finalEcc), "ok");
            }
            catch (OpenCVException ex)
            {
                return (false, 0, 0, 0, "ecc_fail:" + ex.Message);
            }
            finally
            {
                srcWork?.Dispose();
                dstWork?.Dispose();
                warpWork?.Dispose();
                warpFinal?.Dispose();
            }
        }

        private Mat CreateIdentityWarp(MotionTypes motionType)
            => motionType == MotionTypes.Homography ? Mat.Eye(3, 3, MatType.CV_32F) : Mat.Eye(2, 3, MatType.CV_32F);

        private void PrepareWorkingPair(Mat srcGray, Mat dstGray, out Mat srcWork, out Mat dstWork, out double scaleX, out double scaleY)
        {
            srcWork = srcGray.Clone();
            // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
            // dstWork = dstGray.Size() == srcGray.Size() ? dstGray.Clone() : dstGray.Resize(srcGray.Size());
            if (dstGray.Size() == srcGray.Size())
            {
                dstWork = dstGray.Clone();
            }
            else
            {
                dstWork = new Mat();
                Cv2.Resize(dstGray, dstWork, srcGray.Size(), 0, 0, InterpolationFlags.Linear);
            }

            var maxEdge = Math.Max(srcWork.Rows, srcWork.Cols);
            if (maxEdge <= MaxWorkingEdge)
            {
                scaleX = 1.0;
                scaleY = 1.0;
                return;
            }

            var uniformScale = MaxWorkingEdge / (double)Math.Max(1, maxEdge);
            var targetW = Math.Max(32, (int)Math.Round(srcWork.Cols * uniformScale));
            var targetH = Math.Max(32, (int)Math.Round(srcWork.Rows * uniformScale));
            var target = new OpenCvSharp.Size(targetW, targetH);

            var srcSmall = new Mat();
            var dstSmall = new Mat();
            Cv2.Resize(srcWork, srcSmall, target, 0, 0, InterpolationFlags.Area);
            Cv2.Resize(dstWork, dstSmall, target, 0, 0, InterpolationFlags.Area);

            srcWork.Dispose();
            dstWork.Dispose();
            srcWork = srcSmall;
            dstWork = dstSmall;
            scaleX = targetW / (double)Math.Max(1, srcGray.Cols);
            scaleY = targetH / (double)Math.Max(1, srcGray.Rows);
        }

        private void ScaleWarpTranslation(Mat warp, double txScale, double tyScale)
        {
            if (MotionType == MotionTypes.Homography)
            {
                warp.Set(0, 2, (float)(warp.At<float>(0, 2) * txScale));
                warp.Set(1, 2, (float)(warp.At<float>(1, 2) * tyScale));
                return;
            }

            if (warp.Rows >= 2 && warp.Cols >= 3)
            {
                warp.Set(0, 2, (float)(warp.At<float>(0, 2) * txScale));
                warp.Set(1, 2, (float)(warp.At<float>(1, 2) * tyScale));
            }
        }

        private (double dx, double dy) DecodeWarpMatrix(Mat warp)
        {
            if (warp == null || warp.Empty()) return (0, 0);

            if (MotionType == MotionTypes.Homography)
                return (warp.At<float>(0, 2), warp.At<float>(1, 2));

            return (warp.At<float>(0, 2), warp.At<float>(1, 2));
        }

        private static double NormalizeEccScore(double ecc)
            => Math.Max(0.0, Math.Min(1.0, (ecc + 1.0) / 2.0));

        private static double NormalizeFraction(double value)
            => Math.Max(0.005, Math.Min(1.0, value));

        private static Mat CropBorderRoi(Mat img, EdgeSide side, double fractionWPercent, double fractionHPercent, int minPx, out int ox, out int oy)
        {
            var h = img.Rows;
            var w = img.Cols;
            var cropW = Math.Max(minPx, (int)(w * fractionWPercent));
            var cropH = Math.Max(minPx, (int)(h * fractionHPercent));
            cropW = Math.Min(w, cropW);
            cropH = Math.Min(h, cropH);

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
                    x = w - cropW;
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
                    y = h - cropH;
                    cropW = w;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }

            ox = x;
            oy = y;
            return new Mat(img, new Rect(x, y, cropW, cropH));
        }

        private static Mat EnsureGray32F(Mat src)
        {
            var gray = new Mat();
            if (src.Channels() == 1)
                src.CopyTo(gray);
            else
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

            var f = new Mat();
            gray.ConvertTo(f, MatType.CV_32F);
            gray.Dispose();
            return f;
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
    }
}
