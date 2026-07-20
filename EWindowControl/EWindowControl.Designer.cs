using HalconDotNet;

namespace EWindowControl
{
    /// <summary>
/// 
/// </summary>
    partial class EWindowControl
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
            this.pl_Control = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pl_hsmart = new System.Windows.Forms.Panel();
            this.btn_CrossV = new System.Windows.Forms.Button();
            this.btn_CrossH = new System.Windows.Forms.Button();
            this.hSmartWindowControl1 = new HalconDotNet.HSmartWindowControl();
            this.lb_Info = new System.Windows.Forms.Label();
            this.pl_Control.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.pl_hsmart.SuspendLayout();
            this.SuspendLayout();
            // 
            // pl_Control
            // 
            this.pl_Control.BackColor = System.Drawing.Color.Black;
            this.pl_Control.Controls.Add(this.tableLayoutPanel1);
            this.pl_Control.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pl_Control.Location = new System.Drawing.Point(0, 0);
            this.pl_Control.Name = "pl_Control";
            this.pl_Control.Size = new System.Drawing.Size(485, 458);
            this.pl_Control.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.pl_hsmart, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lb_Info, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 95F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(485, 458);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // pl_hsmart
            // 
            this.pl_hsmart.Controls.Add(this.btn_CrossV);
            this.pl_hsmart.Controls.Add(this.btn_CrossH);
            this.pl_hsmart.Controls.Add(this.hSmartWindowControl1);
            this.pl_hsmart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pl_hsmart.Location = new System.Drawing.Point(3, 25);
            this.pl_hsmart.Name = "pl_hsmart";
            this.pl_hsmart.Size = new System.Drawing.Size(479, 430);
            this.pl_hsmart.TabIndex = 1;
            // 
            // btn_CrossV
            // 
            this.btn_CrossV.BackColor = System.Drawing.Color.Red;
            this.btn_CrossV.Enabled = false;
            this.btn_CrossV.FlatAppearance.BorderSize = 0;
            this.btn_CrossV.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_CrossV.Location = new System.Drawing.Point(237, 140);
            this.btn_CrossV.Name = "btn_CrossV";
            this.btn_CrossV.Size = new System.Drawing.Size(2, 150);
            this.btn_CrossV.TabIndex = 2;
            this.btn_CrossV.UseVisualStyleBackColor = false;
            this.btn_CrossV.Visible = false;
            // 
            // btn_CrossH
            // 
            this.btn_CrossH.BackColor = System.Drawing.Color.Red;
            this.btn_CrossH.Enabled = false;
            this.btn_CrossH.FlatAppearance.BorderSize = 0;
            this.btn_CrossH.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_CrossH.Location = new System.Drawing.Point(164, 213);
            this.btn_CrossH.Name = "btn_CrossH";
            this.btn_CrossH.Size = new System.Drawing.Size(150, 2);
            this.btn_CrossH.TabIndex = 1;
            this.btn_CrossH.UseVisualStyleBackColor = false;
            this.btn_CrossH.Visible = false;
            // 
            // hSmartWindowControl1
            // 
            this.hSmartWindowControl1.AutoScroll = true;
            this.hSmartWindowControl1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.hSmartWindowControl1.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.hSmartWindowControl1.BackColor = System.Drawing.Color.Brown;
            this.hSmartWindowControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hSmartWindowControl1.HDoubleClickToFitContent = false;
            this.hSmartWindowControl1.HDrawingObjectsModifier = HalconDotNet.HSmartWindowControl.DrawingObjectsModifier.None;
            this.hSmartWindowControl1.HImagePart = new System.Drawing.Rectangle(0, 0, 640, 480);
            this.hSmartWindowControl1.HKeepAspectRatio = false;
            this.hSmartWindowControl1.HMoveContent = true;
            this.hSmartWindowControl1.HZoomContent = HalconDotNet.HSmartWindowControl.ZoomContent.WheelForwardZoomsIn;
            this.hSmartWindowControl1.Location = new System.Drawing.Point(0, 0);
            this.hSmartWindowControl1.Margin = new System.Windows.Forms.Padding(0);
            this.hSmartWindowControl1.Name = "hSmartWindowControl1";
            this.hSmartWindowControl1.Size = new System.Drawing.Size(479, 430);
            this.hSmartWindowControl1.TabIndex = 0;
            this.hSmartWindowControl1.WindowSize = new System.Drawing.Size(479, 430);
            this.hSmartWindowControl1.HMouseMove += new HalconDotNet.HMouseEventHandler(this.hSmartWindowControl1_HMouseMove);
            this.hSmartWindowControl1.HMouseDown += new HalconDotNet.HMouseEventHandler(this.hSmartWindowControl1_HMouseDown);
            this.hSmartWindowControl1.HMouseUp += new HalconDotNet.HMouseEventHandler(this.hSmartWindowControl1_HMouseUp);
            this.hSmartWindowControl1.HInitWindow += new HalconDotNet.HInitWindowEventHandler(this.hSmartWindowControl1_HInitWindow);
            this.hSmartWindowControl1.MouseEnter += new System.EventHandler(this.hSmartWindowControl1_MouseEnter);
            this.hSmartWindowControl1.MouseLeave += new System.EventHandler(this.hSmartWindowControl1_MouseLeave);
            // 
            // lb_Info
            // 
            this.lb_Info.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lb_Info.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.lb_Info.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.lb_Info.Location = new System.Drawing.Point(3, 0);
            this.lb_Info.Name = "lb_Info";
            this.lb_Info.Size = new System.Drawing.Size(479, 22);
            this.lb_Info.TabIndex = 2;
            this.lb_Info.Text = "---";
            this.lb_Info.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // EWindowControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.pl_Control);
            this.Name = "EWindowControl";
            this.Size = new System.Drawing.Size(485, 458);
            this.pl_Control.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.pl_hsmart.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pl_Control;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel pl_hsmart;
        private HalconDotNet.HSmartWindowControl hSmartWindowControl1;
        private System.Windows.Forms.Label lb_Info;
        private System.Windows.Forms.Button btn_CrossV;
        private System.Windows.Forms.Button btn_CrossH;
    }
}
