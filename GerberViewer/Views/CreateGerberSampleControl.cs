using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using GerberViewer.Stitching.Configuration;
using GerberViewer.Stitching.Imaging;
using GerberViewer.Workflow.Models;

namespace GerberViewer.Views
{
    public partial class CreateGerberSampleControl : UserControl
    {
        private readonly GerberSampleConfig _sampleConfig = new GerberSampleConfig();
        private CancellationTokenSource _createSampleCts;
        public WorkflowContext WorkflowContext { get; set; }

        public CreateGerberSampleControl()
        {
            InitializeComponent();
            sampleConfigGrid.SelectedObject = _sampleConfig;
        }

        private void btnOpenSample_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open external sample raster";
                dlg.Filter = "Raster images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All files|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _sampleConfig.SourceRasterPath = dlg.FileName;
                txtSamplePath.Text = dlg.FileName;
                if (WorkflowContext != null)
                {
                    WorkflowContext.SampleRasterPath = dlg.FileName;
                    WorkflowContext.SampleConfig.SourceRasterPath = dlg.FileName;
                    WorkflowContext.NotifyChanged();
                }
                sampleConfigGrid.Refresh();
                lblCreateSampleStatus.Text = "Sample selected";
            }
        }

        private void btnLoadSampleConfig_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_sampleConfig.SourceRasterPath) && WorkflowContext != null) _sampleConfig.SourceRasterPath = WorkflowContext.SampleRasterPath;
            sampleConfigGrid.SelectedObject = _sampleConfig;
            sampleConfigGrid.Refresh();
            lblCreateSampleStatus.Text = "Config loaded";
        }

        private async void btnCreateSample_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_sampleConfig.SourceRasterPath) || !File.Exists(_sampleConfig.SourceRasterPath)) { MessageBox.Show(this, "Please select a sample raster first."); return; }
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select sample output directory";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _sampleConfig.OutputDirectory = dlg.SelectedPath;
            }
            btnCreateSample.Enabled = false; btnCancelCreateSample.Enabled = true; prgCreateSample.Value = 0;
            _createSampleCts = new CancellationTokenSource();
            var progress = new Progress<SampleCropProgress>(p => { prgCreateSample.Maximum = Math.Max(1, p.Total); prgCreateSample.Value = Math.Min(prgCreateSample.Maximum, p.Completed); lblCreateSampleStatus.Text = p.Message; });
            try
            {
                var result = await new SampleTileGenerator().GenerateAsync(_sampleConfig, _createSampleCts.Token, progress);
                lblCreateSampleStatus.Text = "Completed";
                if (WorkflowContext != null) { WorkflowContext.OutputDirectory = result.OutputDirectory; WorkflowContext.ManifestPath = result.ManifestPath; WorkflowContext.NotifyChanged(); }
            }
            catch (OperationCanceledException) { lblCreateSampleStatus.Text = "Cancelled - incomplete output marked"; }
            catch (Exception ex) { lblCreateSampleStatus.Text = "Failed - incomplete output marked"; MessageBox.Show(this, ex.Message, "Create sample failed"); }
            finally { btnCreateSample.Enabled = true; btnCancelCreateSample.Enabled = false; if (_createSampleCts != null) { _createSampleCts.Dispose(); _createSampleCts = null; } }
        }

        private void btnCancelCreateSample_Click(object sender, EventArgs e)
        {
            if (_createSampleCts != null) _createSampleCts.Cancel();
        }
    }
}
