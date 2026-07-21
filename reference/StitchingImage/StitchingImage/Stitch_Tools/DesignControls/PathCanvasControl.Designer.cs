namespace StitchingImage.Stitch_Tools.DesignControls
{
    partial class PathCanvasControl
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
            this.panelTop = new System.Windows.Forms.Panel();
            this.flowTop = new System.Windows.Forms.FlowLayoutPanel();
            this.chkShowArrange = new System.Windows.Forms.CheckBox();
            this.chkShowTraversal = new System.Windows.Forms.CheckBox();
            this.panelCanvas = new System.Windows.Forms.Panel();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.statusStripTraversal = new System.Windows.Forms.StatusStrip();
            this.statusLabelTraversal = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusLabelLegend = new System.Windows.Forms.ToolStripStatusLabel();
            this.panelTop.SuspendLayout();
            this.flowTop.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.statusStripTraversal.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.flowTop);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Margin = new System.Windows.Forms.Padding(2);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(315, 27);
            this.panelTop.TabIndex = 0;
            // 
            // flowTop
            // 
            this.flowTop.Controls.Add(this.chkShowArrange);
            this.flowTop.Controls.Add(this.chkShowTraversal);
            this.flowTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowTop.Location = new System.Drawing.Point(0, 0);
            this.flowTop.Margin = new System.Windows.Forms.Padding(2);
            this.flowTop.Name = "flowTop";
            this.flowTop.Padding = new System.Windows.Forms.Padding(6, 4, 6, 0);
            this.flowTop.Size = new System.Drawing.Size(315, 27);
            this.flowTop.TabIndex = 0;
            this.flowTop.WrapContents = false;
            // 
            // chkShowArrange
            // 
            this.chkShowArrange.AutoSize = true;
            this.chkShowArrange.Checked = true;
            this.chkShowArrange.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowArrange.ForeColor = System.Drawing.Color.ForestGreen;
            this.chkShowArrange.Location = new System.Drawing.Point(8, 6);
            this.chkShowArrange.Margin = new System.Windows.Forms.Padding(2);
            this.chkShowArrange.Name = "chkShowArrange";
            this.chkShowArrange.Size = new System.Drawing.Size(96, 16);
            this.chkShowArrange.TabIndex = 0;
            this.chkShowArrange.Text = "Arrange arrows";
            this.chkShowArrange.UseVisualStyleBackColor = true;
            // 
            // chkShowTraversal
            // 
            this.chkShowTraversal.AutoSize = true;
            this.chkShowTraversal.Checked = true;
            this.chkShowTraversal.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowTraversal.ForeColor = System.Drawing.Color.Red;
            this.chkShowTraversal.Location = new System.Drawing.Point(108, 6);
            this.chkShowTraversal.Margin = new System.Windows.Forms.Padding(2);
            this.chkShowTraversal.Name = "chkShowTraversal";
            this.chkShowTraversal.Size = new System.Drawing.Size(101, 16);
            this.chkShowTraversal.TabIndex = 1;
            this.chkShowTraversal.Text = "Traversal arrows";
            this.chkShowTraversal.UseVisualStyleBackColor = true;
            // 
            // panelCanvas
            // 
            this.panelCanvas.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.panelCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelCanvas.Location = new System.Drawing.Point(0, 27);
            this.panelCanvas.Margin = new System.Windows.Forms.Padding(2);
            this.panelCanvas.Name = "panelCanvas";
            this.panelCanvas.Size = new System.Drawing.Size(315, 225);
            this.panelCanvas.TabIndex = 1;
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.statusStripTraversal);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 252);
            this.panelBottom.Margin = new System.Windows.Forms.Padding(2);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(315, 33);
            this.panelBottom.TabIndex = 2;
            // 
            // statusStripTraversal
            // 
            this.statusStripTraversal.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStripTraversal.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabelTraversal,
            this.statusLabelLegend});
            this.statusStripTraversal.Location = new System.Drawing.Point(0, 11);
            this.statusStripTraversal.Name = "statusStripTraversal";
            this.statusStripTraversal.Padding = new System.Windows.Forms.Padding(1, 0, 10, 0);
            this.statusStripTraversal.ShowItemToolTips = true;
            this.statusStripTraversal.Size = new System.Drawing.Size(315, 22);
            this.statusStripTraversal.SizingGrip = false;
            this.statusStripTraversal.TabIndex = 1;
            this.statusStripTraversal.Text = "statusStripTraversal";
            // 
            // statusLabelTraversal
            // 
            this.statusLabelTraversal.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.statusLabelTraversal.Name = "statusLabelTraversal";
            this.statusLabelTraversal.Size = new System.Drawing.Size(52, 17);
            this.statusLabelTraversal.Text = "Traversal";
            // 
            // statusLabelLegend
            // 
            this.statusLabelLegend.BackColor = System.Drawing.Color.DarkSalmon;
            this.statusLabelLegend.Name = "statusLabelLegend";
            this.statusLabelLegend.Size = new System.Drawing.Size(46, 17);
            this.statusLabelLegend.Text = "Legend";
            // 
            // PathCanvasControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Controls.Add(this.panelCanvas);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelTop);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "PathCanvasControl";
            this.Size = new System.Drawing.Size(315, 285);
            this.panelTop.ResumeLayout(false);
            this.flowTop.ResumeLayout(false);
            this.flowTop.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            this.statusStripTraversal.ResumeLayout(false);
            this.statusStripTraversal.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.FlowLayoutPanel flowTop;
        private System.Windows.Forms.CheckBox chkShowArrange;
        private System.Windows.Forms.CheckBox chkShowTraversal;
        private System.Windows.Forms.Panel panelCanvas;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.StatusStrip statusStripTraversal;
        private System.Windows.Forms.ToolStripStatusLabel statusLabelTraversal;
        private System.Windows.Forms.ToolStripStatusLabel statusLabelLegend;
    }
}
