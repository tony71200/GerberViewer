using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GerberViewer.Workflow
{
    public sealed class HalconValidationResult { public bool Success { get; set; } public string Diagnostics { get; set; } public string StartupLogPath { get; set; } }
    public static class HalconRuntimeValidator
    {
        public static HalconValidationResult Validate()
        {
            var lines = new System.Collections.Generic.List<string>();
            if (!Environment.Is64BitProcess) lines.Add("FAIL: GerberViewer must run as an x64 process."); else lines.Add("OK: x64 process.");
            var roots = new[] { AppDomain.CurrentDomain.BaseDirectory, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dll"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "EWindowControl", "dll") };
            var managed = roots.Select(r => Path.Combine(r, "halcondotnetxl.dll")).FirstOrDefault(File.Exists) ?? roots.Select(r => Path.Combine(r, "halcondotnet.dll")).FirstOrDefault(File.Exists);
            if (managed == null) lines.Add("FAIL: HALCON managed DLL halcondotnetxl.dll/halcondotnet.dll not found."); else lines.Add("OK: HALCON managed DLL: " + managed);
            var nativeHint = Environment.GetEnvironmentVariable("HALCONROOT");
            if (string.IsNullOrWhiteSpace(nativeHint)) lines.Add("FAIL: HALCONROOT is not set; native HALCON DLL resolution cannot be verified."); else lines.Add("OK: HALCONROOT=" + nativeHint);
            if (managed != null) { var an = AssemblyName.GetAssemblyName(managed); lines.Add("OK: HALCON managed version " + an.Version); }
            lines.Add("License check, basic operator self-test, and NCC smoke test require HALCON runtime binding and are mandatory before continuing.");
            var ok = lines.All(l => !l.StartsWith("FAIL", StringComparison.OrdinalIgnoreCase));
            if (!ok) lines.Add("HALCON validation failed; OpenCV fallback is intentionally disabled.");
            var log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "halcon-startup.log"); File.WriteAllLines(log, lines.ToArray());
            return new HalconValidationResult { Success = ok, Diagnostics = string.Join(Environment.NewLine, lines.ToArray()), StartupLogPath = log };
        }
    }
}
