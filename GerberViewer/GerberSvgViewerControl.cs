using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace GerberViewer
{
    public sealed class GerberSvgViewerControl : UserControl
    {
        private readonly WebView2 _webView;
        private readonly Label _message;
        private Task _initializationTask;
        private bool _webViewReady;

        public GerberSvgViewerControl()
        {
            BackColor = Color.FromArgb(12, 14, 14);
            _webView = new WebView2 { Dock = DockStyle.Fill, Visible = false };
            _message = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(130, 140, 140),
                BackColor = Color.FromArgb(12, 14, 14),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Initializing SVG preview..."
            };
            Controls.Add(_webView);
            Controls.Add(_message);
        }

        public bool IsAvailable { get { return _webViewReady; } }

        public Task EnsureInitializedAsync()
        {
            if (_initializationTask == null) _initializationTask = InitializeAsync();
            return _initializationTask;
        }

        public async Task LoadSvg(string svg)
        {
            await EnsureInitializedAsync().ConfigureAwait(true);
            if (!_webViewReady) throw new InvalidOperationException(_message.Text);
            _webView.NavigateToString(BuildHtml(svg ?? string.Empty));
        }

        public void FitToView() { ExecuteScript("window.gerberViewer && window.gerberViewer.fitToView();"); }
        public void ResetView() { ExecuteScript("window.gerberViewer && window.gerberViewer.resetView();"); }
        public void SetLayerVisibility(int layerIndex, bool visible)
        {
            ExecuteScript("window.gerberViewer && window.gerberViewer.setLayerVisibility(" + layerIndex + "," + (visible ? "true" : "false") + ");");
        }
        public void SetLayerColor(int layerIndex, Color color)
        {
            ExecuteScript("window.gerberViewer && window.gerberViewer.setLayerColor(" + layerIndex + ",'" + ColorTranslator.ToHtml(color) + "');");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _webView.Dispose();
            base.Dispose(disposing);
        }

        private async Task InitializeAsync()
        {
            try
            {
                await _webView.EnsureCoreWebView2Async(null).ConfigureAwait(true);
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                _webViewReady = true;
                _message.Visible = false;
                _webView.Visible = true;
            }
            catch (Exception ex)
            {
                _webViewReady = false;
                _message.Text = "WebView2 unavailable; using bitmap preview fallback.\r\n" + ex.Message;
            }
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!string.Equals(e.Uri, "about:blank", StringComparison.OrdinalIgnoreCase)) e.Cancel = true;
        }

        private void ExecuteScript(string script)
        {
            if (!_webViewReady || _webView.CoreWebView2 == null) return;
            _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        private static string BuildHtml(string svg)
        {
            return "<!doctype html><html><head><meta charset='utf-8'><style>html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#0c0e0e}#host{width:100%;height:100%;display:flex;align-items:center;justify-content:center}svg{max-width:none;max-height:none;transform-origin:0 0;user-select:none}</style></head><body><div id='host'>" + svg + "</div><script>" + Script + "</script></body></html>";
        }

        private const string Script = @"
(function(){
var svg=document.querySelector('svg'), scale=1, tx=0, ty=0, dragging=false, lastX=0, lastY=0;
function apply(){ if(svg) svg.style.transform='translate('+tx+'px,'+ty+'px) scale('+scale+')'; }
function fit(){ if(!svg) return; var r=svg.getBoundingClientRect(), sx=innerWidth/r.width, sy=innerHeight/r.height; scale=Math.max(0.01, Math.min(sx, sy)*0.95); tx=(innerWidth-r.width*scale)/2; ty=(innerHeight-r.height*scale)/2; apply(); }
window.gerberViewer={fitToView:fit, resetView:function(){scale=1;tx=0;ty=0;apply();}, setLayerVisibility:function(i,v){var e=document.getElementById('gerber-layer-'+i); if(e)e.style.display=v?'':'none';}, setLayerColor:function(i,c){var e=document.getElementById('gerber-layer-'+i); if(e){e.querySelectorAll('[fill]').forEach(function(n){if(n.getAttribute('fill')!='none')n.setAttribute('fill',c);}); e.querySelectorAll('[stroke]').forEach(function(n){if(n.getAttribute('stroke')!='none')n.setAttribute('stroke',c);});}}};
addEventListener('resize', fit); addEventListener('load', fit);
document.addEventListener('wheel', function(ev){ev.preventDefault(); var f=ev.deltaY<0?1.25:0.8, ns=Math.max(0.01, Math.min(64,scale*f)); tx=ev.clientX-(ev.clientX-tx)*ns/scale; ty=ev.clientY-(ev.clientY-ty)*ns/scale; scale=ns; apply();}, {passive:false});
document.addEventListener('mousedown', function(ev){dragging=true;lastX=ev.clientX;lastY=ev.clientY;}); document.addEventListener('mousemove', function(ev){if(!dragging)return; tx+=ev.clientX-lastX; ty+=ev.clientY-lastY; lastX=ev.clientX; lastY=ev.clientY; apply();}); document.addEventListener('mouseup', function(){dragging=false;});
fit();
})();";
    }
}
