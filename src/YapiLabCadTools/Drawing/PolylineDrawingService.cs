using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using YapiLabCadTools.Core.Geometry;
using YapiLabCadTools.Core.Models;
using YapiLabCadTools.Core.Utils;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace YapiLabCadTools.Drawing
{
    /// <summary>
    /// Draws the parsed coordinates into the active document. All entities are created
    /// inside a single document lock + transaction, which keeps 100k-vertex jobs fast
    /// and makes the whole operation one AutoCAD UNDO step.
    /// </summary>
    public sealed class PolylineDrawingService : IDrawingService
    {
        /// <summary>Layer used for every shape except the primary (largest) one.</summary>
        private const string SecondaryLayerName = "YAPI";

        private readonly ILayerService _layerService;

        public PolylineDrawingService(ILayerService layerService)
        {
            _layerService = layerService ?? throw new ArgumentNullException(nameof(layerService));
        }

        /// <inheritdoc />
        public DrawResult Draw(IReadOnlyList<IReadOnlyList<CoordinatePoint>> shapes, DrawOptions options)
        {
            if (shapes is null || shapes.Count == 0 || shapes.All(s => s.Count < 2))
            {
                throw new InvalidOperationException("Çizim için en az 2 geçerli nokta gerekli.");
            }

            Document doc = AcApp.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("Açık bir çizim yok. Lütfen önce AutoCAD'de bir çizim açın.");

            Database db = doc.Database;

            // The shape with the most vertices is treated as the parcel boundary; any
            // remaining shapes (building footprints, etc.) share a secondary layer so they
            // stay visually and selectably distinct from the boundary.
            int primaryIndex = 0;
            for (int i = 1; i < shapes.Count; i++)
            {
                if (shapes[i].Count > shapes[primaryIndex].Count)
                {
                    primaryIndex = i;
                }
            }

            int totalPoints = 0;
            int shapesDrawn = 0;
            string primaryLayer = "0";
            string? secondaryLayer = null;
            GeometryStats primaryStats = GeometryStats.Empty;
            bool primaryClosed = false;
            var allPoints = new List<CoordinatePoint>();

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                for (int i = 0; i < shapes.Count; i++)
                {
                    IReadOnlyList<CoordinatePoint> points = shapes[i];
                    if (points.Count < 2)
                    {
                        continue;
                    }

                    bool isPrimary = i == primaryIndex;
                    string layerName = "0";
                    if (options.CreateLayer)
                    {
                        if (isPrimary)
                        {
                            if (!string.IsNullOrWhiteSpace(options.LayerName))
                            {
                                layerName = _layerService.EnsureLayer(tr, db, options.LayerName);
                            }
                        }
                        else
                        {
                            layerName = _layerService.EnsureLayer(tr, db, SecondaryLayerName);
                        }
                    }

                    bool closed = options.ClosePolyline && points.Count > 2;
                    GeometryStats stats = PolygonMath.Compute(points, closed);

                    AppendPolyline(tr, space, points, closed, layerName);

                    if (options.DrawPointMarkers)
                    {
                        AppendPointMarkers(tr, space, points, options, layerName, db);
                    }

                    if (options.DrawPointNumbers)
                    {
                        AppendPointNumbers(tr, space, points, options, layerName);
                    }

                    if (options.DrawSummaryText)
                    {
                        AppendSummaryText(tr, space, stats, closed, options, layerName);
                    }

                    totalPoints += points.Count;
                    shapesDrawn++;
                    allPoints.AddRange(points);

                    if (isPrimary)
                    {
                        primaryLayer = layerName;
                        primaryStats = stats;
                        primaryClosed = closed;
                    }
                    else
                    {
                        secondaryLayer = layerName;
                    }
                }

                tr.Commit();
            }

            if (options.ZoomToResult)
            {
                ZoomTo(doc.Editor, PolygonMath.Compute(allPoints, closed: false));
            }

            return new DrawResult(
                totalPoints,
                primaryClosed ? primaryStats.Area : 0,
                primaryStats.Perimeter,
                primaryLayer,
                primaryClosed,
                shapesDrawn,
                secondaryLayer);
        }

        private static void AppendPolyline(
            Transaction tr,
            BlockTableRecord space,
            IReadOnlyList<CoordinatePoint> points,
            bool closed,
            string layerName)
        {
            var polyline = new AcPolyline(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                polyline.AddVertexAt(i, new Point2d(points[i].X, points[i].Y), 0, 0, 0);
            }

            polyline.Closed = closed;
            polyline.Layer = layerName;
            space.AppendEntity(polyline);
            tr.AddNewlyCreatedDBObject(polyline, true);
        }

        private static void AppendPointMarkers(
            Transaction tr,
            BlockTableRecord space,
            IReadOnlyList<CoordinatePoint> points,
            DrawOptions options,
            string layerName,
            Database db)
        {
            // PDMODE/PDSIZE are drawing-wide settings that control how DBPoints render.
            db.Pdmode = options.PointSymbol switch
            {
                PointSymbol.Plus => 2,
                PointSymbol.Cross => 3,
                PointSymbol.Circle => 33,
                _ => 0
            };
            db.Pdsize = options.TextHeight * 0.6;

            foreach (CoordinatePoint p in points)
            {
                var dbPoint = new DBPoint(new Point3d(p.X, p.Y, 0)) { Layer = layerName };
                space.AppendEntity(dbPoint);
                tr.AddNewlyCreatedDBObject(dbPoint, true);
            }
        }

        private static void AppendPointNumbers(
            Transaction tr,
            BlockTableRecord space,
            IReadOnlyList<CoordinatePoint> points,
            DrawOptions options,
            string layerName)
        {
            double offset = options.TextHeight * 0.4;
            for (int i = 0; i < points.Count; i++)
            {
                CoordinatePoint p = points[i];
                var text = new DBText
                {
                    TextString = string.IsNullOrWhiteSpace(p.Label) ? (i + 1).ToString() : p.Label,
                    Position = new Point3d(p.X + offset, p.Y + offset, 0),
                    Height = options.TextHeight,
                    Layer = layerName
                };
                space.AppendEntity(text);
                tr.AddNewlyCreatedDBObject(text, true);
            }
        }

        private static void AppendSummaryText(
            Transaction tr,
            BlockTableRecord space,
            GeometryStats stats,
            bool closed,
            DrawOptions options,
            string layerName)
        {
            string contents = closed
                ? $"Alan: {NumberFormatting.Area(stats.Area)}\\PÇevre: {NumberFormatting.Length(stats.Perimeter)}"
                : $"Uzunluk: {NumberFormatting.Length(stats.Perimeter)}";

            var mtext = new MText
            {
                Contents = contents,
                Location = new Point3d(stats.CenterX, stats.CenterY, 0),
                TextHeight = options.TextHeight * 1.25,
                Attachment = AttachmentPoint.MiddleCenter,
                Layer = layerName
            };
            space.AppendEntity(mtext);
            tr.AddNewlyCreatedDBObject(mtext, true);
        }

        private static void ZoomTo(Editor editor, GeometryStats stats)
        {
            using ViewTableRecord view = editor.GetCurrentView();

            double width = stats.Width;
            double height = stats.Height;
            if (width < 1e-6) width = 10;
            if (height < 1e-6) height = 10;

            const double padding = 1.2;
            double aspect = view.Width / view.Height;
            double newHeight = Math.Max(height * padding, width * padding / aspect);

            view.CenterPoint = new Point2d(
                (stats.MinX + stats.MaxX) / 2,
                (stats.MinY + stats.MaxY) / 2);
            view.Height = newHeight;
            view.Width = newHeight * aspect;
            editor.SetCurrentView(view);
        }
    }
}
