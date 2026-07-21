using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using System.IO;

namespace StitchingImage.Stitch_Tools.Utils
{
    public static class ImageRead
    {
        public sealed class ImageReadException : Exception
        {
            public ImageReadException(string message, Exception innerException = null) : base(message, innerException)
            {
            }
        }

        public static Mat ReadImage(string filename, ImreadModes mode)
        {
            /// <summary>
            /// Read image with OpenCvSharp; if failed, fallback to Bitmap and convert to Mat.
            /// Throws ImageReadException if all attempts fail.
            /// </summary>
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
            {
                var message = $"Image not found: {filename}";
                Logger.Error(message);
                throw new ImageReadException(message);
            }

            var reductionFactor = GetReductionFactor(mode);
            try
            {
                using (var m = Cv2.ImRead(filename, mode))
                {
                    if (m != null && !m.Empty())
                        return m.Clone();
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"OpenCV ImRead failed for {filename}: {ex.Message}");
            }

            try
            {
                using (var bmp = new Bitmap(filename))
                {
                    var wantGray = WantsGray(mode);
                    var image = BitmapToMat(bmp, wantGray);
                    if (image == null || image.Empty())
                        throw new ImageReadException($"Bitmap conversion failed for {filename}.");

                    if (reductionFactor > 1)
                        return ResizeByFactor(image, reductionFactor);
                    return image;
                }
            }
            catch (Exception ex)
            {
                var message = $"Failed to read image: {filename}";
                Logger.Error(message, ex);
                throw new ImageReadException(message, ex);
            }
        }

        private static Mat ResizeByFactor(Mat src, int reductionFactor)
        {
            if (reductionFactor <= 1)
                return src;

            var nw = Math.Max(1, src.Cols / reductionFactor);
            var nh = Math.Max(1, src.Rows / reductionFactor);
            var resized = new Mat();
            Cv2.Resize(src, resized, new OpenCvSharp.Size(nw, nh), 0, 0, InterpolationFlags.Area);
            src.Dispose();
            return resized;
        }

        private static int GetReductionFactor(ImreadModes mode)
        {
            switch (mode)
            {
                case ImreadModes.ReducedColor2:
                case ImreadModes.ReducedGrayscale2:
                    return 2;
                case ImreadModes.ReducedColor4:
                case ImreadModes.ReducedGrayscale4:
                    return 4;
                case ImreadModes.ReducedColor8:
                case ImreadModes.ReducedGrayscale8:
                    return 8;
                default:
                    return 1;
            }
        }

        public static Mat BitmapToMat(Bitmap bmp, bool isGray)
        {
            if (bmp == null) return null;
            // Normalize to 24bpp BGR for predictable conversion
            using (var bgr24 = Ensure24bppBgr(bmp))
            {
                var rect = new Rectangle(0, 0, bgr24.Width, bgr24.Height);
                var data = bgr24.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    // GDI+ Format24bppRgb is BGR order
                    using (var bgr = Mat.FromPixelData(
                        rows: bgr24.Height,
                        cols: bgr24.Width,
                        type: MatType.CV_8UC3,
                        data: data.Scan0,
                        step: (long)data.Stride))
                    {
                        if (!isGray) return bgr.Clone();
                        var gray = new Mat();
                        Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);
                        return gray;
                    }
                }
                catch
                {
                    return null;
                }
                finally
                {
                    bgr24.UnlockBits(data);
                }
            }
        }

        private static bool WantsGray(ImreadModes mode)
        {
            // Covers: Grayscale + ReducedGrayscale2/4/8
            return mode == ImreadModes.Grayscale
                   || mode == ImreadModes.ReducedGrayscale2
                   || mode == ImreadModes.ReducedGrayscale4
                   || mode == ImreadModes.ReducedGrayscale8;
        }

        private static Bitmap Ensure24bppBgr(Bitmap src)
        {
            if (src.PixelFormat == PixelFormat.Format24bppRgb)
                return (Bitmap)src.Clone();

            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(dst))
            {
                g.DrawImage(src, 0, 0, src.Width, src.Height);
            }
            return dst;
        }

        public static (ImreadModes mode, int reductionFactor) SelectReducedMode(double desiredScale)
        {
            if (desiredScale <= 0.18) return (ImreadModes.ReducedColor8, 8);
            if (desiredScale <= 0.35) return (ImreadModes.ReducedColor4, 4);
            if (desiredScale <= 0.70) return (ImreadModes.ReducedColor2, 2);
            return (ImreadModes.Color, 1);
        }


    }
}
