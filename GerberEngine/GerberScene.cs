// GerberEngine/GerberScene.cs
// Renderer-independent Gerber scene. All coordinates and bounds are double millimeters.
using System.Collections.Generic;

namespace GerberEngine
{
    /// <summary>
    /// Parsed Gerber scene containing only domain data in millimeters.
    /// </summary>
    public sealed class GerberScene
    {
        private readonly List<GerberSceneLayer> _layers = new List<GerberSceneLayer>();

        public IReadOnlyList<GerberSceneLayer> Layers { get { return _layers; } }

        internal void AddLayer(GerberSceneLayer layer)
        {
            _layers.Add(layer);
        }

        public RectangleD GetBoundsMm()
        {
            RectangleD b = RectangleD.Empty;
            foreach (GerberSceneLayer layer in _layers) b.Expand(layer.GetBoundsMm());
            return b;
        }
    }
}
