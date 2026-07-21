using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YapiLabCadTools.Core.Geometry;
using YapiLabCadTools.Core.Models;

namespace YapiLabCadTools.Core.Parsing
{
    /// <summary>
    /// The main parsing pipeline: delimiter detection → header skipping → tokenizing →
    /// column classification → row building. Pure and side-effect free, so it can run
    /// on a background thread; it never throws on malformed input.
    /// </summary>
    public sealed class SmartCoordinateParser : ICoordinateParser
    {
        /// <inheritdoc />
        public ParseResult Parse(string? text, ColumnLayout layoutOverride = ColumnLayout.Auto)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return ParseResult.Empty;
            }

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            DelimiterKind delimiter = DelimiterDetector.Detect(lines);

            // First pass: tokenize every line, skip leading headers/titles, and separate
            // data lines from broken ones.
            var dataLines = new List<(int LineNo, string[] Tokens)>();
            var badLines = new List<(int LineNo, string Raw)>();
            var headerTokenLines = new List<string[]>();
            int skippedHeaders = 0;
            bool dataStarted = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] tokens = DelimiterDetector.Split(line, delimiter);
                bool looksLikeData = tokens.Count(NumberParser.IsNumeric) >= 2;

                if (looksLikeData)
                {
                    dataStarted = true;
                    dataLines.Add((i + 1, tokens));
                }
                else if (dataStarted && !HeaderDetector.IsHeaderLine(tokens, requireKnownWord: true))
                {
                    badLines.Add((i + 1, line.Trim()));
                }
                else
                {
                    // Leading titles/headers, or stray header lines between data blocks.
                    skippedHeaders++;
                    headerTokenLines.Add(tokens);
                }
            }

            if (dataLines.Count == 0)
            {
                var emptyFormat = new FormatInfo
                {
                    Delimiter = delimiter,
                    Layout = ColumnLayout.YX,
                    SkippedHeaderLines = skippedHeaders,
                    Confidence = 0,
                    Description = "Veri satırı bulunamadı"
                };
                return new ParseResult(
                    badLines.Select(b => InvalidRow(b.LineNo, b.Raw)).ToList(),
                    emptyFormat);
            }

            // Resolve the column layout (auto-detection or user override).
            List<string[]> tokenRows = dataLines.Select(d => d.Tokens).ToList();
            ColumnClassification classification = ColumnClassifier.Classify(tokenRows);
            GeoOrderHint headerHint = GeographicCoordinateDetector.HintFromHeaderTokens(headerTokenLines);
            (bool hasLabel, bool eastingFirst, double confidence, bool detected, bool isGeographic, bool latFirst) =
                ResolveLayout(layoutOverride, classification, tokenRows, headerHint);

            int firstCoord = hasLabel ? 1 : 0;
            int secondCoord = firstCoord + 1;
            int requiredColumns = secondCoord + 1;

            // Second pass: build rows in source order.
            var rows = new List<ParsedRow>(dataLines.Count + badLines.Count);
            foreach ((int lineNo, string[] tokens) in dataLines)
            {
                rows.Add(isGeographic
                    ? BuildGeographicRow(lineNo, tokens, hasLabel, latFirst, firstCoord, secondCoord, requiredColumns)
                    : BuildRow(lineNo, tokens, hasLabel, eastingFirst, firstCoord, secondCoord, requiredColumns));
            }

            rows.AddRange(badLines.Select(b => InvalidRow(b.LineNo, b.Raw)));
            rows.Sort((a, b) => a.LineNumber.CompareTo(b.LineNumber));

            ColumnLayout layout = isGeographic
                ? (hasLabel
                    ? (latFirst ? ColumnLayout.NoEnlemBoylam : ColumnLayout.NoBoylamEnlem)
                    : (latFirst ? ColumnLayout.EnlemBoylam : ColumnLayout.BoylamEnlem))
                : (hasLabel
                    ? (eastingFirst ? ColumnLayout.NoYX : ColumnLayout.NoXY)
                    : (eastingFirst ? ColumnLayout.YX : ColumnLayout.XY));

            bool decimalComma = dataLines
                .Take(100)
                .Any(d => d.Tokens.Skip(firstCoord).Take(2).Any(NumberParser.UsesDecimalComma));

            int? utmZone = isGeographic ? FindUtmZone(tokenRows, firstCoord, secondCoord, latFirst) : null;

            var format = new FormatInfo
            {
                Delimiter = delimiter,
                Layout = layout,
                HasLabelColumn = hasLabel,
                SkippedHeaderLines = skippedHeaders,
                UsesDecimalComma = decimalComma,
                IsGeographicSource = isGeographic,
                UtmZone = utmZone,
                Confidence = confidence,
                Description = BuildDescription(layout, delimiter, decimalComma, skippedHeaders,
                    layoutOverride != ColumnLayout.Auto, detected, isGeographic, utmZone)
            };

            return new ParseResult(rows, format);
        }

        private static (bool HasLabel, bool EastingFirst, double Confidence, bool Detected, bool IsGeographic, bool LatFirst) ResolveLayout(
            ColumnLayout layoutOverride,
            ColumnClassification classification,
            IReadOnlyList<string[]> dataRows,
            GeoOrderHint headerHint)
        {
            switch (layoutOverride)
            {
                case ColumnLayout.NoYX: return (true, true, 1.0, true, false, true);
                case ColumnLayout.NoXY: return (true, false, 1.0, true, false, true);
                case ColumnLayout.YX: return (false, true, 1.0, true, false, true);
                case ColumnLayout.XY: return (false, false, 1.0, true, false, true);
                case ColumnLayout.NoEnlemBoylam: return (true, true, 1.0, true, true, true);
                case ColumnLayout.NoBoylamEnlem: return (true, true, 1.0, true, true, false);
                case ColumnLayout.EnlemBoylam: return (false, true, 1.0, true, true, true);
                case ColumnLayout.BoylamEnlem: return (false, true, 1.0, true, true, false);
                default:
                    (bool isGeographic, bool latFirst, double geoConfidence) = GeographicCoordinateDetector.Detect(
                        dataRows, classification.FirstCoordIndex, classification.SecondCoordIndex, headerHint);
                    if (isGeographic)
                    {
                        return (classification.HasLabelColumn, true, geoConfidence, true, true, latFirst);
                    }

                    return (classification.HasLabelColumn,
                            classification.EastingFirst,
                            classification.OrderConfidence,
                            classification.OrderDetectedFromValues,
                            false,
                            true);
            }
        }

        private static ParsedRow BuildRow(
            int lineNo,
            string[] tokens,
            bool hasLabel,
            bool eastingFirst,
            int firstCoord,
            int secondCoord,
            int requiredColumns)
        {
            if (tokens.Length < requiredColumns)
            {
                return new ParsedRow
                {
                    LineNumber = lineNo,
                    LabelText = hasLabel && tokens.Length > 0 ? tokens[0] : null,
                    EastText = tokens.Length > firstCoord ? tokens[firstCoord] : string.Empty,
                    Error = $"Eksik kolon: {tokens.Length} bulundu, en az {requiredColumns} gerekli"
                };
            }

            string? label = hasLabel ? tokens[0] : null;
            string firstToken = tokens[firstCoord];
            string secondToken = tokens[secondCoord];
            string eastText = eastingFirst ? firstToken : secondToken;
            string northText = eastingFirst ? secondToken : firstToken;

            bool eastOk = NumberParser.TryParse(eastText, out double east);
            bool northOk = NumberParser.TryParse(northText, out double north);

            string? error = null;
            if (!eastOk && !northOk)
            {
                error = $"Sayı okunamadı: '{eastText}' ve '{northText}'";
            }
            else if (!eastOk)
            {
                error = $"Y (Sağa) değeri sayı değil: '{eastText}'";
            }
            else if (!northOk)
            {
                error = $"X (Yukarı) değeri sayı değil: '{northText}'";
            }

            return new ParsedRow
            {
                LineNumber = lineNo,
                LabelText = label,
                EastText = eastText,
                NorthText = northText,
                East = eastOk ? east : null,
                North = northOk ? north : null,
                Error = error
            };
        }

        private static ParsedRow BuildGeographicRow(
            int lineNo,
            string[] tokens,
            bool hasLabel,
            bool latFirst,
            int firstCoord,
            int secondCoord,
            int requiredColumns)
        {
            if (tokens.Length < requiredColumns)
            {
                return new ParsedRow
                {
                    LineNumber = lineNo,
                    LabelText = hasLabel && tokens.Length > 0 ? tokens[0] : null,
                    EastText = tokens.Length > firstCoord ? tokens[firstCoord] : string.Empty,
                    Error = $"Eksik kolon: {tokens.Length} bulundu, en az {requiredColumns} gerekli"
                };
            }

            string? label = hasLabel ? tokens[0] : null;
            string firstToken = tokens[firstCoord];
            string secondToken = tokens[secondCoord];
            string latText = latFirst ? firstToken : secondToken;
            string lonText = latFirst ? secondToken : firstToken;

            bool latOk = NumberParser.TryParse(latText, out double lat);
            bool lonOk = NumberParser.TryParse(lonText, out double lon);

            string? error = null;
            if (!latOk && !lonOk)
            {
                error = $"Sayı okunamadı: '{latText}' ve '{lonText}'";
            }
            else if (!latOk)
            {
                error = $"Enlem değeri sayı değil: '{latText}'";
            }
            else if (!lonOk)
            {
                error = $"Boylam değeri sayı değil: '{lonText}'";
            }
            else if (lat < -90 || lat > 90)
            {
                error = $"Enlem -90..90 aralığında olmalı: '{latText}'";
            }
            else if (lon < -180 || lon > 180)
            {
                error = $"Boylam -180..180 aralığında olmalı: '{lonText}'";
            }

            double? east = null;
            double? north = null;
            string eastText = latText;
            string northText = lonText;

            if (error is null)
            {
                UtmCoordinate utm = GeographicProjection.ToUtm(lat, lon);
                east = utm.Easting;
                north = utm.Northing;
                eastText = FormatMeters(utm.Easting);
                northText = FormatMeters(utm.Northing);
            }

            return new ParsedRow
            {
                LineNumber = lineNo,
                LabelText = label,
                EastText = eastText,
                NorthText = northText,
                East = east,
                North = north,
                Error = error
            };
        }

        private static string FormatMeters(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

        private static int? FindUtmZone(List<string[]> tokenRows, int firstCoord, int secondCoord, bool latFirst)
        {
            int lonIndex = latFirst ? secondCoord : firstCoord;
            foreach (string[] tokens in tokenRows)
            {
                if (lonIndex < tokens.Length &&
                    NumberParser.TryParse(tokens[lonIndex], out double lon) &&
                    lon is >= -180 and <= 180)
                {
                    return GeographicProjection.ZoneForLongitude(lon);
                }
            }

            return null;
        }

        private static ParsedRow InvalidRow(int lineNo, string rawLine)
        {
            const int maxExcerpt = 60;
            string excerpt = rawLine.Length > maxExcerpt ? rawLine[..maxExcerpt] + "…" : rawLine;
            return new ParsedRow
            {
                LineNumber = lineNo,
                EastText = excerpt,
                Error = "Geçersiz satır: koordinat çifti bulunamadı"
            };
        }

        private static string BuildDescription(
            ColumnLayout layout,
            DelimiterKind delimiter,
            bool decimalComma,
            int skippedHeaders,
            bool isOverride,
            bool orderDetected,
            bool isGeographic,
            int? utmZone)
        {
            string layoutName = layout switch
            {
                ColumnLayout.NoYX => "No Y X (Sağa-Yukarı)",
                ColumnLayout.NoXY => "No X Y (Yukarı-Sağa)",
                ColumnLayout.YX => "Y X (Sağa-Yukarı)",
                ColumnLayout.XY => "X Y (Yukarı-Sağa)",
                ColumnLayout.NoEnlemBoylam => "No Enlem Boylam (WGS84°)",
                ColumnLayout.NoBoylamEnlem => "No Boylam Enlem (WGS84°)",
                ColumnLayout.EnlemBoylam => "Enlem Boylam (WGS84°)",
                ColumnLayout.BoylamEnlem => "Boylam Enlem (WGS84°)",
                _ => "?"
            };

            string delimiterName = delimiter switch
            {
                DelimiterKind.Tab => "Sekme (Tab)",
                DelimiterKind.Semicolon => "Noktalı virgül",
                DelimiterKind.Comma => "Virgül",
                _ => "Boşluk"
            };

            var parts = new List<string>
            {
                layoutName,
                delimiterName,
                decimalComma ? "ondalık virgül" : "ondalık nokta"
            };

            if (isGeographic)
            {
                parts.Add(utmZone.HasValue
                    ? $"WGS84 → UTM {utmZone}N dönüştürüldü"
                    : "WGS84 → UTM dönüştürüldü");
            }

            if (skippedHeaders > 0)
            {
                parts.Add($"{skippedHeaders} başlık satırı atlandı");
            }

            if (isOverride)
            {
                parts.Add("elle seçildi");
            }
            else if (!orderDetected && !isGeographic)
            {
                parts.Add("varsayılan sıra");
            }

            return string.Join(" • ", parts);
        }
    }
}
