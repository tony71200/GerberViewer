using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using GerberViewer.Stitching.RobotManager;

namespace GerberViewer.Stitching.Configuration
{
    [DataContract]
    public enum OverlapUnit 
    { 
        Pixel = 0, 
        Percent = 1 
    }
    [DataContract]
    public enum SamplePreprocessMode 
    { 
        None = 0, 
        Resize = 1, 
        FitPad = 2, 
        CenterCrop = 3 
    }
    [DataContract]
    public enum SampleOutputFormat 
    { 
        Tiff = 0,
        BigTiff = 1,
        Png = 2, 
        Bmp = 3, 
        Jpeg = 4,
    }

    [DataContract]
    public sealed class GerberSampleConfig
    {
        [DataMember]
        public string SourceRasterPath { get; set; }
        [DataMember]
        public string OutputDirectory { get; set; }
        [DataMember]
        public int Rows { get; set; } = 8;
        [DataMember]
        public int Columns { get; set; } = 10;
        [DataMember]
        public OrderMode CropOrder { get; set; } = OrderMode.Zigzag;
        [DataMember]
        public StartOrder StartOrder { get; set; } = StartOrder.TopLeftDown;
        [DataMember]
        public bool InvertImage { get; set; } = false;
        [DataMember]
        public double OverlapValue { get; set; } = 60;
        [DataMember]
        public OverlapUnit OverlapUnit { get; set; } = OverlapUnit.Pixel;
        [DataMember]
        public SamplePreprocessMode PreprocessMode { get; set; } = SamplePreprocessMode.None;
        [DataMember]
        public bool KeepAspectRatio { get; set; } = true;
        [DataMember]
        public SampleOutputFormat OutputFormat { get; set; } = SampleOutputFormat.Tiff;
        [DataMember]
        public string TileNamePattern { get; set; } = "Sample_R{row:00}_C{col:00}_O{order:000}";
        [DataMember]
        public int ProcessedWidth { get; set; } = 4096;
        [DataMember]
        public int ProcessedHeight { get; set; } = 4096;
        [IgnoreDataMember]
        public Color PadColor { get; set; } = Color.White;
    }

    public sealed class SampleConfigValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public bool IsValid { get { return Errors.Count == 0; } }
    }

    public static class GerberSampleConfigValidator
    {
        public static SampleConfigValidationResult Validate(
            GerberSampleConfig config, 
            Size sourceSize)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var result = new SampleConfigValidationResult();
            if (config.Rows < 1) 
                result.Errors.Add("Rows must be >= 1.");
            if (config.Columns < 1) 
                result.Errors.Add("Columns must be >= 1.");
            if (config.OverlapUnit == OverlapUnit.Percent && (config.OverlapValue < 0 || config.OverlapValue >= 100)) 
                result.Errors.Add("Percent overlap must satisfy 0 <= P < 100.");

            var processedWidth = config.ProcessedWidth > 0 ? config.ProcessedWidth : sourceSize.Width;
            var processedHeight = config.ProcessedHeight > 0 ? config.ProcessedHeight : sourceSize.Height;
            if (config.KeepAspectRatio && 
                config.PreprocessMode == SamplePreprocessMode.Resize && 
                config.ProcessedWidth > 0 && 
                config.ProcessedHeight > 0 && 
                sourceSize.Width > 0 && 
                sourceSize.Height > 0)
            {
                var sx = (double)config.ProcessedWidth / sourceSize.Width;
                var sy = (double)config.ProcessedHeight / sourceSize.Height;
                if (Math.Abs(sx - sy) > 0.0001) result.Errors.Add("Non-uniform Resize is rejected when KeepAspectRatio=true.");
            }

            if (config.Rows >= 1 && 
                config.Columns >= 1 && 
                processedWidth > 0 && 
                processedHeight > 0 && 
                config.OverlapUnit == OverlapUnit.Pixel)
            {
                var tileWidth = (processedWidth + Math.Max(0, config.Columns - 1) * config.OverlapValue) / config.Columns;
                var tileHeight = (processedHeight + Math.Max(0, config.Rows - 1) * config.OverlapValue) / config.Rows;
                if (tileWidth <= config.OverlapValue) result.Errors.Add("Pixel overlap makes tileWidth <= overlap.");
                if (tileHeight <= config.OverlapValue) result.Errors.Add("Pixel overlap makes tileHeight <= overlap.");
            }
            return result;
        }
    }
}
