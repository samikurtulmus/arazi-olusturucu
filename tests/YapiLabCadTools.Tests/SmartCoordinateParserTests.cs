using System.Linq;
using Xunit;
using YapiLabCadTools.Core.Models;
using YapiLabCadTools.Core.Parsing;

namespace YapiLabCadTools.Tests
{
    /// <summary>
    /// End-to-end tests of the parsing pipeline against every input format listed in
    /// the product specification. Coordinates use realistic Turkish TM values:
    /// easting (Y/Sağa) ≈ 456,789 and northing (X/Yukarı) ≈ 4,423,456.
    /// CAD convention throughout: CoordinatePoint.X = easting, CoordinatePoint.Y = northing.
    /// </summary>
    public class SmartCoordinateParserTests
    {
        private readonly SmartCoordinateParser _parser = new();

        [Fact]
        public void Parse_NoYX_TabSeparated()
        {
            const string text = "1\t456789.12\t4423456.78\n2\t456800.50\t4423460.10\n3\t456810.00\t4423440.00";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(ColumnLayout.NoYX, result.Format.Layout);
            Assert.Equal(DelimiterKind.Tab, result.Format.Delimiter);
            Assert.True(result.Format.HasLabelColumn);
            Assert.Equal(0, result.ErrorCount);
            Assert.Equal(3, result.ValidPoints.Count);
            Assert.Equal(456789.12, result.ValidPoints[0].X, 6);
            Assert.Equal(4423456.78, result.ValidPoints[0].Y, 6);
            Assert.Equal("1", result.ValidPoints[0].Label);
            Assert.True(result.Format.Confidence >= 0.9);
        }

        [Fact]
        public void Parse_NoXY_NorthingFirst_IsSwappedIntoCadAxes()
        {
            const string text = "1 4423456.78 456789.12\n2 4423460.10 456800.50";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(ColumnLayout.NoXY, result.Format.Layout);
            // Easting must still land on CAD X even though it was the last column.
            Assert.Equal(456789.12, result.ValidPoints[0].X, 6);
            Assert.Equal(4423456.78, result.ValidPoints[0].Y, 6);
        }

        [Fact]
        public void Parse_YX_WithoutLabelColumn()
        {
            const string text = "456789.12 4423456.78\n456800.50 4423460.10";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(ColumnLayout.YX, result.Format.Layout);
            Assert.False(result.Format.HasLabelColumn);
            Assert.Null(result.ValidPoints[0].Label);
            Assert.Equal(456789.12, result.ValidPoints[0].X, 6);
        }

        [Fact]
        public void Parse_XY_NorthingFirst_WithoutLabelColumn()
        {
            const string text = "4423456.78 456789.12\n4423460.10 456800.50";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(ColumnLayout.XY, result.Format.Layout);
            Assert.Equal(456789.12, result.ValidPoints[0].X, 6);
            Assert.Equal(4423456.78, result.ValidPoints[0].Y, 6);
        }

        [Fact]
        public void Parse_CommaCsv()
        {
            const string text = "1,456789.12,4423456.78\n2,456800.50,4423460.10";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(DelimiterKind.Comma, result.Format.Delimiter);
            Assert.Equal(ColumnLayout.NoYX, result.Format.Layout);
            Assert.Equal(2, result.ValidPoints.Count);
        }

        [Fact]
        public void Parse_SemicolonCsv_WithDecimalComma()
        {
            const string text = "1;456789,12;4423456,78\n2;456800,50;4423460,10";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(DelimiterKind.Semicolon, result.Format.Delimiter);
            Assert.True(result.Format.UsesDecimalComma);
            Assert.Equal(456789.12, result.ValidPoints[0].X, 6);
        }

        [Fact]
        public void Parse_SpaceSeparated_DecimalComma()
        {
            const string text = "456789,12 4423456,78\n456800,50 4423460,10";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(DelimiterKind.Whitespace, result.Format.Delimiter);
            Assert.True(result.Format.UsesDecimalComma);
            Assert.Equal(456789.12, result.ValidPoints[0].X, 6);
            Assert.Equal(4423456.78, result.ValidPoints[0].Y, 6);
        }

        [Fact]
        public void Parse_MixedAndMultipleSpaces()
        {
            const string text = "1   456789.12 \t 4423456.78\n2      456800.50   4423460.10";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(2, result.ValidPoints.Count);
            Assert.Equal(0, result.ErrorCount);
        }

        [Fact]
        public void Parse_SkipsTitleAndHeaderLines()
        {
            const string text =
                "PARSEL KÖŞE KOORDİNATLARI\n" +
                "\n" +
                "Nokta No\tY (Sağa)\tX (Yukarı)\n" +
                "1\t456789.12\t4423456.78\n" +
                "2\t456800.50\t4423460.10\n";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(2, result.Format.SkippedHeaderLines);
            Assert.Equal(2, result.ValidPoints.Count);
            Assert.Equal(0, result.ErrorCount);
        }

