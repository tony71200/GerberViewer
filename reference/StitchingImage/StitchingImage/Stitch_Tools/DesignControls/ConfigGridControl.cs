using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Stitch_Tools.DesignControls
{
    public sealed class ConfigGridControl : UserControl
    {
        private readonly DataGridView _grid;
        private readonly HashSet<string> _highlightKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Color _highlightColor = Color.LightBlue;
        private IEditableConfig _config;
        private Action _saveAction;
        private bool _isRefreshing;

        public ConfigGridControl()
        {
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window
            };

            var keyColumn = new DataGridViewTextBoxColumn
            {
                Name = "Key",
                HeaderText = "Key",
                ReadOnly = true,
                FillWeight = 45,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var valueColumn = new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "Value",
                ReadOnly = false,
                FillWeight = 55,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            _grid.Columns.Add(keyColumn);
            _grid.Columns.Add(valueColumn);
            _grid.CellEndEdit += OnCellEndEdit;
            _grid.DataError += OnDataError;

            Controls.Add(_grid);
        }

        public void Initialize(IEditableConfig config, Action saveAction)
        {
            _config = config;
            _saveAction = saveAction;
            LoadConfigToGrid();
        }

        public void RefreshValues()
        {
            if (_config == null)
                return;

            LoadConfigToGrid();
        }

        public void HighlightKeys(IEnumerable<string> keys, Color? highlightColor = null)
        {
            _highlightKeys.Clear();
            if (keys != null)
            {
                foreach (var key in keys)
                {
                    if (!string.IsNullOrWhiteSpace(key))
                        _highlightKeys.Add(key.Trim());
                }
            }

            if (highlightColor.HasValue)
                _highlightColor = highlightColor.Value;

            ApplyHighlights();
        }

        private void LoadConfigToGrid()
        {
            if (_config == null)
                return;

            var readOnlyKeys = (_config as IReadOnlyConfigKeys)?.GetReadOnlyKeys()
                ?? Array.Empty<string>();
            var readOnlySet = new HashSet<string>(readOnlyKeys);

            _isRefreshing = true;
            _grid.Rows.Clear();
            foreach (var key in _config.GetKeys())
            {
                var value = _config.GetValue(key);
                var rowIndex = _grid.Rows.Add(key, FormatValue(value));
                var row = _grid.Rows[rowIndex];
                if (readOnlySet.Contains(key))
                    row.Cells[1].ReadOnly = true;
            }
            _isRefreshing = false;
            ApplyHighlights();
        }

        private void OnCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_isRefreshing || _config == null)
                return;

            if (e.RowIndex < 0 || e.ColumnIndex != 1)
                return;

            var key = _grid.Rows[e.RowIndex].Cells[0].Value?.ToString();
            var rawValue = _grid.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? string.Empty;

            if (!_config.UpdateValue(key, rawValue, out var error))
            {
                Logger.Warning($"Config update failed for {key}: {error}");
                MessageBox.Show(this, error ?? "Invalid value.", "Invalid Value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _grid.Rows[e.RowIndex].Cells[1].Value = FormatValue(_config.GetValue(key));
                return;
            }

            Logger.Info($"Config updated: {key}={rawValue}");
            _saveAction?.Invoke();
        }

        private void OnDataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Logger.Warning($"Config grid data error: {e.Exception?.Message}");
            e.ThrowException = false;
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return string.Empty;

            switch (value)
            {
                case double d:
                    return d.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                case bool b:
                    return b ? "true" : "false";
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        private void ApplyHighlights()
        {
            if (_grid.Rows.Count == 0)
                return;

            foreach (DataGridViewRow row in _grid.Rows)
            {
                var key = row.Cells[0].Value?.ToString();
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var shouldHighlight = _highlightKeys.Contains(key);
                row.Cells[1].Style.BackColor = shouldHighlight ? _highlightColor : SystemColors.Window;
            }
        }
    }
}
