namespace YapiLabCadTools.Core.Models
{
    /// <summary>
    /// Geometry summary of a point list, computed without AutoCAD so the UI preview
    /// works before anything is drawn.
    /// </summary>
    /// <param name="PointCount">Number of points.</param>
    /// <param name="Area">Enclosed (shoelace) area, assuming the ring is closed; 0 for fewer than 3 points.</param>
    /// <param name="Perimeter">Total length; includes the closing segment when computed as closed.</param>
    /// <param name="MinX">Bounding box minimum easting.</param>
    /// <param name="MinY">Bounding box minimum northing.</param>
    /// <param name="MaxX">Bounding box maximum easting.</param>
    /// <param name="MaxY">Bounding box maximum northing.</param>
    /// <param name="CenterX">Label placement point (polygon centroid, or bbox center as fallback).</param>
    /// <param name="CenterY">Label placement point (polygon centroid, or bbox center as fallback).</param>
    public sealed record GeometryStats(
        int PointCount,
        double Area,
        double Perimeter,
        double MinX,
        double MinY,
        double MaxX,
        double MaxY,
        double CenterX,
        double CenterY)
    {
        /// <summary>Bounding box width (easting extent).</summary>
        public double Width => MaxX - MinX;

        /// <summary>Bounding box height (northing extent).</summary>
        public double Height => MaxY - MinY;

        /// <summary>Empty statistics.</summary>
        public static GeometryStats Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
