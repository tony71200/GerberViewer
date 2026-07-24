using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using GerberViewer.Stitching.Imaging.ImageInterop;
using GerberViewer.Stitching.Models;
using HalconDotNet;
using OpenCvSharp;

namespace GerberViewer.Stitching.Stitching
{
    public enum StitchBlendMode 
    { 
        NoBlend, 
        WeightedAverage, 
        Feather 
    }
    public sealed class StitchFromGlobalTransformsOptions 
    { 
        public StitchingEngine StitchingEngine { get; set; } = StitchingEngine.HalconThenOpenCvFallback; 
        public int PreviewUpdateInterval { get; set; } = 4; 
        public double MaxPreviewMegapixels { get; set; } = 32; 
        public TiffMode TiffMode { get; set; } = TiffMode.BigTiff; 
        public StitchBlendMode BlendMode { get; set; } = StitchBlendMode.Feather; 
        public bool EnableBlending { get; set; } 
        public bool ForceGray8Output { get; set; } 
        public string OutputPath { get; set; } 
    }
    public sealed class StitchPreview 
    { 
        public Bitmap Preview { get; set; } 
        public int PlacedCount { get; set; } 
        public int TotalCount { get; set; } 
    }

    public sealed class GlobalTransformStitcher
    {
        private readonly IImageInteropService _imageInterop = new ImageInteropService();

