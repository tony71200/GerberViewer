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
        private readonly GerberRasterExportRenderer _renderer = new GerberRasterExportRenderer();
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
        /// Bbox combines most visible layers (mm). Empty if nothing.
        /// </summary>
        /// <returns></returns>
        public RectangleD GetCombinedBoundsMm()
        {
            return BuildScene().GetCombinedBoundsMm();
        }

        public GerberScene BuildScene()
        {
            var scene = new GerberScene();
            for (int i = 0; i < _layers.Count; i++)
            {
                GerberLayer layer = _layers[i];
                scene.Layers.Add(new GerberSceneLayer
                {
                    SourceLayer = layer,
                    Id = "layer-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Name = layer.FileName,
                    Type = layer.Type,
                    Visible = layer.Visible,
                    DisplayColor = layer.DisplayColor,
                    Primitives = layer.Primitives
                });
            }
            return scene;
        }
        /// <summary>
        /// Transformers are used for both rendering and reversing mouse coordinates (FR-008, FR-009).
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private CoordinateTransformer CreateTransformer(RasterExportOptions options)
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
        private Bitmap RenderLayerForExport(GerberLayer layer, RasterExportOptions options)
        {
            CoordinateTransformer t = CreateTransformer(options);
            return _renderer.RenderLayerOpaque(layer, t, options.ResolveForeground(layer), options.ResolveBackground());
        }
        /// <summary>
        /// Render combines all visible layers (list order = bottom to top).
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private Bitmap RenderCombinedForExport(RasterExportOptions options)
        {
            CoordinateTransformer t = CreateTransformer(options);
            return _renderer.RenderCombined(_layers, t, options.Mode, options.ResolveBackground(), OnProgress);
        }


        public string RenderCombinedSvg(SvgRenderOptions options)
        {
            return _svgRenderer.RenderCombinedSvg(BuildScene(), options);
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
