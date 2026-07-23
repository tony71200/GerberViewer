using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HalconDotNet;
using GerberViewer.Stitching.Alignment;
using GerberViewer.Stitching.Arrangement;
using GerberViewer.Stitching.Comparison;
using GerberViewer.Stitching.DesignControls;
using GerberViewer.Stitching.Models;
using GerberViewer.Workflow.Models;

namespace GerberViewer.Views
{
    public partial class AlignStitchingControl : UserControl
    {
        private WorkflowContext _workflowContext;
        private readonly CapturedImageLoader _capturedImageLoader = new CapturedImageLoader();
        private IList<CapturedImageInfo> _capturedImages = new List<CapturedImageInfo>();
        private SampleManifest _manifest;
        private GerberViewer.Stitching.Models.AlignStitchConfig _config = new GerberViewer.Stitching.Models.AlignStitchConfig();
        private CancellationTokenSource _runCancellation;
        private readonly Elog_1_0.Elog _logger = new Elog_1_0.Elog();
        private AlignStitchWorkflowResult _lastWorkflowResult;
        private SampleComparisonResult _lastComparisonResult;
        private string _lastPublishedStitchedPath;

        public WorkflowContext WorkflowContext
        {
            get { return _workflowContext; }
            set
            {
                if (object.ReferenceEquals(_workflowContext, value)) return;

                if (_workflowContext != null)
                    _workflowContext.Changed -= WorkflowContext_Changed;

                _workflowContext = value;

                if (_workflowContext != null)
                {
                    if (_workflowContext.AlignStitchConfig == null)
                        _workflowContext.AlignStitchConfig = new GerberViewer.Stitching.Models.AlignStitchConfig();
                    _config = _workflowContext.AlignStitchConfig;
                    _workflowContext.Changed += WorkflowContext_Changed;
                }
                else
                {
                    _config = new GerberViewer.Stitching.Models.AlignStitchConfig();
                }

                RefreshContextUi();
            }
        }

        public AlignStitchingControl() { InitializeComponent(); InitializeLogger(); alignConfigGrid.SelectedObject = _config; orderPathCanvas.NodeSelected += orderPathCanvas_NodeSelected; lstCapturedImages.SelectedIndexChanged += lstCapturedImages_SelectedIndexChanged; RefreshContextUi(); }
        private void InitializeLogger() { _logger.Debug = true; _logger.SetOpenListBox(true, lstTab3Log); _logger.SetOpenFile(true, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Tab3"), "AlignStitch"); }
        private void WorkflowContext_Changed(object sender, EventArgs e) { RefreshContextUi(); }

        private void RefreshContextUi()
        {
            if (alignConfigGrid != null && !object.ReferenceEquals(alignConfigGrid.SelectedObject, _config))
                alignConfigGrid.SelectedObject = _config;

            if (_workflowContext != null)
            {
                if (!string.IsNullOrWhiteSpace(_workflowContext.ManifestPath))
                {
                    txtManifestPath.Text = _workflowContext.ManifestPath;
                    _config.InputManifestPath = _workflowContext.ManifestPath;
                }
                if (!string.IsNullOrWhiteSpace(_workflowContext.OutputDirectory))
                {
                    txtOutputFolder.Text = _workflowContext.OutputDirectory;
                    _config.OutputPath = _workflowContext.OutputDirectory;
                }
            }

            UpdateRunState();
        }

        private void btnSelectManifest_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "sample_manifest.json|sample_manifest.json|JSON files|*.json|All files|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                LoadManifest(dlg.FileName);
            }
        }

        private void LoadManifest(string path)
        {
            ClearManifestState();
            try
            {
                var manifest = SampleManifestSerializer.Read(path);
                var validation = SampleManifestValidator.Validate(manifest, true);
                if (!validation.IsValid) { ShowBlocked("Manifest validation blocked", validation.Errors); return; }
                _manifest = manifest; txtManifestPath.Text = path; _config.InputManifestPath = path;
                if (_workflowContext != null) { _workflowContext.ManifestPath = path; _workflowContext.AlignStitchConfig = _config; _workflowContext.NotifyChanged(); }
                RenderManifestInfo(path, manifest); _logger.WriteInfo("Manifest selected and validated: " + path); LoadCapturedImages();
            }
            catch (Exception ex) { ShowBlocked("Manifest read failed", new[] { ex.Message }); }
            UpdateRunState();
        }