        public string StitchFromGlobalTransforms(
            IList<CapturedImageInfo> images, 
            IList<TileWorkflowState> poses, 
            StitchFromGlobalTransformsOptions options, 
            IProgress<StitchPreview> preview, 
            CancellationToken cancellationToken)
        {
            if (images == null) 
                throw new ArgumentNullException("images");
            if (poses == null) 
                throw new ArgumentNullException("poses");
            options = options ?? new StitchFromGlobalTransformsOptions();
            var output = NormalizeTiffPath(options.OutputPath);
            var creatingPath = ToCreatingPath(output);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var items = BuildItems(images, poses);
                if (items.Count == 0) 
                    throw new InvalidOperationException("No stitchable global transforms to stitch.");
                var bounds = CalculateBounds(items.Select(x => Tuple.Create(x.Image, x.Pose.GlobalPose)).ToList());
                var width = Math.Max(1, (int)Math.Ceiling(bounds.Width));
                var height = Math.Max(1, (int)Math.Ceiling(bounds.Height));
                var bytesPerPixel = options.ForceGray8Output ? 1 : 3;
                var selection = SelectTiffOutput(options.TiffMode, width, height, bytesPerPixel);
                if (options.TiffMode == TiffMode.StandardTiff && 
                    selection.EstimatedBytes > 0xF0000000L) 
                    throw new InvalidOperationException("Standard TIFF selected for an output estimated beyond the standard TIFF limit.");
                if (selection.RequiresBigTiff) throw new NotSupportedException("BigTIFF output is required by size/configuration, but this writer does not claim BigTIFF support because it uses System.Drawing TIFF save.");
                EnsureDirectory(creatingPath);
                if (options.StitchingEngine == StitchingEngine.HalconProjectiveMosaic)
                {
                    StitchToHalconMosaic(items, creatingPath, cancellationToken);
                    ReopenAndValidate(creatingPath, width, height);
                }
                else if (options.StitchingEngine == StitchingEngine.HalconThenOpenCvFallback)
                {
                    try
                    {
                        StitchToHalconMosaic(items, creatingPath, cancellationToken);
                        ReopenAndValidate(creatingPath, width, height);
                    }
                    catch
                    {
                        CleanupCreating(creatingPath);
                        using (var canvas = StitchToMat(items, bounds, width, height, options, preview, cancellationToken))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            SaveStandardTiff(canvas, creatingPath);
                            ReopenAndValidate(creatingPath, width, height);
                        }
                    }
                }
                else
                {
                    using (var canvas = StitchToMat(items, bounds, width, height, options, preview, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        SaveStandardTiff(canvas, creatingPath);
                        ReopenAndValidate(creatingPath, width, height);
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
                Publish(creatingPath, output);
                return output;
            }
            catch (OperationCanceledException)
            {
                CleanupCreating(creatingPath);
                throw;
            }
            catch
            {
                CleanupCreating(creatingPath);
                throw;
            }
            finally
            {
                Directory.Delete(creatingPath, true);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }


        private static void StitchToHalconMosaic(IList<StitchItem> items, string path, CancellationToken cancellationToken)
        {
            if (items == null || items.Count == 0) 
                throw new ArgumentException("At least one stitch item is required.", "items");
            HObject images = null;
            HObject mosaic = null;
            try
            {
                HOperatorSet.GenEmptyObj(out images);
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    HObject image = null;
                    HObject tmp = null;
                    try
                    {
                        HOperatorSet.ReadImage(out image, item.Image.FilePath);
                        HOperatorSet.ConcatObj(images, image, out tmp);
                        images.Dispose();
                        images = tmp;
                        tmp = null;
                    }
                    finally
                    {
                        if (image != null && image.IsInitialized()) image.Dispose();
                        if (tmp != null && tmp.IsInitialized()) tmp.Dispose();
                    }
                }

                var rootInverse = InvertAffine(items[0].Pose.GlobalPose);
                var mappingSource = new HTuple();
                var mappingDest = new HTuple();
                var homMatrices2D = new HTuple();
                for (int i = 1; i < items.Count; i++)
                {
                    var sourceToRoot = MultiplyAffine(rootInverse, items[i].Pose.GlobalPose);
                    mappingSource = mappingSource.TupleConcat(i + 1);
                    mappingDest = mappingDest.TupleConcat(1);
                    homMatrices2D = homMatrices2D.TupleConcat(new HTuple(ToHalconProjective(sourceToRoot)));
                }

                if (items.Count == 1) HOperatorSet.SelectObj(images, out mosaic, 1);
                else
                {
                    HTuple transforms;
                    HOperatorSet.GenProjectiveMosaic(images, out mosaic, new HTuple(1), mappingSource, mappingDest, homMatrices2D, new HTuple("default"), new HTuple("false"), out transforms);
                }
                WriteHalconImage(path, mosaic);
            }
            finally
            {
                if (mosaic != null && mosaic.IsInitialized()) 
                    mosaic.Dispose();
                if (images != null && images.IsInitialized()) 
                    images.Dispose();
            }
        }

        private static void WriteHalconImage(string path, HObject image)
        {
            EnsureDirectory(path);
            // TODO Create a static function to check size of image to choose extension output is BigTiff/Tiff/PNG
            var ext = Path.GetExtension(path).ToLowerInvariant();
            //var format = ext == ".tif" || ext == ".tiff" ? "tiff" : ext.TrimStart('.');
            var format = "bigtiff none";
            HOperatorSet.WriteImage(image, new HTuple(format), new HTuple(0), new HTuple(path));
        }

        public static double[] ToHalconProjective(double[,] h)
        {
            if (h == null) throw new ArgumentNullException("h");
            if (h.GetLength(0) != 3 || h.GetLength(1) != 3) throw new ArgumentException("Projective transform must be a 3x3 matrix.", "h");

            // The canonical stitching transform uses OpenCV/image coordinates:
            //   x/column' = h00*x + h01*y + h02
            //   y/row'    = h10*x + h11*y + h12
            // HALCON projective mosaic matrices are expressed in row/column order.
            // Convert by sandwiching the canonical matrix with the row/column swap so
            // HALCON receives row/column coefficients instead of accidentally treating
            // x as row and y as column.
            return new[]
            {
                h[1, 1], h[1, 0], h[1, 2],
                h[0, 1], h[0, 0], h[0, 2],
                h[2, 1], h[2, 0], h[2, 2]
            };
        }

        private static double[,] InvertAffine(double[,] h)
        {
            var det = h[0, 0] * h[1, 1] - h[0, 1] * h[1, 0];
            if (Math.Abs(det) < 1e-12) 
                throw new InvalidOperationException("Cannot invert singular affine transform for HALCON mosaic stitching.");
            var a = h[1, 1] / det; 
            var b = -h[0, 1] / det; 
            var d = -h[1, 0] / det; 
            var e = h[0, 0] / det;
            var c = -(a * h[0, 2] + b * h[1, 2]); 
            var f = -(d * h[0, 2] + e * h[1, 2]);
            return new[,] 
            { 
                { a, b, c }, 
                { d, e, f }, 
                { 0d, 0d, 1d } 
            };
        }

        private static double[,] MultiplyAffine(double[,] a, double[,] b)
        {
            return new[,]
            {
                { a[0,0] * b[0,0] + a[0,1] * b[1,0], a[0,0] * b[0,1] + a[0,1] * b[1,1], a[0,0] * b[0,2] + a[0,1] * b[1,2] + a[0,2] },
                { a[1,0] * b[0,0] + a[1,1] * b[1,0], a[1,0] * b[0,1] + a[1,1] * b[1,1], a[1,0] * b[0,2] + a[1,1] * b[1,2] + a[1,2] },
                { 0d, 0d, 1d }
            };
        }

        private Mat StitchToMat(IList<StitchItem> items, RectangleF bounds, int width, int height, StitchFromGlobalTransformsOptions options, IProgress<StitchPreview> preview, CancellationToken cancellationToken)
        {
            var blendMode = options.EnableBlending && !options.ForceGray8Output ? options.BlendMode : StitchBlendMode.NoBlend;
            var canvasType = options.ForceGray8Output ? MatType.CV_8UC1 : MatType.CV_8UC3;
            Mat canvas8 = blendMode == StitchBlendMode.NoBlend ? new Mat(height, width, canvasType, Scalar.All(0)) : null;
            Mat accum = blendMode == StitchBlendMode.NoBlend ? null : new Mat(height, width, MatType.CV_32FC3, Scalar.All(0));
            Mat weights = blendMode == StitchBlendMode.NoBlend ? null : new Mat(height, width, MatType.CV_32FC1, Scalar.All(0));
            try
            {
                for (int idx = 0; idx < items.Count; idx++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using (var image = LoadForStitch(items[idx].Image.FilePath, options.ForceGray8Output))
                    using (var mask = new Mat(image.Rows, image.Cols, MatType.CV_8UC1, Scalar.All(255)))
                    using (var warp = ToCanvasWarp(items[idx].Pose.GlobalPose, bounds))
                    using (var warped = new Mat())
                    using (var warpedMask = new Mat())
                    {
                        Cv2.WarpAffine(image, warped, warp, new OpenCvSharp.Size(width, height), InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));
                        Cv2.WarpAffine(mask, warpedMask, warp, new OpenCvSharp.Size(width, height), InterpolationFlags.Nearest, BorderTypes.Constant, Scalar.All(0));
                        if (blendMode == StitchBlendMode.NoBlend) warped.CopyTo(canvas8, warpedMask);
                        else AccumulateBlend(warped, warpedMask, accum, weights, blendMode);
                    }
                    if (preview != null && options.PreviewUpdateInterval > 0 && ((idx + 1) % options.PreviewUpdateInterval == 0 || idx == items.Count - 1))
                    {
                        using (var current = blendMode == StitchBlendMode.NoBlend ? canvas8.Clone() : ResolveBlend(accum, weights))
                            preview.Report(new StitchPreview { Preview = MakePreview(current, options.MaxPreviewMegapixels), PlacedCount = idx + 1, TotalCount = items.Count });
                    }
                }
                if (blendMode == StitchBlendMode.NoBlend) { var result = canvas8; canvas8 = null; return result; }
                return ResolveBlend(accum, weights);
            }
            finally
            {
                if (canvas8 != null) canvas8.Dispose();
                if (accum != null) accum.Dispose();
                if (weights != null) weights.Dispose();
            }
        }

