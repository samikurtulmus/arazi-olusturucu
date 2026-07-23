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
        private const int SidebarWidth = 360;

        private readonly ICoordinateParser _parser;
        private readonly IDrawingService _drawingService;
        private readonly IFileService _fileService;

        private List<PointRow> _rows = new();
        private int[] _groupIndexes = Array.Empty<int>();
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
        private readonly Timer _windowStateTimer = new() { Interval = 400 };

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

            // Sizing is deliberately NOT driven by a fixed design-time constant plus
            // AutoScaleMode.Dpi's automatic scale-up: in AutoCAD's hosted, per-monitor-DPI
            // process that multiplication has proven unpredictable in practice (a modest
            // constant has come out both too small and, after "fixing" that, far too large).
            // Instead every size decision below is taken from Screen.WorkingArea — the real,
            // measured screen the window is actually opening on — so nothing depends on
            // predicting how the framework's DPI scaling behaves in this host.
            //
            // The form is rebuilt from scratch on every AutoCAD session/NETLOAD, so without the
            // restore it forgets any size the user dragged it to and reopens at the default
            // every time.
            Load += (_, _) =>
            {
                if (!WindowStateStore.TryRestore(this))
                {
                    ApplyDefaultSize();
                }

                ClampToWorkingArea();
            };

            // The screen Load sees isn't always the one the window ends up centered/placed on
            // (final placement happens slightly later in the Show pipeline); re-checking once
            // more after the window is actually visible catches that edge case cheaply.
            Shown += (_, _) => ClampToWorkingArea();

            _windowStateTimer.Tick += (_, _) =>
            {
                _windowStateTimer.Stop();
                WindowStateStore.Save(this);
            };

            // Passive only: shrink back only if the window is genuinely bigger than the screen
            // it's on. This must never actively fight a resize in progress — an earlier version
            // tried to repeatedly re-assert a remembered "correct" size for a few seconds after
            // open, and that fought the user's own manual drag-resize too (a resize attempt could
            // get silently reverted within a few hundred ms, making the window feel impossible to
            // enlarge). Whatever the original oversizing cause was, undoing the user's own actions
            // is worse, so this only ever caps genuine overflow, never re-imposes a target.
            Resize += (_, _) =>
            {
                ClampToWorkingArea();
                ScheduleWindowStateSave();
            };
            Move += (_, _) => ScheduleWindowStateSave();
            FormClosing += (_, _) => WindowStateStore.Save(this);
        }

        private void ScheduleWindowStateSave()
        {
            if (WindowState == FormWindowState.Minimized)
            {
                return;
            }

            _windowStateTimer.Stop();
            _windowStateTimer.Start();
        }

        /// <summary>
        /// First-run size: a comfortable fraction of the real screen's working area, capped at a
        /// sane absolute maximum — not a fixed pixel constant, so nothing depends on predicting
        /// how AutoScaleMode.Dpi's scale-up behaves in this AutoCAD-hosted process.
        /// </summary>
        private void ApplyDefaultSize()
        {
            Rectangle working = Screen.FromControl(this).WorkingArea;
            int width = Math.Clamp((int)(working.Width * 0.55), MinimumSize.Width, 1100);
            int height = Math.Clamp((int)(working.Height * 0.70), MinimumSize.Height, 820);
            Size = new Size(width, height);
            CenterToScreen();
        }

        /// <summary>
        /// Hard, unconditional safety net: the window can never end up bigger than the screen
        /// it is actually opening on, whether the cause is DPI auto-scaling, a size restored
        /// from a different/bigger monitor, or anything else.
        /// </summary>
        private void ClampToWorkingArea()
        {
            Rectangle working = Screen.FromControl(this).WorkingArea;
            int width = Math.Min(Width, working.Width);
            int height = Math.Min(Height, working.Height);
            if (width != Width || height != Height)
            {
                Size = new Size(width, height);
            }

            int x = Math.Max(working.Left, Math.Min(Left, working.Right - Width));
            int y = Math.Max(working.Top, Math.Min(Top, working.Bottom - Height));
            Location = new Point(x, y);
        }

        // ------------------------------------------------------------------ layout

        private void BuildLayout()
        {
            Text = Texts.WindowTitle;
            StartPosition = FormStartPosition.CenterScreen;

            // Deliberately no AutoScaleMode.Dpi here. AutoCAD 2027 is itself a modern,
            // per-monitor-v2 DPI-aware host, which already auto-scales fonts/controls on its
            // own; layering WinForms' legacy AutoScaleMode.Dpi on top of that competes with it
            // and rescales our explicitly computed Size a second time (this is what actually
            // caused the window to keep coming out oversized / spilling off-screen — not a
            // one-off bug in the sizing math, but two DPI-scaling systems fighting each other).
            // All sizing below is computed once, in real screen pixels, with nothing left to
            // rescale it afterward.
            AutoScaleMode = AutoScaleMode.None;

            MinimumSize = new Size(880, 640);
            AllowDrop = true;
            KeyPreview = true;
            Theme.Apply(this);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Theme.Background,
                Padding = new Padding(0)
            };
            // THE critical style. Without an explicit Percent width the single column is
            // auto-sized, meaning it grows to the widest child's *preferred* width — and the
            // one-line hint label prefers ~1900px. Every row then gets laid out on that
            // phantom width, which pushed the fixed-width sidebar (and earlier, the bottom
            // panel's right column) past the form's right edge no matter how wide the user
            // dragged the window. This was the real cause of the "window never fits" saga.
            // With Percent 100 the column is exactly the form's width and the hint wraps.
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            root.Controls.Add(BuildToolbar(), 0, 0);
            root.Controls.Add(BuildHint(), 0, 1);
            root.Controls.Add(BuildBody(), 0, 2);
            Controls.Add(root);

            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
        }

        /// <summary>
        /// Coordinate grid on the left (takes whatever width is left), a fixed-width sidebar
        /// (options/preview/draw/result, stacked) on the right. A fixed sidebar width means the
        /// draw button and result panel always have the same comfortable size regardless of the
        /// window's overall width — unlike the previous 3-column percentage-based bottom bar,
        /// nothing here depends on the total window width being "just right".
        /// </summary>
        private Control BuildBody()
        {
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.Background
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SidebarWidth));

            body.Controls.Add(BuildGrid(), 0, 0);
            body.Controls.Add(BuildSidebar(), 1, 0);
            return body;
        }

        private Control BuildSidebar()
        {
            var sidebar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Theme.Background,
                Padding = new Padding(0, 0, 8, 8)
            };
            sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            sidebar.Controls.Add(BuildOptionsGroup(), 0, 0);
            sidebar.Controls.Add(BuildPreviewGroup(), 0, 1);
            sidebar.Controls.Add(BuildDrawButton(), 0, 2);
            sidebar.Controls.Add(BuildResultGroup(), 0, 3);
            return sidebar;
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

        private GroupBox BuildOptionsGroup()
        {
            var group = new GroupBox
            {
                Text = Texts.OptionsTitle,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 6, 10, 6)
            };
            Theme.StyleGroup(group);

            // Each checkbox gets its own full-width row (spanning both columns); only the three
            // label/control pairs (marker symbol, layer name, text height) use the two columns
            // side by side. A single narrow sidebar column has no room for the old "two
            // checkboxes side by side" layout without wrapping text awkwardly.
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 7,
                Font = Theme.BaseFont,
                ForeColor = Theme.TextPrimary
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            for (int i = 0; i < 7; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

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
            layout.SetColumnSpan(_closeCheck, 2);
            layout.Controls.Add(_numbersCheck, 0, 1);
            layout.SetColumnSpan(_numbersCheck, 2);
            layout.Controls.Add(_summaryCheck, 0, 2);
            layout.SetColumnSpan(_summaryCheck, 2);
            layout.Controls.Add(_zoomCheck, 0, 3);
            layout.SetColumnSpan(_zoomCheck, 2);
            layout.Controls.Add(_markersCheck, 0, 4);
            layout.Controls.Add(_symbolCombo, 1, 4);
            layout.Controls.Add(_layerCheck, 0, 5);
            layout.Controls.Add(_layerNameBox, 1, 5);
            layout.Controls.Add(MakeOptionLabel(Texts.TextHeightLabel), 0, 6);
            layout.Controls.Add(_textHeightBox, 1, 6);

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
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 6, 10, 6)
            };
            Theme.StyleGroup(group);

            // AutoSize (not Dock=Fill): inside an AutoSize group a docked label reports zero
            // preferred size, collapsing the whole group to just its title. MaximumSize caps the
            // width at what fits the fixed sidebar, so long lines (e.g. the detected-format
            // description) wrap instead of getting clipped at the group's edge.
            _previewValues = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                MaximumSize = new Size(SidebarWidth - 40, 0),
                Font = Theme.BaseFont,
                ForeColor = Theme.TextPrimary,
                Text = string.Empty
            };
            group.Controls.Add(_previewValues);
            return group;
        }

        private Button BuildDrawButton()
        {
            _drawButton = new Button
            {
                Text = Texts.DrawButton,
                Dock = DockStyle.Top,
                Height = 48,
                Margin = new Padding(4, 8, 4, 8)
            };
            Theme.StylePrimaryButton(_drawButton);
            _drawButton.Click += (_, _) => DrawNow();
            return _drawButton;
        }

        private GroupBox BuildResultGroup()
        {
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
            return resultGroup;
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
            // Editing "No" can move shape boundaries for every row below it, so recompute
            // grouping and repaint the whole grid rather than just this one row.
            RecomputeGroups();
            _grid.Invalidate();
            SchedulePreviewUpdate();
        }

        private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count || e.CellStyle is null)
            {
                return;
            }

            if (_rows[e.RowIndex].Error is not null)
            {
                e.CellStyle.BackColor = Theme.ErrorBackground;
                e.CellStyle.ForeColor = Theme.ErrorText;
                return;
            }

            // Shade alternate detected shapes (e.g. building footprints after a parcel
            // boundary) so a multi-shape paste is visible before drawing, not a surprise after.
            if (e.RowIndex < _groupIndexes.Length && _groupIndexes[e.RowIndex] % 2 == 1)
            {
                e.CellStyle.BackColor = Theme.GroupBandBackground;
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
            // A TKGM "parsel sorgu" GeoJSON export (or any Polygon/MultiPolygon FeatureCollection)
            // gets converted into the same tabular "No Enlem Boylam" text a manual paste would
            // produce, so the rest of the pipeline — parsing, UTM conversion, shape grouping,
            // undo, format re-detection — handles it with no special-casing beyond this line.
            if (GeoJsonParcelReader.TryConvert(text, out string converted))
            {
                text = converted;
            }

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
            RecomputeGroups();
            _grid.CancelEdit();
            _grid.ClearSelection();
            _grid.RowCount = _rows.Count;
            _grid.Invalidate();
        }

        private void RecomputeGroups()
        {
            _groupIndexes = PointGrouping.AssignGroupIndexes(_rows.Select(r => r.No).ToList());
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
            List<List<CoordinatePoint>> groups = ValidPointGroups();
            List<CoordinatePoint> allPoints = groups.SelectMany(g => g).ToList();
            int errorCount = _rows.Count(r => !r.IsValid);

            // The bounding box covers everything (order-independent), but area/perimeter are
            // only meaningful for one ring, so those come from the primary (largest) shape —
            // the same shape the draw step will report as the parcel boundary.
            List<CoordinatePoint> primary = groups.Count == 0
                ? new List<CoordinatePoint>()
                : groups.OrderByDescending(g => g.Count).First();

            GeometryStats bbox = PolygonMath.Compute(allPoints, closed: false);
            GeometryStats primaryStats = PolygonMath.Compute(primary, _closeCheck.Checked);

            string bounds = allPoints.Count == 0
                ? Texts.PreviewNone
                : $"{NumberFormatting.Length(bbox.Width)} × {NumberFormatting.Length(bbox.Height)}";

            string format = _lastFormat is null || _lastFormat.Description.Length == 0
                ? Texts.PreviewNone
                : _lastFormat.Description;

            string shapeCountLine = groups.Count > 1
                ? $"\r\n{Texts.PreviewShapeCount} {groups.Count:N0}"
                : string.Empty;

            _previewValues.Text =
                $"{Texts.PreviewPointCount} {allPoints.Count:N0}\r\n" +
                $"{Texts.PreviewArea} {(primary.Count >= 3 ? NumberFormatting.Area(primaryStats.Area) : Texts.PreviewNone)}\r\n" +
                $"{Texts.PreviewPerimeter} {(primary.Count >= 2 ? NumberFormatting.Length(primaryStats.Perimeter) : Texts.PreviewNone)}\r\n" +
                $"{Texts.PreviewBounds} {bounds}\r\n" +
                $"{Texts.PreviewFormat} {format}\r\n" +
                $"{Texts.PreviewErrors} {errorCount:N0}" +
                shapeCountLine;

            _drawButton.Text = allPoints.Count > 0
                ? string.Format(Texts.DrawButtonWithCount, allPoints.Count.ToString("N0"))
                : Texts.DrawButton;
            _drawButton.Enabled = groups.Count > 0;
        }

        /// <summary>
        /// Valid points, split into separate shapes wherever <see cref="_groupIndexes"/> marks a
        /// new group (see <see cref="PointGrouping"/>). Groups left with fewer than 2 usable
        /// points after invalid rows are filtered out are dropped entirely.
        /// </summary>
        private List<List<CoordinatePoint>> ValidPointGroups()
        {
            var groups = new List<List<CoordinatePoint>>();
            for (int i = 0; i < _rows.Count; i++)
            {
                PointRow row = _rows[i];
                if (!row.IsValid)
                {
                    continue;
                }

                int groupIndex = i < _groupIndexes.Length ? _groupIndexes[i] : 0;
                while (groups.Count <= groupIndex)
                {
                    groups.Add(new List<CoordinatePoint>());
                }

                groups[groupIndex].Add(row.ToPoint());
            }

            return groups.Where(g => g.Count >= 2).ToList();
        }

        // --------------------------------------------------------------------- draw

        private void DrawNow()
        {
            List<List<CoordinatePoint>> groups = ValidPointGroups();
            if (groups.Count == 0)
            {
                ShowResult(Texts.ErrorNotEnoughPoints, isError: true);
                return;
            }

            DrawOptions options = BuildDrawOptions();
            _drawButton.Enabled = false;
            try
            {
                DrawResult result = _drawingService.Draw(groups, options);
                ShowResult(BuildResultMessage(result), isError: false);
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

        private static string BuildResultMessage(DrawResult result)
        {
            if (result.ShapeCount <= 1)
            {
                return result.Closed
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
            }

            return result.Closed
                ? string.Format(
                    Texts.ResultSuccessClosedMulti,
                    result.PointCount.ToString("N0"),
                    result.ShapeCount.ToString("N0"),
                    NumberFormatting.Area(result.Area),
                    NumberFormatting.Length(result.Perimeter),
                    result.LayerName,
                    result.SecondaryLayerName)
                : string.Format(
                    Texts.ResultSuccessOpenMulti,
                    result.PointCount.ToString("N0"),
                    result.ShapeCount.ToString("N0"),
                    NumberFormatting.Length(result.Perimeter),
                    result.LayerName,
                    result.SecondaryLayerName);
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
                _windowStateTimer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
