namespace GerberViewer.Views
{
    partial class AlignStitchingControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSampleRaster = new System.Windows.Forms.Label();
            this.lblLastOutput = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(24, 24);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Align and Stitching workflow";
            this.lblSampleRaster.AutoSize = true;
            this.lblSampleRaster.Location = new System.Drawing.Point(24, 64);
            this.lblSampleRaster.Name = "lblSampleRaster";
            this.lblSampleRaster.Text = "Sample raster: -";
            this.lblLastOutput.AutoSize = true;
            this.lblLastOutput.Location = new System.Drawing.Point(24, 96);
            this.lblLastOutput.Name = "lblLastOutput";
            this.lblLastOutput.Text = "Last stitched output: -";
            this.Controls.Add(this.lblLastOutput);
            this.Controls.Add(this.lblSampleRaster);
            this.Controls.Add(this.lblTitle);
            this.Name = "AlignStitchingControl";
            this.Size = new System.Drawing.Size(800, 500);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSampleRaster;
        private System.Windows.Forms.Label lblLastOutput;
    }
}
