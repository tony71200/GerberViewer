namespace GerberViewer.Stitching.DesignControls
{
    public sealed partial class PathCanvasControl
    {
        private System.ComponentModel.IContainer components = null;
        private BufferedPanel canvasPanel;
        private System.Windows.Forms.CheckBox chkExpectedOrder;
        private System.Windows.Forms.Label legendLabel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.canvasPanel = new BufferedPanel();
            this.chkExpectedOrder = new System.Windows.Forms.CheckBox();
            this.legendLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // canvasPanel
            // 
            this.canvasPanel.BackColor = System.Drawing.Color.White;
            this.canvasPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvasPanel.Location = new System.Drawing.Point(0, 48);
            this.canvasPanel.Name = "canvasPanel";
            this.canvasPanel.Size = new System.Drawing.Size(420, 252);
            this.canvasPanel.TabIndex = 0;
            this.canvasPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.canvasPanel_Paint);
            this.canvasPanel.MouseClick += new System.Windows.Forms.MouseEventHandler(this.canvasPanel_MouseClick);
            // 
            // chkExpectedOrder
            // 
            this.chkExpectedOrder.Checked = true;
            this.chkExpectedOrder.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkExpectedOrder.Dock = System.Windows.Forms.DockStyle.Top;
            this.chkExpectedOrder.Location = new System.Drawing.Point(0, 24);
            this.chkExpectedOrder.Name = "chkExpectedOrder";
            this.chkExpectedOrder.Size = new System.Drawing.Size(420, 24);
            this.chkExpectedOrder.TabIndex = 1;
            this.chkExpectedOrder.Text = "Expected order arrows";
            this.chkExpectedOrder.CheckedChanged += new System.EventHandler(this.chkExpectedOrder_CheckedChanged);
            // 
            // legendLabel
            // 
            this.legendLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.legendLabel.Location = new System.Drawing.Point(0, 0);
            this.legendLabel.Name = "legendLabel";
            this.legendLabel.Size = new System.Drawing.Size(420, 24);
            this.legendLabel.TabIndex = 2;
            this.legendLabel.Text = "Expected | Traversal | Neighbor recovery | Interpolation anchors | Final states";
            // 
            // PathCanvasControl
            // 
            this.Controls.Add(this.canvasPanel);
            this.Controls.Add(this.chkExpectedOrder);
            this.Controls.Add(this.legendLabel);
            this.Name = "PathCanvasControl";
            this.Size = new System.Drawing.Size(420, 300);
            this.ResumeLayout(false);
        }
    }
}
