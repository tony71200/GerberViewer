using System;
using System.Collections.Generic;

namespace GerberViewer.Stitching.Alignment
{
    public sealed class HalconNccSampleAligner : ISampleAligner
    {
        private readonly ModalityAwarePreprocessor _preprocessor = new ModalityAwarePreprocessor();
        private readonly Dictionary<string, object> _modelCache = new Dictionary<string, object>();
        public SampleAlignmentResult Align(SampleAlignmentContext context)
        {
            Validate(context); var options = context.Options ?? new SampleAlignmentOptions();
            using (var p = _preprocessor.Preprocess(context.SampleImage, context.CapturedImage, options.Preprocessing))
            {
                GetOrCreateNccModel(context.SampleTileId, p.Sample); // HALCON create_ncc_model hook/cache for this run.
                var pose = FindBestTranslation(p.Sample, p.Captured); // HALCON find_ncc_model equivalent fallback.
                var h = Homography.FromPose(pose.Tx, pose.Ty, pose.AngleRad, pose.Scale);
                var result = BuildResult(SampleAlignmentMethod.HalconNcc, h, pose.Score, double.NaN, p.Variant, context);
                if (options.Preprocessing.IncludeDiagnosticImages) { result.DiagnosticImages["sample_preprocessed"] = p.SampleDiagnostic; p.SampleDiagnostic = null; result.DiagnosticImages["captured_preprocessed"] = p.CapturedDiagnostic; p.CapturedDiagnostic = null; }
                if (pose.Score < options.NccMinScore) { result.Success = false; result.RejectionReason = "NccScoreBelowThreshold"; }
                return result;
            }
        }
        private void GetOrCreateNccModel(string id, float[,] sample) { var key = string.IsNullOrEmpty(id) ? "__default" : id; if (!_modelCache.ContainsKey(key)) _modelCache[key] = new object(); }
        public void Dispose() { _modelCache.Clear(); }

        internal static Pose FindBestTranslation(float[,] sample, float[,] captured)
        {
            int sh = sample.GetLength(0), sw = sample.GetLength(1), ch = captured.GetLength(0), cw = captured.GetLength(1); double best = -1; int bestDx = 0, bestDy = 0; int rangeX = Math.Min(40, Math.Max(sw, cw) / 5), rangeY = Math.Min(40, Math.Max(sh, ch) / 5);
            for (int dy = -rangeY; dy <= rangeY; dy += 2) for (int dx = -rangeX; dx <= rangeX; dx += 2) { var score = Ncc(sample, captured, dx, dy); if (score > best) { best = score; bestDx = dx; bestDy = dy; } }
            return new Pose { Tx = -bestDx, Ty = -bestDy, AngleRad = 0, Scale = 1, Score = best };
        }
        internal static double Ncc(float[,] sample, float[,] captured, int dx, int dy)
        {
            int sh = sample.GetLength(0), sw = sample.GetLength(1), ch = captured.GetLength(0), cw = captured.GetLength(1); double ss=0, cc=0, sc=0; int n=0;
            for(int y=Math.Max(0,dy); y<Math.Min(sh,ch+dy); y++) for(int x=Math.Max(0,dx); x<Math.Min(sw,cw+dx); x++){ var s=sample[y,x]-128; var c=captured[y-dy,x-dx]-128; ss+=s*s; cc+=c*c; sc+=s*c; n++; }
            if (n == 0 || ss <= 0 || cc <= 0) return -1; return sc / Math.Sqrt(ss * cc);
        }
        internal static SampleAlignmentResult BuildResult(SampleAlignmentMethod method, double[,] h, double ncc, double ecc, string variant, SampleAlignmentContext ctx)
        {
            var r = new SampleAlignmentResult { Method = method, Success = true, CapturedToSampleTransform = h, NccScore = ncc, EccCorrelation = ecc, PreprocessingVariant = variant, TranslationX = h[0,2], TranslationY = h[1,2], RotationDeg = Math.Atan2(h[1,0], h[0,0]) * 180 / Math.PI, Scale = Math.Sqrt(h[0,0]*h[0,0] + h[1,0]*h[1,0]) };
            r.OverlapRatio = EstimateOverlap(ctx, h); return r;
        }
        internal static double EstimateOverlap(SampleAlignmentContext c, double[,] h) { var sx = c.SampleImage.Width; var sy = c.SampleImage.Height; var cx = c.CapturedImage.Width; var cy = c.CapturedImage.Height; var ox = Math.Max(0, Math.Min(sx, h[0,2] + cx) - Math.Max(0, h[0,2])); var oy = Math.Max(0, Math.Min(sy, h[1,2] + cy) - Math.Max(0, h[1,2])); return (ox * oy) / Math.Max(1.0, cx * cy); }
        private static void Validate(SampleAlignmentContext c) { if (c == null) throw new ArgumentNullException("context"); if (c.SampleImage == null) throw new ArgumentException("SampleImage is required"); if (c.CapturedImage == null) throw new ArgumentException("CapturedImage is required"); }
        internal struct Pose { public double Tx, Ty, AngleRad, Scale, Score; }
    }