        private static void AccumulateBlend(Mat warped, Mat warpedMask, Mat accum, Mat weights, StitchBlendMode blendMode)
        {
            using (var image32 = new Mat())
            using (var weight = BuildWeight(warpedMask, blendMode))
            using (var weight3 = To3Channels(weight))
            using (var weightedImage = new Mat())
            {
                warped.ConvertTo(image32, MatType.CV_32FC3);
                Cv2.Multiply(image32, weight3, weightedImage);
                Cv2.Add(accum, weightedImage, accum);
                Cv2.Add(weights, weight, weights);
            }
        }

        private static Mat BuildWeight(Mat mask, StitchBlendMode blendMode)
        {
            var weight = new Mat();
            if (blendMode == StitchBlendMode.Feather)
            {
                using (var binary = new Mat())
                {
                    Cv2.Threshold(mask, binary, 0, 255, ThresholdTypes.Binary);
                    Cv2.DistanceTransform(binary, weight, DistanceTypes.L2, DistanceTransformMasks.Mask3);
                    Cv2.Normalize(weight, weight, 0.0, 1.0, NormTypes.MinMax);
                    mask.ConvertTo(binary, MatType.CV_32FC1, 1.0 / 255.0);
                    Cv2.Multiply(weight, binary, weight);
                }
            }
            else mask.ConvertTo(weight, MatType.CV_32FC1, 1.0 / 255.0);
            return weight;
        }

