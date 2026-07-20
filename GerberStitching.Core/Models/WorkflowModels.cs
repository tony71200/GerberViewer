using System;
using System.Collections.Generic;
using GerberViewer.Stitching.RobotManager;

namespace GerberViewer.Stitching.Models
{
    public enum PoseSource { SampleAlignment, NeighborAlignment, AnchorAdjusted, Interpolated, ExpectedGridOffset, Manual, Excluded, Failed }

    public sealed class GerberSampleConfig { public int Rows { get; set; } public int Columns { get; set; } public double TileWidth { get; set; } public double TileHeight { get; set; } public StartOrder StartOrder { get; set; } = StartOrder.TopLeftRight; }
    public sealed class AlignStitchConfig { public double MinOverlapRatio { get; set; } = 0.01; public double MaxAbsRotationDeg { get; set; } = 8.0; public bool EnforceRobotDirection { get; set; } = true; }
    public sealed class GerberWorkflowConfig { public GerberSampleConfig Sample { get; set; } = new GerberSampleConfig(); public AlignStitchConfig Alignment { get; set; } = new AlignStitchConfig(); }
    public sealed class SampleManifest { public string RootDirectory { get; set; } public IList<SampleTileInfo> Tiles { get; set; } = new List<SampleTileInfo>(); public DateTime CreatedUtc { get; set; } = DateTime.UtcNow; }
    public sealed class SampleTileInfo { public int Row { get; set; } public int Column { get; set; } public string ExpectedPath { get; set; } public double ExpectedX { get; set; } public double ExpectedY { get; set; } }
    public sealed class CapturedImageInfo { public string FilePath { get; set; } public int Row { get; set; } public int Column { get; set; } public double RobotX { get; set; } public double RobotY { get; set; } public DateTime CapturedUtc { get; set; } }
    public sealed class StitchImagePose { public int Row { get; set; } public int Column { get; set; } public double X { get; set; } public double Y { get; set; } public double RotationDeg { get; set; } public PoseSource Source { get; set; } }
    public sealed class ProcessingReport { public bool Succeeded { get; set; } public IList<string> Messages { get; set; } = new List<string>(); public IList<StitchImagePose> Poses { get; set; } = new List<StitchImagePose>(); }
}
