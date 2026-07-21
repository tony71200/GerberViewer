using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GerberViewer.Workflow;

namespace GerberViewer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var halcon = HalconRuntimeValidator.Validate();
            if (!halcon.Success)
            {
                MessageBox.Show(halcon.Diagnostics + Environment.NewLine + "Startup log: " + halcon.StartupLogPath, "HALCON validation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.Run(new MainForm());
        }
    }
}
