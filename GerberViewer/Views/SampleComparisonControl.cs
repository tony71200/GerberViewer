using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;
using GerberViewer.Stitching.Comparison;

namespace GerberViewer.Views
{
    public sealed class SampleComparisonViewData : IDisposable
    {
        public Bitmap SamplePreview { get; set; }
        public Bitmap RealityPreview { get; set; }
        public Bitmap OverlayPreview { get; set; }
        public Bitmap AbsoluteDifferencePreview { get; set; }
        public Bitmap EdgeOverlayPreview { get; set; }
        public Bitmap BinaryMaskPreview { get; set; }
        public Bitmap ErrorHeatmapPreview { get; set; }
        public ComparisonMetrics Metrics { get; set; }
        public bool IsAuthoritative { get; set; }
        public string CoordinateSpace { get; set; }
        public string WarningMessage { get; set; }
        public string SamplePath { get; set; }
        public string RealityPath { get; set; }
        public string OutputDirectory { get; set; }

        public static SampleComparisonViewData FromResult(SampleComparisonResult result, string samplePath, string realityPath)
        {
            if (result == null) return null;
            return new SampleComparisonViewData
            {
                SamplePreview = LoadBitmap(result.SamplePreviewPath),
                RealityPreview = LoadBitmap(result.StitchedPreviewPath),
                OverlayPreview = LoadBitmap(result.AlphaOverlayPath),
                AbsoluteDifferencePreview = LoadBitmap(result.AbsoluteDifferencePath),
                EdgeOverlayPreview = LoadBitmap(result.EdgeOverlayPath),
                Metrics = result.Metrics,
                IsAuthoritative = result.IsAuthoritative,
                CoordinateSpace = result.CoordinateSpace,
                WarningMessage = result.Warnings == null ? null : string.Join(Environment.NewLine, result.Warnings.ToArray()),
                SamplePath = samplePath,
                RealityPath = realityPath,
                OutputDirectory = Path.GetDirectoryName(result.MetadataPath)
            };
        }

        public SampleComparisonViewData CloneForUi()
        {
            return new SampleComparisonViewData
            {
                SamplePreview = Clone(SamplePreview),
                RealityPreview = Clone(RealityPreview),
                OverlayPreview = Clone(OverlayPreview),
                AbsoluteDifferencePreview = Clone(AbsoluteDifferencePreview),
                EdgeOverlayPreview = Clone(EdgeOverlayPreview),
                BinaryMaskPreview = Clone(BinaryMaskPreview),
                ErrorHeatmapPreview = Clone(ErrorHeatmapPreview),
                Metrics = Metrics,
                IsAuthoritative = IsAuthoritative,
                CoordinateSpace = CoordinateSpace,
                WarningMessage = WarningMessage,
                SamplePath = SamplePath,
                RealityPath = RealityPath,
                OutputDirectory = OutputDirectory
            };
        }

        public void Dispose()
        {
            if (SamplePreview != null) { SamplePreview.Dispose(); SamplePreview = null; }
            if (RealityPreview != null) { RealityPreview.Dispose(); RealityPreview = null; }
            if (OverlayPreview != null) { OverlayPreview.Dispose(); OverlayPreview = null; }
            if (AbsoluteDifferencePreview != null) { AbsoluteDifferencePreview.Dispose(); AbsoluteDifferencePreview = null; }
            if (EdgeOverlayPreview != null) { EdgeOverlayPreview.Dispose(); EdgeOverlayPreview = null; }
            if (BinaryMaskPreview != null) { BinaryMaskPreview.Dispose(); BinaryMaskPreview = null; }
            if (ErrorHeatmapPreview != null) { ErrorHeatmapPreview.Dispose(); ErrorHeatmapPreview = null; }
        }

        private static Bitmap LoadBitmap(string path) { return string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? null : new Bitmap(path); }
        private static Bitmap Clone(Bitmap bitmap) { return bitmap == null ? null : (Bitmap)bitmap.Clone(); }
    }

    public partial class SampleComparisonControl : UserControl
    {
        private SampleComparisonViewData _data;
        private Bitmap _modeGeneratedPreview;
        private bool _blinkShowSample = true;
        private bool _isSynchronizingView;

