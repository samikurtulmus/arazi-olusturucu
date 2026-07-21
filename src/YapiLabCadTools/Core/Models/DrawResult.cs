namespace YapiLabCadTools.Core.Models
{
    /// <summary>Summary of a completed drawing operation, shown in the result panel.</summary>
    /// <param name="PointCount">Number of vertices drawn.</param>
    /// <param name="Area">Polyline area in drawing units² (0 when open).</param>
    /// <param name="Perimeter">Polyline length in drawing units.</param>
    /// <param name="LayerName">Layer the entities were placed on.</param>
    /// <param name="Closed">Whether the polyline was closed.</param>
    public sealed record DrawResult(
        int PointCount,
        double Area,
        double Perimeter,
        string LayerName,
        bool Closed);
}
