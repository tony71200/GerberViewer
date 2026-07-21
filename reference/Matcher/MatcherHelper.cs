using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PCM_Inspection_Demo.Matcher
{
    internal static class MatcherHelper
    {
        /// <summary>
        /// These utilities are shared by all Matcher classes.
        /// </summary>
        /// 
        // ---------------------
        // Bitmap / Rectangle -> Mat
        // ---------------------

        /// <summary>
        /// crop ROI from Bitmap and convert to grayscale Mat float [0,1].
        /// </summary> 
        /// 
        public static Mat BitmapRoiToFloatGray(Bitmap bmp, Rectangle roi)
        {
            using (Mat bgr = BitmapRoiToMat(bmp, roi))
            {
                return ToFloatGray(bgr);
            }
        }

        /// <summary>
        /// Crop ROI from Bitmap and return as BGR Mat (8U).
        /// </summary> 
        /// 
        public static Mat BitmapRoiToMat(Bitmap bmp, Rectangle roi)
        {
            // Clamp ROI to image bounds
            roi = ClampRoi(bmp, roi);

            // Lock bits to read fast pixel data
            BitmapData bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                // Create Mat to buffer
                using (Mat full = new Mat(bmp.Height, bmp.Width, MatType.CV_8UC4))
                {
                    int stride = bmpData.Stride;
                    int totalBytes = Math.Abs(stride) * bmpData.Height;
                    byte[] buffer = new byte[totalBytes];
                    Marshal.Copy(bmpData.Scan0, buffer, 0, totalBytes);
                    Marshal.Copy(buffer, 0, full.Data, totalBytes);

                    // Convert BGRA to BGR
                    using (Mat bgr = new Mat())
                    {
                        Cv2.CvtColor(full, bgr, ColorConversionCodes.BGRA2BGR);

                        // Crop ROI
                        Mat roiMat = new Mat(bgr, new Rect(roi.X, roi.Y, roi.Width, roi.Height));
                        return roiMat.Clone(); // Clone to detach from parent Mat
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }
        }

        // ---------------------
        // Convert style
        // ---------------------

        /// <summary>
        /// BGR/Gray Mat 8U → Float32 grayscale [0,1].
        /// </summary>
        /// 
        public static Mat ToFloatGray(Mat src)
        {
            Mat gray = new Mat();
            if (src.Channels() > 1) Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            else
                gray = src.Clone();

            Mat floatMat = new Mat();
            gray.ConvertTo(floatMat, MatType.CV_32F, 1.0 / 255.0);
            gray.Dispose();
            return floatMat;
        }

        // ---------------------
        // Fourier / Spectrum
        // ---------------------

        /// <summary>
        /// Calculate log-magnitude spectrum (for Fourier-Mellin).
        /// The result has normalized [0,1], and the quadrant has shifted to the center.
        /// </summary> 
        /// 
        public static Mat GetMagnitudeSpectrum(Mat floatGray)
        {
            // Padding to optimal DFT size
            int m = Cv2.GetOptimalDFTSize(floatGray.Rows);
            int n = Cv2.GetOptimalDFTSize(floatGray.Cols);

            Mat padded = new Mat();
            Cv2.CopyMakeBorder(floatGray, padded, 0, m - floatGray.Rows, 0, n - floatGray.Cols, BorderTypes.Constant, Scalar.All(0));

            // DFT
            Mat[] planes = { padded, Mat.Zeros(padded.Size(), MatType.CV_32F) };

            Mat complex = new Mat();
            Cv2.Merge(planes, complex);
            Cv2.Dft(complex, complex);

            Cv2.Split(complex, out Mat[] splitPlanes);
            Mat magnitude = new Mat();
            Cv2.Magnitude(splitPlanes[0], splitPlanes[1], magnitude);

            // Log scale
            magnitude += Scalar.All(1);
            Cv2.Log(magnitude, magnitude);

            // Crop even size + shift quadrants
            int cr = magnitude.Rows & -2;
            int cc = magnitude.Cols & -2;
            magnitude = magnitude[new Rect(0, 0, cc, cr)];
            ShiftQuadrants(magnitude);

            // Normalize
            Cv2.Normalize(magnitude, magnitude, 0, 1, NormTypes.MinMax);

            // Dispose temporaries
            complex.Dispose();
            splitPlanes[0].Dispose();
            splitPlanes[1].Dispose();
            padded.Dispose();

            return magnitude;
        }

        /// <summary>
        /// Shift the spectrum four quadrants toward the center of the image.
        /// </summary> 
        /// 
        public static void ShiftQuadrants(Mat mag)
        {
            int cx = mag.Cols / 2;
            int cy = mag.Rows / 2;

            Mat q0 = new Mat(mag, new Rect(0, 0, cx, cy));
            Mat q1 = new Mat(mag, new Rect(cx, 0, cx, cy));
            Mat q2 = new Mat(mag, new Rect(0, cy, cx, cy));
            Mat q3 = new Mat(mag, new Rect(cx, cy, cx, cy));

            Mat tmp = new Mat();
            q0.CopyTo(tmp); q3.CopyTo(q0); tmp.CopyTo(q3);
            q1.CopyTo(tmp); q2.CopyTo(q1); tmp.CopyTo(q2);
            tmp.Dispose();
        }

        // ─────────────────────
        //  Geometric transform
        // ─────────────────────
        /// <summary>
        /// Compensate rotation + scale up the image (inverse transform).
        /// </summary>
        /// 
        public static Mat CompensateTransform(Mat img, double angleDeg, double scale, OpenCvSharp.Size targetSize)
        {
            Point2f center = new Point2f(img.Cols / 2f, img.Rows / 2f);
            // Inverse transform: rotate by -angle and scale by 1/scale
            Mat M = Cv2.GetRotationMatrix2D(center, -angleDeg, 1.0 / scale);
            Mat res = new Mat();
            Cv2.WarpAffine(img, res, M, targetSize, InterpolationFlags.Linear, BorderTypes.Reflect101);
            M.Dispose();
            return res;
        }

        // ─────────────────────
        //  Utility
        // ─────────────────────

        /// <summary>
        /// Clamp ROI to Bitmap bounds.
        /// </summary>
        /// 
        public static Rectangle ClampRoi(Bitmap bmp, Rectangle roi)
        {
            int x = Math.Max(0, roi.X);
            int y = Math.Max(0, roi.Y);
            int w = Math.Min(bmp.Width - x, roi.Width);
            int h = Math.Min(bmp.Height - y, roi.Height);
            return new Rectangle(x, y, w, h);
        }

        /// <summary>
        /// Check minimum ROI size to avoid crashes in DFT or other operations.
        /// </summary>
        ///
        public static bool IsRoiValid(Rectangle roi, int minSize = 16)
            => roi.Width >= minSize && roi.Height >= minSize;
    }
}
