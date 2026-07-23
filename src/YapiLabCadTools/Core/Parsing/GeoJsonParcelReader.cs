using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YapiLabCadTools.Core.Parsing
{
    /// <summary>
    /// Converts a TKGM "parsel sorgu" GeoJSON export (or any standard GeoJSON
    /// FeatureCollection of Polygon/MultiPolygon geometries) into the same "No Enlem
    /// Boylam" tabular text the grid already understands, so the existing parser,
    /// shape-grouping and drawing pipeline handle it without any special-casing.
    /// </summary>
    /// <remarks>
    /// Each ring — a parcel's outer boundary, or a building/void ring inside it — becomes
    /// its own restarting "No" block, and multiple features (e.g. every parcel of an "ada"
    /// queried at once) simply chain one after another in the same output. Both cases are
    /// already split into separate shapes by <see cref="PointGrouping"/>, so nothing else in
    /// the app needs to know the input came from JSON.
    /// </remarks>
    public static class GeoJsonParcelReader
    {
        /// <summary>
        /// Tries to read <paramref name="rawText"/> as GeoJSON and produce an equivalent
        /// "No\tEnlem\tBoylam" text. Returns false (leaving <paramref name="coordinateText"/>
        /// empty) for anything that isn't recognizable GeoJSON, so callers can fall back to
        /// treating the input as a normal pasted coordinate list.
        /// </summary>
        public static bool TryConvert(string rawText, out string coordinateText)
        {
            coordinateText = string.Empty;

            string trimmed = rawText.TrimStart();
            if (trimmed.Length == 0 || trimmed[0] != '{')
            {
                return false;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(rawText);
            }
            catch (JsonException)
            {
                return false;
            }

            using (document)
            {
                if (!document.RootElement.TryGetProperty("features", out JsonElement features) ||
                    features.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var text = new StringBuilder();
                text.Append("No\tEnlem\tBoylam\n");
                bool anyRing = false;

                foreach (JsonElement feature in features.EnumerateArray())
                {
                    if (!feature.TryGetProperty("geometry", out JsonElement geometry) ||
                        !geometry.TryGetProperty("type", out JsonElement geometryType) ||
                        !geometry.TryGetProperty("coordinates", out JsonElement coordinates))
                    {
                        continue;
                    }

                    switch (geometryType.GetString())
                    {
                        case "Polygon":
                            anyRing |= AppendPolygonRings(text, coordinates);
                            break;
                        case "MultiPolygon":
                            foreach (JsonElement polygon in coordinates.EnumerateArray())
                            {
                                anyRing |= AppendPolygonRings(text, polygon);
                            }

                            break;
                    }
                }

                if (!anyRing)
                {
                    return false;
                }

                coordinateText = text.ToString();
                return true;
            }
        }

        private static bool AppendPolygonRings(StringBuilder text, JsonElement rings)
        {
            bool any = false;
            foreach (JsonElement ring in rings.EnumerateArray())
            {
                any |= AppendRing(text, ring);
            }

            return any;
        }

        private static bool AppendRing(StringBuilder text, JsonElement ring)
        {
            var points = new List<(double Lon, double Lat)>();
            foreach (JsonElement coord in ring.EnumerateArray())
            {
                if (coord.ValueKind != JsonValueKind.Array || coord.GetArrayLength() < 2)
                {
                    continue;
                }

                // GeoJSON coordinate order is always [longitude, latitude].
                points.Add((coord[0].GetDouble(), coord[1].GetDouble()));
            }

            if (points.Count > 1 && IsSamePoint(points[0], points[^1]))
            {
                // GeoJSON rings explicitly repeat the first point at the end to close the
                // ring; our own "Kapalı polyline" option already re-adds that closing segment.
                points.RemoveAt(points.Count - 1);
            }

            if (points.Count < 2)
            {
                return false;
            }

            for (int i = 0; i < points.Count; i++)
            {
                text.Append(i + 1).Append('\t')
                    .Append(points[i].Lat.ToString("F7", CultureInfo.InvariantCulture)).Append('\t')
                    .Append(points[i].Lon.ToString("F7", CultureInfo.InvariantCulture)).Append('\n');
            }

            return true;
        }

        private static bool IsSamePoint((double Lon, double Lat) a, (double Lon, double Lat) b) =>
            Math.Abs(a.Lon - b.Lon) < 1e-12 && Math.Abs(a.Lat - b.Lat) < 1e-12;
    }
}
