// GerberViewer/MainForm.cs
// The entire logic of the form (Designer.cs only has the layout).

// Online simulation functions (gerberviewer.com): drag and drop multiple files, list of classes
// On/off + change color, canvas zoom/pan, mouse coordinates mm/inch, export PNG (FR-003..FR-017).
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GerberEngine;

namespace GerberViewer
{
    public partial class MainForm : Form
    {
        private readonly GerberEngineFacade _engine = new GerberEngineFacade();
        private bool _suppressCheckEvent;                    // tranh render lai khi dang nap danh sach
        private bool _rendering;

        private const int LargePrimitiveWarningThreshold = 50000;
        public MainForm()
        {
            InitializeComponent();
            tscDpi.SelectedIndex = 2;    // 600 Export DPI
            tscMode.SelectedIndex = 0;   // Realistic
        }

        // ---------- Nap file (FR-003) ----------
        private void tsbOpen_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Multiselect = true;
                dlg.Filter = "Gerber files|*.gbr;*.ger;*.gtl;*.gbl;*.gts;*.gbs;*.gto;*.gbo;*.gko;*.gm1;*.gml;*.art;*.pho|All files|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadFiles(dlg.FileNames);
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null) LoadFiles(files);
        }

        private void LoadFiles(IEnumerable<string> paths)
        {
            lblStatus.Text = "Parsing...";
            int warnings = 0;
            int primitiveCount = 0;
            foreach (string path in paths)
            {
                try
                {
                    GerberLayer layer = _engine.LoadLayer(path);
                    warnings += layer.Warnings.Count;
                    primitiveCount += layer.Primitives.Count;
                    AddLayerItem(layer);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Khong doc duoc \"" + Path.GetFileName(path) + "\":\r\n" + ex.Message,
                        "Loi nap file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            lblStatus.Text = warnings > 0
                ? "Loaded - " + warnings + " parser warnings (see layer tooltip)"
                : "Loaded";
            if (primitiveCount >= LargePrimitiveWarningThreshold)
                lblStatus.Text += " | large scene: " + primitiveCount.ToString("N0") + " primitives; SVG preview uses vector refinement path";
            RenderPreviewAsync(true);
        }

        private void AddLayerItem(GerberLayer layer)
        {
            _suppressCheckEvent = true;
            try
            {
                string imgKey = AddColorSwatch(layer.DisplayColor);
                var item = new ListViewItem(layer.FileName) { Tag = layer, Checked = layer.Visible, ImageKey = imgKey };
                item.SubItems.Add(layer.Type.ToString());
                if (layer.Warnings.Count > 0)
                    item.ToolTipText = string.Join("\r\n", layer.Warnings.ToArray());
                lvLayers.Items.Add(item);
            }
            finally { _suppressCheckEvent = false; }
        }

        private string AddColorSwatch(Color c)
        {
            string key = c.ToArgb().ToString();
            if (!imgColors.Images.ContainsKey(key))
            {
                var bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    using (var b = new SolidBrush(c)) g.FillRectangle(b, 1, 1, 14, 14);
                    g.DrawRectangle(Pens.Gray, 1, 1, 13, 13);
                }
                imgColors.Images.Add(key, bmp);
            }
            return key;
        }

        // ---------- Danh sach lop (FR-004) ----------

        private GerberLayer SelectedLayer
        {
            get { return lvLayers.SelectedItems.Count > 0 ? (GerberLayer)lvLayers.SelectedItems[0].Tag : null; }
        }

        private void lvLayers_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_suppressCheckEvent) return;
            ((GerberLayer)e.Item.Tag).Visible = e.Item.Checked;
            RenderPreviewAsync(false);
        }

        private void lvLayers_MouseDoubleClick(object sender, MouseEventArgs e) { ChangeSelectedColor(); }
        private void miChangeColor_Click(object sender, EventArgs e) { ChangeSelectedColor(); }

        private void ChangeSelectedColor()
        {
            GerberLayer layer = SelectedLayer;
            if (layer == null) return;
            using (var dlg = new ColorDialog { Color = layer.DisplayColor, FullOpen = true })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                layer.DisplayColor = dlg.Color;
                lvLayers.SelectedItems[0].ImageKey = AddColorSwatch(dlg.Color);
                RenderPreviewAsync(false);
            }
        }

        private void miRemove_Click(object sender, EventArgs e)
        {
            GerberLayer layer = SelectedLayer;
            if (layer == null) return;
            _engine.RemoveLayer(layer);
            lvLayers.SelectedItems[0].Remove();
            RenderPreviewAsync(false);
        }

        private void miMoveUp_Click(object sender, EventArgs e) { MoveSelected(-1); }
        private void miMoveDown_Click(object sender, EventArgs e) { MoveSelected(1); }

        private void MoveSelected(int delta)
        {
            if (lvLayers.SelectedItems.Count == 0) return;
            ListViewItem item = lvLayers.SelectedItems[0];
            int idx = item.Index, newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= lvLayers.Items.Count) return;
            _engine.MoveLayer((GerberLayer)item.Tag, newIdx);
            _suppressCheckEvent = true;
            try
            {
                lvLayers.Items.RemoveAt(idx);
                lvLayers.Items.Insert(newIdx, item);
                item.Selected = true;
            }
            finally { _suppressCheckEvent = false; }
            RenderPreviewAsync(false);
        }

        // ---------- Render preview nen (FR-016, FR-017) ----------

        private SvgRenderOptions BuildPreviewOptions()
        {
            return new SvgRenderOptions
            {
                Mode = tscMode.SelectedIndex == 1 ? ColorMode.BinaryMask : ColorMode.Realistic
            };
        }

        private RasterExportOptions BuildExportOptionsOnUiThread()
        {
            return new RasterExportOptions
            {
                Dpi = int.Parse(tscDpi.SelectedItem.ToString()),
                Mode = tscMode.SelectedIndex == 1 ? ColorMode.BinaryMask : ColorMode.Realistic
            };
        }

        private void tsbRender_Click(object sender, EventArgs e) { RenderPreviewAsync(true); }
        private void tsbFit_Click(object sender, EventArgs e) { previewHost.FitToView(); UpdateZoomLabel(); }

        private void RenderPreviewAsync(bool fit)
        {
            if (_rendering) return;
            if (_engine.GetCombinedBoundsMm().IsEmpty)
            {
                previewHost.SetSvg(null, false);
                lblBoardSize.Text = "";
                return;
            }

            _rendering = true;
            lblStatus.Text = "Generating preview...";
            SvgRenderOptions opts = BuildPreviewOptions();

            // Render o worker thread; Bitmap ban giao quyen so huu cho canvas sau khi xong (Spec 5.1.4)
            Task.Run(() =>
            {
                string svg = _engine.RenderCombinedSvg(opts);
                return svg;
            }).ContinueWith(task =>
            {
                BeginInvoke((Action)(() =>
                {
                    _rendering = false;
                    if (task.IsFaulted)
                    {
                        string msg = task.Exception.GetBaseException().Message;
                        lblStatus.Text = "Loi render: " + msg;
                        MessageBox.Show(this, msg, "Render", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    previewHost.SetSvg(task.Result, fit);
                    RectangleD b = _engine.GetCombinedBoundsMm();
                    lblBoardSize.Text = string.Format("Board: {0:0.##} x {1:0.##} mm", b.Width, b.Height);
                    lblStatus.Text = "Ready (SVG preview, Export DPI independent)";
                    UpdateZoomLabel();
                }));
            });
        }

        // ---------- Toa do chuot (FR-009) ----------

        private void UpdateZoomLabel()
        {
            lblZoom.Text = previewHost.HasPreview ? string.Format("Zoom: {0:0}%", previewHost.Zoom * 100) : "Zoom: -";
        }

        // ---------- Export PNG (FR-012) ----------

        private void tsbExportSelected_Click(object sender, EventArgs e)
        {
            List<GerberLayer> targets = new List<GerberLayer>();
            foreach (ListViewItem item in lvLayers.Items)
                if (item.Checked) targets.Add((GerberLayer)item.Tag);
            if (targets.Count == 0) { lblStatus.Text = "No visible/checked layers selected"; return; }

            using (var dlg = new FolderBrowserDialog { Description = "Choose folder for per-layer PNG export" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                RasterExportOptions exportOptions = BuildExportOptionsOnUiThread();
                RunExport(() =>
                {
                    RasterExportOptions opts = exportOptions;
                    foreach (GerberLayer layer in targets)
                    {
                        string name = Path.GetFileNameWithoutExtension(layer.FileName)
                                    + "_" + layer.Type + "_" + opts.Dpi + "dpi.png";
                        _engine.ExportLayerPng(layer, opts, Path.Combine(dlg.SelectedPath, name));
                    }
                    return targets.Count + " file PNG -> " + dlg.SelectedPath;
                });
            }
        }

        private void tsbExportCombined_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog { Filter = "PNG image|*.png", FileName = "board_combined.png" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                RasterExportOptions exportOptions = BuildExportOptionsOnUiThread();
                RunExport(() =>
                {
                    _engine.ExportCombinedPng(exportOptions, dlg.FileName);
                    return "Da xuat " + dlg.FileName;
                });
            }
        }

        /// <summary>Chay export o worker thread, khoa nut trong luc chay (FR-017).</summary>
        private void RunExport(Func<string> work)
        {
            tsbExportSelected.Enabled = tsbExportCombined.Enabled = false;
            lblStatus.Text = "Exporting PNG...";
            Task.Run(work).ContinueWith(task =>
            {
                BeginInvoke((Action)(() =>
                {
                    tsbExportSelected.Enabled = tsbExportCombined.Enabled = true;
                    if (task.IsFaulted)
                    {
                        string msg = task.Exception.GetBaseException().Message;
                        lblStatus.Text = "Loi xuat: " + msg;
                        MessageBox.Show(this, msg, "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else lblStatus.Text = task.Result;
                }));
            });
        }
    }
}
