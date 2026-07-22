using System;
using System.Drawing;
using GerberStitching.Tests.Matching;
using GerberViewer.Stitching.Imaging.ImageInterop;
using GerberViewer.Stitching.Transforms;
using OpenCvSharp;

namespace GerberStitching.Tests.Imaging
{
    public static class ImageInteropTests
    {
        public static void RunAll()
        {
            BitmapToMatBgrPreservesSizeChannelsAndPixels();
            MatMono8AndMono16ReportBitDepth();
            HObjectRoundTripCopiesMono8Buffer();
            TransformIdentityTranslationMultiplicationAndInversion();
            TransformMatRoundTripUsesCv64F();
            TransformHalconRoundTripUsesHomMat2D();
        }

        private static void BitmapToMatBgrPreservesSizeChannelsAndPixels()
        {
            var service = new ImageInteropService();
            using (var bitmap = MatcherSyntheticImageFactory.CreateAsymmetricBgrBitmap(64, 48))
            using (var mat = service.ToMatCopy(bitmap, InteropPixelFormat.Bgr8))
            {
                AssertEqual(64, mat.Cols, "Mat width must match Bitmap width.");
                AssertEqual(48, mat.Rows, "Mat height must match Bitmap height.");
                AssertEqual(3, mat.Channels(), "BGR8 Mat must have 3 channels.");
                AssertEqual(MatType.CV_8UC3, mat.Type(), "BGR8 Mat must be CV_8UC3.");
                var pixel = mat.At<Vec3b>(1, 1);
                AssertEqual(30, pixel.Item0, "Bitmap RGB blue component must map to Mat B channel.");
                AssertEqual(20, pixel.Item1, "Bitmap RGB green component must map to Mat G channel.");
                AssertEqual(10, pixel.Item2, "Bitmap RGB red component must map to Mat R channel.");
                var info = service.Describe(mat);
                AssertEqual(InteropPixelFormat.Bgr8, info.PixelFormat, "Mat description must report BGR8.");
                AssertEqual(8, info.BitDepth, "BGR8 bit depth must be 8.");
            }
        }

        private static void MatMono8AndMono16ReportBitDepth()
        {
            var service = new ImageInteropService();
            using (var mono8 = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(32, 24))
            using (var mono16 = MatcherSyntheticImageFactory.CreateAsymmetricMono16Mat(32, 24))
            {
                var info8 = service.Describe(mono8);
                AssertEqual(InteropPixelFormat.Mono8, info8.PixelFormat, "Mono8 format must be described.");
                AssertEqual(1, info8.ChannelCount, "Mono8 channel count must be 1.");
                AssertEqual(8, info8.BitDepth, "Mono8 bit depth must be 8.");
                AssertEqual(37, mono8.At<byte>(1, 1), "Mono8 representative pixel must be preserved in source test image.");

                var info16 = service.Describe(mono16);
                AssertEqual(InteropPixelFormat.Mono16, info16.PixelFormat, "Mono16 format must be described.");
                AssertEqual(1, info16.ChannelCount, "Mono16 channel count must be 1.");
                AssertEqual(16, info16.BitDepth, "Mono16 bit depth must be 16.");
                AssertEqual(1024, mono16.At<ushort>(1, 1), "Mono16 representative pixel must be preserved in source test image.");
            }
        }

        private static void HObjectRoundTripCopiesMono8Buffer()
        {
            var service = new ImageInteropService();
            using (var source = MatcherSyntheticImageFactory.CreateAsymmetricMono8Mat(32, 24))
            using (var hObject = service.ToHObjectCopy(source, InteropPixelFormat.Mono8))
            using (var roundTrip = service.ToMatCopy(hObject))
            {
                AssertEqual(source.Cols, roundTrip.Cols, "HObject round-trip width must match source Mat width.");
                AssertEqual(source.Rows, roundTrip.Rows, "HObject round-trip height must match source Mat height.");
                AssertEqual(1, roundTrip.Channels(), "HObject round-trip Mono8 Mat must have one channel.");
                AssertEqual(MatType.CV_8UC1, roundTrip.Type(), "HObject round-trip Mono8 Mat must be CV_8UC1.");
                AssertEqual(37, roundTrip.At<byte>(1, 1), "HObject round-trip representative pixel must be copied, not borrowed from a disposed buffer.");
                AssertEqual(211, roundTrip.At<byte>(roundTrip.Rows - 2, roundTrip.Cols - 2), "HObject round-trip asymmetric marker must be preserved.");
            }
        }

        private static void TransformIdentityTranslationMultiplicationAndInversion()
        {
            var identity = Transform2D.Identity;
            TransformAssert.IsIdentity(identity, 1e-12, "Identity transform must be identity.");

            var translation = Transform2D.Translation(12.5, -7.25);
            var expectedTranslation = new[,] { { 1d, 0d, 12.5 }, { 0d, 1d, -7.25 }, { 0d, 0d, 1d } };
            TransformAssert.AreEqual(expectedTranslation, translation.ToArray(), 1e-12, "Translation matrix must be canonical double[3,3].");

            var multiplied = translation.Multiply(identity);
            TransformAssert.AreEqual(expectedTranslation, multiplied.ToArray(), 1e-12, "Multiplying by identity must preserve translation.");

            var inverse = translation.Invert();
            TransformAssert.IsIdentity(translation.Multiply(inverse), 1e-12, "Transform multiplied by inverse must be identity.");
        }

        private static void TransformMatRoundTripUsesCv64F()
        {
            var matrix = Transform2D.Translation(3.25, 4.5).ToArray();
            using (var mat = Transform2D.ToMatCv64FCopy(matrix))
            {
                AssertEqual(MatType.CV_64FC1, mat.Type(), "Transform Mat must be CV_64F.");
                TransformAssert.AreEqual(matrix, Transform2D.FromMatCv64FCopy(mat), 1e-12, "CV_64F transform round trip must preserve values.");
            }
        }

        private static void TransformHalconRoundTripUsesHomMat2D()
        {
            var matrix = new[,] { { 1d, 0d, 9d }, { 0d, 1d, -4d }, { 0d, 0d, 1d } };
            using (var tuple = Transform2D.ToHalconHomMat2DCopy(matrix))
            {
                AssertEqual(6, tuple.Length, "HALCON HomMat2D affine tuple must have 6 values.");
                TransformAssert.AreEqual(matrix, Transform2D.FromHalconHomMat2DCopy(tuple), 1e-12, "HALCON HomMat2D round trip must preserve affine values.");
            }
        }

        private static void AssertEqual(int expected, int actual, string message)
        {
            if (expected != actual) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual);
        }

        private static void AssertEqual(MatType expected, MatType actual, string message)
        {
            if (expected != actual) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual);
        }

        private static void AssertEqual(InteropPixelFormat expected, InteropPixelFormat actual, string message)
        {
            if (expected != actual) throw new InvalidOperationException(message + " Expected: " + expected + "; Actual: " + actual);
        }
    }
}
