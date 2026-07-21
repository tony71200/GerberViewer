using System;
using OpenCvSharp;

namespace StitchingImage.Stitch_Tools.Utils
{
    public static class AlignmentRefinement
    {
        /// <summary>
        /// Refine alignment in the overlap using ECC (Enhanced Correlation Coefficient).
        /// Suitable for small translation/affine updates after an initial estimate.
        /// Typical flow:
        /// 1) Warp image B into A using initial transform.
        /// 2) Crop overlap region.
        /// 3) Run Cv2.FindTransformECC to refine M.
        /// </summary>
        /// 
        public static Mat RefineWithEcc(Mat imageA, Mat imageB, Mat initialWarp, MotionTypes motionType, TermCriteria criteria)
        {
            if ( imageA == null ) throw new ArgumentNullException(nameof(imageA));
            if ( imageB == null ) throw new ArgumentNullException(nameof(imageB));
            if (imageA.Empty() || imageB.Empty())
                return EnsureWarp(initialWarp, motionType);

            var crit = criteria;
            if (crit.MaxCount <= 0 && crit.Epsilon <= 0)
                crit = new TermCriteria(CriteriaTypes.Count | CriteriaTypes.Eps, 50, 1e-6);

            using (var grayA = EnsureGray32F(imageA))
            using (var grayBBase = EnsureGray32F(imageB))
            using (var grayB = new Mat())
            {
                if (grayA.Empty() || grayBBase.Empty())
                    return EnsureWarp(initialWarp, motionType);

                if (grayA.Size() != grayBBase.Size())
                    Cv2.Resize(grayBBase, grayB, grayA.Size(), 0, 0, InterpolationFlags.Linear);
                else
                    grayBBase.CopyTo(grayB);

                var warp = EnsureWarp(initialWarp, motionType);
                if (warp.Type() != MatType.CV_32F)
                {
                    var tmp = new Mat();
                    warp.ConvertTo(tmp, MatType.CV_32F);
                    warp.Dispose();
                    warp = tmp;
                }

                Cv2.FindTransformECC(grayA, grayB, warp, motionType, crit);
                return warp;
            }
        }

        /// <summary>
        /// Refine translation using phase correlation (tx/ty only).
        /// Best when you expect pure translation and have good overlap/texture.
        /// Typical flow:
        /// 1) Warp B with initial translation.
        /// 2) Crop overlap.
        /// 3) Use Cv2.PhaseCorrelate to estimate residual shift.
        /// </summary>
        /// 
        public static Point2d RefineWithPhaseCorrelation(Mat overlapA, Mat overlapB)
        {
            if ( overlapA == null ) throw new ArgumentNullException(nameof(overlapA));
            if ( overlapB == null ) throw new ArgumentNullException(nameof(overlapB));

            using (var a = EnsureGray32F(overlapA))
            using (var bBase = EnsureGray32F(overlapB))
            using (var b = new Mat())
            using (Mat hann  = new Mat())
            {
                if (a.Empty() || bBase.Empty())
                    return new Point2d(0, 0);

                if (a.Size() != bBase.Size())
                    Cv2.Resize(bBase, b, a.Size(), 0, 0, InterpolationFlags.Linear);
                else
                    bBase.CopyTo(b);
                Cv2.CreateHanningWindow(hann, a.Size(), MatType.CV_32F);
                double response;
                return Cv2.PhaseCorrelate(a, b, hann, out response);
            }
        }

        /// <summary>
        /// Refine alignment using normalized cross-correlation (NCC) template matching.
        /// Useful when overlap texture is strong and you have a prior bounding box.
        /// Typical flow:
        /// 1) Extract a template from A (overlap).
        /// 2) Search near prior location in B.
        /// 3) Take the peak NCC as delta.
        /// </summary>
        public static Point2d RefineWithTemplateMatching(Mat searchImage, Mat templateImage)
        {
            if (searchImage == null) throw new ArgumentNullException(nameof(searchImage));
            if (templateImage == null) throw new ArgumentNullException(nameof(templateImage));

            using (var search = EnsureGray32F(searchImage))
            using (var templ = EnsureGray32F(templateImage))
            using (var result = new Mat())
            {
                if (search.Empty() || templ.Empty())
                    return new Point2d(0, 0);

                if (search.Width < templ.Width || search.Height < templ.Height)
                    return new Point2d(0, 0);

                Cv2.MatchTemplate(search, templ, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out _, out _, out var maxLoc);
                return new Point2d(maxLoc.X, maxLoc.Y);
            }
        }

        /// <summary>
        /// Estimate seam/consistency for blending or trust-region decisions.
        /// Options to explore:
        /// - Gradient magnitude differences to find stable seam.
        /// - Photometric consistency masks for preferring higher-quality regions.
        /// </summary>
        public static Mat EstimateSeamMask(Mat warpedA, Mat warpedB)
        {
            if (warpedA == null) throw new ArgumentNullException(nameof(warpedA));
            if (warpedB == null) throw new ArgumentNullException(nameof(warpedB));
            if (warpedA.Empty() || warpedB.Empty())
                return new Mat();

            using (var grayA = EnsureGray32F(warpedA))
            using (var grayBBase = EnsureGray32F(warpedB))
            using (var grayB = new Mat())
            using (var gradAx = new Mat())
            using (var gradAy = new Mat())
            using (var gradBx = new Mat())
            using (var gradBy = new Mat())
            using (var magA = new Mat())
            using (var magB = new Mat())
            {
                if (grayA.Size() != grayBBase.Size())
                    Cv2.Resize(grayBBase, grayB, grayA.Size(), 0, 0, InterpolationFlags.Linear);
                else
                    grayBBase.CopyTo(grayB);

                Cv2.Sobel(grayA, gradAx, MatType.CV_32F, 1, 0, 3);
                Cv2.Sobel(grayA, gradAy, MatType.CV_32F, 0, 1, 3);
                Cv2.Sobel(grayB, gradBx, MatType.CV_32F, 1, 0, 3);
                Cv2.Sobel(grayB, gradBy, MatType.CV_32F, 0, 1, 3);

                Cv2.Magnitude(gradAx, gradAy, magA);
                Cv2.Magnitude(gradBx, gradBy, magB);

                using (var mask = new Mat())
                {
                    Cv2.Compare(magA, magB, mask, CmpType.LE);
                    return mask.Clone();
                }
            }
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

        private static Mat EnsureWarp (Mat initialWarp, MotionTypes motionType)
        {
            if (initialWarp != null && !initialWarp.Empty())
            {
                if (motionType == MotionTypes.Homography)
                {
                    if (initialWarp.Rows == 3 && initialWarp.Cols == 3)
                        return initialWarp.Clone();
                }
                else if (initialWarp.Rows == 2 && initialWarp.Cols == 3)
                {
                    return initialWarp.Clone();
                }
            }

            if (motionType == MotionTypes.Homography)
                return Mat.Eye(3, 3, MatType.CV_32F).ToMat();
            return Mat.Eye(2, 3, MatType.CV_32F).ToMat();
        }
    }
}
