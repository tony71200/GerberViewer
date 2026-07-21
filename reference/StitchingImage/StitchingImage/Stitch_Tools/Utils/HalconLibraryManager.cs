// Tony 30/01/2026: Handle local HALCON DLL discovery for runtime fallback logic.
using System;
using System.IO;
using HalconDotNet;

namespace StitchingImage.Stitch_Tools.Utils
{
    public static class HalconLibraryManager
    {
        public const string HalconFolderRelative = "ExternalLibraries\\Halcon";
        private const string HalconDotNetDll = "halcondotnetxl.dll";
        private const string HDevEngineDll = "hdevenginedotnetxl.dll";
        public static bool AllowHalcon { get; private set; } = true;

        public static bool TryFindHalconFolder(out string folder, out string missing)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                baseDir,
                Path.Combine(baseDir, HalconFolderRelative)
            };

            foreach (var candidate in candidates)
            {
                var halconPath = Path.Combine(candidate, HalconDotNetDll);
                var hdevPath = Path.Combine(candidate, HDevEngineDll);
                if (File.Exists(halconPath) && File.Exists(hdevPath))
                {
                    folder = candidate;
                    missing = string.Empty;
                    return true;
                }
            }

            folder = Path.Combine(baseDir, HalconFolderRelative);
            missing = BuildMissingMessage(baseDir);
            return false;
        }

        public static bool TryRunSelfTest(out string message, out bool licenseExpired, out int errorCode)
        {
            message = string.Empty;
            licenseExpired = false;
            errorCode = 0;

            try
            {
                HOperatorSet.GenEmptyObj(out HObject obj);
                obj.Dispose();
                return true;
            }
            catch (HOperatorException ex)
            {
                if (TryGetHalconErrorCode(ex, out var code))
                {
                    errorCode = code;
                    licenseExpired = code == 2042;
                }
                message = $"HALCON self-test failed (code {errorCode}): {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                message = $"HALCON self-test failed: {ex.Message}";
                return false;
            }
        }

        public static void DisableHalcon(string reason)
        {
            AllowHalcon = false;
        }

        private static string BuildMissingMessage(string baseDir)
        {
            var expectedFolder = Path.Combine(baseDir, HalconFolderRelative);
            return $"Missing HALCON DLLs. Expected in '{expectedFolder}' or app folder: {HalconDotNetDll}, {HDevEngineDll}.";
        }

        private static bool TryGetHalconErrorCode(HOperatorException ex, out int code)
        {
            code = 0;
            if (ex == null)
                return false;

            var prop = ex.GetType().GetProperty("ErrorCode");
            if (prop?.GetValue(ex) is int propCode)
            {
                code = propCode;
                return true;
            }

            var method = ex.GetType().GetMethod("GetErrorCode", Type.EmptyTypes);
            if (method?.Invoke(ex, null) is int methodCode)
            {
                code = methodCode;
                return true;
            }

            return false;
        }
    }
}
