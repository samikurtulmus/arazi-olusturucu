using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YapiLabCadTools.Core.Parsing
{
    /// <summary>
    /// Recognizes header/title lines ("NOKTA NO   Y   X", "PARSEL KÖŞE KOORDİNATLARI", …)
    /// so they can be skipped before the data starts.
    /// </summary>
    public static class HeaderDetector
    {
        /// <summary>
        /// Known header words, compared after normalization (Turkish characters folded to
        /// ASCII, punctuation removed, upper-cased).
        /// </summary>
        private static readonly HashSet<string> HeaderWords = new(StringComparer.Ordinal)
        {
            "NO", "NOKTA", "NOKTANO", "NOKTAADI", "NOKTAISMI", "NOKTAISIM",
            "P", "PN", "PT", "POINT", "POINTNO", "POINTID", "ID", "AD", "ADI", "ISIM", "NAME",
            "X", "Y", "Z", "KOT", "H", "ELEV", "ELEVATION",
            "NORTHING", "EASTING", "SAGA", "SAGADEGER", "YUKARI", "YUKARIDEGER",
            "KOORDINAT", "KOORDINATNO", "KOORDINATLAR", "COORD", "COORDINATE", "COORDINATES",
            "ENLEM", "BOYLAM", "LAT", "LATITUDE", "LON", "LONG", "LONGITUDE"
        };

        /// <summary>
        /// True when the tokenized line looks like a header rather than data:
        /// fewer than two numeric tokens, and either a known header word is present
        /// or the line contains no numbers at all (an arbitrary title).
        /// </summary>
        /// <param name="tokens">Tokenized line.</param>
        /// <param name="requireKnownWord">
        /// When true, only lines containing a known header word qualify. The parser uses
        /// this once data rows have started, so repeated page headers are still skipped
        /// but arbitrary garbage lines are reported as errors instead of being swallowed.
        /// </param>
        public static bool IsHeaderLine(string[] tokens, bool requireKnownWord = false)
        {
            if (tokens.Length == 0)
            {
                return false;
            }

            int numericCount = tokens.Count(NumberParser.IsNumeric);
            if (numericCount >= 2)
            {
                return false;
            }

            if (tokens.Any(t => HeaderWords.Contains(Normalize(t))))
            {
                return true;
            }

            return !requireKnownWord && numericCount == 0;
        }

        /// <summary>Folds Turkish characters to ASCII, strips punctuation and upper-cases — used to compare tokens against <see cref="HeaderWords"/>.</summary>
        public static string Normalize(string token)
        {
            var sb = new StringBuilder(token.Length);
            foreach (char ch in token)
            {
                char folded = ch switch
                {
                    'ı' or 'İ' => 'I',
                    'ğ' or 'Ğ' => 'G',
                    'ş' or 'Ş' => 'S',
                    'ü' or 'Ü' => 'U',
                    'ö' or 'Ö' => 'O',
                    'ç' or 'Ç' => 'C',
                    _ => ch
                };

                if (char.IsLetterOrDigit(folded))
                {
                    sb.Append(char.ToUpperInvariant(folded));
                }
            }

            return sb.ToString();
        }
    }
}
