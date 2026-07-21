namespace YapiLabCadTools.Core.Models
{
    /// <summary>Point marker style (maps to AutoCAD PDMODE in the drawing layer).</summary>
    public enum PointSymbol
    {
        /// <summary>Simple dot.</summary>
        Dot,

        /// <summary>Plus sign (+).</summary>
        Plus,

        /// <summary>Cross (X).</summary>
        Cross,

        /// <summary>Empty circle.</summary>
        Circle
    }

    /// <summary>User-selected drawing options. Contains no AutoCAD types on purpose.</summary>
    public sealed class DrawOptions
    {
        /// <summary>Close the polyline (parcel boundary). Default: true.</summary>
        public bool ClosePolyline { get; set; } = true;

        /// <summary>Write the point number/name next to each vertex.</summary>
        public bool DrawPointNumbers { get; set; } = true;

        /// <summary>Place an AutoCAD point entity at each vertex.</summary>
        public bool DrawPointMarkers { get; set; }

        /// <summary>Marker style used when <see cref="DrawPointMarkers"/> is enabled.</summary>
        public PointSymbol PointSymbol { get; set; } = PointSymbol.Plus;

        /// <summary>Write an area/perimeter summary text near the centroid.</summary>
        public bool DrawSummaryText { get; set; } = true;

        /// <summary>Create the target layer if it does not exist.</summary>
        public bool CreateLayer { get; set; } = true;

        /// <summary>Target layer name. Default: "PARSEL".</summary>
        public string LayerName { get; set; } = "PARSEL";

        /// <summary>Text height in drawing units for labels and summary.</summary>
        public double TextHeight { get; set; } = 1.0;

        /// <summary>Zoom to the created geometry after drawing.</summary>
        public bool ZoomToResult { get; set; } = true;
    }
}
