using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using GerberViewer.Stitching.Alignment;
using GerberViewer.Stitching.Models;
using GerberViewer.Stitching.Stitching;

namespace GerberStitching.Tests.Stitching
{
    public static class GlobalTransformStitcherTests
    {
        public static void RunAll()
        {
            TranslationOnly();
            RotationBoundsUseCorners();
            ScaleBoundsUseCorners();
            NegativeBounds();
            OverlapBlendingModes();
            ExcludedTileIsNotStitched();
            FallbackNotStitchable();
            OutputReopen();
            CancellationCleanup();
            HalconProjectiveConversionUsesRowColumnOrder();
        }

        private static void TranslationOnly()
        {
            using (var fixture = new Fixture())
            {
                fixture.AddImage(0, Color.Red);
                fixture.AddImage(1, Color.Green);
                var output = fixture.Stitch(new[] { State(0, 0, 0, 0, PoseSource.SampleAlignment), State(1, 20, 0, 1, PoseSource.SampleAlignment) }, StitchBlendMode.NoBlend, CancellationToken.None);
                AssertTrue(File.Exists(output), "Translation-only stitch output must exist.");
            }
        }

        private static void RotationBoundsUseCorners()
        {
            var image = new CapturedImageInfo { OrderIndex = 0, Width = 20, Height = 10, FilePath = "missing" };
            var bounds = GlobalTransformStitcher.CalculateBounds(new List<Tuple<CapturedImageInfo, double[,]>> { Tuple.Create(image, Rotate(45, 0, 0)) });
            AssertTrue(bounds.Width > 20, "Rotated bounds must use all transformed corners, not X/Y + Width/Height only.");
        }

        private static void ScaleBoundsUseCorners()
        {
            var image = new CapturedImageInfo { OrderIndex = 0, Width = 20, Height = 10, FilePath = "missing" };
            var bounds = GlobalTransformStitcher.CalculateBounds(new List<Tuple<CapturedImageInfo, double[,]>> { Tuple.Create(image, new[,] { { 2d, 0d, 0d }, { 0d, 2d, 0d }, { 0d, 0d, 1d } }) });
            AssertTrue(bounds.Width >= 40 && bounds.Height >= 20, "Scaled bounds must use full transform corners.");
        }

        private static void NegativeBounds()
        {
            var image = new CapturedImageInfo { OrderIndex = 0, Width = 20, Height = 10, FilePath = "missing" };
            var bounds = GlobalTransformStitcher.CalculateBounds(new List<Tuple<CapturedImageInfo, double[,]>> { Tuple.Create(image, Homography.FromPose(-30, -40, 0, 1)) });
            AssertTrue(bounds.Left <= -30 && bounds.Top <= -40, "Negative transformed bounds must be preserved before canvas offset.");
        }

        private static void OverlapBlendingModes()
        {
            using (var fixture = new Fixture())
            {
                fixture.AddImage(0, Color.Red);
                fixture.AddImage(1, Color.Blue);
                fixture.Stitch(new[] { State(0, 0, 0, 0, PoseSource.SampleAlignment), State(1, 8, 0, 1, PoseSource.SampleAlignment) }, StitchBlendMode.NoBlend, CancellationToken.None);
                fixture.Stitch(new[] { State(0, 0, 0, 0, PoseSource.SampleAlignment), State(1, 8, 0, 1, PoseSource.SampleAlignment) }, StitchBlendMode.WeightedAverage, CancellationToken.None);
                fixture.Stitch(new[] { State(0, 0, 0, 0, PoseSource.SampleAlignment), State(1, 8, 0, 1, PoseSource.SampleAlignment) }, StitchBlendMode.Feather, CancellationToken.None);
            }
        }

        private static void ExcludedTileIsNotStitched()
        {
            using (var fixture = new Fixture())
            {
                fixture.AddImage(0, Color.Red);
                fixture.AddImage(1, Color.Blue);
                AssertThrows(delegate { fixture.Stitch(new[] { State(0, 0, 0, 0, PoseSource.Excluded), State(1, 20, 0, 1, PoseSource.Failed) }, StitchBlendMode.NoBlend, CancellationToken.None); }, "Excluded/failed tiles must not be stitched.");
            }
        }

