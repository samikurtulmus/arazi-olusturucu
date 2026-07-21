using System.Globalization;

namespace YapiLabCadTools.Core.Parsing
{
    /// <summary>
    /// Culture-tolerant number parsing: accepts both decimal comma and decimal point,
    /// with or without thousand separators. Never throws.
    /// </summary>
    public static class NumberParser
    {
        /// <summary>
        /// Tries to parse a token as a number.
        /// Handles: "1234.56", "1234,56", "1.234.567,89", "1,234,567.89", "-42", "+7".
        /// A single comma is always treated as a decimal comma (Turkish convention);
        /// a single dot is always treated as a decimal point.
        /// </summary>
        /// <param name="token">Raw cell text.</param>
        /// <param name="value">Parsed value on success.</param>
        /// <returns>True when the token is a valid number.</returns>
        public static bool TryParse(string? token, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string s = token.Trim();
            int lastComma = s.LastIndexOf(',');
            int lastDot = s.LastIndexOf('.');

            string normalized;
            if (lastComma >= 0 && lastDot >= 0)
            {
                // Both separators present: the one that appears last is the decimal
                // separator, the other one is a thousand separator.
                char decimalSep = lastComma > lastDot ? ',' : '.';
                char thousandSep = decimalSep == ',' ? '.' : ',';
                normalized = s.Replace(thousandSep.ToString(), string.Empty);
                if (decimalSep == ',')
                {
                    normalized = normalized.Replace(',', '.');
                }
            }
            else if (lastComma >= 0)
            {
                // Only commas: one comma is a decimal comma, several are thousand separators.
                normalized = s.IndexOf(',') == lastComma
                    ? s.Replace(',', '.')
                    : s.Replace(",", string.Empty);
            }
            else if (lastDot >= 0)
            {
                // Only dots: one dot is a decimal point, several are thousand separators.
                normalized = s.IndexOf('.') == lastDot
                    ? s
                    : s.Replace(".", string.Empty);
            }
            else
            {
                normalized = s;
            }

            return double.TryParse(
                normalized,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        /// <summary>True when the token parses as a number.</summary>
        public static bool IsNumeric(string? token) => TryParse(token, out _);

        /// <summary>True when the token is numeric and uses a decimal comma (e.g. "123,45").</summary>
        public static bool UsesDecimalComma(string? token)
        {
            if (token is null || !TryParse(token, out _))
            {
                return false;
            }

            int lastComma = token.LastIndexOf(',');
            int lastDot = token.LastIndexOf('.');
            if (lastComma < 0)
            {
                return false;
            }

            // Decimal comma when it is the last separator, or the only (single) comma.
            return lastDot < 0
                ? token.IndexOf(',') == lastComma
                : lastComma > lastDot;
        }
    }
}