        public SampleComparisonControl()
        {
            InitializeComponent();
            cmbComparisonMode.DataSource = Enum.GetValues(typeof(ComparisonMode));
            cmbComparisonMode.SelectedItem = ComparisonMode.SideBySide;
            comparisonBlinkTimer.Tick += comparisonBlinkTimer_Tick;
            cmbComparisonMode.SelectedIndexChanged += (s, e) => UpdateModeView();
            nudAlpha.ValueChanged += (s, e) => UpdateModeView();
            btnRefreshComparison.Click += (s, e) => UpdateModeView();
            btnStopBlink.Click += (s, e) => StopBlink();
            btnFitBoth.Click += (s, e) => FitBoth();
            btnResetView.Click += (s, e) => ResetBoth();
            btnSaveCurrentView.Click += (s, e) => SaveCurrentView();
            btnOpenOutputFolder.Click += (s, e) => OpenOutputFolder();
            sampleImageView.ViewChanged += imageView_ViewChanged;
            realityImageView.ViewChanged += imageView_ViewChanged;
            sampleImageView.ImageMouseMove += imageView_ImageMouseMove;
            realityImageView.ImageMouseMove += imageView_ImageMouseMove;
            ClearComparisonResult();
        }

        public ComparisonMode CurrentMode { get { return (ComparisonMode)cmbComparisonMode.SelectedItem; } }
        public bool IsBlinkRunning { get { return comparisonBlinkTimer.Enabled; } }
        public string CoordinateStatusText { get { return txtCoordinateStatus.Text; } }
        public string MetricsDisplayText { get { return txtMetrics.Text; } }
        public string WarningDisplayText { get { return txtWarnings.Text; } }

        public void SetComparisonResult(SampleComparisonViewData data, bool cloneImages)
        {
            StopBlink();
            ClearOwnedData();
            _data = cloneImages && data != null ? data.CloneForUi() : data;
            if (_data != null)
            {
                _data.BinaryMaskPreview = _data.BinaryMaskPreview ?? CreateBinaryMaskComparison(_data.SamplePreview, _data.RealityPreview);
                _data.ErrorHeatmapPreview = _data.ErrorHeatmapPreview ?? CreateErrorHeatmap(_data.AbsoluteDifferencePreview);
            }
            BindMetadata();
            UpdateModeView();
        }

        public void SetComparisonResult(SampleComparisonResult result, string samplePath, string realityPath)
        {
            using (var data = SampleComparisonViewData.FromResult(result, samplePath, realityPath)) SetComparisonResult(data, true);
        }

        public void ClearComparisonResult()
        {
            StopBlink();
            ClearOwnedData();
            txtCoordinateStatus.Text = "No comparison result loaded.";
            txtInputInfo.Text = string.Empty;
            txtMetrics.Text = string.Empty;
            txtWarnings.Text = string.Empty;
            lblCursorInfo.Text = "Cursor: -";
            sampleImageView.ClearImage();
            realityImageView.ClearImage();
        }

        public void SelectComparisonMode(ComparisonMode mode)
        {
            cmbComparisonMode.SelectedItem = mode;
            UpdateModeView();
        }

        public void StartBlinkForTest()
        {
            SelectComparisonMode(ComparisonMode.Blink);
            StartBlink();
        }

