using System;
using System.Diagnostics;
using System.Threading;
using GerberViewer.Stitching.Transforms;
using OpenCvSharp;

namespace GerberViewer.Stitching.Matching
{
    public sealed class PharseCorrMatcher : IMatcher
    {
        private readonly MatcherGeometryValidator _validator = new MatcherGeometryValidator();
        public string MatcherName { get { return "PharseCorrMatcher"; } }

        public MatchResult Match(MatchRequest request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            if (cancellationToken.IsCancellationRequested)
                return WithTime(MatchResult.Failed(MatcherName, MatchFailureReason.Cancelled, "Phase correlation was cancelled before start."), sw);

            var invalid = _validator.ValidateRequest(request, MatcherName);
            if (invalid != null) return WithTime(invalid, sw);
            var options = request.Options ?? new MatcherOptions();

            try
            {
                using (var referenceRoi = CropCopy(request.ReferenceImage, MatcherGeometryValidator.NormalizeRoi(request.ReferenceImage, request.ReferenceRoi)))
                using (var movingRoi = CropCopy(request.MovingImage, MatcherGeometryValidator.NormalizeRoi(request.MovingImage, request.MovingRoi)))
                using (var reference32 = ToGray32FCopy(referenceRoi))
                using (var moving32 = ToGray32FCopy(movingRoi))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var preparedInvalid = _validator.ValidatePreparedPair(reference32, moving32, options, MatcherName);
                    if (preparedInvalid != null) return WithTime(preparedInvalid, sw);

                    using (var window = new Mat())
                    {
                        Cv2.CreateHanningWindow(window, reference32.Size(), MatType.CV_32F);
                        cancellationToken.ThrowIfCancellationRequested();
                        double response;
                        var movingShiftInReference = Cv2.PhaseCorrelate(reference32, moving32, window, out response);
                        var translationX = -movingShiftInReference.X;
                        var translationY = -movingShiftInReference.Y;
                        var translationInvalid = _validator.ValidateTranslation(translationX, translationY, response, options, MatcherName);
                        if (translationInvalid != null)
                        {
                            translationInvalid.RawScore = response;
                            translationInvalid.NormalizedConfidence = response;
                            return WithTime(translationInvalid, sw);
                        }

                        var transform = MatcherTransformConverter.FromTranslation(translationX, translationY);
                        var result = new MatchResult
                        {
                            Success = true,
                            MatcherName = MatcherName,
                            MovingToReferenceTransform = transform,
                            TranslationX = translationX,
                            TranslationY = translationY,
                            RotationDeg = 0d,
                            Scale = 1d,
                            RawScore = response,
                            NormalizedConfidence = response,
                            OverlapRatio = MatcherGeometryValidator.CalculateSameSizeOverlap(reference32, moving32),
                            FailureReason = MatchFailureReason.None
                        };
                        result.Diagnostics["OpenCvFunction"] = "Cv2.PhaseCorrelate";
                        result.Diagnostics["WindowFunction"] = "Cv2.CreateHanningWindow";
                        result.Diagnostics["TransformDirection"] = "MovingImage -> ReferenceImage";
                        return WithTime(result, sw);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return WithTime(MatchResult.Failed(MatcherName, MatchFailureReason.Cancelled, "Phase correlation was cancelled."), sw);
            }
            catch (Exception ex)
            {
                return WithTime(MatchResult.Failed(MatcherName, MatchFailureReason.RuntimeFailure, ex.Message), sw);
            }
        }

        public void Dispose()
        {
        }

        private static MatchResult WithTime(MatchResult result, Stopwatch sw)
        {
            sw.Stop();
            result.ProcessingTime = sw.Elapsed;
            return result;
        }

        private static Mat CropCopy(Mat image, Rect roi)
        {
            using (var view = new Mat(image, roi))
                return view.Clone();
        }

        private static Mat ToGray32FCopy(Mat source)
        {
            Mat gray = null;
            try
            {
                if (source.Channels() == 1) gray = source.Clone();
                else if (source.Channels() == 3)
                {
                    gray = new Mat();
                    Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
                }
                else if (source.Channels() == 4)
                {
                    gray = new Mat();
                    Cv2.CvtColor(source, gray, ColorConversionCodes.BGRA2GRAY);
                }
                else throw new NotSupportedException("Unsupported channel count for phase correlation: " + source.Channels());

                var gray32 = new Mat();
                gray.ConvertTo(gray32, MatType.CV_32F);
                return gray32;
            }
            finally
            {
                if (gray != null) gray.Dispose();
            }
        }
    }
}
