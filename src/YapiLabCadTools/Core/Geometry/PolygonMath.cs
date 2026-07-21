using System;
using System.Collections.Generic;
using YapiLabCadTools.Core.Models;

namespace YapiLabCadTools.Core.Geometry
{
    /// <summary>
    /// Pure 2D polygon math used for the live preview (and for the summary text),
    /// so no AutoCAD entity has to exist before showing area/perimeter to the user.
    /// </summary>
    public static class PolygonMath
    {
        /// <summary>
        /// Computes the statistics of a point list.
        /// </summary>
        /// <param name="points">Vertices in drawing order.</param>
        /// <param name="closed">
        /// When true the closing segment (last → first) is included in the perimeter.
        /// The area is always the shoelace area of the closed ring (0 for fewer than 3 points).
        /// </param>
        public static GeometryStats Compute(IReadOnlyList<CoordinatePoint> points, bool closed)
        {
            int n = points.Count;
            if (n == 0)
            {
                return GeometryStats.Empty;
            }

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            double sumX = 0, sumY = 0;
            foreach (CoordinatePoint p in points)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
                sumX += p.X;
                sumY += p.Y;
            }

            double perimeter = 0;
            for (int i = 1; i < n; i++)
            {
                perimeter += Distance(points[i - 1], points[i]);
            }

            if (closed && n > 2)
            {
                perimeter += Distance(points[n - 1], points[0]);
            }

            // Shoelace formula; signed area is also reused for the centroid.
            double signedArea2 = 0;
            double cx = 0, cy = 0;
            if (n >= 3)
            {
                for (int i = 0; i < n; i++)
                {
                    CoordinatePoint a = points[i];
                    CoordinatePoint b = points[(i + 1) % n];
                    double cross = a.X * b.Y - b.X * a.Y;
                    signedArea2 += cross;
                    cx += (a.X + b.X) * cross;
                    cy += (a.Y + b.Y) * cross;
                }
            }

            double area = Math.Abs(signedArea2) / 2.0;

            double centerX, centerY;
            if (Math.Abs(signedArea2) > 1e-9)
            {
                centerX = cx / (3.0 * signedArea2);
                centerY = cy / (3.0 * signedArea2);
            }
            else
            {
                // Degenerate (collinear or < 3 points): fall back to the vertex average.
                centerX = sumX / n;
                centerY = sumY / n;
            }

            return new GeometryStats(n, area, perimeter, minX, minY, maxX, maxY, centerX, centerY);
        }

        private static double Distance(CoordinatePoint a, CoordinatePoint b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