        public void StopBlinkForTest() { StopBlink(); }
        public void FitBoth() { sampleImageView.FitToWindow(); realityImageView.FitToWindow(); }
        public void ResetBoth() { sampleImageView.ResetView(); realityImageView.ResetView(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopBlink();
                ClearOwnedData();
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void BindMetadata()
        {
            if (_data == null) return;
            txtCoordinateStatus.Text = _data.IsAuthoritative
                ? "Authoritative comparison in " + Safe(_data.CoordinateSpace)
                : "Visual comparison only. Accuracy metrics are not authoritative because coordinate mapping is incomplete.";
            txtInputInfo.Text = "Sample path: " + Safe(_data.SamplePath) + Environment.NewLine +
                                "Reality path: " + Safe(_data.RealityPath) + Environment.NewLine +
                                "Sample dimensions: " + FormatSize(_data.SamplePreview) + Environment.NewLine +
                                "Reality dimensions: " + FormatSize(_data.RealityPreview) + Environment.NewLine +
                                "Coordinate space: " + Safe(_data.CoordinateSpace) + Environment.NewLine +
                                "Authoritative: " + (_data.IsAuthoritative ? "Yes" : "No");
            txtWarnings.Text = _data.WarningMessage ?? string.Empty;
            txtMetrics.Text = FormatMetrics(_data.Metrics, _data.IsAuthoritative);
        }

        private void UpdateModeView()
        {
            if (_data == null)
            {
                sampleImageView.ClearImage();
                realityImageView.ClearImage();
                return;
            }
            DisposeModeGeneratedPreview();
            StopBlink(false);
            var mode = CurrentMode;
            grpSample.Text = "Sample Image";
            grpReality.Text = "Reality Image / Comparison Result";
            sampleImageView.SetImage(_data.SamplePreview, true, true);
            switch (mode)
            {
                case ComparisonMode.SideBySide:
                    realityImageView.SetImage(_data.RealityPreview, true, true);
                    break;
                case ComparisonMode.SampleOnly:
                    realityImageView.ClearImage();
                    break;
                case ComparisonMode.RealityOnly:
                    sampleImageView.ClearImage();
                    realityImageView.SetImage(_data.RealityPreview, true, true);
                    break;
                case ComparisonMode.AlphaOverlay:
                    _modeGeneratedPreview = CreateAlphaOverlay(_data.SamplePreview, _data.RealityPreview, (double)nudAlpha.Value / 100.0);
                    realityImageView.SetImage(_modeGeneratedPreview, true, true);
                    grpReality.Text = "Alpha Overlay";
                    break;
                case ComparisonMode.Blink:
                    StartBlink();
                    break;
                case ComparisonMode.AbsoluteDifference:
                    realityImageView.SetImage(_data.AbsoluteDifferencePreview, true, true);
                    grpReality.Text = "Absolute Difference";
                    break;
                case ComparisonMode.EdgeOverlay:
                    realityImageView.SetImage(_data.EdgeOverlayPreview, true, true);
                    grpReality.Text = "Edge Overlay";
                    break;
                case ComparisonMode.BinaryMaskComparison:
                    realityImageView.SetImage(_data.BinaryMaskPreview, true, true);
                    grpReality.Text = "Binary Mask Comparison";
                    break;
                case ComparisonMode.ErrorHeatmap:
                    realityImageView.SetImage(_data.ErrorHeatmapPreview, true, true);
                    grpReality.Text = "Error Heatmap";
                    break;
            }
        }

        private void StartBlink()
        {
            if (_data == null) return;
            _blinkShowSample = true;
            comparisonBlinkTimer.Interval = (int)nudBlinkInterval.Value;
            comparisonBlinkTimer.Start();
            comparisonBlinkTimer_Tick(this, EventArgs.Empty);
        }

        private void StopBlink() { StopBlink(true); }
        private void StopBlink(bool restoreModeImage)
        {
            comparisonBlinkTimer.Stop();
            if (restoreModeImage && _data != null && CurrentMode == ComparisonMode.Blink) realityImageView.SetImage(_data.RealityPreview, true, true);
        }

        private void comparisonBlinkTimer_Tick(object sender, EventArgs e)
        {
            if (_data == null) { StopBlink(); return; }
            realityImageView.SetImage(_blinkShowSample ? _data.SamplePreview : _data.RealityPreview, true, true);
            grpReality.Text = _blinkShowSample ? "Blink: Sample Image" : "Blink: Reality Image";
            _blinkShowSample = !_blinkShowSample;
        }

        private void imageView_ViewChanged(object sender, EventArgs e)
        {
            if (!chkSynchronizeView.Checked || _isSynchronizingView) return;
            var source = sender as ComparisonImageView;
            var target = object.ReferenceEquals(source, sampleImageView) ? realityImageView : sampleImageView;
            _isSynchronizingView = true;
            target.SetView(source.Zoom, source.ViewOffset, false);
            _isSynchronizingView = false;
        }

        private void imageView_ImageMouseMove(object sender, ComparisonImageMouseEventArgs e)
        {
            if (_data == null) return;
            var x = (int)Math.Floor(e.ImagePoint.X);
            var y = (int)Math.Floor(e.ImagePoint.Y);
            lblCursorInfo.Text = string.Format("ProcessedSampleGlobalPixels X={0} Y={1}; Sample={2}; Reality={3}; Diff={4}; Valid={5}", x, y, PixelValue(_data.SamplePreview, x, y), PixelValue(_data.RealityPreview, x, y), PixelValue(_data.AbsoluteDifferencePreview, x, y), IsInside(_data.SamplePreview, x, y) && IsInside(_data.RealityPreview, x, y) ? "Yes" : "No");
        }

        private void SaveCurrentView()
        {
            if (_data == null) return;
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "PNG image|*.png";
                dlg.FileName = "comparison_view.png";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                using (var bitmap = new Bitmap(Math.Max(1, realityImageView.Width), Math.Max(1, realityImageView.Height)))
                {
                    realityImageView.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                    bitmap.Save(dlg.FileName, ImageFormat.Png);
                }
            }
        }

        private void OpenOutputFolder()
        {
            if (_data == null || string.IsNullOrWhiteSpace(_data.OutputDirectory) || !Directory.Exists(_data.OutputDirectory)) return;
            Process.Start(_data.OutputDirectory);
        }

        private void ClearOwnedData()
        {
            DisposeModeGeneratedPreview();
            if (_data != null) { _data.Dispose(); _data = null; }
        }

        private void DisposeModeGeneratedPreview()
        {
            if (_modeGeneratedPreview != null) { _modeGeneratedPreview.Dispose(); _modeGeneratedPreview = null; }
        }

        private static string FormatMetrics(ComparisonMetrics metrics, bool authoritative)
        {
            if (metrics == null) return string.Empty;
            var prefix = authoritative ? string.Empty : "PREVIEW ONLY - ";
            var sb = new StringBuilder();
            sb.AppendLine(prefix + "Valid Overlap Ratio: " + FormatRatio(metrics.ValidOverlapRatio));
            sb.AppendLine(prefix + "Normalized Cross-Correlation: " + FormatNumber(metrics.NormalizedCrossCorrelation));
            sb.AppendLine(prefix + "Binary Mask IoU: " + FormatRatio(metrics.BinaryMaskIoU));
            sb.AppendLine(prefix + "Edge Precision: " + FormatRatio(metrics.EdgePrecision));
            sb.AppendLine(prefix + "Edge Recall: " + FormatRatio(metrics.EdgeRecall));
            sb.AppendLine(prefix + "Edge F1 Score: " + FormatRatio(metrics.EdgeF1Score));
            sb.AppendLine(prefix + "Mean Edge Distance: " + FormatNumber(metrics.MeanEdgeDistancePixels) + " px");
            sb.AppendLine(prefix + "P95 Edge Distance: " + FormatNumber(metrics.P95EdgeDistancePixels) + " px");
            sb.AppendLine(prefix + "Absolute Difference Mean: " + FormatNumber(metrics.AbsoluteDifferenceMean));
            sb.AppendLine(prefix + "Absolute Difference P95: " + FormatNumber(metrics.AbsoluteDifferenceP95));
            sb.AppendLine(prefix + "Absolute Difference Max: " + FormatNumber(metrics.AbsoluteDifferenceMax));
            sb.AppendLine("No OverallScore is shown; metrics are independent, not a hidden composite accuracy value.");
            return sb.ToString();
        }

        private static Bitmap CreateAlphaOverlay(Bitmap sample, Bitmap reality, double alpha)
        {
            if (sample == null || reality == null) return null;
            var width = Math.Min(sample.Width, reality.Width);
            var height = Math.Min(sample.Height, reality.Height);
            var result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(result))
            {
                var cm = new System.Drawing.Imaging.ColorMatrix { Matrix33 = (float)Math.Max(0, Math.Min(1, alpha)) };
                using (var attr = new ImageAttributes())
                {
                    attr.SetColorMatrix(cm);
                    g.DrawImage(sample, new Rectangle(0, 0, width, height), 0, 0, width, height, GraphicsUnit.Pixel, attr);
                }
                cm.Matrix33 = (float)(1.0 - Math.Max(0, Math.Min(1, alpha)));
                using (var attr = new ImageAttributes())
                {
                    attr.SetColorMatrix(cm);
                    g.DrawImage(reality, new Rectangle(0, 0, width, height), 0, 0, width, height, GraphicsUnit.Pixel, attr);
                }
            }
            return result;
        }

