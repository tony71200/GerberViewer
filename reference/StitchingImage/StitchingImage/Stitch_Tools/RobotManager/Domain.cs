using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StitchingImage.Stitch_Tools.RobotManager
{
    public sealed class ImageInfo
    {
        public ImageInfo(string filePath, int groupId, int imageId, int? positionId, double xRobot, double yRobot)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            GroupId = groupId;
            ImageId = imageId;
            PositionId = positionId;
            XRobot = xRobot;
            YRobot = yRobot;
        }

        public string FilePath { get; }
        public int GroupId { get; }
        public int ImageId { get; }
        public int? PositionId {  get; }
        public double XRobot { get; }
        public double YRobot { get; }

        public override string ToString() => $"G{GroupId} #{ImageId} P{(PositionId.HasValue ? PositionId.Value.ToString() : "None")} (x={XRobot:0.###}, y={YRobot:0.###})";
    }

    public enum OrderMode
    {
        Zigzag = 0,
        Branch = 1,
        BranchDown = 2
    }

    public enum StartCorner
    {
        TopLeft = 0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3
    }
    public enum RobotMovement
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3
    }

    // [Codex] [Change time: 260318] [Retain metadata-driven modes and expose business-friendly manual fallback choices]
//    public enum ClusterOrderMode
//    {
//        Coordinates = 0,
//        Position = 1
//    }
    public enum ClusterOrderMode
    {
        Coordinates = 0,
        Position = 1,
        ManualRow = 2,
        ManualColumn = 3
    }

    public sealed class OrderOptions
    {
        public double GapFactor { get; set; } = 2.0;
        public double RowFactor { get; set; } = 0.6;
        public bool InvertXOnParse { get; set; } = true;
        /// <summary>
        /// Business traversal directions: row0 RTL, row1 LTR, row2 RTL... when enabled.
        /// Used for both Zigzag and Branch row directions.
        /// Note: <see cref="OrderMode.Zigzag"/> is the business-facing enum value for Zigzag traversal.
        /// </summary>
        public OrderMode Mode { get; set; } = OrderMode.Branch;
        public StartCorner StartCorner { get; set; } = StartCorner.TopLeft;
        public RobotMovement RobotMovement { get; set; } = RobotMovement.Right;
        public ClusterOrderMode ClusterOrder { get; set; } = ClusterOrderMode.Coordinates;
        public int NodeInterval { get; set; } = 21;
    }

    /// <summary>
    /// Legacy RobotOrderer component used by the old ordering pipeline.
    /// Keep this type for compatibility while the traversal-first runtime path uses <see cref="TraversalComponent"/>.
    /// </summary>
    public sealed class OrderComponent
    {
        public int ComponentIndex { get; set; }
        public ImageInfo[] Points { get; set; }
        public Bounds2D Bounds { get; set; }
        public OrderGraph Graph { get; set; }
        public double EstimateDistanceX { get; set; }

        public double EstimateDistanceY { get; set; }
        public HashSet<(int AId, int BId)> SpecialGapEdges { get; set; }
    }

