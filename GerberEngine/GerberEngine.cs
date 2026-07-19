// GerberEngine/GerberEngine.cs
// GerberEngine's public FACADE API (BR-003, NFR-004).
// This is a stable contract for reuse: WinForms app, console tool, service...
// Depends only on System.Drawing - NOT dependent on System.Windows.Forms.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace GerberEngine
{
    /// <summary>
    /// Render/export options (FR-010, FR-011).
    /// </summary>
    public sealed class RenderOptions
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
        private readonly GerberRenderer _renderer = new GerberRenderer();
        private readonly GerberRasterExportRenderer _rasterExportRenderer = new GerberRasterExportRenderer();
        private readonly GerberSvgRenderer _svgRenderer = new GerberSvgRenderer();

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
        /// <summary>
        /// Bbox combines most visible layers (mm). Empty if nothing.
        /// </summary>
        /// <returns></returns>
        public RectangleD GetCombinedBoundsMm()
        {
            return BuildScene().BoundsMm;
        }

        public GerberScene BuildScene()
        {
            return new GerberScene(_layers);
        }

        public string RenderSvg(SvgRenderOptions options)
        {
            return _svgRenderer.Render(BuildScene(), options);
        }
        /// <summary>
        /// Transformers are used for both rendering and reversing mouse coordinates (FR-008, FR-009).
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public CoordinateTransformer CreateTransformer(RenderOptions options)
        {
            return new CoordinateTransformer(GetCombinedBoundsMm(), options.Dpi, options.MarginMm);
        }

        // ---------- Render (FR-010..FR-014) ----------
        /// <summary>
        /// Render ONE layer on top, so it should be solid. Use a Bbox to ensure the layers fit together tightly.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public Bitmap RenderLayer(GerberLayer layer, RenderOptions options)
        {
            CoordinateTransformer t = CreateTransformer(options);
            return _renderer.RenderLayerOpaque(layer, t, options.ResolveForeground(layer), options.ResolveBackground());
        }
        /// <summary>
        /// Render combines all visible layers (list order = bottom to top).
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
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
            RasterExportOptions exportOptions = ToRasterExportOptions(options);
            GerberScene scene = BuildScene();
            GerberSceneLayer sceneLayer = FindSceneLayer(scene, layer);
            using (Bitmap bmp = _rasterExportRenderer.RenderLayer(sceneLayer, scene.BoundsMm, exportOptions))
                SavePng(bmp, exportOptions.Dpi, outputPath);
        }

        public void ExportCombinedPng(RenderOptions options, string outputPath)
        {
            RasterExportOptions exportOptions = ToRasterExportOptions(options);
            using (Bitmap bmp = _rasterExportRenderer.RenderCombined(BuildScene(), exportOptions, OnProgress))
                SavePng(bmp, exportOptions.Dpi, outputPath);
        }

        private static RasterExportOptions ToRasterExportOptions(RenderOptions options)
        {
            return new RasterExportOptions
            {
                Dpi = options.Dpi,
                Mode = options.Mode,
                MarginMm = options.MarginMm,
                Background = options.ResolveBackground(),
                InvertBinary = options.InvertBinary
            };
        }

        private static GerberSceneLayer FindSceneLayer(GerberScene scene, GerberLayer layer)
        {
            foreach (GerberSceneLayer sceneLayer in scene.Layers)
                if (object.ReferenceEquals(sceneLayer.SourceLayer, layer)) return sceneLayer;
            throw new ArgumentException("Layer is not part of this scene.");
        }

        private static void SavePng(Bitmap bmp, int dpi, string path)
        {
            bmp.SetResolution(dpi, dpi);   // Write DPI metadata to PNG
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
