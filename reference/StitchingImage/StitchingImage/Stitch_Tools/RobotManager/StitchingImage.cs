using OpenCvSharp;
using StitchingImage.Stitch_Tools.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HalconDotNet;
using System.Xml.Linq;
using System.Runtime.InteropServices;

namespace StitchingImage.Stitch_Tools.RobotManager
{
    public sealed class StitchingImageResult
    {
        public double StitchingMs { get; set; }
        public double SaveMs { get; set; }
    }

    public sealed class StitchingImage
    {
        private const double FullMegapix = 1_000_000.0;

        public SaveMode SaveMode { get; set; } = SaveMode.Preview;
        public double ComposeMegapix { get; set; } = 10.0;
        #region Stitching Image
        public StitchingImageResult Stitch(TraversalComponent comp, IReadOnlyList<EdgeTransform> transforms, StitchRunConfig cfg, string outPath)
        {
            ValidateTraversalComponent(comp);
            if (transforms == null) throw new ArgumentNullException(nameof(transforms));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (string.IsNullOrWhiteSpace(outPath)) throw new ArgumentNullException(nameof(outPath));

            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? ".");

            var swStitch = Stopwatch.StartNew();

            var nodes = comp.Points.ToDictionary(p => p.ImageId);
            var sizesFull = nodes.ToDictionary(kv => kv.Key, kv => EstimateOriginalSize(kv.Value.FilePath));
            var rootId = TraversalGraph.GuessRootId(comp.Graph) ?? nodes.Keys.Min();
            if (!nodes.ContainsKey(rootId)) rootId = nodes.Keys.Min();

