using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GerberViewer.Stitching.Comparison;
using GerberViewer.Views;

namespace GerberStitching.Tests.UI
{
    public static class SampleComparisonControlTests
    {
        public static void RunAll()
        {
            CreateBindReplaceClearDispose();
            DisposeWhileBlinking();
            AllModesRender();
            MetricsDisplay();
            NonAuthoritativeWarning();
            ComparisonImageViewZoomPanFitReset();
            AccuracySanityIdenticalAndShifted();
        }

        private static void CreateBindReplaceClearDispose()
        {
            using (var control = new SampleComparisonControl())
            using (var first = ViewData(false))
            using (var second = ViewData(true))
            {
                control.SetComparisonResult(first, true);
                AssertTrue(control.MetricsDisplayText.Contains("Valid Overlap Ratio"), "Metrics must render after bind.");
                Render(control);
                control.SetComparisonResult(second, true);
                AssertTrue(control.CoordinateStatusText.Contains("Authoritative"), "Replacement result must update coordinate status.");
                control.ClearComparisonResult();
                AssertTrue(control.CoordinateStatusText.Contains("No comparison"), "Clear must remove stale result.");
            }
        }

        private static void DisposeWhileBlinking()
        {
            var control = new SampleComparisonControl();
            using (var data = ViewData(true)) control.SetComparisonResult(data, true);
            control.StartBlinkForTest();
            AssertTrue(control.IsBlinkRunning, "Blink timer must start.");
            control.Dispose();
        }

        private static void AllModesRender()
        {
            using (var control = new SampleComparisonControl())
            using (var data = ViewData(true))
            {
                control.SetComparisonResult(data, true);
                foreach (ComparisonMode mode in Enum.GetValues(typeof(ComparisonMode)))
                {
                    control.SelectComparisonMode(mode);
                    Render(control);
                    if (mode == ComparisonMode.Blink) control.StopBlinkForTest();
                }
            }
        }

        private static void MetricsDisplay()
        {
            using (var control = new SampleComparisonControl())
            using (var data = ViewData(true))
            {
                control.SetComparisonResult(data, true);
                var text = control.MetricsDisplayText;
                AssertTrue(text.Contains("Normalized Cross-Correlation"), "NCC metric must be displayed.");
                AssertTrue(text.Contains("Binary Mask IoU"), "IoU metric must be displayed.");
                AssertTrue(text.Contains("Edge Precision"), "Edge precision metric must be displayed.");
                AssertTrue(text.Contains("Edge Recall"), "Edge recall metric must be displayed.");
                AssertTrue(text.Contains("Edge F1"), "Edge F1 metric must be displayed.");
                AssertTrue(text.Contains("P95 Edge Distance"), "P95 edge distance metric must be displayed.");
                AssertTrue(text.Contains("Absolute Difference Mean"), "Absolute difference mean must be displayed.");
            }
        }

        private static void NonAuthoritativeWarning()
        {
            using (var control = new SampleComparisonControl())
            using (var data = ViewData(false))
            {
                control.SetComparisonResult(data, true);
                AssertTrue(control.CoordinateStatusText.Contains("Visual comparison only"), "Non-authoritative result must be explicit.");
                AssertTrue(control.MetricsDisplayText.Contains("PREVIEW ONLY"), "Metrics must be marked preview-only when not authoritative.");
            }
        }

        private static void ComparisonImageViewZoomPanFitReset()
        {
            using (var view = new ComparisonImageView())
            using (var image = Synthetic(64, 64, 0))
            {
                view.Size = new Size(200, 120);
                view.SetImage(image, true, false);
                view.SetView(2.0f, new PointF(10, 12), true);
                AssertTrue(Math.Abs(view.Zoom - 2.0f) < 0.001, "Zoom must be settable for synchronized view tests.");
                view.FitToWindow();
                view.ResetView();
                using (var bitmap = new Bitmap(200, 120)) view.DrawToBitmap(bitmap, new Rectangle(0, 0, 200, 120));
            }
        }

