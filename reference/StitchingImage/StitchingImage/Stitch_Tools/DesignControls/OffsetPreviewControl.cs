using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using StitchingImage.Stitch_Tools.Matcher;
using StitchingImage.Stitch_Tools.RobotManager;
using StitchingImage.Stitch_Tools.Utils;
using Point = OpenCvSharp.Point;

namespace StitchingImage.Stitch_Tools.DesignControls
{
    public partial class OffsetPreviewControl : UserControl
    {
        private const double PreviewMegapix = 2.0;
        private TraversalBatchResult _data;
        private List<EdgeInfo> _horizontalEdges = new List<EdgeInfo>();
        private List<EdgeInfo> _verticalEdges = new List<EdgeInfo>();
        private EdgeInfo _selectedHorizontal;
        private EdgeInfo _selectedVertical;
        private bool _isBusy;
        private StitchingConfig _matchConfig = new StitchingConfig();

        public OffsetPreviewControl()
        {
            InitializeComponent();
            cmbMode.SelectedIndex = 0;
            cmbHorizontal.SelectedIndexChanged += (s, e) =>
            {
                _selectedHorizontal = cmbHorizontal.SelectedItem as EdgeInfo;
                UpdateRobotInfo(_selectedHorizontal, txtHRobotDelta, txtHRobotDist);
            };
            cmbVertical.SelectedIndexChanged += (s, e) =>
            {
                _selectedVertical = cmbVertical.SelectedItem as EdgeInfo;
                UpdateRobotInfo(_selectedVertical, txtVRobotDelta, txtVRobotDist);
            };
            cmbMode.SelectedIndexChanged += (s, e) => UpdateModeUi();
            btnRun.Click += async (s, e) => await RunPreviewAsync();
            btnDebug.Click += async (s, e) => await RunDebugAsync();
            nudHTx.KeyDown += ManualKeyDown;
            nudHTy.KeyDown += ManualKeyDown;
            nudVTx.KeyDown += ManualKeyDown;
            nudVTy.KeyDown += ManualKeyDown;
            
            previewHorizontal.ManualMeasureChanged += (s, e) => ApplyManualDelta(previewHorizontal, e);
            previewVertical.ManualMeasureChanged += (s, e) => ApplyManualDelta(previewVertical, e);
            UpdateModeUi();
        }

        public void SetData(TraversalBatchResult data)
        {
            _data = data;
            BuildEdgeLists();
            BindEdgeCombos();
            UpdateModeUi();
        }

        public void SetMatchConfig(StitchingConfig cfg)
        {
            _matchConfig = cfg ?? new StitchingConfig();
            LoadConfigToUi();
        }

        private void LoadConfigToUi()
        {
            if (_matchConfig == null)
                return;

            nudHTx.Value = ClampNudValue(nudHTx, (decimal)_matchConfig.ManualOffsetHorizontalTx);
            nudHTy.Value = ClampNudValue(nudHTy, (decimal)_matchConfig.ManualOffsetHorizontalTy);
            nudVTx.Value = ClampNudValue(nudVTx, (decimal)_matchConfig.ManualOffsetVerticalTx);
            nudVTy.Value = ClampNudValue(nudVTy, (decimal)_matchConfig.ManualOffsetVerticalTy);
        }

