using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using GerberViewer.Stitching.Comparison;
using GerberViewer.Stitching.Models;

namespace GerberStitching.Tests.Comparison
{
    public static class SampleComparisonServiceTests
    {
        public static void RunAll()
        {
            ManifestV1BackwardCompatible();
            ManifestV2MetadataValidation();
            IdentitySourceToProcessedIsAuthoritative();
            ResizeTransformIsAuthoritative();
            MissingTransformBlocksAuthoritativeOverlay();
            AlphaOverlayProduct();
            DifferenceProduct();
            EdgeOverlayProduct();
            MetricSanity();
            PreviewScaling();
        }

        private static void ManifestV1BackwardCompatible()
        {
            using (var fixture = new Fixture(32, 32))
            {
                var path = Path.Combine(fixture.Root, "manifest_v1.json");
                File.WriteAllText(path, "{\"ManifestVersion\":1,\"RootDirectory\":\"" + EscapeJson(fixture.Root) + "\",\"SourceRasterPath\":\"" + EscapeJson(fixture.SourcePath) + "\",\"SourceWidth\":32,\"SourceHeight\":32,\"ProcessedWidth\":32,\"ProcessedHeight\":32,\"CropOrder\":\"Zigzag\",\"StartOrder\":\"TopLeft\",\"Tiles\":[{\"OrderIndex\":0,\"Row\":0,\"Column\":0,\"ExpectedPath\":\"tile.png\",\"ExpectedX\":0,\"ExpectedY\":0,\"Width\":32,\"Height\":32}]}");
                var manifest = SampleManifestSerializer.Read(path);
                var result = SampleManifestValidator.Validate(manifest, false);
                AssertTrue(result.IsValid, "Manifest v1 must remain valid for backward-compatible readers.");
            }
        }

        private static void ManifestV2MetadataValidation()
        {
            using (var fixture = new Fixture(32, 32))
            {
                var manifest = fixture.CreateManifest(SampleManifest.CurrentVersion, fixture.SourcePath, IdentityJagged(), 32, 32, 32, 32);
                manifest.PreprocessMode = "SyntheticIdentity";
                manifest.ProcessedChannelCount = 3;
                manifest.ProcessedBitDepth = 8;
                var path = Path.Combine(fixture.Root, "manifest_v2.json");
                SampleManifestSerializer.WriteValidated(path, manifest, false);
                var readback = SampleManifestSerializer.Read(path);
                var result = SampleManifestValidator.Validate(readback, false);
                AssertTrue(result.IsValid, "Manifest v2 metadata must validate without breaking existing tile contract.");
                AssertTrue(readback.SourceToProcessedTransform != null, "Manifest v2 reader must preserve SourceToProcessedTransform.");
            }
        }

        private static void IdentitySourceToProcessedIsAuthoritative()
        {
            using (var fixture = new Fixture(32, 32))
            {
                var manifest = fixture.CreateManifest(SampleManifest.CurrentVersion, null, IdentityJagged(), 32, 32, 32, 32);
                var result = fixture.Generate(manifest, false, 4);
                AssertTrue(result.IsAuthoritative, "Identity SourceToProcessedTransform must be authoritative.");
                AssertProducts(result);
            }
        }

        private static void ResizeTransformIsAuthoritative()
        {
            using (var fixture = new Fixture(20, 20))
            {
                var manifest = fixture.CreateManifest(SampleManifest.CurrentVersion, null, new[] { new[] { 2d, 0d, 0d }, new[] { 0d, 2d, 0d }, new[] { 0d, 0d, 1d } }, 20, 20, 40, 40);
                fixture.WriteStitched(40, 40);
                var result = fixture.Generate(manifest, false, 4);
                AssertTrue(result.IsAuthoritative, "Explicit resize transform must be authoritative.");
                AssertProducts(result);
            }
        }

        private static void MissingTransformBlocksAuthoritativeOverlay()
        {
            using (var fixture = new Fixture(24, 24))
            {
                var manifest = fixture.CreateManifest(SampleManifest.CurrentVersion, null, null, 24, 24, 48, 48);
                fixture.WriteStitched(48, 48);
                var result = fixture.Generate(manifest, false, 4);
                AssertFalse(result.IsAuthoritative, "Missing transform must not be authoritative when dimensions differ.");
                AssertFalse(result.ProductsGenerated, "Authoritative products must be blocked when non-authoritative preview is disabled.");
                AssertTrue(File.Exists(result.MetadataPath), "Blocked comparison must still write metadata with warning.");
            }
        }

        private static void AlphaOverlayProduct()
        {
            using (var fixture = new Fixture(32, 32))
            {
                var result = fixture.Generate(fixture.CreateManifest(1, null, null, 32, 32, 32, 32), false, 4);
                AssertTrue(File.Exists(result.AlphaOverlayPath), "Alpha overlay product must be generated.");
            }
        }

        private static void DifferenceProduct()
        {
            using (var fixture = new Fixture(32, 32))
            {
                var result = fixture.Generate(fixture.CreateManifest(1, null, null, 32, 32, 32, 32), false, 4);
                AssertTrue(File.Exists(result.AbsoluteDifferencePath), "Absolute difference product must be generated.");
            }
        }

        private static void EdgeOverlayProduct()
        {
            using (var fixture = new Fixture(32, 32))
            {
                var result = fixture.Generate(fixture.CreateManifest(1, null, null, 32, 32, 32, 32), false, 4);
                AssertTrue(File.Exists(result.EdgeOverlayPath), "Edge overlay product must be generated.");
            }
        }