        private static void FallbackNotStitchable()
        {
            var cap = new CapturedImageInfo { OrderIndex = 0, Row = 0, Column = 0 };
            var fallback = TileWorkflowState.From(cap, Homography.Identity(), PoseSource.ExpectedGridOffset, null, "fallback");
            AssertFalse(fallback.IsStitchable, "Expected-grid fallback must not be stitchable.");
        }

        private static void OutputReopen()
        {
            using (var fixture = new Fixture())
            {
                fixture.AddImage(0, Color.Red);
                var output = fixture.Stitch(new[] { State(0, 0, 0, 0, PoseSource.SampleAlignment) }, StitchBlendMode.NoBlend, CancellationToken.None);
                using (var reopened = new Bitmap(output)) AssertTrue(reopened.Width > 0 && reopened.Height > 0, "Output must reopen after validation.");
            }
        }

        private static void CancellationCleanup()
        {
            using (var fixture = new Fixture())
            using (var cts = new CancellationTokenSource())
            {
                fixture.AddImage(0, Color.Red);
                cts.Cancel();
                AssertThrows(delegate { fixture.Stitch(new[] { State(0, 0, 0, 0, PoseSource.SampleAlignment) }, StitchBlendMode.NoBlend, cts.Token); }, "Cancellation must throw.");
                AssertFalse(File.Exists(Path.Combine(fixture.Root, ".creating", "out.tif")), "Cancellation must not leave a publishable .creating output.");
            }
        }

        private static void HalconProjectiveConversionUsesRowColumnOrder()
        {
            var canonical = new[,]
            {
                { 1d, 0d, 13d },
                { 0d, 1d, 29d },
                { 0d, 0d, 1d }
            };
            var halcon = GlobalTransformStitcher.ToHalconProjective(canonical);
            AssertNear(29d, halcon[2], 1e-12, "HALCON row translation must come from canonical Y/row translation.");
            AssertNear(13d, halcon[5], 1e-12, "HALCON column translation must come from canonical X/column translation.");
        }

        private static TileWorkflowState State(int order, double x, double y, int column, PoseSource source)
        {
            return TileWorkflowState.From(new CapturedImageInfo { OrderIndex = order, Row = 0, Column = column }, Homography.FromPose(x, y, 0, 1), source, null, null);
        }

        private static double[,] Rotate(double angleDeg, double tx, double ty)
        {
            var a = angleDeg * Math.PI / 180.0;
            var c = Math.Cos(a);
            var s = Math.Sin(a);
            return new[,] { { c, -s, tx }, { s, c, ty }, { 0d, 0d, 1d } };
        }

        private sealed class Fixture : IDisposable
        {
            private readonly List<CapturedImageInfo> _images = new List<CapturedImageInfo>();
            public Fixture() { Root = Path.Combine(Path.GetTempPath(), "gv_stitch_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(Root); }
            public string Root { get; private set; }
            public void AddImage(int order, Color color)
            {
                var path = Path.Combine(Root, "img" + order + ".png");
                using (var bitmap = new Bitmap(20, 20, PixelFormat.Format24bppRgb))
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(color);
                    g.FillRectangle(Brushes.White, 2, 2, 6, 8);
                    bitmap.Save(path, ImageFormat.Png);
                }
                _images.Add(new CapturedImageInfo { OrderIndex = order, Row = 0, Column = order, Width = 20, Height = 20, FilePath = path });
            }
            public string Stitch(IEnumerable<TileWorkflowState> states, StitchBlendMode blendMode, CancellationToken token)
            {
                return new GlobalTransformStitcher().StitchFromGlobalTransforms(_images, new List<TileWorkflowState>(states), new StitchFromGlobalTransformsOptions { OutputPath = Path.Combine(Root, "out.tif"), EnableBlending = blendMode != StitchBlendMode.NoBlend, BlendMode = blendMode, PreviewUpdateInterval = 1, MaxPreviewMegapixels = 1 }, null, token);
            }
            public void Dispose() { if (Directory.Exists(Root)) Directory.Delete(Root, true); }
        }

        private static void AssertTrue(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static void AssertFalse(bool value, string message) { if (value) throw new InvalidOperationException(message); }
        private static void AssertNear(double expected, double actual, double tolerance, string message) { if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(message + " Expected " + expected + ", actual " + actual + "."); }
        private static void AssertThrows(Action action, string message) { try { action(); } catch { return; } throw new InvalidOperationException(message); }
    }
}
