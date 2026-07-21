using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace StitchingImage.Stitch_Tools.Utils
{
    public enum LogLevel { Info, Warning, Error }
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly List<ListBox> _listBoxes = new List<ListBox>();
        private static readonly List<string> _entries = new List<string>();
        private static string _logDirectory;

        public static void Initialize (ListBox listBox, string logFilePath)
        {
            // Tony 20260202 Split log output by level with per-day files.
            _logDirectory = logFilePath;
            if (!string.IsNullOrWhiteSpace(logFilePath) && Path.HasExtension(logFilePath))
                _logDirectory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrWhiteSpace(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            RegisterListBox(listBox);
        }

        public static void RegisterListBox(ListBox listBox)
        {
            if (listBox == null || listBox.IsDisposed)
                return;

            string[] entriesSnapshot;
            lock (_lock)
            {
                if (!_listBoxes.Contains(listBox))
                    _listBoxes.Add(listBox);
                entriesSnapshot = _entries.ToArray();
            }

            SyncListBox(listBox, entriesSnapshot);
        }

        public static void Info(string message) => Log(LogLevel.Info, message);
        public static void Warning(string message) => Log(LogLevel.Warning, message);

        public static void Error(string message, Exception ex = null)
        {
            var full = ex == null ? message : $"{message} | {ex.Message}";
            Log(LogLevel.Error, full);
        }

        public static void Log(LogLevel level, string message)
        {
            var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}]: {message}";
            List<ListBox> listBoxesSnapshot;
            lock (_lock)
            {
                // Tony 20260202 Write logs to LOG_<level>_<YYYYMMDD>.txt files.
                if (!string.IsNullOrWhiteSpace(_logDirectory))
                {
                    var fileName = $"LOG_{level.ToString().ToLowerInvariant()}_{DateTime.Now:yyyyMMdd}.txt";
                    var path = Path.Combine(_logDirectory, fileName);
                    File.AppendAllText(path, entry + Environment.NewLine);
                }

                _entries.Add(entry);
                _listBoxes.RemoveAll(lb => lb == null || lb.IsDisposed);
                listBoxesSnapshot = _listBoxes.ToList();
            }
            foreach (var listBox in listBoxesSnapshot)
            {
                if (listBox.InvokeRequired)
                    listBox.BeginInvoke(new Action(() => AppendToListBox(listBox, entry)));
                else
                    AppendToListBox(listBox, entry);
            }
        }

        private static void SyncListBox(ListBox listBox, string[] entries)
        {
            if (listBox == null || listBox.IsDisposed)
                return;
            if (listBox.InvokeRequired)
                listBox.BeginInvoke(new Action(() => ApplyEntries(listBox, entries)));
            else
                ApplyEntries(listBox, entries);
        }

        private static void ApplyEntries(ListBox listBox, string[] entries)
        {
            if (listBox == null || listBox.IsDisposed)
                return;
            listBox.Items.Clear();
            if (entries.Length > 0)
                listBox.Items.AddRange(entries);
            if (listBox.Items.Count > 0)
                listBox.TopIndex = listBox.Items.Count - 1;
        }

        private static void AppendToListBox(ListBox listBox, string entry)
        {
            if(listBox == null || listBox.IsDisposed) return;
            listBox.Items.Add(entry);
            if (listBox.Items.Count > 0) listBox.TopIndex = listBox.Items.Count - 1;
        }
    }
}
