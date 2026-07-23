using System.Collections.Generic;
using YapiLabCadTools.Core.Models;

namespace YapiLabCadTools.Drawing
{
    /// <summary>Creates AutoCAD geometry from parsed coordinates.</summary>
    public interface IDrawingService
    {
        /// <summary>
        /// Draws one polyline per shape (plus optional points, labels and summary text) into
        /// the active document using a single locked transaction. The shape with the most
        /// vertices is treated as the parcel boundary and uses <see cref="DrawOptions.LayerName"/>;
        /// any remaining shapes (e.g. building footprints from a parsel query) go on a
        /// secondary layer.
        /// </summary>
        /// <param name="shapes">One or more vertex lists, each in drawing order (CAD X = easting, CAD Y = northing).</param>
        /// <param name="options">User-selected drawing options.</param>
        /// <returns>Summary of what was drawn, for the result panel.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// No open document, or every shape has fewer than two points. The message is user-facing Turkish.
        /// </exception>
        DrawResult Draw(IReadOnlyList<IReadOnlyList<CoordinatePoint>> shapes, DrawOptions options);
    }
}