        private static Mat To3Channels(Mat weight)
        {
            Mat[] channels = { weight, weight, weight };
            var result = new Mat();
            Cv2.Merge(channels, result);
            return result;
        }

        private static Mat ResolveBlend(Mat accum, Mat weights)
        {
            using (var safeWeights = new Mat())
            using (var weight3 = new Mat())
            using (var output32 = new Mat())
            {
                Cv2.Add(weights, Scalar.All(1e-6), safeWeights);
                Cv2.Merge(new[] { safeWeights, safeWeights, safeWeights }, weight3);
                Cv2.Divide(accum, weight3, output32);
                var output8 = new Mat();
                output32.ConvertTo(output8, MatType.CV_8UC3);
                return output8;
            }
        }

        private Mat LoadForStitch(string path, bool forceGray8)
        {
            using (var bitmap = new Bitmap(path)) 
                return _imageInterop.ToMatCopy(bitmap, forceGray8 ? InteropPixelFormat.Mono8 : InteropPixelFormat.Bgr8);
        }

        private static Mat ToCanvasWarp(double[,] transform, RectangleF bounds)
        {
            var warp = new Mat(2, 3, MatType.CV_64FC1);
            warp.Set<double>(0, 0, transform[0, 0]); warp.Set<double>(0, 1, transform[0, 1]); warp.Set<double>(0, 2, transform[0, 2] - bounds.Left);
            warp.Set<double>(1, 0, transform[1, 0]); warp.Set<double>(1, 1, transform[1, 1]); warp.Set<double>(1, 2, transform[1, 2] - bounds.Top);
            return warp;
        }

        private static IList<StitchItem> BuildItems(
            IList<CapturedImageInfo> images, 
            IList<TileWorkflowState> poses)
        {
            var imageByOrder = images.ToDictionary(i => i.OrderIndex);
            var result = new List<StitchItem>();
            foreach (var pose in poses.Where(p => p.IsStitchable).OrderBy(p => p.OrderIndex))
            {
                CapturedImageInfo image;
                if (imageByOrder.TryGetValue(pose.OrderIndex, out image)) result.Add(new StitchItem(image, pose));
            }
            return result;
        }

        public static RectangleF CalculateBounds(IList<Tuple<CapturedImageInfo, double[,]>> items)
        {
            if (items == null || items.Count == 0) throw new ArgumentException("At least one transformed image is required.", "items");
            double left = double.PositiveInfinity, top = double.PositiveInfinity, right = double.NegativeInfinity, bottom = double.NegativeInfinity;
            foreach (var it in items)
            {
                var width = it.Item1.Width > 0 ? it.Item1.Width : ImageSize(it.Item1.FilePath).Width;
                var height = it.Item1.Height > 0 ? it.Item1.Height : ImageSize(it.Item1.FilePath).Height;
                foreach (var p in TransformCorners(it.Item2, width, height))
                {
                    left = Math.Min(left, p.X); top = Math.Min(top, p.Y); right = Math.Max(right, p.X); bottom = Math.Max(bottom, p.Y);
                }
            }
            return RectangleF.FromLTRB((float)Math.Floor(left), (float)Math.Floor(top), (float)Math.Ceiling(right), (float)Math.Ceiling(bottom));
        }

        private static IEnumerable<PointF> TransformCorners(double[,] h, int width, int height)
        {
            var pts = new[] { new PointF(0, 0), new PointF(width, 0), new PointF(0, height), new PointF(width, height) };
            foreach (var p in pts) yield return new PointF((float)(h[0, 0] * p.X + h[0, 1] * p.Y + h[0, 2]), (float)(h[1, 0] * p.X + h[1, 1] * p.Y + h[1, 2]));
        }

