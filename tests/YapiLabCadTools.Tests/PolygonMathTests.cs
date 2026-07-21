using System.Collections.Generic;
using Xunit;
using YapiLabCadTools.Core.Geometry;
using YapiLabCadTools.Core.Models;

namespace YapiLabCadTools.Tests
{
    public class PolygonMathTests
    {
        private static readonly List<CoordinatePoint> Square = new()
        {
            new CoordinatePoint(0, 0),
            new CoordinatePoint(10, 0),
            new CoordinatePoint(10, 10),
            new CoordinatePoint(0, 10)
        };

        [Fact]
        public void Compute_ClosedSquare()
        {
            GeometryStats stats = PolygonMath.Compute(Square, closed: true);

            Assert.Equal(4, stats.PointCount);
            Assert.Equal(100, stats.Area, 9);
            Assert.Equal(40, stats.Perimeter, 9);
            Assert.Equal(0, stats.MinX);
            Assert.Equal(10, stats.MaxY);
            Assert.Equal(5, stats.CenterX, 9);
            Assert.Equal(5, stats.CenterY, 9);
        }

        [Fact]
        public void Compute_OpenSquare_ExcludesClosingSegment()
        {
            GeometryStats stats = PolygonMath.Compute(Square, closed: false);

            Assert.Equal(30, stats.Perimeter, 9);
            // Area is still reported for the implied ring.
            Assert.Equal(100, stats.Area, 9);
        }

        [Fact]
        public void Compute_TwoPoints_NoArea()
        {
            var line = new List<CoordinatePoint>
            {
                new(0, 0),
                new(3, 4)
            };

            GeometryStats stats = PolygonMath.Compute(line, closed: true);

            Assert.Equal(0, stats.Area);
            Assert.Equal(5, stats.Perimeter, 9);
        }

        [Fact]
        public void Compute_Empty()
        {
            GeometryStats stats = PolygonMath.Compute(new List<CoordinatePoint>(), closed: true);

            Assert.Equal(GeometryStats.Empty, stats);
        }

        [Fact]
        public void Compute_CollinearPoints_FallsBackToAverageCenter()
        {
            var collinear = new List<CoordinatePoint>
            {
                new(0, 0),
                new(5, 5),
                new(10, 10)
            };

            GeometryStats stats = PolygonMath.Compute(collinear, closed: true);

            Assert.Equal(0, stats.Area, 6);
            Assert.Equal(5, stats.CenterX, 9);
            Assert.Equal(5, stats.CenterY, 9);
        }
    }
}
