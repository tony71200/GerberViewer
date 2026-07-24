using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using HalconDotNet;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GerberViewer.Stitching.Configuration;
using GerberViewer.Stitching.Imaging;
using GerberViewer.Workflow.Models;

namespace GerberViewer.Views
{
    public partial class CreateGerberSampleControl : UserControl
    {
        private GerberSampleConfig _sampleConfig = new GerberSampleConfig();
        private readonly SampleConfigStore _configStore = new SampleConfigStore();
        private readonly Dictionary<int, SampleTileState> _tileStates = new Dictionary<int, SampleTileState>();
        private CancellationTokenSource _createSampleCts;
        private HObject _sampleSourceImage;
        private Size _sampleSourceSize;
        private PreparedSampleRun _preparedRun;
        private SampleGridLayout _currentLayout;
        public WorkflowContext WorkflowContext { get; set; }

        public CreateGerberSampleControl()
        {
            InitializeComponent();
            LoadConfigOnStartup();
        }

        private void LoadConfigOnStartup()
        {
            try { CommitConfig(_configStore.LoadOrCreateDefault(), "Ready - config: " + _configStore.ConfigPath); }
            catch (InvalidDataException ex) { CommitConfig(_configStore.Load(), "Warning: invalid config replaced. " + ex.Message); }
            catch (Exception ex) { lblCreateSampleStatus.Text = "Config load failed"; MessageBox.Show(this, ex.ToString(), "Config load failed"); CommitConfig(new GerberSampleConfig(), "Ready - default config in memory"); }
        }

