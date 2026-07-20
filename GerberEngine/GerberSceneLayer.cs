// GerberEngine/GerberSceneLayer.cs
// Renderer-independent layer data. All coordinates and bounds are double millimeters.
using System.Collections.Generic;

namespace GerberEngine
{
    /// <summary>
    /// One parsed Gerber file as scene-domain data, with no DPI, Bitmap, or UI display state.
    /// </summary>
    public class GerberSceneLayer
    {
        public string FilePath;
        public string FileName;
        public LayerType Type = LayerType.Unknown;
        public GerberUnit SourceUnit = GerberUnit.Millimeter;
        public List<GerberScenePrimitive> Primitives = new List<GerberScenePrimitive>();
        public List<string> Warnings = new List<string>();
        public GerberParserDiagnostics Diagnostics = new GerberParserDiagnostics();

        public RectangleD GetBoundsMm()
        {
            RectangleD b = RectangleD.Empty;
            foreach (GerberScenePrimitive p in Primitives) b.Expand(p.GetBoundsMm());
            return b;
        }
    }
}
