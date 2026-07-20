namespace GerberViewer.Views
{
    partial class CreateGerberSampleControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnOpenSample = new System.Windows.Forms.Button();
            this.lblInfo = new System.Windows.Forms.Label();
            this.lblSamplePath = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.btnOpenSample.Location = new System.Drawing.Point(24, 24);
            this.btnOpenSample.Name = "btnOpenSample";
            this.btnOpenSample.Size = new System.Drawing.Size(130, 32);
            this.btnOpenSample.Text = "Open Sample";
            this.btnOpenSample.UseVisualStyleBackColor = true;
            this.btnOpenSample.Click += new System.EventHandler(this.btnOpenSample_Click);
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(24, 72);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Text = "Version 1 accepts only an external raster selected with Open Sample.";
            this.lblSamplePath.AutoSize = true;
            this.lblSamplePath.Location = new System.Drawing.Point(24, 104);
            this.lblSamplePath.Name = "lblSamplePath";
            this.lblSamplePath.Text = "No sample selected";
            this.Controls.Add(this.lblSamplePath);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnOpenSample);
            this.Name = "CreateGerberSampleControl";
            this.Size = new System.Drawing.Size(800, 500);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnOpenSample;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Label lblSamplePath;
    }
}
