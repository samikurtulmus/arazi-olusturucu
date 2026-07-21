using System.Globalization;

namespace YapiLabCadTools.Core.Utils
{
    /// <summary>User-facing number formatting (Turkish culture for measurements).</summary>
    public static class NumberFormatting
    {
        /// <summary>Turkish culture used for areas/lengths shown to the user.</summary>
        public static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary>Formats an area, e.g. "1.234,57 m²".</summary>
        public static string Area(double value) => value.ToString("N2", Turkish) + " m²";

        /// <summary>Formats a length, e.g. "142,80 m".</summary>
        public static string Length(double value) => value.ToString("N2", Turkish) + " m";

        /// <summary>Formats a coordinate for display/editing (invariant, dot decimal).</summary>
        public static string Coordinate(double value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
