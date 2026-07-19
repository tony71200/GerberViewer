namespace GerberViewer
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.tsbOpen = new System.Windows.Forms.ToolStripButton();
            this.tslDpi = new System.Windows.Forms.ToolStripLabel();
            this.tscDpi = new System.Windows.Forms.ToolStripComboBox();
            this.tslMode = new System.Windows.Forms.ToolStripLabel();
            this.tscMode = new System.Windows.Forms.ToolStripComboBox();
            this.tsbRender = new System.Windows.Forms.ToolStripButton();
            this.tsbFit = new System.Windows.Forms.ToolStripButton();
            this.tsbExportSelected = new System.Windows.Forms.ToolStripButton();
            this.tsbExportCombined = new System.Windows.Forms.ToolStripButton();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.lvLayers = new System.Windows.Forms.ListView();
            this.colLayerName = new System.Windows.Forms.ColumnHeader();
            this.colLayerType = new System.Windows.Forms.ColumnHeader();
            this.imgColors = new System.Windows.Forms.ImageList(this.components);
            this.ctxLayers = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.miChangeColor = new System.Windows.Forms.ToolStripMenuItem();
            this.miMoveUp = new System.Windows.Forms.ToolStripMenuItem();
            this.miMoveDown = new System.Windows.Forms.ToolStripMenuItem();
            this.miRemove = new System.Windows.Forms.ToolStripMenuItem();
            this.canvas = new GerberViewer.GerberCanvas();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblBoardSize = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblCoords = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblZoom = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.ctxLayers.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // toolStrip
            //
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.tsbOpen,
                new System.Windows.Forms.ToolStripSeparator(),
                this.tslDpi, this.tscDpi,
                this.tslMode, this.tscMode,
                new System.Windows.Forms.ToolStripSeparator(),
                this.tsbRender, this.tsbFit,
                new System.Windows.Forms.ToolStripSeparator(),
                this.tsbExportSelected, this.tsbExportCombined});
            this.toolStrip.Name = "toolStrip";
            //
            // tsbOpen
            //
            this.tsbOpen.Text = "Open Gerber...";
            this.tsbOpen.Click += new System.EventHandler(this.tsbOpen_Click);
            //
            // tscDpi / tscMode
            //
            this.tslDpi.Text = "DPI:";
            this.tscDpi.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscDpi.Items.AddRange(new object[] { "150", "300", "600", "1200", "3600", "4800"});
            this.tscDpi.Size = new System.Drawing.Size(70, 25);
            this.tslMode.Text = "Mau:";
            this.tscMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.tscMode.Items.AddRange(new object[] { "Realistic", "Binary Mask" });
            this.tscMode.Size = new System.Drawing.Size(100, 25);
            //
            // tsbRender / tsbFit / export
            //
            this.tsbRender.Text = "Render Preview";
            this.tsbRender.Click += new System.EventHandler(this.tsbRender_Click);
            this.tsbFit.Text = "Fit";
            this.tsbFit.Click += new System.EventHandler(this.tsbFit_Click);
            this.tsbExportSelected.Text = "Export Selected PNG";
            this.tsbExportSelected.Click += new System.EventHandler(this.tsbExportSelected_Click);
            this.tsbExportCombined.Text = "Export Combined PNG";
            this.tsbExportCombined.Click += new System.EventHandler(this.tsbExportCombined_Click);
            //
            // splitContainer
            //
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer.SplitterDistance = 260;
            this.splitContainer.Panel1.Controls.Add(this.lvLayers);
            this.splitContainer.Panel2.Controls.Add(this.canvas);
            //
            // lvLayers
            //
            this.lvLayers.CheckBoxes = true;
            this.lvLayers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colLayerName, this.colLayerType});
            this.lvLayers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvLayers.FullRowSelect = true;
            this.lvLayers.HideSelection = false;
            this.lvLayers.SmallImageList = this.imgColors;
            this.lvLayers.ShowItemToolTips = true;
            this.lvLayers.View = System.Windows.Forms.View.Details;
            this.lvLayers.ContextMenuStrip = this.ctxLayers;
            this.lvLayers.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lvLayers_ItemChecked);
            this.lvLayers.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvLayers_MouseDoubleClick);
            this.colLayerName.Text = "Layer";
            this.colLayerName.Width = 150;
            this.colLayerType.Text = "Loai";
            this.colLayerType.Width = 100;
            //
            // imgColors
            //
            this.imgColors.ImageSize = new System.Drawing.Size(16, 16);
            this.imgColors.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            //
            // ctxLayers
            //
            this.ctxLayers.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.miChangeColor, this.miMoveUp, this.miMoveDown,
                new System.Windows.Forms.ToolStripSeparator(), this.miRemove});
            this.miChangeColor.Text = "Doi mau lop...";
            this.miChangeColor.Click += new System.EventHandler(this.miChangeColor_Click);
            this.miMoveUp.Text = "Chuyen len";
            this.miMoveUp.Click += new System.EventHandler(this.miMoveUp_Click);
            this.miMoveDown.Text = "Chuyen xuong";
            this.miMoveDown.Click += new System.EventHandler(this.miMoveDown_Click);
            this.miRemove.Text = "Xoa lop";
            this.miRemove.Click += new System.EventHandler(this.miRemove_Click);
            //
            // canvas (custom control - logic o GerberCanvas.cs, Spec 5.2.2)
            //
            this.canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.canvas.Name = "canvas";
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.lblStatus, this.lblBoardSize, this.lblCoords, this.lblZoom});
            this.lblStatus.Spring = true;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblStatus.Text = "San sang";
            this.lblBoardSize.Text = "";
            this.lblCoords.Text = "X: -  Y: -";
            this.lblCoords.AutoSize = false;
            this.lblCoords.Size = new System.Drawing.Size(260, 17);
            this.lblZoom.Text = "Zoom: -";
            //
            // MainForm
            //
            this.AllowDrop = true;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi; // Spec 5.1.5
            this.ClientSize = new System.Drawing.Size(1200, 750);
            // THU TU ADD QUYET DINH LAYOUT (Spec 5.2.3): Fill truoc, strip sau
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.toolStrip);
            this.Name = "MainForm";
            this.Text = "Gerber Viewer & PNG Converter (.NET 4.8 WinForms)";
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ctxLayers.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton tsbOpen;
        private System.Windows.Forms.ToolStripLabel tslDpi;
        private System.Windows.Forms.ToolStripComboBox tscDpi;
        private System.Windows.Forms.ToolStripLabel tslMode;
        private System.Windows.Forms.ToolStripComboBox tscMode;
        private System.Windows.Forms.ToolStripButton tsbRender;
        private System.Windows.Forms.ToolStripButton tsbFit;
        private System.Windows.Forms.ToolStripButton tsbExportSelected;
        private System.Windows.Forms.ToolStripButton tsbExportCombined;

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.ListView lvLayers;
        private System.Windows.Forms.ColumnHeader colLayerName;
        private System.Windows.Forms.ColumnHeader colLayerType;
        private System.Windows.Forms.ImageList imgColors;
        private System.Windows.Forms.ContextMenuStrip ctxLayers;
        private System.Windows.Forms.ToolStripMenuItem miChangeColor;
        private System.Windows.Forms.ToolStripMenuItem miMoveUp;
        private System.Windows.Forms.ToolStripMenuItem miMoveDown;
        private System.Windows.Forms.ToolStripMenuItem miRemove;
        private GerberCanvas canvas;

        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel lblBoardSize;
        private System.Windows.Forms.ToolStripStatusLabel lblCoords;
        private System.Windows.Forms.ToolStripStatusLabel lblZoom;
    }
}

