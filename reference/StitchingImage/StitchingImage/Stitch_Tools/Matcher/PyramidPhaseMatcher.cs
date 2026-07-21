using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace StitchingImage.Stitch_Tools.Matcher
{
    /// <summary>
    /// [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
    /// Ported from docs/PyramidPhaseMatcher.cs and adapted to PairMatching pipeline.
    /// </summary>
    public sealed class PyramidPhaseMatcher : PairMatching
    {
        public PyramidPhaseMatcher(StitchingImage.Stitch_Tools.Utils.StitchingConfig cfg) : base(cfg) { }

        public int PyramidLevels { get; set; } = 3;
        public double MinLevelConfidence { get; set; } = 0.02;
        public bool UseHanningWindow { get; set; } = true;

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

                    var (ok, dx, dy, conf, reason) = RunPyramid(aGray, bGray);
                    if (!ok)
                        return Fail(reason);

                    var scaleRatio = scaleAWork / Math.Max(1e-9, scaleBWork);
                    var txWork = (ax - (bx * scaleRatio)) + dx;
                    var tyWork = (ay - (by * scaleRatio)) + dy;

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
                        Accuracy = Clamp01((0.8 * conf) + (0.2 * overlap))
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
                        DThetaRad = 0,
                        Tx = tx,
                        Ty = ty,
                        Eval = ev
                    };
                }
            }
        }

        private (bool ok, double dx, double dy, double confidence, string reason) RunPyramid(Mat src, Mat dst)
        {
            var pyrSrc = BuildPyramid(src, PyramidLevels);
            var pyrDst = BuildPyramid(dst, PyramidLevels);
            try
            {
                double totalDx = 0.0, totalDy = 0.0, sumConf = 0.0;
                int validLevels = 0;

                for (int i = Math.Min(pyrSrc.Count, pyrDst.Count) - 1; i >= 0; i--)
                {
                    double levelScale = Math.Pow(2.0, i);
                    Mat sAligned = null, dAligned = null, hanning = null;
                    try
                    {
                        AlignPairSize(pyrSrc[i], pyrDst[i], out sAligned, out dAligned);
                        if (UseHanningWindow)
                        {
                            hanning = new Mat();
                            Cv2.CreateHanningWindow(hanning, sAligned.Size(), MatType.CV_32F);
                        }

                        var shift = Cv2.PhaseCorrelate(sAligned, dAligned, hanning, out var conf);
                        if (conf >= MinLevelConfidence)
                        {
                            totalDx += (-shift.X) * levelScale;
                            totalDy += (-shift.Y) * levelScale;
                            sumConf += conf;
                            validLevels++;
                        }
                    }
                    finally
                    {
                        sAligned?.Dispose();
                        dAligned?.Dispose();
                        hanning?.Dispose();
                    }
                }

                if (validLevels == 0)
                    return (false, 0, 0, 0, "pyramid_low_confidence");

                return (true, totalDx, totalDy, sumConf / validLevels, "ok");
            }
            finally
            {
                foreach (var mat in pyrSrc) mat.Dispose();
                foreach (var mat in pyrDst) mat.Dispose();
            }
        }

        private static List<Mat> BuildPyramid(Mat img, int levels)
        {
            var pyr = new List<Mat>();
            var current = img.Clone();
            pyr.Add(current);
            for (int i = 1; i < levels; i++)
            {
                if (current.Rows < 16 || current.Cols < 16)
                    break;

                var down = new Mat();
                Cv2.PyrDown(current, down);
                pyr.Add(down);
                current = down;
            }
            return pyr;
        }

        private static void AlignPairSize(Mat src, Mat dst, out Mat srcAligned, out Mat dstAligned)
        {
            if (src.Size() == dst.Size())
            {
                srcAligned = src.Clone();
                dstAligned = dst.Clone();
                return;
            }

            var w = Math.Max(16, Math.Min(src.Cols, dst.Cols));
            var h = Math.Max(16, Math.Min(src.Rows, dst.Rows));
            var roi = new Rect(0, 0, w, h);
            srcAligned = new Mat(src, roi).Clone();
            dstAligned = new Mat(dst, roi).Clone();
        }

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
                    x = 0; y = 0; cropH = h;
                    break;
                case EdgeSide.Right:
                    x = w - cropW; y = 0; cropH = h;
                    break;
                case EdgeSide.Top:
                    x = 0; y = 0; cropW = w;
                    break;
                case EdgeSide.Bottom:
                    x = 0; y = h - cropH; cropW = w;
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