// [Codex] [Change time: 260319] [Document Zigzag business meaning without changing public enum names]
//    /// <summary>
//    /// Unified format: nodes + HNext/VNext links.
//    /// - HNext: next node in the current row (directional).
//    /// - VNext:
//    ///   - Branch: head(row i) -> head(row i+1)
//    ///   - BranchDown: tail(row i) -> head(row i+1)
//    ///   - Snake : tail(row i) -> head(row i+1)
//    /// </summary>
    /// <summary>
    /// Unified format: nodes + HNext/VNext links.
    /// - HNext: next node in the current row/column traversal (directional).
    /// - VNext:
    ///   - Branch: head(row i) -> head(row i+1)
    ///   - BranchDown: corresponding node(row i) -> node(row i+1)
    ///   - Zigzag business mode: tail(row i) -> head(row i+1)
    ///   Note: the public enum value is <see cref="OrderMode.Zigzag"/>.
    /// </summary>
    /// 
    public sealed class OrderGraph
    {
        public OrderMode Mode { get; set; }
        public OrderedRow[] Rows { get; set; }
        public Dictionary<int, NodeLinks> LinksById { get; set; } // key: ImageId
        public RobotMovement RobotMovement { get; set; }
    }

    public sealed class OrderedRow
    {
        public int RowIndex { get; set; }
        public ImageInfo[] Sequence { get; set; }  // already direction-applied
        public ImageInfo Head => (Sequence != null && Sequence.Length > 0) ? Sequence[0] : null;
        public ImageInfo Tail => (Sequence != null && Sequence.Length > 0) ? Sequence[Sequence.Length - 1] : null;
    }

    // [Codex] [Change time: 260320] [Add ImageGrid 2D layout type]
    /// <summary>
    /// A 2D physical layout of ImageInfo tiles, indexed by [row, col].
    /// Grid[r, c] is the tile at physical row r and column c — never reordered.
    /// Traversal direction per row is stored in RowForward[].
    /// </summary>
    public sealed class ImageGrid
    {
        /// <summary>Physical tile matrix. Grid[r, c] is stable and never reversed.</summary>
        public ImageInfo[,] Grid { get; }

        /// <summary>Number of rows in the grid.</summary>
        public int Rows { get; }

        /// <summary>Number of columns in the grid.</summary>
        public int Cols { get; }

        /// <summary>
        /// Traversal direction for each row.
        /// RowForward[r] = true  → traverse col 0..Cols-1 (left to right)
        /// RowForward[r] = false → traverse col Cols-1..0 (right to left)
        /// </summary>
        public bool[] RowForward { get; }

        /// <summary>
        /// Column index that serves as the spine for Branch mode.
        /// SpineCol = 0 when StartCorner is TopLeft or BottomLeft.
        /// SpineCol = Cols-1 when StartCorner is TopRight or BottomRight.
        /// </summary>
        public int SpineCol { get; }

        public ImageGrid(ImageInfo[,] grid, bool[] rowForward, int spineCol)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (rowForward == null) throw new ArgumentNullException(nameof(rowForward));

            Grid = grid;
            Rows = grid.GetLength(0);
            Cols = grid.GetLength(1);
            RowForward = rowForward;
            SpineCol = spineCol;
        }

        /// <summary>
        /// Returns the ImageInfo at physical position (r, c). Returns null if out of bounds.
        /// </summary>
        public ImageInfo At(int r, int c)
        {
            if (r < 0 || r >= Rows || c < 0 || c >= Cols)
                return null;

            return Grid[r, c];
        }

        /// <summary>
        /// Returns the traversal sequence for row r, respecting RowForward[r].
        /// </summary>
        public ImageInfo[] GetRowSequence(int r)
        {
            var seq = new ImageInfo[Cols];
            for (int c = 0; c < Cols; c++)
                seq[c] = RowForward[r] ? Grid[r, c] : Grid[r, Cols - 1 - c];

            return seq;
        }
    }

    public sealed class NodeLinks
    {
        public int ImageId { get; set; }
        public int? HNext { get; set; }
        public int? VNext { get; set; }
    }


    /// <summary>
    /// Legacy RobotOrderer batch result used by the old ordering pipeline.
    /// The active runtime traversal flow uses <see cref="TraversalBatchResult"/>.
    /// </summary>
    public sealed class OrderedGroupResult
    {
        public int GroupId { get; set; }
        public OrderComponent[] Components { get; set; }
        public double EstimateDistance { get; set; } = 0.0;
    }

    #region Links
    public class Link
    {
        public int ImageId { get; set; }

        // Graph 
        public int? HNext { get; set; }
        public int? VNext { get; set; }
        // Linear traver
        public int? Prev { get; set; }
        public int? Next { get; set; }
        // (layout/debug) links
        public int? LineNext { get; set; }
        public int? InterLineNext { get; set; }
    }
    #endregion

    #region Arrange
    public sealed class ArrangeGraph
    {
        /// <summary>
        /// Legacy alias (kept for source compatibility). Prefer using <see cref="Link"/>.
        /// </summary>
        /// 
        [Obsolete("Use Link")]
        public sealed class Links : Link { }
        /// <summary>ImageId -> Link record for this layout graph.</summary>
        public Dictionary<int, Link> LinksById { get; } = new Dictionary<int, Link>();
        /// <summary>Legacy alias of <see cref="LinksById"/>.</summary>
        /// 
        [Obsolete("Use LinksById")]
        public Dictionary<int, Link> ById => LinksById;

        public Link GetOrCreate(int imageId)
        {
            if (!LinksById.TryGetValue(imageId, out var links))
            {
                links = new Link { ImageId = imageId };
                LinksById[imageId] = links;
            }
            return links;
        }
    }

    public sealed class ArrangeComponent
    {
        public int Index { get; set; }
        public IReadOnlyList<ImageInfo> Items { get; set; }
        public List<List<ImageInfo>> Matrix { get; set; }
        public OrderOptions OptionsUsed { get; set; }
        public ArrangeGraph Graph { get; set; }

        // For drawing
        public Dictionary<int, GridCell> CellById { get; set; } = new Dictionary<int, GridCell>();
        public List<PathSegment> Path {  get; set; } = new List<PathSegment>();
        public int? StartId { get; set; }
    }

    public sealed class ArrangeBatchResult
    {
        public List<ArrangeComponent> Components { get; set; }
        public (double StepX, double StepY) TypicalStep {  get; set; }
    }

    // [Codex] [Change time: 260323] [Introduce traversal-first runtime result types built from RobotArrange + TraversalGraph]
    /// <summary>
    /// Active runtime traversal component built from <see cref="ArrangeComponent"/> + <see cref="TraversalGraph"/>.
    /// Preview/stitch consumers should use this type instead of <see cref="OrderComponent"/>.
    /// </summary>
    public sealed class TraversalComponent
    {
        public int ComponentIndex { get; set; }
        public ImageInfo[] Points { get; set; }
        public Bounds2D Bounds { get; set; }
        public TraversalGraph Graph { get; set; }
        public double EstimateDistanceX { get; set; }
        public double EstimateDistanceY { get; set; }
        public HashSet<(int AId, int BId)> SpecialGapEdges { get; set; }
        public ArrangeComponent ArrangeComponent { get; set; }
    }

    // [Codex] [Change time: 260323] [Introduce traversal-first runtime result types built from RobotArrange + TraversalGraph]
    /// <summary>
    /// Active runtime traversal batch result consumed by preview/stitch flows.
    /// </summary>
    public sealed class TraversalBatchResult
    {
        public int GroupId { get; set; }
        public TraversalComponent[] Components { get; set; }
        public double EstimateDistance { get; set; } = 0.0;
    }

    public enum LinkDirection
    {
        None = 0,
        Left, Right, Up, Down,
        Jump
    }

    public readonly struct GridCell
    {
        public int Row { get; }
        public int Col { get; }
        public GridCell(int row, int col)
        {
            Row = row; Col = col;
        }
        public override string ToString() => $"({Row}, {Col})";
    }

    public sealed class PathSegment
    {
        public int FromId { get; set; }
        public int ToId { get; set; }
        public GridCell FromCell { get; set; }
        public GridCell ToCell { get; set; }
        public LinkDirection Direction { get; set; }
    }

    #endregion

    public struct Bounds2D
    {
        public double MinX, MinY, MaxX, MaxY;
        private static bool IsFinite(double v)
            => !(double.IsNaN(v) || double.IsInfinity(v));

        public static Bounds2D FromPoints(ImageInfo[] pts)
        {
            if (pts == null || pts.Length == 0)
                return new Bounds2D { MinX = 0, MinY = 0, MaxX = 1, MaxY = 1 };

            double minX = double.MaxValue; 
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            int used = 0;
            for (int i = 0; i < pts.Length; i++)
            {
                var x = pts[i].XRobot;
                var y = pts[i].YRobot;
                if (!IsFinite(x) || !IsFinite(y)) continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                used++;
            }

            if (used == 0)
                return new Bounds2D { MinX = 0, MinY = 0, MaxX = 1, MaxY = 1 };

            // If all points share same X or Y, expand a bit to avoid span=0
            if (Math.Abs(maxX - minX) < 1e-9) { maxX = minX + 1; }
            if (Math.Abs(maxY - minY) < 1e-9) { maxY = minY + 1; }
            return new Bounds2D { MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY };
            
        }

        public static LinkDirection ComputeDirection(GridCell a, GridCell b)
        {
            if (a.Row < 0 || b.Row < 0) return LinkDirection.Jump;

            int dr = b.Row - a.Row;
            int dc = b.Col - a.Col;

            if (dr == 0 && dc == 1) return LinkDirection.Right;
            if (dr == 0 && dc == -1) return LinkDirection.Left;
            if (dr == 1 && dc == 0) return LinkDirection.Down;
            if (dr == -1 && dc == 0) return LinkDirection.Up;

            return LinkDirection.Jump;
        }
    }
}
