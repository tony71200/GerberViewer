using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using CvPoint = OpenCvSharp.Point;
using DrawingPoint = System.Drawing.Point;

namespace GerberStitching.Tests.Matching
{
    public static class MatcherSyntheticImageFactory
    {
        public static Bitmap CreateAsymmetricBgrBitmap(int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Black);
                using (var red = new SolidBrush(Color.FromArgb(220, 20, 40)))
                using (var green = new SolidBrush(Color.FromArgb(20, 180, 70)))
                using (var blue = new SolidBrush(Color.FromArgb(30, 80, 230)))
                using (var yellow = new SolidBrush(Color.FromArgb(230, 220, 30)))
                {
                    g.FillRectangle(red, width / 8, height / 6, width / 4, height / 5);
                    g.FillEllipse(green, width / 2, height / 5, width / 5, height / 3);
                    g.FillPolygon(blue, new[] { new DrawingPoint(width - width / 5, height - height / 6), new DrawingPoint(width / 2, height - height / 4), new DrawingPoint(width - width / 3, height / 2) });
                    g.FillRectangle(yellow, width / 3, height - height / 5, width / 7, height / 9);
                }
            }
            bitmap.SetPixel(1, 1, Color.FromArgb(10, 20, 30));
            bitmap.SetPixel(width - 2, height - 2, Color.FromArgb(200, 100, 50));
            return bitmap;
        }

        public static Mat CreateAsymmetricMono8Mat(int width, int height)
        {
            var mat = new Mat(height, width, MatType.CV_8UC1, Scalar.All(0));
            Cv2.Rectangle(mat, new Rect(width / 8, height / 6, width / 4, height / 5), Scalar.All(80), -1);
            Cv2.Circle(mat, new CvPoint(width * 3 / 5, height / 3), width / 10, Scalar.All(180), -1);
            Cv2.Line(mat, new CvPoint(width / 4, height * 3 / 4), new CvPoint(width - width / 6, height / 2), Scalar.All(240), 3);
            mat.Set<byte>(1, 1, 37);
            mat.Set<byte>(height - 2, width - 2, 211);
            return mat;
        }

        public static Mat CreateAsymmetricMono16Mat(int width, int height)
        {
            var mat = new Mat(height, width, MatType.CV_16UC1, Scalar.All(0));
            Cv2.Rectangle(mat, new Rect(width / 8, height / 6, width / 4, height / 5), Scalar.All(4096), -1);
            Cv2.Circle(mat, new CvPoint(width * 3 / 5, height / 3), width / 10, Scalar.All(32768), -1);
            Cv2.Line(mat, new CvPoint(width / 4, height * 3 / 4), new CvPoint(width - width / 6, height / 2), Scalar.All(60000), 3);
            mat.Set<ushort>(1, 1, 1024);
            mat.Set<ushort>(height - 2, width - 2, 50000);
            return mat;
        }
    }
}
