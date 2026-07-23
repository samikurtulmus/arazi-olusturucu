using System.Linq;
using Xunit;
using YapiLabCadTools.Core.Parsing;

namespace YapiLabCadTools.Tests
{
    public class PointGroupingTests
    {
        [Fact]
        public void SingleContinuousList_StaysOneGroup()
        {
            string[] labels = { "1", "2", "3", "4", "5" };

            int[] groups = PointGrouping.AssignGroupIndexes(labels);

            Assert.All(groups, g => Assert.Equal(0, g));
        }

        [Fact]
        public void ParcelPlusBuildings_SplitsOnEachReset()
        {
            // 5-point parcel boundary followed by two 4-point building footprints,
            // exactly the shape a parsel-sorgu export concatenates them in.
            string[] labels =
            {
                "1", "2", "3", "4", "5",
                "1", "2", "3", "4",
                "1", "2", "3", "4"
            };

            int[] groups = PointGrouping.AssignGroupIndexes(labels);

            Assert.Equal(new[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2 }, groups);
        }

        [Fact]
        public void NonNumericLabels_NeverSplit()
        {
            string[] labels = { "P1", "P2", "P3", "P1", "P2" };

            int[] groups = PointGrouping.AssignGroupIndexes(labels);

            Assert.All(groups, g => Assert.Equal(0, g));
        }

        [Fact]
        public void BlankLabelBreaksTheNumericRun_WithoutForcingASplit()
        {
            // A blank/missing "No" cell can't be compared, so it must not itself be read
            // as a reset — the next real number resumes cleanly in the same group.
            string[] labels = { "1", "2", "", "3", "4" };

            int[] groups = PointGrouping.AssignGroupIndexes(labels);

            Assert.All(groups, g => Assert.Equal(0, g));
        }

        [Fact]
        public void EmptyInput_ReturnsEmptyArray()
        {
            Assert.Empty(PointGrouping.AssignGroupIndexes(System.Array.Empty<string>()));
        }
    }
}