        private void SyncManualOffsetsToConfig()
        {
            if (_matchConfig == null)
                return;

            _matchConfig.ManualOffsetHorizontalTx = (double)nudHTx.Value;
            _matchConfig.ManualOffsetHorizontalTy = (double)nudHTy.Value;
            _matchConfig.ManualOffsetVerticalTx = (double)nudVTx.Value;
            _matchConfig.ManualOffsetVerticalTy = (double)nudVTy.Value;
        }

// [Codex] [Change time: 260320] [Replace raw LinksById edge lists with traversal-aware horizontal edges and physical-column-aware vertical labels]
//        private void BuildEdgeLists()
//        {
//            _horizontalEdges = new List<EdgeInfo>();
//            _verticalEdges = new List<EdgeInfo>();
//
//            if (_data?.Components == null)
//                return;
//
//            foreach (var comp in _data.Components)
//            {
//                if (comp.Graph?.LinksById == null || comp.Points == null)
//                    continue;
//                var distanceX = comp.EstimateDistanceX;
//                var distanceY = comp.EstimateDistanceY;
//                var byId = comp.Points.ToDictionary(p => p.ImageId);
//                foreach (var kv in comp.Graph.LinksById)
//                {
//                    var fromId = kv.Key;
//                    var link = kv.Value;
//                    if (!byId.TryGetValue(fromId, out var from))
//                        continue;
//
//                    if (link.HNext.HasValue && byId.TryGetValue(link.HNext.Value, out var toH))
//                    {
//                        _horizontalEdges.Add(new EdgeInfo(comp.ComponentIndex, EdgeDir.Horizontal, from, toH, distanceX, distanceY));
//                    }
//
//                    if (link.VNext.HasValue && byId.TryGetValue(link.VNext.Value, out var toV))
//                    {
//                        _verticalEdges.Add(new EdgeInfo(comp.ComponentIndex, EdgeDir.Vertical, from, toV, distanceX, distanceY));
//                    }
//                }
//            }
//        }
        private void BuildEdgeLists()
        {
            _horizontalEdges = new List<EdgeInfo>();
            _verticalEdges = new List<EdgeInfo>();

            if (_data?.Components == null)
                return;

            foreach (var comp in _data.Components)
            {
                if (comp.Graph == null || comp.Points == null)
                    continue;

                var graph = comp.Graph;
                var distanceX = comp.EstimateDistanceX;
                var distanceY = comp.EstimateDistanceY;
                var byId = comp.Points.ToDictionary(p => p.ImageId);
                var placementById = BuildPlacementById(comp);

                foreach (var kv in graph.LinksById ?? new Dictionary<int, Link>())
                {
                    var fromId = kv.Key;
                    var link = kv.Value;
                    if (link == null)
                        continue;

                    if (link.HNext.HasValue && byId.TryGetValue(fromId, out var fromH) && byId.TryGetValue(link.HNext.Value, out var toH))
                    {
                        var horizontalDetail = DescribeHorizontalEdge(placementById, fromId, toH.ImageId);
                        _horizontalEdges.Add(new EdgeInfo(comp.ComponentIndex, EdgeDir.Horizontal, fromH, toH, distanceX, distanceY, horizontalDetail));
                    }

                    if (!link.VNext.HasValue)
                        continue;

                    if (!byId.TryGetValue(fromId, out var fromV) || !byId.TryGetValue(link.VNext.Value, out var toV))
                        continue;

                    var verticalDetail = DescribeVerticalEdge(graph, placementById, fromId, toV.ImageId);
                    _verticalEdges.Add(new EdgeInfo(comp.ComponentIndex, EdgeDir.Vertical, fromV, toV, distanceX, distanceY, verticalDetail));
                }
            }
        }

// [Codex] [Change time: 260320] [Preserve combo selection when edge lists are rebuilt after graph/layout updates]
//        private void BindEdgeCombos()
//        {
//            cmbHorizontal.DataSource = null;
//            cmbVertical.DataSource = null;
//
//            cmbHorizontal.DisplayMember = nameof(EdgeInfo.Label);
//            cmbVertical.DisplayMember = nameof(EdgeInfo.Label);
//
//            cmbHorizontal.DataSource = _horizontalEdges;
//            cmbVertical.DataSource = _verticalEdges;
//
//            _selectedHorizontal = cmbHorizontal.SelectedItem as EdgeInfo;
//            _selectedVertical = cmbVertical.SelectedItem as EdgeInfo;
//            UpdateRobotInfo(_selectedHorizontal, txtHRobotDelta, txtHRobotDist);
//            UpdateRobotInfo(_selectedVertical, txtVRobotDelta, txtVRobotDist);
//        }
        private void BindEdgeCombos()
        {
            var previousHorizontal = _selectedHorizontal;
            var previousVertical = _selectedVertical;

            cmbHorizontal.DataSource = null;
            cmbVertical.DataSource = null;

            cmbHorizontal.DisplayMember = nameof(EdgeInfo.Label);
            cmbVertical.DisplayMember = nameof(EdgeInfo.Label);

            cmbHorizontal.DataSource = _horizontalEdges;
            cmbVertical.DataSource = _verticalEdges;

            _selectedHorizontal = SelectMatchingEdge(cmbHorizontal, _horizontalEdges, previousHorizontal);
            _selectedVertical = SelectMatchingEdge(cmbVertical, _verticalEdges, previousVertical);
            UpdateRobotInfo(_selectedHorizontal, txtHRobotDelta, txtHRobotDist);
            UpdateRobotInfo(_selectedVertical, txtVRobotDelta, txtVRobotDist);
        }

        private EdgeInfo SelectMatchingEdge(ComboBox comboBox, List<EdgeInfo> edges, EdgeInfo previous)
        {
            if (comboBox == null || edges == null || edges.Count == 0)
                return null;

            var match = previous == null
                ? edges[0]
                : edges.FirstOrDefault(edge => edge.IsSamePair(previous)) ?? edges[0];
            comboBox.SelectedItem = match;
            return match;
        }

        private Dictionary<int, EdgePlacement> BuildPlacementById(TraversalComponent comp)
        {
            var result = new Dictionary<int, EdgePlacement>();
            var graph = comp?.Graph;
            if (graph?.CellById == null || graph.CellById.Count == 0)
                return result;

            foreach (var group in graph.CellById.GroupBy(kv => kv.Value.Row))
            {
                var cells = group.OrderBy(kv => kv.Value.Col).ToArray();
                for (int colIndex = 0; colIndex < cells.Length; colIndex++)
                {
                    result[cells[colIndex].Key] = new EdgePlacement(group.Key, colIndex, cells.Length);
                }
            }

            return result;
        }

