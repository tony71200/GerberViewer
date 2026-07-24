using System;
using CoreAlignStitchConfig = GerberViewer.Stitching.Models.AlignStitchConfig;

namespace GerberViewer.Workflow.Models
{
    /// <summary>
    /// Shared workflow state passed between tabs without exposing private controls.
    /// </summary>
    public sealed class WorkflowContext
    {
        public string SampleRasterPath { get; set; }
        public SampleGerberConfig SampleConfig { get; set; } = new SampleGerberConfig();
        public string ManifestPath { get; set; }
        public string OutputDirectory { get; set; }
        public CoreAlignStitchConfig AlignStitchConfig { get; set; } = new CoreAlignStitchConfig();
        public string LastStitchedOutputPath { get; set; }

        public event EventHandler Changed;

        public void NotifyChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public sealed class SampleGerberConfig
    {
        public string SourceRasterPath { get; set; }
        public int Dpi { get; set; } = 600;
        public string ColorMode { get; set; } = "Realistic";
    }

}
