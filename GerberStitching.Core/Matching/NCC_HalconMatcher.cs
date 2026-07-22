using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
#if DEBUG
using System.Drawing;
using System.Windows.Forms;
using OpenCvSharp.Extensions;
#endif
using GerberViewer.Stitching.Imaging.ImageInterop;
using GerberViewer.Stitching.Transforms;
using HalconDotNet;
using OpenCvSharp;

namespace GerberViewer.Stitching.Matching
{
    public sealed class NCC_HalconMatcher : IMatcher
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, NccModelEntry> _modelCache = new Dictionary<string, NccModelEntry>(StringComparer.Ordinal);
        private readonly IImageInteropService _imageInterop;
        private readonly MatcherGeometryValidator _validator = new MatcherGeometryValidator();
        private bool _disposed;

        public NCC_HalconMatcher() : this(new ImageInteropService())
        {
        }

        public NCC_HalconMatcher(IImageInteropService imageInterop)
        {
            if (imageInterop == null) throw new ArgumentNullException("imageInterop");
            _imageInterop = imageInterop;
        }

        public string MatcherName { get { return "NCC_HalconMatcher"; } }

        public MatchResult Match(MatchRequest request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            if (cancellationToken.IsCancellationRequested)
                return WithTime(MatchResult.Failed(MatcherName, MatchFailureReason.Cancelled, "HALCON NCC was cancelled before start."), sw);
            var invalid = _validator.ValidateRequest(request, MatcherName);
            if (invalid != null) return WithTime(invalid, sw);
            //var options = request.Options ?? new MatcherOptions();
            var options = new MatcherOptions();
            HTuple row = null;
            HTuple column = null;
            HTuple angle = null;
            HTuple score = null;
            try
            {
                using (var movingHObject = _imageInterop.ToHObjectCopy(request.MovingImage, InteropPixelFormat.Mono8))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var key = BuildCacheKey(request, options);
#if DEBUG
                    ShowDebugInputDialog(request, options, key);
#endif
                    var entry = GetOrCreateModel(key, request.ReferenceImage, options, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    HOperatorSet.FindNccModel(movingHObject, entry.ModelId, options.NccAngleStartRad, options.NccAngleExtentRad, options.NccMinScore, options.NccMaxMatches, options.NccMaxOverlap, options.NccSubPixel, options.NccNumLevels, out row, out column, out angle, out score);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (score == null || score.Length <= 0)
                        return WithTime(MatchResult.Failed(MatcherName, MatchFailureReason.CorrelationBelowThreshold, "HALCON find_ncc_model returned no match above NccMinScore."), sw);

                    var bestRow = row[0].D;
                    var bestColumn = column[0].D;
                    var bestAngle = angle[0].D;
                    var bestScore = score[0].D;
                    if (bestScore < options.NccMinScore)
                        return WithTime(MatchResult.Failed(MatcherName, MatchFailureReason.CorrelationBelowThreshold, "HALCON NCC score is below NccMinScore."), sw);

                    var referenceToMoving = ReferenceToMovingFromHalcon(bestColumn, bestRow, bestAngle);
                    var movingToReference = new Transform2D(referenceToMoving).Invert();
                    var geometryFailure = ValidateNccGeometry(movingToReference, request.ReferenceImage, request.MovingImage, options);
                    if (geometryFailure != null) return WithTime(geometryFailure, sw);
                    var result = new MatchResult
                    {
                        Success = true,
                        MatcherName = MatcherName,
                        MovingToReferenceTransform = movingToReference,
                        TranslationX = movingToReference[0, 2],
                        TranslationY = movingToReference[1, 2],
                        RotationDeg = Math.Atan2(movingToReference[1, 0], movingToReference[0, 0]) * 180.0 / Math.PI,
                        Scale = 1d,
                        RawScore = bestScore,
                        NormalizedConfidence = bestScore,
                        OverlapRatio = 1d,
                        FailureReason = MatchFailureReason.None
                    };
                    result.Diagnostics["HalconCreateOperator"] = "create_ncc_model";
                    result.Diagnostics["HalconFindOperator"] = "find_ncc_model";
                    result.Diagnostics["HalconClearOperator"] = "clear_ncc_model";
                    result.Diagnostics["ModelCacheKey"] = key;
                    result.Diagnostics["CacheHit"] = entry.UseCount > 1 ? "true" : "false";
                    result.Diagnostics["ModelOrigin"] = "row=0,column=0 via set_ncc_model_origin; row/column output maps reference image origin into moving image coordinates";
                    result.Diagnostics["HalconRow"] = bestRow.ToString(CultureInfo.InvariantCulture);
                    result.Diagnostics["HalconColumn"] = bestColumn.ToString(CultureInfo.InvariantCulture);
                    result.Diagnostics["HalconAngleRad"] = bestAngle.ToString(CultureInfo.InvariantCulture);
                    result.Diagnostics["HalconNccScore"] = bestScore.ToString(CultureInfo.InvariantCulture);
                    result.Diagnostics["TransformDirection"] = "MovingImage -> ReferenceImage";
                    return WithTime(result, sw);
                }
            }
            catch (OperationCanceledException)
            {
                return WithTime(MatchResult.Failed(MatcherName, MatchFailureReason.Cancelled, "HALCON NCC was cancelled."), sw);
            }
            catch (Exception ex)
            {
                return WithTime(MatchResult.Failed(MatcherName, MatchFailureReason.RuntimeFailure, ex.Message), sw);
            }
            finally
            {
                DisposeTuple(row); DisposeTuple(column); DisposeTuple(angle); DisposeTuple(score);
            }
        }

