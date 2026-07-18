// GerberEngine/GerberEngine.cs
// FACADE API cong khai cua GerberEngine (BR-003, NFR-004).
// Day la hop dong on dinh de tai su dung: WinForms app, console tool, service...
// Chi phu thuoc System.Drawing - KHONG phu thuoc System.Windows.Forms.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace GerberEngine
{
    /// <summary>Tuy chon render/xuat anh (FR-010, FR-011).</summary>
    public sealed class RenderOptions
    {
        public int Dpi = 600;                            // 150/300/600/1200
        public ColorMode Mode = ColorMode.Realistic;
        public double MarginMm = 2.0;
        public bool InvertBinary = false;                // Binary: false = net trang/nen den
        public Color? BackgroundOverride = null;         // null = tu chon theo Mode

        public Color ResolveBackground()
        {
            if (BackgroundOverride.HasValue) return BackgroundOverride.Value;
            if (Mode == ColorMode.BinaryMask) return InvertBinary ? Color.White : Color.Black;
            return GerberRenderer.RealisticBackground;
        }

        public Color ResolveForeground(GerberLayer layer)
        {
            if (Mode == ColorMode.BinaryMask) return InvertBinary ? Color.Black : Color.White;
            return layer.DisplayColor;
        }
    }

    public sealed class RenderProgressEventArgs : EventArgs
    {
        public int Done, Total;
        public RenderProgressEventArgs(int done, int total) { Done = done; Total = total; }
    }

    /// <summary>
    /// Facade: quan ly danh sach lop + render + export.
    /// Vong doi Bitmap tra ve thuoc ve CALLER (caller phai Dispose) - xem Spec 5.1.7.
    /// Thread-safety: cac phuong thuc render duoc phep goi tu worker thread,
    /// nhung KHONG duoc thay doi danh sach Layers song song voi render.
    /// </summary>
    public sealed class GerberEngineFacade
    {
        private readonly List<GerberLayer> _layers = new List<GerberLayer>();
        private readonly GerberRenderer _renderer = new GerberRenderer();

        public IReadOnlyList<GerberLayer> Layers { get { return _layers; } }

        public event EventHandler<RenderProgressEventArgs> RenderProgress;

        // ---------- Quan ly lop (FR-003, FR-004) ----------

        /// <summary>Parse file, tu nhan dien loai lop, gan mau realistic mac dinh.</summary>
        public GerberLayer LoadLayer(string filePath)
        {
            GerberLayer layer = new GerberParser().ParseFile(filePath);
            layer.DisplayColor = GerberRenderer.DefaultColor(layer.Type, ColorMode.Realistic);
            _layers.Add(layer);
            return layer;
        }

        public void RemoveLayer(GerberLayer layer) { _layers.Remove(layer); }

        public void MoveLayer(GerberLayer layer, int newIndex)
        {
            int old = _layers.IndexOf(layer);
            if (old < 0 || newIndex < 0 || newIndex >= _layers.Count) return;
            _layers.RemoveAt(old);
            _layers.Insert(newIndex, layer);
        }

        public void Clear() { _layers.Clear(); }

        /// <summary>Bbox hop nhat cac lop visible (mm). Empty neu khong co gi.</summary>
        public RectangleD GetCombinedBoundsMm()
        {
            RectangleD b = RectangleD.Empty;
            foreach (GerberLayer l in _layers)
                if (l.Visible) b.Expand(l.GetBoundsMm());
            return b;
        }

        /// <summary>Transformer dung chung cho render va doi chieu toa do chuot (FR-008, FR-009).</summary>
        public CoordinateTransformer CreateTransformer(RenderOptions options)
        {
            return new CoordinateTransformer(GetCombinedBoundsMm(), options.Dpi, options.MarginMm);
        }

        // ---------- Render (FR-010..FR-014) ----------

        /// <summary>Render MOT lop tren nen dac. Bbox dung bbox hop nhat de cac lop chong khit nhau.</summary>
        public Bitmap RenderLayer(GerberLayer layer, RenderOptions options)
        {
            CoordinateTransformer t = CreateTransformer(options);
            return _renderer.RenderLayerOpaque(layer, t, options.ResolveForeground(layer), options.ResolveBackground());
        }

        /// <summary>Render gop tat ca lop visible (thu tu danh sach = duoi len tren).</summary>
        public Bitmap RenderCombined(RenderOptions options)
        {
            CoordinateTransformer t = CreateTransformer(options);
            return _renderer.RenderCombined(_layers, t, options.Mode, options.ResolveBackground(), OnProgress);
        }

        private void OnProgress(int done, int total)
        {
            EventHandler<RenderProgressEventArgs> h = RenderProgress;
            if (h != null) h(this, new RenderProgressEventArgs(done, total));
        }

        // ---------- Export PNG (FR-012) ----------

        public void ExportLayerPng(GerberLayer layer, RenderOptions options, string outputPath)
        {
            using (Bitmap bmp = RenderLayer(layer, options))
                SavePng(bmp, options.Dpi, outputPath);
        }

        public void ExportCombinedPng(RenderOptions options, string outputPath)
        {
            using (Bitmap bmp = RenderCombined(options))
                SavePng(bmp, options.Dpi, outputPath);
        }

        private static void SavePng(Bitmap bmp, int dpi, string path)
        {
            bmp.SetResolution(dpi, dpi);   // ghi metadata DPI vao PNG
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