        private static Bitmap CreateBinaryMaskComparison(Bitmap sample, Bitmap reality)
        {
            if (sample == null || reality == null) return null;
            var width = Math.Min(sample.Width, reality.Width);
            var height = Math.Min(sample.Height, reality.Height);
            var result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                var s = Brightness(sample.GetPixel(x, y)) > 20;
                var r = Brightness(reality.GetPixel(x, y)) > 20;
                result.SetPixel(x, y, s && r ? Color.White : s ? Color.Blue : r ? Color.Red : Color.Black);
            }
            return result;
        }

        private static Bitmap CreateErrorHeatmap(Bitmap difference)
        {
            if (difference == null) return null;
            var result = new Bitmap(difference.Width, difference.Height, PixelFormat.Format24bppRgb);
            for (int y = 0; y < difference.Height; y++) for (int x = 0; x < difference.Width; x++)
            {
                var v = Math.Min(255, Brightness(difference.GetPixel(x, y)) * 3);
                result.SetPixel(x, y, Color.FromArgb(v, Math.Max(0, v - 80), 255 - v));
            }
            return result;
        }

        private static int Brightness(Color c) { return (c.R + c.G + c.B) / 3; }
        private static bool IsInside(Bitmap bitmap, int x, int y) { return bitmap != null && x >= 0 && y >= 0 && x < bitmap.Width && y < bitmap.Height; }
        private static string PixelValue(Bitmap bitmap, int x, int y) { if (!IsInside(bitmap, x, y)) return "-"; var c = bitmap.GetPixel(x, y); return c.R == c.G && c.G == c.B ? c.R.ToString() : string.Format("R{0} G{1} B{2}", c.R, c.G, c.B); }
        private static string FormatSize(Bitmap bitmap) { return bitmap == null ? "-" : bitmap.Width + " x " + bitmap.Height; }
        private static string Safe(string value) { return string.IsNullOrWhiteSpace(value) ? "-" : value; }
        private static string FormatRatio(double value) { return value.ToString("0.000") + " (" + (value * 100.0).ToString("0.0") + "%)"; }
        private static string FormatNumber(double value) { return value.ToString("0.###"); }
    }
}