        private static string DescribeHorizontalEdge(Dictionary<int, EdgePlacement> placementById, int fromId, int toId)
        {
            if (placementById != null
                && placementById.TryGetValue(fromId, out var from)
                && placementById.TryGetValue(toId, out var to)
                && from.RowIndex == to.RowIndex)
            {
                return $"Traversal row {from.RowIndex + 1}, step {from.ColumnIndex + 1}->{to.ColumnIndex + 1}";
            }

            return "Traversal link";
        }

        private static string DescribeVerticalEdge(TraversalGraph graph, Dictionary<int, EdgePlacement> placementById, int fromId, int toId)
        {
            if (placementById == null
                || !placementById.TryGetValue(fromId, out var from)
                || !placementById.TryGetValue(toId, out var to))
            {
                return graph?.Mode == OrderMode.BranchDown ? "Physical column link" : "Inter-row link";
            }

            if (graph?.Mode == OrderMode.BranchDown)
                return $"Physical column {from.ColumnIndex + 1}, row {from.RowIndex + 1}->{to.RowIndex + 1}";
            if (graph?.Mode == OrderMode.Branch)
                return $"Spine row {from.RowIndex + 1}->{to.RowIndex + 1}";
            return $"Traversal hop row {from.RowIndex + 1}->{to.RowIndex + 1}";
        }

        private void UpdateModeUi()
        {
            decimal getNumber(TextBox textBox)
            {
                if (string.IsNullOrWhiteSpace(textBox.Text.Trim())) return 0;

                if (decimal.TryParse(textBox.Text.Trim(), out decimal value)) return value;
                return 0;
            }
            var isManual = cmbMode.SelectedIndex == 1;
            nudHTx.Enabled = isManual;
            nudHTy.Enabled = isManual;
            nudVTx.Enabled = isManual;
            nudVTy.Enabled = isManual;
            btnRun.Text = isManual ? "Apply" : "Run Auto";
            previewHorizontal.ManualMode = isManual;
            previewVertical.ManualMode = isManual;

            if (isManual)
            {
                if (_matchConfig != null)
                {
                    nudHTx.Value = ClampNudValue(nudHTx, (decimal)_matchConfig.ManualOffsetHorizontalTx);
                    nudHTy.Value = ClampNudValue(nudHTy, (decimal)_matchConfig.ManualOffsetHorizontalTy);
                    nudVTx.Value = ClampNudValue(nudVTx, (decimal)_matchConfig.ManualOffsetVerticalTx);
                    nudVTy.Value = ClampNudValue(nudVTy, (decimal)_matchConfig.ManualOffsetVerticalTy);
                }
                else
                {
                    nudHTx.Value = getNumber(txtHTx);
                    nudHTy.Value = getNumber(txtHTy);
                    nudVTx.Value = getNumber(txtVTx);
                    nudVTy.Value = getNumber(txtVTy);
                }
                txtHTheta.Text = "0";
                txtVTheta.Text = "0";
                txtHMatchTime.Text = "";
                txtVMatchTime.Text = "";
            }
        }

        private async Task RunPreviewAsync()
        {
            if (_isBusy)
                return;

            if (_selectedHorizontal == null && _selectedVertical == null)
                return;

            _isBusy = true;
            btnRun.Enabled = false;
            SetStatus("Processing...");

            var mode = cmbMode.SelectedIndex == 0 ? PreviewMode.Auto : PreviewMode.Manual;
            var manualH = (double)nudHTx.Value;
            var manualHy = (double)nudHTy.Value;
            var manualV = (double)nudVTx.Value;
            var manualVy = (double)nudVTy.Value;

            if (mode == PreviewMode.Manual)
                SyncManualOffsetsToConfig();

            var horizontalEdge = _selectedHorizontal;
            var verticalEdge = _selectedVertical;

            var result = await Task.Run(() =>
            {
                var horizontal = BuildPreview(horizontalEdge, mode, manualH, manualHy);
                var vertical = BuildPreview(verticalEdge, mode, manualV, manualVy);
                return (horizontal, vertical);
            });

            if (horizontalEdge != null)
                UpdatePreviewResult(result.horizontal, previewHorizontal, txtHTx, txtHTy, txtHTheta);
            if (verticalEdge != null)
                UpdatePreviewResult(result.vertical, previewVertical, txtVTx, txtVTy, txtVTheta);

            SetStatus("Ready");
            btnRun.Enabled = true;
            _isBusy = false;
        }

