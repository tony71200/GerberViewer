using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using GerberViewer.Stitching.Configuration;
using GerberViewer.Stitching.Imaging;
using GerberViewer.Stitching.Utils;
using GerberViewer.Workflow.Models;

namespace GerberViewer.Views
{
    public partial class CreateGerberSampleControl : UserControl
    {
        private GerberSampleConfig _sampleConfig = new GerberSampleConfig();
        private readonly SampleConfigStore _configStore = new SampleConfigStore();
        private readonly Dictionary<int, SampleTileState> _tileStates = new Dictionary<int, SampleTileState>();
        private CancellationTokenSource _createSampleCts;
        private Bitmap _sampleSource;
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

        private void btnOpenSample_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open external sample raster";
                dlg.Filter = "Raster images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All files|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var decoded = ImageRead.ReadBitmap(dlg.FileName);
                    ReplaceSampleImage(decoded, dlg.FileName);
                    CommitWorkflowContext();
                    RebuildGridOverlay();
                    sampleConfigGrid.Refresh();
                    lblCreateSampleStatus.Text = "Sample loaded and grid rendered";
                }
                catch (Exception ex) { lblCreateSampleStatus.Text = "Sample load failed"; MessageBox.Show(this, ex.ToString(), "Open sample failed"); }
            }
        }

        private void btnLoadSampleConfig_Click(object sender, EventArgs e)
        {
            try
            {
                var loaded = _configStore.LoadOrCreateDefault();
                if (!string.IsNullOrWhiteSpace(_sampleConfig.SourceRasterPath) && string.IsNullOrWhiteSpace(loaded.SourceRasterPath)) loaded.SourceRasterPath = _sampleConfig.SourceRasterPath;
                CommitConfig(loaded, "Config loaded: " + _configStore.ConfigPath);
                if (_sampleSource != null) RebuildGridOverlay();
            }
            catch (InvalidDataException ex) { CommitConfig(_configStore.Load(), "Warning: invalid config replaced. " + ex.Message); if (_sampleSource != null) RebuildGridOverlay(); }
            catch (Exception ex) { lblCreateSampleStatus.Text = "Load Config failed"; MessageBox.Show(this, ex.ToString(), "Load Config failed"); }
        }

        private void btnSaveConfig_Click(object sender, EventArgs e)
        {
            try
            {
                sampleConfigGrid.Refresh();
                var validation = GerberSampleConfigValidator.Validate(_sampleConfig, _sampleSource == null ? Size.Empty : _sampleSource.Size);
                if (!validation.IsValid) { MessageBox.Show(this, string.Join(Environment.NewLine, validation.Errors), "Config validation failed"); return; }
                _configStore.Save(_sampleConfig);
                CommitConfig(_configStore.Load(), "Config saved: " + _configStore.ConfigPath);
                if (_sampleSource != null) RebuildGridOverlay();
            }
            catch (Exception ex) { lblCreateSampleStatus.Text = "Save Config failed"; MessageBox.Show(this, ex.ToString(), "Save Config failed"); }
        }

        private async void btnCreateSample_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_sampleConfig.SourceRasterPath) || !File.Exists(_sampleConfig.SourceRasterPath)) { MessageBox.Show(this, "Please select a sample raster first."); return; }
            using (var dlg = new FolderBrowserDialog()) { dlg.Description = "Select sample output directory"; if (dlg.ShowDialog(this) != DialogResult.OK) return; _sampleConfig.OutputDirectory = dlg.SelectedPath; }
            SetRunUiState(true); prgCreateSample.Value = 0; _createSampleCts = new CancellationTokenSource();
            var progress = new Progress<SampleCropProgress>(p => { prgCreateSample.Maximum = Math.Max(1, p.Total); prgCreateSample.Value = Math.Min(prgCreateSample.Maximum, p.Completed); lblCreateSampleStatus.Text = p.Message; if (_currentLayout != null && p.Completed > 0) { _tileStates[p.Completed - 1] = SampleTileState.Completed; RenderGridOverlay(); } });
            try
            {
                var result = await new SampleTileGenerator().GenerateAsync(_sampleConfig, _createSampleCts.Token, progress);
                lblCreateSampleStatus.Text = "Completed";
                if (WorkflowContext != null) { WorkflowContext.OutputDirectory = result.OutputDirectory; WorkflowContext.ManifestPath = result.ManifestPath; WorkflowContext.NotifyChanged(); }
            }
            catch (OperationCanceledException) { lblCreateSampleStatus.Text = "Cancelled - incomplete output marked"; }
            catch (Exception ex) { lblCreateSampleStatus.Text = "Failed - incomplete output marked"; MessageBox.Show(this, ex.ToString(), "Create sample failed"); }
            finally { SetRunUiState(false); if (_createSampleCts != null) { _createSampleCts.Dispose(); _createSampleCts = null; } }
        }

        private void btnCancelCreateSample_Click(object sender, EventArgs e) { if (_createSampleCts != null) _createSampleCts.Cancel(); }

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
            WorkflowContext.SampleConfig = _sampleConfig;
            WorkflowContext.NotifyChanged();
        }
        private void ReplaceSampleImage(Bitmap decoded, string path)
        {
            var old = _sampleSource;
            _sampleSource = decoded;
            _sampleConfig.SourceRasterPath = path;
            txtSamplePath.Text = path;
            if (old != null) old.Dispose();
            sampleWindow.SetSourceBitmap((Bitmap)_sampleSource.Clone(), true);
        }
        private void RebuildGridOverlay()
        {
            var validation = GerberSampleConfigValidator.Validate(_sampleConfig, _sampleSource.Size);
            if (!validation.IsValid) throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));
            _currentLayout = SampleGeometryCalculator.Calculate(_sampleSource.Width, _sampleSource.Height, _sampleConfig);
            _tileStates.Clear(); foreach (var t in _currentLayout.Tiles) _tileStates[t.OrderIndex] = SampleTileState.Pending;
            RenderGridOverlay();
        }
        private void RenderGridOverlay()
        {
            if (_sampleSource == null) return;
            var bmp = (Bitmap)_sampleSource.Clone();
            using (var g = Graphics.FromImage(bmp))
            using (var font = new Font(FontFamily.GenericSansSerif, Math.Max(10, Math.Min(24, bmp.Width / 80))))
            using (var red = new Pen(Color.Red, 2)) using (var green = new Pen(Color.LimeGreen, 2)) using (var yellow = new Pen(Color.Gold, 2))
            {
                if (_currentLayout != null) foreach (var tile in _currentLayout.Tiles.OrderBy(t => t.OrderIndex))
                {
                    var state = _tileStates.ContainsKey(tile.OrderIndex) ? _tileStates[tile.OrderIndex] : SampleTileState.Pending;
                    var pen = state == SampleTileState.Completed ? green : state == SampleTileState.Processing ? yellow : red;
                    g.DrawRectangle(pen, tile.Rectangle);
                    g.DrawString(tile.OrderIndex.ToString(), font, Brushes.Red, tile.Rectangle.Left + 3, tile.Rectangle.Top + 3);
                }
            }
            sampleWindow.SetSourceBitmap(bmp, false);
        }
        private void SetRunUiState(bool running)
        {
            btnOpenSample.Enabled = !running; btnLoadSampleConfig.Enabled = !running; btnSaveConfig.Enabled = !running; btnCreateSample.Enabled = !running; btnCancelCreateSample.Enabled = running;
        }
    }
}
