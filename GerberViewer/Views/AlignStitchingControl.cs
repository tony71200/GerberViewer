using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
        private bool _cancelAlignStitchRequested;

        public WorkflowContext WorkflowContext
        {
            get { return _workflowContext; }
            set { if (_workflowContext != null) _workflowContext.Changed -= WorkflowContext_Changed; _workflowContext = value; if (_workflowContext != null) _workflowContext.Changed += WorkflowContext_Changed; RefreshContextLabels(); }
        }

        public AlignStitchingControl() { InitializeComponent(); }
        private void WorkflowContext_Changed(object sender, EventArgs e) { RefreshContextLabels(); }

        private void RefreshContextLabels()
        {
            if (lblSampleRaster == null) return;
            lblSampleRaster.Text = _workflowContext == null || string.IsNullOrEmpty(_workflowContext.SampleRasterPath) ? "Sample raster: -" : "Sample raster: " + _workflowContext.SampleRasterPath;
            lblLastOutput.Text = _workflowContext == null || string.IsNullOrEmpty(_workflowContext.LastStitchedOutputPath) ? "Last stitched output: -" : "Last stitched output: " + _workflowContext.LastStitchedOutputPath;
        }

        private void btnOpenImageFolder_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select captured image folder";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                txtImageFolder.Text = dlg.SelectedPath;
                LoadCapturedImages();
            }
        }

        private void LoadCapturedImages()
        {
            lstCapturedImages.Items.Clear();
            var manifestPath = ResolveManifestPath();
            var result = _capturedImageLoader.Load(txtImageFolder.Text, manifestPath);
            if (!result.Succeeded)
            {
                lblImageCount.Text = "Images: blocked";
                MessageBox.Show(this, string.Join(Environment.NewLine, result.Errors), "Align/Stitch validation blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                orderPathCanvas.SetCapturedImages(new CapturedImageInfo[0]);
                return;
            }
            _capturedImages = result.Images;
            foreach (var image in _capturedImages) lstCapturedImages.Items.Add(string.Format("{0:000}: R{1} C{2} - {3}", image.OrderIndex + 1, image.Row, image.Column, Path.GetFileName(image.FilePath)));
            lblImageCount.Text = "Images: " + _capturedImages.Count + " / " + result.ExpectedTileCount;
            orderPathCanvas.SetCapturedImages(_capturedImages);
        }

        private string ResolveManifestPath()
        {
            if (_workflowContext != null)
            {
                if (!string.IsNullOrWhiteSpace(_workflowContext.ManifestPath)) return _workflowContext.ManifestPath;
                if (_workflowContext.AlignStitchConfig != null && !string.IsNullOrWhiteSpace(_workflowContext.AlignStitchConfig.InputManifestPath)) return _workflowContext.AlignStitchConfig.InputManifestPath;
            }
            return string.IsNullOrWhiteSpace(txtImageFolder.Text) ? null : Path.Combine(txtImageFolder.Text, "sample_manifest.json");
        }

        private void btnRunAlignStitch_Click(object sender, EventArgs e)
        {
            if (_capturedImages.Count == 0) { LoadCapturedImages(); if (_capturedImages.Count == 0) return; }
            _cancelAlignStitchRequested = false; btnRunAlignStitch.Enabled = false; btnCancelAlignStitch.Enabled = true; prgAlignStitch.Value = 0;
            for (int i = 0; i < _capturedImages.Count; i++)
            {
                if (_cancelAlignStitchRequested) { _capturedImages[i].State = OrderNodeState.Excluded; break; }
                _capturedImages[i].State = OrderNodeState.Processing; orderPathCanvas.SetCapturedImages(_capturedImages); Application.DoEvents();
                _capturedImages[i].State = OrderNodeState.ExpectedOffset;
                prgAlignStitch.Value = Math.Min(100, (i + 1) * 100 / _capturedImages.Count);
            }
            orderPathCanvas.SetCapturedImages(_capturedImages); btnRunAlignStitch.Enabled = true; btnCancelAlignStitch.Enabled = false;
        }

        private void btnCancelAlignStitch_Click(object sender, EventArgs e) { _cancelAlignStitchRequested = true; }
    }
}