        [Fact]
        public void Parse_EmptyLines_AreIgnored()
        {
            const string text = "\n1 456789.12 4423456.78\n\n\n2 456800.50 4423460.10\n\n";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(2, result.ValidPoints.Count);
            Assert.Equal(0, result.ErrorCount);
        }

        [Fact]
        public void Parse_InvalidCell_ReportedNotThrown()
        {
            const string text = "1 456789.12 4423456.78\n2 abc 4423460.10\n3 456810.00 4423440.00";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(1, result.ErrorCount);
            Assert.Equal(2, result.ValidPoints.Count);
            ParsedRow bad = result.Rows.Single(r => !r.IsValid);
            Assert.Equal(2, bad.LineNumber);
            Assert.Contains("Sağa", bad.Error);
        }

        [Fact]
        public void Parse_GarbageLineAfterData_ReportedAsInvalidRow()
        {
            const string text = "1 456789.12 4423456.78\n???\n2 456800.50 4423460.10";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(1, result.ErrorCount);
            Assert.Equal(2, result.ValidPoints.Count);
            Assert.Equal(2, result.Rows.Single(r => !r.IsValid).LineNumber);
        }

        [Fact]
        public void Parse_AlphanumericPointNames()
        {
            const string text = "P1 456789.12 4423456.78\nP2 456800.50 4423460.10\nZK-3 456810.00 4423440.00";

            ParseResult result = _parser.Parse(text);

            Assert.True(result.Format.HasLabelColumn);
            Assert.Equal(0, result.ErrorCount);
            Assert.Equal("P1", result.ValidPoints[0].Label);
            Assert.Equal("ZK-3", result.ValidPoints[2].Label);
        }

        [Fact]
        public void Parse_LocalCoordinates_DefaultsToEastingFirst()
        {
            const string text = "100.00 200.00\n150.00 200.00\n150.00 260.00\n100.00 260.00";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(ColumnLayout.YX, result.Format.Layout);
            Assert.Equal(0.5, result.Format.Confidence, 3);
            Assert.Equal(100.00, result.ValidPoints[0].X, 6);
        }

        [Fact]
        public void Parse_XYZ_WithoutLabel_IgnoresThirdColumn()
        {
            const string text = "456789.12 4423456.78 912.35\n456800.50 4423460.10 913.10";

            ParseResult result = _parser.Parse(text);

            Assert.False(result.Format.HasLabelColumn);
            Assert.Equal(2, result.ValidPoints.Count);
            Assert.Equal(456789.12, result.ValidPoints[0].X, 6);
        }

        [Fact]
        public void Parse_NoXYZ_FourColumns_IgnoresElevation()
        {
            const string text = "1 4423456.78 456789.12 912.35\n2 4423460.10 456800.50 913.10";

            ParseResult result = _parser.Parse(text);

            Assert.True(result.Format.HasLabelColumn);
            Assert.Equal(ColumnLayout.NoXY, result.Format.Layout);
            Assert.Equal(456789.12, result.ValidPoints[0].X, 6);
        }

        [Fact]
        public void Parse_ManualLayoutOverride_BeatsAutoDetection()
        {
            // Values say "easting first", but the user insists on No X Y.
            const string text = "1 456789.12 4423456.78";

            ParseResult result = _parser.Parse(text, ColumnLayout.NoXY);

            Assert.Equal(ColumnLayout.NoXY, result.Format.Layout);
            Assert.Equal(1.0, result.Format.Confidence, 3);
            // First value is now treated as northing → CAD Y.
            Assert.Equal(4423456.78, result.ValidPoints[0].X, 6);
            Assert.Equal(456789.12, result.ValidPoints[0].Y, 6);
        }

        [Fact]
        public void Parse_NullOrWhitespace_ReturnsEmpty()
        {
            Assert.Empty(_parser.Parse(null).Rows);
            Assert.Empty(_parser.Parse("").Rows);
            Assert.Empty(_parser.Parse("   \n  \n").Rows);
        }

        [Fact]
        public void Parse_OnlyText_NoDataRows()
        {
            ParseResult result = _parser.Parse("Bu bir koordinat listesi değil.\nSadece metin.");

            Assert.Empty(result.ValidPoints);
        }

        [Fact]
        public void Parse_ExcelPaste_TabsAndTrailingNewline()
        {
            // Excel copies cells as tab-separated lines with a trailing CRLF.
            const string text = "1\t456789,12\t4423456,78\r\n2\t456800,50\t4423460,10\r\n";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(DelimiterKind.Tab, result.Format.Delimiter);
            Assert.Equal(2, result.ValidPoints.Count);
            Assert.True(result.Format.UsesDecimalComma);
        }

        [Fact]
        public void Parse_MissingColumn_ReportedPerRow()
        {
            const string text = "1 456789.12 4423456.78\n2 456800.50\n3 456810.00 4423440.00";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(1, result.ErrorCount);
            Assert.Contains("Eksik kolon", result.Rows.Single(r => !r.IsValid).Error);
        }

