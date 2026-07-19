using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GerberViewer
{
    public sealed class GerberPreviewHost : UserControl
    {
        private readonly WebBrowser _browser = new WebBrowser();
        private float _zoom = 1f;
        private string _svg = "";

        public GerberPreviewHost()
        {
            BackColor = Color.FromArgb(12, 14, 14);
            _browser.Dock = DockStyle.Fill;
            _browser.AllowWebBrowserDrop = false;
            _browser.IsWebBrowserContextMenuEnabled = false;
            _browser.WebBrowserShortcutsEnabled = false;
            Controls.Add(_browser);
        }

        public bool HasPreview { get { return !string.IsNullOrEmpty(_svg); } }
        public float Zoom { get { return _zoom; } }

        public void SetSvg(string svg, bool fit)
        {
            _svg = svg ?? "";
            if (fit) _zoom = 1f;
            NavigateSvg();
        }

        public void FitToView()
        {
            _zoom = 1f;
            NavigateSvg();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!HasPreview) return;
            _zoom = Math.Max(0.05f, Math.Min(64f, _zoom * (e.Delta > 0 ? 1.25f : 0.8f)));
            NavigateSvg();
        }

        private void NavigateSvg()
        {
            if (string.IsNullOrEmpty(_svg))
            {
                _browser.DocumentText = "<html><body style='background:#0c0e0e;color:#829090;font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh'>Drop Gerber files here or click Open</body></html>";
                return;
            }

            string html = "<!doctype html><html><head><meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\"><style>html,body{margin:0;width:100%;height:100%;overflow:auto;background:#0c0e0e}#wrap{transform-origin:0 0;transform:scale(" + _zoom.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");}</style></head><body><div id=\"wrap\">" + _svg + "</div></body></html>";
            _browser.DocumentText = html;
        }
    }
}
