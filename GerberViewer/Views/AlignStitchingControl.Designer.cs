namespace GerberViewer.Views
{
    partial class AlignStitchingControl
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSampleRaster = new System.Windows.Forms.Label();
            this.lblLastOutput = new System.Windows.Forms.Label();
            this.btnOpenImageFolder = new System.Windows.Forms.Button();
            this.txtImageFolder = new System.Windows.Forms.TextBox();
            this.lstCapturedImages = new System.Windows.Forms.ListBox();
            this.lblImageCount = new System.Windows.Forms.Label();
            this.btnRunAlignStitch = new System.Windows.Forms.Button();
            this.btnCancelAlignStitch = new System.Windows.Forms.Button();
            this.prgAlignStitch = new System.Windows.Forms.ProgressBar();
            this.resultTabControl = new System.Windows.Forms.TabControl();
            this.tabOrderView = new System.Windows.Forms.TabPage();
            this.tabStitchedImage = new System.Windows.Forms.TabPage();
            this.orderPathCanvas = new GerberViewer.Stitching.DesignControls.PathCanvasControl();
            this.picStitchedImage = new System.Windows.Forms.PictureBox();
            this.resultTabControl.SuspendLayout();
            this.tabOrderView.SuspendLayout();
            this.tabStitchedImage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picStitchedImage)).BeginInit();
            this.SuspendLayout();
            this.lblTitle.AutoSize = true; this.lblTitle.Location = new System.Drawing.Point(16, 16); this.lblTitle.Name = "lblTitle"; this.lblTitle.Text = "Align and Stitching workflow";
            this.lblSampleRaster.AutoSize = true; this.lblSampleRaster.Location = new System.Drawing.Point(16, 44); this.lblSampleRaster.Name = "lblSampleRaster"; this.lblSampleRaster.Text = "Sample raster: -";
            this.lblLastOutput.AutoSize = true; this.lblLastOutput.Location = new System.Drawing.Point(16, 68); this.lblLastOutput.Name = "lblLastOutput"; this.lblLastOutput.Text = "Last stitched output: -";
            this.btnOpenImageFolder.Location = new System.Drawing.Point(16, 98); this.btnOpenImageFolder.Name = "btnOpenImageFolder"; this.btnOpenImageFolder.Size = new System.Drawing.Size(130, 28); this.btnOpenImageFolder.Text = "Open Image Folder"; this.btnOpenImageFolder.UseVisualStyleBackColor = true; this.btnOpenImageFolder.Click += new System.EventHandler(this.btnOpenImageFolder_Click);
            this.txtImageFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); this.txtImageFolder.Location = new System.Drawing.Point(152, 102); this.txtImageFolder.Name = "txtImageFolder"; this.txtImageFolder.ReadOnly = true; this.txtImageFolder.Size = new System.Drawing.Size(616, 22);
            this.lstCapturedImages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left))); this.lstCapturedImages.FormattingEnabled = true; this.lstCapturedImages.ItemHeight = 16; this.lstCapturedImages.Location = new System.Drawing.Point(16, 162); this.lstCapturedImages.Name = "lstCapturedImages"; this.lstCapturedImages.Size = new System.Drawing.Size(280, 292);
            this.lblImageCount.AutoSize = true; this.lblImageCount.Location = new System.Drawing.Point(16, 134); this.lblImageCount.Name = "lblImageCount"; this.lblImageCount.Text = "Images: 0";
            this.btnRunAlignStitch.Location = new System.Drawing.Point(152, 130); this.btnRunAlignStitch.Name = "btnRunAlignStitch"; this.btnRunAlignStitch.Size = new System.Drawing.Size(120, 28); this.btnRunAlignStitch.Text = "Run Align/Stitch"; this.btnRunAlignStitch.UseVisualStyleBackColor = true; this.btnRunAlignStitch.Click += new System.EventHandler(this.btnRunAlignStitch_Click);
            this.btnCancelAlignStitch.Enabled = false; this.btnCancelAlignStitch.Location = new System.Drawing.Point(278, 130); this.btnCancelAlignStitch.Name = "btnCancelAlignStitch"; this.btnCancelAlignStitch.Size = new System.Drawing.Size(80, 28); this.btnCancelAlignStitch.Text = "Cancel"; this.btnCancelAlignStitch.UseVisualStyleBackColor = true; this.btnCancelAlignStitch.Click += new System.EventHandler(this.btnCancelAlignStitch_Click);
            this.prgAlignStitch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); this.prgAlignStitch.Location = new System.Drawing.Point(364, 134); this.prgAlignStitch.Name = "prgAlignStitch"; this.prgAlignStitch.Size = new System.Drawing.Size(404, 20);
            this.resultTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right))); this.resultTabControl.Controls.Add(this.tabOrderView); this.resultTabControl.Controls.Add(this.tabStitchedImage); this.resultTabControl.Location = new System.Drawing.Point(302, 162); this.resultTabControl.Name = "resultTabControl"; this.resultTabControl.SelectedIndex = 0; this.resultTabControl.Size = new System.Drawing.Size(466, 292);
            this.tabOrderView.Controls.Add(this.orderPathCanvas); this.tabOrderView.Location = new System.Drawing.Point(4, 25); this.tabOrderView.Name = "tabOrderView"; this.tabOrderView.Text = "Order View"; this.tabOrderView.UseVisualStyleBackColor = true;
            this.orderPathCanvas.Dock = System.Windows.Forms.DockStyle.Fill; this.orderPathCanvas.Location = new System.Drawing.Point(0, 0); this.orderPathCanvas.Name = "orderPathCanvas"; this.orderPathCanvas.Size = new System.Drawing.Size(458, 263);
            this.tabStitchedImage.Controls.Add(this.picStitchedImage); this.tabStitchedImage.Location = new System.Drawing.Point(4, 25); this.tabStitchedImage.Name = "tabStitchedImage"; this.tabStitchedImage.Text = "Stitched Image"; this.tabStitchedImage.UseVisualStyleBackColor = true;
            this.picStitchedImage.Dock = System.Windows.Forms.DockStyle.Fill; this.picStitchedImage.Location = new System.Drawing.Point(0, 0); this.picStitchedImage.Name = "picStitchedImage"; this.picStitchedImage.Size = new System.Drawing.Size(458, 263); this.picStitchedImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.Controls.Add(this.resultTabControl); this.Controls.Add(this.prgAlignStitch); this.Controls.Add(this.btnCancelAlignStitch); this.Controls.Add(this.btnRunAlignStitch); this.Controls.Add(this.lblImageCount); this.Controls.Add(this.lstCapturedImages); this.Controls.Add(this.txtImageFolder); this.Controls.Add(this.btnOpenImageFolder); this.Controls.Add(this.lblLastOutput); this.Controls.Add(this.lblSampleRaster); this.Controls.Add(this.lblTitle); this.Name = "AlignStitchingControl"; this.Size = new System.Drawing.Size(800, 500);
            this.resultTabControl.ResumeLayout(false); this.tabOrderView.ResumeLayout(false); this.tabStitchedImage.ResumeLayout(false); ((System.ComponentModel.ISupportInitialize)(this.picStitchedImage)).EndInit(); this.ResumeLayout(false); this.PerformLayout();
        }
        private System.Windows.Forms.Label lblTitle, lblSampleRaster, lblLastOutput, lblImageCount; private System.Windows.Forms.Button btnOpenImageFolder, btnRunAlignStitch, btnCancelAlignStitch; private System.Windows.Forms.TextBox txtImageFolder; private System.Windows.Forms.ListBox lstCapturedImages; private System.Windows.Forms.ProgressBar prgAlignStitch; private System.Windows.Forms.TabControl resultTabControl; private System.Windows.Forms.TabPage tabOrderView, tabStitchedImage; private GerberViewer.Stitching.DesignControls.PathCanvasControl orderPathCanvas; private System.Windows.Forms.PictureBox picStitchedImage;
    }
}
