using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GerberViewer.Stitching.Configuration;
using GerberViewer.Stitching.RobotManager;
using GerberViewer.Stitching.Utils;

namespace GerberViewer.Stitching.Imaging
{
    public sealed class SampleCropProgress { public int Completed { get; set; } public int Total { get; set; } public string Message { get; set; } }
    public sealed class SampleCropResult { public string OutputDirectory { get; set; } public string ManifestPath { get; set; } public bool Completed { get; set; } }
    internal sealed class CropRectInfo { public int Row; public int Column; public int OrderIndex; public Rectangle Rect; public string FileName; }

    public sealed class SampleTileGenerator
    {
        public Task<SampleCropResult> GenerateAsync(GerberSampleConfig config, CancellationToken cancellationToken, IProgress<SampleCropProgress> progress)
        {
            return Task.Run(() => Generate(config, cancellationToken, progress), cancellationToken);
        }

        private SampleCropResult Generate(GerberSampleConfig config, CancellationToken token, IProgress<SampleCropProgress> progress)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.SourceRasterPath)) throw new ArgumentException("SourceRasterPath is required.");
            var output = string.IsNullOrWhiteSpace(config.OutputDirectory) ? Path.Combine(Path.GetDirectoryName(config.SourceRasterPath), "sample_output") : config.OutputDirectory;
            var tilesDir = Path.Combine(output, "tiles");
            var incomplete = Path.Combine(output, "INCOMPLETE.txt");
            try
            {
                if (Directory.Exists(output)) Directory.Delete(output, true);
                Directory.CreateDirectory(tilesDir);
                using (var original = ImageRead.ReadBitmap(config.SourceRasterPath))
                using (var processed = Preprocess(original, config))
                {
                    var validation = GerberSampleConfigValidator.Validate(config, original.Size);
                    if (!validation.IsValid) throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));
                    var tiles = BuildTiles(config, processed.Width, processed.Height);
                    SaveOverlay(processed.Size, tiles, Path.Combine(output, "sample_overlay.png"));
                    var total = tiles.Count;
                    foreach (var tile in tiles)
                    {
                        token.ThrowIfCancellationRequested();
                        using (var bmp = processed.Clone(tile.Rect, processed.PixelFormat))
                        {
                            if (config.InvertImage) Invert(bmp);
                            bmp.Save(Path.Combine(tilesDir, tile.FileName), ImageFormatFor(config.OutputFormat));
                        }
                        progress?.Report(new SampleCropProgress { Completed = tile.OrderIndex + 1, Total = total, Message = tile.FileName });
                    }
                    File.WriteAllText(Path.Combine(output, "sample_config.json"), ConfigJson(config), Encoding.UTF8);
                    var manifest = ManifestJson(config, original, processed, tiles);
                    var manifestPath = Path.Combine(output, "sample_manifest.json");
                    File.WriteAllText(manifestPath, manifest, Encoding.UTF8);
                    return new SampleCropResult { OutputDirectory = output, ManifestPath = manifestPath, Completed = true };
                }
            }
            catch
            {
                try { if (Directory.Exists(output)) { File.WriteAllText(incomplete, "Sample generation did not complete."); } } catch { }
                throw;
            }
        }

        private static Bitmap Preprocess(Bitmap source, GerberSampleConfig config)
        {
            var w = config.ProcessedWidth > 0 ? config.ProcessedWidth : source.Width;
            var h = config.ProcessedHeight > 0 ? config.ProcessedHeight : source.Height;
            if (config.PreprocessMode == SamplePreprocessMode.None) return ImageRead.Ensure24bppBgr(source);
            var dest = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(dest))
            {
                g.Clear(config.PadColor);
                Rectangle dst;
                if (config.PreprocessMode == SamplePreprocessMode.Resize || !config.KeepAspectRatio) dst = new Rectangle(0, 0, w, h);
                else
                {
                    var scale = config.PreprocessMode == SamplePreprocessMode.CenterCrop ? Math.Max((double)w / source.Width, (double)h / source.Height) : Math.Min((double)w / source.Width, (double)h / source.Height);
                    var dw = (int)Math.Round(source.Width * scale); var dh = (int)Math.Round(source.Height * scale);
                    dst = new Rectangle((w - dw) / 2, (h - dh) / 2, dw, dh);
                }
                g.DrawImage(source, dst);
            }
            return dest;
        }

        private static List<CropRectInfo> BuildTiles(GerberSampleConfig config, int width, int height)
        {
            double ox = config.OverlapUnit == OverlapUnit.Percent ? width / (double)config.Columns * config.OverlapValue / 100.0 : config.OverlapValue;
            double oy = config.OverlapUnit == OverlapUnit.Percent ? height / (double)config.Rows * config.OverlapValue / 100.0 : config.OverlapValue;
            var x = Boundaries(width, config.Columns, ox); var y = Boundaries(height, config.Rows, oy);
            var physical = new ImageInfo[config.Rows, config.Columns];
            for (int r = 0, id = 0; r < config.Rows; r++) for (int c = 0; c < config.Columns; c++, id++) physical[r, c] = new ImageInfo("", 0, id, null, c, r, r, c);
            var options = OrderOptions.FromStartOrder(config.StartOrder, config.CropOrder);
            var arranged = RobotArrange.FromPhysicalMatrix(0, physical, options);
            var graph = TraversalGraph.Build(arranged.Components[0].Matrix, options);
            var ordered = OrderedItems(graph, options);
            var list = new List<CropRectInfo>();
            for (int i = 0; i < ordered.Count; i++)
            {
                var item = ordered[i]; var rect = Rectangle.FromLTRB(x[item.Column], y[item.Row], x[item.Column + 1], y[item.Row + 1]);
                list.Add(new CropRectInfo { Row = item.Row, Column = item.Column, OrderIndex = i, Rect = rect, FileName = FormatName(config.TileNamePattern, item.Row, item.Column, i) + ExtensionFor(config.OutputFormat) });
            }
            return list;
        }

        private static List<ImageInfo> OrderedItems(TraversalGraph graph, OrderOptions options)
        {
            var ordered = new List<ImageInfo>();
            var matrix = graph.Matrix;
            if (options.RobotMovement == RobotMovement.Down || options.RobotMovement == RobotMovement.Up)
            {
                var maxCols = 0; foreach (var row in matrix) if (row.Count > maxCols) maxCols = row.Count;
                for (int c = 0; c < maxCols; c++)
                {
                    var rows = new List<int>(); for (int r = 0; r < matrix.Count; r++) rows.Add(r);
                    if (options.Mode == OrderMode.Zigzag && c % 2 == 1) rows.Reverse();
                    foreach (var r in rows) if (c < matrix[r].Count && matrix[r][c] != null) ordered.Add(matrix[r][c]);
                }
            }
            else
            {
                for (int r = 0; r < matrix.Count; r++)
                {
                    var row = new List<ImageInfo>(matrix[r]);
                    if (options.Mode == OrderMode.Zigzag && r % 2 == 1) row.Reverse();
                    ordered.AddRange(row);
                }
            }
            return ordered;
        }

        private static string ExtensionFor(SampleOutputFormat format)
        {
            switch (format)
            {
                case SampleOutputFormat.Bmp: return ".bmp";
                case SampleOutputFormat.Jpeg: return ".jpg";
                default: return ".png";
            }
        }

        private static ImageFormat ImageFormatFor(SampleOutputFormat format)
        {
            switch (format)
            {
                case SampleOutputFormat.Bmp: return ImageFormat.Bmp;
                case SampleOutputFormat.Jpeg: return ImageFormat.Jpeg;
                default: return ImageFormat.Png;
            }
        }

        private static int[] Boundaries(int size, int count, double overlap)
        {
            var b = new int[count + 1]; b[0] = 0; b[count] = size;
            var tile = (size + (count - 1) * overlap) / count;
            for (int i = 1; i < count; i++) b[i] = Math.Max(0, Math.Min(size, (int)Math.Round(i * (tile - overlap))));
            return b;
        }
        private static string FormatName(string p, int r, int c, int o) => (p ?? "Sample_R{row:00}_C{col:00}_O{order:000}").Replace("{row:00}", r.ToString("00")).Replace("{col:00}", c.ToString("00")).Replace("{order:000}", o.ToString("000"));
        private static void Invert(Bitmap bmp) { for (int y = 0; y < bmp.Height; y++) for (int x = 0; x < bmp.Width; x++) { var c = bmp.GetPixel(x, y); bmp.SetPixel(x, y, Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B)); } }
        private static void SaveOverlay(Size size, List<CropRectInfo> tiles, string path) { using (var bmp = new Bitmap(size.Width, size.Height)) using (var g = Graphics.FromImage(bmp)) { g.Clear(Color.Transparent); using (var pen = new Pen(Color.Red, 2)) foreach (var t in tiles) g.DrawRectangle(pen, t.Rect); bmp.Save(path, ImageFormat.Png); } }
        private static string Hash(string path) { using (var sha = SHA256.Create()) using (var fs = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "").ToLowerInvariant(); }
        private static string Q(string s) => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        private static string ConfigJson(GerberSampleConfig c) => "{\n  \"sourceRasterPath\": " + Q(c.SourceRasterPath) + ",\n  \"rows\": " + c.Rows + ", \"columns\": " + c.Columns + ",\n  \"cropOrder\": " + Q(c.CropOrder.ToString()) + ", \"startOrder\": " + Q(c.StartOrder.ToString()) + "\n}";
        private static string ManifestJson(GerberSampleConfig c, Bitmap original, Bitmap processed, List<CropRectInfo> tiles)
        {
            var res = StartOrderResolver.Resolve(c.StartOrder); var sb = new StringBuilder();
            sb.AppendLine("{"); sb.AppendLine("  \"appVersion\": \"GerberViewer\", \"specVersion\": \"sample-v1\",");
            sb.AppendLine("  \"timestampUtc\": " + Q(DateTime.UtcNow.ToString("o")) + ",");
            sb.AppendLine("  \"source\": { \"path\": " + Q(c.SourceRasterPath) + ", \"sha256\": " + Q(Hash(c.SourceRasterPath)) + ", \"dpiX\": " + original.HorizontalResolution + ", \"dpiY\": " + original.VerticalResolution + " },");
            sb.AppendLine("  \"dimensions\": { \"originalWidth\": " + original.Width + ", \"originalHeight\": " + original.Height + ", \"processedWidth\": " + processed.Width + ", \"processedHeight\": " + processed.Height + " },");
            sb.AppendLine("  \"sourceToProcessedTransform\": { \"mode\": " + Q(c.PreprocessMode.ToString()) + ", \"keepAspectRatio\": " + c.KeepAspectRatio.ToString().ToLowerInvariant() + " },");
            sb.AppendLine("  \"rows\": " + c.Rows + ", \"columns\": " + c.Columns + ", \"overlap\": { \"value\": " + c.OverlapValue + ", \"unit\": " + Q(c.OverlapUnit.ToString()) + " },");
            sb.AppendLine("  \"cropOrder\": " + Q(c.CropOrder.ToString()) + ", \"startOrder\": " + Q(c.StartOrder.ToString()) + ", \"resolvedStartCorner\": " + Q(res.StartCorner.ToString()) + ", \"resolvedMovement\": " + Q(res.RobotMovement.ToString()) + ", \"inversion\": " + c.InvertImage.ToString().ToLowerInvariant() + ",");
            sb.AppendLine("  \"tiles\": [");
            for (int i = 0; i < tiles.Count; i++) { var t = tiles[i]; sb.Append("    { \"row\": "+t.Row+", \"column\": "+t.Column+", \"orderIndex\": "+t.OrderIndex+", \"file\": "+Q("tiles/"+t.FileName)+", \"x\": "+t.Rect.X+", \"y\": "+t.Rect.Y+", \"width\": "+t.Rect.Width+", \"height\": "+t.Rect.Height+" }"); sb.AppendLine(i + 1 == tiles.Count ? "" : ","); }
            sb.AppendLine("  ]\n}"); return sb.ToString();
        }
    }
}
