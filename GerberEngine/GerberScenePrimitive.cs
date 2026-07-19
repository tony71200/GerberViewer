// GerberEngine/GerberScenePrimitive.cs
// Renderer-independent scene primitive contract. All coordinates and bounds are double millimeters.

namespace GerberEngine
{
    /// <summary>
    /// Base scene primitive with geometry expressed only in double millimeters.
    /// </summary>
    public abstract class GerberScenePrimitive
    {
        public GerberPolarity Polarity = GerberPolarity.Dark;
        public abstract RectangleD GetBoundsMm();
    }
}
