// Tony 30/01/2026: Provide unified error reporting with log + MessageBox and optional line info.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace StitchingImage.Stitch_Tools.Utils
{
    public static class ErrorReporter
    {
        public static void Report(string code, string title, string message, Exception ex = null)
        {
            var detail = BuildErrorDetail(code, message, ex);
            Logger.Error(detail);
            ShowMessage(title, detail);
        }

        public static string BuildErrorDetail(string code, string message, Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append("[").Append(code).Append("] ").Append(message);

            var exDetail = FormatException(ex);
            if (!string.IsNullOrWhiteSpace(exDetail))
            {
                sb.Append(" | ").Append(exDetail);
            }

            return sb.ToString();
        }

        private static string FormatException(Exception ex)
        {
            if (ex == null)
                return string.Empty;

            var baseMessage = $"{ex.GetType().Name}: {ex.Message}";
            var lineInfo = GetLineInfo(ex);
            return string.IsNullOrWhiteSpace(lineInfo) ? baseMessage : $"{baseMessage} ({lineInfo})";
        }

        private static string GetLineInfo(Exception ex)
        {
            try
            {
                var trace = new StackTrace(ex, true);
                var frame = trace.GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0);
                if (frame == null)
                    return string.Empty;

                var file = frame.GetFileName();
                var fileName = string.IsNullOrWhiteSpace(file) ? "unknown" : Path.GetFileName(file);
                return $"{fileName}:{frame.GetFileLineNumber()}";
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void ShowMessage(string title, string detail)
        {
            try
            {
                MessageBox.Show(detail, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // ignore UI errors
            }
        }
    }
}