        public int CachedModelCount
        {
            get { lock (_syncRoot) return _modelCache.Count; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            lock (_syncRoot)
            {
                foreach (var entry in _modelCache.Values) entry.ClearOnce();
                _modelCache.Clear();
                _disposed = true;
            }
        }

        private NccModelEntry GetOrCreateModel(string key, Mat referenceImage, MatcherOptions options, CancellationToken cancellationToken)
        {
            lock (_syncRoot)
            {
                NccModelEntry existing;
                if (_modelCache.TryGetValue(key, out existing))
                {
                    existing.UseCount++;
                    return existing;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            HTuple modelId = null;
            try
            {
                using (var referenceHObject = _imageInterop.ToHObjectCopy(referenceImage, InteropPixelFormat.Mono8))
                {
                    HOperatorSet.CreateNccModel(referenceHObject, options.NccNumLevels, options.NccAngleStartRad, options.NccAngleExtentRad, options.NccAngleStepRad, options.NccMetric, out modelId);
                    HOperatorSet.SetNccModelOrigin(modelId, 0.0, 0.0);
                    var entry = new NccModelEntry(modelId);
                    modelId = null;
                    lock (_syncRoot)
                    {
                        NccModelEntry race;
                        if (_modelCache.TryGetValue(key, out race))
                        {
                            entry.ClearOnce();
                            race.UseCount++;
                            return race;
                        }
                        _modelCache.Add(key, entry);
                        return entry;
                    }
                }
            }
            finally
            {
                DisposeTuple(modelId);
            }
        }

        private static string BuildCacheKey(MatchRequest request, MatcherOptions options)
        {
            var sampleTileId = !string.IsNullOrWhiteSpace(request.SampleTileId) ? request.SampleTileId : (request.OrderIndex.HasValue ? request.OrderIndex.Value.ToString(CultureInfo.InvariantCulture) : "__unspecified_tile__");
            var preprocessingVariant = string.IsNullOrWhiteSpace(options.PreprocessingVariant) ? "default" : options.PreprocessingVariant;
            return string.Join("|", new[] {
                "SampleTileId=" + sampleTileId,
                "PreprocessingVariant=" + preprocessingVariant,
                "Rows=" + request.ReferenceImage.Rows.ToString(CultureInfo.InvariantCulture),
                "Cols=" + request.ReferenceImage.Cols.ToString(CultureInfo.InvariantCulture),
                "Type=" + request.ReferenceImage.Type().ToString(),
                "Channels=" + request.ReferenceImage.Channels().ToString(CultureInfo.InvariantCulture),
                "ContentHash=" + CalculateMatContentHash(request.ReferenceImage),
                "AngleStart=" + options.NccAngleStartRad.ToString("R", CultureInfo.InvariantCulture),
                "AngleExtent=" + options.NccAngleExtentRad.ToString("R", CultureInfo.InvariantCulture),
                "AngleStep=" + options.NccAngleStepRad.ToString("R", CultureInfo.InvariantCulture),
                "Metric=" + options.NccMetric,
                "NumLevels=" + options.NccNumLevels.ToString(CultureInfo.InvariantCulture)
            });
        }


        private static string CalculateMatContentHash(Mat image)
        {
            if (image == null || image.Empty()) return "empty";

            const ulong offsetBasis = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offsetBasis;
            var rowLength = checked((int)(image.Cols * image.ElemSize()));
            var buffer = new byte[rowLength];

            for (var row = 0; row < image.Rows; row++)
            {
                Marshal.Copy(image.Ptr(row), buffer, 0, rowLength);
                for (var i = 0; i < rowLength; i++)
                {
                    hash ^= buffer[i];
                    hash *= prime;
                }
            }

            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }
#if DEBUG
        private static void ShowDebugInputDialog(MatchRequest request, MatcherOptions options, string cacheKey)
        {
            if (Debugger.IsAttached == false) return;
            using (var form = new Form())
            using (var split = new SplitContainer())
            using (var referenceBox = new PictureBox())
            using (var movingBox = new PictureBox())
            using (var infoBox = new TextBox())
            using (var referenceBitmap = MatToDebugBitmap(request.ReferenceImage))
            using (var movingBitmap = MatToDebugBitmap(request.MovingImage))
            {
                form.Text = "NCC_HalconMatcher DEBUG - Input Images";
                form.Width = 1200;
                form.Height = 800;
                form.StartPosition = FormStartPosition.CenterScreen;

                infoBox.Multiline = true;
                infoBox.ReadOnly = true;
                infoBox.ScrollBars = ScrollBars.Vertical;
                infoBox.Dock = DockStyle.Top;
                infoBox.Height = 150;
                infoBox.Text = BuildDebugInfoText(request, options, cacheKey);

                split.Dock = DockStyle.Fill;
                split.Orientation = Orientation.Vertical;
                split.Panel1.Controls.Add(referenceBox);
                split.Panel2.Controls.Add(movingBox);

                ConfigureDebugPictureBox(referenceBox, referenceBitmap, "ReferenceImage");
                ConfigureDebugPictureBox(movingBox, movingBitmap, "MovingImage / movingHObject source");

                form.Controls.Add(split);
                form.Controls.Add(infoBox);
                form.ShowDialog();
            }
        }

        private static void ConfigureDebugPictureBox(PictureBox pictureBox, Bitmap bitmap, string label)
        {
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.Image = (Bitmap)bitmap.Clone();
            pictureBox.Tag = label;
        }

        private static Bitmap MatToDebugBitmap(Mat image)
        {
            if (image == null || image.Empty()) return new Bitmap(1, 1);
            using (var display = new Mat())
            {
                if (image.Channels() == 1)
                    Cv2.CvtColor(image, display, ColorConversionCodes.GRAY2BGR);
                else if (image.Channels() == 4)
                    Cv2.CvtColor(image, display, ColorConversionCodes.BGRA2BGR);
                else
                    image.CopyTo(display);
                return BitmapConverter.ToBitmap(display);
            }
        }

        private static string BuildDebugInfoText(MatchRequest request, MatcherOptions options, string cacheKey)
        {
            return string.Join(Environment.NewLine, new[] {
                "The movingHObject below is created from request.MovingImage with InteropPixelFormat.Mono8.",
                "ReferenceImage: " + DescribeMat(request.ReferenceImage),
                "MovingImage: " + DescribeMat(request.MovingImage),
                "SampleTileId: " + (request.SampleTileId ?? "<null>"),
                "OrderIndex: " + (request.OrderIndex.HasValue ? request.OrderIndex.Value.ToString(CultureInfo.InvariantCulture) : "<null>"),
                "PreprocessingVariant: " + (options.PreprocessingVariant ?? "<null>"),
                "CacheKey: " + cacheKey
            });
        }

        private static string DescribeMat(Mat image)
        {
            if (image == null) return "<null>";
            return string.Format(CultureInfo.InvariantCulture, "Rows={0}, Cols={1}, Type={2}, Channels={3}, ContentHash={4}", image.Rows, image.Cols, image.Type(), image.Channels(), CalculateMatContentHash(image));
        }
#endif

        private MatchResult ValidateNccGeometry(Transform2D movingToReference, Mat referenceImage, Mat movingImage, MatcherOptions options)
        {
            if (movingToReference == null) return MatchResult.Failed(MatcherName, MatchFailureReason.NonFiniteTransform, "HALCON NCC did not produce a transform.");
            var translationFailure = _validator.ValidateTranslation(movingToReference[0, 2], movingToReference[1, 2], 1.0, options, MatcherName);
            if (translationFailure != null) return translationFailure;
            var rotationDeg = Math.Atan2(movingToReference[1, 0], movingToReference[0, 0]) * 180.0 / Math.PI;
            if (Math.Abs(rotationDeg) > options.MaxAbsRotationDeg) return MatchResult.Failed(MatcherName, MatchFailureReason.GeometryRejected, "HALCON NCC rotation exceeds MaxAbsRotationDeg.");
            var scaleX = Math.Sqrt(movingToReference[0, 0] * movingToReference[0, 0] + movingToReference[1, 0] * movingToReference[1, 0]);
            var scaleY = Math.Sqrt(movingToReference[0, 1] * movingToReference[0, 1] + movingToReference[1, 1] * movingToReference[1, 1]);
            if (scaleX < options.MinScale || scaleX > options.MaxScale || scaleY < options.MinScale || scaleY > options.MaxScale) return MatchResult.Failed(MatcherName, MatchFailureReason.GeometryRejected, "HALCON NCC scale is outside configured bounds.");
            var overlap = MatcherGeometryValidator.CalculateSameSizeOverlap(referenceImage, movingImage);
            if (overlap < options.MinOverlapRatio) return MatchResult.Failed(MatcherName, MatchFailureReason.GeometryRejected, "HALCON NCC image overlap is below MinOverlapRatio.");
            return null;
        }

        private static double[,] ReferenceToMovingFromHalcon(double column, double row, double angleRad)
        {
            var c = Math.Cos(angleRad);
            var s = Math.Sin(angleRad);
            return new[,] { { c, -s, column }, { s, c, row }, { 0d, 0d, 1d } };
        }

        private static MatchResult WithTime(MatchResult result, Stopwatch sw)
        {
            sw.Stop();
            result.ProcessingTime = sw.Elapsed;
            return result;
        }

        private static void DisposeTuple(HTuple tuple)
        {
            if (tuple != null) tuple.Dispose();
        }

        private sealed class NccModelEntry
        {
            private bool _cleared;
            public NccModelEntry(HTuple modelId)
            {
                if (modelId == null) throw new ArgumentNullException("modelId");
                ModelId = modelId;
                UseCount = 1;
            }

            public HTuple ModelId { get; private set; }
            public int UseCount { get; set; }

            public void ClearOnce()
            {
                if (_cleared) return;
                HOperatorSet.ClearNccModel(ModelId);
                ModelId.Dispose();
                _cleared = true;
            }
        }
    }
}