        public static TiffOutputSelection SelectTiffOutput(TiffMode mode, int width, int height, int bytesPerPixel)
        {
            var bytes = EstimateByteCount(width, height, bytesPerPixel);
            if (mode == TiffMode.BigTiff) return new TiffOutputSelection(true, bytes, "Configured BigTIFF mode.");
            if (mode == TiffMode.StandardTiff) return new TiffOutputSelection(false, bytes, "Configured StandardTIFF mode.");
            return new TiffOutputSelection(bytes > 0xF0000000L || width > 65500 || height > 65500, bytes, "Auto selection from dimensions and byte count.");
        }

        public static bool SelectBigTiff(TiffMode mode, int width, int height, int bytesPerPixel) 
        { return SelectTiffOutput(mode, width, height, bytesPerPixel).RequiresBigTiff; 
        }
        public static long EstimateByteCount(int width, int height, int bytesPerPixel) 
        { 
            return (long)Math.Max(0, width) * Math.Max(0, height) * Math.Max(1, bytesPerPixel); 
        }
        public static string NormalizeTiffPath(string path) 
        { 
            if (string.IsNullOrWhiteSpace(path)) 
                path = Path.Combine(Environment.CurrentDirectory, "stitched.tiff"); 
            var ext = Path.GetExtension(path).ToLowerInvariant(); 
            if (ext != ".tif" && ext != ".tiff") 
                path = Path.ChangeExtension(path, ".tif"); 
            return path; 
        }

        private static void SaveStandardTiff(Mat image, string path)
        {
            if (!Cv2.ImWrite(path, image)) 
                throw new IOException("OpenCV failed to write stitched TIFF: " + path);
        }

        private static Bitmap MakePreview(Mat src, double maxPreviewMegapixels)
        {
            var scale = Math.Min(1.0, Math.Sqrt((maxPreviewMegapixels * 1000000.0) / Math.Max(1.0, src.Cols * src.Rows)));
            using (var previewMat = new Mat())
            {
                if (scale >= .999) src.CopyTo(previewMat);
                else Cv2.Resize(src, previewMat, new OpenCvSharp.Size(Math.Max(1, (int)(src.Cols * scale)), Math.Max(1, (int)(src.Rows * scale))), 0, 0, InterpolationFlags.Area);
                return new ImageInteropService().ToBitmapCopy(previewMat);
            }
        }

        private static void ReopenAndValidate(string path, int width, int height)
        {
            if (!File.Exists(path)) 
                throw new IOException("Stitched output was not written: " + path);
            var info = new FileInfo(path);
            if (info.Length <= 0) 
                throw new IOException("Stitched output was empty: " + path);
        }

        private static void Publish(string creatingPath, string output)
        {
            EnsureDirectory(output);
            if (File.Exists(output)) File.Delete(output);
            File.Move(creatingPath, output);
        }

        private static void CleanupCreating(string creatingPath) 
        { 
            if (!string.IsNullOrWhiteSpace(creatingPath) && File.Exists(creatingPath)) 
                File.Delete(creatingPath); 
        }
        private static void EnsureDirectory(string path) 
        { 
            var dir = Path.GetDirectoryName(path); 
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) 
                Directory.CreateDirectory(dir); }
        private static string ToCreatingPath(string output) 
        { 
            var dir = Path.GetDirectoryName(output); 
            var file = Path.GetFileName(output); 
            return Path.Combine(string.IsNullOrWhiteSpace(dir) ? Environment.CurrentDirectory : dir, ".creating", file); 
        }
        private static System.Drawing.Size ImageSize(string path) 
        { 
            using (var bitmap = new Bitmap(path)) 
                return bitmap.Size; 
        }

        private sealed class StitchItem
        {
            public StitchItem(CapturedImageInfo image, TileWorkflowState pose) 
            { 
                Image = image; 
                Pose = pose; 
            }
            public CapturedImageInfo Image { get; private set; }
            public TileWorkflowState Pose { get; private set; }
        }
    }

    public sealed class TiffOutputSelection
    {
        public TiffOutputSelection(bool requiresBigTiff, long estimatedBytes, string reason) { RequiresBigTiff = requiresBigTiff; EstimatedBytes = estimatedBytes; Reason = reason; }
        public bool RequiresBigTiff { get; private set; }
        public long EstimatedBytes { get; private set; }
        public string Reason { get; private set; }
    }
}