        private async Task RunDebugAsync()
        {
            if (_isBusy)
                return;

            if (_selectedHorizontal == null && _selectedVertical == null)
                return;

            _isBusy = true;
            btnDebug.Enabled = false;
            SetStatus("Debug matching...");

            var horizontalEdge = _selectedHorizontal;
            var verticalEdge = _selectedVertical;
            var cfg = _matchConfig?.Clone() ?? new StitchingConfig();

            var result = await Task.Run(() =>
            {
                var horizontal = BuildDebugPreview(horizontalEdge, cfg);
                var vertical = BuildDebugPreview(verticalEdge, cfg);
                return (horizontal, vertical);
            });

            if (horizontalEdge != null && result.horizontal != null)
                previewHorizontal.SetImage(result.horizontal, preserveView: true);
            if (verticalEdge != null && result.vertical != null)
                previewVertical.SetImage(result.vertical, preserveView: true);

            SetStatus("Ready");
            btnDebug.Enabled = true;
            _isBusy = false;
        }

        private void UpdatePreviewResult(PreviewResult result, ImagePreviewControl target, TextBox txBox, TextBox tyBox, TextBox thetaBox)
        {
            if (result == null)
                return;

            target.SetImage(result.Image, preserveView: true);
            target.ManualScaleToFull = result.ScaleA > 0 ? 1.0 / result.ScaleA : 1.0;
            txBox.Text = result.Tx.ToString("0.###");
            tyBox.Text = result.Ty.ToString("0.###");
            thetaBox.Text = result.ThetaDeg.ToString("0.###");

            if (target == previewHorizontal)
            {
                txtHOverlap.Text = result.OverlapPercent.ToString("0.##");
                txtHMatchTime.Text = result.MatchSeconds > 0 ? result.MatchSeconds.ToString("0.###") : "";
            }
            else
            {
                txtVOverlap.Text = result.OverlapPercent.ToString("0.##");
                txtVMatchTime.Text = result.MatchSeconds > 0 ? result.MatchSeconds.ToString("0.###") : "";
            }
        }

        private PreviewResult BuildPreview(EdgeInfo edge, PreviewMode mode, double manualTx, double manualTy)
        {
            if (edge == null)
                return null;

            using (var imgARaw = LoadPreviewMat(edge.From.FilePath, PreviewMegapix, out var scaleA))
            using (var imgBRaw = LoadPreviewMat(edge.To.FilePath, PreviewMegapix, out var scaleB))
            using (var imgA = ApplyConvertScale(imgARaw, _matchConfig))
            using (var imgB = ApplyConvertScale(imgBRaw, _matchConfig))
            {
                if (imgA.Empty() || imgB.Empty())
                    return null;

                Mat rigidFull = null;
                double tx = 0;
                double ty = 0;
                double thetaRad = 0;
                double overlapPercent = 0;
                double matchSeconds = 0;

                if (mode == PreviewMode.Auto)
                {
                    using (var matcher = PairMatching.CreateMatcher(_matchConfig?.Clone() ?? new StitchingConfig()))
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        var dx = edge.To.XRobot - edge.From.XRobot;
                        var dy = edge.To.YRobot - edge.From.YRobot;
                        var direction = edge.Direction == EdgeDir.Horizontal ? Direction.Horizontal : Direction.Vertical;

                        var pr = matcher.MatchPair(edge.From.FilePath, edge.To.FilePath, dx, dy, edge.EstimateDistanceX, edge.EstimateDistanceY, direction);
                        sw.Stop();
                        matchSeconds = sw.Elapsed.TotalSeconds;
                        if (pr?.Eval == null || pr.MRigidBToA == null|| !pr.Eval.IsMatch)
                        {
                            var fail_tx = 0.0;
                            var fail_ty = 0.0;
                            SetStatus($"PairMatch failed: {pr?.Eval?.Reason ?? "unknown"}");
                            if (direction == Direction.Horizontal)
                            {
                                fail_tx = imgA.Width / scaleA;
                                fail_ty = 0.0;
                            }
                            else
                            {
                                fail_ty = imgA.Height / scaleA;
                                fail_tx = 0.0;
                            }
                            rigidFull = CreateTranslationMat((double)fail_tx, (double)fail_ty, 0);
                            tx = fail_tx;
                            ty = fail_ty;
                            thetaRad = 0.0;
                            overlapPercent = 0.0;
                        }
                        else
                        {
                            rigidFull = pr.MRigidBToA.Clone();
                            tx = pr.Tx;
                            ty = pr.Ty;
                            thetaRad = pr.DThetaRad;
                            overlapPercent = (pr.Eval?.OverlapRatio ?? 0) * 100.0;
                        }

                        pr?.HFullBToA?.Dispose();
                        pr?.MRigidBToA?.Dispose();
                    }
                }
                else
                {
                    rigidFull = CreateTranslationMat(manualTx, manualTy, 0);
                    tx = manualTx;
                    ty = manualTy;
                    thetaRad = 0;
                }

                using (rigidFull)
                using (var rigidPreview = ScaleRigidTransform(rigidFull, scaleA, scaleB))
                using (var composed = ComposePreview(imgA, imgB, rigidPreview))
                {
                    var bmp = BitmapConverter.ToBitmap(composed);
                    if (overlapPercent <= 0)
                        overlapPercent = EstimateOverlapPercent(imgA, imgB, rigidPreview);
                    return new PreviewResult(bmp, tx, ty, thetaRad * 180.0 / Math.PI, overlapPercent, matchSeconds, scaleA);
                }
            }
        }

