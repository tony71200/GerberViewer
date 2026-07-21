namespace StitchingImage
{
    partial class FormatDialog
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
            this.root = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.top = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this._txtPattern = new System.Windows.Forms.TextBox();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCheck = new System.Windows.Forms.Button();
            this.middle = new System.Windows.Forms.SplitContainer();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._lstKeywords = new System.Windows.Forms.ListBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this._lstFiles = new System.Windows.Forms.ListBox();
            this.results = new System.Windows.Forms.TableLayoutPanel();
            this._txtY = new System.Windows.Forms.TextBox();
            this._txtX = new System.Windows.Forms.TextBox();
            this._txtPosition = new System.Windows.Forms.TextBox();
            this._txtId = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this._txtGroup = new System.Windows.Forms.TextBox();
            this.bottom = new System.Windows.Forms.TableLayoutPanel();
            this._lblStatus = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.root.SuspendLayout();
            this.top.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.middle)).BeginInit();
            this.middle.Panel1.SuspendLayout();
            this.middle.Panel2.SuspendLayout();
            this.middle.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.results.SuspendLayout();
            this.bottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // root
            // 
            this.root.ColumnCount = 1;
            this.root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.Controls.Add(this.label1, 0, 0);
            this.root.Controls.Add(this.top, 0, 1);
            this.root.Controls.Add(this.middle, 0, 2);
            this.root.Controls.Add(this.results, 0, 3);
            this.root.Controls.Add(this.bottom, 0, 4);
            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.Location = new System.Drawing.Point(0, 0);
            this.root.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.root.Name = "root";
            this.root.RowCount = 5;
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 18F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.root.Size = new System.Drawing.Size(684, 361);
            this.root.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "Format Parser";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // top
            // 
            this.top.ColumnCount = 4;
            this.top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 44F));
            this.top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.top.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.top.Controls.Add(this.label2, 0, 0);
            this.top.Controls.Add(this._txtPattern, 1, 0);
            this.top.Controls.Add(this.btnApply, 3, 0);
            this.top.Controls.Add(this.btnCheck, 2, 0);
            this.top.Dock = System.Windows.Forms.DockStyle.Fill;
            this.top.Location = new System.Drawing.Point(2, 19);
            this.top.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.top.Name = "top";
            this.top.RowCount = 1;
            this.top.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.top.Size = new System.Drawing.Size(680, 23);
            this.top.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 0);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 12);
            this.label2.TabIndex = 0;
            this.label2.Text = "Format";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _txtPattern
            // 
            this._txtPattern.AllowDrop = true;
            this._txtPattern.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtPattern.Location = new System.Drawing.Point(46, 1);
            this._txtPattern.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this._txtPattern.Name = "_txtPattern";
            this._txtPattern.Size = new System.Drawing.Size(377, 22);
            this._txtPattern.TabIndex = 1;
            this._txtPattern.DragDrop += new System.Windows.Forms.DragEventHandler(this._txtPattern_DragDrop);
            this._txtPattern.DragEnter += new System.Windows.Forms.DragEventHandler(this._txtPattern_DragEnter);
            // 
            // btnApply
            // 
            this.btnApply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnApply.Location = new System.Drawing.Point(554, 1);
            this.btnApply.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(124, 21);
            this.btnApply.TabIndex = 2;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnCheck
            // 
            this.btnCheck.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCheck.Location = new System.Drawing.Point(427, 1);
            this.btnCheck.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Size = new System.Drawing.Size(123, 21);
            this.btnCheck.TabIndex = 3;
            this.btnCheck.Text = "Check";
            this.btnCheck.UseVisualStyleBackColor = true;
            this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            // 
            // middle
            // 
            this.middle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.middle.Location = new System.Drawing.Point(2, 44);
            this.middle.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.middle.Name = "middle";
            // 
            // middle.Panel1
            // 
            this.middle.Panel1.Controls.Add(this.groupBox1);
            // 
            // middle.Panel2
            // 
            this.middle.Panel2.Controls.Add(this.groupBox2);
            this.middle.Size = new System.Drawing.Size(680, 273);
            this.middle.SplitterDistance = 170;
            this.middle.SplitterWidth = 2;
            this.middle.TabIndex = 2;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this._lstKeywords);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.groupBox1.Size = new System.Drawing.Size(170, 273);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Keyword";
            // 
            // _lstKeywords
            // 
            this._lstKeywords.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lstKeywords.FormattingEnabled = true;
            this._lstKeywords.ItemHeight = 12;
            this._lstKeywords.Location = new System.Drawing.Point(2, 16);
            this._lstKeywords.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this._lstKeywords.Name = "_lstKeywords";
            this._lstKeywords.Size = new System.Drawing.Size(166, 256);
            this._lstKeywords.TabIndex = 0;
            this._lstKeywords.MouseDown += new System.Windows.Forms.MouseEventHandler(this._lstKeywords_MouseDown);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this._lstFiles);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.groupBox2.Size = new System.Drawing.Size(508, 273);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Sample file name image";
            // 
            // _lstFiles
            // 
            this._lstFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lstFiles.FormattingEnabled = true;
            this._lstFiles.ItemHeight = 12;
            this._lstFiles.Location = new System.Drawing.Point(2, 16);
            this._lstFiles.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this._lstFiles.Name = "_lstFiles";
            this._lstFiles.Size = new System.Drawing.Size(504, 256);
            this._lstFiles.TabIndex = 0;
            // 
            // results
            // 
            this.results.ColumnCount = 10;
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.results.Controls.Add(this._txtY, 9, 0);
            this.results.Controls.Add(this._txtX, 7, 0);
            this.results.Controls.Add(this._txtPosition, 5, 0);
            this.results.Controls.Add(this._txtId, 3, 0);
            this.results.Controls.Add(this.label3, 0, 0);
            this.results.Controls.Add(this.label4, 2, 0);
            this.results.Controls.Add(this.label5, 4, 0);
            this.results.Controls.Add(this.label6, 6, 0);
            this.results.Controls.Add(this.label7, 8, 0);
            this.results.Controls.Add(this._txtGroup, 1, 0);
            this.results.Dock = System.Windows.Forms.DockStyle.Fill;
            this.results.Location = new System.Drawing.Point(2, 319);
            this.results.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.results.Name = "results";
            this.results.RowCount = 1;
            this.results.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.results.Size = new System.Drawing.Size(680, 16);
            this.results.TabIndex = 3;
            // 
            // _txtY
            // 
            this._txtY.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtY.Location = new System.Drawing.Point(614, 1);
            this._txtY.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this._txtY.Name = "_txtY";
            this._txtY.ReadOnly = true;
            this._txtY.Size = new System.Drawing.Size(64, 22);
            this._txtY.TabIndex = 9;
            // 
            // _txtX
            // 
            this._txtX.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtX.Location = new System.Drawing.Point(478, 1);
            this._txtX.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this._txtX.Name = "_txtX";
            this._txtX.ReadOnly = true;
            this._txtX.Size = new System.Drawing.Size(64, 22);
            this._txtX.TabIndex = 8;
            // 
            // _txtPosition
            // 
            this._txtPosition.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtPosition.Location = new System.Drawing.Point(342, 1);
            this._txtPosition.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this._txtPosition.Name = "_txtPosition";
            this._txtPosition.ReadOnly = true;
            this._txtPosition.Size = new System.Drawing.Size(64, 22);
            this._txtPosition.TabIndex = 7;
            // 
            // _txtId
            // 
            this._txtId.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtId.Location = new System.Drawing.Point(206, 1);
            this._txtId.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this._txtId.Name = "_txtId";
            this._txtId.ReadOnly = true;
            this._txtId.Size = new System.Drawing.Size(64, 22);
            this._txtId.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(2, 0);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Group Id";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(138, 0);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(15, 12);
            this.label4.TabIndex = 1;
            this.label4.Text = "Id";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(274, 0);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 16);
            this.label5.TabIndex = 2;
            this.label5.Text = "Position";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(410, 0);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(13, 12);
            this.label6.TabIndex = 3;
            this.label6.Text = "X";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(546, 0);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(13, 12);
            this.label7.TabIndex = 4;
            this.label7.Text = "Y";
            // 
            // _txtGroup
            // 
            this._txtGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtGroup.Location = new System.Drawing.Point(70, 1);
            this._txtGroup.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this._txtGroup.Name = "_txtGroup";
            this._txtGroup.ReadOnly = true;
            this._txtGroup.Size = new System.Drawing.Size(64, 22);
            this._txtGroup.TabIndex = 5;
            // 
            // bottom
            // 
            this.bottom.ColumnCount = 2;
            this.bottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottom.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 76F));
            this.bottom.Controls.Add(this._lblStatus, 0, 0);
            this.bottom.Controls.Add(this.btnCancel, 1, 0);
            this.bottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottom.Location = new System.Drawing.Point(2, 337);
            this.bottom.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.bottom.Name = "bottom";
            this.bottom.RowCount = 1;
            this.bottom.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottom.Size = new System.Drawing.Size(680, 23);
            this.bottom.TabIndex = 4;
            // 
            // _lblStatus
            // 
            this._lblStatus.AutoSize = true;
            this._lblStatus.Location = new System.Drawing.Point(2, 0);
            this._lblStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(35, 12);
            this._lblStatus.TabIndex = 0;
            this._lblStatus.Text = "Ready";
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnCancel
            // 
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCancel.Location = new System.Drawing.Point(606, 1);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(72, 21);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // FormatDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 361);
            this.ControlBox = false;
            this.Controls.Add(this.root);
            this.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Name = "FormatDialog";
            this.ShowIcon = false;
            this.Text = "Format Parser";
            this.root.ResumeLayout(false);
            this.root.PerformLayout();
            this.top.ResumeLayout(false);
            this.top.PerformLayout();
            this.middle.Panel1.ResumeLayout(false);
            this.middle.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.middle)).EndInit();
            this.middle.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.results.ResumeLayout(false);
            this.results.PerformLayout();
            this.bottom.ResumeLayout(false);
            this.bottom.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel root;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel top;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox _txtPattern;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCheck;
        private System.Windows.Forms.SplitContainer middle;
        private System.Windows.Forms.ListBox _lstKeywords;
        private System.Windows.Forms.ListBox _lstFiles;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel results;
        private System.Windows.Forms.TableLayoutPanel bottom;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox _txtY;
        private System.Windows.Forms.TextBox _txtX;
        private System.Windows.Forms.TextBox _txtPosition;
        private System.Windows.Forms.TextBox _txtId;
        private System.Windows.Forms.TextBox _txtGroup;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Button btnCancel;
    }
}