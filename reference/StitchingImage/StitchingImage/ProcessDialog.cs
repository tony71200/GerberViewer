using StitchingImage.Stitch_Tools.Utils;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace StitchingImage
{
    public partial class ProcessDialog : Form
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private int _totalOrders = 1;
        private bool _completed;

        public event Action CancelRequested;

        public ProcessDialog()
        {
            InitializeComponent();
            Logger.RegisterListBox(listB_Processing);
            _stopwatch.Start();
        }

        public void SetTotalOrders(int totalOrders)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int>(SetTotalOrders), totalOrders);
                return;
            }

            _totalOrders = Math.Max(1, totalOrders);
            _progressBar.Minimum = 0;
            _progressBar.Maximum = _totalOrders;
            _progressBar.Value = 0;
        }

        public void UpdateStatus(int currentIndex, int groupId, int orderId, int imageCount)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, int, int, int>(UpdateStatus), currentIndex, groupId, orderId, imageCount);
                return;
            }

            var safeIndex = Math.Min(Math.Max(0, currentIndex), _totalOrders);
            _progressBar.Value = safeIndex;
            _lblGroup.Text = $"Group: {groupId}";
            _lblOrder.Text = $"Order: {safeIndex}/{_totalOrders} (ID {orderId})";
            _lblImages.Text = $"Images: {imageCount}";
            _lblElapsed.Text = $"Elapsed: {_stopwatch.Elapsed:hh\\:mm\\:ss}";
        }

        public void MarkCompleted()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(MarkCompleted));
                return;
            }

            _progressBar.Value = _totalOrders;
            _lblTitle.Text = "Stitching completed";
            _lblElapsed.Text = $"Elapsed: {_stopwatch.Elapsed:hh\\:mm\\:ss}";
            _completed = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_completed)
                return;

            CancelRequested?.Invoke();
        }
    }
}
