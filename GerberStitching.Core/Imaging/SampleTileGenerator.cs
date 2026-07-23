using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GerberViewer.Stitching.Configuration;
using GerberViewer.Stitching.Models;
using ConfigGerberSampleConfig = GerberViewer.Stitching.Configuration.GerberSampleConfig;
using HalconDotNet;

namespace GerberViewer.Stitching.Imaging
{
    public sealed class SampleCropProgress { public int Completed { get; set; } public int Total { get; set; } public int OrderIndex { get; set; } public SampleTileState State { get; set; } public string Message { get; set; } }
    public sealed class SampleCropResult { public string OutputDirectory { get; set; } public string ManifestPath { get; set; } public bool Completed { get; set; } }

    public sealed class SampleTileGenerator
    {
        public Task<SampleCropResult> GenerateAsync(PreparedSampleRun preparedRun, string outputRoot, CancellationToken cancellationToken, IProgress<SampleCropProgress> progress)
        {
            return Task.Run(() => Generate(preparedRun, outputRoot, cancellationToken, progress), cancellationToken);
        }

        private SampleCropResult Generate(PreparedSampleRun run, string outputRoot, CancellationToken token, IProgress<SampleCropProgress> progress)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (string.IsNullOrWhiteSpace(outputRoot)) throw new ArgumentException("Output root is required.", nameof(outputRoot));
            var root = Path.GetFullPath(outputRoot);
            var runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var temp = Path.Combine(root, ".creating_" + runId);
            var final = Path.Combine(root, "GerberSample_" + runId);
            var marker = Path.Combine(temp, ".gerber_sample_run");
            try
            {
                Directory.CreateDirectory(root);
                Directory.CreateDirectory(temp);
                File.WriteAllText(marker, runId, Encoding.UTF8);
                var tilesDir = Path.Combine(temp, "tiles");
                Directory.CreateDirectory(tilesDir);
                var total = run.TilesByOrder.Count;
                foreach (var tile in run.TilesByOrder)
                {
                    token.ThrowIfCancellationRequested();
                    progress?.Report(new SampleCropProgress { Completed = Math.Max(0, tile.OrderIndex), Total = total, OrderIndex = tile.OrderIndex, State = SampleTileState.Processing, Message = "Cropping " + tile.OrderIndex });
                    var fileName = FormatName(run.ConfigSnapshot.TileNamePattern, tile.Row, tile.Column, tile.OrderIndex) + ExtensionFor(run.ConfigSnapshot.OutputFormat);
                    var path = Path.Combine(tilesDir, fileName);
                    using (var cropped = Crop(run.ProcessedImage, tile))
                    {
                        HOperatorSet.WriteImage(cropped, HalconFormatFor(run.ConfigSnapshot.OutputFormat), 0, path);
                    }
                    VerifyImageReadable(path);
                    progress?.Report(new SampleCropProgress { Completed = tile.OrderIndex + 1, Total = total, OrderIndex = tile.OrderIndex, State = SampleTileState.Completed, Message = fileName });
                }
                var processedSampleFileName = "processed_sample.tiff";
                WriteProcessedSample(Path.Combine(temp, processedSampleFileName), run.ProcessedImage);
                WriteConfig(Path.Combine(temp, "sample_config.json"), run.ConfigSnapshot);
                var manifest = BuildManifest(run, final);
                ValidateTileFilesInTemp(run, temp);
                var manifestPathInTemp = Path.Combine(temp, "sample_manifest.json");
                SampleManifestSerializer.WriteValidated(manifestPathInTemp, manifest, false);
                if (Directory.Exists(final)) throw new IOException("Final run directory already exists: " + final);
                Directory.Move(temp, final);
                var manifestPath = Path.Combine(final, "sample_manifest.json");
                var finalValidation = SampleManifestValidator.Validate(SampleManifestSerializer.Read(manifestPath), true);
                if (!finalValidation.IsValid) throw new InvalidOperationException("Published manifest validation failed: " + string.Join(Environment.NewLine, finalValidation.Errors));
                return new SampleCropResult { OutputDirectory = final, ManifestPath = manifestPath, Completed = true };
            }
            catch
            {
                SafeCleanTemp(temp, marker);
                throw;
            }
        }

        private static HObject Crop(HObject source, SampleTileLayout tile)
        {
            HObject part = null;
            HOperatorSet.CropRectangle1(source, out part, tile.Rectangle.Top, tile.Rectangle.Left, tile.Rectangle.Bottom - 1, tile.Rectangle.Right - 1);
            return part;
        }

        private static void VerifyImageReadable(string path)
        {
            HObject read = null;
            try { HOperatorSet.ReadImage(out read, path); }
            finally { if (read != null && read.IsInitialized()) read.Dispose(); }
        }