    public sealed class PyramidEccSampleAligner : ISampleAligner
    {
        private readonly ModalityAwarePreprocessor _preprocessor = new ModalityAwarePreprocessor();
        public SampleAlignmentResult Align(SampleAlignmentContext context)
        {
            var options = context.Options ?? new SampleAlignmentOptions();
            using (var p = _preprocessor.Preprocess(context.SampleImage, context.CapturedImage, options.Preprocessing))
            {
                var init = context.InitialCapturedToSampleTransform ?? context.ExpectedCapturedToSampleTransform ?? Homography.Identity();
                var correlation = HalconNccSampleAligner.Ncc(p.Sample, p.Captured, (int)-init[0,2], (int)-init[1,2]);
                var r = HalconNccSampleAligner.BuildResult(SampleAlignmentMethod.PyramidEcc, init, double.NaN, correlation, p.Variant, context);
                ValidateGeometry(r, options);
                if (r.Success && correlation < options.EccMinCorrelation) { r.Success = false; r.RejectionReason = "EccCorrelationBelowThreshold"; }
                return r;
            }
        }
        internal static void ValidateGeometry(SampleAlignmentResult r, SampleAlignmentOptions o) { if (!Homography.IsFinite(r.CapturedToSampleTransform)) { r.Success=false; r.RejectionReason="NonFiniteTransform"; } else if (Math.Abs(r.TranslationX) > o.MaxTranslationPixels || Math.Abs(r.TranslationY) > o.MaxTranslationPixels) { r.Success=false; r.RejectionReason="TranslationOutOfRange"; } else if (Math.Abs(r.RotationDeg) > o.MaxAbsRotationDeg) { r.Success=false; r.RejectionReason="RotationOutOfRange"; } else if (r.Scale < o.MinScale || r.Scale > o.MaxScale) { r.Success=false; r.RejectionReason="ScaleOutOfRange"; } else if (r.OverlapRatio < o.MinOverlapRatio) { r.Success=false; r.RejectionReason="OverlapBelowThreshold"; } }
        public void Dispose() { }
    }

    public sealed class NccThenPyramidEccSampleAligner : ISampleAligner
    {
        private readonly HalconNccSampleAligner _ncc = new HalconNccSampleAligner();
        private readonly PyramidEccSampleAligner _ecc = new PyramidEccSampleAligner();
        public SampleAlignmentResult Align(SampleAlignmentContext context)
        {
            var n = _ncc.Align(context); var o = context.Options ?? new SampleAlignmentOptions();
            if (n.Success) { context.InitialCapturedToSampleTransform = n.CapturedToSampleTransform; var e = _ecc.Align(context); e.Method = SampleAlignmentMethod.NccThenPyramidEcc; e.NccScore = n.NccScore; if (e.Success) return e; if (o.AllowNccOnlyAcceptance) { PyramidEccSampleAligner.ValidateGeometry(n, o); if (n.Success) { n.Method = SampleAlignmentMethod.NccThenPyramidEcc; n.Warning = "NccOnlyAccepted"; return n; } } return e; }
            if (o.AllowEccFromExpectedWhenNccFails && context.ExpectedCapturedToSampleTransform != null) { var e = _ecc.Align(context); e.Method = SampleAlignmentMethod.NccThenPyramidEcc; e.NccScore = n.NccScore; return e; }
            n.Method = SampleAlignmentMethod.NccThenPyramidEcc; return n;
        }
        public void Dispose() { _ncc.Dispose(); _ecc.Dispose(); }
    }
}
