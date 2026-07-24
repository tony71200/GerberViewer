namespace GerberViewer.Views
{
    partial class AlignStitchingControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblImageCount;
        private System.Windows.Forms.Button btnSelectManifest, btnOpenImageFolder, btnSelectOutputFolder, btnRunAlignStitch, btnCancelAlignStitch;
        private System.Windows.Forms.TextBox txtManifestPath, txtImageFolder, txtOutputFolder, txtDiagnostics;
        private System.Windows.Forms.ListBox lstTab3Log;
        private System.Windows.Forms.ProgressBar prgAlignStitch;
        private System.Windows.Forms.TabControl resultTabControl;
        private System.Windows.Forms.TabPage tabOrderView, tabDiagnostics, tabStitchedImage, tabComparison, tabLogs;
        private GerberViewer.Stitching.DesignControls.PathCanvasControl orderPathCanvas;
        private System.Windows.Forms.PictureBox picStitchedImage;
        private System.Windows.Forms.CheckBox chkShowSampleMask;
        private GerberViewer.Views.SampleComparisonControl sampleComparisonControl;

        private void InitializeComponent()
        {
            this.btnSelectManifest = new System.Windows.Forms.Button();
            this.txtManifestPath = new System.Windows.Forms.TextBox();
            this.btnOpenImageFolder = new System.Windows.Forms.Button();
            this.txtImageFolder = new System.Windows.Forms.TextBox();
            this.btnSelectOutputFolder = new System.Windows.Forms.Button();
            this.txtOutputFolder = new System.Windows.Forms.TextBox();
            this.lblImageCount = new System.Windows.Forms.Label();
            this.btnRunAlignStitch = new System.Windows.Forms.Button();
            this.btnCancelAlignStitch = new System.Windows.Forms.Button();
            this.prgAlignStitch = new System.Windows.Forms.ProgressBar();
            this.resultTabControl = new System.Windows.Forms.TabControl();
            this.tabOrderView = new System.Windows.Forms.TabPage();
            this.orderPathCanvas = new GerberViewer.Stitching.DesignControls.PathCanvasControl();
            this.tabDiagnostics = new System.Windows.Forms.TabPage();
            this.txtDiagnostics = new System.Windows.Forms.TextBox();
            this.tabStitchedImage = new System.Windows.Forms.TabPage();
            this.resultWindow = new GerberViewer.Views.GerberSampleWindow();
            this.chkShowSampleMask = new System.Windows.Forms.CheckBox();
            this.picStitchedImage = new System.Windows.Forms.PictureBox();
            this.tabComparison = new System.Windows.Forms.TabPage();
            this.sampleComparisonControl = new GerberViewer.Views.SampleComparisonControl();
            this.tabLogs = new System.Windows.Forms.TabPage();
            this.lstTab3Log = new System.Windows.Forms.ListBox();
            this.tableLayout_AlignControl = new System.Windows.Forms.TableLayoutPanel();
            this.split_AlignContain = new System.Windows.Forms.SplitContainer();
            this.infotabControl = new System.Windows.Forms.TabControl();
            this.tabPage_ListMap = new System.Windows.Forms.TabPage();
            this.lstCapturedImages = new System.Windows.Forms.ListBox();
            this.tabPage_Params = new System.Windows.Forms.TabPage();
            this.alignConfigGrid = new System.Windows.Forms.PropertyGrid();
            this.tabPage_Log = new System.Windows.Forms.TabPage();
            this.txtManifestInfo = new System.Windows.Forms.TextBox();
            this.resultTabControl.SuspendLayout();
            this.tabOrderView.SuspendLayout();
            this.tabDiagnostics.SuspendLayout();
            this.tabStitchedImage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picStitchedImage)).BeginInit();
            this.tabComparison.SuspendLayout();
            this.tabLogs.SuspendLayout();
            this.tableLayout_AlignControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.split_AlignContain)).BeginInit();
            this.split_AlignContain.Panel1.SuspendLayout();
            this.split_AlignContain.Panel2.SuspendLayout();
            this.split_AlignContain.SuspendLayout();
            this.infotabControl.SuspendLayout();
            this.tabPage_ListMap.SuspendLayout();
            this.tabPage_Params.SuspendLayout();
            this.tabPage_Log.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSelectManifest
            // 
            this.btnSelectManifest.Location = new System.Drawing.Point(3, 3);
            this.btnSelectManifest.Name = "btnSelectManifest";
            this.btnSelectManifest.Size = new System.Drawing.Size(130, 24);
            this.btnSelectManifest.TabIndex = 13;
            this.btnSelectManifest.Text = "Select Manifest";
            this.btnSelectManifest.Click += new System.EventHandler(this.btnSelectManifest_Click);
            // 
            // txtManifestPath
            // 
            this.txtManifestPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtManifestPath.Location = new System.Drawing.Point(153, 3);
            this.txtManifestPath.Name = "txtManifestPath";
            this.txtManifestPath.ReadOnly = true;
            this.txtManifestPath.Size = new System.Drawing.Size(393, 31);
            this.txtManifestPath.TabIndex = 12;
            // 
            // btnOpenImageFolder
            // 
            this.btnOpenImageFolder.Location = new System.Drawing.Point(3, 33);
            this.btnOpenImageFolder.Name = "btnOpenImageFolder";
            this.btnOpenImageFolder.Size = new System.Drawing.Size(144, 24);
            this.btnOpenImageFolder.TabIndex = 10;
            this.btnOpenImageFolder.Text = "Select Captured Folder";
            this.btnOpenImageFolder.Click += new System.EventHandler(this.btnOpenImageFolder_Click);
            // 
            // txtImageFolder
            // 
            this.txtImageFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtImageFolder.Location = new System.Drawing.Point(153, 33);
            this.txtImageFolder.Name = "txtImageFolder";
            this.txtImageFolder.ReadOnly = true;
            this.txtImageFolder.Size = new System.Drawing.Size(393, 31);
            this.txtImageFolder.TabIndex = 9;
            this.txtImageFolder.TextChanged += new System.EventHandler(this.txtImageFolder_TextChanged);
            // 
            // btnSelectOutputFolder
            // 
            this.btnSelectOutputFolder.Location = new System.Drawing.Point(552, 33);
            this.btnSelectOutputFolder.Name = "btnSelectOutputFolder";
            this.btnSelectOutputFolder.Size = new System.Drawing.Size(144, 24);
            this.btnSelectOutputFolder.TabIndex = 8;
            this.btnSelectOutputFolder.Text = "Select Output Folder";
            this.btnSelectOutputFolder.Click += new System.EventHandler(this.btnSelectOutputFolder_Click);
            // 
            // txtOutputFolder
            // 
            this.txtOutputFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputFolder.Location = new System.Drawing.Point(702, 33);
            this.txtOutputFolder.Name = "txtOutputFolder";
            this.txtOutputFolder.ReadOnly = true;
            this.txtOutputFolder.Size = new System.Drawing.Size(393, 31);
            this.txtOutputFolder.TabIndex = 7;
            // 
            // lblImageCount
            // 
            this.lblImageCount.AutoSize = true;
            this.lblImageCount.Location = new System.Drawing.Point(3, 567);
            this.lblImageCount.Name = "lblImageCount";
            this.lblImageCount.Size = new System.Drawing.Size(105, 25);
            this.lblImageCount.TabIndex = 5;
            this.lblImageCount.Text = "Images: 0";
            // 
            // btnRunAlignStitch
            // 
            this.btnRunAlignStitch.Location = new System.Drawing.Point(552, 3);
            this.btnRunAlignStitch.Name = "btnRunAlignStitch";
            this.btnRunAlignStitch.Size = new System.Drawing.Size(120, 24);
            this.btnRunAlignStitch.TabIndex = 4;
            this.btnRunAlignStitch.Text = "Run Align/Stitch";
            this.btnRunAlignStitch.Click += new System.EventHandler(this.btnRunAlignStitch_Click);
            // 
            // btnCancelAlignStitch
            // 
            this.btnCancelAlignStitch.Enabled = false;
            this.btnCancelAlignStitch.Location = new System.Drawing.Point(702, 3);
            this.btnCancelAlignStitch.Name = "btnCancelAlignStitch";
            this.btnCancelAlignStitch.Size = new System.Drawing.Size(80, 24);
            this.btnCancelAlignStitch.TabIndex = 3;
            this.btnCancelAlignStitch.Text = "Cancel";
            this.btnCancelAlignStitch.Click += new System.EventHandler(this.btnCancelAlignStitch_Click);
            // 
            // prgAlignStitch
            // 
            this.tableLayout_AlignControl.SetColumnSpan(this.prgAlignStitch, 3);
            this.prgAlignStitch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.prgAlignStitch.Location = new System.Drawing.Point(153, 570);
            this.prgAlignStitch.Name = "prgAlignStitch";
            this.prgAlignStitch.Size = new System.Drawing.Size(942, 19);
            this.prgAlignStitch.TabIndex = 2;
            // 
            // resultTabControl
            // 
            this.resultTabControl.Controls.Add(this.tabOrderView);
            this.resultTabControl.Controls.Add(this.tabDiagnostics);
            this.resultTabControl.Controls.Add(this.tabStitchedImage);
            this.resultTabControl.Controls.Add(this.tabComparison);
            this.resultTabControl.Controls.Add(this.tabLogs);
            this.resultTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultTabControl.Location = new System.Drawing.Point(0, 0);
            this.resultTabControl.Name = "resultTabControl";
            this.resultTabControl.SelectedIndex = 0;
            this.resultTabControl.Size = new System.Drawing.Size(788, 501);
            this.resultTabControl.TabIndex = 0;
            // 
            // tabOrderView
            // 
            this.tabOrderView.Controls.Add(this.orderPathCanvas);
            this.tabOrderView.Location = new System.Drawing.Point(8, 39);
            this.tabOrderView.Name = "tabOrderView";
            this.tabOrderView.Size = new System.Drawing.Size(772, 454);
            this.tabOrderView.TabIndex = 0;
            this.tabOrderView.Text = "Order and Status";
            // 
            // orderPathCanvas
            // 
            this.orderPathCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.orderPathCanvas.Location = new System.Drawing.Point(0, 0);
            this.orderPathCanvas.Name = "orderPathCanvas";
            this.orderPathCanvas.Size = new System.Drawing.Size(772, 454);
            this.orderPathCanvas.TabIndex = 0;
            // 
            // tabDiagnostics
            // 
            this.tabDiagnostics.Controls.Add(this.txtDiagnostics);
            this.tabDiagnostics.Location = new System.Drawing.Point(8, 39);
            this.tabDiagnostics.Name = "tabDiagnostics";
            this.tabDiagnostics.Size = new System.Drawing.Size(772, 454);
            this.tabDiagnostics.TabIndex = 1;
            this.tabDiagnostics.Text = "Alignment Diagnostics";
            // 
            // txtDiagnostics
            // 
            this.txtDiagnostics.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDiagnostics.Location = new System.Drawing.Point(0, 0);
            this.txtDiagnostics.Multiline = true;
            this.txtDiagnostics.Name = "txtDiagnostics";
            this.txtDiagnostics.ReadOnly = true;
            this.txtDiagnostics.Size = new System.Drawing.Size(772, 454);
            this.txtDiagnostics.TabIndex = 0;
            this.txtDiagnostics.Text = "OrderIndex, Row/Column, paths, NCC, ECC, CapturedToSampleTransform, pose source a" +
    "nd rejection reason appear in logs/report.";
            // 
            // tabStitchedImage
            // 
            this.tabStitchedImage.Controls.Add(this.resultWindow);
            this.tabStitchedImage.Controls.Add(this.chkShowSampleMask);
            this.tabStitchedImage.Controls.Add(this.picStitchedImage);
            this.tabStitchedImage.Location = new System.Drawing.Point(8, 39);
            this.tabStitchedImage.Name = "tabStitchedImage";
            this.tabStitchedImage.Size = new System.Drawing.Size(772, 454);
            this.tabStitchedImage.TabIndex = 2;
            this.tabStitchedImage.Text = "Stitched Result";
            // 
            // resultWindow
            // 
            this.resultWindow.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.resultWindow.DefaultRoiSize = 128;
            this.resultWindow.EnableDoubleClickZoom = false;
            this.resultWindow.EnableInfo = true;
            this.resultWindow.EnableInfoFromUser = false;
            this.resultWindow.EnableMouseWheelZoom = true;
            this.resultWindow.Location = new System.Drawing.Point(0, 30);
            this.resultWindow.LockRoiScale = false;
            this.resultWindow.Name = "resultWindow";
            this.resultWindow.Size = new System.Drawing.Size(772, 424);
            this.resultWindow.SourceBitmap = null;
            this.resultWindow.SourceHobject = null;
            this.resultWindow.TabIndex = 1;
            this.resultWindow.Tol_MagicWand = 50;
            this.resultWindow.VisibleROI = true;
            this.resultWindow.VisibleROIText = false;
            this.resultWindow.WinOperate = 1;
            this.resultWindow.ZoomRatio = 120;
            // 
            // chkShowSampleMask
            // 
            this.chkShowSampleMask.AutoSize = true;
            this.chkShowSampleMask.Location = new System.Drawing.Point(8, 3);
            this.chkShowSampleMask.Name = "chkShowSampleMask";
            this.chkShowSampleMask.Size = new System.Drawing.Size(284, 29);
            this.chkShowSampleMask.TabIndex = 2;
            this.chkShowSampleMask.Text = "Show Tab2 sample mask";
            this.chkShowSampleMask.UseVisualStyleBackColor = true;
            this.chkShowSampleMask.CheckedChanged += new System.EventHandler(this.chkShowSampleMask_CheckedChanged);
            // 
            // picStitchedImage
            // 
            this.picStitchedImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picStitchedImage.Location = new System.Drawing.Point(0, 0);
            this.picStitchedImage.Name = "picStitchedImage";
            this.picStitchedImage.Size = new System.Drawing.Size(772, 454);
            this.picStitchedImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picStitchedImage.TabIndex = 0;
            this.picStitchedImage.TabStop = false;
            this.picStitchedImage.Visible = false;
            // 
            // tabComparison
            // 
            this.tabComparison.Controls.Add(this.sampleComparisonControl);
            this.tabComparison.Location = new System.Drawing.Point(8, 39);
            this.tabComparison.Name = "tabComparison";
            this.tabComparison.Size = new System.Drawing.Size(772, 454);
            this.tabComparison.TabIndex = 3;
            this.tabComparison.Text = "Sample Comparison";
            // 
            // sampleComparisonControl
            // 
            this.sampleComparisonControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sampleComparisonControl.Location = new System.Drawing.Point(0, 0);
            this.sampleComparisonControl.Name = "sampleComparisonControl";
            this.sampleComparisonControl.Size = new System.Drawing.Size(772, 454);
            this.sampleComparisonControl.TabIndex = 0;
            // 
            // tabLogs
            // 
            this.tabLogs.Controls.Add(this.lstTab3Log);
            this.tabLogs.Location = new System.Drawing.Point(8, 39);
            this.tabLogs.Name = "tabLogs";
            this.tabLogs.Size = new System.Drawing.Size(772, 454);
            this.tabLogs.TabIndex = 4;
            this.tabLogs.Text = "Logs";
            // 
            // lstTab3Log
            // 
            this.lstTab3Log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstTab3Log.ItemHeight = 25;
            this.lstTab3Log.Location = new System.Drawing.Point(0, 0);
            this.lstTab3Log.Name = "lstTab3Log";
            this.lstTab3Log.Size = new System.Drawing.Size(772, 454);
            this.lstTab3Log.TabIndex = 0;
            // 
            // tableLayout_AlignControl
            // 
            this.tableLayout_AlignControl.ColumnCount = 4;
            this.tableLayout_AlignControl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayout_AlignControl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayout_AlignControl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tableLayout_AlignControl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayout_AlignControl.Controls.Add(this.btnSelectManifest, 0, 0);
            this.tableLayout_AlignControl.Controls.Add(this.lblImageCount, 0, 3);
            this.tableLayout_AlignControl.Controls.Add(this.btnOpenImageFolder, 0, 1);
            this.tableLayout_AlignControl.Controls.Add(this.prgAlignStitch, 1, 3);
            this.tableLayout_AlignControl.Controls.Add(this.txtImageFolder, 1, 1);
            this.tableLayout_AlignControl.Controls.Add(this.btnCancelAlignStitch, 3, 0);
            this.tableLayout_AlignControl.Controls.Add(this.btnSelectOutputFolder, 2, 1);
            this.tableLayout_AlignControl.Controls.Add(this.btnRunAlignStitch, 2, 0);
            this.tableLayout_AlignControl.Controls.Add(this.txtOutputFolder, 3, 1);
            this.tableLayout_AlignControl.Controls.Add(this.split_AlignContain, 0, 2);
            this.tableLayout_AlignControl.Controls.Add(this.txtManifestPath, 1, 0);
            this.tableLayout_AlignControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayout_AlignControl.Location = new System.Drawing.Point(0, 0);
            this.tableLayout_AlignControl.Name = "tableLayout_AlignControl";
            this.tableLayout_AlignControl.RowCount = 4;
            this.tableLayout_AlignControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayout_AlignControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayout_AlignControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayout_AlignControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayout_AlignControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayout_AlignControl.Size = new System.Drawing.Size(1098, 592);
            this.tableLayout_AlignControl.TabIndex = 14;
            // 
            // split_AlignContain
            // 
            this.tableLayout_AlignControl.SetColumnSpan(this.split_AlignContain, 4);
            this.split_AlignContain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split_AlignContain.Location = new System.Drawing.Point(3, 63);
            this.split_AlignContain.Name = "split_AlignContain";
            // 
            // split_AlignContain.Panel1
            // 
            this.split_AlignContain.Panel1.Controls.Add(this.infotabControl);
            // 
            // split_AlignContain.Panel2
            // 
            this.split_AlignContain.Panel2.Controls.Add(this.resultTabControl);
            this.split_AlignContain.Size = new System.Drawing.Size(1092, 501);
            this.split_AlignContain.SplitterDistance = 300;
            this.split_AlignContain.TabIndex = 14;
            // 
            // infotabControl
            // 
            this.infotabControl.Controls.Add(this.tabPage_ListMap);
            this.infotabControl.Controls.Add(this.tabPage_Params);
            this.infotabControl.Controls.Add(this.tabPage_Log);
            this.infotabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infotabControl.Location = new System.Drawing.Point(0, 0);
            this.infotabControl.Name = "infotabControl";
            this.infotabControl.SelectedIndex = 0;
            this.infotabControl.Size = new System.Drawing.Size(300, 501);
            this.infotabControl.TabIndex = 0;
            // 
            // tabPage_ListMap
            // 
            this.tabPage_ListMap.Controls.Add(this.lstCapturedImages);
            this.tabPage_ListMap.Location = new System.Drawing.Point(8, 39);
            this.tabPage_ListMap.Name = "tabPage_ListMap";
            this.tabPage_ListMap.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_ListMap.Size = new System.Drawing.Size(284, 454);
            this.tabPage_ListMap.TabIndex = 0;
            this.tabPage_ListMap.Text = "List Map";
            this.tabPage_ListMap.UseVisualStyleBackColor = true;
            // 
            // lstCapturedImages
            // 
            this.lstCapturedImages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstCapturedImages.ItemHeight = 25;
            this.lstCapturedImages.Location = new System.Drawing.Point(3, 3);
            this.lstCapturedImages.Name = "lstCapturedImages";
            this.lstCapturedImages.Size = new System.Drawing.Size(278, 448);
            this.lstCapturedImages.TabIndex = 6;
            // 
            // tabPage_Params
            // 
            this.tabPage_Params.Controls.Add(this.alignConfigGrid);
            this.tabPage_Params.Location = new System.Drawing.Point(8, 39);
            this.tabPage_Params.Name = "tabPage_Params";
            this.tabPage_Params.Size = new System.Drawing.Size(284, 454);
            this.tabPage_Params.TabIndex = 2;
            this.tabPage_Params.Text = "Parameters";
            this.tabPage_Params.UseVisualStyleBackColor = true;
            // 
            // alignConfigGrid
            // 
            this.alignConfigGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.alignConfigGrid.Location = new System.Drawing.Point(0, 0);
            this.alignConfigGrid.Name = "alignConfigGrid";
            this.alignConfigGrid.Size = new System.Drawing.Size(284, 454);
            this.alignConfigGrid.TabIndex = 1;
            // 
            // tabPage_Log
            // 
            this.tabPage_Log.Controls.Add(this.txtManifestInfo);
            this.tabPage_Log.Location = new System.Drawing.Point(8, 39);
            this.tabPage_Log.Name = "tabPage_Log";
            this.tabPage_Log.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_Log.Size = new System.Drawing.Size(284, 454);
            this.tabPage_Log.TabIndex = 1;
            this.tabPage_Log.Text = "Infos";
            this.tabPage_Log.UseVisualStyleBackColor = true;
            // 
            // txtManifestInfo
            // 
            this.txtManifestInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtManifestInfo.Location = new System.Drawing.Point(3, 3);
            this.txtManifestInfo.Multiline = true;
            this.txtManifestInfo.Name = "txtManifestInfo";
            this.txtManifestInfo.ReadOnly = true;
            this.txtManifestInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtManifestInfo.Size = new System.Drawing.Size(278, 448);
            this.txtManifestInfo.TabIndex = 11;
            // 
            // AlignStitchingControl
            // 
            this.Controls.Add(this.tableLayout_AlignControl);
            this.Name = "AlignStitchingControl";
            this.Size = new System.Drawing.Size(1098, 592);
            this.resultTabControl.ResumeLayout(false);
            this.tabOrderView.ResumeLayout(false);
            this.tabDiagnostics.ResumeLayout(false);
            this.tabDiagnostics.PerformLayout();
            this.tabStitchedImage.ResumeLayout(false);
            this.tabStitchedImage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picStitchedImage)).EndInit();
            this.tabComparison.ResumeLayout(false);
            this.tabLogs.ResumeLayout(false);
            this.tableLayout_AlignControl.ResumeLayout(false);
            this.tableLayout_AlignControl.PerformLayout();
            this.split_AlignContain.Panel1.ResumeLayout(false);
            this.split_AlignContain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.split_AlignContain)).EndInit();
            this.split_AlignContain.ResumeLayout(false);
            this.infotabControl.ResumeLayout(false);
            this.tabPage_ListMap.ResumeLayout(false);
            this.tabPage_Params.ResumeLayout(false);
            this.tabPage_Log.ResumeLayout(false);
            this.tabPage_Log.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TableLayoutPanel tableLayout_AlignControl;
        private System.Windows.Forms.SplitContainer split_AlignContain;
        private System.Windows.Forms.TabControl infotabControl;
        private System.Windows.Forms.TabPage tabPage_ListMap;
        private System.Windows.Forms.ListBox lstCapturedImages;
        private System.Windows.Forms.TabPage tabPage_Params;
        private System.Windows.Forms.PropertyGrid alignConfigGrid;
        private System.Windows.Forms.TabPage tabPage_Log;
        private System.Windows.Forms.TextBox txtManifestInfo;
        private GerberSampleWindow resultWindow;
    }
}
