using System.Collections.Generic;
using System.Linq;

namespace YapiLabCadTools.Core.Models
{
    /// <summary>Complete outcome of parsing a coordinate text.</summary>
    public sealed class ParseResult
    {
        /// <summary>All data rows in source order, including invalid ones.</summary>
        public IReadOnlyList<ParsedRow> Rows { get; }

        /// <summary>Format detection details.</summary>
        public FormatInfo Format { get; }

        /// <summary>Coordinates of the valid rows, in source order.</summary>
        public IReadOnlyList<CoordinatePoint> ValidPoints { get; }

        /// <summary>Number of rows that could not be parsed.</summary>
        public int ErrorCount { get; }

        public ParseResult(IReadOnlyList<ParsedRow> rows, FormatInfo format)
        {
            Rows = rows;
            Format = format;
            ValidPoints = rows
                .Where(r => r.IsValid)
                .Select(r => new CoordinatePoint(r.East!.Value, r.North!.Value, r.LabelText))
                .ToList();
            ErrorCount = rows.Count(r => !r.IsValid);
        }

        /// <summary>An empty result (no input text / no data lines).</summary>
        public static ParseResult Empty { get; } = new(
            new List<ParsedRow>(),
            new FormatInfo { Layout = ColumnLayout.YX, Confidence = 0, Description = "Veri yok" });
    }
}
