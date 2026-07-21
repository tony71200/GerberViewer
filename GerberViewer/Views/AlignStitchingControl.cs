using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using GerberViewer.Stitching.Alignment;
using GerberViewer.Stitching.Arrangement;
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
        private readonly GerberViewer.Stitching.Models.AlignStitchConfig _config = new GerberViewer.Stitching.Models.AlignStitchConfig();
        private CancellationTokenSource _runCancellation;
        private readonly Elog_1_0.Elog _logger = new Elog_1_0.Elog();

        public WorkflowContext WorkflowContext
        {
            get { return _workflowContext; }
            set { if (_workflowContext != null) _workflowContext.Changed -= WorkflowContext_Changed; _workflowContext = value; if (_workflowContext != null) _workflowContext.Changed += WorkflowContext_Changed; RefreshContextLabels(); }
        }

        public AlignStitchingControl() { InitializeComponent(); InitializeLogger(); alignConfigGrid.SelectedObject = _config; UpdateRunState(); }
        private void InitializeLogger() { _logger.Debug = true; _logger.SetOpenListBox(true, lstTab3Log); _logger.SetOpenFile(true, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Tab3"), "AlignStitch"); }
        private void WorkflowContext_Changed(object sender, EventArgs e) { RefreshContextLabels(); }

        private void RefreshContextLabels()
        {
            if (lblSampleRaster == null) return;
            lblSampleRaster.Text = _workflowContext == null || string.IsNullOrEmpty(_workflowContext.SampleRasterPath) ? "Source sample raster: -" : "Source sample raster: " + _workflowContext.SampleRasterPath;
            lblLastOutput.Text = _workflowContext == null || string.IsNullOrEmpty(_workflowContext.LastStitchedOutputPath) ? "Last stitched output: -" : "Last stitched output: " + _workflowContext.LastStitchedOutputPath;
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
                if (_workflowContext != null) _workflowContext.ManifestPath = path;
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
                txtOutputFolder.Text = dlg.SelectedPath; _config.OutputPath = dlg.SelectedPath; _logger.WriteInfo("Output root selected: " + dlg.SelectedPath); UpdateRunState();
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
                var progress = new Progress<WorkflowProgress>(p => { prgAlignStitch.Value = p.Total <= 0 ? 0 : Math.Min(100, p.Current * 100 / p.Total); if (p.Image != null) { p.Image.State = OrderNodeState.Processing; orderPathCanvas.SetCapturedImages(_capturedImages); } _logger.WriteInfo("OrderIndex " + (p.Image == null ? -1 : p.Image.OrderIndex) + " Stage " + p.Stage); });
                var result = await svc.RunAsync(_config, _manifest, _capturedImages, progress, _runCancellation.Token);
                foreach (var state in result.States) { var img = _capturedImages.FirstOrDefault(x => x.OrderIndex == state.OrderIndex); if (img != null) img.State = ToNodeState(state.Source); }
                orderPathCanvas.SetCapturedImages(_capturedImages);
                if (!result.Report.Succeeded) throw new InvalidOperationException("Alignment did not produce verified poses for every tile; stitching publication is blocked.");
            }
            catch (OperationCanceledException) { _logger.WriteWarning("Run cancelled; no manifest/report was published."); }
            catch (Exception ex) { _logger.WriteError("Run failed: " + ex); MessageBox.Show(this, ex.Message, "Align/Stitch failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { _runCancellation.Dispose(); _runCancellation = null; btnCancelAlignStitch.Enabled = false; UpdateRunState(); }
        }

        private void btnCancelAlignStitch_Click(object sender, EventArgs e) { if (_runCancellation != null) _runCancellation.Cancel(); }
        private void ClearManifestState() { _manifest = null; txtManifestPath.Text = string.Empty; txtManifestInfo.Clear(); _capturedImages = new List<CapturedImageInfo>(); lstCapturedImages.Items.Clear(); orderPathCanvas.SetCapturedImages(_capturedImages); }
        private void ShowBlocked(string title, IEnumerable<string> errors) { var message = string.Join(Environment.NewLine, errors); _logger.WriteWarning(title + ": " + message); MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        private void UpdateRunState() { btnRunAlignStitch.Enabled = _runCancellation == null && _manifest != null && _capturedImages.Count == (_manifest.Tiles == null ? -1 : _manifest.Tiles.Count) && Directory.Exists(txtOutputFolder.Text); }
        private static OrderNodeState ToNodeState(PoseSource source) { if (source == PoseSource.SampleAlignment) return OrderNodeState.SampleAlignOk; if (source == PoseSource.NeighborAlignment) return OrderNodeState.NeighborAlignOk; if (source == PoseSource.AnchorAdjusted) return OrderNodeState.AnchorAdjusted; if (source == PoseSource.Interpolated) return OrderNodeState.Interpolated; if (source == PoseSource.Manual) return OrderNodeState.Manual; if (source == PoseSource.Excluded) return OrderNodeState.Excluded; if (source == PoseSource.ExpectedGridOffset) return OrderNodeState.ExpectedGridOffset; return OrderNodeState.Failed; }
        private static string ResolveTilePath(string manifestFolder, string expectedPath) { return Path.IsPathRooted(expectedPath) ? expectedPath : Path.Combine(manifestFolder, expectedPath); }
        private static string CommonParent(IList<string> paths) { if (paths == null || paths.Count == 0) return string.Empty; var dirs = paths.Select(Path.GetDirectoryName).Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); return dirs.Count == 1 ? dirs[0] : string.Join("; ", dirs.Take(3).ToArray()); }
    }
}