        [Fact]
        public void Parse_LargeInput_100kRows()
        {
            var sb = new System.Text.StringBuilder(4_000_000);
            for (int i = 0; i < 100_000; i++)
            {
                sb.Append(i + 1).Append('\t')
                  .Append((456000 + i * 0.05).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append('\t')
                  .Append((4423000 + i * 0.03).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            }

            ParseResult result = _parser.Parse(sb.ToString());

            Assert.Equal(100_000, result.ValidPoints.Count);
            Assert.Equal(0, result.ErrorCount);
        }

        [Fact]
        public void Parse_RowOrder_IsPreserved()
        {
            const string text = "5 456789.12 4423456.78\n3 456800.50 4423460.10\n9 456810.00 4423440.00";

            ParseResult result = _parser.Parse(text);

            Assert.Equal(new[] { "5", "3", "9" }, result.ValidPoints.Select(p => p.Label));
        }

        // ---------------------------------------------------------- WGS84 Enlem/Boylam

        [Fact]
        public void Parse_EnlemBoylam_WithHeader_ConvertsToUtmMeters()
        {
            const string text =
                "No\tEnlem\tBoylam\n" +
                "1\t41.1886\t28.8750\n" +
                "2\t41.1890\t28.8747\n" +
                "3\t41.1890\t28.8745\n" +
                "4\t41.1884\t28.8745\n" +
                "5\t41.1885\t28.8751\n" +
                "6\t41.1886\t28.8750\n";

            ParseResult result = _parser.Parse(text);

            Assert.True(result.Format.IsGeographicSource);
            Assert.Equal(ColumnLayout.NoEnlemBoylam, result.Format.Layout);
            Assert.Equal(35, result.Format.UtmZone);
            Assert.Equal(0, result.ErrorCount);
            Assert.Equal(6, result.ValidPoints.Count);

            // Converted coordinates land in the standard Turkish TM ranges instead of
            // staying as raw degrees — this is what makes the shape drawable in CAD.
            foreach (var point in result.ValidPoints)
            {
                Assert.InRange(point.X, 100_000, 900_000);
                Assert.InRange(point.Y, 4_000_000, 4_900_000);
            }

            Assert.Equal("1", result.ValidPoints[0].Label);
            // Point 1 and point 6 are the same coordinate, so the polygon should close cleanly.
            Assert.Equal(result.ValidPoints[0].X, result.ValidPoints[5].X, 3);
            Assert.Equal(result.ValidPoints[0].Y, result.ValidPoints[5].Y, 3);
        }

        [Fact]
        public void Parse_EnlemBoylam_WithoutHeader_DetectedFromValueRanges()
        {
            // Same data, no header this time — must still be recognized as geographic
            // from the Turkey-specific lat/lon value ranges, not misread as raw TM meters.
            const string text =
                "1\t41.1886\t28.8750\n" +
                "2\t41.1890\t28.8747\n" +
                "3\t41.1890\t28.8745\n";

            ParseResult result = _parser.Parse(text);

            Assert.True(result.Format.IsGeographicSource);
            Assert.Equal(3, result.ValidPoints.Count);
            Assert.InRange(result.ValidPoints[0].X, 100_000, 900_000);
            Assert.InRange(result.ValidPoints[0].Y, 4_000_000, 4_900_000);
        }

        [Fact]
        public void Parse_BoylamEnlem_ReversedHeaderOrder_IsRespected()
        {
            const string text =
                "No\tBoylam\tEnlem\n" +
                "1\t28.8750\t41.1886\n" +
                "2\t28.8747\t41.1890\n";

            ParseResult result = _parser.Parse(text);

            Assert.True(result.Format.IsGeographicSource);
            Assert.Equal(ColumnLayout.NoBoylamEnlem, result.Format.Layout);

            ParseResult latFirstResult = _parser.Parse(
                "No\tEnlem\tBoylam\n1\t41.1886\t28.8750\n2\t41.1890\t28.8747\n");

            // Column order swapped in the source, but the drawn CAD point must be identical.
            Assert.Equal(latFirstResult.ValidPoints[0].X, result.ValidPoints[0].X, 3);
            Assert.Equal(latFirstResult.ValidPoints[0].Y, result.ValidPoints[0].Y, 3);
        }

        [Fact]
        public void Parse_ManualEnlemBoylamOverride_ForcesGeographicConversion()
        {
            const string text = "1 41.1886 28.8750\n2 41.1890 28.8747";

            ParseResult result = _parser.Parse(text, ColumnLayout.NoEnlemBoylam);

            Assert.True(result.Format.IsGeographicSource);
            Assert.Equal(1.0, result.Format.Confidence, 3);
            Assert.InRange(result.ValidPoints[0].X, 100_000, 900_000);
        }

        [Fact]
        public void Parse_TmCoordinates_AreNeverMisreadAsGeographic()
        {
            // Regression guard: ordinary TM data (far outside Turkey's lat/lon ranges)
            // must never be routed through the lat/lon conversion path.
            const string text = "1 456789.12 4423456.78\n2 456800.50 4423460.10";

            ParseResult result = _parser.Parse(text);

            Assert.False(result.Format.IsGeographicSource);
            Assert.Equal(456789.12, result.ValidPoints[0].X, 6);
        }
    }
}
