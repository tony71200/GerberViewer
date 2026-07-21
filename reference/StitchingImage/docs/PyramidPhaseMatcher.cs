using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using OpenCvSharp;

namespace PCM_Inspection_Demo.Matcher
{
    /// <summary>
    /// Pyramid Phase Correlation Matcher.
    /// Uses an image pyramid (coarse → fine) to expand the range of phase correlation.
    ///
    /// Advantages:
    /// - Much faster than ECC.
    /// - Can handle large translations that 1-level phase correlation cannot handle.
    /// - Can be combined with Fourier-Mellin to handle additional rotation/scale.
    ///
    /// Disadvantages:
    /// - Cannot solve rotation/scale directly (requires Fourier-Mellin as a pre-step).
    /// - Scale changes > ~20% will be less accurate.
    /// </summary>
    internal class PyramidPhaseMatcher : BaseMatcher
    {
        /// <summary>Number of pyramid levels (default 3: coarse → medium → fine).</summary>
        /// 
        public int PyramidLevels { get; set; } = 3;
        /// <summary>
        /// Minimum confidence threshold at each level.
        /// Levels below the threshold will be skipped (use shift = 0 for that level).
        /// </summary>
        /// 
        public double MinLevelConfidence { get; set; } = 0.02;
        /// <summary>
        // If true: use a hanning window before phase correlation to reduce spectral leakage.
        // Increases accuracy but takes approximately 5% more time.
        /// </summary>
        /// 
        public bool UseHanningWindow { get; set; } = true;
        /// Base Matcher
        /// 
        public override string MatcherName => $"Pyramid Phase Correlation (Levels={PyramidLevels})";
        public override MatchResult Run(
            Bitmap srcImage, Rectangle srcRoi,
            Bitmap dstImage, Rectangle dstRoi,
            CancellationToken token)
        {
            // Convention: sampleImage is reference, testImage is moving image.
            // Output transform is T(test -> reference).
            if (!MatcherHelper.IsRoiValid(srcRoi) || !MatcherHelper.IsRoiValid(dstRoi))
                return Fail("ROI is too small (minimum 16×16).");
            try
            {
                token.ThrowIfCancellationRequested();
                // 1. Get ROI → float gray
                using (Mat srcFloat = MatcherHelper.BitmapRoiToFloatGray(srcImage, srcRoi))
                using (Mat dstFloat = MatcherHelper.BitmapRoiToFloatGray(dstImage, dstRoi))
                {
                    token.ThrowIfCancellationRequested();
                    // 2. Build pyramids
                    var pyrSrc = BuildPyramid(srcFloat, PyramidLevels);
                    var pyrDst = BuildPyramid(dstFloat, PyramidLevels);

                    token.ThrowIfCancellationRequested();

                    // 3. Accumulate shift from coarse → fine
                    double totalDx = 0.0, totalDy = 0.0, sumConf = 0.0;
                    int validLevels = 0;

                    for (int i = PyramidLevels - 1; i >= 0; i--)
                    {
                        token.ThrowIfCancellationRequested();

                        double levelScale = Math.Pow(2.0, i); // Level 0 = full res, Level 1 = 1/2, Level 2 = 1/4, etc.

                        Mat s = pyrSrc[i];
                        Mat d = pyrDst[i];
                        Mat hanningWindow = null;
                        Mat sAligned = null;
                        Mat dAligned = null;
                        try
                        {
                            AlignPairSize(s, d, out sAligned, out dAligned);

                            if (UseHanningWindow)
                            {
                                hanningWindow = new Mat();
                                Cv2.CreateHanningWindow(hanningWindow, sAligned.Size(), MatType.CV_32F);
                            }

                            Point2d shift = Cv2.PhaseCorrelate(sAligned, dAligned, hanningWindow, out double conf);
                            if (conf >= MinLevelConfidence)
                            {
                                // Accumulate shift (scale up by 2x for each finer level)
                                totalDx += shift.X * levelScale;
                                totalDy += shift.Y * levelScale;
                                sumConf += conf;
                                validLevels++;
                            }
                        }
                        finally
                        {
                            hanningWindow?.Dispose();
                            sAligned?.Dispose();
                            dAligned?.Dispose();
                        }
                    }
                    foreach (var m in pyrSrc) m.Dispose();
                    foreach (var m in pyrDst) m.Dispose();

                    if (validLevels == 0)
                        return Fail("No valid pyramid levels found (confidence too low).");

                    double avgConf = sumConf / validLevels;

                    return new MatchResult
                    {
                        Success = true,
                        Dx = totalDx,
                        Dy = totalDy,
                        AngleDeg = 0.0, // Not estimated by this matcher
                        Confidence = Math.Max(0d, Math.Min(1d, avgConf)),
                        Message = $"Done (valid levels: {validLevels}, avg confidence: {avgConf:F4})"
                    };
                }
            }
            catch (OperationCanceledException)
            {
                return Fail("Operation cancelled.");
            }
            catch (Exception ex)
            {
                return Fail($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Construct a Gaussian pyramid. Index 0 = original image (fine), index N-1 = coarsest.
        /// </summary>
        /// 
        private static List<Mat> BuildPyramid(Mat img, int levels)
        {
            var pyr = new List<Mat>();
            Mat current = img.Clone();
            pyr.Add(current);

            for (int i = 1; i < levels; i++)
            {
                // Đảm bảo kích thước tối thiểu
                if (current.Rows < 16 || current.Cols < 16)
                    break;

                Mat down = new Mat();
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

            int w = Math.Max(16, Math.Min(src.Cols, dst.Cols));
            int h = Math.Max(16, Math.Min(src.Rows, dst.Rows));
            var roi = new Rect(0, 0, w, h);

            srcAligned = new Mat(src, roi).Clone();
            dstAligned = new Mat(dst, roi).Clone();
        }
    }
}
