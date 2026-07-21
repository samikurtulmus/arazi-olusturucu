namespace YapiLabCadTools.Core.Models
{
    /// <summary>
    /// Column order of the source coordinate list, expressed in Turkish surveying terms:
    /// "Y" is the easting (Sağa değer) and "X" is the northing (Yukarı değer).
    /// </summary>
    public enum ColumnLayout
    {
        /// <summary>Let the parser detect the layout automatically.</summary>
        Auto = 0,

        /// <summary>Point number, easting (Y/Sağa), northing (X/Yukarı) — the classic Turkish coordinate sheet.</summary>
        NoYX,

        /// <summary>Point number, northing (X/Yukarı), easting (Y/Sağa).</summary>
        NoXY,

        /// <summary>Easting (Y/Sağa), northing (X/Yukarı) — no point number column.</summary>
        YX,

        /// <summary>Northing (X/Yukarı), easting (Y/Sağa) — no point number column.</summary>
        XY,

        /// <summary>
        /// Point number, latitude (Enlem), longitude (Boylam) in decimal degrees (WGS84) —
        /// e.g. land-registry (tapu kadastro) GPS lists. Converted to UTM meters before drawing.
        /// </summary>
        NoEnlemBoylam,

        /// <summary>Point number, longitude (Boylam), latitude (Enlem) in decimal degrees (WGS84).</summary>
        NoBoylamEnlem,

        /// <summary>Latitude (Enlem), longitude (Boylam) in decimal degrees (WGS84) — no point number column.</summary>
        EnlemBoylam,

        /// <summary>Longitude (Boylam), latitude (Enlem) in decimal degrees (WGS84) — no point number column.</summary>
        BoylamEnlem
    }
}
