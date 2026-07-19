// GerberEngine/GerberEngine.cs
// GerberEngine's public FACADE API (BR-003, NFR-004).
// This is a stable contract for reuse: WinForms app, console tool, service...
// Depends only on System.Drawing - NOT dependent on System.Windows.Forms.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace GerberEngine
{
    /// <summary>
    /// Raster PNG export options (FR-010, FR-011).
    /// </summary>
    public sealed class RasterExportOptions
    {
        public int Dpi = 600;                            // 150/300/600/1200
        public ColorMode Mode = ColorMode.Realistic;
        public double MarginMm = 2.0;
        public bool InvertBinary = false;                // Binary: false = net trang/nen den
        public Color? BackgroundOverride = null;

        public Color ResolveBackground()
        {
            if (BackgroundOverride.HasValue) return BackgroundOverride.Value;
            if (Mode == ColorMode.BinaryMask) return InvertBinary ? Color.White : Color.Black;
            return GerberRasterExportRenderer.RealisticBackground;
        }

        public Color ResolveForeground(GerberLayer layer)
        {
            if (Mode == ColorMode.BinaryMask) return InvertBinary ? Color.Black : Color.White;
            return layer.DisplayColor;
        }
    }

    public sealed class PreviewSettings
    {
        public int ViewportWidthPx = 1000;
        public int ViewportHeightPx = 1000;
        public ColorMode Mode = ColorMode.Realistic;
        public double MarginMm = 2.0;
        public Color? BackgroundOverride = null;

        public Color ResolveBackground()
        {
            if (BackgroundOverride.HasValue) return BackgroundOverride.Value;
            if (Mode == ColorMode.BinaryMask) return Color.Black;
            return GerberRasterExportRenderer.RealisticBackground;
        }
    }

    public sealed class PreviewBitmapRenderResult : IDisposable
    {
        private readonly CoordinateTransformer _transformer;

        internal PreviewBitmapRenderResult(Bitmap bitmap, CoordinateTransformer transformer)
        {
            Bitmap = bitmap;
            _transformer = transformer;
        }

        public Bitmap Bitmap { get; private set; }
        public RectangleD ContentBoundsMm { get { return _transformer.ContentBoundsMm; } }

        public PointD ImagePixelToMm(float px, float py)
        {
            return _transformer.ToMm(px, py);
        }

        public void Dispose()
        {
            if (Bitmap != null) { Bitmap.Dispose(); Bitmap = null; }
        }
    }

    public sealed class RenderProgressEventArgs : EventArgs
    {
        public int Done, Total;
        public RenderProgressEventArgs(int done, int total)
        {
            this.Done = done;
            this.Total = total;
        }
    }
    /// <summary>
    /// Facade: manages layer list + render + export.
    /// Bitmap return loop belongs to CALLER (caller must Dispose) - see Spec 5.1.7.
    /// Thread-safety: render methods are allowed to be called from worker threads,
    /// but the Layer list cannot be changed in parallel with rendering.
    /// </summary>
    public sealed class GerberEngineFacade
    {
        private readonly List<GerberLayer> _layers = new List<GerberLayer>();
        private readonly GerberRasterExportRenderer _rasterExportRenderer = new GerberRasterExportRenderer();

        public IReadOnlyList<GerberLayer> Layers { get { return _layers; } }
        public event EventHandler<RenderProgressEventArgs> RenderProgress;

        // Class Management (FR-003, FR-004)
        /// <summary>
        /// Parse file, automatically identify class type, assign default realistic color.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public GerberLayer LoadLayer(string filePath)
        {
            GerberLayer layer = new GerberParser().ParseFile(filePath);
            layer.DisplayColor = GerberRasterExportRenderer.DefaultColor(layer.Type, ColorMode.Realistic);
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

        /// <summary>
        /// Parse files into a renderer-independent scene with no DPI or Bitmap dependencies.
        /// </summary>
        public GerberScene LoadScene(IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            return new GerberSceneBuilder().Build(filePaths, cancellationToken);
        }
        /// <summary>
        /// Bbox combines most visible layers (mm). Empty if nothing.
        /// </summary>
        /// <returns></returns>
        public RectangleD GetCombinedBoundsMm()
        {
            RectangleD b = RectangleD.Empty;
            foreach (GerberLayer l in _layers)
                if (l.Visible) b.Expand(l.GetBoundsMm());
            return b;
        }
        /// <summary>
        /// Transformers are used for both rendering and reversing mouse coordinates (FR-008, FR-009).
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public CoordinateTransformer CreateExportTransformer(RasterExportOptions options)
        {
            return new CoordinateTransformer(GetCombinedBoundsMm(), options.Dpi, options.MarginMm);
        }

        [Obsolete("Use CreateExportTransformer because this transformer is tied to raster export DPI.")]
        public CoordinateTransformer CreateTransformer(RasterExportOptions options)
        {
            return CreateExportTransformer(options);
        }

        public PreviewBitmapRenderResult RenderCombinedViewportBitmap(PreviewSettings options)
        {
            if (options == null) throw new ArgumentNullException("options");
            RectangleD bounds = GetCombinedBoundsMm();
            if (bounds.IsEmpty) throw new InvalidOperationException("The bounding box is empty - there is no content to render.");
            double widthMm = bounds.Width + 2 * options.MarginMm;
            double heightMm = bounds.Height + 2 * options.MarginMm;
            double pxPerMm = Math.Min(options.ViewportWidthPx / widthMm, options.ViewportHeightPx / heightMm);
            int dpi = Math.Max(1, (int)Math.Floor(pxPerMm * 25.4));
            CoordinateTransformer t = new CoordinateTransformer(bounds, dpi, options.MarginMm);
            Bitmap bmp = _rasterExportRenderer.RenderCombinedForExport(_layers, t, options.Mode, options.ResolveBackground(), OnProgress);
            return new PreviewBitmapRenderResult(bmp, t);
        }

        [Obsolete("Use RenderLayerForExport to make DPI-based raster export usage explicit.")]
        public Bitmap RenderLayer(GerberLayer layer, RasterExportOptions options)
        {
            return RenderLayerForExport(layer, options);
        }

        [Obsolete("Use RenderCombinedForExport to make DPI-based raster export usage explicit.")]
        public Bitmap RenderCombined(RasterExportOptions options)
        {
            return RenderCombinedForExport(options);
        }

        // ---------- Render (FR-010..FR-014) ----------
        /// <summary>
        /// Render ONE layer on top, so it should be solid. Use a Bbox to ensure the layers fit together tightly.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Bitmap RenderLayerForExport(GerberLayer layer, RasterExportOptions options)
        {
            CoordinateTransformer t = CreateExportTransformer(options);
            return _rasterExportRenderer.RenderLayerOpaqueForExport(layer, t, options.ResolveForeground(layer), options.ResolveBackground());
        }
        /// <summary>
        /// Render combines all visible layers (list order = bottom to top).
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public Bitmap RenderCombinedForExport(RasterExportOptions options)
        {
            CoordinateTransformer t = CreateExportTransformer(options);
            return _rasterExportRenderer.RenderCombinedForExport(_layers, t, options.Mode, options.ResolveBackground(), OnProgress);
        }

        private void OnProgress(int done, int total)
        {
            EventHandler<RenderProgressEventArgs> h = RenderProgress;
            if (h != null) h(this, new RenderProgressEventArgs(done, total));
        }

        // ---------- Export PNG (FR-012) ----------

        public void ExportLayerPng(GerberLayer layer, RasterExportOptions options, string outputPath)
        {
            using (Bitmap bmp = RenderLayerForExport(layer, options))
                SavePng(bmp, options.Dpi, outputPath);
        }

        public void ExportCombinedPng(RasterExportOptions options, string outputPath)
        {
            using (Bitmap bmp = RenderCombinedForExport(options))
                SavePng(bmp, options.Dpi, outputPath);
        }

        private static void SavePng(Bitmap bmp, int dpi, string path)
        {
            bmp.SetResolution(dpi, dpi);   // Write DPI metadata to PNG
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
