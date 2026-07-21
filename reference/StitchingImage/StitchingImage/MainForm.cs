using StitchingImage.Connection;
using StitchingImage.Stitch_Tools.Matcher;
using StitchingImage.Stitch_Tools.RobotManager;
using StitchingImage.Stitch_Tools.Utils;
using StitchingImageRunner = StitchingImage.Stitch_Tools.RobotManager.StitchingImage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StitchingImage
{
    public partial class MainForm : Form
    {
        private readonly AppSettings _settings;
        private readonly ImageStore _store = new ImageStore();
        private ArrangeBatchResult _currentArrange;
        private TraversalBatchResult _currentTraversal;
        private int? _currentGroupId;
        private int? _requestedGroupId;
        // [Codex] [Change time: 260324] [Allow Run Selected to execute multiple selected groups from UI list.]
        private HashSet<int> _requestedGroupIds;
        private string _lastFolder;
        private string _outputFolder;
        private string _filenamePattern;
        private readonly StitchingConfig _matchConfig;
        private readonly SystemConfig _systemConfig;
        private readonly TcpConnectionManager _connectionManager = new TcpConnectionManager();
        private bool _applyingSystemConfig;
        private FileSystemWatcher _systemConfigWatcher;
        private bool _isStitching;
        private bool _pendingSystemConfigReload;
        private bool _reloadScheduled;
        private bool _reloadInProgress;
        private DateTime _lastSystemConfigWriteUtc;
        private CancellationTokenSource _stitchCts;
        private readonly Color _tcpButtonDefaultColor;
        private int _lastLoadFileCount;
        private int _lastLoadParsedCount;
        private string _lastLoadSampleImagePath;
        private RobotArrange robotArrange;

        public MainForm(AppSettings settings)
        {
            _settings = settings ?? new AppSettings();
            _matchConfig = _settings.MatchConfig?.Clone() ?? new StitchingConfig();
            _systemConfig = _settings.SystemConfig?.Clone() ?? new SystemConfig();
            InitializeComponent();
            _btnChangeFormat.Click += (s, e) => OpenFormatDialog();
            _btnReloadByPattern.Click += (s, e) => ReloadCurrentFolderIfAny();
            _filenamePattern = _settings.FilenamePattern ?? string.Empty;
            _txtFilenamePattern.Text = _filenamePattern;
            _txtFilenamePattern.ReadOnly = true;
            // [Codex] [Change time: 260323] [Restore persisted manual NodeInterval into the UI control]
            num_node.Value = Math.Max(num_node.Minimum, Math.Min(num_node.Maximum, (decimal)_settings.NodeInterval));
            // [Codex] [Change time: 260319] [Stop treating layout zigzag toggle as stitch traversal mode]
//            _chkZigzagByIndex.CheckedChanged += (s, e) => { RecomputeCurrent(); UpdateOrderMode(); };
            _chkLayoutZigzag.CheckedChanged += (s, e) => UpdateArrangementHelpText();
            // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
            // _cmbMatchMethod.Items.AddRange(new object[]
            // {
            //     "Coarse+Fine",
            //     "ORB",
            //     "ORB+PhaseCorr",
            //     "SIFT",
            //     "BRISK",
            //     "Manual",
            //     "PhaseCorr"
            // });
            _cmbMatchMethod.Items.AddRange(new object[]
            {
                "Coarse+Fine",
                "ORB",
                "ORB+PhaseCorr",
                "SIFT",
                "BRISK",
                "Manual",
                "PhaseCorr",
                // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                "EccMatcher",
                "EccMatcher2",
                "PyramidPhaseMatcher"
            });
            // [Codex] [Change time: 260319] [Default traversal mode combo independently from layout arrangement checkbox]
//            _cmbMode.SelectedIndex = 1;
            _cmbTraversalMode.SelectedIndex = 0;
            _cmbScanDirection.SelectedIndex = 0;
            // [Codex] [Change time: 260318] [Default the fallback selector to the metadata-first position strategy]
//            _cmbClusterOrder.SelectedIndex = 1;
            _cmbClusterOrder.SelectedIndex = (int)ClusterOrderMode.Position;
            ApplySystemConfigToUi();
            _cmbMatchMethod.SelectedIndex = MatchMethodIndex(_matchConfig.Method);
            // [Codex] [Change time: 260319] [Refresh traversal UI copy from the dedicated traversal combo]
//            _cmbMode.SelectedIndexChanged += (s, e) =>
            _cmbTraversalMode.SelectedIndexChanged += (s, e) =>
            {
                RecomputeCurrent();
                UpdateOrderMode();
                UpdateArrangementHelpText();
            };
            _cmbScanDirection.SelectedIndexChanged += (s, e) =>
            {
                RecomputeCurrent();
                UpdateScanDirection();
            };
            _cmbClusterOrder.SelectedIndexChanged += (s, e) =>
            {
                RecomputeCurrent();
                UpdateClusterOrder();
                UpdateArrangementHelpText();
            };
            _cmbMatchMethod.SelectedIndexChanged += (s, e) => UpdateMatchMethod();
            // Tony 20260202 Send log output to LOG_<level>_<YYYYMMDD>.txt.
            Logger.Initialize(Log_ListBox, Environment.CurrentDirectory);
            Logger.Info("Application initialized.");
            EnsurePreviewControls();
            _offsetPreview.SetMatchConfig(_matchConfig);
            UpdateMatchMethod();
            nudAlpha.ValueChanged += (s, e) => ApplyConfigChanges();
            nudBeta.ValueChanged += (s, e) => ApplyConfigChanges();
            _nudPreviewMp.ValueChanged += (s, e) => UpdateSystemConfigFromUi();
            num_node.ValueChanged += (s, e) =>
            {
                // [Codex] [Change time: 260323] [Persist NodeInterval whenever the manual interval control changes]
                _settings.NodeInterval = (int)num_node.Value;
                _settings.Save();
                RecomputeCurrent();
            };
            nudAlpha.Value = (decimal)_matchConfig.ConvertAlpha;
            nudBeta.Value = (decimal)_matchConfig.ConvertBeta;

            _configGrid.Initialize(_matchConfig, SaveConfigChanges);
            _systemConfigGrid.Initialize(_systemConfig, SaveSystemConfigChanges);
            _connectionManager.StateChanged += (c,s) => OnConnectionStateChanged(c, s);
            // Tony 20260202 Wire protocol events to ISP-only protocol view.
            _connectionManager.ProtocolSent += message => AppendProtocolMessage(_txtIspSent, message);
            _connectionManager.ProtocolReceived += message => AppendProtocolMessage(_txtIspReceived, message);
            _connectionManager.ProtocolData += message => AppendProtocolMessage(_txtIspData, message);
            _connectionManager.ProtocolStatus += message => AppendProtocolMessage(_txtIspStatus, message);
            _connectionManager.StitchRequested += OnStitchRequested;
            // Tony 20260202 Provide stitching state for TCP BUSY responses.
            _connectionManager.IsStitching = () => _isStitching;
            _connectionManager.StitchStopRequested += reason => StopStitching(reason);
            cnt_btn.Click += (s, e) => ToggleConnection();
            tcp_txt.Text = "Disconnected";
            _tcpButtonDefaultColor = cnt_btn.BackColor;
#if DEBUG
            // Tony 20260202 Populate protocol response samples for AH01 in DEBUG.
            InitializeProtocolSamples();
            _btnSendToAh01.Click += async (s, e) => await SendProtocolSampleAsync();
#else
            // Tony 20260203 Hide protocol sample controls in Release builds.
            _cmbProtocolSamples.Visible = false;
            _btnSendToAh01.Visible = false;
#endif
            StartSystemConfigWatcher();
            UpdateArrangementHelpText();
            robotArrange = new RobotArrange();
        }

        private void EnsurePreviewControls()
        {
            if (_offsetPreview == null)
            {
                _offsetPreview = new Stitch_Tools.DesignControls.OffsetPreviewControl
                {
                    Dock = DockStyle.Fill,
                    Name = "_offsetPreview"
                };

                if (tabPage2 != null)
                {
                    tabPage2.Controls.Clear();
                    tabPage2.Controls.Add(_offsetPreview);
                }
            }

            if (_canvas == null)
            {
                _canvas = new Stitch_Tools.DesignControls.PathCanvasControl
                {
                    Dock = DockStyle.Fill,
                    Name = "_canvas"
                };

                if (splitOrderPrev != null)
                {
                    splitOrderPrev.Panel1.Controls.Clear();
                    splitOrderPrev.Panel1.Controls.Add(_canvas);
                }
            }
        }

        private OrderMode CurrentMode
        {
            get
            {
                // [Codex] [Change time: 260319] [Map traversal combo directly to stitch traversal OrderMode]
//                switch (_cmbMode.SelectedIndex)
                switch (_cmbTraversalMode.SelectedIndex)
                {
                    case 0:
                        return OrderMode.Zigzag;
                    case 1:
                        return OrderMode.Branch;
                    case 2:
                        return OrderMode.BranchDown;
                    default:
                        return OrderMode.Branch;
                }
            }
        }

        private void UpdateOrderMode()
        {
            // [Codex] [Change time: 260319] [Persist the traversal combo selection directly without mixing in layout checkbox]
//            switch (_cmbMode.SelectedIndex)
            switch (_cmbTraversalMode.SelectedIndex)
            {
                case 0:
                    _systemConfig.OrderMode = OrderMode.Zigzag; break;
                case 1:
                    _systemConfig.OrderMode = OrderMode.Branch; break;
                case 2:
                    _systemConfig.OrderMode = OrderMode.BranchDown; break;
                default:
                    _systemConfig.OrderMode = OrderMode.Branch; break;
            }
            _systemConfigGrid.RefreshValues();
            SaveSystemConfigChanges();
        }
        // [Codex] [Change time: 260318] [Map UI selection directly to metadata/manual ordering modes]
//        private ClusterOrderMode CurrentClusterOrder => _cmbClusterOrder.SelectedIndex == 1
//            ? ClusterOrderMode.Position
//            : ClusterOrderMode.Coordinates;
        private ClusterOrderMode CurrentClusterOrder
        {
            get
            {
                switch (_cmbClusterOrder.SelectedIndex)
                {
                    case 1:
                        return ClusterOrderMode.Position;
                    case 2:
                        return ClusterOrderMode.ManualRow;
                    case 3:
                        return ClusterOrderMode.ManualColumn;
                    case 0:
                    default:
                        return ClusterOrderMode.Coordinates;
                }
            }
        }
        // [Codex] [Change time: 260318] [Persist the expanded metadata/manual ordering selection]
//        private void UpdateClusterOrder()
//        {
//            switch (_cmbClusterOrder.SelectedIndex)
//            {
//                case 0:
//                    _systemConfig.ClusterOrderMode = ClusterOrderMode.Coordinates; break;
//                case 1:
//                default:
//                    _systemConfig.ClusterOrderMode = ClusterOrderMode.Position; break;
//            }
//            _systemConfigGrid.RefreshValues();
//            SaveSystemConfigChanges();
//        }
        private void UpdateClusterOrder()
        {
            _systemConfig.ClusterOrderMode = CurrentClusterOrder;
            _systemConfigGrid.RefreshValues();
            SaveSystemConfigChanges();
        }
        private void UpdateScanDirection()
        {
            switch (_cmbScanDirection.SelectedIndex)
            {
                case 0: _systemConfig.StartCorner = StartCorner.TopLeft; _systemConfig.RobotMovement = RobotMovement.Right; break;
                case 1: _systemConfig.StartCorner = StartCorner.TopLeft; _systemConfig.RobotMovement = RobotMovement.Down; break;
                case 2: _systemConfig.StartCorner = StartCorner.TopRight; _systemConfig.RobotMovement = RobotMovement.Left; break;
                case 3: _systemConfig.StartCorner = StartCorner.TopRight; _systemConfig.RobotMovement = RobotMovement.Down; break;
                case 4: _systemConfig.StartCorner = StartCorner.BottomLeft; _systemConfig.RobotMovement = RobotMovement.Right; break;
                case 5: _systemConfig.StartCorner = StartCorner.BottomLeft; _systemConfig.RobotMovement = RobotMovement.Up; break;
                case 6: _systemConfig.StartCorner = StartCorner.BottomRight; _systemConfig.RobotMovement = RobotMovement.Left; break;
                case 7: _systemConfig.StartCorner = StartCorner.BottomRight; _systemConfig.RobotMovement = RobotMovement.Up; break;
                default: _systemConfig.StartCorner = StartCorner.TopLeft; _systemConfig.RobotMovement = RobotMovement.Right; break;
            }
            Logger.Info($"Scan Direction set to {_systemConfig.StartCorner} -> {_systemConfig.RobotMovement}.");
            _systemConfigGrid.RefreshValues();
            SaveSystemConfigChanges();
        }

        private void UpdateMatchMethod()
        {
            switch (_cmbMatchMethod.SelectedIndex)
            {
                case 0:
                    _matchConfig.Method = Method.CoarseFine;
                    break;
                case 1:
                    _matchConfig.Method = Method.Orb;
                    break;
                case 2:
                    _matchConfig.Method = Method.OrbPharse;
                    break;
                case 3:
                    _matchConfig.Method = Method.Sift;
                    break;
                case 4:
                    _matchConfig.Method = Method.Brisk;
                    break;
                case 5:
                    _matchConfig.Method = Method.Manual;
                    break;
                // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                // case 6:
                //     _matchConfig.Method = Method.PhaseCorr;
                //     break;
                // default:
                //     _matchConfig.Method = Method.CoarseFine;
                //     break;
                case 6:
                    _matchConfig.Method = Method.PhaseCorr;
                    break;
                // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                // case 7:
                //     _matchConfig.Method = Method.EccMatcher2;
                //     break;
                // case 8:
                //     _matchConfig.Method = Method.PyramidPhaseMatcher;
                //     break;
                case 7:
                    _matchConfig.Method = Method.EccMatcher;
                    break;
                case 8:
                    _matchConfig.Method = Method.EccMatcher2;
                    break;
                case 9:
                    _matchConfig.Method = Method.PyramidPhaseMatcher;
                    break;
                default:
                    _matchConfig.Method = Method.CoarseFine;
                    break;
            }
            Logger.Info($"Match method set to {_matchConfig.Method}.");
            _configGrid.RefreshValues();
            SaveConfigChanges();
        }

        private void OpenFormatDialog()
        {
            var files = Array.Empty<string>();
            if (!string.IsNullOrWhiteSpace(_lastFolder) && Directory.Exists(_lastFolder))
            {
                var exts = new[] { ".bmp", ".png", ".jpg", ".jpeg", ".tif", ".tiff" };
                files = Directory.EnumerateFiles(_lastFolder)
                    .Where(f => exts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .Select(Path.GetFileName)
                    .OrderBy(x => x)
                    .ToArray();
            }

            using (var dlg = new FormatDialog(_txtFilenamePattern.Text, files))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                _txtFilenamePattern.Text = dlg.Pattern;
                ApplyFilenamePatternFromUi();
                ReloadCurrentFolderIfAny();
            }
        }

        private static int MatchMethodIndex(Method method)
        {
            switch (method)
            {
                case Method.CoarseFine:
                    return 0;
                case Method.Orb:
                    return 1;
                case Method.OrbPharse:
                    return 2;
                case Method.Sift:
                    return 3;
                case Method.Brisk:
                    return 4;
                case Method.Manual:
                    return 5;
                // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                // case Method.PhaseCorr:
                // default:
                //     return 6;
                case Method.PhaseCorr:
                    return 6;
                // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                // case Method.EccMatcher2:
                //     return 7;
                // case Method.PyramidPhaseMatcher:
                //     return 8;
                case Method.EccMatcher:
                    return 7;
                case Method.EccMatcher2:
                    return 8;
                case Method.PyramidPhaseMatcher:
                    return 9;
                default:
                    return 0;
            }
        }
        private void ReloadCurrentFolderIfAny()
        {
            if (!string.IsNullOrWhiteSpace(_lastFolder) && Directory.Exists(_lastFolder))
                LoadFolder(_lastFolder);
        }

        private void LoadFolder(string folder)
        {
            _lastFolder = folder;
            // [Codex] [Change time: 260324] [Reflect active folder path in the read-only folder textbox for both manual and auto load flows]
            lbl_dir.Text = folder ?? string.Empty;
            _store.Clear();
            _currentArrange = null;
            _currentTraversal = null;
            _currentGroupId = null;

            var invertX = _chkInvertX.Checked;

            var exts = new[] { ".bmp", ".png", ".jpg", ".jpeg", ".tif", ".tiff" };
            var files = Directory.EnumerateFiles(folder)
                .Where(f => exts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToArray();

            int parsed = 0;
            // [Codex] [Change time: 260318] [Track metadata availability separately so manual fallback only appears when both are missing]
//            var hasMissingPositionOrCoordinates = false;
            var hasCompletePositionMetadata = true;
            var hasCompleteCoordinateMetadata = true;
            _lastLoadSampleImagePath = null;
            ApplyFilenamePatternFromUi();
            foreach (var f in files)
            {
                ImageInfo info;
                if (!FilenameParser.TryParse(f, invertX, out info)) continue;
                _store.Add(info);
                parsed++;
                // [Codex] [Change time: 260318] [Separate PositionId and X/Y metadata completeness checks]
//                if (!info.PositionId.HasValue || double.IsNaN(info.XRobot) || double.IsNaN(info.YRobot))
//                    hasMissingPositionOrCoordinates = true;
                if (!info.PositionId.HasValue)
                    hasCompletePositionMetadata = false;
                if (double.IsNaN(info.XRobot) || double.IsNaN(info.YRobot))
                    hasCompleteCoordinateMetadata = false;
                if (_lastLoadSampleImagePath == null)
                    _lastLoadSampleImagePath = f;
            }

            // [Codex] [Change time: 260318] [Only enable manual row/column controls when both metadata sources are unavailable]
//            num_node.Enabled = hasMissingPositionOrCoordinates;
//            _chkInvertX.Enabled = !hasMissingPositionOrCoordinates;
//            _cmbClusterOrder.Enabled = !hasMissingPositionOrCoordinates;
//            _nudGap.Enabled = !hasMissingPositionOrCoordinates;
//            _nudRow.Enabled = !hasMissingPositionOrCoordinates;
            var requiresManualFallback = !hasCompletePositionMetadata && !hasCompleteCoordinateMetadata;
            num_node.Enabled = requiresManualFallback;
            _cmbClusterOrder.Enabled = true;
            _chkInvertX.Enabled = hasCompleteCoordinateMetadata;
            _nudGap.Enabled = hasCompleteCoordinateMetadata;
            _nudRow.Enabled = hasCompleteCoordinateMetadata;

            // [Codex] [Change time: 260319] [Keep cluster-order interaction responsive while constraining only invalid manual metadata cases]
//            if (hasCompletePositionMetadata)
//                _cmbClusterOrder.SelectedIndex = (int)ClusterOrderMode.Position;
//            else if (hasCompleteCoordinateMetadata)
//                _cmbClusterOrder.SelectedIndex = (int)ClusterOrderMode.Coordinates;
//            else if (CurrentClusterOrder != ClusterOrderMode.ManualRow && CurrentClusterOrder != ClusterOrderMode.ManualColumn)
//                _cmbClusterOrder.SelectedIndex = (int)ClusterOrderMode.ManualRow;
            if (!requiresManualFallback
                && (CurrentClusterOrder == ClusterOrderMode.ManualRow || CurrentClusterOrder == ClusterOrderMode.ManualColumn))
            {
                _cmbClusterOrder.SelectedIndex = hasCompletePositionMetadata
                    ? (int)ClusterOrderMode.Position
                    : (int)ClusterOrderMode.Coordinates;
            }
            else if (requiresManualFallback
                && CurrentClusterOrder != ClusterOrderMode.ManualRow
                && CurrentClusterOrder != ClusterOrderMode.ManualColumn)
            {
                _cmbClusterOrder.SelectedIndex = (int)ClusterOrderMode.ManualRow;
            }

            _lastLoadFileCount = files.Length;
            _lastLoadParsedCount = parsed;
            _lstGroups.Items.Clear();
            foreach (var gid in _store.GroupIds)
                _lstGroups.Items.Add(gid);

            //_lblStatus.Text = $"Folder: {folder} | Files: {files.Length} | Parsed: {parsed} | Groups: {_store.GroupIds.Count}";
            UpdateArrangementHelpText();
            // persist immediately
            _settings.LastFolderPath = _lastFolder ?? string.Empty;
            _settings.Save();
            if (_lstGroups.Items.Count > 0)
                _lstGroups.SelectedIndex = 0;
            else
            {
                _canvas.SetData(null);
                _offsetPreview.SetData(null);
            }
        }


        private void ApplyFilenamePatternFromUi()
        {
            var raw = _txtFilenamePattern.Text?.Trim() ?? string.Empty;
            try
            {
                FilenameParser.ConfigurePattern(raw);
                _filenamePattern = raw;
                _settings.FilenamePattern = _filenamePattern;
                _settings.Save();
                if (_lblFilenamePatternStatus != null)
                {
                    _lblFilenamePatternStatus.Text = string.IsNullOrWhiteSpace(raw)
                        ? "Using default parser format"
                        : "Pattern active";
                }
            }
            catch (Exception ex)
            {
                if (_lblFilenamePatternStatus != null)
                    _lblFilenamePatternStatus.Text = $"Pattern invalid: {ex.Message}";
            }
        }

        private void RecomputeCurrent()
        {
            if (_applyingSystemConfig)
                return;
            if (_currentGroupId == null) return;

            ImageInfo[] images;
            if (!_store.TryGetGroup(_currentGroupId.Value, out images)) return;

            var opt = new OrderOptions
            {
                GapFactor = (double)_nudGap.Value,
                RowFactor = (double)_nudRow.Value,
                InvertXOnParse = _chkInvertX.Checked,
                Mode = CurrentMode,
// [Antigravity] [Change time: 260318] [Merged StartCorner and RobotMovement]
//                StartCorner = CurrentCorner,
//                RobotMovement = CurrentRobotDir,
                StartCorner = _systemConfig.StartCorner,
                RobotMovement = _systemConfig.RobotMovement,
                ClusterOrder = CurrentClusterOrder,
                NodeInterval = (int)num_node.Value
            };

            _currentArrange = robotArrange.Arrange(images, opt);
            _currentTraversal = TraversalGraph.BuildBatch(_currentGroupId.Value, _currentArrange, opt);
#if DEBUG
            foreach (var comp in _currentTraversal?.Components ?? Array.Empty<TraversalComponent>())
                comp.Graph?.DebugPrintEdges();
            DebugVisualizeArrange.PrintArrangeResult(_currentArrange);
#endif
            var orderCount = _currentTraversal?.Components?.Length ?? 0;
            //_lblStatus.Text = $"Group {_currentGroupId.Value}: imgs={images.Length} | orders={orderCount}";

            if (_currentTraversal == null || _currentTraversal.Components == null || _currentTraversal.Components.Length == 0)
            {
                _canvas.SetData(null);
                _offsetPreview.SetData(null);
                return;
            }

            _canvas.SetHideAxesWhenNoCoordinates(_currentTraversal.Components
                .SelectMany(c => c.Points ?? Array.Empty<ImageInfo>())
                .Any(p => double.IsNaN(p.XRobot) || double.IsNaN(p.YRobot)));
            _canvas.SetData(_currentArrange, _currentTraversal);
            _offsetPreview.SetData(_currentTraversal);
        }

        private void SelectFolderAndLoad(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description = "Select the folder containing the CameraImage...",
                ShowNewFolderButton = false,
                SelectedPath = !string.IsNullOrWhiteSpace(_lastFolder) ? _lastFolder : ""
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                lbl_dir.Text = dlg.SelectedPath;
                LoadFolder(dlg.SelectedPath);
                Logger.Info($"New Directory: {dlg.SelectedPath}");
            }
        }

        private void OnGroupSelected(object sender, EventArgs e)
        {
            if (!(_lstGroups.SelectedItem is int gid)) return;
            _currentGroupId = gid;
            RecomputeCurrent();
        }

        private void _nudGap_ValueChanged(object sender, EventArgs e)
        {
            RecomputeCurrent();
            UpdateSystemConfigFromUi();
        }

        private void _nudRow_ValueChanged(object sender, EventArgs e)
        {
            RecomputeCurrent();
            UpdateSystemConfigFromUi();
        }

        private void _chkInvertX_CheckedChanged(object sender, EventArgs e)
        {
            if (_applyingSystemConfig)
                return;
            RecomputeCurrent();
            ReloadCurrentFolderIfAny();
        }

        private void _chkStartRTL_CheckedChanged(object sender, EventArgs e)
        {
            ReloadCurrentFolderIfAny();
        }

        private void ApplyConfigChanges()
        {
            if (_matchConfig == null)
                return;

            _matchConfig.ConvertAlpha = (double)nudAlpha.Value;
            _matchConfig.ConvertBeta = (double)nudBeta.Value;
            _configGrid.RefreshValues();
            SaveConfigChanges();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
//#if DEBUG
//            // [Codex] [Change time: 260320] [Run ImageGrid graph verification on startup in DEBUG builds]
//            var verifyFailures = RobotOrderer.VerifyImageGridGraphParity();
//            if (verifyFailures.Count > 0)
//            {
//                foreach (var f in verifyFailures)
//                    Logger.Warning($"[GraphVerify] {f}");
//                MessageBox.Show(
//                    $"ImageGrid graph verification failed with {verifyFailures.Count} error(s).\nSee log for details.",
//                    "Graph Verification", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//            }
//            else
//            {
//                Logger.Info($"[GraphVerify] All {verifyFailures.Count} cases passed.");
//            }
//#endif
            // [Codex] [Change time: 260324] [Show last persisted folder path even before auto-load validation]
            lbl_dir.Text = _settings.LastFolderPath ?? string.Empty;
            if (_systemConfig.AutoLoadLastFolder
                && !string.IsNullOrWhiteSpace(_settings.LastFolderPath)
                && Directory.Exists(_settings.LastFolderPath))
                LoadFolder(_settings.LastFolderPath);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _settings.LastFolderPath = _lastFolder ?? string.Empty;
            _settings.MatchConfig = _matchConfig.Clone();
            _settings.SystemConfig = _systemConfig.Clone();
            _settings.Save();
            _connectionManager.Dispose();
            _systemConfigWatcher?.Dispose();
            Logger.Info("Application closed.");
        }

        private void SaveConfigChanges()
        {
            _settings.MatchConfig = _matchConfig.Clone();
            _settings.Save();
        }

        private void SaveSystemConfigChanges()
        {
            _lastSystemConfigWriteUtc = DateTime.UtcNow;
            _settings.SystemConfig = _systemConfig.Clone();
            _settings.Save();
            ApplySystemConfigToUi();
        }

        private void ToggleConnection()
        {
            if (_connectionManager.State == ConnectionStateTCP.Connected || _connectionManager.State == ConnectionStateTCP.Connecting)
            {
                AppendProtocolMessage(_txtIspSent, $"[{DateTime.Now:HH:mm:ss}] Disconnect request");
                _connectionManager.Stop();
                return;
            }

            var attempts = Math.Max(1, _systemConfig.ConnectAttempts);
            var delaySeconds = Math.Max(0.1, _systemConfig.ConnectDelaySeconds);
            var delay = TimeSpan.FromSeconds(delaySeconds);
            var host = string.IsNullOrWhiteSpace(_systemConfig.RobotHost) ? "127.0.0.1" : _systemConfig.RobotHost;
            var port = _systemConfig.RobotPort;
            AppendProtocolMessage(_txtIspSent, $"[{DateTime.Now:HH:mm:ss}] Connect request to {host}:{port}");
            _connectionManager.Start(host, port, attempts, delay);
        }

        private void OnConnectionStateChanged(ConnectionStateTCP state, string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnConnectionStateChanged(state, message)));
                return;
            }

            tcp_txt.Text = state == ConnectionStateTCP.Disconnected ? string.Empty : message;
            AppendProtocolMessage(_txtIspReceived, $"[{DateTime.Now:HH:mm:ss}] {message}");
            if (state == ConnectionStateTCP.Connecting || state == ConnectionStateTCP.Connected)
            {
                StatusButton(cnt_btn, true);
            }
            else StatusButton(cnt_btn, false);
            if (_isStitching && !string.IsNullOrWhiteSpace(message)
                && message.IndexOf("STOP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                StopStitching("TCP/IP stop request received.");
            }
        }

        private void AppendProtocolMessage(System.Windows.Forms.TextBox target, string message)
        {
            if (target == null)
                return;

            if (target.InvokeRequired)
            {
                target.BeginInvoke(new Action(() => AppendProtocolMessage(target, message)));
                return;
            }

            target.AppendText(message + Environment.NewLine);
        }

        private void StatusButton(System.Windows.Forms.Button btn, bool status)
        {
            if (btn == null) return;
            if (btn.InvokeRequired)
            {
                btn.BeginInvoke(new Action(() =>  StatusButton(btn, status)));
                return;
            }
            if (status)
            {
                btn.BackColor = Color.Green;
            }
            else
            {
                // Tony 20260202 Reset to default color when TCP/IP host is stopped.
                btn.BackColor = _tcpButtonDefaultColor;
            }
        }

        private void NotifyProtocolStatus(string message)
        {
            AppendProtocolMessage(_txtIspStatus, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        private void StopStitching(string reason)
        {
            if (!_isStitching)
                return;

            Logger.Warning($"Stitching cancel requested: {reason}");
            NotifyProtocolStatus($"Stop stitching: {reason}");
            _stitchCts?.Cancel();
        }

        private void SaveModeChanged(object sender, EventArgs e)
        {
            var previewEnabled = CurrentSaveMode == SaveMode.Preview;
            _nudPreviewMp.Enabled = previewEnabled;
            _lblPreviewMp.Enabled = previewEnabled;
            UpdateSystemConfigFromUi();
        }

        private void SelectOutputFolder(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog
            {
                Description = "Select output folder for stitched images...",
                ShowNewFolderButton = true,
                SelectedPath = !string.IsNullOrWhiteSpace(_outputFolder) ? _outputFolder : _lastFolder
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _outputFolder = dlg.SelectedPath;
                _txtOutput.Text = _outputFolder;
            }
        }

        private SaveMode CurrentSaveMode => _cmbSaveMode.SelectedIndex == 1 ? SaveMode.Full : SaveMode.Preview;
        private void ApplySystemConfigToUi()
        {
            _applyingSystemConfig = true;

            _cmbSaveMode.SelectedIndex = _systemConfig.SaveMode == SaveMode.Full ? 1 : 0;

            _chkInvertX.Checked = _systemConfig.InvertX;
            // [Codex] [Change time: 260318] [Honor persisted manual row/column ordering selections]
//            _cmbClusterOrder.SelectedIndex = _systemConfig.ClusterOrderMode == ClusterOrderMode.Position ? 1 : 0;
            _cmbClusterOrder.SelectedIndex = (int)_systemConfig.ClusterOrderMode;
            // [Codex] [Change time: 260319] [Restore traversal combo from persisted OrderMode independently of layout checkbox]
            _cmbTraversalMode.SelectedIndex = GetTraversalModeIndex(_systemConfig.OrderMode);
            _chkLayoutZigzag.Checked = true;

            _cmbScanDirection.SelectedIndex = GetScanDirectionIndex(_systemConfig.StartCorner, _systemConfig.RobotMovement);

            var previewMp = (decimal)Math.Max((double)_nudPreviewMp.Minimum,
                Math.Min((double)_nudPreviewMp.Maximum, _systemConfig.ComposeMegapix));
            _nudPreviewMp.Value = previewMp;

            SaveModeChanged(this, EventArgs.Empty);
            _applyingSystemConfig = false;
            UpdateArrangementHelpText();
            RecomputeCurrent();
        }

        // [Codex] [Change time: 260319] [Add traversal combo mapper for the clarified UI]
        private static int GetTraversalModeIndex(OrderMode mode)
        {
            if (mode == OrderMode.Zigzag) return 0;
            if (mode == OrderMode.BranchDown) return 2;
            return 1;
        }

        // [Codex] [Change time: 260319] [Surface the distinction between arrangement and stitch traversal in one short status line]
        private void UpdateArrangementHelpText()
        {
            if (_lblArrangementHelp == null)
                return;

            var clusterLabel = _cmbClusterOrder.SelectedItem as string ?? CurrentClusterOrder.ToString();
            var layoutLabel = _chkLayoutZigzag.Checked
                ? "Layout: rows/columns are arranged as zigzag lanes when the selected ordering source allows it."
                : "Layout: lanes may still come from metadata/manual ordering even when the zigzag layout hint is unchecked.";

            var manualNote = (CurrentClusterOrder == ClusterOrderMode.ManualRow || CurrentClusterOrder == ClusterOrderMode.ManualColumn)
                ? " Manual layout needs both PositionId and X/Y metadata to be unavailable."
                : string.Empty;

            _lblArrangementHelp.Text = $"{layoutLabel} Traversal: {_cmbTraversalMode.Text} controls stitch graph flow only. Ordering source: {clusterLabel}.{manualNote}";
        }

        // [Codex] [Change time: 260318] [Add missing scan direction index mapper]
        private static int GetScanDirectionIndex(StartCorner corner, RobotMovement movement)
        {
            if (corner == StartCorner.TopLeft && movement == RobotMovement.Right) return 0;
            if (corner == StartCorner.TopLeft && movement == RobotMovement.Down) return 1;
            if (corner == StartCorner.TopRight && movement == RobotMovement.Left) return 2;
            if (corner == StartCorner.TopRight && movement == RobotMovement.Down) return 3;
            if (corner == StartCorner.BottomLeft && movement == RobotMovement.Right) return 4;
            if (corner == StartCorner.BottomLeft && movement == RobotMovement.Up) return 5;
            if (corner == StartCorner.BottomRight && movement == RobotMovement.Left) return 6;
            if (corner == StartCorner.BottomRight && movement == RobotMovement.Up) return 7;

            return 0;
        }


        private void UpdateSystemConfigFromUi()
        {
            if (_applyingSystemConfig)
                return;

            _systemConfig.SaveMode = CurrentSaveMode;
            _systemConfig.ComposeMegapix = (double)_nudPreviewMp.Value;
            _systemConfig.GapFactor = (double)_nudGap.Value;
            _systemConfig.GapRow = (double)_nudRow.Value;
            _systemConfigGrid.RefreshValues();
            SaveSystemConfigChanges();
        }

        private void StartSystemConfigWatcher()
        {
            var configDir = AppSettings.SettingsDir;
            Directory.CreateDirectory(configDir);

            _systemConfigWatcher = new FileSystemWatcher(configDir)
            {
                Filter = Path.GetFileName(AppSettings.SystemConfigPath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };

            _systemConfigWatcher.Changed += OnSystemConfigFileChanged;
            _systemConfigWatcher.Created += OnSystemConfigFileChanged;
            _systemConfigWatcher.Renamed += OnSystemConfigFileChanged;
            _systemConfigWatcher.EnableRaisingEvents = true;
        }

        private void OnSystemConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            if (IsDisposed)
                return;

            var elapsed = DateTime.UtcNow - _lastSystemConfigWriteUtc;
            if (elapsed < TimeSpan.FromMilliseconds(300))
                return;

            ScheduleSystemConfigReload($"File {e.ChangeType}");
        }

        private void ScheduleSystemConfigReload(string reason)
        {
            if (_reloadScheduled)
                return;

            _reloadScheduled = true;
            Task.Run(async () =>
            {
                await Task.Delay(300);
                if (IsDisposed)
                    return;

                BeginInvoke(new Action(() =>
                {
                    _reloadScheduled = false;
                    if (_isStitching)
                    {
                        _pendingSystemConfigReload = true;
                        Logger.Info($"System config change detected ({reason}). Will reload after stitching completes.");
                        NotifyProtocolStatus("System config update queued (waiting for stitching to finish).");
                        return;
                    }

                    ReloadSystemConfig($"Detected change ({reason}).");
                }));
            });
        }

        private void ReloadSystemConfig(string reason)
        {
            if (_reloadInProgress)
                return;

            _reloadInProgress = true;
            try
            {
                Logger.Info($"Reloading system config: {reason}");
                NotifyProtocolStatus("System config updating...");

                var loaded = SystemConfig.LoadFromYaml(AppSettings.SystemConfigPath);
                ApplySystemConfigValues(loaded);

                Logger.Info("System config reloaded successfully.");
                NotifyProtocolStatus("System config update completed.");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to reload system config.", ex);
                NotifyProtocolStatus($"System config update failed: {ex.Message}");
            }
            finally
            {
                _reloadInProgress = false;
            }
        }

        private void ApplySystemConfigValues(SystemConfig source)
        {
            if (source == null)
                return;

            _systemConfig.RobotHost = source.RobotHost;
            _systemConfig.RobotPort = source.RobotPort;
            _systemConfig.ConnectAttempts = source.ConnectAttempts;
            _systemConfig.ConnectDelaySeconds = source.ConnectDelaySeconds;
            _systemConfig.AutoLoadLastFolder = source.AutoLoadLastFolder;
            _systemConfig.AutoLoadFolder = source.AutoLoadFolder;
            _systemConfig.ClusterOrderMode = source.ClusterOrderMode;
            _systemConfig.OrderMode = source.OrderMode;
            _systemConfig.InvertX = source.InvertX;
            _systemConfig.StartCorner = source.StartCorner;
            _systemConfig.RobotMovement = source.RobotMovement;
            _systemConfig.GapFactor = source.GapFactor;
            _systemConfig.GapRow = source.GapRow;
            _systemConfig.SaveMode = source.SaveMode;
            _systemConfig.ComposeMegapix = source.ComposeMegapix;

            _settings.SystemConfig = _systemConfig.Clone();
            _systemConfigGrid.RefreshValues();
            ApplySystemConfigToUi();
        }

        private OrderOptions BuildOrderOptions()
        {
            return new OrderOptions
            {
                GapFactor = (double)_nudGap.Value,
                RowFactor = (double)_nudRow.Value,
                InvertXOnParse = _chkInvertX.Checked,
                Mode = CurrentMode,
                // [Codex] [Change time: 260318] [Replace removed legacy corner/direction properties with unified scan-direction state]
                // StartCorner = CurrentCorner,
                // RobotMovement = CurrentRobotDir,
                StartCorner = _systemConfig.StartCorner,
                RobotMovement = _systemConfig.RobotMovement,
                ClusterOrder = CurrentClusterOrder,
                NodeInterval = (int)num_node.Value
            };
        }

        private StitchRunConfig BuildRunConfig()
        {
            var matchCfg = _matchConfig.Clone();
            matchCfg.WorkMegapix = 10;
            matchCfg.CoarseWorkMegapix = 2;
            matchCfg.FineWorkMegapix = 10;
            matchCfg.RansacThresh = 5;
            matchCfg.EnforceRobotDirection = true;
            matchCfg.MaxPerpOffsetPx = 10;
            matchCfg.PreferPerpOffsetConstraint = true;

            var cfg = new StitchRunConfig
            {
                MatchCfg = matchCfg,
                UseRigidForGlobal = true,
                MaxCanvasMegapix = 250,
                FallbackOffsetHorizontal = (-15, -339),
                FallbackOffsetVertical = (-311, -13)
            };

            return cfg;
        }

        private StitchingImageRunner BuildStitcher()
        {
            return new StitchingImageRunner
            {
                SaveMode = CurrentSaveMode,
                ComposeMegapix = _systemConfig.ComposeMegapix
            };
        }

        private bool TryEnsureOutputFolder(out string outputFolder, bool allowPrompt)
        {
            outputFolder = _txtOutput.Text.Trim();
            if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
            {
                if (!allowPrompt)
                {
                    outputFolder = Path.Combine(_lastFolder, "RESULT");
                    _outputFolder = outputFolder;
                    PrepareOutputFolder(outputFolder);
                    _txtOutput.Text = outputFolder;
                    return true;
                    //return false;
                }

                using (var dlg = new FolderBrowserDialog
                {
                    Description = "Select output folder for stitched images...",
                    ShowNewFolderButton = true,
                    SelectedPath = !string.IsNullOrWhiteSpace(outputFolder) ? outputFolder : ""
                })
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return false;

                    outputFolder = dlg.SelectedPath;
                    _txtOutput.Text = outputFolder;
                }
            }

            Directory.CreateDirectory(outputFolder);
            _outputFolder = outputFolder;
            return true;
        }

        private static void PrepareOutputFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return;

            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    try { File.Delete(file); } catch { }
                }

                foreach (var dir in Directory.EnumerateDirectories(folder))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }

            Directory.CreateDirectory(folder);
        }

        private sealed class StitchJob
        {
            public int GroupId { get; set; }
            public TraversalComponent Component { get; set; }
        }

        // [Codex] [Change time: 260324] [Support both single-request group and multi-selected UI groups for stitching jobs.]
        private List<StitchJob> BuildJobs(OrderOptions options, int? targetGroupId, HashSet<int> targetGroupIds)
        {
            var jobs = new List<StitchJob>();
            var groupIds = _store.GroupIds;
            if (targetGroupIds != null && targetGroupIds.Count > 0)
            {
                groupIds = groupIds.Where(targetGroupIds.Contains).ToList();
            }
            else if (targetGroupId.HasValue && targetGroupId.Value >= 0 && groupIds.Contains(targetGroupId.Value))
            {
                groupIds = new List<int> { targetGroupId.Value };
            }

            foreach (var groupId in groupIds)
            {
                if (!_store.TryGetGroup(groupId, out var images)) continue;
                var arrange = robotArrange.Arrange(images, options);
                var traversal = TraversalGraph.BuildBatch(groupId, arrange, options);
                foreach (var graphComp in traversal.Components ?? Array.Empty<TraversalComponent>())
                    graphComp.Graph?.DebugPrintEdges();
                foreach (var comp in traversal.Components ?? Array.Empty<TraversalComponent>())
                {
                    jobs.Add(new StitchJob
                    {
                        GroupId = groupId,
                        Component = comp
                    });
                }
            }

            return jobs;
        }

        private async void runAll_btn_Click(object sender, EventArgs e)
        {
            _requestedGroupId = null;
            _requestedGroupIds = null;
            await ExecuteStitchingAsync(true, true);
        }

        // [Codex] [Change time: 260324] [Execute stitch flow like Run All but restrict to groups selected in _lstGroups.]
        private async void runSelected_btn_Click(object sender, EventArgs e)
        {
            var selectedGroupIds = _lstGroups.SelectedItems.Cast<object>()
                .Where(item => item is int)
                .Select(item => (int)item)
                .Distinct()
                .ToHashSet();

            if (selectedGroupIds.Count == 0)
            {
                MessageBox.Show(this, "Please select at least one group.", "Stitching", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _requestedGroupId = null;
            _requestedGroupIds = selectedGroupIds;
            await ExecuteStitchingAsync(true, true);
        }

        private async void OnStitchRequested(StitchRequest request)
        {
            if (request?.Data == null)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnStitchRequested(request)));
                return;
            }

            // Tony 20260202 Apply stitch request data from AH01 and run stitching.
            if (!TryApplyStitchRequest(request.Data, out var errorCode, out var errorMessage))
            {
                Logger.Error($"Stitch request rejected: {errorMessage}");
                StopStitching($"Stitch request rejected: {errorMessage}");
                await _connectionManager.SendStitchResultsAsync(request.MessageId, false, errorCode);
                return;
            }
