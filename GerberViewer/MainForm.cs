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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GerberEngine;

namespace GerberViewer
{
    public partial class MainForm : Form
    {
        private readonly GerberEngineFacade _engine = new GerberEngineFacade();
        private PreviewBitmapRenderResult _previewRenderResult; // maps preview image pixels back to board mm (FR-009)
        private bool _suppressCheckEvent;                    // tranh render lai khi dang nap danh sach
        private bool _rendering;
        private CancellationTokenSource _loadFilesCts;
        private CancellationTokenSource _previewCts;

        private const int LargePrimitiveWarningThreshold = 50000;
        public MainForm()
        {
            InitializeComponent();
            tscDpi.SelectedIndex = 2;    // 600 Export DPI
            tscMode.SelectedIndex = 0;   // Realistic
            // Event co generic args - wire tay o day (Designer khong serialize duoc EventHandler<PointF?>)
            canvas.ImageCursorMoved += Canvas_ImageCursorMoved;
            InitializeSvgViewerAsync();
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_loadFilesCts != null) _loadFilesCts.Cancel();
            if (_previewCts != null) _previewCts.Cancel();
            svgViewer.PrepareForFormClosing();
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

        private async void LoadFiles(IEnumerable<string> paths)
        {
            string[] files = paths == null ? new string[0] : paths.ToArray();
            if (files.Length == 0) return;

            CancellationTokenSource previousLoad = _loadFilesCts;
            if (previousLoad != null) previousLoad.Cancel();
            CancellationTokenSource currentLoad = new CancellationTokenSource();
            _loadFilesCts = currentLoad;
            CancellationToken token = currentLoad.Token;

            lblStatus.Text = "Reading file 0/" + files.Length;

            try
            {
                LoadFilesResult result = await Task.Run(() => LoadFilesOnWorker(files, token), token);

                foreach (GerberLayer layer in result.Layers)
                    AddLayerItem(layer);

                foreach (LayerLoadError error in result.Errors)
                {
                    MessageBox.Show(this, "Khong doc duoc \"" + Path.GetFileName(error.Path) + "\":\r\n" + error.Message,
                        "Loi nap file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                lblStatus.Text = result.Warnings > 0
                    ? "Loaded " + result.LoadedCount + "/" + result.TotalCount + " files - " + result.Warnings + " parser warnings (see layer tooltip)"
                    : "Loaded " + result.LoadedCount + "/" + result.TotalCount + " files";
                if (result.PrimitiveCount >= LargePrimitiveWarningThreshold)
                    lblStatus.Text += " | large scene: " + result.PrimitiveCount.ToString("N0") + " primitives; preview uses capped DPI for responsiveness";
                RenderPreviewAsync(true);
            }
            catch (OperationCanceledException)
            {
                if (ReferenceEquals(_loadFilesCts, currentLoad))
                    lblStatus.Text = "File load canceled";
            }
            finally
            {
                if (ReferenceEquals(_loadFilesCts, currentLoad))
                {
                    _loadFilesCts = null;
                    currentLoad.Dispose();
                }
            }
        }

        private LoadFilesResult LoadFilesOnWorker(string[] files, CancellationToken token)
        {
            var result = new LoadFilesResult { TotalCount = files.Length };
            for (int i = 0; i < files.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                string path = files[i];
                int fileNumber = i + 1;
                UpdateLoadStatus("Reading file " + fileNumber + "/" + files.Length + ": " + Path.GetFileName(path), token);
                try
                {
                    token.ThrowIfCancellationRequested();
                    UpdateLoadStatus("Parsing commands " + fileNumber + "/" + files.Length + ": " + Path.GetFileName(path), token);
                    GerberLayer layer = _engine.LoadLayer(path);
                    if (token.IsCancellationRequested)
                    {
                        _engine.RemoveLayer(layer);
                        token.ThrowIfCancellationRequested();
                    }
                    result.LoadedCount++;
                    result.Warnings += layer.Warnings.Count;
                    result.PrimitiveCount += layer.Primitives.Count;
                    result.Layers.Add(layer);
                    UpdateLoadStatus("Loaded " + result.LoadedCount + "/" + files.Length + " files", token);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    result.Errors.Add(new LayerLoadError { Path = path, Message = ex.Message });
                    UpdateLoadStatus("Skipped " + fileNumber + "/" + files.Length + ": " + Path.GetFileName(path), token);
                }
            }
            return result;
        }

        private void UpdateLoadStatus(string text, CancellationToken token)
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke((Action)(() =>
            {
                if (!token.IsCancellationRequested)
                    lblStatus.Text = text;
            }));
        }

        private sealed class LoadFilesResult
        {
            public int TotalCount;
            public int LoadedCount;
            public int Warnings;
            public int PrimitiveCount;
            public readonly List<GerberLayer> Layers = new List<GerberLayer>();
            public readonly List<LayerLoadError> Errors = new List<LayerLoadError>();
        }

        private sealed class LayerLoadError
        {
            public string Path;
            public string Message;
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

        private RasterExportOptions BuildExportOptions()
        {
            return new RasterExportOptions
            {
                Dpi = int.Parse(tscDpi.SelectedItem.ToString()),
                Mode = tscMode.SelectedIndex == 1 ? ColorMode.BinaryMask : ColorMode.Realistic
            };
        }

        private PreviewSettings BuildPreviewSettings()
        {
            return new PreviewSettings
            {
                ViewportWidthPx = Math.Max(1, canvas.ClientSize.Width),
                ViewportHeightPx = Math.Max(1, canvas.ClientSize.Height),
                Mode = tscMode.SelectedIndex == 1 ? ColorMode.BinaryMask : ColorMode.Realistic
            };
        }

        private void tsbRender_Click(object sender, EventArgs e) { RenderPreviewAsync(true); }
        private void tsbFit_Click(object sender, EventArgs e)
        {
            if (svgViewer.Visible && svgViewer.IsAvailable) svgViewer.FitToView();
            else { canvas.FitToView(); UpdateZoomLabel(); }
        }

        private async void InitializeSvgViewerAsync()
        {
            await svgViewer.EnsureInitializedAsync();
            if (svgViewer.IsAvailable)
            {
                svgViewer.Visible = true;
                canvas.Visible = false;
                canvas.SetImage(null, false);
            }
        }

        private SvgRenderOptions BuildSvgOptions()
        {
            return new SvgRenderOptions
            {
                Mode = tscMode.SelectedIndex == 1 ? ColorMode.BinaryMask : ColorMode.Realistic,
                IncludeBackground = true
            };
        }

        private GerberScene CreateVisibleSceneSnapshot()
        {
            var scene = new GerberScene();
            foreach (GerberLayer layer in _engine.Layers)
                if (layer.Visible) scene.AddLayer(layer);
            return scene;
        }

        private void RenderPreviewAsync(bool fit)
        {
            if (_engine.GetCombinedBoundsMm().IsEmpty)
            {
                canvas.SetImage(null, false);
                DisposePreviewRenderResult();
                lblBoardSize.Text = "";
                return;
            }

            if (_previewCts != null) _previewCts.Cancel();
            CancellationTokenSource currentRender = new CancellationTokenSource();
            _previewCts = currentRender;
            CancellationToken token = currentRender.Token;

            _rendering = true;
            lblStatus.Text = svgViewer.IsAvailable ? "Generating SVG preview..." : "Generating bitmap fallback preview...";
            bool useSvg = svgViewer.IsAvailable;
            GerberScene scene = CreateVisibleSceneSnapshot();
            SvgRenderOptions svgOptions = BuildSvgOptions();
            PreviewSettings previewSettings = BuildPreviewSettings();

            Task.Run<object>(() =>
            {
                token.ThrowIfCancellationRequested();
                if (useSvg) return new GerberSvgRenderer().Render(scene, svgOptions, token);
                return _engine.RenderCombinedViewportBitmap(previewSettings);
            }, token).ContinueWith(task =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((Action)(async () =>
                {
                    if (!ReferenceEquals(_previewCts, currentRender)) return;
                    _rendering = false;
                    _previewCts = null;
                    currentRender.Dispose();
                    if (task.IsCanceled) return;
                    if (task.IsFaulted)
                    {
                        string msg = task.Exception.GetBaseException().Message;
                        lblStatus.Text = "Loi render: " + msg;
                        MessageBox.Show(this, msg, "Render", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DisposePreviewRenderResult();
                    RectangleD b = scene.GetBoundsMm();
                    lblBoardSize.Text = string.Format("Board: {0:0.##} x {1:0.##} mm", b.Width, b.Height);

                    if (useSvg)
                    {
                        try
                        {
                            await svgViewer.LoadSvg((string)task.Result);
                            svgViewer.Visible = true;
                            canvas.Visible = false;
                            canvas.SetImage(null, false);
                            if (fit) svgViewer.FitToView();
                            lblStatus.Text = "Ready (SVG/WebView2 preview, Export DPI independent)";
                            UpdateZoomLabel();
                            return;
                        }
                        catch
                        {
                            svgViewer.Visible = false;
                            canvas.Visible = true;
                            RenderPreviewAsync(fit);
                            return;
                        }
                    }

                    _previewRenderResult = (PreviewBitmapRenderResult)task.Result;
                    canvas.Visible = true;
                    svgViewer.Visible = false;
                    canvas.SetImage(_previewRenderResult.Bitmap, fit);
                    lblStatus.Text = "Ready (bitmap fallback preview, Export DPI independent)";
                    UpdateZoomLabel();
                }));
            });
        }

        private void DisposePreviewRenderResult()
        {
            if (_previewRenderResult != null)
            {
                _previewRenderResult.Dispose();
                _previewRenderResult = null;
            }
        }

        // ---------- Toa do chuot (FR-009) ----------

        private void Canvas_ImageCursorMoved(object sender, PointF? imagePx)
        {
            UpdateZoomLabel();
            if (imagePx == null || _previewRenderResult == null)
            {
                lblCoords.Text = "X: -  Y: -";
                return;
            }
            PointD mm = _previewRenderResult.ImagePixelToMm(imagePx.Value.X, imagePx.Value.Y);
            lblCoords.Text = string.Format("X: {0:0.###} mm ({1:0.####}\")  Y: {2:0.###} mm ({3:0.####}\")",
                mm.X, mm.X / 25.4, mm.Y, mm.Y / 25.4);
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
                    RasterExportOptions opts = BuildExportOptions();
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
                    _engine.ExportCombinedPng(BuildExportOptions(), dlg.FileName);
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
