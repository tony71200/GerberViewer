using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GerberViewer.Stitching.Models
{
    [DataContract]
    public sealed class SampleManifest
    {
        public const int CurrentVersion = 1;
        [DataMember(Order = 1)] public int ManifestVersion { get; set; }
        [DataMember(Order = 2)] public string RootDirectory { get; set; }
        [DataMember(Order = 3)] public string SourceRasterPath { get; set; }
        [DataMember(Order = 4)] public int SourceWidth { get; set; }
        [DataMember(Order = 5)] public int SourceHeight { get; set; }
        [DataMember(Order = 6)] public int ProcessedWidth { get; set; }
        [DataMember(Order = 7)] public int ProcessedHeight { get; set; }
        [DataMember(Order = 8)] public string CropOrder { get; set; }
        [DataMember(Order = 9)] public string StartOrder { get; set; }
        [DataMember(Order = 10)] public List<SampleTileInfo> Tiles { get; set; }
        [DataMember(Order = 11)] public DateTime CreatedUtc { get; set; }
    }

    [DataContract]
    public sealed class SampleTileInfo
    {
        [DataMember(Order = 1)] public int OrderIndex { get; set; }
        [DataMember(Order = 2)] public int Row { get; set; }
        [DataMember(Order = 3)] public int Column { get; set; }
        [DataMember(Order = 4)] public string ExpectedPath { get; set; }
        [DataMember(Order = 5)] public int ExpectedX { get; set; }
        [DataMember(Order = 6)] public int ExpectedY { get; set; }
        [DataMember(Order = 7)] public int Width { get; set; }
        [DataMember(Order = 8)] public int Height { get; set; }
    }

    public sealed class SampleManifestValidationResult
    {
        public List<string> Errors { get; private set; }
        public bool IsValid { get { return Errors.Count == 0; } }
        public SampleManifestValidationResult() { Errors = new List<string>(); }
    }

    public static class SampleManifestSerializer
    {
        public static void WriteValidated(string path, SampleManifest manifest, bool requireFiles)
        {
            var result = SampleManifestValidator.Validate(manifest, requireFiles);
            if (!result.IsValid) throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using (var stream = File.Create(path)) new DataContractJsonSerializer(typeof(SampleManifest)).WriteObject(stream, manifest);
            var readback = Read(path);
            var readbackResult = SampleManifestValidator.Validate(readback, requireFiles);
            if (!readbackResult.IsValid) throw new InvalidOperationException("Manifest readback failed: " + string.Join(Environment.NewLine, readbackResult.Errors));
        }

        public static SampleManifest Read(string path)
        {
            using (var stream = File.OpenRead(path)) return (SampleManifest)new DataContractJsonSerializer(typeof(SampleManifest)).ReadObject(stream);
        }
    }

    public static class SampleManifestValidator
    {
        public static SampleManifestValidationResult Validate(SampleManifest manifest, bool requireFiles)
        {
            var result = new SampleManifestValidationResult();
            if (manifest == null) { result.Errors.Add("Manifest is null."); return result; }
            if (manifest.ManifestVersion != SampleManifest.CurrentVersion) result.Errors.Add("Unsupported ManifestVersion: " + manifest.ManifestVersion + ".");
            if (string.IsNullOrWhiteSpace(manifest.RootDirectory)) result.Errors.Add("RootDirectory is required.");
            if (manifest.ProcessedWidth <= 0 || manifest.ProcessedHeight <= 0) result.Errors.Add("Processed dimensions must be positive.");
            if (manifest.SourceWidth <= 0 || manifest.SourceHeight <= 0) result.Errors.Add("Source dimensions must be positive.");
            if (manifest.Tiles == null || manifest.Tiles.Count == 0) { result.Errors.Add("Tiles must not be empty."); return result; }
            var orders = new HashSet<int>(); var cells = new HashSet<string>();
            foreach (var t in manifest.Tiles)
            {
                if (t == null) { result.Errors.Add("Tile is null."); continue; }
                if (!orders.Add(t.OrderIndex)) result.Errors.Add("Duplicate OrderIndex: " + t.OrderIndex + ".");
                var cell = t.Row + "," + t.Column;
                if (!cells.Add(cell)) result.Errors.Add("Duplicate Row/Column: " + cell + ".");
                if (t.OrderIndex < 0) result.Errors.Add("Negative OrderIndex: " + t.OrderIndex + ".");
                if (t.Row < 0 || t.Column < 0) result.Errors.Add("Negative Row/Column at order " + t.OrderIndex + ".");
                if (t.ExpectedX < 0 || t.ExpectedY < 0) result.Errors.Add("Negative ExpectedX/Y at order " + t.OrderIndex + ".");
                if (t.Width <= 0 || t.Height <= 0) result.Errors.Add("Non-positive Width/Height at order " + t.OrderIndex + ".");
                if (t.ExpectedX + t.Width > manifest.ProcessedWidth || t.ExpectedY + t.Height > manifest.ProcessedHeight) result.Errors.Add("Tile outside processed image at order " + t.OrderIndex + ".");
                if (string.IsNullOrWhiteSpace(t.ExpectedPath)) result.Errors.Add("ExpectedPath missing at order " + t.OrderIndex + ".");
                else if (requireFiles && !File.Exists(t.ExpectedPath)) result.Errors.Add("ExpectedPath unreadable/missing at order " + t.OrderIndex + ": " + t.ExpectedPath);
            }
            for (int i = 0; i < manifest.Tiles.Count; i++) if (!orders.Contains(i)) result.Errors.Add("Missing OrderIndex: " + i + ".");
            return result;
        }
    }
}
