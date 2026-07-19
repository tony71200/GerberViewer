using System.Collections.Generic;
using System.Drawing;

namespace GerberEngine
{
    public sealed class GerberScene
    {
        private readonly List<GerberSceneLayer> _layers = new List<GerberSceneLayer>();

        public IReadOnlyList<GerberSceneLayer> Layers { get { return _layers; } }
        public RectangleD BoundsMm { get; private set; }

        public GerberScene(IEnumerable<GerberLayer> layers)
        {
            BoundsMm = RectangleD.Empty;
            foreach (GerberLayer layer in layers)
            {
                GerberSceneLayer sceneLayer = new GerberSceneLayer(layer);
                _layers.Add(sceneLayer);
                if (sceneLayer.Visible) BoundsMm.Expand(sceneLayer.BoundsMm);
            }
        }
    }

    public sealed class GerberSceneLayer
    {
        public GerberLayer SourceLayer { get; private set; }
        public string FileName { get { return SourceLayer.FileName; } }
        public LayerType Type { get { return SourceLayer.Type; } }
        public bool Visible { get { return SourceLayer.Visible; } }
        public Color DisplayColor { get { return SourceLayer.DisplayColor; } }
        public IReadOnlyList<GerberPrimitive> Primitives { get { return SourceLayer.Primitives; } }
        public RectangleD BoundsMm { get; private set; }

        public GerberSceneLayer(GerberLayer sourceLayer)
        {
            SourceLayer = sourceLayer;
            BoundsMm = sourceLayer.GetBoundsMm();
        }
    }
}
