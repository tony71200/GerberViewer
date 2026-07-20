// GerberEngine/GerberSceneBuilder.cs
// Builds renderer-independent Gerber scenes with no DPI or Bitmap dependencies.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GerberEngine
{
    /// <summary>
    /// Parses Gerber files and assembles a scene-domain model.
    /// </summary>
    public sealed class GerberSceneBuilder
    {
        public GerberScene Build(IEnumerable<string> filePaths)
        {
            return Build(filePaths, CancellationToken.None, null);
        }

        public GerberScene Build(IEnumerable<string> filePaths, Action<string> reportProgress)
        {
            return Build(filePaths, CancellationToken.None, reportProgress);
        }

        public GerberScene Build(IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            return Build(filePaths, cancellationToken, null);
        }

        public GerberScene Build(IEnumerable<string> filePaths, CancellationToken cancellationToken, Action<string> reportProgress)
        {
            if (filePaths == null) throw new ArgumentNullException("filePaths");

            var scene = new GerberScene();
            int layerIndex = 0;
            foreach (string filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Report(reportProgress, "Reading file " + (layerIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                string content = File.ReadAllText(filePath);

                cancellationToken.ThrowIfCancellationRequested();
                Report(reportProgress, "Parsing commands");
                GerberLayer parsed = new GerberParser().ParseContent(filePath, content, cancellationToken, reportProgress);

                cancellationToken.ThrowIfCancellationRequested();
                Report(reportProgress, "Building scene");
                scene.AddLayer(parsed);
                layerIndex++;
            }

            cancellationToken.ThrowIfCancellationRequested();
            Report(reportProgress, "Calculating bounds");
            CalculateBounds(scene, cancellationToken, reportProgress);
            return scene;
        }

        private static void Report(Action<string> reportProgress, string stage)
        {
            if (reportProgress != null) reportProgress(stage);
        }

        private static RectangleD CalculateBounds(GerberScene scene, CancellationToken cancellationToken, Action<string> reportProgress)
        {
            RectangleD bounds = RectangleD.Empty;
            int layerIndex = 0;
            foreach (GerberSceneLayer layer in scene.Layers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Report(reportProgress, "Calculating bounds for layer " + (layerIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                int primitiveIndex = 0;
                foreach (GerberScenePrimitive primitive in layer.Primitives)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (primitiveIndex > 0 && primitiveIndex % 10000 == 0) Report(reportProgress, "Calculated bounds for " + primitiveIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + " primitives");
                    bounds.Expand(primitive.GetBoundsMm());
                    primitiveIndex++;
                }
                layerIndex++;
            }
            return bounds;
        }
    }
}
