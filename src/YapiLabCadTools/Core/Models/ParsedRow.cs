namespace YapiLabCadTools.Core.Models
{
    /// <summary>
    /// One row of the source coordinate list after parsing. Invalid rows are kept
    /// (with <see cref="Error"/> set) so the UI can show and let the user fix them.
    /// </summary>
    public sealed class ParsedRow
    {
        /// <summary>1-based line number in the original text (for error reporting).</summary>
        public int LineNumber { get; init; }

        /// <summary>Raw point number / name text, if the list has a label column.</summary>
        public string? LabelText { get; init; }

        /// <summary>
        /// Raw easting (Y/Sağa) token as it appeared in the source. For a WGS84 Enlem/Boylam
        /// source this holds the converted UTM easting instead, formatted in meters.
        /// </summary>
        public string EastText { get; init; } = string.Empty;

        /// <summary>
        /// Raw northing (X/Yukarı) token as it appeared in the source. For a WGS84 Enlem/Boylam
        /// source this holds the converted UTM northing instead, formatted in meters.
        /// </summary>
        public string NorthText { get; init; } = string.Empty;

        /// <summary>Parsed easting value, if the token was a valid number.</summary>
        public double? East { get; init; }

        /// <summary>Parsed northing value, if the token was a valid number.</summary>
        public double? North { get; init; }

        /// <summary>Turkish, user-facing description of what is wrong with this row; null when valid.</summary>
        public string? Error { get; init; }

        /// <summary>True when both coordinates parsed successfully and no structural error exists.</summary>
        public bool IsValid => Error is null && East.HasValue && North.HasValue;
    }
}
