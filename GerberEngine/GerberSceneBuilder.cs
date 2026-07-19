// GerberEngine/GerberSceneBuilder.cs
// Builds renderer-independent Gerber scenes with no DPI or Bitmap dependencies.
using System.Collections.Generic;
using System.Threading;

namespace GerberEngine
{
    /// <summary>
    /// Parses Gerber files and assembles a scene-domain model.
    /// </summary>
    public sealed class GerberSceneBuilder
    {
        public GerberScene Build(IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            var scene = new GerberScene();
            foreach (string filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GerberLayer parsed = new GerberParser().ParseFile(filePath);
                scene.AddLayer(parsed);
            }

            return scene;
        }
    }
}
