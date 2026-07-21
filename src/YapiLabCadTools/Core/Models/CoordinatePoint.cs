namespace YapiLabCadTools.Core.Models
{
    /// <summary>
    /// A single coordinate in CAD drawing space.
    /// </summary>
    /// <remarks>
    /// <see cref="X"/> is the CAD X axis value, i.e. the easting (Turkish surveying "Y / Sağa değer").
    /// <see cref="Y"/> is the CAD Y axis value, i.e. the northing (Turkish surveying "X / Yukarı değer").
    /// The parser normalizes every input layout into this convention so the rest of the
    /// application never has to reason about column order again.
    /// </remarks>
    /// <param name="X">Easting / CAD X.</param>
    /// <param name="Y">Northing / CAD Y.</param>
    /// <param name="Label">Optional point number or name (e.g. "101", "P5").</param>
    public sealed record CoordinatePoint(double X, double Y, string? Label = null);
}
