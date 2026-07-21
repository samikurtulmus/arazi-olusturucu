using System;

namespace YapiLabCadTools.Core.Geometry
{
    /// <summary>A projected UTM coordinate.</summary>
    /// <param name="Easting">Easting in meters (500,000 m false easting already applied).</param>
    /// <param name="Northing">Northing in meters.</param>
    /// <param name="Zone">UTM zone number (1-60).</param>
    public readonly record struct UtmCoordinate(double Easting, double Northing, int Zone);

    /// <summary>
    /// Converts WGS84 geographic coordinates (decimal degrees) to UTM meters, so that
    /// GPS/land-registry "Enlem/Boylam" lists can be drawn on the same metric plane as
    /// classic Turkish TM coordinate sheets.
    /// </summary>
    /// <remarks>
    /// Standard Snyder transverse-Mercator forward formulas on the WGS84 ellipsoid
    /// (a = 6,378,137 m, e² = 0.00669438), 6° zones, k0 = 0.9996, 500,000 m false easting.
    /// Accurate to well under a meter within a UTM zone, which is far tighter than the
    /// precision of hand-entered land-registry coordinates and more than enough for
    /// drawing a parcel's shape and area correctly.
    /// </remarks>
    public static class GeographicProjection
    {
        private const double SemiMajorAxis = 6378137.0;
        private const double EccentricitySquared = 0.00669438;
        private const double ScaleFactor = 0.9996;
        private const double FalseEasting = 500000.0;
        private const double SouthernFalseNorthing = 10000000.0;
        private const double DegToRad = Math.PI / 180.0;

        /// <summary>Converts a WGS84 latitude/longitude (decimal degrees) to UTM meters.</summary>
        public static UtmCoordinate ToUtm(double latitudeDeg, double longitudeDeg)
        {
            int zone = ZoneForLongitude(longitudeDeg);
            double lonOriginDeg = (zone - 1) * 6 - 180 + 3;

            double latRad = latitudeDeg * DegToRad;
            double lonRad = longitudeDeg * DegToRad;
            double lonOriginRad = lonOriginDeg * DegToRad;

            double eccPrimeSquared = EccentricitySquared / (1 - EccentricitySquared);

            double sinLat = Math.Sin(latRad);
            double cosLat = Math.Cos(latRad);
            double tanLat = Math.Tan(latRad);

            double n = SemiMajorAxis / Math.Sqrt(1 - EccentricitySquared * sinLat * sinLat);
            double t = tanLat * tanLat;
            double c = eccPrimeSquared * cosLat * cosLat;
            double a = cosLat * (lonRad - lonOriginRad);

            double m = SemiMajorAxis * (
                (1 - EccentricitySquared / 4 - 3 * EccentricitySquared * EccentricitySquared / 64
                    - 5 * EccentricitySquared * EccentricitySquared * EccentricitySquared / 256) * latRad
                - (3 * EccentricitySquared / 8 + 3 * EccentricitySquared * EccentricitySquared / 32
                    + 45 * EccentricitySquared * EccentricitySquared * EccentricitySquared / 1024) * Math.Sin(2 * latRad)
                + (15 * EccentricitySquared * EccentricitySquared / 256
                    + 45 * EccentricitySquared * EccentricitySquared * EccentricitySquared / 1024) * Math.Sin(4 * latRad)
                - (35 * EccentricitySquared * EccentricitySquared * EccentricitySquared / 3072) * Math.Sin(6 * latRad));

            double a2 = a * a;
            double a3 = a2 * a;
            double a4 = a2 * a2;
            double a5 = a4 * a;
            double a6 = a4 * a2;

            double easting = ScaleFactor * n * (a + (1 - t + c) * a3 / 6
                + (5 - 18 * t + t * t + 72 * c - 58 * eccPrimeSquared) * a5 / 120) + FalseEasting;

            double northing = ScaleFactor * (m + n * tanLat * (a2 / 2
                + (5 - t + 9 * c + 4 * c * c) * a4 / 24
                + (61 - 58 * t + t * t + 600 * c - 330 * eccPrimeSquared) * a6 / 720));

            if (latitudeDeg < 0)
            {
                northing += SouthernFalseNorthing;
            }

            return new UtmCoordinate(easting, northing, zone);
        }

        /// <summary>The standard 6°-wide UTM zone number for a given longitude.</summary>
        public static int ZoneForLongitude(double longitudeDeg) =>
            Math.Clamp((int)Math.Floor((longitudeDeg + 180) / 6) + 1, 1, 60);
    }
}
