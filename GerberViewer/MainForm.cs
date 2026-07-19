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
        private CoordinateTransformer _previewTransformer;   // doi chieu toa do chuot (FR-009)
        private bool _suppressCheckEvent;                    // tranh render lai khi dang nap danh sach
        private bool _rendering;
        private readonly List<CanvasMeasurementOverlay> _measurements = new List<CanvasMeasurementOverlay>();
        private readonly List<PointF> _pendingImagePoints = new List<PointF>();
        private readonly List<PointD> _pendingWorldPoints = new List<PointD>();

        private const int PreviewDpi = 300;
        private const int LargePrimitiveWarningThreshold = 50000;
        public MainForm()
        {
            InitializeComponent();
            tscDpi.SelectedIndex = 2;    // 600 Export DPI
            tscMode.SelectedIndex = 0;   // Realistic
            // Event co generic args - wire tay o day (Designer khong serialize duoc EventHandler<PointF?>)
            canvas.ImageCursorMoved += Canvas_ImageCursorMoved;
            canvas.ImageClicked += Canvas_ImageClicked;
            SetInteractionMode(CanvasInteractionMode.PanInspect);
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
                lblStatus.Text += " | large scene: " + primitiveCount.ToString("N0") + " primitives; preview uses capped DPI for responsiveness";
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

        private RenderOptions BuildOptions(bool forPreview)
        {
            int dpi = forPreview ? PreviewDpi : int.Parse(tscDpi.SelectedItem.ToString());
            return new RenderOptions
            {
                Dpi = dpi,
                Mode = tscMode.SelectedIndex == 1 ? ColorMode.BinaryMask : ColorMode.Realistic
            };
        }

        private void tsbRender_Click(object sender, EventArgs e) { RenderPreviewAsync(true); }
        private void tsbFit_Click(object sender, EventArgs e) { canvas.FitToView(); UpdateZoomLabel(); }

        private void tsbPan_Click(object sender, EventArgs e) { SetInteractionMode(CanvasInteractionMode.PanInspect); }
        private void tsbMeasureDistance_Click(object sender, EventArgs e) { SetInteractionMode(CanvasInteractionMode.MeasureDistance); }
        private void tsbMeasureAngle_Click(object sender, EventArgs e) { SetInteractionMode(CanvasInteractionMode.MeasureAngle); }
        private void tsbClearMeasurements_Click(object sender, EventArgs e)
        {
            _measurements.Clear();
            _pendingImagePoints.Clear();
            _pendingWorldPoints.Clear();
            canvas.ClearMeasurementOverlay();
            lblStatus.Text = "Measurements cleared";
        }

        private void SetInteractionMode(CanvasInteractionMode mode)
        {
            canvas.InteractionMode = mode;
            tsbPan.Checked = mode == CanvasInteractionMode.PanInspect;
            tsbMeasureDistance.Checked = mode == CanvasInteractionMode.MeasureDistance;
            tsbMeasureAngle.Checked = mode == CanvasInteractionMode.MeasureAngle;
            _pendingImagePoints.Clear();
            _pendingWorldPoints.Clear();
            canvas.SetPendingMeasurement(_pendingImagePoints, null);
            lblStatus.Text = mode == CanvasInteractionMode.PanInspect ? "Pan/Inspect mode" : (mode == CanvasInteractionMode.MeasureDistance ? "Measure Distance: click P1 then P2" : "Measure Angle: click A, vertex V, then B");
        }

        private void RenderPreviewAsync(bool fit)
        {
            if (_rendering) return;
            if (_engine.GetCombinedBoundsMm().IsEmpty)
            {
                canvas.SetImage(null, false);
                _previewTransformer = null;
                lblBoardSize.Text = "";
                return;
            }

            _rendering = true;
            lblStatus.Text = "Generating preview...";
            RenderOptions opts = BuildOptions(true);

            // Render o worker thread; Bitmap ban giao quyen so huu cho canvas sau khi xong (Spec 5.1.4)
            Task.Run(() =>
            {
                CoordinateTransformer t = _engine.CreateTransformer(opts);
                Bitmap bmp = _engine.RenderCombined(opts);
                return Tuple.Create(bmp, t);
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
                    _previewTransformer = task.Result.Item2;
                    canvas.SetImage(task.Result.Item1, fit);
                    RectangleD b = _previewTransformer.ContentBoundsMm;
                    lblBoardSize.Text = string.Format("Board: {0:0.##} x {1:0.##} mm", b.Width, b.Height);
                    lblStatus.Text = "Ready (preview transform, Export DPI independent)";
                    UpdateZoomLabel();
                }));
            });
        }

        // ---------- Toa do chuot (FR-009) ----------

        private void Canvas_ImageCursorMoved(object sender, PointF? imagePx)
        {
            UpdateZoomLabel();
            canvas.SetPendingMeasurement(_pendingImagePoints, imagePx);
            if (imagePx == null || _previewTransformer == null)
            {
                lblCoords.Text = "X: -  Y: -";
                return;
            }
            PointD mm = _previewTransformer.ToMm(imagePx.Value.X, imagePx.Value.Y);
            lblCoords.Text = string.Format("X: {0:0.###} mm ({1:0.####}\")  Y: {2:0.###} mm ({3:0.####}\")",
                mm.X, mm.X / 25.4, mm.Y, mm.Y / 25.4);
        }


        private void Canvas_ImageClicked(object sender, PointF imagePx)
        {
            if (_previewTransformer == null) return;
            if (canvas.InteractionMode == CanvasInteractionMode.PanInspect) return;

            PointD world = _previewTransformer.ToMm(imagePx.X, imagePx.Y);
            _pendingImagePoints.Add(imagePx);
            _pendingWorldPoints.Add(world);

            int requiredPoints = canvas.InteractionMode == CanvasInteractionMode.MeasureAngle ? 3 : 2;
            if (_pendingWorldPoints.Count < requiredPoints)
            {
                canvas.SetPendingMeasurement(_pendingImagePoints, null);
                lblStatus.Text = canvas.InteractionMode == CanvasInteractionMode.MeasureAngle ?
                    "Measure Angle: click the next point" : "Measure Distance: click P2";
                return;
            }

            CanvasMeasurementOverlay overlay = new CanvasMeasurementOverlay();
            overlay.ImagePoints.AddRange(_pendingImagePoints);
            overlay.IsAngle = canvas.InteractionMode == CanvasInteractionMode.MeasureAngle;
            overlay.Label = overlay.IsAngle ? FormatAngleMeasurement(_pendingWorldPoints) : FormatDistanceMeasurement(_pendingWorldPoints);
            _measurements.Add(overlay);
            _pendingImagePoints.Clear();
            _pendingWorldPoints.Clear();
            canvas.SetMeasurements(_measurements);
            canvas.SetPendingMeasurement(_pendingImagePoints, null);
            lblStatus.Text = "Measurement added: " + overlay.Label.Replace("\n", " | ");
        }

        private static string FormatDistanceMeasurement(IList<PointD> pts)
        {
            double dx = pts[1].X - pts[0].X;
            double dy = pts[1].Y - pts[0].Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double bearing = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            if (bearing < 0) bearing += 360.0;
            return string.Format("ΔX {0:0.###} mm\nΔY {1:0.###} mm\nD {2:0.###} mm\nθ {3:0.##}°", dx, dy, distance, bearing);
        }

        private static string FormatAngleMeasurement(IList<PointD> pts)
        {
            PointD a = pts[0], v = pts[1], b = pts[2];
            double ax = a.X - v.X, ay = a.Y - v.Y;
            double bx = b.X - v.X, by = b.Y - v.Y;
            double la = Math.Sqrt(ax * ax + ay * ay);
            double lb = Math.Sqrt(bx * bx + by * by);
            if (la < 1e-12 || lb < 1e-12) return "Angle undefined";
            double cos = (ax * bx + ay * by) / (la * lb);
            cos = Math.Max(-1.0, Math.Min(1.0, cos));
            double angle = Math.Acos(cos) * 180.0 / Math.PI;
            return string.Format("∠ {0:0.##}°", angle);
        }

        private void UpdateZoomLabel()
        {
            lblZoom.Text = canvas.HasImage ? string.Format("Zoom: {0:0}%", canvas.Zoom * 100) : "Zoom: -";
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
                RunExport(() =>
                {
                    RenderOptions opts = BuildOptions(false);
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
                RunExport(() =>
                {
                    _engine.ExportCombinedPng(BuildOptions(false), dlg.FileName);
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