            // adjacency from AId -> list of (BId,dir)
            var adj = TraversalGraph.EnumerateEdges(comp.Graph)
                .GroupBy(e => e.AId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var tfByEdge = transforms.ToDictionary(t => (t.AId, t.BId), t => t);

            // PoseFull[id] = H_img_to_root (3x3 CV_64F)
            var poseFull = new Dictionary<int, Mat> { [rootId] = Eye3x3() };
            var q = new Queue<int>();
            q.Enqueue(rootId);

            // Caches (python v8): meanVertical + meanHorizontal(by y_robot//1000)
            var verticalSamples = new List<(double Tx, double Ty)>();
            var horizontalBuckets = new Dictionary<int, List<(double Tx, double Ty)>>();
            double prevGoodTheta = 0.0;

            while (q.Count > 0)
            {
                var aId = q.Dequeue();
                var aInfo = nodes[aId];

                if (!adj.TryGetValue(aId, out var outs)) continue;

                var Ha = poseFull[aId];
                var thetaA = ThetaFromH(Ha);
                var thetaUse = (Math.Abs(thetaA) > 1e-9) ? thetaA : prevGoodTheta;

                foreach (var e in outs)
                {
                    var bId = e.BId;
                    if (poseFull.ContainsKey(bId)) continue;

                    var bInfo = nodes[bId];
                    var dx = bInfo.XRobot - aInfo.XRobot;
                    var dy = bInfo.YRobot - aInfo.YRobot;

                    Mat HBToA = null;

                    if (tfByEdge.TryGetValue((aId, bId), out var t) && t.Ok)
                    {
                        prevGoodTheta = t.ThetaRad;

                        // store tx/ty samples for cache mean (python)
                        if (e.Direction == EdgeDir.Vertical)
                        {
                            verticalSamples.Add((t.Tx, t.Ty));
                        }
                        else
                        {
                            var key = (int)Math.Floor(aInfo.YRobot / 1000.0);
                            if (!horizontalBuckets.TryGetValue(key, out var bucket))
                                horizontalBuckets[key] = bucket = new List<(double Tx, double Ty)>();
                            bucket.Add((t.Tx, t.Ty));
                        }
                        if (cfg.UseRigidForGlobal && t.MRigidBToA != null && !t.MRigidBToA.Empty())
                            HBToA = ToHomography(t.MRigidBToA);
                        else
                            HBToA = t.HBToA_Full.Clone();
                    }
                    else
                    {
                        // try cached mean first (python v8)
                        if (!TryGetCachedFallback(aInfo, e.Direction, thetaUse, verticalSamples, horizontalBuckets, out HBToA, out var src))
                        {
                           var aSize = EstimateOriginalSize(aInfo.FilePath);
                           HBToA = FallbackBToA(e.Direction, aSize, thetaUse, dx, dy, cfg);
                           src = "cfg_fallback";
                        }

                        if (tfByEdge.TryGetValue((aId, bId), out var t2))
                        {
                           t2.UsedFallback = true;
                           t2.Reason = src;
                        }

                        var fbTx = HBToA.At<double>(0, 2);
                        var fbTy = HBToA.At<double>(1, 2);
                        Logger.Info($"[MATCHING FAILS: {aId}->{bId}] use {src} tx/ty={fbTx:0.###}/{fbTy:0.###} theta={thetaUse:0.####}");
                    }

                    poseFull[bId] = (Ha * HBToA).ToMat();
                    HBToA.Dispose();

                    q.Enqueue(bId);
                }
            }

            var composeTargetMp = SaveMode == SaveMode.Full ? FullMegapix : ComposeMegapix;
            var maxCanvasMp = SaveMode == SaveMode.Full ? FullMegapix : cfg.MaxCanvasMegapix;

            // Compose scale
            var rootSize = sizesFull[rootId];
            var composeScale = TargetMegapixScale(rootSize, composeTargetMp);
            composeScale = Clamp01(composeScale);

            // Scale poses: H' = S * H * S^{-1}
            using (var S = ScaleMat(composeScale))
            using (var SInv = ScaleMat(1.0 / composeScale))
            {
                var pose = new Dictionary<int, Mat>(poseFull.Count);
                foreach (var kv in poseFull)
                {
                    using (var tmp = (S * kv.Value).ToMat())
                    {
                        pose[kv.Key] = (tmp * SInv).ToMat();
                    }
                }

                // Canvas bounds
                double minx = double.PositiveInfinity, miny = double.PositiveInfinity;
                double maxx = double.NegativeInfinity, maxy = double.NegativeInfinity;

                foreach (var kv in pose)
                {
                    var id = kv.Key;
                    var sizeFull = sizesFull[id];

                    var w = sizeFull.Width * composeScale;
                    var h = sizeFull.Height * composeScale;

                    var corners = new[]
                    {
                        new Point2d(0,0),
                        new Point2d(w,0),
                        new Point2d(w,h),
                        new Point2d(0,h),
                    };

                    foreach (var c in corners)
                    {
                        var p = ApplyH(kv.Value, c);
                        if (p.X < minx) minx = p.X;
                        if (p.Y < miny) miny = p.Y;
                        if (p.X > maxx) maxx = p.X;
                        if (p.Y > maxy) maxy = p.Y;
                    }
                }
                if (!IsFinite(minx) || !IsFinite(miny) || !IsFinite(maxx) || !IsFinite(maxy))
                    throw new InvalidOperationException("Invalid canvas bounds.");

                var canvasW0 = (int)Math.Ceiling(maxx - minx);
                var canvasH0 = (int)Math.Ceiling(maxy - miny);

                var scaleCanvas = TargetMegapixScale(new Size(canvasW0, canvasH0), maxCanvasMp);
                scaleCanvas = Clamp01(scaleCanvas);

                var canvasW = Math.Max(1, (int)Math.Round(canvasW0 * scaleCanvas));
                var canvasH = Math.Max(1, (int)Math.Round(canvasH0 * scaleCanvas));

                using (var T = Eye3x3())
                using (var Sc = ScaleMat(scaleCanvas))
                using (var canvas = new Mat(canvasH, canvasW, MatType.CV_8UC3, Scalar.All(0)))
                {
                    T.Set<double>(0, 2, -minx);
                    T.Set<double>(1, 2, -miny);

                    var readScale = composeScale;

                    foreach (var kv in pose)
                    {
                        var id = kv.Key;
                        var path = nodes[id].FilePath;

                        using (var img = ReadForScale(path, sizesFull[id], readScale))
                        using (var Ht = (T * kv.Value).ToMat())
                        using (var Hc = (Sc * Ht).ToMat())
                        using (var warped = new Mat())
                        using (var mask = new Mat(img.Rows, img.Cols, MatType.CV_8UC1, Scalar.All(255)))
                        using (var maskWarped = new Mat())
                        {
                            Cv2.WarpPerspective(img, warped, Hc, new Size(canvasW, canvasH),
                                InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(0));

                            Cv2.WarpPerspective(mask, maskWarped, Hc, new Size(canvasW, canvasH),
                                InterpolationFlags.Nearest, BorderTypes.Constant, Scalar.All(0));

                            warped.CopyTo(canvas, maskWarped);
                        }
                    }

                    swStitch.Stop();

                    var swSave = Stopwatch.StartNew();
                    SaveCanvas(outPath, canvas);
                    swSave.Stop();

                    foreach (var m in pose.Values) m.Dispose();
                    foreach (var m in poseFull.Values) m.Dispose();

                    return new StitchingImageResult
                    {
                        StitchingMs = swStitch.Elapsed.TotalMilliseconds,
                        SaveMs = swSave.Elapsed.TotalMilliseconds
                    };
                }
            }
        }

        /// <summary>
        /// Stitch using HALCON gen_projective_mosaic, with the same input/output contract as <see cref="Stitch"/>.
        /// Requires a reference to HalconDotNet.
        /// </summary>
        public StitchingImageResult StitchHalcon(TraversalComponent comp, IReadOnlyList<EdgeTransform> transforms, StitchRunConfig cfg, string outPath)
        {
            ValidateTraversalComponent(comp);
            if (transforms == null) throw new ArgumentNullException(nameof(transforms));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (string.IsNullOrWhiteSpace(outPath)) throw new ArgumentNullException(nameof(outPath));

            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? ".");
            StitchingImageResult result = null;

            
            
            var swStitch = Stopwatch.StartNew();

