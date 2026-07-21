// Tony 30/01/2026: Define centralized error codes for easier log tracing.
namespace StitchingImage.Stitch_Tools.Utils
{
    public static class ErrorCodePLP
    {
        // Main flow errors (MAI)
        public const string MainStartupFailed = "MAI001";
        public const string MainUiThreadException = "MAI002";
        public const string MainUnhandledException = "MAI003";

        // Function/runtime errors (FUN)
        public const string FuncHalconLibrariesMissing = "FUN001";
        public const string FuncHalconStitchFailed = "FUN002";
        public const string FuncFallbackStitchFailed = "FUN003";

        // Data/IO errors (DAT)
        public const string DataSharedFolderEmpty = "DAT001";
        public const string DataSharedFolderInsufficient = "DAT002";
        public const string DataImageReadFailed = "DAT003";
        public const string DataImageFormatInvalid = "DAT004";
        public const string DataImageSaveFailed = "DAT005";
        public const string DataStitchFailed = "DAT006";
        public const string DataStitchCancelled = "DAT007";
        public const string DataImageCountMismatch = "DAT008";
    }
}
