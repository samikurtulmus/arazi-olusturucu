using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using YapiLabCadTools.Core.Geometry;
using YapiLabCadTools.Core.Models;
using YapiLabCadTools.Core.Parsing;
using YapiLabCadTools.Core.Utils;
using YapiLabCadTools.Drawing;
using YapiLabCadTools.Services;

namespace YapiLabCadTools.UI
{
    /// <summary>
    /// Main modeless window: paste/open/edit coordinates in a virtual-mode grid,
    /// see a live preview and draw with one click. Designed so a first-time user
    /// only needs Ctrl+V and the Draw button.
    /// </summary>
    public sealed class MainForm : Form
    {
        private const int MaxUndoDepth = 25;
        private const int PreviewDebounceMs = 300;

        private readonly ICoordinateParser _parser;
        private readonly IDrawingService _drawingService;
        private readonly IFileService _fileService;

        private List<PointRow> _rows = new();
        private readonly List<List<PointRow>> _undoStack = new();
        private string? _rawText;
        private FormatInfo? _lastFormat;

        // Controls
        private ToolStrip _toolbar = null!;
        private ToolStripComboBox _formatCombo = null!;
        private ToolStripButton _undoButton = null!;
        private Label _hintLabel = null!;
        private DataGridView _grid = null!;
        private CheckBox _closeCheck = null!;
        private CheckBox _numbersCheck = null!;
        private CheckBox _markersCheck = null!;
        private ComboBox _symbolCombo = null!;
        private CheckBox _summaryCheck = null!;
        private CheckBox _layerCheck = null!;
        private TextBox _layerNameBox = null!;
        private NumericUpDown _textHeightBox = null!;
        private CheckBox _zoomCheck = null!;
        private Label _previewValues = null!;
        private Button _drawButton = null!;
        private Label _resultLabel = null!;
        private readonly Timer _previewTimer = new() { Interval = PreviewDebounceMs };

        public MainForm(ICoordinateParser parser, IDrawingService drawingService, IFileService fileService)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _drawingService = drawingService ?? throw new ArgumentNullException(nameof(drawingService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));

            BuildLayout();
            _previewTimer.Tick += (_, _) =>
            {
                _previewTimer.Stop();
                UpdatePreview();
            };
            UpdatePreview();
        }

        // ------------------------------------------------------------------ layout

        private void BuildLayout()
        {
            Text = Texts.WindowTitle;
            StartPosition = FormStartPosition.CenterScreen;

            // AutoCAD hosts the modeless dialog in a per-monitor-DPI-aware process; without
            // an explicit DPI AutoScaleMode the form keeps its 96-DPI design size on screens
            // above 100% scaling, so it opens visibly smaller until the user resizes it.
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);

