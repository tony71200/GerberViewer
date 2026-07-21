// Tony 20260202: Base exception types for error-code-driven handling.
using System;

namespace StitchingImage.Stitch_Tools.Utils
{
    public abstract class ErrorCodeException : Exception
    {
        protected ErrorCodeException(string code, string message, Exception innerException = null)
            : base(message, innerException)
        {
            Code = code;
        }

        public string Code { get; }
    }

    public sealed class PlpException : ErrorCodeException
    {
        public PlpException(string code, string message, Exception innerException = null)
            : base(code, message, innerException)
        {
        }
    }

    public sealed class ConnectionException : ErrorCodeException
    {
        public ConnectionException(string code, string message, Exception innerException = null)
            : base(code, message, innerException)
        {
        }
    }
}
