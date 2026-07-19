using System;
using System.Collections.Generic;
using System.Drawing;

namespace GerberEngine
{
    public sealed class GerberRasterExportRenderer
    {
        private readonly GerberRenderer _renderer = new GerberRenderer();

        public Bitmap RenderCombined(GerberScene scene, RasterExportOptions options, Action<int, int> progress)
        {
            CoordinateTransformer t = new CoordinateTransformer(scene.BoundsMm, options.Dpi, options.MarginMm);
            return _renderer.RenderCombined(ToLayerList(scene), t, options.Mode, ResolveBackground(options), progress);
        }

        public Bitmap RenderLayer(GerberSceneLayer layer, RectangleD sceneBoundsMm, RasterExportOptions options)
        {
            CoordinateTransformer t = new CoordinateTransformer(sceneBoundsMm, options.Dpi, options.MarginMm);
            return _renderer.RenderLayerOpaque(layer.SourceLayer, t, ResolveForeground(layer, options), ResolveBackground(options));
        }

        private static Color ResolveBackground(RasterExportOptions options)
        {
            if (options.Mode == ColorMode.BinaryMask) return options.InvertBinary ? Color.White : Color.Black;
            return options.Background;
        }

        private static Color ResolveForeground(GerberSceneLayer layer, RasterExportOptions options)
        {
            if (options.Mode == ColorMode.BinaryMask) return options.InvertBinary ? Color.Black : Color.White;
            return layer.DisplayColor;
        }

        private static IList<GerberLayer> ToLayerList(GerberScene scene)
        {
            List<GerberLayer> layers = new List<GerberLayer>();
            foreach (GerberSceneLayer layer in scene.Layers) layers.Add(layer.SourceLayer);
            return layers;
        }
    }
}
