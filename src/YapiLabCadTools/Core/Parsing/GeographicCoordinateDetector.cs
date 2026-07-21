using System;
using System.Collections.Generic;
using System.Linq;

namespace YapiLabCadTools.Core.Parsing
{
    /// <summary>What a header line says about latitude/longitude column order, if anything.</summary>
    public enum GeoOrderHint
    {
        /// <summary>No header word identified either column as a latitude/longitude.</summary>
        None,

        /// <summary>The latitude header word (Enlem/Lat) appeared before the longitude one.</summary>
        LatFirst,

        /// <summary>The longitude header word (Boylam/Lon) appeared before the latitude one.</summary>
        LonFirst
    }

    /// <summary>
    /// Recognizes WGS84 "Enlem/Boylam" (latitude/longitude) coordinate lists so they can
    /// be converted to UTM meters before drawing, instead of being misread as raw TM
    /// eastings/northings.
    /// </summary>
    /// <remarks>
    /// Detection prefers an explicit header hint (a skipped header line naming Enlem/Boylam
    /// or Lat/Lon). Without one, it falls back to value ranges valid for Turkey: latitudes
    /// fall in roughly [34°, 43°] and longitudes in [24°, 46°] — well below the smallest
    /// realistic TM easting (~10,000 m), so there is no overlap with projected coordinates.
    /// </remarks>
    public static class GeographicCoordinateDetector
    {
        private const double LatitudeMin = 34.0;
        private const double LatitudeMax = 43.0;
        private const double LongitudeMin = 24.0;
        private const double LongitudeMax = 46.0;
        private const double RangeMatchRatio = 0.95;
        private const int MaxSampleRows = 500;

        /// <summary>Scans skipped header lines for a latitude/longitude word and returns their relative order.</summary>
        public static GeoOrderHint HintFromHeaderTokens(IEnumerable<string[]> headerLines)
        {
            foreach (string[] tokens in headerLines)
            {
                int latIndex = -1;
                int lonIndex = -1;

                for (int i = 0; i < tokens.Length; i++)
                {
                    string normalized = HeaderDetector.Normalize(tokens[i]);
                    if (latIndex < 0 && (normalized == "ENLEM" || normalized == "LAT" || normalized == "LATITUDE"))
                    {
                        latIndex = i;
                    }

                    if (lonIndex < 0 && (normalized == "BOYLAM" || normalized == "LON" || normalized == "LONG" || normalized == "LONGITUDE"))
                    {
                        lonIndex = i;
                    }
                }

                if (latIndex >= 0 && lonIndex >= 0)
                {
                    return latIndex < lonIndex ? GeoOrderHint.LatFirst : GeoOrderHint.LonFirst;
                }
            }

            return GeoOrderHint.None;
        }

        /// <summary>
        /// Decides whether the two coordinate columns hold WGS84 degrees rather than
        /// projected meters, and if so, which column is the latitude.
        /// </summary>
        public static (bool IsGeographic, bool LatFirst, double Confidence) Detect(
            IReadOnlyList<string[]> dataRows,
            int firstCoordIndex,
            int secondCoordIndex,
            GeoOrderHint headerHint)
        {
            if (headerHint != GeoOrderHint.None)
            {
                return (true, headerHint == GeoOrderHint.LatFirst, 0.95);
            }

            List<double> first = ColumnValues(dataRows, firstCoordIndex);
            List<double> second = ColumnValues(dataRows, secondCoordIndex);
            if (first.Count == 0 || second.Count == 0)
            {
                return (false, true, 0);
            }

            // GPS-derived degree values almost always carry a fraction; whole-number local
            // coordinates that happen to land in the lat/lon range are not geographic data.
            if (!first.Concat(second).Any(HasFraction))
            {
                return (false, true, 0);
            }

            double firstAsLat = FractionInRange(first, LatitudeMin, LatitudeMax);
            double secondAsLon = FractionInRange(second, LongitudeMin, LongitudeMax);
            double firstAsLon = FractionInRange(first, LongitudeMin, LongitudeMax);
            double secondAsLat = FractionInRange(second, LatitudeMin, LatitudeMax);

            if (firstAsLat >= RangeMatchRatio && secondAsLon >= RangeMatchRatio)
            {
                return (true, true, 0.75);
            }

            if (firstAsLon >= RangeMatchRatio && secondAsLat >= RangeMatchRatio)
            {
                return (true, false, 0.75);
            }

            return (false, true, 0);
        }

        private static bool HasFraction(double value) => Math.Abs(value - Math.Round(value)) > 1e-6;

        private static List<double> ColumnValues(IReadOnlyList<string[]> rows, int index)
        {
            var values = new List<double>(Math.Min(rows.Count, MaxSampleRows));
            for (int i = 0; i < rows.Count && values.Count < MaxSampleRows; i++)
            {
                string[] row = rows[i];
                if (index < row.Length && NumberParser.TryParse(row[index], out double v))
                {
                    values.Add(v);
                }
            }

            return values;
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
