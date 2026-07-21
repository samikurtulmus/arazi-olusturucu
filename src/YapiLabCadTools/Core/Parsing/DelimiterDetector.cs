using System;
using System.Collections.Generic;
using System.Linq;
using YapiLabCadTools.Core.Models;

namespace YapiLabCadTools.Core.Parsing
{
    /// <summary>
    /// Detects the column delimiter of a coordinate text by scoring each candidate on a
    /// sample of lines: the delimiter that turns the most lines into 2–5 mostly-numeric
    /// tokens wins. This naturally resolves the "comma as delimiter vs decimal comma"
    /// ambiguity: with decimal commas, splitting on the comma produces non-numeric
    /// fragments and the candidate scores poorly.
    /// </summary>
    public static class DelimiterDetector
    {
        private const int SampleSize = 100;

        /// <summary>Detects the most plausible delimiter for the given lines.</summary>
        public static DelimiterKind Detect(IEnumerable<string> lines)
        {
            List<string> sample = lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Take(SampleSize)
                .ToList();

            if (sample.Count == 0)
            {
                return DelimiterKind.Whitespace;
            }

            // Order encodes tie-break priority: an explicit delimiter beats plain whitespace.
            DelimiterKind[] candidates =
            {
                DelimiterKind.Tab,
                DelimiterKind.Semicolon,
                DelimiterKind.Comma,
                DelimiterKind.Whitespace
            };

            DelimiterKind best = DelimiterKind.Whitespace;
            int bestScore = -1;
            foreach (DelimiterKind kind in candidates)
            {
                if (kind != DelimiterKind.Whitespace &&
                    !sample.Any(l => l.Contains(DelimiterChar(kind))))
                {
                    continue;
                }

                int score = sample.Count(l => IsPlausibleDataLine(Split(l, kind)));
                if (score > bestScore)
                {
                    bestScore = score;
                    best = kind;
                }
            }

            return best;
        }

        /// <summary>Splits a line into trimmed, non-empty tokens using the given delimiter.</summary>
        public static string[] Split(string line, DelimiterKind kind)
        {
            if (kind == DelimiterKind.Whitespace)
            {
                // Splits on any run of spaces/tabs, which also handles "mixed" and
                // "multiple spaces" input.
                return line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            }

            return line
                .Split(DelimiterChar(kind))
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToArray();
        }

        private static char DelimiterChar(DelimiterKind kind) => kind switch
        {
            DelimiterKind.Tab => '\t',
            DelimiterKind.Semicolon => ';',
            DelimiterKind.Comma => ',',
            _ => ' '
        };

        private static bool IsPlausibleDataLine(string[] tokens)
        {
            if (tokens.Length < 2 || tokens.Length > 5)
            {
                return false;
            }

            if (tokens.Count(NumberParser.IsNumeric) < 2)
            {
                return false;
            }

            // Only the first column may be non-numeric (a point number/name). This is what
            // disambiguates "456789,12 4423456,78": splitting it on the comma leaves a
            // non-numeric fragment in the middle, so the comma candidate is rejected and
            // whitespace + decimal comma wins.
            return tokens.Skip(1).All(NumberParser.IsNumeric);
        }
    }
}
