using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Elog_1_0
{
    /// <summary>
    /// Recovered compatibility logger for the original Elog_1_0.dll.
    /// Public API is based on the assembly metadata and usages in ImageEditor.
    /// </summary>
    public class Elog : IDisposable
    {
        private readonly object _sync = new object();
        private bool _debug = true;
        private bool _fileEnabled;
        private bool _listBoxEnabled;
        private bool _disposed;
        private string _directoryPath = AppDomain.CurrentDomain.BaseDirectory;
        private string _fileName = "Log";
        private ListBox _listBox;
        private Color _infoColor = Color.Black;
        private Color _warningColor = Color.DarkOrange;
        private Color _errorColor = Color.DarkRed;

        public bool Debug
        {
            get { return _debug; }
            set { _debug = value; }
        }

        public void SetOpenFile(bool isOpen, string paramFolderPath, string paramFileName)
        {
            lock (_sync)
            {
                _fileEnabled = isOpen;
                if (!string.IsNullOrWhiteSpace(paramFolderPath))
                    _directoryPath = paramFolderPath;
                if (!string.IsNullOrWhiteSpace(paramFileName))
                    _fileName = paramFileName;
                if (_fileEnabled)
                    Directory.CreateDirectory(_directoryPath);
            }
        }

        public void SetOpenListBox(bool isOpen, ListBox listBox)
        {
            lock (_sync)
            {
                _listBoxEnabled = isOpen;
                _listBox = listBox;
                if (_listBox != null)
                {
                    _listBox.DrawMode = DrawMode.OwnerDrawFixed;
                    _listBox.DrawItem -= ListBox_DrawItem;
                    _listBox.DrawItem += ListBox_DrawItem;
                }
            }
        }

        public void SetDeleteFile(bool isDelete, string paramFolderPath, int days)
        {
            if (!isDelete || string.IsNullOrWhiteSpace(paramFolderPath) || !Directory.Exists(paramFolderPath))
                return;

            foreach (var file in Directory.GetFiles(paramFolderPath, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-Math.Abs(days)))
                        File.Delete(file);
                }
                catch { }
            }
        }

        public void SetInfoFontColor(Color color) { _infoColor = color; }
        public void SetWarningFontColor(Color color) { _warningColor = color; }
        public void SetErrorFontColor(Color color) { _errorColor = color; }

        public void WriteInfo(string message) { Write("Info", message, _infoColor); }
        public void WriteWarning(string message) { Write("Warning", message, _warningColor); }
        public void WriteError(string message) { Write("Error", message, _errorColor); }

        private void Write(string level, string message, Color color)
        {
            if (!_debug) return;
            var line = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] {2}", DateTime.Now, level, message ?? string.Empty);
            lock (_sync)
            {
                if (_fileEnabled)
                    AppendToFile(line);
                if (_listBoxEnabled && _listBox != null && !_listBox.IsDisposed)
                    AppendToListBox(line, color);
            }
        }

        private void AppendToFile(string line)
        {
            Directory.CreateDirectory(_directoryPath);
            var path = Path.Combine(_directoryPath, _fileName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".txt");
            File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
        }

        private void AppendToListBox(string line, Color color)
        {
            if (_listBox.InvokeRequired)
            {
                _listBox.BeginInvoke(new Action(() => AppendToListBox(line, color)));
                return;
            }
            _listBox.Items.Add(new LogListBoxItem { Text = line, Color = color });
            _listBox.TopIndex = Math.Max(0, _listBox.Items.Count - 1);
        }

        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0) return;
            var lb = (ListBox)sender;
            var item = lb.Items[e.Index] as LogListBoxItem;
            var text = item != null ? item.Text : Convert.ToString(lb.Items[e.Index]);
            var brush = new SolidBrush(item != null ? item.Color : lb.ForeColor);
            try { e.Graphics.DrawString(text, e.Font, brush, e.Bounds); }
            finally { brush.Dispose(); }
            e.DrawFocusRectangle();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_listBox != null)
                _listBox.DrawItem -= ListBox_DrawItem;
        }

        private sealed class LogListBoxItem
        {
            public string Text { get; set; }
            public Color Color { get; set; }
            public override string ToString() { return Text; }
        }
    }
}
