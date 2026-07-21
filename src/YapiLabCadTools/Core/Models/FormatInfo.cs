namespace YapiLabCadTools.Core.Models
{
    /// <summary>What the parser detected (or was told) about the source format.</summary>
    public sealed class FormatInfo
    {
        /// <summary>Detected column delimiter.</summary>
        public DelimiterKind Delimiter { get; init; }

        /// <summary>Resolved column layout — never <see cref="ColumnLayout.Auto"/>.</summary>
        public ColumnLayout Layout { get; init; }

        /// <summary>True when the list has a leading point number / name column.</summary>
        public bool HasLabelColumn { get; init; }

        /// <summary>Number of leading header/title lines that were skipped.</summary>
        public int SkippedHeaderLines { get; init; }

        /// <summary>True when the numbers use a decimal comma (e.g. "4234567,89").</summary>
        public bool UsesDecimalComma { get; init; }

        /// <summary>True when the source held WGS84 latitude/longitude degrees, converted to UTM meters.</summary>
        public bool IsGeographicSource { get; init; }

        /// <summary>UTM zone the coordinates were converted into, when <see cref="IsGeographicSource"/> is true.</summary>
        public int? UtmZone { get; init; }

        /// <summary>
        /// Detection confidence in [0, 1]. 1.0 = user override, ~0.95 = coordinate ranges
        /// clearly identified easting/northing, 0.5 = fell back to the Y-X (Sağa-Yukarı) default.
        /// </summary>
        public double Confidence { get; init; }

        /// <summary>Human-readable Turkish summary shown in the preview panel.</summary>
        public string Description { get; init; } = string.Empty;
    }
}
