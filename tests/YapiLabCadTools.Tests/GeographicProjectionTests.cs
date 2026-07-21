using System;
using Xunit;
using YapiLabCadTools.Core.Geometry;

namespace YapiLabCadTools.Tests
{
    public class GeographicProjectionTests
    {
        [Fact]
        public void ZoneForLongitude_Istanbul_Is35()
        {
            Assert.Equal(35, GeographicProjection.ZoneForLongitude(28.8750));
        }

        [Fact]
        public void ToUtm_EquatorAtCentralMeridian_IsExactFalseOrigin()
        {
            // At the equator on a zone's central meridian, easting is exactly the false
            // easting (500,000 m) and northing is exactly zero — a formula-independent
            // sanity check for the zone/false-origin bookkeeping.
            UtmCoordinate utm = GeographicProjection.ToUtm(0.0, 3.0); // zone 31 central meridian

            Assert.Equal(31, utm.Zone);
            Assert.Equal(500000.0, utm.Easting, 3);
            Assert.Equal(0.0, utm.Northing, 3);
        }

        [Fact]
        public void ToUtm_NearbyPoints_PreserveRealWorldDistance()
        {
            // Two of the user's sample land-registry points, ~45 m apart. The projected
            // Euclidean distance should match a simple equirectangular estimate closely —
            // UTM scale distortion over tens of meters is a tiny fraction of a percent.
            const double lat1 = 41.1886, lon1 = 28.8750;
            const double lat2 = 41.1890, lon2 = 28.8747;

            UtmCoordinate p1 = GeographicProjection.ToUtm(lat1, lon1);
            UtmCoordinate p2 = GeographicProjection.ToUtm(lat2, lon2);

            double projectedDistance = Math.Sqrt(
                Math.Pow(p2.Easting - p1.Easting, 2) + Math.Pow(p2.Northing - p1.Northing, 2));

            const double earthRadius = 6378137.0;
            double latAvgRad = (lat1 + lat2) / 2 * Math.PI / 180.0;
            double dy = (lat2 - lat1) * Math.PI / 180.0 * earthRadius;
            double dx = (lon2 - lon1) * Math.PI / 180.0 * Math.Cos(latAvgRad) * earthRadius;
            double expectedDistance = Math.Sqrt(dx * dx + dy * dy);

            Assert.Equal(expectedDistance, projectedDistance, 1);
        }

        [Fact]
        public void ToUtm_SouthernHemisphere_AddsFalseNorthing()
        {
            UtmCoordinate utm = GeographicProjection.ToUtm(-33.0, 151.0); // Sydney-ish

            Assert.True(utm.Northing > 5000000.0);
        }
    }
}
