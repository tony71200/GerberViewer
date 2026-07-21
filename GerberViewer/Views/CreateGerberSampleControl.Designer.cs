namespace GerberViewer.Views
{
    partial class CreateGerberSampleControl
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing) { DisposeSampleSource(); if (components != null) components.Dispose(); } base.Dispose(disposing); }
        private void InitializeComponent()
        {
            this.commandLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnOpenSample = new System.Windows.Forms.Button();
            this.txtSamplePath = new System.Windows.Forms.TextBox();
            this.btnLoadSampleConfig = new System.Windows.Forms.Button();
            this.btnSaveConfig = new System.Windows.Forms.Button();
            this.btnRefreshPreview = new System.Windows.Forms.Button();
            this.btnCreateSample = new System.Windows.Forms.Button();
            this.btnCancelCreateSample = new System.Windows.Forms.Button();
            this.prgCreateSample = new System.Windows.Forms.ProgressBar();
            this.lblCreateSampleStatus = new System.Windows.Forms.Label();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.sampleWindow = new GerberViewer.Views.GerberSampleWindow();
            this.sampleConfigGrid = new System.Windows.Forms.PropertyGrid();
            this.commandLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // commandLayout
            // 
            this.commandLayout.ColumnCount = 7;
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 116F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 108F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.commandLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 84F));
            this.commandLayout.Controls.Add(this.btnOpenSample, 0, 0);
            this.commandLayout.Controls.Add(this.txtSamplePath, 1, 0);
            this.commandLayout.Controls.Add(this.btnLoadSampleConfig, 2, 0);
            this.commandLayout.Controls.Add(this.btnSaveConfig, 3, 0);
            this.commandLayout.Controls.Add(this.btnRefreshPreview, 4, 0);
            this.commandLayout.Controls.Add(this.btnCreateSample, 5, 0);
            this.commandLayout.Controls.Add(this.btnCancelCreateSample, 6, 0);
            this.commandLayout.Controls.Add(this.prgCreateSample, 0, 1);
            this.commandLayout.Controls.Add(this.lblCreateSampleStatus, 4, 1);
            this.commandLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.commandLayout.Location = new System.Drawing.Point(0, 0);
            this.commandLayout.Name = "commandLayout";
            this.commandLayout.RowCount = 2;
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            this.commandLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.commandLayout.Size = new System.Drawing.Size(800, 66);
            this.commandLayout.TabIndex = 1;
            // 
            // btnOpenSample
            // 
            this.btnOpenSample.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOpenSample.Location = new System.Drawing.Point(3, 3);
            this.btnOpenSample.Name = "btnOpenSample";
            this.btnOpenSample.Size = new System.Drawing.Size(110, 32);
            this.btnOpenSample.TabIndex = 0;
            this.btnOpenSample.Text = "Open Sample";
            this.btnOpenSample.UseVisualStyleBackColor = true;
            this.btnOpenSample.Click += new System.EventHandler(this.btnOpenSample_Click);
            // 
            // txtSamplePath
            // 
            this.txtSamplePath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSamplePath.Location = new System.Drawing.Point(119, 9);
            this.txtSamplePath.Margin = new System.Windows.Forms.Padding(3, 9, 3, 3);
            this.txtSamplePath.Name = "txtSamplePath";
            this.txtSamplePath.ReadOnly = true;
            this.txtSamplePath.Size = new System.Drawing.Size(190, 31);
            this.txtSamplePath.TabIndex = 1;
            // 
            // btnLoadSampleConfig
            // 
            this.btnLoadSampleConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLoadSampleConfig.Location = new System.Drawing.Point(315, 3);
            this.btnLoadSampleConfig.Name = "btnLoadSampleConfig";
            this.btnLoadSampleConfig.Size = new System.Drawing.Size(102, 32);
            this.btnLoadSampleConfig.TabIndex = 2;
            this.btnLoadSampleConfig.Text = "Load Config";
            this.btnLoadSampleConfig.UseVisualStyleBackColor = true;
            this.btnLoadSampleConfig.Click += new System.EventHandler(this.btnLoadSampleConfig_Click);
            // 
            // btnSaveConfig
            // 
            this.btnSaveConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveConfig.Location = new System.Drawing.Point(423, 3);
            this.btnSaveConfig.Name = "btnSaveConfig";
            this.btnSaveConfig.Size = new System.Drawing.Size(102, 32);
            this.btnSaveConfig.TabIndex = 3;
            this.btnSaveConfig.Text = "Save Config";
            this.btnSaveConfig.UseVisualStyleBackColor = true;
            this.btnSaveConfig.Click += new System.EventHandler(this.btnSaveConfig_Click);
            // 
            // btnRefreshPreview
            // 
            this.btnRefreshPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefreshPreview.Location = new System.Drawing.Point(531, 3);
            this.btnRefreshPreview.Name = "btnRefreshPreview";
            this.btnRefreshPreview.Size = new System.Drawing.Size(86, 32);
            this.btnRefreshPreview.TabIndex = 4;
            this.btnRefreshPreview.Text = "Refresh";
            this.btnRefreshPreview.UseVisualStyleBackColor = true;
            this.btnRefreshPreview.Click += new System.EventHandler(this.btnRefreshPreview_Click);
            // 
            // btnCreateSample
            // 
            this.btnCreateSample.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCreateSample.Location = new System.Drawing.Point(623, 3);
            this.btnCreateSample.Name = "btnCreateSample";
            this.btnCreateSample.Size = new System.Drawing.Size(90, 32);
            this.btnCreateSample.TabIndex = 5;
            this.btnCreateSample.Text = "Create";
            this.btnCreateSample.UseVisualStyleBackColor = true;
            this.btnCreateSample.Click += new System.EventHandler(this.btnCreateSample_Click);
            // 
            // btnCancelCreateSample
            // 
            this.btnCancelCreateSample.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCancelCreateSample.Enabled = false;
            this.btnCancelCreateSample.Location = new System.Drawing.Point(719, 3);
            this.btnCancelCreateSample.Name = "btnCancelCreateSample";
            this.btnCancelCreateSample.Size = new System.Drawing.Size(78, 32);
            this.btnCancelCreateSample.TabIndex = 6;
            this.btnCancelCreateSample.Text = "Cancel";
            this.btnCancelCreateSample.UseVisualStyleBackColor = true;
            this.btnCancelCreateSample.Click += new System.EventHandler(this.btnCancelCreateSample_Click);
            // 
            // prgCreateSample
            // 
            this.commandLayout.SetColumnSpan(this.prgCreateSample, 4);
            this.prgCreateSample.Dock = System.Windows.Forms.DockStyle.Fill;
            this.prgCreateSample.Location = new System.Drawing.Point(3, 44);
            this.prgCreateSample.Margin = new System.Windows.Forms.Padding(3, 6, 3, 4);
            this.prgCreateSample.Name = "prgCreateSample";
            this.prgCreateSample.Size = new System.Drawing.Size(522, 18);
            this.prgCreateSample.TabIndex = 7;
            // 
            // lblCreateSampleStatus
            // 
            this.lblCreateSampleStatus.AutoSize = true;
            this.commandLayout.SetColumnSpan(this.lblCreateSampleStatus, 2);
            this.lblCreateSampleStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCreateSampleStatus.Location = new System.Drawing.Point(531, 38);
            this.lblCreateSampleStatus.Name = "lblCreateSampleStatus";
            this.lblCreateSampleStatus.Size = new System.Drawing.Size(182, 28);
            this.lblCreateSampleStatus.TabIndex = 8;
            this.lblCreateSampleStatus.Text = "Ready";
            this.lblCreateSampleStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 66);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.sampleWindow);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.sampleConfigGrid);
            this.splitContainer.Size = new System.Drawing.Size(800, 434);
            this.splitContainer.SplitterDistance = 520;
            this.splitContainer.TabIndex = 0;
            // 
            // sampleWindow
            // 
            this.sampleWindow.DefaultRoiSize = 128;
            this.sampleWindow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sampleWindow.EnableDoubleClickZoom = false;
            this.sampleWindow.EnableInfo = true;
            this.sampleWindow.EnableInfoFromUser = false;
            this.sampleWindow.EnableMouseWheelZoom = false;
            this.sampleWindow.Location = new System.Drawing.Point(0, 0);
            this.sampleWindow.LockRoiScale = false;
            this.sampleWindow.Name = "sampleWindow";
            this.sampleWindow.Size = new System.Drawing.Size(520, 434);
            this.sampleWindow.SourceBitmap = null;
            this.sampleWindow.SourceHobject = null;
            this.sampleWindow.TabIndex = 0;
            this.sampleWindow.Tol_MagicWand = 50;
            this.sampleWindow.VisibleROI = true;
            this.sampleWindow.VisibleROIText = false;
            this.sampleWindow.WinOperate = 0;
            this.sampleWindow.ZoomRatio = 120;
            // 
            // sampleConfigGrid
            // 
            this.sampleConfigGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sampleConfigGrid.Location = new System.Drawing.Point(0, 0);
            this.sampleConfigGrid.Name = "sampleConfigGrid";
            this.sampleConfigGrid.Size = new System.Drawing.Size(276, 434);
            this.sampleConfigGrid.TabIndex = 0;
            // 
            // CreateGerberSampleControl
            // 
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.commandLayout);
            this.Name = "CreateGerberSampleControl";
            this.Size = new System.Drawing.Size(800, 500);
            this.commandLayout.ResumeLayout(false);
            this.commandLayout.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        private System.Windows.Forms.TableLayoutPanel commandLayout; private System.Windows.Forms.Button btnOpenSample; private System.Windows.Forms.TextBox txtSamplePath; private System.Windows.Forms.Button btnLoadSampleConfig; private System.Windows.Forms.Button btnSaveConfig; private System.Windows.Forms.Button btnRefreshPreview; private System.Windows.Forms.Button btnCreateSample; private System.Windows.Forms.Button btnCancelCreateSample; private System.Windows.Forms.ProgressBar prgCreateSample; private System.Windows.Forms.Label lblCreateSampleStatus; private System.Windows.Forms.SplitContainer splitContainer; private GerberViewer.Views.GerberSampleWindow sampleWindow; private System.Windows.Forms.PropertyGrid sampleConfigGrid;
    }
}