        private async void btnOpenSample_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open external sample raster";
                dlg.Filter = "Raster images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All files|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    SetRunUiState(true);
                    BeginCreateSampleLoading("Loading sample image...");
                    HObject decoded = null;
                    var selectedPath = dlg.FileName;
                    await Task.Run(() => HOperatorSet.ReadImage(out decoded, selectedPath));
                    ReplaceSampleImage(decoded, selectedPath);
                    CommitWorkflowContext();
                    await PrepareCurrentSampleAsync(true);
                    sampleConfigGrid.Refresh();
                    SetPreparedStatus("Sample loaded and grid rendered");
                }
                catch (Exception ex) { lblCreateSampleStatus.Text = "Sample load failed"; MessageBox.Show(this, ex.ToString(), "Open sample failed"); }
                finally { EndCreateSampleLoading(); SetRunUiState(false); }
            }
        }

        private async void btnLoadSampleConfig_Click(object sender, EventArgs e)
        {
            try
            {
                var loaded = _configStore.LoadOrCreateDefault();
                if (!string.IsNullOrWhiteSpace(_sampleConfig.SourceRasterPath) && string.IsNullOrWhiteSpace(loaded.SourceRasterPath)) loaded.SourceRasterPath = _sampleConfig.SourceRasterPath;
                CommitConfig(loaded, "Config loaded: " + _configStore.ConfigPath);
                if (_sampleSourceImage != null) await PrepareCurrentSampleAsync(false);
            }
            catch (InvalidDataException ex) { CommitConfig(_configStore.Load(), "Warning: invalid config replaced. " + ex.Message); if (_sampleSourceImage != null) await PrepareCurrentSampleAsync(false); }
            catch (Exception ex) { lblCreateSampleStatus.Text = "Load Config failed"; MessageBox.Show(this, ex.ToString(), "Load Config failed"); }
        }

        private async void btnSaveConfig_Click(object sender, EventArgs e)
        {
            try
            {
                sampleConfigGrid.Refresh();
                var validation = GerberSampleConfigValidator.Validate(_sampleConfig, _sampleSourceImage == null ? Size.Empty : _sampleSourceSize);
                if (!validation.IsValid) { MessageBox.Show(this, string.Join(Environment.NewLine, validation.Errors), "Config validation failed"); return; }
                _configStore.Save(_sampleConfig);
                CommitConfig(_configStore.Load(), "Config saved: " + _configStore.ConfigPath);
                if (_sampleSourceImage != null) await PrepareCurrentSampleAsync(false);
            }
            catch (Exception ex) { lblCreateSampleStatus.Text = "Save Config failed"; MessageBox.Show(this, ex.ToString(), "Save Config failed"); }
        }

        private async void btnRefreshPreview_Click(object sender, EventArgs e)
        {
            try
            {
                if (_sampleSourceImage == null || !_sampleSourceImage.IsInitialized())
                {
                    MessageBox.Show(this, "Please open a sample image before refreshing the preview.", "Refresh preview");
                    return;
                }
                sampleConfigGrid.Refresh();
                await PrepareCurrentSampleAsync(false);
                SetPreparedStatus("Preview refreshed");
            }
            catch (Exception ex)
            {
                lblCreateSampleStatus.Text = "Refresh preview failed";
                MessageBox.Show(this, ex.ToString(), "Refresh preview failed");
            }
        }

        private async void btnCreateSample_Click(object sender, EventArgs e)
        {
            if (_preparedRun == null || string.IsNullOrWhiteSpace(_sampleConfig.SourceRasterPath) || !File.Exists(_sampleConfig.SourceRasterPath)) { MessageBox.Show(this, "Please select a sample raster first and refresh preview."); return; }
            using (var dlg = new FolderBrowserDialog()) { dlg.Description = "Select sample output root directory"; if (dlg.ShowDialog(this) != DialogResult.OK) return; _sampleConfig.OutputDirectory = dlg.SelectedPath; }
            if (!ConfirmOutputRoot(_sampleConfig.OutputDirectory)) return;
            SetRunUiState(true); prgCreateSample.Style = ProgressBarStyle.Blocks; prgCreateSample.MarqueeAnimationSpeed = 0; prgCreateSample.Value = 0; _createSampleCts = new CancellationTokenSource();
            var progress = new Progress<SampleCropProgress>(p => { prgCreateSample.Maximum = Math.Max(1, p.Total); prgCreateSample.Value = Math.Min(prgCreateSample.Maximum, p.Completed); lblCreateSampleStatus.Text = p.Message; if (_currentLayout != null) { _tileStates[p.OrderIndex] = p.State; RenderGridOverlay(); } });
            try
            {
                var result = await new SampleTileGenerator().GenerateAsync(_preparedRun, _sampleConfig.OutputDirectory, _createSampleCts.Token, progress);
                lblCreateSampleStatus.Text = "Completed";
                if (WorkflowContext != null) { WorkflowContext.OutputDirectory = result.OutputDirectory; WorkflowContext.ManifestPath = result.ManifestPath; WorkflowContext.NotifyChanged(); }
            }
            catch (OperationCanceledException) { lblCreateSampleStatus.Text = "Cancelled - no manifest published"; }
            catch (Exception ex) { lblCreateSampleStatus.Text = "Failed - no manifest published"; MessageBox.Show(this, ex.ToString(), "Create sample failed"); }
            finally { SetRunUiState(false); if (_createSampleCts != null) { _createSampleCts.Dispose(); _createSampleCts = null; } }
        }

        private void btnCancelCreateSample_Click(object sender, EventArgs e) { if (_createSampleCts != null) _createSampleCts.Cancel(); }

        private void BeginCreateSampleLoading(string message)
        {
            prgCreateSample.Style = ProgressBarStyle.Marquee;
            prgCreateSample.MarqueeAnimationSpeed = 30;
            lblCreateSampleStatus.Text = message;
        }

        private void EndCreateSampleLoading()
        {
            prgCreateSample.Style = ProgressBarStyle.Blocks;
            prgCreateSample.MarqueeAnimationSpeed = 0;
            prgCreateSample.Value = 0;
        }

        private void CommitConfig(GerberSampleConfig config, string status)
        {
            _sampleConfig = config ?? new GerberSampleConfig();
            sampleConfigGrid.SelectedObject = _sampleConfig;
            txtSamplePath.Text = _sampleConfig.SourceRasterPath ?? string.Empty;
            CommitWorkflowContext();
            lblCreateSampleStatus.Text = status;
        }
        private void CommitWorkflowContext()
        {
            if (WorkflowContext == null) return;
            WorkflowContext.SampleRasterPath = _sampleConfig.SourceRasterPath;
            if (WorkflowContext.SampleConfig == null) WorkflowContext.SampleConfig = new SampleGerberConfig();
            WorkflowContext.SampleConfig.SourceRasterPath = _sampleConfig.SourceRasterPath;
            WorkflowContext.NotifyChanged();
        }
        private void ReplaceSampleImage(HObject decoded, string path)
        {
            if (decoded == null || !decoded.IsInitialized()) throw new InvalidOperationException("HALCON did not return a valid sample image: " + path);
            HObject old = _sampleSourceImage;
            _sampleSourceImage = decoded;
            HTuple width = null;
            HTuple height = null;
            try
            {
                HOperatorSet.GetImageSize(_sampleSourceImage, out width, out height);
                _sampleSourceSize = new Size(width.I, height.I);
            }
            finally
            {
                if (width != null) width.Dispose();
                if (height != null) height.Dispose();
            }
            _sampleConfig.SourceRasterPath = path;
            txtSamplePath.Text = path;
            if (old != null && old.IsInitialized()) old.Dispose();
            sampleWindow.SetSourceImage(_sampleSourceImage, true);
        }
        private async Task PrepareCurrentSampleAsync(bool fit)
        {
            var validation = GerberSampleConfigValidator.Validate(_sampleConfig, _sampleSourceSize);
            if (!validation.IsValid) throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));
            var source = _sampleSourceImage;
            var config = _sampleConfig;
            var prepared = await Task.Run(() => new SamplePreparationService().Prepare(source, config, CancellationToken.None));
            var old = _preparedRun;
            _preparedRun = prepared;
            _currentLayout = prepared.Layout;
            if (old != null) old.Dispose();
            _tileStates.Clear(); foreach (var t in _currentLayout.Tiles) _tileStates[t.OrderIndex] = SampleTileState.Pending;
            LogLayoutWarnings(_currentLayout);
            sampleWindow.SetSourceImage(_preparedRun.ProcessedImage, fit);
            RenderGridOverlay();
        }
        private void LogLayoutWarnings(SampleGridLayout layout)
        {
            if (layout == null || layout.Warnings == null || layout.Warnings.Count == 0) return;
            var message = string.Join(Environment.NewLine, layout.Warnings.Select(w => "Sample grid warning: " + w).ToArray());
            Trace.TraceWarning(message);
            lblCreateSampleStatus.Text = BuildPreparedStatus("Preview prepared");
        }

        private void SetPreparedStatus(string baseStatus)
        {
            lblCreateSampleStatus.Text = BuildPreparedStatus(baseStatus);
        }

        private string BuildPreparedStatus(string baseStatus)
        {
            if (_currentLayout == null || _currentLayout.Warnings == null || _currentLayout.Warnings.Count == 0) return baseStatus;
            return baseStatus + " - warnings: " + string.Join("; ", _currentLayout.Warnings.ToArray());
        }

        private void RenderGridOverlay()
        {
            if (_sampleSourceImage == null || !_sampleSourceImage.IsInitialized()) return;
            if (_currentLayout == null)
            {
                sampleWindow.SetSourceImage(_sampleSourceImage, false);
                return;
            }
            var overlays = _currentLayout.Tiles.OrderBy(t => t.OrderIndex).Select(tile =>
            {
                var state = _tileStates.ContainsKey(tile.OrderIndex) ? _tileStates[tile.OrderIndex] : SampleTileState.Pending;
                var color = state == SampleTileState.Completed ? "green" : state == SampleTileState.Processing ? "yellow" : "red";
                return Tuple.Create(tile.Rectangle, color, tile.OrderIndex.ToString());
            }).ToList();
            sampleWindow.RenderImageOverlay(overlays);
        }
        private void DisposeSampleSource()
        {
            if (_preparedRun != null) { _preparedRun.Dispose(); _preparedRun = null; }
            if (_sampleSourceImage != null && _sampleSourceImage.IsInitialized()) _sampleSourceImage.Dispose();
            _sampleSourceImage = null;
            _sampleSourceSize = Size.Empty;
        }

        private bool ConfirmOutputRoot(string outputRoot)
        {
            var root = Path.GetFullPath(outputRoot);
            if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
            {
                var answer = MessageBox.Show(this, "The selected output root is not empty. GerberViewer will create a new GerberSample_<runId> folder and will not delete the selected root.\n\nPath: " + root + "\n\nContinue?", "Confirm sample output", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return answer == DialogResult.OK;
            }
            return true;
        }

        private void SetRunUiState(bool running)
        {
            btnOpenSample.Enabled = !running; btnLoadSampleConfig.Enabled = !running; btnSaveConfig.Enabled = !running; btnRefreshPreview.Enabled = !running; btnCreateSample.Enabled = !running; btnCancelCreateSample.Enabled = running;
        }
    }
}