        private static void AccuracySanityIdenticalAndShifted()
        {
            using (var control = new SampleComparisonControl())
            using (var identical = ViewData(true))
            using (var shifted = ViewData(true, 5))
            {
                control.SetComparisonResult(identical, true);
                AssertTrue(identical.Metrics.BinaryMaskIoU > 0.99, "Identical synthetic IoU should be near 1.");
                AssertTrue(identical.Metrics.EdgeF1Score > 0.99, "Identical synthetic Edge F1 should be near 1.");
                control.SetComparisonResult(shifted, true);
                AssertTrue(shifted.Metrics.EdgeF1Score < identical.Metrics.EdgeF1Score, "Shifted synthetic Edge F1 should decrease.");
                AssertTrue(shifted.Metrics.MeanEdgeDistancePixels > identical.Metrics.MeanEdgeDistancePixels, "Shifted synthetic edge distance should increase.");
            }
        }

        private static SampleComparisonViewData ViewData(bool authoritative, int realityOffset = 0)
        {
            var sample = Synthetic(80, 64, 0);
            var reality = Synthetic(80, 64, realityOffset);
            return new SampleComparisonViewData
            {
                SamplePreview = sample,
                RealityPreview = reality,
                OverlayPreview = Synthetic(80, 64, 1),
                AbsoluteDifferencePreview = SyntheticDifference(sample, reality),
                EdgeOverlayPreview = Synthetic(80, 64, 2),
                Metrics = Metrics(realityOffset),
                IsAuthoritative = authoritative,
                CoordinateSpace = "ProcessedSampleGlobalPixels",
                WarningMessage = authoritative ? string.Empty : "Visual comparison only. Accuracy metrics are not authoritative because coordinate mapping is incomplete.",
                SamplePath = "sample_preview.png",
                RealityPath = "reality_preview.png"
            };
        }

        private static ComparisonMetrics Metrics(int offset)
        {
            if (offset == 0) return new ComparisonMetrics { ValidOverlapRatio = 1, NormalizedCrossCorrelation = 1, BinaryMaskIoU = 1, EdgeOverlap = 1, EdgePrecision = 1, EdgeRecall = 1, EdgeF1Score = 1, MeanEdgeDistancePixels = 0, P95EdgeDistancePixels = 0, AbsoluteDifferenceMean = 0, AbsoluteDifferenceP95 = 0, AbsoluteDifferenceMax = 0 };
            return new ComparisonMetrics { ValidOverlapRatio = .9, NormalizedCrossCorrelation = .7, BinaryMaskIoU = .72, EdgeOverlap = .6, EdgePrecision = .65, EdgeRecall = .62, EdgeF1Score = .63, MeanEdgeDistancePixels = offset, P95EdgeDistancePixels = offset + 2, AbsoluteDifferenceMean = 12, AbsoluteDifferenceP95 = 64, AbsoluteDifferenceMax = 255 };
        }

        private static Bitmap Synthetic(int width, int height, int offset)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Black);
                using (var brush = new SolidBrush(Color.White)) g.FillRectangle(brush, 8 + offset, 8, 28, 18);
                using (var brush = new SolidBrush(Color.Gray)) g.FillEllipse(brush, 42 + offset, 28, 20, 14);
            }
            return bitmap;
        }

        private static Bitmap SyntheticDifference(Bitmap sample, Bitmap reality)
        {
            var width = Math.Min(sample.Width, reality.Width);
            var height = Math.Min(sample.Height, reality.Height);
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                var s = sample.GetPixel(x, y);
                var r = reality.GetPixel(x, y);
                var d = Math.Abs(s.R - r.R);
                bitmap.SetPixel(x, y, Color.FromArgb(d, d, d));
            }
            return bitmap;
        }

        private static void Render(Control control)
        {
            control.Size = new Size(800, 500);
            using (var bitmap = new Bitmap(800, 500)) control.DrawToBitmap(bitmap, new Rectangle(0, 0, 800, 500));
        }

        private static void AssertTrue(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