        private Bitmap BuildDebugPreview(EdgeInfo edge, StitchingConfig cfg)
        {
            if (edge == null || cfg == null)
                return null;

            if (cfg.Method == Method.Manual)
            {
                using (var imgARaw = LoadPreviewMat(edge.From.FilePath, PreviewMegapix, out var scaleA))
                using (var imgBRaw = LoadPreviewMat(edge.To.FilePath, PreviewMegapix, out var scaleB))
                using (var imgA = ApplyConvertScale(imgARaw, cfg))
                using (var imgB = ApplyConvertScale(imgBRaw, cfg))
                {
                    if (imgA.Empty() || imgB.Empty())
                        return null;

                    var (tx, ty) = edge.Direction == EdgeDir.Horizontal
                        ? (cfg.ManualOffsetHorizontalTx, cfg.ManualOffsetHorizontalTy)
                        : (cfg.ManualOffsetVerticalTx, cfg.ManualOffsetVerticalTy);
                    using (var rigidFull = CreateTranslationMat(tx, ty, 0))
                    using (var rigidPreview = ScaleRigidTransform(rigidFull, scaleA, scaleB))
                    using (var composed = ComposePreview(imgA, imgB, rigidPreview))
                    {
                        return BitmapConverter.ToBitmap(composed);
                    }
                }
            }

            var directionHint = edge.Direction == EdgeDir.Horizontal ? Direction.Horizontal : Direction.Vertical;
            var dxRobot = edge.To.XRobot - edge.From.XRobot;
            var dyRobot = edge.To.YRobot - edge.From.YRobot;

            using (var matcher = PairMatching.CreateMatcher(cfg.Clone()))
            {
                var debug = matcher.MatchPairWithDebug(edge.From.FilePath, edge.To.FilePath, dxRobot, dyRobot, edge.EstimateDistanceX, edge.EstimateDistanceY, directionHint);
                using (debug?.DebugInfo)
                {
                    var info = debug?.DebugInfo;
                    if (info?.RoiA == null || info.RoiB == null || info.RoiA.Empty() || info.RoiB.Empty())
                        return null;

                    var matches = info.GetDisplayMatches();
                    return DrawMatchesComposite(info.RoiA, info.RoiB, info.KeypointsA, info.KeypointsB, matches, info.Direction == Direction.Vertical);
                }
            }
        }

        private static Mat ApplyConvertScale(Mat src, StitchingConfig cfg)
        {
            if (src == null || src.Empty())
                return src?.Clone() ?? new Mat();

            if (cfg == null)
                return src.Clone();

            var dst = new Mat();
            Cv2.ConvertScaleAbs(src, dst, cfg.ConvertAlpha, cfg.ConvertBeta);
            return dst;
        }

        private static double AdjustOverlapFraction(Direction direction, double dxRobot, double dyRobot, double estimateDistance, double baseFrac)
        {
            if (estimateDistance <= 0)
                return baseFrac;

            var actual = direction == Direction.Horizontal ? Math.Abs(dxRobot) : Math.Abs(dyRobot);
            if (actual <= 1e-9)
                return baseFrac;

            var scale = estimateDistance / actual;
            //scale = Math.Max(3.5, Math.Min(3.0, scale));

            var adjusted = baseFrac * scale;
            return Math.Max(0.05, Math.Min(0.9, adjusted));
        }

        private static Mat ExtractEdgeRoi(
            Mat img,
            string side,
            double frac,
            int minPx,
            out int ox,
            out int oy,
            out Rect roiRect)
        {
            frac = Math.Max(0.0, Math.Min(1.0, frac));
            minPx = Math.Max(1, minPx);

            var h = img.Rows;
            var w = img.Cols;

            roiRect = EdgeRoiRect(h, w, side, frac, minPx);
            ox = roiRect.X;
            oy = roiRect.Y;
            return new Mat(img, roiRect);
        }

        private static Rect EdgeRoiRect(int h, int w, string side, double frac, int minPx)
        {
            if (side == "left" || side == "right")
            {
                var rw = (int)Math.Round(frac * w);
                rw = Math.Max(minPx, Math.Min(w, rw));
                var x = (side == "right") ? 0 : (w - rw);
                return new Rect(x, 0, rw, h);
            }

            var rh = (int)Math.Round(frac * h);
            rh = Math.Max(minPx, Math.Min(h, rh));
            var y = (side == "top") ? 0 : (h - rh);
            return new Rect(0, y, w, rh);
        }

