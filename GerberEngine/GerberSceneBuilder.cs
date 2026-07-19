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
        public GerberScene Build(IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            return Build(filePaths, cancellationToken, null);
        }

        public GerberScene Build(IEnumerable<string> filePaths, CancellationToken cancellationToken, Action<string> reportProgress)
        {
            if (filePaths == null) throw new ArgumentNullException("filePaths");

            var scene = new GerberScene();
            foreach (string filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Report(reportProgress, "Reading file");
                string content = File.ReadAllText(filePath);

                cancellationToken.ThrowIfCancellationRequested();
                Report(reportProgress, "Parsing commands");
                GerberLayer parsed = new GerberParser().ParseContent(filePath, content);

                cancellationToken.ThrowIfCancellationRequested();
                Report(reportProgress, "Building scene");
                scene.AddLayer(parsed);
            }

            cancellationToken.ThrowIfCancellationRequested();
            Report(reportProgress, "Calculating bounds");
            scene.GetBoundsMm();
            return scene;
        }

        private static void Report(Action<string> reportProgress, string stage)
        {
            if (reportProgress != null) reportProgress(stage);
        }
    }
}