        private static void ValidateTileFilesInTemp(PreparedSampleRun run, string tempRoot)
        {
            foreach (var tile in run.TilesByOrder)
            {
                var fileName = FormatName(run.ConfigSnapshot.TileNamePattern, tile.Row, tile.Column, tile.OrderIndex) + ExtensionFor(run.ConfigSnapshot.OutputFormat);
                var path = Path.Combine(tempRoot, "tiles", fileName);
                if (!File.Exists(path)) throw new FileNotFoundException("Generated tile is missing before publication: " + path, path);
            }
        }

        private static SampleManifest BuildManifest(PreparedSampleRun run, string finalRoot)
        {
            return new SampleManifest
            {
                ManifestVersion = SampleManifest.CurrentVersion,
                RootDirectory = finalRoot,
                SourceRasterPath = run.ConfigSnapshot.SourceRasterPath,
                SourceWidth = run.SourceWidth,
                SourceHeight = run.SourceHeight,
                ProcessedWidth = run.ProcessedWidth,
                ProcessedHeight = run.ProcessedHeight,
                CropOrder = run.ConfigSnapshot.CropOrder.ToString(),
                StartOrder = run.ConfigSnapshot.StartOrder.ToString(),
                CreatedUtc = DateTime.UtcNow,
                ProcessedSamplePath = Path.Combine(finalRoot, "processed_sample.tiff"),
                SourceToProcessedTransform = SourceToProcessedTransform(run),
                PreprocessMode = run.PreprocessMetadata == null ? null : run.PreprocessMetadata.Mode.ToString(),
                ProcessedChannelCount = CountChannels(run.ProcessedImage),
                ProcessedBitDepth = BitDepth(run.ProcessedImage),
                Tiles = run.TilesByOrder.Select(t => new SampleTileInfo { OrderIndex = t.OrderIndex, Row = t.Row, Column = t.Column, ExpectedPath = Path.Combine(finalRoot, "tiles", FormatName(run.ConfigSnapshot.TileNamePattern, t.Row, t.Column, t.OrderIndex) + ExtensionFor(run.ConfigSnapshot.OutputFormat)), ExpectedX = t.Rectangle.X, ExpectedY = t.Rectangle.Y, Width = t.Rectangle.Width, Height = t.Rectangle.Height }).ToList()
            };
        }

        private static void WriteProcessedSample(string path, HObject processedImage)
        {
            HOperatorSet.WriteImage(processedImage, "tiff", 0, path);
            VerifyImageReadable(path);
        }

        private static double[][] SourceToProcessedTransform(PreparedSampleRun run)
        {
            var sx = run.SourceWidth == 0 ? 1.0 : (double)run.ProcessedWidth / run.SourceWidth;
            var sy = run.SourceHeight == 0 ? 1.0 : (double)run.ProcessedHeight / run.SourceHeight;
            return new[] { new[] { sx, 0d, 0d }, new[] { 0d, sy, 0d }, new[] { 0d, 0d, 1d } };
        }

        private static int CountChannels(HObject image)
        {
            HTuple channels = null;
            try { HOperatorSet.CountChannels(image, out channels); return channels.I; }
            finally { if (channels != null) channels.Dispose(); }
        }

        private static int BitDepth(HObject image)
        {
            HTuple type = null;
            try
            {
                HOperatorSet.GetImageType(image, out type);
                var value = type.S;
                if (value == "byte") return 8;
                if (value == "uint2" || value == "int2") return 16;
                if (value == "int4" || value == "real") return 32;
                return 0;
            }
            finally { if (type != null) type.Dispose(); }
        }

        private static void WriteConfig(string path, ConfigGerberSampleConfig config)
        {
            using (var stream = File.Create(path)) new DataContractJsonSerializer(typeof(ConfigGerberSampleConfig)).WriteObject(stream, config);
        }

        private static void SafeCleanTemp(string temp, string marker)
        {
            if (string.IsNullOrWhiteSpace(temp) || !Directory.Exists(temp) || string.IsNullOrWhiteSpace(marker) || !File.Exists(marker)) return;
            Directory.Delete(temp, true);
        }

        private static string ExtensionFor(SampleOutputFormat format) { switch (format) { case SampleOutputFormat.Bmp: return ".bmp"; case SampleOutputFormat.Jpeg: return ".jpg"; default: return ".png"; } }
        private static string HalconFormatFor(SampleOutputFormat format) { switch (format) { case SampleOutputFormat.Bmp: return "bmp"; case SampleOutputFormat.Jpeg: return "jpeg"; default: return "png"; } }
        private static string FormatName(string p, int r, int c, int o) { return (p ?? "Sample_R{row:00}_C{col:00}_O{order:000}").Replace("{row:00}", r.ToString("00")).Replace("{col:00}", c.ToString("00")).Replace("{order:000}", o.ToString("000")); }
    }
}
