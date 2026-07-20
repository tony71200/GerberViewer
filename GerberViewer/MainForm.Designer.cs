namespace GerberViewer
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.mainTabControl = new System.Windows.Forms.TabControl();
            this.tabReadGerber = new System.Windows.Forms.TabPage();
            this.tabCreateGerberSample = new System.Windows.Forms.TabPage();
            this.tabAlignStitching = new System.Windows.Forms.TabPage();
            this.readGerberControl = new GerberViewer.Views.ReadGerberControl();
            this.createGerberSampleControl = new GerberViewer.Views.CreateGerberSampleControl();
            this.alignStitchingControl = new GerberViewer.Views.AlignStitchingControl();
            this.mainTabControl.SuspendLayout();
            this.tabReadGerber.SuspendLayout();
            this.tabCreateGerberSample.SuspendLayout();
            this.tabAlignStitching.SuspendLayout();
            this.SuspendLayout();
            //
            // mainTabControl
            //
            this.mainTabControl.Controls.Add(this.tabReadGerber);
            this.mainTabControl.Controls.Add(this.tabCreateGerberSample);
            this.mainTabControl.Controls.Add(this.tabAlignStitching);
            this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTabControl.Name = "mainTabControl";
            this.mainTabControl.SelectedIndex = 0;
            //
            // tabReadGerber
            //
            this.tabReadGerber.Controls.Add(this.readGerberControl);
            this.tabReadGerber.Name = "tabReadGerber";
            this.tabReadGerber.Text = "Read Gerber";
            this.tabReadGerber.UseVisualStyleBackColor = true;
            //
            // readGerberControl
            //
            this.readGerberControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.readGerberControl.Name = "readGerberControl";
            //
            // tabCreateGerberSample
            //
            this.tabCreateGerberSample.Controls.Add(this.createGerberSampleControl);
            this.tabCreateGerberSample.Name = "tabCreateGerberSample";
            this.tabCreateGerberSample.Text = "Create Gerber Sample";
            this.tabCreateGerberSample.UseVisualStyleBackColor = true;
            //
            // createGerberSampleControl
            //
            this.createGerberSampleControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.createGerberSampleControl.Name = "createGerberSampleControl";
            //
            // tabAlignStitching
            //
            this.tabAlignStitching.Controls.Add(this.alignStitchingControl);
            this.tabAlignStitching.Name = "tabAlignStitching";
            this.tabAlignStitching.Text = "Align and Stitching";
            this.tabAlignStitching.UseVisualStyleBackColor = true;
            //
            // alignStitchingControl
            //
            this.alignStitchingControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.alignStitchingControl.Name = "alignStitchingControl";
            //
            // MainForm
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1400, 780);
            this.Controls.Add(this.mainTabControl);
            this.Name = "MainForm";
            this.Text = "Gerber Viewer";
            this.mainTabControl.ResumeLayout(false);
            this.tabReadGerber.ResumeLayout(false);
            this.tabCreateGerberSample.ResumeLayout(false);
            this.tabAlignStitching.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TabControl mainTabControl;
        private System.Windows.Forms.TabPage tabReadGerber;
        private System.Windows.Forms.TabPage tabCreateGerberSample;
        private System.Windows.Forms.TabPage tabAlignStitching;
        private Views.ReadGerberControl readGerberControl;
        private Views.CreateGerberSampleControl createGerberSampleControl;
        private Views.AlignStitchingControl alignStitchingControl;
    }
}
