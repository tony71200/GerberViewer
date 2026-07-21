namespace GerberViewer.Views
{
    partial class CreateGerberSampleControl
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }
        private void InitializeComponent()
        {
            this.commandLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnOpenSample = new System.Windows.Forms.Button();
            this.txtSamplePath = new System.Windows.Forms.TextBox();
            this.btnLoadSampleConfig = new System.Windows.Forms.Button();
            this.btnSaveConfig = new System.Windows.Forms.Button();
            this.btnCreateSample = new System.Windows.Forms.Button();
            this.btnCancelCreateSample = new System.Windows.Forms.Button();
            this.prgCreateSample = new System.Windows.Forms.ProgressBar();
            this.lblCreateSampleStatus = new System.Windows.Forms.Label();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.sampleWindow = new EWindowControl.EWindowControl();
            this.sampleConfigGrid = new System.Windows.Forms.PropertyGrid();
            this.commandLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            this.commandLayout.ColumnCount = 6;
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 116F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 84F));
            this.commandLayout.Controls.Add(this.btnOpenSample, 0, 0);
            this.commandLayout.Controls.Add(this.txtSamplePath, 1, 0);
            this.commandLayout.Controls.Add(this.btnLoadSampleConfig, 2, 0);
            this.commandLayout.Controls.Add(this.btnSaveConfig, 3, 0);
            this.commandLayout.Controls.Add(this.btnCreateSample, 4, 0);
            this.commandLayout.Controls.Add(this.btnCancelCreateSample, 5, 0);
            this.commandLayout.Controls.Add(this.prgCreateSample, 0, 1);
            this.commandLayout.Controls.Add(this.lblCreateSampleStatus, 4, 1);
            this.commandLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.commandLayout.Location = new System.Drawing.Point(0, 0);
            this.commandLayout.Name = "commandLayout";
            this.commandLayout.RowCount = 2;
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.commandLayout.Size = new System.Drawing.Size(800, 66);
            this.btnOpenSample.Dock = System.Windows.Forms.DockStyle.Fill; this.btnOpenSample.Name = "btnOpenSample"; this.btnOpenSample.Text = "Open Sample"; this.btnOpenSample.UseVisualStyleBackColor = true; this.btnOpenSample.Click += new System.EventHandler(this.btnOpenSample_Click);
            this.txtSamplePath.Dock = System.Windows.Forms.DockStyle.Fill; this.txtSamplePath.Name = "txtSamplePath"; this.txtSamplePath.ReadOnly = true; this.txtSamplePath.Margin = new System.Windows.Forms.Padding(3, 9, 3, 3);
            this.btnLoadSampleConfig.Dock = System.Windows.Forms.DockStyle.Fill; this.btnLoadSampleConfig.Name = "btnLoadSampleConfig"; this.btnLoadSampleConfig.Text = "Load Config"; this.btnLoadSampleConfig.UseVisualStyleBackColor = true; this.btnLoadSampleConfig.Click += new System.EventHandler(this.btnLoadSampleConfig_Click);
            this.btnSaveConfig.Dock = System.Windows.Forms.DockStyle.Fill; this.btnSaveConfig.Name = "btnSaveConfig"; this.btnSaveConfig.Text = "Save Config"; this.btnSaveConfig.UseVisualStyleBackColor = true; this.btnSaveConfig.Click += new System.EventHandler(this.btnSaveConfig_Click);
            this.btnCreateSample.Dock = System.Windows.Forms.DockStyle.Fill; this.btnCreateSample.Name = "btnCreateSample"; this.btnCreateSample.Text = "Create"; this.btnCreateSample.UseVisualStyleBackColor = true; this.btnCreateSample.Click += new System.EventHandler(this.btnCreateSample_Click);
            this.btnCancelCreateSample.Dock = System.Windows.Forms.DockStyle.Fill; this.btnCancelCreateSample.Enabled = false; this.btnCancelCreateSample.Name = "btnCancelCreateSample"; this.btnCancelCreateSample.Text = "Cancel"; this.btnCancelCreateSample.UseVisualStyleBackColor = true; this.btnCancelCreateSample.Click += new System.EventHandler(this.btnCancelCreateSample_Click);
            this.commandLayout.SetColumnSpan(this.prgCreateSample, 4); this.prgCreateSample.Dock = System.Windows.Forms.DockStyle.Fill; this.prgCreateSample.Name = "prgCreateSample"; this.prgCreateSample.Margin = new System.Windows.Forms.Padding(3, 6, 3, 4);
            this.commandLayout.SetColumnSpan(this.lblCreateSampleStatus, 2); this.lblCreateSampleStatus.AutoSize = true; this.lblCreateSampleStatus.Dock = System.Windows.Forms.DockStyle.Fill; this.lblCreateSampleStatus.Name = "lblCreateSampleStatus"; this.lblCreateSampleStatus.Text = "Ready"; this.lblCreateSampleStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill; this.splitContainer.Location = new System.Drawing.Point(0, 66); this.splitContainer.Name = "splitContainer"; this.splitContainer.Size = new System.Drawing.Size(800, 434); this.splitContainer.SplitterDistance = 520;
            this.sampleWindow.Dock = System.Windows.Forms.DockStyle.Fill; this.sampleWindow.Name = "sampleWindow";
            this.sampleConfigGrid.Dock = System.Windows.Forms.DockStyle.Fill; this.sampleConfigGrid.Name = "sampleConfigGrid";
            this.splitContainer.Panel1.Controls.Add(this.sampleWindow); this.splitContainer.Panel2.Controls.Add(this.sampleConfigGrid);
            this.Controls.Add(this.splitContainer); this.Controls.Add(this.commandLayout); this.Name = "CreateGerberSampleControl"; this.Size = new System.Drawing.Size(800, 500);
            this.commandLayout.ResumeLayout(false); this.commandLayout.PerformLayout(); this.splitContainer.Panel1.ResumeLayout(false); this.splitContainer.Panel2.ResumeLayout(false); ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit(); this.splitContainer.ResumeLayout(false); this.ResumeLayout(false);
        }
        private System.Windows.Forms.TableLayoutPanel commandLayout; private System.Windows.Forms.Button btnOpenSample; private System.Windows.Forms.TextBox txtSamplePath; private System.Windows.Forms.Button btnLoadSampleConfig; private System.Windows.Forms.Button btnSaveConfig; private System.Windows.Forms.Button btnCreateSample; private System.Windows.Forms.Button btnCancelCreateSample; private System.Windows.Forms.ProgressBar prgCreateSample; private System.Windows.Forms.Label lblCreateSampleStatus; private System.Windows.Forms.SplitContainer splitContainer; private EWindowControl.EWindowControl sampleWindow; private System.Windows.Forms.PropertyGrid sampleConfigGrid;
    }
}
