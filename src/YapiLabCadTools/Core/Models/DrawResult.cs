namespace YapiLabCadTools.Core.Models
{
    /// <summary>Summary of a completed drawing operation, shown in the result panel.</summary>
    /// <param name="PointCount">Total number of vertices drawn, across all shapes.</param>
    /// <param name="Area">Area of the primary (largest) shape in drawing units² (0 when open).</param>
    /// <param name="Perimeter">Perimeter/length of the primary shape in drawing units.</param>
    /// <param name="LayerName">Layer the primary shape was placed on.</param>
    /// <param name="Closed">Whether the primary shape's polyline was closed.</param>
    /// <param name="ShapeCount">Number of separate polylines drawn (parcel + any building footprints).</param>
    /// <param name="SecondaryLayerName">
    /// Layer used for non-primary shapes, when <see cref="ShapeCount"/> is greater than 1.
    /// </param>
    public sealed record DrawResult(
        int PointCount,
        double Area,
        double Perimeter,
        string LayerName,
        bool Closed,
        int ShapeCount = 1,
        string? SecondaryLayerName = null);
}
