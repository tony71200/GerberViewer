// Tony 20260202: Define connection-related error codes for TCP/shared folder handling.
namespace StitchingImage.Stitch_Tools.Utils
{
    public static class ErrorCodeConnection
    {
        // Connection/shared folder errors (CON)
        public const string ConnectionMissing = "CON001";
        public const string SharedFolderNotFound = "CON002";
        public const string SharedFolderTimeout = "CON003";
        public const string SharedFolderUnavailable = "CON004";
        public const string SharedFolderUnauthorized = "CON005";
    }
}
