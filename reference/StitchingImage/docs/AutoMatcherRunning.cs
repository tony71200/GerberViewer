using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PCM_Inspection_Demo.Utils;

namespace PCM_Inspection_Demo.Matcher
{
    public class AutoMatcherRunning
    {
        public string RefPath { get; private set; } = string.Empty;
        public List<string> TestPaths { get; private set; } = new List<string>();
        private BaseMatcher Matcher { get; set; } = null;

        private readonly string _tempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auto-matcher");

        public AutoMatcherRunning(string refPath, List<string> testPaths, BaseMatcher matcher)
        {
            RefPath = refPath;
            TestPaths = testPaths ?? new List<string>();
            Matcher = matcher;
            if (!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
            }
        }

        public override string ToString()
        {
            return $"AutoMatcherRunning | Ref: {RefPath}, Tests: {string.Join(", ", TestPaths)}, Matcher: {Matcher?.MatcherName ?? "<null>"}";
        }

        public void Run()
        {
            RunAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public async Task RunAsync(CancellationToken token, Action<int, int, string> progress = null)
        {
            if (Matcher == null)
            {
                Logger.Error("Matcher is null. Cannot run AutoMatcher.");
                return;
            }

            if (string.IsNullOrWhiteSpace(RefPath) || !File.Exists(RefPath))
            {
                Logger.Warning("Reference image path is invalid. Auto matcher canceled.");
                return;
            }

            if (TestPaths == null || TestPaths.Count == 0)
            {
                Logger.Warning("No test images found. Auto matcher canceled.");
                return;
            }

            using (var reference = new Bitmap(RefPath))
            {
                var refRoi = new Rectangle(0, 0, reference.Width, reference.Height);
                Logger.Info($"Auto matcher started. Ref: {RefPath}, Tests: {TestPaths.Count}, Matcher: {Matcher.MatcherName}");

                var total = TestPaths.Count;
                var processed = 0;
                foreach (var testPath in TestPaths)
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        if (string.IsNullOrWhiteSpace(testPath) || !File.Exists(testPath))
                        {
                            Logger.Warning($"Skip invalid test image path: {testPath}");
                            continue;
                        }

                        using (var test = new Bitmap(testPath))
                        {
                            var testRoi = new Rectangle(0, 0, test.Width, test.Height);
                            var result = await Matcher.RunAsync(reference, refRoi, test, testRoi, token).ConfigureAwait(false);
                            var summary = BaseMatcher.FormatTransformForLog(result);

                            if (!result.Success)
                            {
                                Logger.Warning($"Auto matcher failed. Test: {testPath} | {summary} | Message: {result.Message}");
                                continue;
                            }

                            Logger.Info($"Auto matcher success. Test: {testPath} | {summary}");
                            SaveAlignedDebugImage(test, testPath, result);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Warning("Auto matcher canceled by token.");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error running matcher on Test: {testPath} | Exception: {ex.Message}");
                    }
                    finally
                    {
                        processed++;
                        progress?.Invoke(processed, total, testPath);
                    }
                }
            }

            Logger.Info($"Auto matcher finished. Debug folder: {_tempFolder}");
        }

        private void SaveAlignedDebugImage(Bitmap testImage, string originalPath, BaseMatcher.MatchResult result)
        {
            var fileName = Path.GetFileNameWithoutExtension(originalPath);
            var outPath = Path.Combine(_tempFolder, fileName + "_aligned.png");

            using (var aligned = new Bitmap(testImage.Width, testImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(aligned))
            {
                g.Clear(Color.Black);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                var pivotX = testImage.Width / 2f;
                var pivotY = testImage.Height / 2f;

                g.TranslateTransform(pivotX + (float)result.Dx, pivotY + (float)result.Dy);
                g.RotateTransform((float)result.AngleDeg);
                g.TranslateTransform(-pivotX, -pivotY);
                g.DrawImage(testImage, 0, 0, testImage.Width, testImage.Height);
                aligned.Save(outPath);
            }

            Logger.Info($"Saved aligned test image: {outPath}");
        }
    }
}
