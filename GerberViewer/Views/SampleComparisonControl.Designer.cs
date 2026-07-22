namespace GerberViewer.Views
{
    partial class SampleComparisonControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel layoutMain;
        private System.Windows.Forms.TableLayoutPanel layoutLeft;
        private System.Windows.Forms.SplitContainer splitPreview;
        private System.Windows.Forms.GroupBox grpSample;
        private System.Windows.Forms.GroupBox grpReality;
        private ComparisonImageView sampleImageView;
        private ComparisonImageView realityImageView;
        private System.Windows.Forms.ComboBox cmbComparisonMode;
        private System.Windows.Forms.NumericUpDown nudAlpha;
        private System.Windows.Forms.NumericUpDown nudBlinkInterval;
        private System.Windows.Forms.NumericUpDown nudDifferenceGain;
        private System.Windows.Forms.NumericUpDown nudEdgeTolerance;
        private System.Windows.Forms.CheckBox chkSynchronizeView;
        private System.Windows.Forms.Button btnRefreshComparison;
        private System.Windows.Forms.Button btnStopBlink;
        private System.Windows.Forms.Button btnFitBoth;
        private System.Windows.Forms.Button btnResetView;
        private System.Windows.Forms.Button btnSaveCurrentView;
        private System.Windows.Forms.Button btnOpenOutputFolder;
        private System.Windows.Forms.TextBox txtCoordinateStatus;
        private System.Windows.Forms.TextBox txtInputInfo;
        private System.Windows.Forms.TextBox txtMetrics;
        private System.Windows.Forms.TextBox txtWarnings;
        private System.Windows.Forms.Label lblCursorInfo;
        private System.Windows.Forms.Timer comparisonBlinkTimer;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.layoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.layoutLeft = new System.Windows.Forms.TableLayoutPanel();
            this.cmbComparisonMode = new System.Windows.Forms.ComboBox();
            this.nudAlpha = new System.Windows.Forms.NumericUpDown();
            this.nudBlinkInterval = new System.Windows.Forms.NumericUpDown();
            this.nudDifferenceGain = new System.Windows.Forms.NumericUpDown();
            this.nudEdgeTolerance = new System.Windows.Forms.NumericUpDown();
            this.chkSynchronizeView = new System.Windows.Forms.CheckBox();
            this.btnRefreshComparison = new System.Windows.Forms.Button();
            this.btnStopBlink = new System.Windows.Forms.Button();
            this.btnFitBoth = new System.Windows.Forms.Button();
            this.btnResetView = new System.Windows.Forms.Button();
            this.btnSaveCurrentView = new System.Windows.Forms.Button();
            this.btnOpenOutputFolder = new System.Windows.Forms.Button();
            this.txtCoordinateStatus = new System.Windows.Forms.TextBox();
            this.txtInputInfo = new System.Windows.Forms.TextBox();
            this.txtMetrics = new System.Windows.Forms.TextBox();
            this.txtWarnings = new System.Windows.Forms.TextBox();
            this.lblCursorInfo = new System.Windows.Forms.Label();
            this.splitPreview = new System.Windows.Forms.SplitContainer();
            this.grpSample = new System.Windows.Forms.GroupBox();
            this.sampleImageView = new GerberViewer.Views.ComparisonImageView();
            this.grpReality = new System.Windows.Forms.GroupBox();
            this.realityImageView = new GerberViewer.Views.ComparisonImageView();
            this.comparisonBlinkTimer = new System.Windows.Forms.Timer(this.components);
            this.layoutMain.SuspendLayout();
            this.layoutLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAlpha)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBlinkInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDifferenceGain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEdgeTolerance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitPreview)).BeginInit();
            this.splitPreview.Panel1.SuspendLayout();
            this.splitPreview.Panel2.SuspendLayout();
            this.splitPreview.SuspendLayout();
            this.grpSample.SuspendLayout();
            this.grpReality.SuspendLayout();
            this.SuspendLayout();
            // 
            // layoutMain
            // 
            this.layoutMain.ColumnCount = 2;
            this.layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.layoutMain.Controls.Add(this.layoutLeft, 0, 0);
            this.layoutMain.Controls.Add(this.splitPreview, 1, 0);
            this.layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutMain.Location = new System.Drawing.Point(0, 0);
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.RowCount = 1;
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutMain.Size = new System.Drawing.Size(1000, 650);
            this.layoutMain.TabIndex = 0;
            // 
            // layoutLeft
            // 
            this.layoutLeft.AutoScroll = true;
            this.layoutLeft.ColumnCount = 2;
            this.layoutLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.layoutLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.layoutLeft.Controls.Add(new System.Windows.Forms.Label { Text = "Comparison Method", Dock = System.Windows.Forms.DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            this.layoutLeft.Controls.Add(this.cmbComparisonMode, 1, 0);
            this.layoutLeft.Controls.Add(new System.Windows.Forms.Label { Text = "Alpha (%)", Dock = System.Windows.Forms.DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 1);
            this.layoutLeft.Controls.Add(this.nudAlpha, 1, 1);
            this.layoutLeft.Controls.Add(new System.Windows.Forms.Label { Text = "Blink interval (ms)", Dock = System.Windows.Forms.DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 2);
            this.layoutLeft.Controls.Add(this.nudBlinkInterval, 1, 2);
            this.layoutLeft.Controls.Add(new System.Windows.Forms.Label { Text = "Difference gain", Dock = System.Windows.Forms.DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 3);
            this.layoutLeft.Controls.Add(this.nudDifferenceGain, 1, 3);
            this.layoutLeft.Controls.Add(new System.Windows.Forms.Label { Text = "Edge tolerance (px)", Dock = System.Windows.Forms.DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 4);
            this.layoutLeft.Controls.Add(this.nudEdgeTolerance, 1, 4);
            this.layoutLeft.Controls.Add(this.chkSynchronizeView, 0, 5);
            this.layoutLeft.Controls.Add(this.btnRefreshComparison, 0, 6);
            this.layoutLeft.Controls.Add(this.btnStopBlink, 1, 6);
            this.layoutLeft.Controls.Add(this.btnFitBoth, 0, 7);
            this.layoutLeft.Controls.Add(this.btnResetView, 1, 7);
            this.layoutLeft.Controls.Add(this.btnSaveCurrentView, 0, 8);
            this.layoutLeft.Controls.Add(this.btnOpenOutputFolder, 1, 8);
            this.layoutLeft.Controls.Add(new System.Windows.Forms.Label { Text = "Coordinate Status", Dock = System.Windows.Forms.DockStyle.Fill }, 0, 9);
            this.layoutLeft.Controls.Add(this.txtCoordinateStatus, 0, 10);
            this.layoutLeft.Controls.Add(new System.Windows.Forms.Label { Text = "Input Information", Dock = System.Windows.Forms.DockStyle.Fill }, 0, 11);
            this.layoutLeft.Controls.Add(this.txtInputInfo, 0, 12);
            this.layoutLeft.Controls.Add(new System.Windows.Forms.Label { Text = "Metrics", Dock = System.Windows.Forms.DockStyle.Fill }, 0, 13);
            this.layoutLeft.Controls.Add(this.txtMetrics, 0, 14);
            this.layoutLeft.Controls.Add(new System.Windows.Forms.Label { Text = "Warnings", Dock = System.Windows.Forms.DockStyle.Fill }, 0, 15);
            this.layoutLeft.Controls.Add(this.txtWarnings, 0, 16);
            this.layoutLeft.Controls.Add(this.lblCursorInfo, 0, 17);
            this.layoutLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutLeft.Location = new System.Drawing.Point(3, 3);
            this.layoutLeft.Name = "layoutLeft";
            this.layoutLeft.RowCount = 18;
            for (int i = 0; i < 9; i++) this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 54F));
            this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 92F));
            this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 190F));
            this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            this.layoutLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.layoutLeft.Size = new System.Drawing.Size(344, 644);
            this.layoutLeft.TabIndex = 0;
            this.layoutLeft.SetColumnSpan(this.chkSynchronizeView, 2);
            this.layoutLeft.SetColumnSpan(this.txtCoordinateStatus, 2);
            this.layoutLeft.SetColumnSpan(this.txtInputInfo, 2);
            this.layoutLeft.SetColumnSpan(this.txtMetrics, 2);
            this.layoutLeft.SetColumnSpan(this.txtWarnings, 2);
            this.layoutLeft.SetColumnSpan(this.lblCursorInfo, 2);
            // 
            // controls
            // 
            this.cmbComparisonMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbComparisonMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.nudAlpha.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudAlpha.Minimum = 0;
            this.nudAlpha.Maximum = 100;
            this.nudAlpha.Value = 50;
            this.nudBlinkInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudBlinkInterval.Minimum = 100;
            this.nudBlinkInterval.Maximum = 5000;
            this.nudBlinkInterval.Value = 500;
            this.nudDifferenceGain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudDifferenceGain.Minimum = 1;
            this.nudDifferenceGain.Maximum = 20;
            this.nudDifferenceGain.Value = 3;
            this.nudEdgeTolerance.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudEdgeTolerance.Minimum = 1;
            this.nudEdgeTolerance.Maximum = 20;
            this.nudEdgeTolerance.Value = 2;
            this.chkSynchronizeView.Checked = true;
            this.chkSynchronizeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkSynchronizeView.Text = "Synchronize View";
            this.btnRefreshComparison.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefreshComparison.Text = "Refresh";
            this.btnStopBlink.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStopBlink.Text = "Stop Blink";
            this.btnFitBoth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFitBoth.Text = "Fit Both";
            this.btnResetView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnResetView.Text = "Reset View";
            this.btnSaveCurrentView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSaveCurrentView.Text = "Save View";
            this.btnOpenOutputFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOpenOutputFolder.Text = "Open Folder";
            this.txtCoordinateStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtCoordinateStatus.Multiline = true;
            this.txtCoordinateStatus.ReadOnly = true;
            this.txtInputInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInputInfo.Multiline = true;
            this.txtInputInfo.ReadOnly = true;
            this.txtMetrics.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtMetrics.Multiline = true;
            this.txtMetrics.ReadOnly = true;
            this.txtMetrics.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtWarnings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtWarnings.Multiline = true;
            this.txtWarnings.ReadOnly = true;
            this.txtWarnings.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.lblCursorInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCursorInfo.Text = "Cursor: -";
            // 
            // splitPreview
            // 
            this.splitPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitPreview.Location = new System.Drawing.Point(353, 3);
            this.splitPreview.Name = "splitPreview";
            this.splitPreview.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitPreview.Panel1.Controls.Add(this.grpSample);
            this.splitPreview.Panel2.Controls.Add(this.grpReality);
            this.splitPreview.Size = new System.Drawing.Size(644, 644);
            this.splitPreview.SplitterDistance = 318;
            this.splitPreview.TabIndex = 1;
            // 
            // grpSample
            // 
            this.grpSample.Controls.Add(this.sampleImageView);
            this.grpSample.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSample.Text = "Sample Image";
            this.sampleImageView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sampleImageView.Location = new System.Drawing.Point(3, 27);
            this.sampleImageView.Name = "sampleImageView";
            this.sampleImageView.Size = new System.Drawing.Size(638, 288);
            // 
            // grpReality
            // 
            this.grpReality.Controls.Add(this.realityImageView);
            this.grpReality.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpReality.Text = "Reality Image / Comparison Result";
            this.realityImageView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.realityImageView.Location = new System.Drawing.Point(3, 27);
            this.realityImageView.Name = "realityImageView";
            this.realityImageView.Size = new System.Drawing.Size(638, 294);
            // 
            // SampleComparisonControl
            // 
            this.Controls.Add(this.layoutMain);
            this.Name = "SampleComparisonControl";
            this.Size = new System.Drawing.Size(1000, 650);
            this.layoutMain.ResumeLayout(false);
            this.layoutLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudAlpha)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBlinkInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDifferenceGain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEdgeTolerance)).EndInit();
            this.splitPreview.Panel1.ResumeLayout(false);
            this.splitPreview.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitPreview)).EndInit();
            this.splitPreview.ResumeLayout(false);
            this.grpSample.ResumeLayout(false);
            this.grpReality.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