            var nodes = comp.Points.ToDictionary(p => p.ImageId);
            var sizesFull = nodes.ToDictionary(kv => kv.Key, kv => EstimateOriginalSize(kv.Value.FilePath));
            var rootId = TraversalGraph.GuessRootId(comp.Graph) ?? nodes.Keys.Min();
            if (!nodes.ContainsKey(rootId)) rootId = nodes.Keys.Min();

            var imageIdsInOrder = comp.Points.Select(p => p.ImageId).ToList();
            var idxById = imageIdsInOrder
                .Select((id, i) => (id, idx1: i + 1))
                .ToDictionary(x => x.id, x => x.idx1);

            var startImage = idxById[rootId];

            var adj = TraversalGraph.EnumerateEdges(comp.Graph)
                .GroupBy(e => e.AId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var tfByEdge = transforms.ToDictionary(t => (t.AId, t.BId), t => t);

            var poseFull = new Dictionary<int, Mat> { [rootId] = Eye3x3() };
            var q = new Queue<int>();
            q.Enqueue(rootId);
            var verticalSamples = new List<(double Tx, double Ty)>();
            var horizontalBuckets = new Dictionary<int, List<(double Tx, double Ty)>>();
            double prevGoodTheta = 0.0;

            var usedEdgeMatricesFull = new List<(int SrcIdx1, int DstIdx1, Mat HBToA_Full_Cv)>();

            while (q.Count > 0)
            {
                var aId = q.Dequeue();
                var aInfo = nodes[aId];

                if (!adj.TryGetValue(aId, out var outs)) continue;

                var Ha = poseFull[aId];
                var thetaA = ThetaFromH(Ha);
                var thetaUse = (Math.Abs(thetaA) > 1e-9) ? thetaA : prevGoodTheta;

                foreach (var e in outs)
                {
                    var bId = e.BId;
                    if (poseFull.ContainsKey(bId)) continue;

                    var bInfo = nodes[bId];
                    var dx = bInfo.XRobot - aInfo.XRobot;
                    var dy = bInfo.YRobot - aInfo.YRobot;

                    Mat HBToA = null;

                    if (tfByEdge.TryGetValue((aId, bId), out var t) && t.Ok)
                    {
                        prevGoodTheta = t.ThetaRad;

                        if (e.Direction == EdgeDir.Vertical)
                        {
                            verticalSamples.Add((t.Tx, t.Ty));
                        }
                        else
                        {
                            var key = (int)Math.Floor(aInfo.YRobot / 1000.0);
                            if (!horizontalBuckets.TryGetValue(key, out var bucket))
                                horizontalBuckets[key] = bucket = new List<(double Tx, double Ty)>();
                            bucket.Add((t.Tx, t.Ty));
                        }

                        if (cfg.UseRigidForGlobal && t.MRigidBToA != null && !t.MRigidBToA.Empty())
                            HBToA = ToHomography(t.MRigidBToA);
                        else
                            HBToA = t.HBToA_Full.Clone();
                    }
                    else
                    {
                        if (!TryGetCachedFallback(aInfo, e.Direction, thetaUse, verticalSamples, horizontalBuckets, out HBToA, out var src))
                        {
                           var aSize = EstimateOriginalSize(aInfo.FilePath);
                           HBToA = FallbackBToA(e.Direction, aSize, thetaUse, dx, dy, cfg);
                           src = "cfg_fallback";
                        }

                        if (tfByEdge.TryGetValue((aId, bId), out var t2))
                        {
                           t2.UsedFallback = true;
                           t2.Reason = src;
                        }

                        var fbTx = HBToA.At<double>(0, 2);
                        var fbTy = HBToA.At<double>(1, 2);
                        Logger.Info($"[MATCHING FAILS: {aId}->{bId}] use {src} tx/ty={fbTx:0.###}/{fbTy:0.###} theta={thetaUse:0.####}");
                    }
                    Logger.Info($"[CHECK] {HBToA.At<double>(0, 2)} | {HBToA.At<double>(1, 2)}");
                    poseFull[bId] = (Ha * HBToA).ToMat();

                    usedEdgeMatricesFull.Add((
                        SrcIdx1: idxById[bId],
                        DstIdx1: idxById[aId],
                        HBToA_Full_Cv: HBToA.Clone()
                    ));

                    HBToA.Dispose();
                    q.Enqueue(bId);
                }
            }

            if (usedEdgeMatricesFull.Count == 0)
            {
                Logger.Error("\"No usable edge transforms found for gen_projective_mosaic.");
                throw new InvalidOperationException("No usable edge transforms found for gen_projective_mosaic.");
            }

            var composeTargetMp = SaveMode == SaveMode.Full ? FullMegapix : ComposeMegapix;
            var maxCanvasMp = SaveMode == SaveMode.Full ? FullMegapix : cfg.MaxCanvasMegapix;

            var rootSize = sizesFull[rootId];
            var composeScale = TargetMegapixScale(rootSize, composeTargetMp);
            composeScale = Clamp01(composeScale);

            HObject images = null;
            HObject mosaic = null;

            try
            {
                HOperatorSet.GenEmptyObj(out images);
                // 2) concat images exactly in imageIdsInOrder
                foreach (var id in imageIdsInOrder)
                {
                    var path = nodes[id].FilePath;

                    HObject img;
                    HOperatorSet.ReadImage(out img, path);

                    if (composeScale < 0.999999)
                    {
                        HObject imgScaled;
                        HOperatorSet.ZoomImageFactor(img, out imgScaled, composeScale, composeScale, "bilinear");
                        img.Dispose();
                        img = imgScaled;
                    }

                    HObject tmp;
                    HOperatorSet.ConcatObj(images, img, out tmp);
                    images.Dispose();
                    images = tmp;

                    img.Dispose();
                }

                using (var S = ScaleMat(composeScale))
                using (var SInv = ScaleMat(1.0 / composeScale))
                {
                    HTuple mappingSource = new HTuple();
                    HTuple mappingDest = new HTuple();
                    HTuple homMatrices2D = new HTuple();

                    foreach (var e in usedEdgeMatricesFull)
                    {
                        mappingSource = mappingSource.TupleConcat(e.SrcIdx1);
                        mappingDest = mappingDest.TupleConcat(e.DstIdx1);

                        using (var HscaledCv = ScaleHomographyCv(e.HBToA_Full_Cv, S, SInv))
                        {
                            var hHalcon9 = ToHalconProjective9RowMajorFromCv(HscaledCv);
                            homMatrices2D = homMatrices2D.TupleConcat(new HTuple(hHalcon9));
                        }
                    }
                    HOperatorSet.GenProjectiveMosaic(
                        images,
                        out mosaic,
                        new HTuple(startImage),
                        mappingSource,
                        mappingDest,
                        homMatrices2D,
                        new HTuple("default"),
                        new HTuple("false"),
                        out _);
                }
                swStitch.Stop();

                HOperatorSet.GetImageSize(mosaic, out HTuple wT, out HTuple hT);
                var mosaicW = (int)wT.D;
                var mosaicH = (int)hT.D;

                var scaleCanvas = TargetMegapixScale(new Size(mosaicW, mosaicH), maxCanvasMp);
                scaleCanvas = Clamp01(scaleCanvas);

                if (scaleCanvas < 0.999999)
                {
                    HObject mosaicScaled;
                    HOperatorSet.ZoomImageFactor(mosaic, out mosaicScaled, scaleCanvas, scaleCanvas, "bilinear");
                    mosaic.Dispose();
                    mosaic = mosaicScaled;
                }

                var swSave = Stopwatch.StartNew();
                WriteHalconImage(outPath, mosaic);
                swSave.Stop();
                result =  new StitchingImageResult
                {
                    StitchingMs = swStitch.Elapsed.TotalMilliseconds,
                    SaveMs = swSave.Elapsed.TotalMilliseconds
                };
            }
            catch (Exception e)
            {
                // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                // Auto fallback to incremental HALCON stitching when gen_projective_mosaic fails because of memory pressure.
                var isOutOfMemory = e.Message != null &&
                                    (e.Message.IndexOf("6001", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     e.Message.IndexOf("Not enough memory", StringComparison.OrdinalIgnoreCase) >= 0);
                if (isOutOfMemory)
                {
                    Logger.Warning($"[{nameof(StitchHalcon)}] Memory limit reached, fallback to {nameof(StitchHalcon2)}.");
                    return StitchHalcon2(comp, transforms, cfg, outPath);
                }
                Logger.Error($"Error {nameof(StitchHalcon)}", e);
            }
            finally
            {
                if (mosaic != null) mosaic.Dispose();
                if (images != null) images.Dispose();
                foreach (var kv in poseFull.Values) kv.Dispose();
                foreach (var e in usedEdgeMatricesFull) e.HBToA_Full_Cv.Dispose();
            }
            return result;
        }

        /// <summary>
        /// [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
        /// Memory-optimized HALCON stitch path: compute all transforms first, then stitch incrementally one image at a time.
        /// </summary>
        public StitchingImageResult StitchHalcon2(TraversalComponent comp, IReadOnlyList<EdgeTransform> transforms, StitchRunConfig cfg, string outPath)
        {
            ValidateTraversalComponent(comp);
            if (transforms == null) throw new ArgumentNullException(nameof(transforms));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (string.IsNullOrWhiteSpace(outPath)) throw new ArgumentNullException(nameof(outPath));

            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? ".");

            var swStitch = Stopwatch.StartNew();
            var nodes = comp.Points.ToDictionary(p => p.ImageId);
            var sizesFull = nodes.ToDictionary(kv => kv.Key, kv => EstimateOriginalSize(kv.Value.FilePath));
            var rootId = TraversalGraph.GuessRootId(comp.Graph) ?? nodes.Keys.Min();
            if (!nodes.ContainsKey(rootId)) rootId = nodes.Keys.Min();

            var imageIdsInOrder = comp.Points.Select(p => p.ImageId).ToList();
            var idxById = imageIdsInOrder
                .Select((id, i) => (id, idx1: i + 1))
                .ToDictionary(x => x.id, x => x.idx1);

            var adj = TraversalGraph.EnumerateEdges(comp.Graph)
                .GroupBy(e => e.AId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var tfByEdge = transforms.ToDictionary(t => (t.AId, t.BId), t => t);
            var poseFull = new Dictionary<int, Mat> { [rootId] = Eye3x3() };
            var q = new Queue<int>();
            q.Enqueue(rootId);
            var verticalSamples = new List<(double Tx, double Ty)>();
            var horizontalBuckets = new Dictionary<int, List<(double Tx, double Ty)>>();
            double prevGoodTheta = 0.0;

            while (q.Count > 0)
            {
                var aId = q.Dequeue();
                var aInfo = nodes[aId];

                if (!adj.TryGetValue(aId, out var outs)) continue;

                var Ha = poseFull[aId];
                var thetaA = ThetaFromH(Ha);
                var thetaUse = (Math.Abs(thetaA) > 1e-9) ? thetaA : prevGoodTheta;

                foreach (var e in outs)
                {
                    var bId = e.BId;
                    if (poseFull.ContainsKey(bId)) continue;

                    var bInfo = nodes[bId];
                    var dx = bInfo.XRobot - aInfo.XRobot;
                    var dy = bInfo.YRobot - aInfo.YRobot;

                    Mat HBToA = null;

                    if (tfByEdge.TryGetValue((aId, bId), out var t) && t.Ok)
                    {
                        prevGoodTheta = t.ThetaRad;
                        if (e.Direction == EdgeDir.Vertical)
                        {
                            verticalSamples.Add((t.Tx, t.Ty));
                        }
                        else
                        {
                            var key = (int)Math.Floor(aInfo.YRobot / 1000.0);
                            if (!horizontalBuckets.TryGetValue(key, out var bucket))
                                horizontalBuckets[key] = bucket = new List<(double Tx, double Ty)>();
                            bucket.Add((t.Tx, t.Ty));
                        }

                        if (cfg.UseRigidForGlobal && t.MRigidBToA != null && !t.MRigidBToA.Empty())
                            HBToA = ToHomography(t.MRigidBToA);
                        else
                            HBToA = t.HBToA_Full.Clone();
                    }
                    else
                    {
                        if (!TryGetCachedFallback(aInfo, e.Direction, thetaUse, verticalSamples, horizontalBuckets, out HBToA, out var src))
                        {
                            var aSize = EstimateOriginalSize(aInfo.FilePath);
                            HBToA = FallbackBToA(e.Direction, aSize, thetaUse, dx, dy, cfg);
                            src = "cfg_fallback";
                        }

                        if (tfByEdge.TryGetValue((aId, bId), out var t2))
                        {
                            t2.UsedFallback = true;
                            t2.Reason = src;
                        }
                    }

                    poseFull[bId] = (Ha * HBToA).ToMat();
                    HBToA.Dispose();
                    q.Enqueue(bId);
                }
            }

            var composeTargetMp = SaveMode == SaveMode.Full ? FullMegapix : ComposeMegapix;
            var maxCanvasMp = SaveMode == SaveMode.Full ? FullMegapix : cfg.MaxCanvasMegapix;
            var rootSize = sizesFull[rootId];
            var composeScale = Clamp01(TargetMegapixScale(rootSize, composeTargetMp));

            HObject mosaic = null;
            try
            {
                HOperatorSet.ReadImage(out mosaic, nodes[rootId].FilePath);
                if (composeScale < 0.999999)
                {
                    HObject scaledRoot;
                    HOperatorSet.ZoomImageFactor(mosaic, out scaledRoot, composeScale, composeScale, "bilinear");
                    mosaic.Dispose();
                    mosaic = scaledRoot;
                }

                foreach (var id in imageIdsInOrder.Where(x => x != rootId))
                {
                    if (!poseFull.TryGetValue(id, out var poseBToRoot))
                        continue;

                    using (var S = ScaleMat(composeScale))
                    using (var SInv = ScaleMat(1.0 / composeScale))
                    using (var hScaledCv = ScaleHomographyCv(poseBToRoot, S, SInv))
                    {
                        var hHalcon9 = ToHalconProjective9RowMajorFromCv(hScaledCv);
                        HObject img = null;
                        HObject imgScaled = null;
                        HObject pair = null;
                        HObject newMosaic = null;
                        try
                        {
                            HOperatorSet.ReadImage(out img, nodes[id].FilePath);
                            if (composeScale < 0.999999)
                            {
                                HOperatorSet.ZoomImageFactor(img, out imgScaled, composeScale, composeScale, "bilinear");
                                img.Dispose();
                                img = imgScaled;
                                // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                                // Avoid double dispose: img now owns the scaled handle.
                                // imgScaled = null;
                                imgScaled = null;
                            }

                            HOperatorSet.ConcatObj(mosaic, img, out pair);
                            HOperatorSet.GenProjectiveMosaic(
                                pair,
                                out newMosaic,
                                new HTuple(1),
                                new HTuple(2),
                                new HTuple(1),
                                new HTuple(new HTuple(hHalcon9)),
                                new HTuple("default"),
                                new HTuple("false"),
                                out _);

                            mosaic.Dispose();
                            mosaic = newMosaic;
                            newMosaic = null;
                        }
                        finally
                        {
                            pair?.Dispose();
                            // [GPT-5.3-Codex] [Change time: 260414] [Purpose of change]
                            // img and imgScaled can alias same HALCON handle after scaling; dispose once via img.
                            // img?.Dispose();
                            // imgScaled?.Dispose();
                            img?.Dispose();
                            newMosaic?.Dispose();
                        }
                    }
                }

                swStitch.Stop();

                HOperatorSet.GetImageSize(mosaic, out HTuple wT, out HTuple hT);
                var mosaicW = (int)wT.D;
                var mosaicH = (int)hT.D;
                var scaleCanvas = Clamp01(TargetMegapixScale(new Size(mosaicW, mosaicH), maxCanvasMp));
                if (scaleCanvas < 0.999999)
                {
                    HObject mosaicScaled;
                    HOperatorSet.ZoomImageFactor(mosaic, out mosaicScaled, scaleCanvas, scaleCanvas, "bilinear");
                    mosaic.Dispose();
                    mosaic = mosaicScaled;
                }

                var swSave = Stopwatch.StartNew();
                WriteHalconImage(outPath, mosaic);
                swSave.Stop();

                return new StitchingImageResult
                {
                    StitchingMs = swStitch.Elapsed.TotalMilliseconds,
                    SaveMs = swSave.Elapsed.TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Error {nameof(StitchHalcon2)}", ex);
                return null;
            }
            finally
            {
                mosaic?.Dispose();
                foreach (var kv in poseFull.Values) kv.Dispose();
            }
        }
        #endregion

        #region Helper
        private void SaveCanvas(string path, Mat canvas)
        {
            if (canvas == null || canvas.Empty())
                throw new InvalidOperationException("Canvas is empty.");

            if (SaveMode == SaveMode.Full)
            {
                using (var prepared = PrepareCanvasForBigTiff(canvas, out var isGray))
                {
                    var width = prepared.Cols;
                    var height = prepared.Rows;
                    var channels = prepared.Channels();
                    var bytes = ExtractBytes(prepared, width * height * channels);

                    if (isGray)
                        TiffBigWriter.SaveBigTiffGray8Async(path, width, height, bytes).GetAwaiter().GetResult();
                    else
                        TiffBigWriter.SaveBigTiffRgb24Async(path, width, height, bytes).GetAwaiter().GetResult();
                }
                return;
            }

            if (!Cv2.ImWrite(path, canvas))
                throw new IOException($"Failed to write image: {path}");
        }

        private static Mat PrepareCanvasForBigTiff(Mat canvas, out bool isGray)
        {
            isGray = canvas.Channels() == 1;
            if (isGray)
            {
                if (canvas.Type() == MatType.CV_8UC1)
                    return canvas.Clone();
                var gray8 = new Mat();
                Cv2.ConvertScaleAbs(canvas, gray8);
                return gray8;
            }

            Mat bgr8;
            if (canvas.Channels() == 4)
            {
                bgr8 = new Mat();
                Cv2.CvtColor(canvas, bgr8, ColorConversionCodes.BGRA2BGR);
            }
            else if (canvas.Type() == MatType.CV_8UC3)
            {
                bgr8 = canvas.Clone();
            }
            else
            {
                bgr8 = new Mat();
                Cv2.ConvertScaleAbs(canvas, bgr8);
                if (bgr8.Channels() != 3)
                {
                    var converted = new Mat();
                    Cv2.CvtColor(bgr8, converted, ColorConversionCodes.GRAY2BGR);
                    bgr8.Dispose();
                    bgr8 = converted;
                }
            }

            var rgb = new Mat();
            Cv2.CvtColor(bgr8, rgb, ColorConversionCodes.BGR2RGB);
            bgr8.Dispose();
            return rgb;
        }

        private static byte[] ExtractBytes(Mat mat, int expectedLength)
        {
            var source = mat.IsContinuous() ? mat : mat.Clone();
            try
            {
                var bytes = new byte[expectedLength];
                Marshal.Copy(source.Data, bytes, 0, bytes.Length);
                return bytes;
            }
            finally
            {
                if (!ReferenceEquals(source, mat))
                    source.Dispose();
            }
        }

        private static bool IsFinite(double v) => !(double.IsNaN(v) || double.IsInfinity(v));

        private static (double Tx, double Ty) MeanXY(List<(double Tx, double Ty)> samples)
        {
            if (samples == null || samples.Count == 0) return (0.0, 0.0);
            double sx = 0.0, sy = 0.0;
            for (int i = 0; i < samples.Count; i++)
            {
                sx += samples[i].Tx;
                sy += samples[i].Ty;
            }
            return (sx / samples.Count, sy / samples.Count);
        }

        /// <summary>
        /// Python v8 cache fallback:
        /// - Vertical: mean of all vertical samples
        /// - Horizontal: mean by bucket key = int(a.YRobot//1000), else global mean of all buckets
        /// Returns true if cache exists and sets HBToA (3x3).
        /// </summary>
        private static bool TryGetCachedFallback(
            ImageInfo a,
            EdgeDir direction,
            double thetaUse,
            List<(double Tx, double Ty)> verticalSamples,
            Dictionary<int, List<(double Tx, double Ty)>> horizontalBuckets,
            out Mat HBToA,
            out string source)
        {
            HBToA = null;
            source = "cache_empty";

            if (direction == EdgeDir.Vertical)
            {
                if (verticalSamples != null && verticalSamples.Count > 0)
                {
                    var (tx, ty) = MeanXY(verticalSamples);
                    using (var M = RigidFromThetaTxTy(thetaUse, tx, ty))
                        HBToA = ToHomography(M);
                    source = "cache_vertical_mean";
                    return true;
                }
                source = "cache_vertical_empty";
                return false;
            }

            var key = (int)Math.Floor(a.YRobot / 1000.0);
            if (horizontalBuckets != null && horizontalBuckets.TryGetValue(key, out var bucket) && bucket != null && bucket.Count > 0)
            {
                var (tx, ty) = MeanXY(bucket);
                using (var M = RigidFromThetaTxTy(thetaUse, tx, ty))
                    HBToA = ToHomography(M);
                source = $"cache_horizontal_mean[key={key}]";
                return true;
            }

            if (horizontalBuckets != null && horizontalBuckets.Count > 0)
            {
                var all = new List<(double Tx, double Ty)>();
                foreach (var v in horizontalBuckets.Values)
                {
                    if (v != null && v.Count > 0) all.AddRange(v);
                }
                if (all.Count > 0)
                {
                    var (tx, ty) = MeanXY(all);
                    using (var M = RigidFromThetaTxTy(thetaUse, tx, ty))
                        HBToA = ToHomography(M);
                    source = "cache_horizontal_global_mean";
                    return true;
                }
            }

            source = "cache_horizontal_empty";
            return false;
        }

        // [Codex] [Change time: 260323] [Make StitchingImage explicitly validate traversal-first runtime inputs before composing images]
        private static void ValidateTraversalComponent(TraversalComponent comp)
        {
            if (comp == null) throw new ArgumentNullException(nameof(comp));
            if (comp.Graph == null) throw new ArgumentException("TraversalComponent.Graph is null.");
            if (comp.Points == null || comp.Points.Length == 0) throw new ArgumentException("TraversalComponent.Points empty.");
        }

        private static Mat FallbackBToA(EdgeDir dir, Size aSize, double prevTheta, double dx, double dy, StitchRunConfig cfg)
        {
            var w = (double)aSize.Width;
            var h = (double)aSize.Height;

            double tx, ty;

            if (dir == EdgeDir.Horizontal)
            {
                var (dyOff, dxOff) = cfg.FallbackOffsetHorizontal;
                var stepX = w + dxOff;
                var stepY = dyOff;

                tx = (dx > 0) ? -stepX : stepX;
                ty = stepY;
            }
            else
            {
                var (dyOff, dxOff) = cfg.FallbackOffsetVertical;
                var stepX = dxOff;
                var stepY = h + dyOff;

                ty = (dy < 0) ? -stepY : stepY;
                tx = stepX;
            }

            using (var M = RigidFromThetaTxTy(prevTheta, tx, ty))
                return ToHomography(M);
        }

        private static Mat ReadForScale(string path, Size originalSize, double desiredScale)
        {
            desiredScale = Clamp01(desiredScale);

            var targetW = Math.Max(1, (int)Math.Round(originalSize.Width * desiredScale));
            var targetH = Math.Max(1, (int)Math.Round(originalSize.Height * desiredScale));

            var (mode, factor) = SelectReducedMode(desiredScale);

            using (var tmp = ImageRead.ReadImage(path, mode))
            {
                if (tmp.Empty()) throw new FileNotFoundException("Cannot read image", path);

                if (tmp.Cols == targetW && tmp.Rows == targetH)
                    return tmp.Clone();

                var dst = new Mat();
                Cv2.Resize(tmp, dst,
                    new Size(targetW, targetH),
                    0, 0, InterpolationFlags.Area);
                return dst;
            }
        }

        private static (ImreadModes mode, int reductionFactor) SelectReducedMode(double desiredScale)
        {
            if (desiredScale <= 0.125) return (ImreadModes.ReducedColor8, 8);
            if (desiredScale <= 0.25) return (ImreadModes.ReducedColor4, 4);
            if (desiredScale <= 0.5) return (ImreadModes.ReducedColor2, 2);
            return (ImreadModes.Color, 1);
        }

        private static Size EstimateOriginalSize(string path)
        {
            using (var m8 = ImageRead.ReadImage(path, ImreadModes.ReducedGrayscale8))
                if (!m8.Empty()) return new Size(m8.Cols * 8, m8.Rows * 8);
            using (var m4 = ImageRead.ReadImage(path, ImreadModes.ReducedGrayscale4))
                if (!m4.Empty()) return new Size(m4.Cols * 4, m4.Rows * 4);
            using (var m2 = ImageRead.ReadImage(path, ImreadModes.ReducedGrayscale2))
                if (!m2.Empty()) return new Size(m2.Cols * 2, m2.Rows * 2);
            using (var g = ImageRead.ReadImage(path, ImreadModes.Grayscale))
                if (!g.Empty()) return new Size(g.Cols, g.Rows);
            return new Size(0, 0);
        }

        private static double TargetMegapixScale(Size size, double targetMp)
        {
            var area = (double)size.Width * size.Height;
            var targetPix = Math.Max(1.0, targetMp * 1_000_000.0);
            var s = Math.Sqrt(targetPix / Math.Max(1.0, area));
            return Clamp01(s);
        }

        private static double Clamp01(double v) => Math.Max(1e-6, Math.Min(1.0, v));

        private static Mat Eye3x3()
        {
            var m = new Mat(3, 3, MatType.CV_64FC1);
            m.Set<double>(0, 0, 1); m.Set<double>(0, 1, 0); m.Set<double>(0, 2, 0);
            m.Set<double>(1, 0, 0); m.Set<double>(1, 1, 1); m.Set<double>(1, 2, 0);
            m.Set<double>(2, 0, 0); m.Set<double>(2, 1, 0); m.Set<double>(2, 2, 1);
            return m;
        }

        private static Mat ScaleMat(double s)
        {
            var m = Eye3x3();
            m.Set<double>(0, 0, s);
            m.Set<double>(1, 1, s);
            return m;
        }

        private static Point2d ApplyH(Mat H, Point2d p)
        {
            var x = p.X; var y = p.Y;
            var X = H.At<double>(0, 0) * x + H.At<double>(0, 1) * y + H.At<double>(0, 2);
            var Y = H.At<double>(1, 0) * x + H.At<double>(1, 1) * y + H.At<double>(1, 2);
            var Z = H.At<double>(2, 0) * x + H.At<double>(2, 1) * y + H.At<double>(2, 2);
            Z = Math.Max(1e-12, Z);
            return new Point2d(X / Z, Y / Z);
        }

        private static double ThetaFromH(Mat H)
        {
            var r10 = H.At<double>(1, 0);
            var r00 = H.At<double>(0, 0);
            return Math.Atan2(r10, r00);
        }

        private static Mat RigidFromThetaTxTy(double theta, double tx, double ty)
        {
            var c = Math.Cos(theta);
            var s = Math.Sin(theta);

            var M = new Mat(2, 3, MatType.CV_64FC1);
            M.Set<double>(0, 0, c); M.Set<double>(0, 1, -s); M.Set<double>(0, 2, tx);
            M.Set<double>(1, 0, s); M.Set<double>(1, 1, c); M.Set<double>(1, 2, ty);
            return M;
        }

        private static Mat ToHomography(Mat M2x3)
        {
            var H = Eye3x3();
            H.Set<double>(0, 0, M2x3.At<double>(0, 0));
            H.Set<double>(0, 1, M2x3.At<double>(0, 1));
            H.Set<double>(0, 2, M2x3.At<double>(0, 2));
            H.Set<double>(1, 0, M2x3.At<double>(1, 0));
            H.Set<double>(1, 1, M2x3.At<double>(1, 1));
            H.Set<double>(1, 2, M2x3.At<double>(1, 2));
            return H;
        }
        #endregion

        #region Help Halcon
        private static Mat ScaleHomographyCv(Mat Hcv, Mat S, Mat SInv)
        {
            using (var tmp = (S * Hcv).ToMat())
                return (tmp * SInv).ToMat();
        }

        private static double[] ToHalconProjective9RowMajorFromCv(Mat Hcv)
        {
            if (Hcv == null || Hcv.Empty() || Hcv.Rows != 3 || Hcv.Cols != 3)
                throw new ArgumentException("Hcv must be 3x3 CV_64F.", nameof(Hcv));

            using (var P = Eye3x3())
            {
                P.Set<double>(0, 0, 0); P.Set<double>(0, 1, 1);
                P.Set<double>(1, 0, 1); P.Set<double>(1, 1, 0);
                using (var tmp = (P * Hcv).ToMat())
                using (var Hh = (tmp * P).ToMat())
                {
                    return new[]
                        {
                            Hh.At<double>(0,0), Hh.At<double>(0,1), Hh.At<double>(0,2),
                            Hh.At<double>(1,0), Hh.At<double>(1,1), Hh.At<double>(1,2),
                            Hh.At<double>(2,0), Hh.At<double>(2,1), Hh.At<double>(2,2),
                        };
                }
            }
        }

        private void WriteHalconImage(string path, HObject image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            var format = HalconFormatFromPath(path, SaveMode);
            HOperatorSet.WriteImage(image, new HTuple(format), new HTuple(0), new HTuple(path));
        }

        private static string HalconFormatFromPath(string path, SaveMode mode)
        {
            var ext = (Path.GetExtension(path) ?? string.Empty).Trim().ToLowerInvariant();

            if (mode == SaveMode.Full)
                return "bigtiff none";
            switch (ext)
            {
                case ".tif":
                case ".tiff": return "tiff";
                case ".jpg":
                case ".jpeg":
                case ".jp2":
                case ".png":
                    return "png";
                default:
                    return "tiff";
            }
        }
        #endregion
    }
}
