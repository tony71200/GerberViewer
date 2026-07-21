using StitchingImage.Stitch_Tools.Utils;
using System;
using System.Windows.Forms;

namespace StitchingImage
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Tony 30/01/2026: Add global exception handlers to keep app running and report errors via log/message box.
            Application.ThreadException += (sender, args) =>
            {
                ErrorReporter.Report(
                    ErrorCodePLP.MainUiThreadException,
                    "Unhandled UI Error",
                    "UI thread encountered an unexpected error.",
                    args.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                ErrorReporter.Report(
                    ErrorCodePLP.MainUnhandledException,
                    "Unhandled Error",
                    "Unexpected error occurred.",
                    args.ExceptionObject as Exception);
            };

            // Tony 30/01/2026: Wrap startup in try/catch to avoid crash on initialization errors.
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var settings = AppSettings.Load();
                Logger.Info("Program start.");
                if (HalconLibraryManager.TryFindHalconFolder(out var halconFolder, out _))
                {
                    if (!HalconLibraryManager.TryRunSelfTest(out var message, out var licenseExpired, out var errorCode))
                    {
                        if (licenseExpired)
                        {
                            var result = MessageBox.Show(
                                $"HALCON license expired (error {errorCode}).\n" +
                                "Please update your license or use OpenCV instead.\n\n" +
                                "Click Yes to continue with OpenCV, or No to exit.",
                                "HALCON License Expired",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);
                            if (result == DialogResult.Yes)
                            {
                                HalconLibraryManager.DisableHalcon("license expired");
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show(
                                message,
                                "HALCON Self-Test Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            HalconLibraryManager.DisableHalcon("self-test failed");
                        }
                    }
                }
                Application.Run(new MainForm(settings));
            }
            catch (Exception ex)
            {
                ErrorReporter.Report(
                    ErrorCodePLP.MainStartupFailed,
                    "Startup Error",
                    "Failed to start application.",
                    ex);
            }
        }
    }
}
