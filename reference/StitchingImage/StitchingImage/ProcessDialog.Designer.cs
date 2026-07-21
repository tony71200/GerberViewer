namespace StitchingImage
{
    partial class ProcessDialog
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._lblTitle = new System.Windows.Forms.Label();
            this._lblGroup = new System.Windows.Forms.Label();
            this._lblOrder = new System.Windows.Forms.Label();
            this._lblImages = new System.Windows.Forms.Label();
            this._lblElapsed = new System.Windows.Forms.Label();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this.listB_Processing = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // _lblTitle
            // 
            this._lblTitle.AutoSize = true;
            this._lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._lblTitle.Location = new System.Drawing.Point(9, 8);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(114, 15);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "Stitching images";
            // 
            // _lblGroup
            // 
            this._lblGroup.AutoSize = true;
            this._lblGroup.Location = new System.Drawing.Point(10, 28);
            this._lblGroup.Name = "_lblGroup";
            this._lblGroup.Size = new System.Drawing.Size(45, 12);
            this._lblGroup.TabIndex = 1;
            this._lblGroup.Text = "Group: -";
            // 
            // _lblOrder
            // 
            this._lblOrder.AutoSize = true;
            this._lblOrder.Location = new System.Drawing.Point(10, 45);
            this._lblOrder.Name = "_lblOrder";
            this._lblOrder.Size = new System.Drawing.Size(42, 12);
            this._lblOrder.TabIndex = 2;
            this._lblOrder.Text = "Order: -";
            // 
            // _lblImages
            // 
            this._lblImages.AutoSize = true;
            this._lblImages.Location = new System.Drawing.Point(10, 61);
            this._lblImages.Name = "_lblImages";
            this._lblImages.Size = new System.Drawing.Size(48, 12);
            this._lblImages.TabIndex = 3;
            this._lblImages.Text = "Images: -";
            // 
            // _lblElapsed
            // 
            this._lblElapsed.AutoSize = true;
            this._lblElapsed.Location = new System.Drawing.Point(10, 78);
            this._lblElapsed.Name = "_lblElapsed";
            this._lblElapsed.Size = new System.Drawing.Size(51, 12);
            this._lblElapsed.TabIndex = 4;
            this._lblElapsed.Text = "Elapsed: -";
            // 
            // _progressBar
            // 
            this._progressBar.Location = new System.Drawing.Point(12, 98);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(282, 14);
            this._progressBar.TabIndex = 5;
            // 
            // listB_Processing
            // 
            this.listB_Processing.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listB_Processing.FormattingEnabled = true;
            this.listB_Processing.HorizontalScrollbar = true;
            this.listB_Processing.ItemHeight = 12;
            this.listB_Processing.Location = new System.Drawing.Point(12, 119);
            this.listB_Processing.Name = "listB_Processing";
            this.listB_Processing.Size = new System.Drawing.Size(282, 124);
            this.listB_Processing.TabIndex = 6;
            // 
            // ProcessDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(306, 252);
            this.Controls.Add(this.listB_Processing);
            this.Controls.Add(this._progressBar);
            this.Controls.Add(this._lblElapsed);
            this.Controls.Add(this._lblImages);
            this.Controls.Add(this._lblOrder);
            this.Controls.Add(this._lblGroup);
            this.Controls.Add(this._lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProcessDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Processing";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblGroup;
        private System.Windows.Forms.Label _lblOrder;
        private System.Windows.Forms.Label _lblImages;
        private System.Windows.Forms.Label _lblElapsed;
        private System.Windows.Forms.ProgressBar _progressBar;
        private System.Windows.Forms.ListBox listB_Processing;
    }
}
