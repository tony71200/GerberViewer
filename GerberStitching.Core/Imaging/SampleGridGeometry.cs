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
            var x = Boundaries(processedWidth, config.Columns, ox); var y = Boundaries(processedHeight, config.Rows, oy);
            var coords = PhysicalOrder(config).ToList();
            var layout = new SampleGridLayout { Rows = config.Rows, Columns = config.Columns, TileWidth = x.Length > 1 ? x[1] - x[0] : processedWidth, TileHeight = y.Length > 1 ? y[1] - y[0] : processedHeight, StepX = x.Length > 2 ? x[2] - x[1] : 0, StepY = y.Length > 2 ? y[2] - y[1] : 0 };
            for (int i = 0; i < coords.Count; i++)
            {
                var p = coords[i];
                layout.Tiles.Add(new SampleTileLayout { Row = p.Item1, Column = p.Item2, OrderIndex = i, Rectangle = Rectangle.FromLTRB(x[p.Item2], y[p.Item1], x[p.Item2 + 1], y[p.Item1 + 1]), Predecessor = i == 0 ? (int?)null : i - 1, Successor = i + 1 == coords.Count ? (int?)null : i + 1, Status = SampleTileState.Pending });
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
        private static int[] Boundaries(int size, int count, double overlap) { var b = new int[count + 1]; b[0] = 0; b[count] = size; var tile = (size + (count - 1) * overlap) / count; for (int i = 1; i < count; i++) b[i] = Math.Max(0, Math.Min(size, (int)Math.Round(i * (tile - overlap)))); return b; }
    }
}
