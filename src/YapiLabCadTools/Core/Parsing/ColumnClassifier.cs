using System;
using System.Collections.Generic;
using System.Linq;
using YapiLabCadTools.Core.Models;

namespace YapiLabCadTools.Core.Parsing
{
    /// <summary>Result of column classification for a tokenized coordinate list.</summary>
    public sealed class ColumnClassification
    {
        /// <summary>True when the first column is a point number / name.</summary>
        public bool HasLabelColumn { get; init; }

        /// <summary>Index of the first coordinate column.</summary>
        public int FirstCoordIndex { get; init; }

        /// <summary>Index of the second coordinate column.</summary>
        public int SecondCoordIndex { get; init; }

        /// <summary>True when the first coordinate column is the easting (Y/Sağa değer).</summary>
        public bool EastingFirst { get; init; }

        /// <summary>Confidence of the easting/northing order decision, in [0, 1].</summary>
        public double OrderConfidence { get; init; }

        /// <summary>True when the order was derived from coordinate value ranges (not a default assumption).</summary>
        public bool OrderDetectedFromValues { get; init; }

        /// <summary>The equivalent <see cref="ColumnLayout"/> value.</summary>
        public ColumnLayout ToLayout() => HasLabelColumn
            ? (EastingFirst ? ColumnLayout.NoYX : ColumnLayout.NoXY)
            : (EastingFirst ? ColumnLayout.YX : ColumnLayout.XY);
    }

    /// <summary>
    /// Decides which columns are the point number, easting and northing.
    /// </summary>
    /// <remarks>
    /// The easting/northing order is detected from value ranges valid for Turkey:
    /// TM/UTM northings fall in ~[3.4M, 4.9M] while eastings fall in ~[10k, 1.2M] —
    /// the ranges are disjoint, so when they match, the decision is near certain.
    /// For local/site coordinates the ranges do not discriminate and the parser falls
    /// back to the classic Turkish sheet order "Y X" (easting first), which is also
    /// what the international "X Y" (X = east) convention produces in CAD.
    /// </remarks>
    public static class ColumnClassifier
    {
        private const double NorthingMin = 3_400_000;
        private const double NorthingMax = 4_900_000;
        private const double EastingMin = 10_000;
        private const double EastingMax = 1_200_000;

        /// <summary>Fraction of rows required inside a range before it counts as a match.</summary>
        private const double RangeMatchRatio = 0.9;

        /// <summary>Maximum number of rows inspected; enough for certainty, cheap for 100k-row lists.</summary>
        private const int MaxSampleRows = 500;

        /// <summary>Classifies the columns of the given tokenized data rows.</summary>
        public static ColumnClassification Classify(IReadOnlyList<string[]> dataRows)
        {
            if (dataRows.Count == 0)
            {
                return new ColumnClassification
                {
                    FirstCoordIndex = 0,
                    SecondCoordIndex = 1,
                    EastingFirst = true,
                    OrderConfidence = 0.5
                };
            }

            // Work on rows having the most common token count; odd rows are reported
            // as errors later instead of confusing the detection.
            int modalCount = dataRows
                .GroupBy(r => r.Length)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .First().Key;

            List<string[]> rows = dataRows
                .Where(r => r.Length == modalCount)
                .Take(MaxSampleRows)
                .ToList();

            bool hasLabel = modalCount >= 3 && LooksLikeLabelColumn(rows);
            int c1 = hasLabel ? 1 : 0;
            int c2 = c1 + 1;

            List<double> first = ColumnValues(rows, c1);
            List<double> second = ColumnValues(rows, c2);
            (bool eastingFirst, double confidence, bool detected) = DetectOrder(first, second);

            return new ColumnClassification
            {
                HasLabelColumn = hasLabel,
                FirstCoordIndex = c1,
                SecondCoordIndex = c2,
                EastingFirst = eastingFirst,
                OrderConfidence = confidence,
                OrderDetectedFromValues = detected
            };
        }

        private static bool LooksLikeLabelColumn(List<string[]> rows)
        {
            var tokens = rows.Select(r => r[0]).ToList();

            // Names like "P1", "ZK-3" cannot be parsed as numbers → clearly a label column.
            int nonNumeric = tokens.Count(t => !NumberParser.IsNumeric(t));
            if (nonNumeric >= Math.Max(1, tokens.Count / 2))
            {
                return true;
            }

            var values = new List<double>();
            foreach (string t in tokens)
            {
                if (NumberParser.TryParse(t, out double v))
                {
                    values.Add(v);
                }
            }

            if (values.Count == 0)
            {
                return true;
            }

            // Point numbers are integers; coordinates almost always carry decimals.
            double integerRatio = values.Count(v => Math.Abs(v - Math.Round(v)) < 1e-9)
                / (double)values.Count;
            if (integerRatio < RangeMatchRatio)
            {
                return false;
            }

            // Small integers (1, 2, 3… or 101, 102…) are point numbers, not coordinates.
            if (values.Max(Math.Abs) < EastingMin)
            {
                return true;
            }

            // Large integers could still be numbering — accept when mostly consecutive.
            int ascendingSteps = 0;
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] > values[i - 1] && values[i] - values[i - 1] <= 1000)
                {
                    ascendingSteps++;
                }
            }

            return values.Count > 1 && ascendingSteps >= (values.Count - 1) * 0.8;
        }

        private static List<double> ColumnValues(List<string[]> rows, int index)
        {
            var values = new List<double>(rows.Count);
            foreach (string[] row in rows)
            {
                if (index < row.Length && NumberParser.TryParse(row[index], out double v))
                {
                    values.Add(v);
                }
            }

            return values;
        }

        private static (bool EastingFirst, double Confidence, bool Detected) DetectOrder(
            List<double> first,
            List<double> second)
        {
            double firstNorthing = FractionInRange(first, NorthingMin, NorthingMax);
            double secondNorthing = FractionInRange(second, NorthingMin, NorthingMax);
            double firstEasting = FractionInRange(first, EastingMin, EastingMax);
            double secondEasting = FractionInRange(second, EastingMin, EastingMax);

            if (firstEasting >= RangeMatchRatio && secondNorthing >= RangeMatchRatio)
            {
                return (true, 0.95, true);
            }

            if (firstNorthing >= RangeMatchRatio && secondEasting >= RangeMatchRatio)
            {
                return (false, 0.95, true);
            }

            // Local coordinates or values outside the known ranges: assume the classic
            // Turkish sheet order (easting first), which draws the first value on CAD X.
            return (true, 0.5, false);
        }

        private static double FractionInRange(List<double> values, double min, double max)
        {
            if (values.Count == 0)
            {
                return 0;
            }

            return values.Count(v => v >= min && v <= max) / (double)values.Count;
        }
    }
}
