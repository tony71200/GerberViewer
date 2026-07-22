using System;
using System.Threading;
using GerberViewer.Stitching.Transforms;

namespace GerberViewer.Stitching.Matching
{
    public sealed class MatcherPipelineResult
    {
        public bool Success { get; set; }
        public MatchResult FinalResult { get; set; }
        public MatchResult NccResult { get; set; }
        public MatchResult EccResult { get; set; }
        public bool UsedNccOnlyFallback { get; set; }
        public bool UsedExpectedEccInitialization { get; set; }
        public string SelectedStage { get; set; }
        public string FailureReason { get; set; }
    }

    public sealed class MatcherPipeline
    {
        private readonly IMatcherFactory _factory;

        public MatcherPipeline() : this(new MatcherFactory())
        {
        }

        public MatcherPipeline(IMatcherFactory factory)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            _factory = factory;
        }

        public MatcherPipelineResult MatchDirect(MatchRequest request, bool allowNccOnlyAcceptance, bool allowEccFromExpectedWhenNccFails, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException("request");
            cancellationToken.ThrowIfCancellationRequested();

            MatchResult nccResult;
            using (var ncc = _factory.CreateNccMatcher())
            {
                nccResult = ncc.Match(CloneRequest(request, request.InitialMovingToReferenceTransform), cancellationToken);
            }

            if (nccResult.Success)
            {
                MatchResult eccResult;
                using (var ecc = _factory.CreateEccMatcher())
                {
                    eccResult = ecc.Match(CloneRequest(request, nccResult.MovingToReferenceTransform), cancellationToken);
                }

                if (eccResult.Success)
                    return Success("NCC+ECC", eccResult, nccResult, eccResult, false, false);

                if (allowNccOnlyAcceptance)
                    return Success("NCC_ONLY_AFTER_ECC_FAIL", nccResult, nccResult, eccResult, true, false);

                return Failed("ECC failed after NCC pass and NCC-only acceptance is disabled.", nccResult, eccResult);
            }

            if (allowEccFromExpectedWhenNccFails && request.InitialMovingToReferenceTransform != null)
            {
                MatchResult eccExpectedResult;
                using (var ecc = _factory.CreateEccMatcher())
                {
                    eccExpectedResult = ecc.Match(CloneRequest(request, request.InitialMovingToReferenceTransform), cancellationToken);
                }

                if (eccExpectedResult.Success)
                    return Success("ECC_FROM_EXPECTED_AFTER_NCC_FAIL", eccExpectedResult, nccResult, eccExpectedResult, false, true);

                return Failed("NCC failed and ECC from expected transform also failed.", nccResult, eccExpectedResult);
            }

            return Failed("NCC failed and ECC-from-expected policy is disabled or no expected transform exists.", nccResult, null);
        }

        private static MatcherPipelineResult Success(string stage, MatchResult final, MatchResult ncc, MatchResult ecc, bool nccOnly, bool expectedEcc)
        {
            if (final.Diagnostics != null)
            {
                final.Diagnostics["DirectPipelineStage"] = stage;
                if (ncc != null && !double.IsNaN(ncc.RawScore)) final.Diagnostics["NccScore"] = ncc.RawScore.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (ecc != null && !double.IsNaN(ecc.RawScore)) final.Diagnostics["EccCorrelation"] = ecc.RawScore.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            return new MatcherPipelineResult { Success = true, SelectedStage = stage, FinalResult = final, NccResult = ncc, EccResult = ecc, UsedNccOnlyFallback = nccOnly, UsedExpectedEccInitialization = expectedEcc };
        }

        private static MatcherPipelineResult Failed(string reason, MatchResult ncc, MatchResult ecc)
        {
            return new MatcherPipelineResult { Success = false, SelectedStage = "DIRECT_REJECTED", NccResult = ncc, EccResult = ecc, FailureReason = reason, FinalResult = ecc ?? ncc };
        }

        private static MatchRequest CloneRequest(MatchRequest source, Transform2D initial)
        {
            return new MatchRequest
            {
                ReferenceImage = source.ReferenceImage,
                MovingImage = source.MovingImage,
                ReferenceMask = source.ReferenceMask,
                MovingMask = source.MovingMask,
                ReferenceRoi = source.ReferenceRoi,
                MovingRoi = source.MovingRoi,
                InitialMovingToReferenceTransform = initial,
                Options = source.Options,
                Purpose = source.Purpose,
                OrderIndex = source.OrderIndex,
                SampleTileId = source.SampleTileId,
                Context = source.Context
            };
        }
    }
}
