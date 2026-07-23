using System.Collections.Generic;

namespace YapiLabCadTools.Core.Parsing
{
    /// <summary>
    /// Splits a flat, pasted point list into separate shapes wherever the point-number
    /// column resets — the standard shape of a cadastral/parsel query export, which
    /// concatenates the parcel boundary with building footprints, each restarting its
    /// own "No" numbering at 1.
    /// </summary>
    public static class PointGrouping
    {
        /// <summary>
        /// Assigns a 0-based group index to each row, in source order. A new group starts
        /// whenever a numeric label is not strictly greater than the previous numeric label
        /// (e.g. "52" followed by "1"). Non-numeric or blank labels never trigger a split —
        /// they simply carry the current group, since there is nothing reliable to compare —
        /// so a list without a usable "No" column always comes back as a single group.
        /// </summary>
        public static int[] AssignGroupIndexes(IReadOnlyList<string> labels)
        {
            var groups = new int[labels.Count];
            int currentGroup = 0;
            double? previous = null;

            for (int i = 0; i < labels.Count; i++)
            {
                if (NumberParser.TryParse(labels[i], out double numeric))
                {
                    if (previous.HasValue && numeric <= previous.Value)
                    {
                        currentGroup++;
                    }

                    previous = numeric;
                }
                else
                {
                    previous = null;
                }

                groups[i] = currentGroup;
            }

            return groups;
        }
    }
}
