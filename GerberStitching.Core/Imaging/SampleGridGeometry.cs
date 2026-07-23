using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GerberViewer.Stitching.Configuration;
using GerberViewer.Stitching.RobotManager;

namespace GerberViewer.Stitching.Imaging
{
    public enum SampleTileState { Pending = 0, Processing = 1, Completed = 2, Failed = 3 }
    public sealed class SampleTileLayout
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int OrderIndex { get; set; }
        public Rectangle Rectangle { get; set; }
        public int? Predecessor { get; set; }
        public int? Successor { get; set; }
        public SampleTileState Status { get; set; }
    }
    public sealed class SampleGridLayout
    {
        public IList<SampleTileLayout> Tiles { get; set; } = new List<SampleTileLayout>();
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int StepX { get; set; }
        public int StepY { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int ExpectedTileCount { get { return Rows * Columns; } }
    }
    public static class SampleGeometryCalculator
    {
        public static SampleGridLayout Calculate(int processedWidth, int processedHeight, GerberSampleConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var validation = GerberSampleConfigValidator.Validate(config, new Size(processedWidth, processedHeight));
            if (!validation.IsValid) throw new InvalidOperationException(string.Join(Environment.NewLine, validation.Errors));
            double ox = config.OverlapUnit == OverlapUnit.Percent ? processedWidth / (double)config.Columns * config.OverlapValue / 100.0 : config.OverlapValue;
            double oy = config.OverlapUnit == OverlapUnit.Percent ? processedHeight / (double)config.Rows * config.OverlapValue / 100.0 : config.OverlapValue;
            var x = AxisSegments(processedWidth, config.Columns, ox);
            var y = AxisSegments(processedHeight, config.Rows, oy);
            var coords = PhysicalOrder(config).ToList();
            var layout = new SampleGridLayout { Rows = config.Rows, Columns = config.Columns, TileWidth = x.TileSize, TileHeight = y.TileSize, StepX = x.Step, StepY = y.Step };
            for (int i = 0; i < coords.Count; i++)
            {
                var p = coords[i];
                layout.Tiles.Add(new SampleTileLayout { Row = p.Item1, Column = p.Item2, OrderIndex = i, Rectangle = Rectangle.FromLTRB(x.Starts[p.Item2], y.Starts[p.Item1], x.Ends[p.Item2], y.Ends[p.Item1]), Predecessor = i == 0 ? (int?)null : i - 1, Successor = i + 1 == coords.Count ? (int?)null : i + 1, Status = SampleTileState.Pending });
            }
            return layout;
        }
        private static IEnumerable<Tuple<int,int>> PhysicalOrder(GerberSampleConfig c)
        {
            bool vertical = c.StartOrder == StartOrder.TopLeftDown || c.StartOrder == StartOrder.BottomRightUp;
            bool reverseRows = c.StartOrder == StartOrder.BottomRightLeft || c.StartOrder == StartOrder.BottomRightUp;
            bool reverseCols = c.StartOrder == StartOrder.BottomRightLeft || c.StartOrder == StartOrder.BottomRightUp;
            if (vertical)
            {
                for (int cc = 0; cc < c.Columns; cc++) { int col = reverseCols ? c.Columns - 1 - cc : cc; var rows = Enumerable.Range(0, c.Rows).Select(r => reverseRows ? c.Rows - 1 - r : r).ToList(); if (c.CropOrder == OrderMode.Zigzag && cc % 2 == 1) rows.Reverse(); foreach (var row in rows) yield return Tuple.Create(row, col); }
            }
            else
            {
                for (int rr = 0; rr < c.Rows; rr++) { int row = reverseRows ? c.Rows - 1 - rr : rr; var cols = Enumerable.Range(0, c.Columns).Select(col => reverseCols ? c.Columns - 1 - col : col).ToList(); if (c.CropOrder == OrderMode.Zigzag && rr % 2 == 1) cols.Reverse(); foreach (var col in cols) yield return Tuple.Create(row, col); }
            }
        }
        private sealed class AxisSegmentsResult { public int[] Starts; public int[] Ends; public int TileSize; public int Step; }
        private static AxisSegmentsResult AxisSegments(int size, int count, double overlap)
        {
            var starts = new int[count]; var ends = new int[count];
            if (count <= 1) return new AxisSegmentsResult { Starts = new[] { 0 }, Ends = new[] { size }, TileSize = size, Step = 0 };
            var tile = (size + (count - 1) * overlap) / count;
            var step = tile - overlap;
            var tileSize = Math.Max(1, (int)Math.Ceiling(tile));
            var stepSize = Math.Max(1, (int)Math.Round(step));
            for (int i = 0; i < count; i++)
            {
                var start = (int)Math.Round(i * step);
                var end = start + tileSize;
                if (i == count - 1) { end = size; start = Math.Max(0, end - tileSize); }
                starts[i] = Math.Max(0, Math.Min(size - 1, start));
                ends[i] = Math.Max(starts[i] + 1, Math.Min(size, end));
            }
            return new AxisSegmentsResult { Starts = starts, Ends = ends, TileSize = tileSize, Step = stepSize };
        }
    }
}
