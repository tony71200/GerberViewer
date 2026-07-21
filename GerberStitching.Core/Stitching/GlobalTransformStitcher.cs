using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using GerberViewer.Stitching.Models;

namespace GerberViewer.Stitching.Stitching
{
    public sealed class StitchFromGlobalTransformsOptions { public int PreviewUpdateInterval { get; set; } = 4; public double MaxPreviewMegapixels { get; set; } = 32; public TiffMode TiffMode { get; set; } = TiffMode.Auto; public bool EnableBlending { get; set; } public string OutputPath { get; set; } }
    public sealed class StitchPreview { public Bitmap Preview { get; set; } public int PlacedCount { get; set; } public int TotalCount { get; set; } }

    public sealed class GlobalTransformStitcher
    {
        public string StitchFromGlobalTransforms(IList<CapturedImageInfo> images, IList<TileWorkflowState> poses, StitchFromGlobalTransformsOptions options, IProgress<StitchPreview> preview, CancellationToken cancellationToken)
        {
            if (images == null) throw new ArgumentNullException("images"); if (poses == null) throw new ArgumentNullException("poses"); options = options ?? new StitchFromGlobalTransformsOptions();
            var output = NormalizeTiffPath(options.OutputPath); var poseByKey = poses.Where(p => p.HasValidPose).ToDictionary(p => p.Row + ":" + p.Column);
            var items = images.Where(i => poseByKey.ContainsKey(i.Row + ":" + i.Column)).Select(i => new { Image = i, Pose = poseByKey[i.Row + ":" + i.Column] }).ToList();
            if (items.Count == 0) throw new InvalidOperationException("No valid global transforms to stitch.");
            RectangleF bounds = CalculateBounds(items.Select(x => Tuple.Create(x.Image, x.Pose.GlobalPose)).ToList());
            double scale = Math.Min(1.0, Math.Sqrt((options.MaxPreviewMegapixels * 1000000.0) / Math.Max(1.0, bounds.Width * bounds.Height)));
            int width = Math.Max(1, (int)Math.Ceiling(bounds.Width)); int height = Math.Max(1, (int)Math.Ceiling(bounds.Height));
            bool big = SelectBigTiff(options.TiffMode, width, height, 4);
            if (big && options.TiffMode == TiffMode.StandardTiff) throw new InvalidOperationException("Standard TIFF selected for an output estimated beyond the standard TIFF limit.");
            using (var canvas = new Bitmap(width, height, PixelFormat.Format32bppArgb)) using (var g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.Transparent);
                for (int idx = 0; idx < items.Count; idx++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using (var bmp = new Bitmap(items[idx].Image.FilePath))
                    {
                        var h = items[idx].Pose.GlobalPose; var state = g.Save(); g.TranslateTransform((float)(h[0,2] - bounds.Left), (float)(h[1,2] - bounds.Top)); g.RotateTransform((float)(Math.Atan2(h[1,0], h[0,0]) * 180 / Math.PI)); g.ScaleTransform((float)Math.Sqrt(h[0,0]*h[0,0]+h[1,0]*h[1,0]), (float)Math.Sqrt(h[0,0]*h[0,0]+h[1,0]*h[1,0])); g.DrawImageUnscaled(bmp, 0, 0); g.Restore(state);
                    }
                    if (preview != null && options.PreviewUpdateInterval > 0 && ((idx + 1) % options.PreviewUpdateInterval == 0 || idx == items.Count - 1)) preview.Report(new StitchPreview { Preview = MakePreview(canvas, scale), PlacedCount = idx + 1, TotalCount = items.Count });
                }
                canvas.Save(output, ImageFormat.Tiff);
            }
            return output;
        }
        public static RectangleF CalculateBounds(IList<Tuple<CapturedImageInfo,double[,]>> items) { float l=float.MaxValue,t=float.MaxValue,r=float.MinValue,b=float.MinValue; foreach(var it in items){ float x=(float)it.Item2[0,2], y=(float)it.Item2[1,2], w=it.Item1.Width, h=it.Item1.Height; l=Math.Min(l,x); t=Math.Min(t,y); r=Math.Max(r,x+w); b=Math.Max(b,y+h);} return RectangleF.FromLTRB(l,t,r,b); }
        public static bool SelectBigTiff(TiffMode mode, int width, int height, int bytesPerPixel) { if (mode == TiffMode.BigTiff) return true; if (mode == TiffMode.StandardTiff) return false; return (long)width * height * bytesPerPixel > 0xF0000000L; }
        public static string NormalizeTiffPath(string path) { if (string.IsNullOrWhiteSpace(path)) path = Path.Combine(Environment.CurrentDirectory, "stitched.tif"); var ext = Path.GetExtension(path).ToLowerInvariant(); if (ext != ".tif" && ext != ".tiff") path = Path.ChangeExtension(path, ".tif"); return path; }
        private static Bitmap MakePreview(Bitmap src, double scale) { if (scale >= .999) return (Bitmap)src.Clone(); return new Bitmap(src, Math.Max(1,(int)(src.Width*scale)), Math.Max(1,(int)(src.Height*scale))); }
    }
}