            Size = new Size(940, 760);
            MinimumSize = new Size(860, 640);
            AllowDrop = true;
            KeyPreview = true;
            Theme.Apply(this);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Theme.Background,
                Padding = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            root.Controls.Add(BuildToolbar(), 0, 0);
            root.Controls.Add(BuildHint(), 0, 1);
            root.Controls.Add(BuildGrid(), 0, 2);
            root.Controls.Add(BuildBottomPanel(), 0, 3);
            Controls.Add(root);

            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
        }

        private ToolStrip BuildToolbar()
        {
            _toolbar = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                RenderMode = ToolStripRenderMode.System,
                BackColor = Theme.Surface,
                Padding = new Padding(8, 4, 8, 4),
                ImageScalingSize = new Size(20, 20)
            };

            _toolbar.Items.Add(MakeButton(Texts.OpenFile, (_, _) => OpenFile()));
            _toolbar.Items.Add(MakeButton(Texts.Paste, (_, _) => PasteFromClipboard()));
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(MakeButton(Texts.InsertRow, (_, _) => InsertRow()));
            _toolbar.Items.Add(MakeButton(Texts.DeleteRows, (_, _) => DeleteSelectedRows()));
            _undoButton = MakeButton(Texts.Undo, (_, _) => UndoLast());
            _toolbar.Items.Add(_undoButton);
            _toolbar.Items.Add(MakeButton(Texts.Clear, (_, _) => ClearAll()));
            _toolbar.Items.Add(new ToolStripSeparator());
            _toolbar.Items.Add(new ToolStripLabel(Texts.FormatLabel) { ForeColor = Theme.TextMuted });

            _formatCombo = new ToolStripComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 110,
                FlatStyle = FlatStyle.Flat
            };
            _formatCombo.Items.AddRange(new object[]
            {
                Texts.FormatAuto, Texts.FormatNoYX, Texts.FormatNoXY, Texts.FormatYX, Texts.FormatXY,
                Texts.FormatNoEnlemBoylam, Texts.FormatNoBoylamEnlem
            });
            _formatCombo.ComboBox!.DropDownWidth = 160;
            _formatCombo.SelectedIndex = 0;
            _formatCombo.SelectedIndexChanged += (_, _) => ReparseWithSelectedLayout();
            _toolbar.Items.Add(_formatCombo);

            return _toolbar;
        }

        private static ToolStripButton MakeButton(string text, EventHandler onClick)
        {
            var button = new ToolStripButton(text)
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Margin = new Padding(2, 1, 2, 1)
            };
            button.Click += onClick;
            return button;
        }

        private Label BuildHint()
        {
            _hintLabel = new Label
            {
                Text = Texts.Hint,
                Dock = DockStyle.Fill,
                AutoSize = true,
                ForeColor = Theme.TextMuted,
                BackColor = Theme.Background,
                Padding = new Padding(10, 6, 10, 6)
            };
            return _hintLabel;
        }

        private DataGridView BuildGrid()
        {
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                VirtualMode = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = true,
                ShowCellToolTips = true,
                Margin = new Padding(10, 0, 10, 0),
                StandardTab = true
            };
            Theme.StyleGrid(_grid);

            var noColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = Texts.ColumnNo,
                FillWeight = 22,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            var eastColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = Texts.ColumnEast,
                FillWeight = 39,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            var northColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = Texts.ColumnNorth,
                FillWeight = 39,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            _grid.Columns.AddRange(noColumn, eastColumn, northColumn);

            _grid.CellValueNeeded += OnCellValueNeeded;
            _grid.CellValuePushed += OnCellValuePushed;
            _grid.CellFormatting += OnCellFormatting;
            _grid.CellToolTipTextNeeded += OnCellToolTipTextNeeded;

            return _grid;
        }

        private Control BuildBottomPanel()
        {
            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 3,
                BackColor = Theme.Background,
                Padding = new Padding(8)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            bottom.Controls.Add(BuildOptionsGroup(), 0, 0);
            bottom.Controls.Add(BuildPreviewGroup(), 1, 0);
            bottom.Controls.Add(BuildActionPanel(), 2, 0);
            return bottom;
        }

        private GroupBox BuildOptionsGroup()
        {
            var group = new GroupBox
            {
                Text = Texts.OptionsTitle,
                Dock = DockStyle.Fill,
                Height = 230,
                Padding = new Padding(10, 6, 10, 6)
            };
            Theme.StyleGroup(group);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Font = Theme.BaseFont,
                ForeColor = Theme.TextPrimary
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            _closeCheck = MakeCheck(Texts.OptionClose, true);
            _numbersCheck = MakeCheck(Texts.OptionPointNumbers, true);
            _summaryCheck = MakeCheck(Texts.OptionSummaryText, true);
            _zoomCheck = MakeCheck(Texts.OptionZoom, true);
            _markersCheck = MakeCheck(Texts.OptionMarkers, false);
            _layerCheck = MakeCheck(Texts.OptionCreateLayer, true);

            _symbolCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat
            };
            _symbolCombo.Items.AddRange(new object[]
            {
                Texts.SymbolDot, Texts.SymbolPlus, Texts.SymbolCross, Texts.SymbolCircle
            });
            _symbolCombo.SelectedIndex = 1;

            _layerNameBox = new TextBox { Text = "PARSEL", Dock = DockStyle.Fill };

            _textHeightBox = new NumericUpDown
            {
                Minimum = 0.01M,
                Maximum = 10000M,
                DecimalPlaces = 2,
                Increment = 0.5M,
                Value = 1.0M,
                Dock = DockStyle.Fill
            };

            layout.Controls.Add(_closeCheck, 0, 0);
            layout.Controls.Add(_numbersCheck, 1, 0);
            layout.Controls.Add(_summaryCheck, 0, 1);
            layout.Controls.Add(_zoomCheck, 1, 1);
            layout.Controls.Add(_markersCheck, 0, 2);
            layout.Controls.Add(_symbolCombo, 1, 2);
            layout.Controls.Add(_layerCheck, 0, 3);
            layout.Controls.Add(_layerNameBox, 1, 3);
            layout.Controls.Add(MakeOptionLabel(Texts.TextHeightLabel), 0, 4);
            layout.Controls.Add(_textHeightBox, 1, 4);

            group.Controls.Add(layout);
            return group;
        }

        private static CheckBox MakeCheck(string text, bool isChecked) => new()
        {
            Text = text,
            Checked = isChecked,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        private static Label MakeOptionLabel(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        private GroupBox BuildPreviewGroup()
        {
            var group = new GroupBox
            {
                Text = Texts.PreviewTitle,
                Dock = DockStyle.Fill,
                Height = 230,
                Padding = new Padding(10, 6, 10, 6)
            };
            Theme.StyleGroup(group);

            _previewValues = new Label
            {
                Dock = DockStyle.Fill,
                Font = Theme.BaseFont,
                ForeColor = Theme.TextPrimary,
                Text = string.Empty
            };
            group.Controls.Add(_previewValues);
            return group;
        }

        private Control BuildActionPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _drawButton = new Button
            {
                Text = Texts.DrawButton,
                Dock = DockStyle.Top,
                Height = 48,
                Margin = new Padding(4, 4, 4, 8)
            };
            Theme.StylePrimaryButton(_drawButton);
            _drawButton.Click += (_, _) => DrawNow();

            var resultGroup = new GroupBox
            {
                Text = Texts.ResultTitle,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 6, 10, 6)
            };
            Theme.StyleGroup(resultGroup);

            _resultLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = Theme.BaseFont,
                ForeColor = Theme.TextMuted,
                Text = Texts.ResultIdle
            };
            resultGroup.Controls.Add(_resultLabel);

            panel.Controls.Add(_drawButton, 0, 0);
            panel.Controls.Add(resultGroup, 0, 1);
            return panel;
        }

        // ------------------------------------------------------------ grid virtual mode

        private void OnCellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
            {
                return;
            }

            PointRow row = _rows[e.RowIndex];
            e.Value = e.ColumnIndex switch
            {
                0 => row.No,
                1 => row.East,
                2 => row.North,
                _ => string.Empty
            };
        }

        private void OnCellValuePushed(object? sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
            {
                return;
            }

            PushUndoSnapshot();
            PointRow row = _rows[e.RowIndex];
            string value = e.Value?.ToString() ?? string.Empty;
            switch (e.ColumnIndex)
            {
                case 0: row.No = value; break;
                case 1: row.East = value; break;
                case 2: row.North = value; break;
            }

            row.Revalidate();
            _grid.InvalidateRow(e.RowIndex);
            SchedulePreviewUpdate();
        }

        private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count)
            {
                return;
            }

            if (_rows[e.RowIndex].Error is not null && e.CellStyle is not null)
            {
                e.CellStyle.BackColor = Theme.ErrorBackground;
                e.CellStyle.ForeColor = Theme.ErrorText;
            }
        }

        private void OnCellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _rows.Count)
            {
                e.ToolTipText = _rows[e.RowIndex].Error ?? string.Empty;
            }
        }

        // ------------------------------------------------------------------ actions

        private void OpenFile()
        {
            using var dialog = new OpenFileDialog
            {
                Title = Texts.OpenFileTitle,
                Filter = Texts.OpenFileFilter
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                LoadFile(dialog.FileName);
            }
        }

        private void LoadFile(string path)
        {
            try
            {
                LoadFromText(_fileService.ReadAllText(path));
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                ShowResult(string.Format(Texts.ErrorFileRead, ex.Message), isError: true);
            }
        }

        private void PasteFromClipboard()
        {
            if (!Clipboard.ContainsText())
            {
                ShowResult(Texts.ErrorClipboardEmpty, isError: true);
                return;
            }

            LoadFromText(Clipboard.GetText());
        }

        /// <summary>Parses raw text on a background thread and fills the grid.</summary>
        private async void LoadFromText(string text)
        {
            _rawText = text;
            ColumnLayout layout = SelectedLayoutOverride();
            UseWaitCursor = true;
            try
            {
                ParseResult result = await Task.Run(() => _parser.Parse(text, layout));
                ApplyParseResult(result);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void ReparseWithSelectedLayout()
        {
            if (_rawText is not null)
            {
                LoadFromText(_rawText);
            }
        }

        private ColumnLayout SelectedLayoutOverride() => _formatCombo.SelectedIndex switch
        {
            1 => ColumnLayout.NoYX,
            2 => ColumnLayout.NoXY,
            3 => ColumnLayout.YX,
            4 => ColumnLayout.XY,
            5 => ColumnLayout.NoEnlemBoylam,
            6 => ColumnLayout.NoBoylamEnlem,
            _ => ColumnLayout.Auto
        };

        private void ApplyParseResult(ParseResult result)
        {
            PushUndoSnapshot();
            _rows = result.Rows.Select(PointRow.FromParsed).ToList();
            _lastFormat = result.Format;
            RefreshGrid();
            UpdatePreview();
        }

        private void InsertRow()
        {
            PushUndoSnapshot();
            int index = _grid.CurrentCell?.RowIndex + 1 ?? _rows.Count;
            index = Math.Min(index, _rows.Count);
            var row = new PointRow();
            row.Revalidate();
            _rows.Insert(index, row);
            RefreshGrid();
            SchedulePreviewUpdate();
        }

        private void DeleteSelectedRows()
        {
            List<int> indexes = _grid.SelectedCells
                .Cast<DataGridViewCell>()
                .Select(c => c.RowIndex)
                .Distinct()
                .OrderByDescending(i => i)
                .ToList();

            if (indexes.Count == 0)
            {
                return;
            }

            PushUndoSnapshot();
            foreach (int index in indexes)
            {
                if (index >= 0 && index < _rows.Count)
                {
                    _rows.RemoveAt(index);
                }
            }

            RefreshGrid();
            SchedulePreviewUpdate();
        }

        private void ClearAll()
        {
            if (_rows.Count == 0)
            {
                return;
            }

            PushUndoSnapshot();
            _rows = new List<PointRow>();
            _rawText = null;
            _lastFormat = null;
            RefreshGrid();
            UpdatePreview();
        }

        private void RefreshGrid()
        {
            _grid.CancelEdit();
            _grid.ClearSelection();
            _grid.RowCount = _rows.Count;
            _grid.Invalidate();
        }

        // -------------------------------------------------------------------- undo

        private void PushUndoSnapshot()
        {
            _undoStack.Add(_rows.Select(r => r.Clone()).ToList());
            if (_undoStack.Count > MaxUndoDepth)
            {
                _undoStack.RemoveAt(0);
            }

            _undoButton.Enabled = true;
        }

        private void UndoLast()
        {
            if (_undoStack.Count == 0)
            {
                return;
            }

            _rows = _undoStack[^1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _undoButton.Enabled = _undoStack.Count > 0;
            RefreshGrid();
            UpdatePreview();
        }

        // ------------------------------------------------------------------- preview

        private void SchedulePreviewUpdate()
        {
            _previewTimer.Stop();
            _previewTimer.Start();
        }

        private void UpdatePreview()
        {
            List<CoordinatePoint> points = ValidPoints();
            int errorCount = _rows.Count(r => !r.IsValid);
            GeometryStats stats = PolygonMath.Compute(points, _closeCheck.Checked);

            string bounds = points.Count == 0
                ? Texts.PreviewNone
                : $"{NumberFormatting.Length(stats.Width)} × {NumberFormatting.Length(stats.Height)}";

            string format = _lastFormat is null || _lastFormat.Description.Length == 0
                ? Texts.PreviewNone
                : _lastFormat.Description;

            _previewValues.Text =
                $"{Texts.PreviewPointCount} {points.Count:N0}\r\n" +
                $"{Texts.PreviewArea} {(points.Count >= 3 ? NumberFormatting.Area(stats.Area) : Texts.PreviewNone)}\r\n" +
                $"{Texts.PreviewPerimeter} {(points.Count >= 2 ? NumberFormatting.Length(stats.Perimeter) : Texts.PreviewNone)}\r\n" +
                $"{Texts.PreviewBounds} {bounds}\r\n" +
                $"{Texts.PreviewFormat} {format}\r\n" +
                $"{Texts.PreviewErrors} {errorCount:N0}";

            _drawButton.Text = points.Count > 0
                ? string.Format(Texts.DrawButtonWithCount, points.Count.ToString("N0"))
                : Texts.DrawButton;
            _drawButton.Enabled = points.Count >= 2;
        }

        private List<CoordinatePoint> ValidPoints()
        {
            var points = new List<CoordinatePoint>(_rows.Count);
            foreach (PointRow row in _rows)
            {
                if (row.IsValid)
                {
                    points.Add(row.ToPoint());
                }
            }

            return points;
        }

        // --------------------------------------------------------------------- draw

        private void DrawNow()
        {
            List<CoordinatePoint> points = ValidPoints();
            if (points.Count < 2)
            {
                ShowResult(Texts.ErrorNotEnoughPoints, isError: true);
                return;
            }

            DrawOptions options = BuildDrawOptions();
            _drawButton.Enabled = false;
            try
            {
                DrawResult result = _drawingService.Draw(points, options);
                string message = result.Closed
                    ? string.Format(
                        Texts.ResultSuccessClosed,
                        result.PointCount.ToString("N0"),
                        NumberFormatting.Area(result.Area),
                        NumberFormatting.Length(result.Perimeter),
                        result.LayerName)
                    : string.Format(
                        Texts.ResultSuccessOpen,
                        result.PointCount.ToString("N0"),
                        NumberFormatting.Length(result.Perimeter),
                        result.LayerName);
                ShowResult(message, isError: false);
            }
            catch (Exception ex)
            {
                // The drawing service reports user-facing problems (no open document,
                // AutoCAD runtime errors) via exception messages; the UI must never crash.
                ShowResult(string.Format(Texts.ErrorDrawFailed, ex.Message), isError: true);
            }
            finally
            {
                _drawButton.Enabled = true;
            }
        }

        private DrawOptions BuildDrawOptions() => new()
        {
            ClosePolyline = _closeCheck.Checked,
            DrawPointNumbers = _numbersCheck.Checked,
            DrawPointMarkers = _markersCheck.Checked,
            PointSymbol = (PointSymbol)Math.Max(0, _symbolCombo.SelectedIndex),
            DrawSummaryText = _summaryCheck.Checked,
            CreateLayer = _layerCheck.Checked,
            LayerName = _layerNameBox.Text,
            TextHeight = (double)_textHeightBox.Value,
            ZoomToResult = _zoomCheck.Checked
        };

        private void ShowResult(string message, bool isError)
        {
            _resultLabel.Text = message;
            _resultLabel.ForeColor = isError ? Theme.ErrorText : Theme.SuccessText;
        }

        // ------------------------------------------------------------- drag & drop, keys

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            bool acceptable = e.Data is not null &&
                (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                 e.Data.GetDataPresent(DataFormats.UnicodeText) ||
                 e.Data.GetDataPresent(DataFormats.Text));
            e.Effect = acceptable ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                e.Data.GetData(DataFormats.FileDrop) is string[] files &&
                files.Length > 0)
            {
                LoadFile(files[0]);
                return;
            }

            if (e.Data.GetData(DataFormats.UnicodeText) is string text && text.Length > 0)
            {
                LoadFromText(text);
            }
        }

        /// <summary>Ctrl+V pastes and Ctrl+Z undoes anywhere in the window (outside cell editing).</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!_grid.IsCurrentCellInEditMode)
            {
                if (keyData == (Keys.Control | Keys.V))
                {
                    PasteFromClipboard();
                    return true;
                }

                if (keyData == (Keys.Control | Keys.Z))
                {
                    UndoLast();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _previewTimer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
