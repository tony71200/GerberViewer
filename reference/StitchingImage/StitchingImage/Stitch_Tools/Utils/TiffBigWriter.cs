using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TiffLibrary;
using TiffLibrary.PixelFormats;

namespace StitchingImage.Stitch_Tools.Utils
{
    public static class TiffBigWriter
    {
        /// <summary>
        /// Save an 8-bit grayscale image to BigTIFF (tiled + Deflate).
        /// pixels length must be width*height.
        /// </summary>
        public static async Task SaveBigTiffGray8Async(
            string outputPath,
            int width,
            int height,
            byte[] pixels,
            int tileWidth = 512,
            int tileHeight = 512,
            TiffCompression compression = TiffCompression.Deflate,
            CancellationToken cancellationToken = default)
        {
            ValidateArgs(outputPath, width, height, pixels, bytesPerPixel: 1);

            try
            {
                var builder = new TiffImageEncoderBuilder
                {
                    PhotometricInterpretation = TiffPhotometricInterpretation.BlackIsZero,
                    IsTiled = true,
                    TileSize = new TiffSize(tileWidth, tileHeight),
                    Compression = compression,
                    
                };

                var gray = new TiffGray8[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                    gray[i] = new TiffGray8(pixels[i]);

                var encoder = builder.Build<TiffGray8>();
                var pixelBuffer = new TiffMemoryPixelBuffer<TiffGray8>(gray, width, height, writable: false);

                EnsureOutputDirectory(outputPath);

                using (var writer = await TiffFileWriter.OpenAsync(outputPath, useBigTiff: true).ConfigureAwait(false))
                {
                    TiffStreamOffset ifdOffset;
                    using (var ifdWriter = writer.CreateImageFileDirectory())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await encoder.EncodeAsync(ifdWriter, pixelBuffer).ConfigureAwait(false);
                        ifdOffset = await ifdWriter.FlushAsync().ConfigureAwait(false);
                    }

                    writer.SetFirstImageFileDirectoryOffset(ifdOffset);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"SaveBigTiffGray8Async failed: {ex.GetType().Name}: {ex.Message}. Output='{outputPath}', Size={width}x{height}.",
                    ex);
            }
        }

        /// <summary>
        /// Save an 8-bit RGB image to BigTIFF (tiled + Deflate).
        /// pixelsRgb length must be width*height*3, layout RGBRGB...
        /// </summary>
        public static async Task SaveBigTiffRgb24Async(
            string outputPath,
            int width,
            int height,
            byte[] pixelsRgb,
            int tileWidth = 512,
            int tileHeight = 512,
            TiffCompression compression = TiffCompression.Deflate,
            CancellationToken cancellationToken = default)
        {
            ValidateArgs(outputPath, width, height, pixelsRgb, bytesPerPixel: 3);

            try
            {
                var builder = new TiffImageEncoderBuilder
                {
                    PhotometricInterpretation = TiffPhotometricInterpretation.RGB,
                    IsTiled = true,
                    TileSize = new TiffSize(tileWidth, tileHeight),
                    Compression = compression,
                    
                };

                long pixelCount = (long)width * height;
                var rgb = new TiffRgb24[pixelCount];

                int src = 0;
                for (long i = 0; i < pixelCount; i++)
                {
                    byte r = pixelsRgb[src++];
                    byte g = pixelsRgb[src++];
                    byte b = pixelsRgb[src++];
                    rgb[i] = new TiffRgb24(r, g, b);
                }

                var encoder = builder.Build<TiffRgb24>();
                var pixelBuffer = new TiffMemoryPixelBuffer<TiffRgb24>(rgb, width, height, writable: false);

                EnsureOutputDirectory(outputPath);

                using (var writer = await TiffFileWriter.OpenAsync(outputPath, useBigTiff: true).ConfigureAwait(false))
                {
                    TiffStreamOffset ifdOffset;
                    using (var ifdWriter = writer.CreateImageFileDirectory())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await encoder.EncodeAsync(ifdWriter, pixelBuffer).ConfigureAwait(false);
                        ifdOffset = await ifdWriter.FlushAsync().ConfigureAwait(false);
                    }

                    writer.SetFirstImageFileDirectoryOffset(ifdOffset);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"SaveBigTiffRgb24Async failed: {ex.GetType().Name}: {ex.Message}. Output='{outputPath}', Size={width}x{height}.",
                    ex);
            }
        }

        private static void EnsureOutputDirectory(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static void ValidateArgs(string outputPath, int width, int height, byte[] pixels, int bytesPerPixel)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("outputPath is empty.", nameof(outputPath));
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException($"Invalid size: {width}x{height}.");
            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));

            long expected = (long)width * height * bytesPerPixel;
            if (expected <= 0)
                throw new ArgumentOutOfRangeException($"Invalid expected byte count: {expected}.");

            if (pixels.LongLength != expected)
                throw new ArgumentException(
                    $"Pixel buffer length mismatch. Expected {expected} bytes but got {pixels.LongLength}. " +
                    $"Size={width}x{height}, bpp={bytesPerPixel}.",
                    nameof(pixels));
        }
    }
}
