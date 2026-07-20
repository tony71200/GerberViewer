using System;
using System.Windows.Forms;
using GerberViewer.Workflow.Models;

namespace GerberViewer.Views
{
    public partial class AlignStitchingControl : UserControl
    {
        private WorkflowContext _workflowContext;

        public WorkflowContext WorkflowContext
        {
            get { return _workflowContext; }
            set
            {
                if (_workflowContext != null) _workflowContext.Changed -= WorkflowContext_Changed;
                _workflowContext = value;
                if (_workflowContext != null) _workflowContext.Changed += WorkflowContext_Changed;
                RefreshContextLabels();
            }
        }

        public AlignStitchingControl()
        {
            InitializeComponent();
        }

        private void WorkflowContext_Changed(object sender, EventArgs e)
        {
            RefreshContextLabels();
        }

        private void RefreshContextLabels()
        {
            if (lblSampleRaster == null) return;
            lblSampleRaster.Text = _workflowContext == null || string.IsNullOrEmpty(_workflowContext.SampleRasterPath)
                ? "Sample raster: -"
                : "Sample raster: " + _workflowContext.SampleRasterPath;
            lblLastOutput.Text = _workflowContext == null || string.IsNullOrEmpty(_workflowContext.LastStitchedOutputPath)
                ? "Last stitched output: -"
                : "Last stitched output: " + _workflowContext.LastStitchedOutputPath;
        }
    }
}