#if DEBUG
            Logger.Info("DEBUG: Stitch request received; skipping real run and waiting.");
            //_isStitching = true;
            //await Task.Delay(TimeSpan.FromSeconds(300));
            //_isStitching = false;
            var result = await ExecuteStitchingAsync(false, false);
            await _connectionManager.SendStitchResultsAsync(request.MessageId, true, string.Empty);
            return;
#else
            var result = await ExecuteStitchingAsync(false, false);
            await _connectionManager.SendStitchResultsAsync(request.MessageId, result.Success, result.ErrorCode);
#endif
        }

        private async Task<StitchExecutionResult> ExecuteStitchingAsync(bool allowPrompt, bool showErrors)
        {
            if (_store.GroupIds.Count == 0)
            {
                if (showErrors)
                    MessageBox.Show(this, "No groups loaded yet.", "Stitching", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return new StitchExecutionResult(false, ErrorCodePLP.DataSharedFolderEmpty);
            }

            if (!TryEnsureOutputFolder(out var outputFolder, allowPrompt))
            {
                return new StitchExecutionResult(false, ErrorCodePLP.DataImageSaveFailed);
            }

            var orderOptions = BuildOrderOptions();
            var targetGroupId = _requestedGroupId;
            var targetGroupIds = _requestedGroupIds;
            var jobs = BuildJobs(orderOptions, targetGroupId, targetGroupIds);
            if (jobs.Count == 0)
            {
                if (showErrors)
                    MessageBox.Show(this, "No orders found to stitch.", "Stitching", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return new StitchExecutionResult(false, ErrorCodePLP.DataSharedFolderEmpty);
            }

            var runCfg = BuildRunConfig();
            var stitcher = BuildStitcher();
            runAll_btn.Enabled = false;
            runSelected_btn.Enabled = false;
            _isStitching = true;
            Logger.Info($"Run stitching: method={runCfg.MatchCfg.Method} alpha={runCfg.MatchCfg.ConvertAlpha:0.###} beta={runCfg.MatchCfg.ConvertBeta:0.###} h(tx,ty)=({runCfg.MatchCfg.ManualOffsetHorizontalTx:0.###},{runCfg.MatchCfg.ManualOffsetHorizontalTy:0.###}) v(tx,ty)=({runCfg.MatchCfg.ManualOffsetVerticalTx:0.###},{runCfg.MatchCfg.ManualOffsetVerticalTy:0.###})");

            try
            {
                using (var dialog = new ProcessDialog())
                {
                    dialog.SetTotalOrders(jobs.Count);
                    dialog.Show(this);
                    _stitchCts = new CancellationTokenSource();
                    var token = _stitchCts.Token;
                    dialog.CancelRequested += () => StopStitching("Processing dialog closed.");

                    await Task.Run(() =>
                    {
                        var orderIndex = 0;
                        foreach (var job in jobs)
                        {
                            token.ThrowIfCancellationRequested();
                            var outputPath = Path.Combine(outputFolder, $"group{job.GroupId}_order{job.Component.ComponentIndex}.tiff");
                            dialog.UpdateStatus(orderIndex, job.GroupId, job.Component.ComponentIndex, job.Component.Points?.Length ?? 0);

                            using (var res = OrderStitchRunner.TestOneComponentAndStitch(job.Component, runCfg, stitcher, outputPath))
                            {
                                Debug.WriteLine($"Matching(ms)={res.MatchingMs:0.##}, Stitching(ms)={res.StitchingMs:0.##}, Save(ms)={res.SaveMs:0.##}, out={res.OutputPath}");
                                var bad = res.EdgeTransforms.Count(er => !er.Ok);
                                Debug.WriteLine($"Edges total={res.EdgeTransforms.Count}, bad={bad}, fallback={res.EdgeTransforms.Count(er => er.UsedFallback)}");
                                Logger.Info($"Component {job.Component.ComponentIndex}: edges={res.EdgeTransforms.Count} bad={bad} fallback={res.EdgeTransforms.Count(er => er.UsedFallback)}");
                            }
                            orderIndex++;
                        }
                    }, token);

                    dialog.MarkCompleted();
                    dialog.Close();
                }

                return new StitchExecutionResult(true, string.Empty);
            }
            catch (OperationCanceledException)
            {
                Logger.Warning("Stitching cancelled by user.");
                return new StitchExecutionResult(false, ErrorCodePLP.DataStitchCancelled);
            }
            catch (Exception ex)
            {
                Logger.Error("Stitching failed.", ex);
                if (showErrors)
                    MessageBox.Show(this, $"Stitching failed: {ex.Message}", "Stitching", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new StitchExecutionResult(false, ErrorCodePLP.DataStitchFailed);
            }
            finally
            {
                runAll_btn.Enabled = true;
                runSelected_btn.Enabled = true;
                _isStitching = false;
                _requestedGroupId = null;
                _requestedGroupIds = null;
                _stitchCts?.Dispose();
                _stitchCts = null;
                if (_pendingSystemConfigReload)
                {
                    _pendingSystemConfigReload = false;
                    ReloadSystemConfig("Pending update after stitching.");
                }
            }
        }

        private bool TryApplyStitchRequest(StitchPayload data, out string errorCode, out string errorMessage)
        {
            errorCode = string.Empty;
            errorMessage = string.Empty;
            if (data == null)
            {
                errorCode = ErrorCodePLP.DataStitchFailed;
                errorMessage = "Request payload missing.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(data.SharedFolder))
            {
                _systemConfig.SaveMode = SaveMode.Full;
                var timeout = TimeSpan.FromSeconds(3);
                if (!TryValidateSharedFolder(data.SharedFolder, timeout, out errorCode, out errorMessage))
                {
                    return false;
                }

                try
                {
                    lbl_dir.Text = data.SharedFolder;
                    LoadFolder(data.SharedFolder);
                }
                catch (Exception ex)
                {
                    errorCode = ErrorCodeConnection.SharedFolderUnavailable;
                    errorMessage = $"Failed to load shared folder: {ex.Message}";
                    Logger.Error(errorMessage, ex);
                    return false;
                }
                if (_lastLoadFileCount <= 0)
                {
                    errorCode = ErrorCodePLP.DataSharedFolderEmpty;
                    errorMessage = "Shared folder has no images.";
                    return false;
                }

                if (_lastLoadParsedCount <= 0)
                {
                    errorCode = ErrorCodePLP.DataImageFormatInvalid;
                    errorMessage = "No valid images parsed from shared folder.";
                    return false;
                }

                if (!TryValidateSampleImage(out errorCode, out errorMessage))
                {
                    return false;
                }

                var expectedCount = Math.Max(0, data.Rows * data.Columns);
                if (expectedCount > 0 && _lastLoadParsedCount < expectedCount)
                {
                    errorCode = _lastLoadParsedCount < expectedCount
                        ? ErrorCodePLP.DataSharedFolderInsufficient
                        : ErrorCodePLP.DataImageCountMismatch;
                    errorMessage = $"Image count mismatch. Expected={expectedCount}, Parsed={_lastLoadParsedCount}.";
                    return false;
                }

                _systemConfig.StartCorner = (StartCorner)Math.Max(0, Math.Min(3, data.StartPoint));
                _systemConfig.RobotMovement = (RobotMovement)Math.Max(0, Math.Min(3, data.Direction));

                var overlapRaw = data.Overlap;
                var overlapFraction = overlapRaw > 1.0 ? overlapRaw / 100d : overlapRaw;
                if (overlapRaw > 0)
                {
                    _matchConfig.PhaseCorrFractionW = overlapRaw;
                    _matchConfig.PhaseCorrFractionH = overlapRaw;
                    _matchConfig.RoiMatchFraction = overlapFraction;
                    _configGrid.RefreshValues();
                    _configGrid.HighlightKeys(new[]
                    {
                        nameof(StitchingConfig.PhaseCorrFractionW),
                        nameof(StitchingConfig.PhaseCorrFractionH),
                        nameof(StitchingConfig.RoiMatchFraction)
                    });
                }

                _requestedGroupIds = null;
                _requestedGroupId = data.GroupId;
                if (_requestedGroupId == -1 || !_store.GroupIds.Contains(_requestedGroupId.Value))
                {
                    _requestedGroupId = null;
                }
                else if (_lstGroups.Items.Contains(_requestedGroupId.Value))
                {
                    _lstGroups.SelectedItem = _requestedGroupId.Value;
                }

                ApplySystemConfigToUi();
                return true;
            }

            // If SharedFolder is null or whitespace, treat as error
            errorCode = ErrorCodeConnection.SharedFolderNotFound;
            errorMessage = "Shared folder path is empty.";
            return false;
        }

        private void ApplyStitchRequest(StitchPayload data)
        {
            if (data == null)
                return;

            if (!string.IsNullOrWhiteSpace(data.SharedFolder))
            {
                _systemConfig.SaveMode = SaveMode.Full;
                lbl_dir.Text = data.SharedFolder;
                LoadFolder(data.SharedFolder);
            }

            _systemConfig.StartCorner = (StartCorner)Math.Max(0, Math.Min(3, data.StartPoint));
            _systemConfig.RobotMovement = (RobotMovement)Math.Max(0, Math.Min(3, data.Direction));
            ApplySystemConfigToUi();
        }

        private sealed class StitchExecutionResult
        {
            public StitchExecutionResult(bool success, string errorCode)
            {
                Success = success;
                ErrorCode = errorCode;
            }

            public bool Success { get; }
            public string ErrorCode { get; }
        }

        private bool TryValidateSharedFolder(string folder, TimeSpan timeout, out string errorCode, out string errorMessage)
        {
            errorCode = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(folder))
            {
                errorCode = ErrorCodeConnection.SharedFolderNotFound;
                errorMessage = "Shared folder path is empty.";
                return false;
            }

            try
            {
                var existsTask = Task.Run(() => Directory.Exists(folder));
                if (!existsTask.Wait(timeout))
                {
                    errorCode = ErrorCodeConnection.SharedFolderTimeout;
                    errorMessage = "Shared folder access timed out.";
                    return false;
                }

                if (!existsTask.Result)
                {
                    errorCode = ErrorCodeConnection.SharedFolderNotFound;
                    errorMessage = "Shared folder not found.";
                    return false;
                }

                var probeTask = Task.Run(() => Directory.EnumerateFiles(folder).Take(1).Any());
                if (!probeTask.Wait(timeout))
                {
                    errorCode = ErrorCodeConnection.SharedFolderTimeout;
                    errorMessage = "Shared folder probe timed out.";
                    return false;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                errorCode = ErrorCodeConnection.SharedFolderUnauthorized;
                errorMessage = $"Shared folder access denied: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorCode = ErrorCodeConnection.SharedFolderUnavailable;
                errorMessage = $"Shared folder unavailable: {ex.Message}";
                return false;
            }

            return true;
        }

        private bool TryValidateSampleImage(out string errorCode, out string errorMessage)
        {
            errorCode = string.Empty;
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(_lastLoadSampleImagePath))
                return true;

            try
            {
                using (var img = ImageRead.ReadImage(_lastLoadSampleImagePath, OpenCvSharp.ImreadModes.ReducedColor2))
                {
                    if (img == null || img.Empty())
                    {
                        errorCode = ErrorCodePLP.DataImageReadFailed;
                        errorMessage = "Sample image read returned empty data.";
                        return false;
                    }
                }
            }
            catch (ImageRead.ImageReadException ex)
            {
                errorCode = ErrorCodePLP.DataImageReadFailed;
                errorMessage = $"Sample image read failed: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                errorCode = ErrorCodePLP.DataImageReadFailed;
                errorMessage = $"Sample image read error: {ex.Message}";
                return false;
            }

            return true;
        }
        private sealed class ProtocolSample
        {
            public string LabelProtocol { get; set; }
            public Func<string> Builder { get; set; }

            public override string ToString() => LabelProtocol;
        }

        private void InitializeProtocolSamples()
        {
            var samples = new[]
            {
                new ProtocolSample
                {
                    LabelProtocol = "ISP.LINK.ACK",
                    Builder = () => BuildAckSample("LINK", "OK", string.Empty)
                },
                new ProtocolSample
                {
                    LabelProtocol = "ISP.STITCH.ACK",
                    Builder = () => BuildAckSample("STITCH", "OK", string.Empty)
                },
                new ProtocolSample
                {
                    LabelProtocol = "ISP.STITCH.RESULTS (Success)",
                    Builder = () => BuildResultsSample(true, string.Empty)
                },
                new ProtocolSample
                {
                    LabelProtocol = "ISP.STITCH.RESULTS (Fail)",
                    Builder = () => BuildResultsSample(false, "E1001")
                }
            };

            _cmbProtocolSamples.Items.Clear();
            _cmbProtocolSamples.Items.AddRange(samples);
            if (_cmbProtocolSamples.Items.Count > 0)
                _cmbProtocolSamples.SelectedIndex = 0;
        }

        private async Task SendProtocolSampleAsync()
        {
            if (!(_cmbProtocolSamples.SelectedItem is ProtocolSample sample))
                return;

            var payload = sample.Builder?.Invoke();
            if (string.IsNullOrWhiteSpace(payload))
                return;

            await _connectionManager.SendRawAsync(payload, CancellationToken.None);
        }

        private static string BuildAckSample(string task, string ack, string error)
        {
            return $"{{\"respond\":\"ISP.{task}.ACK\",\"message_id\":1001,\"source\":\"ISP\",\"destination\":\"AH01\",\"task\":\"{task}\",\"action\":\"ACK\",\"timestamp\":\"{DateTime.Now:yyyy-MM-ddTHH:mm:ss}\",\"data\":{{\"ACK\":\"{ack}\",\"errors\":\"{error}\"}}}}";
        }

        private static string BuildResultsSample(bool success, string error)
        {
            var status = success ? 0 : 1;
            var errors = success ? string.Empty : error;
            return $"{{\"respond\":\"ISP.STITCH.RESULTS\",\"message_id\":1001,\"source\":\"ISP\",\"destination\":\"AH01\",\"task\":\"STITCH\",\"action\":\"RESULTS\",\"timestamp\":\"{DateTime.Now:yyyy-MM-ddTHH:mm:ss}\",\"data\":{{\"status\":{status},\"errors\":\"{errors}\"}}}}";
        }


        private void OpenConfigLocation(object sender, EventArgs e)
        {
            try
            {
                var configDir = AppSettings.SettingsDir;
                Directory.CreateDirectory(configDir);

                Process.Start(new ProcessStartInfo
                {
                    FileName = configDir,
                    UseShellExecute = true
                });

                Logger.Info($"Opened config folder: {configDir}");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open config folder.", ex);
                MessageBox.Show(this, $"Failed to open config folder: {ex.Message}", "Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        
    }
}
