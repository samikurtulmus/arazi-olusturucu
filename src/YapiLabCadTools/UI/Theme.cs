using System.Drawing;
using System.Windows.Forms;

namespace YapiLabCadTools.UI
{
    /// <summary>
    /// Central visual style: a flat, light, Office-like look instead of the classic
    /// AutoCAD gray dialogs. All colors and fonts live here so the UI stays consistent.
    /// </summary>
    public static class Theme
    {
        public static readonly Color Background = Color.White;
        public static readonly Color Surface = Color.FromArgb(246, 248, 250);
        public static readonly Color Border = Color.FromArgb(214, 220, 229);
        public static readonly Color Accent = Color.FromArgb(37, 99, 235);
        public static readonly Color AccentDark = Color.FromArgb(29, 78, 216);
        public static readonly Color TextPrimary = Color.FromArgb(31, 41, 55);
        public static readonly Color TextMuted = Color.FromArgb(107, 114, 128);
        public static readonly Color ErrorBackground = Color.FromArgb(254, 226, 226);
        public static readonly Color ErrorText = Color.FromArgb(153, 27, 27);
        public static readonly Color SuccessText = Color.FromArgb(21, 128, 61);
        public static readonly Color GridAlternate = Color.FromArgb(249, 250, 251);
        public static readonly Color GroupBandBackground = Color.FromArgb(237, 242, 255);

        public static readonly Font BaseFont = new("Segoe UI", 9.5F);
        public static readonly Font BoldFont = new("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font ButtonFont = new("Segoe UI Semibold", 11F, FontStyle.Bold);

        /// <summary>Applies the base look to a form.</summary>
        public static void Apply(Form form)
        {
            form.BackColor = Background;
            form.ForeColor = TextPrimary;
            form.Font = BaseFont;
        }

        /// <summary>Styles the primary action button (the big Draw button).</summary>
        public static void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentDark;
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.Font = ButtonFont;
            button.Cursor = Cursors.Hand;
        }

        /// <summary>Styles the coordinate grid.</summary>
        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Background;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Border;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Surface;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Surface;
            grid.ColumnHeadersHeight = 32;
            grid.RowTemplate.Height = 26;
            grid.DefaultCellStyle.BackColor = Background;
            grid.DefaultCellStyle.ForeColor = TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.BackColor = GridAlternate;
            grid.RowHeadersVisible = false;
        }

        /// <summary>Styles a group box used for the options/preview/result panels.</summary>
        public static void StyleGroup(GroupBox group)
        {
            group.ForeColor = TextMuted;
            group.Font = BoldFont;
        }
    }
}
