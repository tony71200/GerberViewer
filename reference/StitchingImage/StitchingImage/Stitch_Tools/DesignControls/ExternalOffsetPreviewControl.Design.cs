using System.Windows.Forms;

namespace StitchingImage.Stitch_Tools.DesignControls
{
    public partial class ExternalOffsetPreviewControl
    {
        // [Codex] [Change time: 260324] [Separate design/layout construction from runtime matching logic.]
        private void InitializeDesignLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var left = BuildLeftPanel();
            var right = BuildRightPanel();
            root.Controls.Add(left, 0, 0);
            root.Controls.Add(right, 1, 0);

            UpdateMode();
        }

        private Control BuildLeftPanel()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            panel.Controls.Add(BuildSourceAndMetricsBlock(), 0, 0);
            panel.Controls.Add(BuildResultManualBlock(), 0, 1);
            panel.Controls.Add(BuildActionBlock(), 0, 2);
            return panel;
        }

        private Control BuildSourceAndMetricsBlock()
        {
            var grp = new GroupBox { Text = "Source", Dock = DockStyle.Fill };
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 6 };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));

            t.Controls.Add(new Label { Text = "Mode", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            _cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbMode.Items.AddRange(new object[] { "Auto", "Manual" });
            _cmbMode.SelectedIndex = 0;
            _cmbMode.SelectedIndexChanged += (s, e) => UpdateMode();
            t.Controls.Add(_cmbMode, 1, 0);

            _btnRun.Text = "Run Auto";
            _btnRun.Dock = DockStyle.Fill;
            _btnRun.Click += (s, e) => Run(false);
            t.Controls.Add(_btnRun, 3, 0);

            _btnApply.Text = "Apply";
            _btnApply.Dock = DockStyle.Fill;
            _btnApply.Click += (s, e) => Run(true);
            t.Controls.Add(_btnApply, 4, 0);

            AddPathRow(t, 1, "Horizontal", _txtHFrom);
            AddPathRow(t, 2, "", _txtHTo);
            AddPathRow(t, 3, "Vertical", _txtVFrom);
            AddPathRow(t, 4, "", _txtVTo);

            InitRO(_txtOverlapH); InitRO(_txtOverlapV); InitRO(_txtTimeH); InitRO(_txtTimeV);
            t.Controls.Add(new Label { Text = "Overlap", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 5);
            t.Controls.Add(_txtOverlapH, 1, 5);
            t.Controls.Add(_txtOverlapV, 2, 5);
            t.Controls.Add(new Label { Text = "Time (s)", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 3, 5);
            t.Controls.Add(_txtTimeH, 4, 5);

            grp.Controls.Add(t);
            return grp;
        }

        private void AddPathRow(TableLayoutPanel t, int row, string label, TextBox txtMain)
        {
            if (t.RowCount <= row)
            {
                t.RowCount = row + 1;
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            }

            t.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, row);
            t.Controls.Add(txtMain, 1, row);
            t.SetColumnSpan(txtMain, 3);
            txtMain.Dock = DockStyle.Fill;
            var btn = new Button { Text = "...", Dock = DockStyle.Fill };
            btn.Click += (s, e) => PickFile(txtMain);
            t.Controls.Add(btn, 4, row);
        }

        private Control BuildResultManualBlock()
        {
            var wrapper = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

            var result = new GroupBox { Text = "Results Tx/Ty/Theta", Dock = DockStyle.Fill };
            var rt = Create2x3WithLabels(_txtHTx, _txtHTy, _txtHTheta, _txtVTx, _txtVTy, _txtVTheta);
            result.Controls.Add(rt);

            var arrows = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            arrows.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            arrows.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var btnToManualH = new Button { Text = ">", Dock = DockStyle.Fill };
            btnToManualH.Click += (s, e) => CopyResultToManual(horizontal: true);
            var btnToManualV = new Button { Text = ">", Dock = DockStyle.Fill };
            btnToManualV.Click += (s, e) => CopyResultToManual(horizontal: false);
            arrows.Controls.Add(btnToManualH, 0, 0);
            arrows.Controls.Add(btnToManualV, 0, 1);

            var manual = new GroupBox { Text = "Manual Tx/Ty", Dock = DockStyle.Fill };
            var mt = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            mt.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            mt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            SetupNud(_nudHTx); SetupNud(_nudHTy); SetupNud(_nudVTx); SetupNud(_nudVTy);
            mt.Controls.Add(new Label { Text = "Horizontal", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            mt.Controls.Add(_nudHTx, 1, 0);
            mt.Controls.Add(_nudHTy, 2, 0);
            mt.Controls.Add(new Label { Text = "Vertical", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 1);
            mt.Controls.Add(_nudVTx, 1, 1);
            mt.Controls.Add(_nudVTy, 2, 1);
            manual.Controls.Add(mt);

            wrapper.Controls.Add(result, 0, 0);
            wrapper.Controls.Add(arrows, 1, 0);
            wrapper.Controls.Add(manual, 2, 0);
            return wrapper;
        }

        private Control BuildActionBlock()
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 4 };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            t.Controls.Add(new Label { Text = "Overlap V", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            t.Controls.Add(_txtOverlapV, 1, 0);
            t.Controls.Add(new Label { Text = "Time V (s)", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 2, 0);
            t.Controls.Add(_txtTimeV, 3, 0);
            return t;
        }

        private Control BuildRightPanel()
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var g1 = new GroupBox { Text = "Horizontal preview", Dock = DockStyle.Fill };
            _previewH.Dock = DockStyle.Fill;
            g1.Controls.Add(_previewH);

            var g2 = new GroupBox { Text = "Vertical preview", Dock = DockStyle.Fill };
            _previewV.Dock = DockStyle.Fill;
            g2.Controls.Add(_previewV);

            t.Controls.Add(g1, 0, 0);
            t.Controls.Add(g2, 0, 1);
            return t;
        }

        private static Control Create2x3WithLabels(TextBox h1, TextBox h2, TextBox h3, TextBox v1, TextBox v2, TextBox v3)
        {
            InitRO(h1); InitRO(h2); InitRO(h3); InitRO(v1); InitRO(v2); InitRO(v3);
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            t.Controls.Add(new Label { Text = "Horizontal", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 0);
            t.Controls.Add(h1, 1, 0); t.Controls.Add(h2, 2, 0); t.Controls.Add(h3, 3, 0);
            t.Controls.Add(new Label { Text = "Vertical", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 1);
            t.Controls.Add(v1, 1, 1); t.Controls.Add(v2, 2, 1); t.Controls.Add(v3, 3, 1);
            return t;
        }

        private static void SetupNud(NumericUpDown nud)
        {
            nud.DecimalPlaces = 2;
            nud.Minimum = -100000;
            nud.Maximum = 100000;
            nud.Dock = DockStyle.Fill;
        }

        private static void InitRO(TextBox tb)
        {
            tb.ReadOnly = true;
            tb.Dock = DockStyle.Fill;
        }
    }
}