        private static Bitmap DrawMatchesComposite(Mat imgA, Mat imgB, KeyPoint[] kpA, KeyPoint[] kpB, DMatch[] matches, bool vertical)
        {
            using (var a = EnsureColor(imgA))
            using (var b = EnsureColor(imgB))
            {
                int outW;
            int outH;
            Point offsetB;
            if (vertical)
            {
                outW = Math.Max(a.Width, b.Width);
                outH = a.Height + b.Height;
                offsetB = new Point(0, a.Height);
            }
            else
            {
                outW = a.Width + b.Width;
                outH = Math.Max(a.Height, b.Height);
                offsetB = new Point(a.Width, 0);
            }

                using (var canvas = new Mat(new OpenCvSharp.Size(outW, outH), MatType.CV_8UC3, Scalar.Black))
                {
                    a.CopyTo(new Mat(canvas, new Rect(0, 0, a.Width, a.Height)));
                    b.CopyTo(new Mat(canvas, new Rect(offsetB.X, offsetB.Y, b.Width, b.Height)));

                    foreach (var m in matches)
                    {
                        var pa = kpA[m.QueryIdx].Pt;
                        var pb = kpB[m.TrainIdx].Pt;
                        var p1 = new Point((int)Math.Round(pa.X), (int)Math.Round(pa.Y));
                        var p2 = new Point((int)Math.Round(pb.X) + offsetB.X, (int)Math.Round(pb.Y) + offsetB.Y);
                        Cv2.Line(canvas, p1, p2, Scalar.LimeGreen, 1);
                        Cv2.Circle(canvas, p1, 2, Scalar.Red, -1);
                        Cv2.Circle(canvas, p2, 2, Scalar.Blue, -1);
                    }

                    return BitmapConverter.ToBitmap(canvas);
                }
            }
        }

        private static Mat EnsureColor(Mat src)
        {
            if (src.Channels() == 3)
                return src.Clone();

            var dst = new Mat();
            Cv2.CvtColor(src, dst, ColorConversionCodes.GRAY2BGR);
            return dst;
        }

        private static Mat LoadPreviewMat(string path, double maxMegapix, out double scale)
        {
            var img = ImageRead.ReadImage(path, ImreadModes.Color);
            if (img.Empty())
            {
                scale = 1;
                return img;
            }

            double megapix = img.Width * img.Height / 1_000_000.0;
            if (megapix <= maxMegapix)
            {
                scale = 1;
                return img;
            }

            scale = Math.Sqrt(maxMegapix / Math.Max(1e-9, megapix));
            var newSize = new OpenCvSharp.Size(
                Math.Max(1, (int)(img.Width * scale)),
                Math.Max(1, (int)(img.Height * scale)));

            var resized = new Mat();
            Cv2.Resize(img, resized, newSize, 0, 0, InterpolationFlags.Area);
            img.Dispose();
            return resized;
        }

        private static Mat CreateTranslationMat(double tx, double ty, double thetaRad)
        {
            var cos = Math.Cos(thetaRad);
            var sin = Math.Sin(thetaRad);
            var mat = new Mat(2, 3, MatType.CV_64F);
            mat.Set(0, 0, cos);
            mat.Set(0, 1, -sin);
            mat.Set(1, 0, sin);
            mat.Set(1, 1, cos);
            mat.Set(0, 2, tx);
            mat.Set(1, 2, ty);
            return mat;
        }

