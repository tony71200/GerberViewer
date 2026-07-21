namespace GerberViewer.Views
{
    partial class CreateGerberSampleControl
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }
        private void InitializeComponent()
        {
            this.btnOpenSample = new System.Windows.Forms.Button();
            this.txtSamplePath = new System.Windows.Forms.TextBox();
            this.btnLoadSampleConfig = new System.Windows.Forms.Button();
            this.btnCreateSample = new System.Windows.Forms.Button();
            this.btnCancelCreateSample = new System.Windows.Forms.Button();
            this.prgCreateSample = new System.Windows.Forms.ProgressBar();
            this.lblCreateSampleStatus = new System.Windows.Forms.Label();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.sampleWindow = new EWindowControl.EWindowControl();
            this.sampleConfigGrid = new System.Windows.Forms.PropertyGrid();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout(); this.splitContainer.Panel2.SuspendLayout(); this.splitContainer.SuspendLayout(); this.SuspendLayout();
            this.btnOpenSample.Location = new System.Drawing.Point(12, 12); this.btnOpenSample.Name = "btnOpenSample"; this.btnOpenSample.Size = new System.Drawing.Size(110, 28); this.btnOpenSample.Text = "Open Sample"; this.btnOpenSample.UseVisualStyleBackColor = true; this.btnOpenSample.Click += new System.EventHandler(this.btnOpenSample_Click);
            this.txtSamplePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); this.txtSamplePath.Location = new System.Drawing.Point(128, 16); this.txtSamplePath.Name = "txtSamplePath"; this.txtSamplePath.ReadOnly = true; this.txtSamplePath.Size = new System.Drawing.Size(344, 20);
            this.btnLoadSampleConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right))); this.btnLoadSampleConfig.Location = new System.Drawing.Point(478, 12); this.btnLoadSampleConfig.Name = "btnLoadSampleConfig"; this.btnLoadSampleConfig.Size = new System.Drawing.Size(120, 28); this.btnLoadSampleConfig.Text = "Load Config"; this.btnLoadSampleConfig.UseVisualStyleBackColor = true; this.btnLoadSampleConfig.Click += new System.EventHandler(this.btnLoadSampleConfig_Click);
            this.btnCreateSample.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right))); this.btnCreateSample.Location = new System.Drawing.Point(604, 12); this.btnCreateSample.Name = "btnCreateSample"; this.btnCreateSample.Size = new System.Drawing.Size(95, 28); this.btnCreateSample.Text = "Create"; this.btnCreateSample.UseVisualStyleBackColor = true; this.btnCreateSample.Click += new System.EventHandler(this.btnCreateSample_Click);
            this.btnCancelCreateSample.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right))); this.btnCancelCreateSample.Enabled = false; this.btnCancelCreateSample.Location = new System.Drawing.Point(705, 12); this.btnCancelCreateSample.Name = "btnCancelCreateSample"; this.btnCancelCreateSample.Size = new System.Drawing.Size(83, 28); this.btnCancelCreateSample.Text = "Cancel"; this.btnCancelCreateSample.UseVisualStyleBackColor = true; this.btnCancelCreateSample.Click += new System.EventHandler(this.btnCancelCreateSample_Click);
            this.prgCreateSample.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); this.prgCreateSample.Location = new System.Drawing.Point(12, 46); this.prgCreateSample.Name = "prgCreateSample"; this.prgCreateSample.Size = new System.Drawing.Size(586, 18);
            this.lblCreateSampleStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); this.lblCreateSampleStatus.AutoSize = true; this.lblCreateSampleStatus.Location = new System.Drawing.Point(604, 49); this.lblCreateSampleStatus.Name = "lblCreateSampleStatus"; this.lblCreateSampleStatus.Size = new System.Drawing.Size(38, 13); this.lblCreateSampleStatus.Text = "Ready";
            this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); this.splitContainer.Location = new System.Drawing.Point(12, 70); this.splitContainer.Name = "splitContainer"; this.splitContainer.Size = new System.Drawing.Size(776, 418); this.splitContainer.SplitterDistance = 500;
            this.sampleWindow.Dock = System.Windows.Forms.DockStyle.Fill; this.sampleWindow.Name = "sampleWindow";
            this.sampleConfigGrid.Dock = System.Windows.Forms.DockStyle.Fill; this.sampleConfigGrid.Name = "sampleConfigGrid";
            this.splitContainer.Panel1.Controls.Add(this.sampleWindow); this.splitContainer.Panel2.Controls.Add(this.sampleConfigGrid);
            this.Controls.Add(this.splitContainer); this.Controls.Add(this.lblCreateSampleStatus); this.Controls.Add(this.prgCreateSample); this.Controls.Add(this.btnCancelCreateSample); this.Controls.Add(this.btnCreateSample); this.Controls.Add(this.btnLoadSampleConfig); this.Controls.Add(this.txtSamplePath); this.Controls.Add(this.btnOpenSample); this.Name = "CreateGerberSampleControl"; this.Size = new System.Drawing.Size(800, 500);
            this.splitContainer.Panel1.ResumeLayout(false); this.splitContainer.Panel2.ResumeLayout(false); ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit(); this.splitContainer.ResumeLayout(false); this.ResumeLayout(false); this.PerformLayout();
        }
        private System.Windows.Forms.Button btnOpenSample; private System.Windows.Forms.TextBox txtSamplePath; private System.Windows.Forms.Button btnLoadSampleConfig; private System.Windows.Forms.Button btnCreateSample; private System.Windows.Forms.Button btnCancelCreateSample; private System.Windows.Forms.ProgressBar prgCreateSample; private System.Windows.Forms.Label lblCreateSampleStatus; private System.Windows.Forms.SplitContainer splitContainer; private EWindowControl.EWindowControl sampleWindow; private System.Windows.Forms.PropertyGrid sampleConfigGrid;
    }
}
