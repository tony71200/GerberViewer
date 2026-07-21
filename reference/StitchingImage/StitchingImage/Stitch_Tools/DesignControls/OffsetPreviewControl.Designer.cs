namespace StitchingImage.Stitch_Tools.DesignControls
{
    partial class OffsetPreviewControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.layoutMain = new System.Windows.Forms.TableLayoutPanel();
            this.splitPreview = new System.Windows.Forms.SplitContainer();
            this.grpHorizontal = new System.Windows.Forms.GroupBox();
            this.previewHorizontal = new StitchingImage.Stitch_Tools.DesignControls.ImagePreviewControl();
            this.grpVertical = new System.Windows.Forms.GroupBox();
            this.previewVertical = new StitchingImage.Stitch_Tools.DesignControls.ImagePreviewControl();
            this.tableLayoutPanel8 = new System.Windows.Forms.TableLayoutPanel();
            this.grB_MatchTime = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.txtHMatchTime = new System.Windows.Forms.TextBox();
            this.txtVMatchTime = new System.Windows.Forms.TextBox();
            this.grb_delta = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.txtHRobotDelta = new System.Windows.Forms.TextBox();
            this.txtVRobotDelta = new System.Windows.Forms.TextBox();
            this.grB_Overlap = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.txtHOverlap = new System.Windows.Forms.TextBox();
            this.txtVOverlap = new System.Windows.Forms.TextBox();
            this.groupBox_Rst = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label6 = new System.Windows.Forms.Label();
            this.txtVTheta = new System.Windows.Forms.TextBox();
            this.txtVTy = new System.Windows.Forms.TextBox();
            this.txtVTx = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtHTx = new System.Windows.Forms.TextBox();
            this.txtHTheta = new System.Windows.Forms.TextBox();
            this.txtHTy = new System.Windows.Forms.TextBox();
            this.gpB_Dist = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.txtHRobotDist = new System.Windows.Forms.TextBox();
            this.txtVRobotDist = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btn_Hmove = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox_Manual = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.nudHTx = new System.Windows.Forms.NumericUpDown();
            this.nudVTy = new System.Windows.Forms.NumericUpDown();
            this.nudVTx = new System.Windows.Forms.NumericUpDown();
            this.nudHTy = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbVertical = new System.Windows.Forms.ComboBox();
            this.cmbHorizontal = new System.Windows.Forms.ComboBox();
            this.cmbMode = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnDebug = new System.Windows.Forms.Button();
            this.btnRun = new System.Windows.Forms.Button();
            this.layoutMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitPreview)).BeginInit();
            this.splitPreview.Panel1.SuspendLayout();
            this.splitPreview.Panel2.SuspendLayout();
            this.splitPreview.SuspendLayout();
            this.grpHorizontal.SuspendLayout();
            this.grpVertical.SuspendLayout();
            this.tableLayoutPanel8.SuspendLayout();
            this.grB_MatchTime.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.grb_delta.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.grB_Overlap.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.groupBox_Rst.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.gpB_Dist.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.groupBox_Manual.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudHTx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVTy)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVTx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHTy)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutMain
            // 
            this.layoutMain.ColumnCount = 2;
            this.layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.layoutMain.Controls.Add(this.splitPreview, 1, 0);
            this.layoutMain.Controls.Add(this.tableLayoutPanel8, 0, 0);
            this.layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutMain.Location = new System.Drawing.Point(0, 0);
            this.layoutMain.Margin = new System.Windows.Forms.Padding(6);
            this.layoutMain.Name = "layoutMain";
            this.layoutMain.RowCount = 1;
            this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.layoutMain.Size = new System.Drawing.Size(1656, 833);
            this.layoutMain.TabIndex = 0;
            // 
            // splitPreview
            // 
            this.splitPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitPreview.Location = new System.Drawing.Point(585, 6);
            this.splitPreview.Margin = new System.Windows.Forms.Padding(6);
            this.splitPreview.Name = "splitPreview";
            this.splitPreview.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitPreview.Panel1
            // 
            this.splitPreview.Panel1.Controls.Add(this.grpHorizontal);
            this.splitPreview.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            // 
            // splitPreview.Panel2
            // 
            this.splitPreview.Panel2.Controls.Add(this.grpVertical);
            this.splitPreview.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.splitPreview.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.splitPreview.Size = new System.Drawing.Size(1065, 821);
            this.splitPreview.SplitterDistance = 408;
            this.splitPreview.SplitterIncrement = 2;
            this.splitPreview.SplitterWidth = 8;
            this.splitPreview.TabIndex = 1;
            // 
            // grpHorizontal
            // 
            this.grpHorizontal.Controls.Add(this.previewHorizontal);
            this.grpHorizontal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpHorizontal.Location = new System.Drawing.Point(0, 0);
            this.grpHorizontal.Margin = new System.Windows.Forms.Padding(6);
            this.grpHorizontal.Name = "grpHorizontal";
            this.grpHorizontal.Padding = new System.Windows.Forms.Padding(6);
            this.grpHorizontal.Size = new System.Drawing.Size(1065, 408);
            this.grpHorizontal.TabIndex = 0;
            this.grpHorizontal.TabStop = false;
            this.grpHorizontal.Text = "Horizontal Preview";
            // 
            // previewHorizontal
            // 
            this.previewHorizontal.BackColor = System.Drawing.Color.DarkGray;
            this.previewHorizontal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previewHorizontal.Location = new System.Drawing.Point(6, 30);
            this.previewHorizontal.ManualMode = false;
            this.previewHorizontal.ManualScaleToFull = 1D;
            this.previewHorizontal.Margin = new System.Windows.Forms.Padding(6);
            this.previewHorizontal.Name = "previewHorizontal";
            this.previewHorizontal.Size = new System.Drawing.Size(1053, 372);
            this.previewHorizontal.TabIndex = 0;
            // 
            // grpVertical
            // 
            this.grpVertical.Controls.Add(this.previewVertical);
            this.grpVertical.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpVertical.Location = new System.Drawing.Point(0, 0);
            this.grpVertical.Margin = new System.Windows.Forms.Padding(6);
            this.grpVertical.Name = "grpVertical";
            this.grpVertical.Padding = new System.Windows.Forms.Padding(6);
            this.grpVertical.Size = new System.Drawing.Size(1065, 405);
            this.grpVertical.TabIndex = 1;
            this.grpVertical.TabStop = false;
            this.grpVertical.Text = "Vertical Preview";
            // 
            // previewVertical
            // 
            this.previewVertical.BackColor = System.Drawing.Color.DarkGray;
            this.previewVertical.Dock = System.Windows.Forms.DockStyle.Fill;
            this.previewVertical.Location = new System.Drawing.Point(6, 30);
            this.previewVertical.ManualMode = false;
            this.previewVertical.ManualScaleToFull = 1D;
            this.previewVertical.Margin = new System.Windows.Forms.Padding(6);
            this.previewVertical.Name = "previewVertical";
            this.previewVertical.Size = new System.Drawing.Size(1053, 369);
            this.previewVertical.TabIndex = 0;
            // 
            // tableLayoutPanel8
            // 
            this.tableLayoutPanel8.AutoScroll = true;
            this.tableLayoutPanel8.ColumnCount = 5;
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel8.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel8.Controls.Add(this.grB_MatchTime, 3, 10);
            this.tableLayoutPanel8.Controls.Add(this.grb_delta, 0, 6);
            this.tableLayoutPanel8.Controls.Add(this.grB_Overlap, 0, 10);
            this.tableLayoutPanel8.Controls.Add(this.groupBox_Rst, 3, 4);
            this.tableLayoutPanel8.Controls.Add(this.gpB_Dist, 0, 8);
            this.tableLayoutPanel8.Controls.Add(this.lblStatus, 3, 1);
            this.tableLayoutPanel8.Controls.Add(this.btn_Hmove, 2, 4);
            this.tableLayoutPanel8.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel8.Controls.Add(this.groupBox_Manual, 0, 4);
            this.tableLayoutPanel8.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel8.Controls.Add(this.cmbVertical, 1, 3);
            this.tableLayoutPanel8.Controls.Add(this.cmbHorizontal, 1, 2);
            this.tableLayoutPanel8.Controls.Add(this.cmbMode, 1, 0);
            this.tableLayoutPanel8.Controls.Add(this.label3, 0, 3);
            this.tableLayoutPanel8.Controls.Add(this.btnDebug, 4, 0);
            this.tableLayoutPanel8.Controls.Add(this.btnRun, 3, 0);
            this.tableLayoutPanel8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel8.Location = new System.Drawing.Point(6, 6);
            this.tableLayoutPanel8.Margin = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel8.Name = "tableLayoutPanel8";
            this.tableLayoutPanel8.RowCount = 12;
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel8.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel8.Size = new System.Drawing.Size(567, 821);
            this.tableLayoutPanel8.TabIndex = 2;
            // 
            // grB_MatchTime
            // 
            this.tableLayoutPanel8.SetColumnSpan(this.grB_MatchTime, 2);
            this.grB_MatchTime.Controls.Add(this.tableLayoutPanel7);
            this.grB_MatchTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grB_MatchTime.Location = new System.Drawing.Point(309, 823);
            this.grB_MatchTime.Margin = new System.Windows.Forms.Padding(6);
            this.grB_MatchTime.Name = "grB_MatchTime";
            this.grB_MatchTime.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel8.SetRowSpan(this.grB_MatchTime, 2);
            this.grB_MatchTime.Size = new System.Drawing.Size(252, 208);
            this.grB_MatchTime.TabIndex = 39;
            this.grB_MatchTime.TabStop = false;
            this.grB_MatchTime.Text = "Match Time (s)";
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 1;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel7.Controls.Add(this.txtHMatchTime, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.txtVMatchTime, 0, 1);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(6, 30);
            this.tableLayoutPanel7.Margin = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 2;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(240, 172);
            this.tableLayoutPanel7.TabIndex = 0;
            // 
            // txtHMatchTime
            // 
            this.txtHMatchTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtHMatchTime.Location = new System.Drawing.Point(6, 6);
            this.txtHMatchTime.Margin = new System.Windows.Forms.Padding(6);
            this.txtHMatchTime.Name = "txtHMatchTime";
            this.txtHMatchTime.ReadOnly = true;
            this.txtHMatchTime.Size = new System.Drawing.Size(228, 31);
            this.txtHMatchTime.TabIndex = 33;
            // 
            // txtVMatchTime
            // 
            this.txtVMatchTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVMatchTime.Location = new System.Drawing.Point(6, 92);
            this.txtVMatchTime.Margin = new System.Windows.Forms.Padding(6);
            this.txtVMatchTime.Name = "txtVMatchTime";
            this.txtVMatchTime.ReadOnly = true;
            this.txtVMatchTime.Size = new System.Drawing.Size(228, 31);
            this.txtVMatchTime.TabIndex = 34;
            // 
            // grb_delta
            // 
            this.tableLayoutPanel8.SetColumnSpan(this.grb_delta, 2);
            this.grb_delta.Controls.Add(this.tableLayoutPanel4);
            this.grb_delta.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grb_delta.Location = new System.Drawing.Point(6, 383);
            this.grb_delta.Margin = new System.Windows.Forms.Padding(6);
            this.grb_delta.Name = "grb_delta";
            this.grb_delta.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel8.SetRowSpan(this.grb_delta, 2);
            this.grb_delta.Size = new System.Drawing.Size(251, 208);
            this.grb_delta.TabIndex = 36;
            this.grb_delta.TabStop = false;
            this.grb_delta.Text = "Robot Δ (dx, dy)";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 1;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Controls.Add(this.txtHRobotDelta, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.txtVRobotDelta, 0, 1);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(6, 30);
            this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 2;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(239, 172);
            this.tableLayoutPanel4.TabIndex = 0;
            // 
            // txtHRobotDelta
            // 
            this.txtHRobotDelta.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtHRobotDelta.Location = new System.Drawing.Point(6, 6);
            this.txtHRobotDelta.Margin = new System.Windows.Forms.Padding(6);
            this.txtHRobotDelta.Name = "txtHRobotDelta";
            this.txtHRobotDelta.ReadOnly = true;
            this.txtHRobotDelta.Size = new System.Drawing.Size(227, 31);
            this.txtHRobotDelta.TabIndex = 24;
            // 
            // txtVRobotDelta
            // 
            this.txtVRobotDelta.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVRobotDelta.Location = new System.Drawing.Point(6, 92);
            this.txtVRobotDelta.Margin = new System.Windows.Forms.Padding(6);
            this.txtVRobotDelta.Name = "txtVRobotDelta";
            this.txtVRobotDelta.ReadOnly = true;
            this.txtVRobotDelta.Size = new System.Drawing.Size(227, 31);
            this.txtVRobotDelta.TabIndex = 25;
            // 
            // grB_Overlap
            // 
            this.tableLayoutPanel8.SetColumnSpan(this.grB_Overlap, 2);
            this.grB_Overlap.Controls.Add(this.tableLayoutPanel6);
            this.grB_Overlap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grB_Overlap.Location = new System.Drawing.Point(6, 823);
            this.grB_Overlap.Margin = new System.Windows.Forms.Padding(6);
            this.grB_Overlap.Name = "grB_Overlap";
            this.grB_Overlap.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel8.SetRowSpan(this.grB_Overlap, 2);
            this.grB_Overlap.Size = new System.Drawing.Size(251, 208);
            this.grB_Overlap.TabIndex = 38;
            this.grB_Overlap.TabStop = false;
            this.grB_Overlap.Text = "Overlap (%)";
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 1;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel6.Controls.Add(this.txtHOverlap, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.txtVOverlap, 0, 1);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(6, 30);
            this.tableLayoutPanel6.Margin = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 2;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(239, 172);
            this.tableLayoutPanel6.TabIndex = 0;
            // 
            // txtHOverlap
            // 
            this.txtHOverlap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtHOverlap.Location = new System.Drawing.Point(6, 6);
            this.txtHOverlap.Margin = new System.Windows.Forms.Padding(6);
            this.txtHOverlap.Name = "txtHOverlap";
            this.txtHOverlap.ReadOnly = true;
            this.txtHOverlap.Size = new System.Drawing.Size(227, 31);
            this.txtHOverlap.TabIndex = 30;
            // 
            // txtVOverlap
            // 
            this.txtVOverlap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVOverlap.Location = new System.Drawing.Point(6, 92);
            this.txtVOverlap.Margin = new System.Windows.Forms.Padding(6);
            this.txtVOverlap.Name = "txtVOverlap";
            this.txtVOverlap.ReadOnly = true;
            this.txtVOverlap.Size = new System.Drawing.Size(227, 31);
            this.txtVOverlap.TabIndex = 31;
            // 
            // groupBox_Rst
            // 
            this.tableLayoutPanel8.SetColumnSpan(this.groupBox_Rst, 2);
            this.groupBox_Rst.Controls.Add(this.tableLayoutPanel2);
            this.groupBox_Rst.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox_Rst.Location = new System.Drawing.Point(309, 204);
            this.groupBox_Rst.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox_Rst.Name = "groupBox_Rst";
            this.groupBox_Rst.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel8.SetRowSpan(this.groupBox_Rst, 2);
            this.groupBox_Rst.Size = new System.Drawing.Size(252, 167);
            this.groupBox_Rst.TabIndex = 1;
            this.groupBox_Rst.TabStop = false;
            this.groupBox_Rst.Text = "Result Tx/Ty/Theta";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel2.Controls.Add(this.label6, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.txtVTheta, 3, 1);
            this.tableLayoutPanel2.Controls.Add(this.txtVTy, 2, 1);
            this.tableLayoutPanel2.Controls.Add(this.txtVTx, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.label7, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.txtHTx, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.txtHTheta, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.txtHTy, 2, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(6, 38);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(240, 123);
            this.tableLayoutPanel2.TabIndex = 21;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(6, 0);
            this.label6.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 61);
            this.label6.TabIndex = 12;
            this.label6.Text = "Horizontal";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtVTheta
            // 
            this.txtVTheta.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVTheta.Location = new System.Drawing.Point(186, 67);
            this.txtVTheta.Margin = new System.Windows.Forms.Padding(6);
            this.txtVTheta.Name = "txtVTheta";
            this.txtVTheta.ReadOnly = true;
            this.txtVTheta.Size = new System.Drawing.Size(48, 31);
            this.txtVTheta.TabIndex = 20;
            // 
            // txtVTy
            // 
            this.txtVTy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVTy.Location = new System.Drawing.Point(126, 67);
            this.txtVTy.Margin = new System.Windows.Forms.Padding(6);
            this.txtVTy.Name = "txtVTy";
            this.txtVTy.ReadOnly = true;
            this.txtVTy.Size = new System.Drawing.Size(48, 31);
            this.txtVTy.TabIndex = 19;
            // 
            // txtVTx
            // 
            this.txtVTx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVTx.Location = new System.Drawing.Point(66, 67);
            this.txtVTx.Margin = new System.Windows.Forms.Padding(6);
            this.txtVTx.Name = "txtVTx";
            this.txtVTx.ReadOnly = true;
            this.txtVTx.Size = new System.Drawing.Size(48, 31);
            this.txtVTx.TabIndex = 18;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label7.Location = new System.Drawing.Point(6, 61);
            this.label7.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(48, 62);
            this.label7.TabIndex = 13;
            this.label7.Text = "Vertical";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtHTx
            // 
            this.txtHTx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtHTx.Location = new System.Drawing.Point(66, 6);
            this.txtHTx.Margin = new System.Windows.Forms.Padding(6);
            this.txtHTx.Name = "txtHTx";
            this.txtHTx.ReadOnly = true;
            this.txtHTx.Size = new System.Drawing.Size(48, 31);
            this.txtHTx.TabIndex = 15;
            // 
            // txtHTheta
            // 
            this.txtHTheta.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtHTheta.Location = new System.Drawing.Point(186, 6);
            this.txtHTheta.Margin = new System.Windows.Forms.Padding(6);
            this.txtHTheta.Name = "txtHTheta";
            this.txtHTheta.ReadOnly = true;
            this.txtHTheta.Size = new System.Drawing.Size(48, 31);
            this.txtHTheta.TabIndex = 17;
            // 
            // txtHTy
            // 
            this.txtHTy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtHTy.Location = new System.Drawing.Point(126, 6);
            this.txtHTy.Margin = new System.Windows.Forms.Padding(6);
            this.txtHTy.Name = "txtHTy";
            this.txtHTy.ReadOnly = true;
            this.txtHTy.Size = new System.Drawing.Size(48, 31);
            this.txtHTy.TabIndex = 16;
            // 
            // gpB_Dist
            // 
            this.tableLayoutPanel8.SetColumnSpan(this.gpB_Dist, 2);
            this.gpB_Dist.Controls.Add(this.tableLayoutPanel5);
            this.gpB_Dist.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gpB_Dist.Location = new System.Drawing.Point(6, 603);
            this.gpB_Dist.Margin = new System.Windows.Forms.Padding(6);
            this.gpB_Dist.Name = "gpB_Dist";
            this.gpB_Dist.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel8.SetRowSpan(this.gpB_Dist, 2);
            this.gpB_Dist.Size = new System.Drawing.Size(251, 208);
            this.gpB_Dist.TabIndex = 37;
            this.gpB_Dist.TabStop = false;
            this.gpB_Dist.Text = "Robot Distance";
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 1;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.Controls.Add(this.txtHRobotDist, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.txtVRobotDist, 0, 1);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(6, 30);
            this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 2;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(239, 172);
            this.tableLayoutPanel5.TabIndex = 0;
            // 
            // txtHRobotDist
            // 
            this.txtHRobotDist.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtHRobotDist.Location = new System.Drawing.Point(6, 6);
            this.txtHRobotDist.Margin = new System.Windows.Forms.Padding(6);
            this.txtHRobotDist.Name = "txtHRobotDist";
            this.txtHRobotDist.ReadOnly = true;
            this.txtHRobotDist.Size = new System.Drawing.Size(227, 31);
            this.txtHRobotDist.TabIndex = 27;
            // 
            // txtVRobotDist
            // 
            this.txtVRobotDist.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtVRobotDist.Location = new System.Drawing.Point(6, 92);
            this.txtVRobotDist.Margin = new System.Windows.Forms.Padding(6);
            this.txtVRobotDist.Name = "txtVRobotDist";
            this.txtVRobotDist.ReadOnly = true;
            this.txtVRobotDist.Size = new System.Drawing.Size(227, 31);
            this.txtVRobotDist.TabIndex = 28;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.tableLayoutPanel8.SetColumnSpan(this.lblStatus, 2);
            this.lblStatus.Location = new System.Drawing.Point(309, 52);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(74, 25);
            this.lblStatus.TabIndex = 22;
            this.lblStatus.Text = "Ready";
            // 
            // btn_Hmove
            // 
            this.btn_Hmove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Hmove.Location = new System.Drawing.Point(269, 204);
            this.btn_Hmove.Margin = new System.Windows.Forms.Padding(6);
            this.btn_Hmove.Name = "btn_Hmove";
            this.tableLayoutPanel8.SetRowSpan(this.btn_Hmove, 2);
            this.btn_Hmove.Size = new System.Drawing.Size(28, 167);
            this.btn_Hmove.TabIndex = 35;
            this.btn_Hmove.Text = "<-";
            this.btn_Hmove.UseVisualStyleBackColor = true;
            this.btn_Hmove.Click += new System.EventHandler(this.btn_Hmove_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Mode";
            // 
            // groupBox_Manual
            // 
            this.groupBox_Manual.AutoSize = true;
            this.tableLayoutPanel8.SetColumnSpan(this.groupBox_Manual, 2);
            this.groupBox_Manual.Controls.Add(this.tableLayoutPanel1);
            this.groupBox_Manual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox_Manual.Location = new System.Drawing.Point(6, 204);
            this.groupBox_Manual.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox_Manual.Name = "groupBox_Manual";
            this.groupBox_Manual.Padding = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel8.SetRowSpan(this.groupBox_Manual, 2);
            this.groupBox_Manual.Size = new System.Drawing.Size(251, 167);
            this.groupBox_Manual.TabIndex = 1;
            this.groupBox_Manual.TabStop = false;
            this.groupBox_Manual.Text = "Manual Tx/Ty";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.nudHTx, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.nudVTy, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.nudVTx, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.nudHTy, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 30);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(239, 131);
            this.tableLayoutPanel1.TabIndex = 11;
            // 
            // nudHTx
            // 
            this.nudHTx.DecimalPlaces = 2;
            this.nudHTx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudHTx.Location = new System.Drawing.Point(6, 6);
            this.nudHTx.Margin = new System.Windows.Forms.Padding(6);
            this.nudHTx.Maximum = new decimal(new int[] {
            200000,
            0,
            0,
            0});
            this.nudHTx.Minimum = new decimal(new int[] {
            200000,
            0,
            0,
            -2147483648});
            this.nudHTx.Name = "nudHTx";
            this.nudHTx.Size = new System.Drawing.Size(107, 31);
            this.nudHTx.TabIndex = 7;
            // 
            // nudVTy
            // 
            this.nudVTy.DecimalPlaces = 2;
            this.nudVTy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudVTy.Location = new System.Drawing.Point(125, 49);
            this.nudVTy.Margin = new System.Windows.Forms.Padding(6);
            this.nudVTy.Maximum = new decimal(new int[] {
            200000,
            0,
            0,
            0});
            this.nudVTy.Minimum = new decimal(new int[] {
            200000,
            0,
            0,
            -2147483648});
            this.nudVTy.Name = "nudVTy";
            this.nudVTy.Size = new System.Drawing.Size(108, 31);
            this.nudVTy.TabIndex = 10;
            // 
            // nudVTx
            // 
            this.nudVTx.DecimalPlaces = 2;
            this.nudVTx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudVTx.Location = new System.Drawing.Point(6, 49);
            this.nudVTx.Margin = new System.Windows.Forms.Padding(6);
            this.nudVTx.Maximum = new decimal(new int[] {
            200000,
            0,
            0,
            0});
            this.nudVTx.Minimum = new decimal(new int[] {
            200000,
            0,
            0,
            -2147483648});
            this.nudVTx.Name = "nudVTx";
            this.nudVTx.Size = new System.Drawing.Size(107, 31);
            this.nudVTx.TabIndex = 9;
            // 
            // nudHTy
            // 
            this.nudHTy.DecimalPlaces = 2;
            this.nudHTy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nudHTy.Location = new System.Drawing.Point(125, 6);
            this.nudHTy.Margin = new System.Windows.Forms.Padding(6);
            this.nudHTy.Maximum = new decimal(new int[] {
            200000,
            0,
            0,
            0});
            this.nudHTy.Minimum = new decimal(new int[] {
            200000,
            0,
            0,
            -2147483648});
            this.nudHTy.Name = "nudHTy";
            this.nudHTy.Size = new System.Drawing.Size(108, 31);
            this.nudHTy.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 94);
            this.label2.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 50);
            this.label2.TabIndex = 2;
            this.label2.Text = "Horizontal";
            // 
            // cmbVertical
            // 
            this.tableLayoutPanel8.SetColumnSpan(this.cmbVertical, 4);
            this.cmbVertical.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbVertical.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbVertical.FormattingEnabled = true;
            this.cmbVertical.Location = new System.Drawing.Point(126, 152);
            this.cmbVertical.Margin = new System.Windows.Forms.Padding(6);
            this.cmbVertical.Name = "cmbVertical";
            this.cmbVertical.Size = new System.Drawing.Size(435, 33);
            this.cmbVertical.TabIndex = 4;
            // 
            // cmbHorizontal
            // 
            this.tableLayoutPanel8.SetColumnSpan(this.cmbHorizontal, 4);
            this.cmbHorizontal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbHorizontal.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHorizontal.FormattingEnabled = true;
            this.cmbHorizontal.Location = new System.Drawing.Point(126, 100);
            this.cmbHorizontal.Margin = new System.Windows.Forms.Padding(6);
            this.cmbHorizontal.Name = "cmbHorizontal";
            this.cmbHorizontal.Size = new System.Drawing.Size(435, 33);
            this.cmbHorizontal.TabIndex = 3;
            // 
            // cmbMode
            // 
            this.cmbMode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbMode.FormattingEnabled = true;
            this.cmbMode.Items.AddRange(new object[] {
            "Auto",
            "Manual"});
            this.cmbMode.Location = new System.Drawing.Point(126, 6);
            this.cmbMode.Margin = new System.Windows.Forms.Padding(6);
            this.cmbMode.Name = "cmbMode";
            this.cmbMode.Size = new System.Drawing.Size(131, 33);
            this.cmbMode.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 146);
            this.label3.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 25);
            this.label3.TabIndex = 5;
            this.label3.Text = "Vertical";
            // 
            // btnDebug
            // 
            this.btnDebug.Location = new System.Drawing.Point(429, 6);
            this.btnDebug.Margin = new System.Windows.Forms.Padding(6);
            this.btnDebug.Name = "btnDebug";
            this.btnDebug.Size = new System.Drawing.Size(115, 40);
            this.btnDebug.TabIndex = 22;
            this.btnDebug.Text = "Debug";
            this.btnDebug.UseVisualStyleBackColor = true;
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(309, 6);
            this.btnRun.Margin = new System.Windows.Forms.Padding(6);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(108, 40);
            this.btnRun.TabIndex = 21;
            this.btnRun.Text = "Run Auto";
            this.btnRun.UseVisualStyleBackColor = true;
            // 
            // OffsetPreviewControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.layoutMain);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "OffsetPreviewControl";
            this.Size = new System.Drawing.Size(1656, 833);
            this.layoutMain.ResumeLayout(false);
            this.splitPreview.Panel1.ResumeLayout(false);
            this.splitPreview.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitPreview)).EndInit();
            this.splitPreview.ResumeLayout(false);
            this.grpHorizontal.ResumeLayout(false);
            this.grpVertical.ResumeLayout(false);
            this.tableLayoutPanel8.ResumeLayout(false);
            this.tableLayoutPanel8.PerformLayout();
            this.grB_MatchTime.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            this.grb_delta.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.grB_Overlap.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel6.PerformLayout();
            this.groupBox_Rst.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.gpB_Dist.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.groupBox_Manual.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudHTx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVTy)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudVTx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHTy)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel layoutMain;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnDebug;
        private System.Windows.Forms.ComboBox cmbMode;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbHorizontal;
        private System.Windows.Forms.ComboBox cmbVertical;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nudHTx;
        private System.Windows.Forms.NumericUpDown nudHTy;
        private System.Windows.Forms.NumericUpDown nudVTx;
        private System.Windows.Forms.NumericUpDown nudVTy;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtHTx;
        private System.Windows.Forms.TextBox txtHTy;
        private System.Windows.Forms.TextBox txtHTheta;
        private System.Windows.Forms.TextBox txtVTx;
        private System.Windows.Forms.TextBox txtVTy;
        private System.Windows.Forms.TextBox txtVTheta;
        private System.Windows.Forms.TextBox txtHRobotDelta;
        private System.Windows.Forms.TextBox txtVRobotDelta;
        private System.Windows.Forms.TextBox txtHRobotDist;
        private System.Windows.Forms.TextBox txtVRobotDist;
        private System.Windows.Forms.TextBox txtHOverlap;
        private System.Windows.Forms.TextBox txtVOverlap;
        private System.Windows.Forms.TextBox txtHMatchTime;
        private System.Windows.Forms.TextBox txtVMatchTime;
        private System.Windows.Forms.SplitContainer splitPreview;
        private System.Windows.Forms.GroupBox grpHorizontal;
        private ImagePreviewControl previewHorizontal;
        private System.Windows.Forms.GroupBox grpVertical;
        private ImagePreviewControl previewVertical;
        private System.Windows.Forms.GroupBox groupBox_Manual;
        private System.Windows.Forms.GroupBox groupBox_Rst;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btn_Hmove;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.GroupBox grb_delta;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.GroupBox grB_MatchTime;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.GroupBox gpB_Dist;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.GroupBox grB_Overlap;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel8;
    }
}