        private static Mat ScaleRigidTransform(Mat rigidFull, double scaleA, double scaleB)
        {
            var rigid3 = Mat.Eye(3, 3, MatType.CV_64F).ToMat();
            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    rigid3.Set(r, c, rigidFull.At<double>(r, c));
                }
            }

            var sA = Mat.Eye(3, 3, MatType.CV_64F).ToMat();
            sA.Set(0, 0, scaleA);
            sA.Set(1, 1, scaleA);

            var sBInv = Mat.Eye(3, 3, MatType.CV_64F).ToMat();
            sBInv.Set(0, 0, 1.0 / Math.Max(1e-9, scaleB));
            sBInv.Set(1, 1, 1.0 / Math.Max(1e-9, scaleB));

            using (var temp = sA * rigid3)
            using (var scaled = temp * sBInv)
            {
                var outMat = new Mat(2, 3, MatType.CV_64F);
                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        var t = scaled.ToMat().At<double>(r, c);
                        outMat.Set(r, c, scaled.ToMat().At<double>(r, c));
                    }
                }
                return outMat;
            }
        }

        private static Mat ComposePreview(Mat imgA, Mat imgB, Mat rigidPreview)
        {
            var wA = imgA.Width;
            var hA = imgA.Height;
            var wB = imgB.Width;
            var hB = imgB.Height;

            var cornersB = new[]
            {
                new Point2f(0, 0),
                new Point2f(wB, 0),
                new Point2f(wB, hB),
                new Point2f(0, hB)
            };

            var cornersBTrans = cornersB.Select(p => ApplyTransform(rigidPreview, p)).ToArray();
            var cornersA = new[]
            {
                new Point2f(0, 0),
                new Point2f(wA, 0),
                new Point2f(wA, hA),
                new Point2f(0, hA)
            };

            var allX = cornersBTrans.Select(p => p.X).Concat(cornersA.Select(p => p.X));
            var allY = cornersBTrans.Select(p => p.Y).Concat(cornersA.Select(p => p.Y));

            var minX = allX.Min();
            var minY = allY.Min();
            var maxX = allX.Max();
            var maxY = allY.Max();

            var shiftX = minX < 0 ? -minX : 0;
            var shiftY = minY < 0 ? -minY : 0;

            var canvasW = (int)Math.Ceiling(maxX + shiftX);
            var canvasH = (int)Math.Ceiling(maxY + shiftY);

            canvasW = Math.Max(canvasW, wA + (int)shiftX);
            canvasH = Math.Max(canvasH, hA + (int)shiftY);

            var shiftMat = rigidPreview.Clone();
            shiftMat.Set(0, 2, shiftMat.At<double>(0, 2) + shiftX);
            shiftMat.Set(1, 2, shiftMat.At<double>(1, 2) + shiftY);

            var canvasB = new Mat(new OpenCvSharp.Size(canvasW, canvasH), MatType.CV_8UC3, Scalar.Black);
            Cv2.WarpAffine(imgB, canvasB, shiftMat, new OpenCvSharp.Size(canvasW, canvasH),
                InterpolationFlags.Linear, BorderTypes.Constant, Scalar.Black);

            var canvasA = new Mat(new OpenCvSharp.Size(canvasW, canvasH), MatType.CV_8UC3, Scalar.Black);
            var roiA = new OpenCvSharp.Rect((int)Math.Round(shiftX), (int)Math.Round(shiftY), wA, hA);
            using (var target = new Mat(canvasA, roiA))
            {
                imgA.CopyTo(target);
            }

            var blended = new Mat();
            Cv2.AddWeighted(canvasA, 0.5, canvasB, 0.5, 0, blended);
            canvasA.Dispose();
            canvasB.Dispose();
            shiftMat.Dispose();

            return blended;
        }

        private static Point2f ApplyTransform(Mat mat, Point2f pt)
        {
            var x = mat.At<double>(0, 0) * pt.X + mat.At<double>(0, 1) * pt.Y + mat.At<double>(0, 2);
            var y = mat.At<double>(1, 0) * pt.X + mat.At<double>(1, 1) * pt.Y + mat.At<double>(1, 2);
            return new Point2f((float)x, (float)y);
        }

        private static double EstimateOverlapPercent(Mat imgA, Mat imgB, Mat rigidPreview)
        {
            var cornersB = new[]
            {
                new Point2f(0, 0),
                new Point2f(imgB.Width, 0),
                new Point2f(imgB.Width, imgB.Height),
                new Point2f(0, imgB.Height)
            };

            var cornersBTrans = cornersB.Select(p => ApplyTransform(rigidPreview, p)).ToArray();
            var minBx = cornersBTrans.Min(p => p.X);
            var maxBx = cornersBTrans.Max(p => p.X);
            var minBy = cornersBTrans.Min(p => p.Y);
            var maxBy = cornersBTrans.Max(p => p.Y);

            var minAx = 0f;
            var maxAx = imgA.Width;
            var minAy = 0f;
            var maxAy = imgA.Height;

            var interLeft = Math.Max(minAx, minBx);
            var interRight = Math.Min(maxAx, maxBx);
            var interTop = Math.Max(minAy, minBy);
            var interBottom = Math.Min(maxAy, maxBy);

            var interW = Math.Max(0, interRight - interLeft);
            var interH = Math.Max(0, interBottom - interTop);
            var interArea = interW * interH;
            var baseArea = imgA.Width * imgA.Height;
            if (baseArea <= 0)
                return 0;

            return interArea / baseArea * 100.0;
        }

        private void SetStatus(string message)
        {
            if (!IsHandleCreated)
                return;

            if (InvokeRequired)
                BeginInvoke(new Action(() => lblStatus.Text = message));
            else
                lblStatus.Text = message;
        }

        private void ApplyManualDelta(ImagePreviewControl source, ImagePreviewControl.ManualMeasureEventArgs args)
        {
            if (cmbMode.SelectedIndex != 1) return;

            var tx = (decimal)args.Dx;
            var ty = (decimal)args.Dy;

            if (source == previewHorizontal)
            {
                nudHTx.Value = nudHTx.Value + ClampNudValue(nudHTx, tx);
                nudHTy.Value = nudHTy.Value + ClampNudValue(nudHTy, ty);
            }
            else if (source == previewVertical)
            {
                nudVTx.Value = nudVTx.Value + ClampNudValue(nudVTx, tx);
                nudVTy.Value = nudVTy.Value + ClampNudValue(nudVTy, ty);
            }
        }

        private static decimal ClampNudValue(NumericUpDown nud, decimal value)
        {
            if (value < nud.Minimum) return nud.Minimum;
            if (value > nud.Maximum) return nud.Maximum;
            return value;
        }

