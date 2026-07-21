using YapiLabCadTools.Core.Models;
using YapiLabCadTools.Core.Parsing;

namespace YapiLabCadTools.UI
{
    /// <summary>
    /// One editable row of the coordinate grid. Keeps the raw cell text (so the user
    /// sees exactly what was pasted) together with the parsed values and any error.
    /// </summary>
    public sealed class PointRow
    {
        /// <summary>Point number / name cell text.</summary>
        public string No { get; set; } = string.Empty;

        /// <summary>Easting (Y/Sağa) cell text.</summary>
        public string East { get; set; } = string.Empty;

        /// <summary>Northing (X/Yukarı) cell text.</summary>
        public string North { get; set; } = string.Empty;

        /// <summary>Parsed easting, when valid.</summary>
        public double? EastValue { get; private set; }

        /// <summary>Parsed northing, when valid.</summary>
        public double? NorthValue { get; private set; }

        /// <summary>Turkish error text for invalid rows; null when the row is valid.</summary>
        public string? Error { get; private set; }

        /// <summary>True when both coordinates are usable numbers.</summary>
        public bool IsValid => Error is null && EastValue.HasValue && NorthValue.HasValue;

        /// <summary>Creates a grid row from a parser row, keeping its raw text and error.</summary>
        public static PointRow FromParsed(ParsedRow parsed) => new()
        {
            No = parsed.LabelText ?? string.Empty,
            East = parsed.EastText,
            North = parsed.NorthText,
            EastValue = parsed.East,
            NorthValue = parsed.North,
            Error = parsed.Error
        };

        /// <summary>Re-parses the cell text after a manual edit.</summary>
        public void Revalidate()
        {
            bool eastEmpty = string.IsNullOrWhiteSpace(East);
            bool northEmpty = string.IsNullOrWhiteSpace(North);
            bool eastOk = NumberParser.TryParse(East, out double east);
            bool northOk = NumberParser.TryParse(North, out double north);

            EastValue = eastOk ? east : null;
            NorthValue = northOk ? north : null;

            if (eastEmpty && northEmpty)
            {
                Error = "Koordinatlar boş";
            }
            else if (!eastOk)
            {
                Error = eastEmpty
                    ? "Y (Sağa) değeri boş"
                    : $"Y (Sağa) değeri sayı değil: '{East}'";
            }
            else if (!northOk)
            {
                Error = northEmpty
                    ? "X (Yukarı) değeri boş"
                    : $"X (Yukarı) değeri sayı değil: '{North}'";
            }
            else
            {
                Error = null;
            }
        }

        /// <summary>Deep copy for undo snapshots.</summary>
        public PointRow Clone() => new()
        {
            No = No,
            East = East,
            North = North,
            EastValue = EastValue,
            NorthValue = NorthValue,
            Error = Error
        };

        /// <summary>The CAD coordinate of a valid row.</summary>
        public CoordinatePoint ToPoint() =>
            new(EastValue!.Value, NorthValue!.Value, string.IsNullOrWhiteSpace(No) ? null : No);
    }
}
