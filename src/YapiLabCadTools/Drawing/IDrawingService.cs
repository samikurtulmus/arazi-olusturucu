using System.Collections.Generic;
using YapiLabCadTools.Core.Models;

namespace YapiLabCadTools.Drawing
{
    /// <summary>Creates AutoCAD geometry from parsed coordinates.</summary>
    public interface IDrawingService
    {
        /// <summary>
        /// Draws the polyline (plus optional points, labels and summary text) into the
        /// active document using a single locked transaction.
        /// </summary>
        /// <param name="points">Vertices in drawing order (CAD X = easting, CAD Y = northing).</param>
        /// <param name="options">User-selected drawing options.</param>
        /// <returns>Summary of what was drawn, for the result panel.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// No open document, or fewer than two points. The message is user-facing Turkish.
        /// </exception>
        DrawResult Draw(IReadOnlyList<CoordinatePoint> points, DrawOptions options);
    }
}