// [Codex] [Change time: 260320] [Expand edge metadata so combo labels explain traversal-vs-physical comparisons after graph refactor]
//        private sealed class EdgeInfo
//        {
//            public EdgeInfo(int componentIndex, EdgeDir direction, ImageInfo from, ImageInfo to, double estimateDistX, double estimateDistY)
//            {
//                ComponentIndex = componentIndex;
//                Direction = direction;
//                From = from;
//                To = to;
//                EstimateDistanceX = estimateDistX;
//                EstimateDistanceY = estimateDistY;
//
//            }
//
//            public int ComponentIndex { get; }
//            public EdgeDir Direction { get; }
//            public ImageInfo From { get; }
//            public ImageInfo To { get; }
//            public string Label => $"C{ComponentIndex}: {From.ImageId} -> {To.ImageId}";
//            public double EstimateDistanceX { get; }
//            public double EstimateDistanceY { get; }
//        }
        private sealed class EdgeInfo
        {
            public EdgeInfo(int componentIndex, EdgeDir direction, ImageInfo from, ImageInfo to, double estimateDistX, double estimateDistY, string detail)
            {
                ComponentIndex = componentIndex;
                Direction = direction;
                From = from;
                To = to;
                EstimateDistanceX = estimateDistX;
                EstimateDistanceY = estimateDistY;
                Detail = detail;
            }

            public int ComponentIndex { get; }
            public EdgeDir Direction { get; }
            public ImageInfo From { get; }
            public ImageInfo To { get; }
            public string Detail { get; }
            public string Label => string.IsNullOrWhiteSpace(Detail)
                ? $"C{ComponentIndex}: {From.ImageId} -> {To.ImageId}"
                : $"C{ComponentIndex}: {From.ImageId} -> {To.ImageId} ({Detail})";
            public double EstimateDistanceX { get; }
            public double EstimateDistanceY { get; }

            public bool IsSamePair(EdgeInfo other)
            {
                if (other == null)
                    return false;

                return ComponentIndex == other.ComponentIndex
                    && Direction == other.Direction
                    && From?.ImageId == other.From?.ImageId
                    && To?.ImageId == other.To?.ImageId;
            }
        }

        private sealed class EdgePlacement
        {
            public EdgePlacement(int rowIndex, int columnIndex, int rowLength)
            {
                RowIndex = rowIndex;
                ColumnIndex = columnIndex;
                RowLength = rowLength;
            }

            public int RowIndex { get; }
            public int ColumnIndex { get; }
            public int RowLength { get; }
        }

        private void UpdateRobotInfo(EdgeInfo edge, TextBox deltaBox, TextBox distBox)
        {
            if (edge == null)
            {
                deltaBox.Text = "";
                distBox.Text = "";
                return;
            }
            var dir = edge.Direction;
            var dx = edge.To.XRobot - edge.From.XRobot;
            var dy = edge.To.YRobot - edge.From.YRobot;
            
            deltaBox.Text = $"dx={dx:0.###}, dy={dy:0.###}";
            distBox.Text = (dir == EdgeDir.Horizontal) ?edge.EstimateDistanceX.ToString("0.###"): edge.EstimateDistanceY.ToString("0.###");
        }

        private void ManualKeyDown(object sender, KeyEventArgs e)
        {
            if (cmbMode.SelectedIndex == 1 && e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                _ = RunPreviewAsync();
            }
        }

        private sealed class PreviewResult
        {
            public PreviewResult(Bitmap image, double tx, double ty, double thetaDeg, double overlapPercent, double matchSeconds, double scaleA)
            {
                Image = image;
                Tx = tx;
                Ty = ty;
                ThetaDeg = thetaDeg;
                OverlapPercent = overlapPercent;
                MatchSeconds = matchSeconds;
                ScaleA = scaleA;
            }

            public Bitmap Image { get; }
            public double Tx { get; }
            public double Ty { get; }
            public double ThetaDeg { get; }
            public double OverlapPercent { get; }
            public double MatchSeconds { get; }
            public double ScaleA { get; }
        }

        private enum PreviewMode
        {
            Auto,
            Manual
        }

        private void btn_Hmove_Click(object sender, EventArgs e)
        {
            
            if (decimal.TryParse(txtHTx.Text.Trim(), out var txH))
                nudHTx.Value = ClampNudValue(nudHTx, txH);
            else nudHTx.Value = 0;

            if (decimal.TryParse(txtHTy.Text.Trim(), out var tyH))
                nudHTy.Value = ClampNudValue(nudHTy, tyH);
            else nudHTy.Value = 0;
            if (decimal.TryParse(txtVTx.Text.Trim(), out var txV))
                nudVTx.Value = ClampNudValue(nudVTx, txV);
            else nudVTx.Value = 0;

            if (decimal.TryParse(txtVTy.Text.Trim(), out var tyV))
                nudVTy.Value = ClampNudValue(nudVTy, tyV);
            else nudVTy.Value = 0;
        }
    }
}
