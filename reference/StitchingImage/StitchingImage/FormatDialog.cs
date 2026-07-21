using StitchingImage.Stitch_Tools.RobotManager;
using StitchingImage.Stitch_Tools.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StitchingImage
{
    public partial class FormatDialog : Form
    {
        public string Pattern => _txtPattern.Text.Trim();
        public FormatDialog(string pattern, string[] sampleFiles)
        {
            InitializeComponent();
            _txtPattern.Text = pattern;
            _lstFiles.Items.Clear();
            _lstFiles.Items.AddRange((sampleFiles ?? Array.Empty<string>().Cast<object>().ToArray()));
            if (_lstFiles.Items.Count > 0 ) _lstFiles.SelectedIndex = 0;
            _lstKeywords.Items.AddRange(new object[]
            {
                "<prefix>",
                "<group_id>",
                "<id>",
                "<position>",
                "<x>", "<y>",
                "<extension>",
                "<ignore>"
            });
        }

        private void InsertKeyword(string keyword)
        {
            if(string.IsNullOrEmpty(keyword)) { return; }
            var pos = _txtPattern.SelectionStart;
            _txtPattern.Text = _txtPattern.Text.Insert(pos, keyword);
            _txtPattern.SelectionStart = pos + keyword.Length;
            _txtPattern.Focus();
        }

        private void _txtPattern_DragEnter(object sender, DragEventArgs e)
        { 
            e.Effect = e.Data.GetDataPresent(typeof(string)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void _txtPattern_DragDrop(object sender, DragEventArgs e)
        {
            string str = e.Data.GetData(typeof(string)) as string;
            InsertKeyword(e.Data.GetData(typeof(string)) as string);
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            CheckCurrent();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void _lstKeywords_MouseDown(object sender, MouseEventArgs e)
        {
            if (_lstKeywords.SelectedItem != null)
                _lstKeywords.DoDragDrop(_lstKeywords.SelectedItem.ToString(), DragDropEffects.Copy);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult= DialogResult.Cancel;
            Close();
        }

        private void CheckCurrent()
        {
            var filename = _lstFiles.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(filename))
            {
                _lblStatus.Text = "No sample filename to check.";
                return;
            }
            if (!FilenameParser.TryParseWithPattern(filename, Pattern, true, out ImageInfo info, out var err))
            {
                _txtGroup.Text = _txtId.Text = _txtPosition.Text = _txtX.Text = _txtY.Text = "NaN";
                _lblStatus.Text = $"Parse failed: {err}";
                return;
            }
            _txtGroup.Text = info.GroupId.ToString(CultureInfo.InvariantCulture);
            _txtId.Text = info.ImageId.ToString(CultureInfo.InvariantCulture);
            _txtPosition.Text = info.PositionId.HasValue ? info.PositionId.Value.ToString(CultureInfo.InvariantCulture) : "None";
            _txtX.Text = double.IsNaN(info.XRobot) ? "N/A" : info.XRobot.ToString("0.###", CultureInfo.InvariantCulture);
            _txtY.Text = double.IsNaN(info.YRobot) ? "N/A" : info.YRobot.ToString("0.###", CultureInfo.InvariantCulture);
            _lblStatus.Text = "Parse success.";
        }
    }
}