        private static void MetricSanity()
        {
            using (var fixture = new Fixture(32, 32))
            {
                var result = fixture.Generate(fixture.CreateManifest(1, null, null, 32, 32, 32, 32), false, 4);
                AssertTrue(result.Metrics.ValidOverlapRatio > 0.5, "Valid overlap ratio must reflect overlapping non-background content.");
                AssertTrue(result.Metrics.BinaryMaskIoU >= 0 && result.Metrics.BinaryMaskIoU <= 1, "Binary mask IoU must be normalized.");
                AssertTrue(result.Metrics.EdgeOverlap >= 0 && result.Metrics.EdgeOverlap <= 1, "Edge overlap must be normalized.");
            }
        }

        private static void PreviewScaling()
        {
            using (var fixture = new Fixture(120, 120))
            {
                var result = fixture.Generate(fixture.CreateManifest(1, null, null, 120, 120, 120, 120), false, 0.001);
                using (var preview = new Bitmap(result.SamplePreviewPath)) AssertTrue(preview.Width < 120 && preview.Height < 120, "Preview must be bounded by MaxPreviewMegapixels.");
            }
        }

        private static SampleManifest Manifest(int version, string processedPath, double[][] transform, int sourceWidth, int sourceHeight, int processedWidth, int processedHeight)
        {
            return new SampleManifest
            {
                ManifestVersion = version,
                RootDirectory = Path.GetTempPath(),
                SourceRasterPath = "source.png",
                ProcessedSamplePath = processedPath,
                SourceToProcessedTransform = transform,
                SourceWidth = sourceWidth,
                SourceHeight = sourceHeight,
                ProcessedWidth = processedWidth,
                ProcessedHeight = processedHeight,
                CropOrder = "Zigzag",
                StartOrder = "TopLeft",
                CreatedUtc = DateTime.UtcNow,
                Tiles = new System.Collections.Generic.List<SampleTileInfo> { new SampleTileInfo { OrderIndex = 0, Row = 0, Column = 0, ExpectedPath = "tile.png", ExpectedX = 0, ExpectedY = 0, Width = Math.Max(1, processedWidth), Height = Math.Max(1, processedHeight) } }
            };
        }

        private static double[][] IdentityJagged() { return new[] { new[] { 1d, 0d, 0d }, new[] { 0d, 1d, 0d }, new[] { 0d, 0d, 1d } }; }

        private sealed class Fixture : IDisposable
        {
            public Fixture(int width, int height)
            {
                Root = Path.Combine(Path.GetTempPath(), "gv_compare_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Root);
                SourcePath = Path.Combine(Root, "source.png");
                StitchedPath = Path.Combine(Root, "stitched.png");
                OutputDirectory = Path.Combine(Root, "comparison");
                WriteImage(SourcePath, width, height, Color.Black, Color.White, 0);
                WriteImage(StitchedPath, width, height, Color.Black, Color.White, 2);
            }
            public string Root { get; private set; }
            public string SourcePath { get; private set; }
            public string StitchedPath { get; private set; }
            public string OutputDirectory { get; private set; }
            public void WriteStitched(int width, int height) { WriteImage(StitchedPath, width, height, Color.Black, Color.White, 2); }
            public SampleManifest CreateManifest(int version, string processedPath, double[][] transform, int sourceWidth, int sourceHeight, int processedWidth, int processedHeight)
            {
                var manifest = Manifest(version, processedPath == null ? null : SourcePath, transform, sourceWidth, sourceHeight, processedWidth, processedHeight);
                manifest.RootDirectory = Root;
                manifest.SourceRasterPath = SourcePath;
                return manifest;
            }
            public SampleComparisonResult Generate(SampleManifest manifest, bool allowNonAuthoritative, double maxPreviewMp)
            {
                return new SampleComparisonService().Generate(new SampleComparisonRequest { Manifest = manifest, StitchedImagePath = StitchedPath, OutputDirectory = OutputDirectory, AllowNonAuthoritativeVisualPreview = allowNonAuthoritative, Alpha = 0.5, MaxPreviewMegapixels = maxPreviewMp }, System.Threading.CancellationToken.None);
            }
            public void Dispose() { if (Directory.Exists(Root)) Directory.Delete(Root, true); }
        }

        private static void WriteImage(string path, int width, int height, Color background, Color feature, int offset)
        {
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(background);
                using (var brush = new SolidBrush(feature)) g.FillRectangle(brush, 3 + offset, 4, Math.Max(2, width / 3), Math.Max(2, height / 4));
                g.DrawEllipse(Pens.Gray, Math.Max(1, width / 2), Math.Max(1, height / 3), Math.Max(2, width / 5), Math.Max(2, height / 6));
                bitmap.Save(path, ImageFormat.Png);
            }
        }

        private static string EscapeJson(string value) { return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\""); }

        private static void AssertProducts(SampleComparisonResult result)
        {
            AssertTrue(result.ProductsGenerated, "Comparison products must be marked generated.");
            AssertTrue(File.Exists(result.SamplePreviewPath), "Sample preview must exist.");
            AssertTrue(File.Exists(result.StitchedPreviewPath), "Stitched preview must exist.");
            AssertTrue(File.Exists(result.AlphaOverlayPath), "Alpha overlay must exist.");
            AssertTrue(File.Exists(result.AbsoluteDifferencePath), "Absolute difference must exist.");
            AssertTrue(File.Exists(result.EdgeOverlayPath), "Edge overlay must exist.");
            AssertTrue(File.Exists(result.MetadataPath), "comparison_metadata.json must exist.");
        }

        private static void AssertTrue(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static void AssertFalse(bool value, string message) { if (value) throw new InvalidOperationException(message); }
    }
}
