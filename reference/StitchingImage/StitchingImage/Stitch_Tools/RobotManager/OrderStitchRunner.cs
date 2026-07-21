// =======================================================
// File: StitchingImage/Stitch_Tools/OrderStitchRunner.cs
// .NET Framework 4.8
// OpenCvSharp4 4.11.0.20250507
// =======================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenCvSharp;
using StitchingImage.Stitch_Tools.Matcher;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.RobotManager
{
    public enum EdgeDir { Horizontal, Vertical }

    public sealed class EdgeTransform : IDisposable
    {
        // Directed edge: A -> B, stored transform maps B -> A
        public int AId { get; set; }
        public int BId { get; set; }
        public EdgeDir Direction { get; set; }

        public bool Ok { get; set; }
        public bool UsedFallback { get; set; }
        public string Reason { get; set; } = "ok";

        public Mat HBToA_Full { get; set; }   // 3x3 CV_64F
        public Mat MRigidBToA { get; set; }   // 2x3 CV_64F

        public double Tx { get; set; }
        public double Ty { get; set; }
        public double ThetaRad { get; set; }

        public double MatchMs { get; set; }

        public void Dispose()
        {
            HBToA_Full?.Dispose();
            MRigidBToA?.Dispose();
            HBToA_Full = null;
            MRigidBToA = null;
        }
    }

    public sealed class StitchRunConfig
    {
        // Matching
        public StitchingConfig MatchCfg { get; set; } = new StitchingConfig();

        // Compose
        public double MaxCanvasMegapix { get; set; } = 250.0;
        public bool UseRigidForGlobal { get; set; } = true; // true => use MRigidBToA if Ok else H

        // Python-like fallback offsets (dy_off, dx_off)
        public (double DyOff, double DxOff) FallbackOffsetHorizontal { get; set; } = (9.5, -339.0);
        public (double DyOff, double DxOff) FallbackOffsetVertical { get; set; } = (-306.0, -58.0);
    }

    public sealed class OrderRunResult : IDisposable
    {
        public IReadOnlyList<EdgeTransform> EdgeTransforms { get; set; }
        public double MatchingMs { get; set; }
        public double StitchingMs { get; set; }
        public double SaveMs { get; set; }
        public string OutputPath { get; set; }

        public void Dispose()
        {
            if (EdgeTransforms == null) return;
            foreach (var e in EdgeTransforms) e.Dispose();
        }
    }
    public static class OrderStitchRunner
    {
        // [GPT-5.2-Codex] [Change time: 260319] [Centralize graph edge direction hints so matcher orientation comes from HNext/VNext before robot-delta fallback]
        private static Direction ToMatcherDirection(EdgeDir edgeDirection)
            => edgeDirection == EdgeDir.Horizontal ? Direction.Horizontal : Direction.Vertical;

        // ---------------------------
        // 1) TEST 1 ORDER: matching time + stitching time
        // ---------------------------
        public static OrderRunResult TestOneComponentAndStitch(TraversalComponent comp, StitchRunConfig cfg, StitchingImage stitcher, string outputPath)
        {
            ValidateTraversalComponent(comp);
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (stitcher == null) throw new ArgumentNullException(nameof(stitcher));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            Logger.Info($"Start matching component {comp.ComponentIndex} images={comp.Points.Length} method={cfg.MatchCfg.Method}");

            var nodes = comp.Points.ToDictionary(p => p.ImageId);

            //var sizesFull = nodes.ToDictionary(kv => kv.Key, kv => EstimateOriginalSize(kv.Value.FilePath));
            var edges = TraversalGraph.EnumerateEdges(comp.Graph).ToList();
            if (edges.Count == 0) throw new InvalidOperationException("Graph has no edges (HNext/VNext).");

            var edgeResults = new List<EdgeTransform>(edges.Count);

            var swMatchTotal = Stopwatch.StartNew();
            using (var matcher = PairMatching.CreateMatcher(cfg.MatchCfg))
            {
                foreach (var e in edges)
                {
                    var swEdge = Stopwatch.StartNew();

                    var a = nodes[e.AId];
                    var b = nodes[e.BId];

                    var dx = b.XRobot - a.XRobot;
                    var dy = b.YRobot - a.YRobot;
                    var isSpecialGap = comp.SpecialGapEdges != null
                        && comp.SpecialGapEdges.Contains((e.AId, e.BId));

                    try
                    {
                        PairResult pr;
                        if (matcher is PhaseCorrMatcher phaseCorr)
                        {
                            pr = phaseCorr.MatchPairWithSpecialGap(
                                a.FilePath,
                                b.FilePath,
                                dxRobot: dx,
                                dyRobot: dy,
                                estimateDistX: comp.EstimateDistanceX,
                                estimateDistY: comp.EstimateDistanceY,
                                // directionHint: (e.Direction == EdgeDir.Horizontal ? Direction.Horizontal : Direction.Vertical),
                                directionHint: ToMatcherDirection(e.Direction),
                                isSpecialGap: isSpecialGap);
                        }
                        else
                        {
                            pr = matcher.MatchPair(
                                a.FilePath,
                                b.FilePath,
                                dxRobot: dx,
                                dyRobot: dy,
                                estimateDistX: comp.EstimateDistanceX,
                                estimateDistY: comp.EstimateDistanceY,
                                // directionHint: (e.Direction == EdgeDir.Horizontal ? Direction.Horizontal : Direction.Vertical));
                                directionHint: ToMatcherDirection(e.Direction));
                        }

                        swEdge.Stop();

                        var ok = pr?.Eval != null; //&& pr.Eval.IsMatch;
                        Logger.Info($"[MATCHING] {a.ImageId}->{b.ImageId} {PairMatching.ToHomoString(pr)} {PairMatching.ToEvalString(pr)}");

                        if (!ok)
                        {
                            pr?.HFullBToA?.Dispose();
                            pr?.MRigidBToA?.Dispose();
                        }
                        Logger.Info($"[CHECK] {pr?.HFullBToA.At<double>(0, 2)} | {pr?.HFullBToA.At<double>(1, 2)}");
                        edgeResults.Add(new EdgeTransform
                        {
                            AId = e.AId,
                            BId = e.BId,
                            Direction = e.Direction,
                            Ok = ok,
                            UsedFallback = false,
                            Reason = ok ? "ok" : (pr?.Eval?.Reason ?? "match_fail"),
                            HBToA_Full = ok ? pr.HFullBToA : null,
                            MRigidBToA = ok ? pr.MRigidBToA : null,
                            Tx = pr?.Tx ?? 0,
                            Ty = pr?.Ty ?? 0,
                            ThetaRad = pr?.DThetaRad ?? 0,
                            MatchMs = swEdge.Elapsed.TotalMilliseconds
                        });
                    }
                    catch (Exception ex)
                    {
                        swEdge.Stop();
                        Logger.Error($"Edge matching failed: {a.ImageId}->{b.ImageId}", ex);
                        edgeResults.Add(new EdgeTransform
                        {
                            AId = e.AId,
                            BId = e.BId,
                            Direction = e.Direction,
                            Ok = false,
                            UsedFallback = false,
                            Reason = "exception:" + ex.GetType().Name,
                            HBToA_Full = null,
                            MRigidBToA = null,
                            Tx =  0,
                            Ty =  0,
                            MatchMs = swEdge.Elapsed.TotalMilliseconds
                        });
                    }
                }
            }
            swMatchTotal.Stop();

            // Tony 30/01/2026: Prefer HALCON when available; otherwise fall back to OpenCV without crashing.
            StitchingImageResult stitchResult = null;
            if (HalconLibraryManager.TryFindHalconFolder(out var halconFolder, out var missing) && HalconLibraryManager.AllowHalcon)
            {
                try
                {
                    stitchResult = stitcher.StitchHalcon(comp, edgeResults, cfg, outputPath);
                    Logger.Info($"HALCON stitch completed using libraries from: {halconFolder}");
                }
                catch (Exception ex)
                {
                    ErrorReporter.Report(
                        ErrorCodePLP.FuncHalconStitchFailed,
                        "HALCON Stitch Error",
                        "HALCON stitching failed. Falling back to OpenCV Stitch.",
                        ex);
                }
            }
            else
            {
                try
                {
                    stitchResult = stitcher.Stitch(comp, edgeResults, cfg, outputPath);
                    Logger.Info("Fallback OpenCV stitch completed.");
                }
                catch (Exception ex)
                {
                    ErrorReporter.Report(
                        ErrorCodePLP.FuncFallbackStitchFailed,
                        "OpenCV Stitch Error",
                        "OpenCV stitching failed. Output may be incomplete.",
                        ex);
                    stitchResult = new StitchingImageResult();
                }
            }
            if (stitchResult == null)
            {
                stitchResult = new StitchingImageResult();
                Logger.Error("Stitching failed with unknown error. No result returned.");
            }
            //Logger.Info($"Finished component {comp.ComponentIndex} matching={swMatchTotal.Elapsed.TotalMilliseconds:0.##}ms stitching={stitchResult.StitchingMs:0.##}ms saving={stitchResult.SaveMs:0.##}ms output={outputPath}");

            return new OrderRunResult
            {
                EdgeTransforms = edgeResults,
                MatchingMs = swMatchTotal.Elapsed.TotalMilliseconds,
                StitchingMs = stitchResult != null? stitchResult.StitchingMs: 0.0,
                SaveMs = stitchResult != null? stitchResult.SaveMs: 0.0,
                OutputPath = outputPath
            };
        }

        // [Codex] [Change time: 260323] [Make the active stitch runner path explicitly traversal-first and fail fast on invalid traversal inputs]
        private static void ValidateTraversalComponent(TraversalComponent comp)
        {
            if (comp == null) throw new ArgumentNullException(nameof(comp));
            if (comp.Graph == null) throw new ArgumentException("TraversalComponent.Graph is null.");
            if (comp.Points == null || comp.Points.Length == 0) throw new ArgumentException("TraversalComponent.Points empty.");
        }

    }
}