        private void RenderManifestInfo(string path, SampleManifest manifest)
        {
            var folder = Path.GetDirectoryName(path) ?? string.Empty;
            var tilesFolder = CommonParent(manifest.Tiles.Select(t => ResolveTilePath(folder, t.ExpectedPath)).ToList());
            txtManifestInfo.Text = string.Join(Environment.NewLine, new[] {
                "Manifest file: " + path,
                "Manifest folder: " + folder,
                "Sample run root: " + (Directory.Exists(manifest.RootDirectory) ? manifest.RootDirectory : folder),
                "Sample tiles folder: " + tilesFolder,
                "Source sample raster: " + manifest.SourceRasterPath,
                "Processed sample/reference image: ProcessedSampleGlobalPixels " + manifest.ProcessedWidth + " x " + manifest.ProcessedHeight,
                "Rows x Columns: " + (manifest.Tiles.Max(t => t.Row) + 1) + " x " + (manifest.Tiles.Max(t => t.Column) + 1),
                "Expected tile count: " + manifest.Tiles.Count,
                "Crop order: " + manifest.CropOrder,
                "Start order: " + manifest.StartOrder,
                "Coordinate space: Expected X/Y and Global X/Y are processed-sample pixels"
            });
        }

        private void btnOpenImageFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select captured image folder";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                txtImageFolder.Text = dlg.SelectedPath; _config.CapturedFolderPath = dlg.SelectedPath; _logger.WriteInfo("Captured folder selected: " + dlg.SelectedPath); LoadCapturedImages();
            }
        }

        private void btnSelectOutputFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select output root folder (the root will not be deleted)";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                sampleComparisonControl.ClearComparisonResult();
                txtOutputFolder.Text = dlg.SelectedPath; _config.OutputPath = dlg.SelectedPath; if (_workflowContext != null) { _workflowContext.OutputDirectory = dlg.SelectedPath; _workflowContext.AlignStitchConfig = _config; _workflowContext.NotifyChanged(); } _logger.WriteInfo("Output root selected: " + dlg.SelectedPath); UpdateRunState();
            }
        }

        private void LoadCapturedImages()
        {
            _capturedImages = new List<CapturedImageInfo>(); lstCapturedImages.Items.Clear(); orderPathCanvas.SetCapturedImages(_capturedImages);
            if (_manifest == null || string.IsNullOrWhiteSpace(txtImageFolder.Text)) { UpdateRunState(); return; }
            var result = _capturedImageLoader.Load(txtImageFolder.Text, txtManifestPath.Text);
            if (!result.Succeeded) { lblImageCount.Text = "Images: blocked"; ShowBlocked("Captured validation blocked", result.Errors); UpdateRunState(); return; }
            sampleComparisonControl.ClearComparisonResult();
            _capturedImages = result.Images;
            foreach (var image in _capturedImages) lstCapturedImages.Items.Add(string.Format("{0:000}: R{1} C{2} - {3}", image.OrderIndex, image.Row, image.Column, Path.GetFileName(image.FilePath)));
            lblImageCount.Text = "Images: " + _capturedImages.Count + " / " + result.ExpectedTileCount;
            orderPathCanvas.SetCapturedImages(_capturedImages); _logger.WriteInfo("Captured images validated: " + _capturedImages.Count); UpdateRunState();
        }

        private async void btnRunAlignStitch_Click(object sender, EventArgs e)
        {
            if (_runCancellation != null) return;
            if (!ValidateRunInputs()) return;

            var finalRunDir = Path.Combine(txtOutputFolder.Text, "AlignStitch_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
            var creatingDir = Path.Combine(finalRunDir, ".creating");
            Directory.CreateDirectory(creatingDir);
            _logger.WriteInfo("Run start. Application-owned creating directory: " + creatingDir);
            _runCancellation = new CancellationTokenSource();
            SetRunControlsEnabled(false);
            prgAlignStitch.Value = 0;
            var runConfig = CloneConfigForRun(_config, creatingDir);

            try
            {
                sampleComparisonControl.ClearComparisonResult();
                _lastWorkflowResult = null;
                _lastComparisonResult = null;
                _lastPublishedStitchedPath = null;
                var svc = new AlignStitchWorkflowService(null, null);
                var progress = new Progress<WorkflowProgress>(p =>
                {
                    prgAlignStitch.Value = p.Total <= 0 ? 0 : Math.Min(100, p.Current * 100 / p.Total);
                    if (p.Image != null) orderPathCanvas.SetSnapshot(BuildProgressSnapshot(p.Image.OrderIndex, OrderNodeState.Processing));
                    _logger.WriteInfo("OrderIndex " + (p.Image == null ? -1 : p.Image.OrderIndex) + " Stage " + p.Stage);
                });

                var result = await svc.RunAsync(runConfig, _manifest, _capturedImages, progress, _runCancellation.Token);
                _runCancellation.Token.ThrowIfCancellationRequested();
                _lastWorkflowResult = result;
                RebuildFinalStates(result);
                orderPathCanvas.SetFinalStates(result.States, result.Report.RecoveryEdges);
                RefreshDiagnostics();
                if (!result.Report.Succeeded) throw new InvalidOperationException("Alignment did not produce verified poses for every tile; stitching publication is blocked.");

                var comparison = await GenerateComparisonAsync(result, _runCancellation.Token);
                _runCancellation.Token.ThrowIfCancellationRequested();
                var reportPath = WriteProcessingReport(result.Report, creatingDir);
                ValidateRunOutputs(result.Report, reportPath);
                PublishRunDirectory(finalRunDir, creatingDir);
                var publishedStitchedPath = Path.Combine(finalRunDir, Path.GetFileName(result.Report.FinalOutputPath));
                result.Report.FinalOutputPath = publishedStitchedPath;
                BindPublishedComparison(comparison, finalRunDir, publishedStitchedPath);
                ShowPublishedStitchedResult();
                if (_workflowContext != null)
                {
                    _workflowContext.LastStitchedOutputPath = publishedStitchedPath;
                    _workflowContext.AlignStitchConfig = _config;
                    _workflowContext.NotifyChanged();
                }
                _logger.WriteInfo("Run published: " + finalRunDir);
            }
            catch (OperationCanceledException)
            {
                _logger.WriteWarning("Run cancelled; completed output was not published.");
                CleanupCreatingDirectory(creatingDir);
            }
            catch (Exception ex)
            {
                _logger.WriteError("Run failed: " + ex);
                CleanupCreatingDirectory(creatingDir);
                MessageBox.Show(this, ex.Message, "Align/Stitch failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (_runCancellation != null) _runCancellation.Dispose();
                _runCancellation = null;
                SetRunControlsEnabled(true);
                UpdateRunState();
            }
        }

        private void btnCancelAlignStitch_Click(object sender, EventArgs e) { if (_runCancellation != null) _runCancellation.Cancel(); }
        private void ClearManifestState() { _manifest = null; _lastWorkflowResult = null; _lastComparisonResult = null; _lastPublishedStitchedPath = null; txtManifestPath.Text = string.Empty; txtManifestInfo.Clear(); txtDiagnostics.Clear(); sampleComparisonControl.ClearComparisonResult(); _capturedImages = new List<CapturedImageInfo>(); lstCapturedImages.Items.Clear(); orderPathCanvas.SetCapturedImages(_capturedImages); }

        private bool ValidateRunInputs()
        {
            if (_manifest == null) { ShowBlocked("Run blocked", new[] { "No validated manifest is loaded." }); return false; }
            if (_capturedImages == null || _capturedImages.Count != (_manifest.Tiles == null ? -1 : _manifest.Tiles.Count)) { ShowBlocked("Run blocked", new[] { "Captured folder is not validated against the manifest." }); return false; }
            if (string.IsNullOrWhiteSpace(txtOutputFolder.Text) || !Directory.Exists(txtOutputFolder.Text)) { sampleComparisonControl.ClearComparisonResult(); ShowBlocked("Run blocked", new[] { "Output folder is missing or invalid: " + (txtOutputFolder.Text ?? "-") }); return false; }
            return true;
        }

        private static GerberViewer.Stitching.Models.AlignStitchConfig CloneConfigForRun(GerberViewer.Stitching.Models.AlignStitchConfig source, string creatingDir)
        {
            source = source ?? new GerberViewer.Stitching.Models.AlignStitchConfig();
            return new GerberViewer.Stitching.Models.AlignStitchConfig
            {
                InputManifestPath = source.InputManifestPath, CapturedFolderPath = source.CapturedFolderPath, OutputPath = creatingDir, AlignmentMethod = source.AlignmentMethod,
                NccMinScore = source.NccMinScore, EccMinCorrelation = source.EccMinCorrelation, MaxTranslationPixels = source.MaxTranslationPixels, MaxAbsRotationDeg = source.MaxAbsRotationDeg,
                MinScale = source.MinScale, MaxScale = source.MaxScale, MinOverlapRatio = source.MinOverlapRatio, AllowNccOnlyAcceptance = source.AllowNccOnlyAcceptance,
                AllowEccFromExpectedWhenNccFails = source.AllowEccFromExpectedWhenNccFails, EnableNeighborRecovery = source.EnableNeighborRecovery, EnableAnchorInterpolation = source.EnableAnchorInterpolation,
                AllowExpectedGridFallback = source.AllowExpectedGridFallback, RequireManualConfirmationForExpectedGrid = source.RequireManualConfirmationForExpectedGrid, StitchingEngine = source.StitchingEngine, PreviewUpdateInterval = source.PreviewUpdateInterval,
                MaxPreviewMegapixels = source.MaxPreviewMegapixels, TiffMode = source.TiffMode, BigTiffTileWidth = source.BigTiffTileWidth, BigTiffTileHeight = source.BigTiffTileHeight
            };
        }

        private void RebuildFinalStates(AlignStitchWorkflowResult result)
        {
            foreach (var state in result.States)
            {
                var img = _capturedImages.FirstOrDefault(x => x.OrderIndex == state.OrderIndex);
                if (img != null) img.State = ToNodeState(state.Source);
            }
        }

        private void SetRunControlsEnabled(bool enabled)
        {
            btnSelectManifest.Enabled = enabled;
            btnOpenImageFolder.Enabled = enabled;
            btnSelectOutputFolder.Enabled = enabled;
            alignConfigGrid.Enabled = enabled;
            lstCapturedImages.Enabled = enabled;
            btnRunAlignStitch.Enabled = enabled && _runCancellation == null;
            btnCancelAlignStitch.Enabled = !enabled;
        }

        private async System.Threading.Tasks.Task<SampleComparisonResult> GenerateComparisonAsync(AlignStitchWorkflowResult workflowResult, CancellationToken token)
        {
            if (workflowResult == null || workflowResult.Report == null || string.IsNullOrWhiteSpace(workflowResult.Report.FinalOutputPath) || _manifest == null) return null;
            var stitchedPath = workflowResult.Report.FinalOutputPath;
            var outputDir = Path.Combine(Path.GetDirectoryName(stitchedPath) ?? txtOutputFolder.Text, "comparison");
            var service = new SampleComparisonService();
            var request = new SampleComparisonRequest { Manifest = _manifest, StitchedImagePath = stitchedPath, OutputDirectory = outputDir, AllowNonAuthoritativeVisualPreview = true, Alpha = 0.5, MaxPreviewMegapixels = _config.MaxPreviewMegapixels };
            var comparison = await System.Threading.Tasks.Task.Run(() => service.Generate(request, token), token);
            _logger.WriteInfo("Sample comparison generated: " + comparison.MetadataPath);
            return comparison;
        }

        private void BindPublishedComparison(SampleComparisonResult comparison, string finalRunDir, string publishedStitchedPath)
        {
            _lastComparisonResult = comparison;
            _lastPublishedStitchedPath = publishedStitchedPath;
            if (comparison == null) return;
            var comparisonDir = Path.Combine(finalRunDir, "comparison");
            comparison.SamplePreviewPath = RebasePublishedPath(comparison.SamplePreviewPath, comparisonDir);
            comparison.StitchedPreviewPath = RebasePublishedPath(comparison.StitchedPreviewPath, comparisonDir);
            comparison.AlphaOverlayPath = RebasePublishedPath(comparison.AlphaOverlayPath, comparisonDir);
            comparison.AbsoluteDifferencePath = RebasePublishedPath(comparison.AbsoluteDifferencePath, comparisonDir);
            comparison.EdgeOverlayPath = RebasePublishedPath(comparison.EdgeOverlayPath, comparisonDir);
            comparison.MetadataPath = RebasePublishedPath(comparison.MetadataPath, comparisonDir);
            var manifestFolder = Path.GetDirectoryName(txtManifestPath.Text) ?? string.Empty;
            var samplePath = ResolveOptionalPath(manifestFolder, !string.IsNullOrWhiteSpace(_manifest.ProcessedSamplePath) ? _manifest.ProcessedSamplePath : _manifest.SourceRasterPath);
            sampleComparisonControl.SetComparisonResult(comparison, samplePath, publishedStitchedPath);
        }

        private void ShowPublishedStitchedResult()
        {
            var path = chkShowSampleMask.Checked && _lastComparisonResult != null && !string.IsNullOrWhiteSpace(_lastComparisonResult.AlphaOverlayPath)
                ? _lastComparisonResult.AlphaOverlayPath
                : _lastPublishedStitchedPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
            HObject image = null;
            try
            {
                HOperatorSet.ReadImage(out image, path);
                resultWindow.SetSourceImage(image, true);
                resultWindow.RenderImageOverlay(BuildStitchedOverlays());
                resultTabControl.SelectedTab = tabStitchedImage;
                _logger.WriteInfo("Stitched result displayed in tabStitchedImage using " + (chkShowSampleMask.Checked ? "sample overlap mask" : "stitched output") + ": " + path);
            }
            finally
            {
                if (image != null && image.IsInitialized()) image.Dispose();
            }
        }

        private IList<Tuple<System.Drawing.Rectangle, string, string>> BuildStitchedOverlays()
        {
            var overlays = new List<Tuple<System.Drawing.Rectangle, string, string>>();
            if (_lastWorkflowResult == null || _lastWorkflowResult.States == null || _capturedImages == null) return overlays;
            var bounds = CalculateStitchedBounds(_lastWorkflowResult.States, _capturedImages);
            foreach (var state in _lastWorkflowResult.States.OrderBy(s => s.OrderIndex))
            {
                var image = _capturedImages.FirstOrDefault(i => i.OrderIndex == state.OrderIndex);
                var tile = _manifest == null || _manifest.Tiles == null ? null : _manifest.Tiles.FirstOrDefault(t => t.OrderIndex == state.OrderIndex);
                var rect = state.HasValidPose && image != null ? ProjectedBounds(state.GlobalPose, image.Width, image.Height, bounds) : ExpectedTileBounds(tile, bounds);
                if (rect.Width <= 0 || rect.Height <= 0) continue;
                var color = state.AlignmentSucceeded ? "green" : "yellow";
                overlays.Add(Tuple.Create(rect, color, ScoreLabel(state)));
            }
            return overlays;
        }

        private static System.Drawing.RectangleF CalculateStitchedBounds(IList<TileWorkflowState> states, IList<CapturedImageInfo> images)
        {
            var rects = states.Where(s => s.HasValidPose).Select(s => { var img = images.FirstOrDefault(i => i.OrderIndex == s.OrderIndex); return img == null ? System.Drawing.RectangleF.Empty : ProjectedBoundsF(s.GlobalPose, img.Width, img.Height); }).Where(r => r.Width > 0 && r.Height > 0).ToList();
            if (rects.Count == 0) return System.Drawing.RectangleF.Empty;
            var left = rects.Min(r => r.Left); var top = rects.Min(r => r.Top); var right = rects.Max(r => r.Right); var bottom = rects.Max(r => r.Bottom);
            return System.Drawing.RectangleF.FromLTRB(left, top, right, bottom);
        }

        private static System.Drawing.Rectangle ExpectedTileBounds(SampleTileInfo tile, System.Drawing.RectangleF canvasBounds)
        {
            if (tile == null) return System.Drawing.Rectangle.Empty;
            return new System.Drawing.Rectangle((int)Math.Round(tile.ExpectedX - canvasBounds.Left), (int)Math.Round(tile.ExpectedY - canvasBounds.Top), tile.Width, tile.Height);
        }

        private static System.Drawing.Rectangle ProjectedBounds(double[,] h, int width, int height, System.Drawing.RectangleF canvasBounds)
        {
            var r = ProjectedBoundsF(h, width, height);
            return System.Drawing.Rectangle.FromLTRB((int)Math.Floor(r.Left - canvasBounds.Left), (int)Math.Floor(r.Top - canvasBounds.Top), (int)Math.Ceiling(r.Right - canvasBounds.Left), (int)Math.Ceiling(r.Bottom - canvasBounds.Top));
        }

        private static System.Drawing.RectangleF ProjectedBoundsF(double[,] h, int width, int height)
        {
            var xs = new[] { 0d, width, 0d, width }; var ys = new[] { 0d, 0d, height, height };
            for (int i = 0; i < xs.Length; i++) { var x = xs[i]; var y = ys[i]; xs[i] = h[0, 0] * x + h[0, 1] * y + h[0, 2]; ys[i] = h[1, 0] * x + h[1, 1] * y + h[1, 2]; }
            return System.Drawing.RectangleF.FromLTRB((float)xs.Min(), (float)ys.Min(), (float)xs.Max(), (float)ys.Max());
        }

        private static string ScoreLabel(TileWorkflowState state)
        {
            var score = state == null || state.Alignment == null ? double.NaN : (!double.IsNaN(state.Alignment.NccScore) ? state.Alignment.NccScore : state.Alignment.EccCorrelation);
            if (double.IsNaN(score) || double.IsInfinity(score)) return "0";
            return Math.Max(0, Math.Min(100, (int)Math.Round(score * 100))).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void chkShowSampleMask_CheckedChanged(object sender, EventArgs e) { ShowPublishedStitchedResult(); }

        private static string RebasePublishedPath(string originalPath, string publishedDirectory)
        {
            return string.IsNullOrWhiteSpace(originalPath) ? originalPath : Path.Combine(publishedDirectory, Path.GetFileName(originalPath));
        }
        private string WriteProcessingReport(ProcessingReport report, string creatingDir)
        {
            var path = Path.Combine(creatingDir, "processing_report.json");
            var sb = new StringBuilder();
            sb.AppendLine("{");
            AppendJson(sb, "succeeded", report.Succeeded ? "true" : "false", true, true);
            AppendJson(sb, "runStatus", report.RunStatus.ToString(), true);
            AppendJson(sb, "finalOutputPath", report.FinalOutputPath, true);
            sb.AppendLine("  \"messages\": [");
            AppendJsonArray(sb, report.Messages);
            sb.AppendLine("  ],");
            sb.AppendLine("  \"warnings\": [");
            AppendJsonArray(sb, report.Warnings);
            sb.AppendLine("  ],");
            sb.AppendLine("  \"tileReports\": [");
            for (int i = 0; i < report.TileReports.Count; i++)
            {
                var r = report.TileReports[i];
                sb.Append("    {");
                sb.Append("\"orderIndex\": ").Append(r.OrderIndex).Append(", ");
                sb.Append("\"row\": ").Append(r.Row).Append(", ");
                sb.Append("\"column\": ").Append(r.Column).Append(", ");
                sb.Append("\"stage\": \"").Append(EscapeJson(r.PipelineStage)).Append("\", ");
                sb.Append("\"nccScore\": ").Append(FormatJsonNumber(r.NccScore)).Append(", ");
                sb.Append("\"eccCorrelation\": ").Append(FormatJsonNumber(r.EccCorrelation)).Append(", ");
                sb.Append("\"preprocessingVariant\": \"").Append(EscapeJson(r.PreprocessingVariant)).Append("\", ");
                sb.Append("\"rejectionReason\": \"").Append(EscapeJson(r.RejectionReason)).Append("\"}");
                sb.AppendLine(i + 1 == report.TileReports.Count ? string.Empty : ",");
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            File.WriteAllText(path, sb.ToString());
            return path;
        }

        private static void ValidateRunOutputs(ProcessingReport report, string reportPath)
        {
            if (report == null || string.IsNullOrWhiteSpace(report.FinalOutputPath) || !File.Exists(report.FinalOutputPath)) throw new IOException("Stitched output validation failed before publish.");
            if (string.IsNullOrWhiteSpace(reportPath) || !File.Exists(reportPath)) throw new IOException("Processing report was not written before publish.");
            using (var reopened = new System.Drawing.Bitmap(report.FinalOutputPath))
                if (reopened.Width <= 0 || reopened.Height <= 0) throw new IOException("Stitched TIFF reopen validation returned invalid dimensions.");
            var comparisonMetadata = Path.Combine(Path.GetDirectoryName(report.FinalOutputPath), "comparison", "comparison_metadata.json");
            if (!File.Exists(comparisonMetadata)) throw new IOException("Comparison metadata was not generated before publish.");
        }

        private static string PublishRunDirectory(string finalRunDir, string creatingDir)
        {
            if (!Directory.Exists(creatingDir)) throw new DirectoryNotFoundException("Creating directory missing before publish: " + creatingDir);
            foreach (var file in Directory.GetFiles(creatingDir))
            {
                var target = Path.Combine(finalRunDir, Path.GetFileName(file));
                if (File.Exists(target)) File.Delete(target);
                File.Move(file, target);
            }
            foreach (var dir in Directory.GetDirectories(creatingDir))
            {
                try
                {
                    var target = Path.Combine(finalRunDir, Path.GetFileName(dir));
                    if (Directory.Exists(target))
                    {
                        Directory.Delete(dir, true);
                        continue;
                    }
                    Directory.Move(dir, target);
                }
                catch (Exception ex)
                {
                    
                }
            }

            Directory.Delete(creatingDir, true);
            return finalRunDir;
        }

        private static void CleanupCreatingDirectory(string creatingDir)
        {
            if (!string.IsNullOrWhiteSpace(creatingDir) && Directory.Exists(creatingDir)) Directory.Delete(creatingDir, true);
        }

        private static void AppendJson(StringBuilder sb, string name, string value, bool comma, bool raw = false)
        {
            sb.Append("  \"").Append(name).Append("\": ");
            if (raw) sb.Append(value); else sb.Append("\"").Append(EscapeJson(value)).Append("\"");
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void AppendJsonArray(StringBuilder sb, IList<string> values)
        {
            values = values ?? new List<string>();
            for (int i = 0; i < values.Count; i++) sb.Append("    \"").Append(EscapeJson(values[i])).Append(i + 1 == values.Count ? "\"\n" : "\",\n");
        }

        private static string FormatJsonNumber(double value) { return double.IsNaN(value) || double.IsInfinity(value) ? "null" : value.ToString("R", System.Globalization.CultureInfo.InvariantCulture); }
        private static string EscapeJson(string value) { return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\""); }

        private void ShowBlocked(string title, IEnumerable<string> errors) { var message = string.Join(Environment.NewLine, errors); _logger.WriteWarning(title + ": " + message); MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        private void UpdateRunState() { btnRunAlignStitch.Enabled = _runCancellation == null && _manifest != null && _capturedImages.Count == (_manifest.Tiles == null ? -1 : _manifest.Tiles.Count) && Directory.Exists(txtOutputFolder.Text); }
        private static OrderNodeState ToNodeState(PoseSource source) { if (source == PoseSource.SampleAlignment) return OrderNodeState.SampleAlignOk; if (source == PoseSource.NeighborAlignment) return OrderNodeState.NeighborAlignOk; if (source == PoseSource.AnchorAdjusted) return OrderNodeState.AnchorAdjusted; if (source == PoseSource.Interpolated) return OrderNodeState.Interpolated; if (source == PoseSource.Manual) return OrderNodeState.Manual; if (source == PoseSource.Excluded) return OrderNodeState.Excluded; if (source == PoseSource.ExpectedGridOffset) return OrderNodeState.ExpectedGridOffset; return OrderNodeState.Failed; }
        private static string ResolveTilePath(string manifestFolder, string expectedPath) { return Path.IsPathRooted(expectedPath) ? expectedPath : Path.Combine(manifestFolder, expectedPath); }
        private static string ResolveOptionalPath(string manifestFolder, string path) { if (string.IsNullOrWhiteSpace(path)) return string.Empty; return Path.IsPathRooted(path) ? path : Path.Combine(manifestFolder, path); }
        private static string CommonParent(IList<string> paths) { if (paths == null || paths.Count == 0) return string.Empty; var dirs = paths.Select(Path.GetDirectoryName).Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); return dirs.Count == 1 ? dirs[0] : string.Join("; ", dirs.Take(3).ToArray()); }

        private PathCanvasSnapshot BuildProgressSnapshot(int processingOrderIndex, OrderNodeState processingState)
        {
            var nodes = _capturedImages.Select(x => new GerberViewer.Stitching.DesignControls.PathCanvasNode(x.OrderIndex, x.Row, x.Column, x.RobotX, x.RobotY, x.OrderIndex == processingOrderIndex ? processingState : x.State)).ToList();
            var edges = nodes.OrderBy(n => n.OrderIndex).Zip(nodes.OrderBy(n => n.OrderIndex).Skip(1), (a, b) => new GerberViewer.Stitching.DesignControls.PathCanvasEdge(a.NodeId, b.NodeId, "Expected order", null, null)).ToList();
            return new GerberViewer.Stitching.DesignControls.PathCanvasSnapshot(nodes, edges);
        }

        private void orderPathCanvas_NodeSelected(object sender, GerberViewer.Stitching.DesignControls.PathCanvasNodeSelectedEventArgs e)
        {
            SelectCapturedOrder(e.Node.OrderIndex, false);
        }

        private void lstCapturedImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstCapturedImages.SelectedIndex < 0 || lstCapturedImages.SelectedIndex >= _capturedImages.Count) return;
            SelectCapturedOrder(_capturedImages[lstCapturedImages.SelectedIndex].OrderIndex, true);
        }

        private void SelectCapturedOrder(int orderIndex, bool updateCanvas)
        {
            var idx = _capturedImages.ToList().FindIndex(x => x.OrderIndex == orderIndex);
            if (idx >= 0 && lstCapturedImages.SelectedIndex != idx) lstCapturedImages.SelectedIndex = idx;
            if (updateCanvas) orderPathCanvas.SetSelectedOrderIndex(orderIndex);
            RefreshDiagnostics(orderIndex);
        }

        private void RefreshDiagnostics(int? selectedOrderIndex = null)
        {
            if (_lastWorkflowResult == null || _lastWorkflowResult.Report == null) return;
            var lines = new List<string>();
            var reports = _lastWorkflowResult.Report.TileReports.Where(r => !selectedOrderIndex.HasValue || r.OrderIndex == selectedOrderIndex.Value).OrderBy(r => r.OrderIndex);
            foreach (var r in reports)
                lines.Add(string.Format("OrderIndex {0} R{1} C{2} Stage={3} NCC={4} ECC={5} Variant={6} Rejection={7} Recovery={8}", r.OrderIndex, r.Row, r.Column, r.PipelineStage, r.NccScore, r.EccCorrelation, r.PreprocessingVariant, r.RejectionReason, r.FallbackReason));
            foreach (var e in _lastWorkflowResult.Report.RecoveryEdges.Where(e => !selectedOrderIndex.HasValue || e.TargetOrderIndex == selectedOrderIndex.Value || e.AnchorOrderIndex == selectedOrderIndex.Value))
                lines.Add(string.Format("Recovery edge Anchor={0} Target={1} Direction={2} Matcher={3} Phase={4} ECC={5} Overlap={6} Reason={7} Transform={8}", e.AnchorOrderIndex, e.TargetOrderIndex, e.Direction, e.Matcher, e.PhaseScore, e.EccCorrelation, e.OverlapRatio, e.Reason, FormatTransform(e.TargetToAnchorTransform)));
            txtDiagnostics.Text = string.Join(Environment.NewLine, lines.ToArray());
        }

        private static string FormatTransform(double[,] matrix)
        {
            if (matrix == null) return "<null>";
            return string.Format("[{0:0.###},{1:0.###},{2:0.###}; {3:0.###},{4:0.###},{5:0.###}; {6:0.###},{7:0.###},{8:0.###}]", matrix[0,0], matrix[0,1], matrix[0,2], matrix[1,0], matrix[1,1], matrix[1,2], matrix[2,0], matrix[2,1], matrix[2,2]);
        }

        private void txtImageFolder_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
