using System;
using System.IO;
using System.Windows.Forms;
using GerberViewer.Workflow.Models;

namespace GerberViewer.Views
{
    public partial class CreateGerberSampleControl : UserControl
    {
        public WorkflowContext WorkflowContext { get; set; }

        public CreateGerberSampleControl()
        {
            InitializeComponent();
        }

        private void btnOpenSample_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Open external sample raster";
                dlg.Filter = "Raster images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All files|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                if (WorkflowContext != null)
                {
                    WorkflowContext.SampleRasterPath = dlg.FileName;
                    WorkflowContext.SampleConfig.SourceRasterPath = dlg.FileName;
                    WorkflowContext.NotifyChanged();
                }

                lblSamplePath.Text = dlg.FileName;
            }
        }
    }
}
