using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using GerberViewer.Stitching.Alignment;
using GerberViewer.Stitching.Arrangement;
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
                txtOutputFolder.Text = dlg.SelectedPath; _config.OutputPath = dlg.SelectedPath; if (_workflowContext != null) { _workflowContext.OutputDirectory = dlg.SelectedPath; _workflowContext.AlignStitchConfig = _config; _workflowContext.NotifyChanged(); } _logger.WriteInfo("Output root selected: " + dlg.SelectedPath); UpdateRunState();
            }
        }

        private void LoadCapturedImages()
        {
            _capturedImages = new List<CapturedImageInfo>(); lstCapturedImages.Items.Clear(); orderPathCanvas.SetCapturedImages(_capturedImages);
            if (_manifest == null || string.IsNullOrWhiteSpace(txtImageFolder.Text)) { UpdateRunState(); return; }
            var result = _capturedImageLoader.Load(txtImageFolder.Text, txtManifestPath.Text);
            if (!result.Succeeded) { lblImageCount.Text = "Images: blocked"; ShowBlocked("Captured validation blocked", result.Errors); UpdateRunState(); return; }
            _capturedImages = result.Images;
            foreach (var image in _capturedImages) lstCapturedImages.Items.Add(string.Format("{0:000}: R{1} C{2} - {3}", image.OrderIndex, image.Row, image.Column, Path.GetFileName(image.FilePath)));
            lblImageCount.Text = "Images: " + _capturedImages.Count + " / " + result.ExpectedTileCount;
            orderPathCanvas.SetCapturedImages(_capturedImages); _logger.WriteInfo("Captured images validated: " + _capturedImages.Count); UpdateRunState();
        }

        private async void btnRunAlignStitch_Click(object sender, EventArgs e)
        {
            if (_runCancellation != null) return;
            var runDir = Path.Combine(txtOutputFolder.Text, "AlignStitch_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"), ".creating");
            Directory.CreateDirectory(runDir); _logger.WriteInfo("Run start. Application-owned creating directory: " + runDir);
            _runCancellation = new CancellationTokenSource(); btnRunAlignStitch.Enabled = false; btnCancelAlignStitch.Enabled = true; prgAlignStitch.Value = 0;
            try
            {
                var svc = new AlignStitchWorkflowService(null, null);
                var progress = new Progress<WorkflowProgress>(p => { prgAlignStitch.Value = p.Total <= 0 ? 0 : Math.Min(100, p.Current * 100 / p.Total); if (p.Image != null) orderPathCanvas.SetSnapshot(BuildProgressSnapshot(p.Image.OrderIndex, OrderNodeState.Processing)); _logger.WriteInfo("OrderIndex " + (p.Image == null ? -1 : p.Image.OrderIndex) + " Stage " + p.Stage); });
                var result = await svc.RunAsync(_config, _manifest, _capturedImages, progress, _runCancellation.Token);
                _lastWorkflowResult = result;
                foreach (var state in result.States) { var img = _capturedImages.FirstOrDefault(x => x.OrderIndex == state.OrderIndex); if (img != null) img.State = ToNodeState(state.Source); }
                orderPathCanvas.SetFinalStates(result.States, result.Report.RecoveryEdges);
                RefreshDiagnostics();
                if (!result.Report.Succeeded) throw new InvalidOperationException("Alignment did not produce verified poses for every tile; stitching publication is blocked.");
            }
            catch (OperationCanceledException) { _logger.WriteWarning("Run cancelled; no manifest/report was published."); }
            catch (Exception ex) { _logger.WriteError("Run failed: " + ex); MessageBox.Show(this, ex.Message, "Align/Stitch failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { _runCancellation.Dispose(); _runCancellation = null; btnCancelAlignStitch.Enabled = false; UpdateRunState(); }
        }

        private void btnCancelAlignStitch_Click(object sender, EventArgs e) { if (_runCancellation != null) _runCancellation.Cancel(); }
        private void ClearManifestState() { _manifest = null; _lastWorkflowResult = null; txtManifestPath.Text = string.Empty; txtManifestInfo.Clear(); txtDiagnostics.Clear(); _capturedImages = new List<CapturedImageInfo>(); lstCapturedImages.Items.Clear(); orderPathCanvas.SetCapturedImages(_capturedImages); }
        private void ShowBlocked(string title, IEnumerable<string> errors) { var message = string.Join(Environment.NewLine, errors); _logger.WriteWarning(title + ": " + message); MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        private void UpdateRunState() { btnRunAlignStitch.Enabled = _runCancellation == null && _manifest != null && _capturedImages.Count == (_manifest.Tiles == null ? -1 : _manifest.Tiles.Count) && Directory.Exists(txtOutputFolder.Text); }
        private static OrderNodeState ToNodeState(PoseSource source) { if (source == PoseSource.SampleAlignment) return OrderNodeState.SampleAlignOk; if (source == PoseSource.NeighborAlignment) return OrderNodeState.NeighborAlignOk; if (source == PoseSource.AnchorAdjusted) return OrderNodeState.AnchorAdjusted; if (source == PoseSource.Interpolated) return OrderNodeState.Interpolated; if (source == PoseSource.Manual) return OrderNodeState.Manual; if (source == PoseSource.Excluded) return OrderNodeState.Excluded; if (source == PoseSource.ExpectedGridOffset) return OrderNodeState.ExpectedGridOffset; return OrderNodeState.Failed; }
        private static string ResolveTilePath(string manifestFolder, string expectedPath) { return Path.IsPathRooted(expectedPath) ? expectedPath : Path.Combine(manifestFolder, expectedPath); }
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
