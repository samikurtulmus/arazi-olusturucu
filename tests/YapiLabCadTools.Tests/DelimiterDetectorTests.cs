using Xunit;
using YapiLabCadTools.Core.Models;
using YapiLabCadTools.Core.Parsing;

namespace YapiLabCadTools.Tests
{
    public class DelimiterDetectorTests
    {
        [Fact]
        public void Detect_TabSeparated()
        {
            var lines = new[]
            {
                "1\t456789.12\t4423456.78",
                "2\t456800.00\t4423460.00"
            };

            Assert.Equal(DelimiterKind.Tab, DelimiterDetector.Detect(lines));
        }

        [Fact]
        public void Detect_SemicolonWithDecimalComma()
        {
            var lines = new[]
            {
                "1;456789,12;4423456,78",
                "2;456800,00;4423460,00"
            };

            Assert.Equal(DelimiterKind.Semicolon, DelimiterDetector.Detect(lines));
        }

        [Fact]
        public void Detect_CommaCsv()
        {
            var lines = new[]
            {
                "1,456789.12,4423456.78",
                "2,456800.00,4423460.00"
            };

            Assert.Equal(DelimiterKind.Comma, DelimiterDetector.Detect(lines));
        }

        [Fact]
        public void Detect_SpaceSeparated()
        {
            var lines = new[]
            {
                "1 456789.12 4423456.78",
                "2 456800.00 4423460.00"
            };

            Assert.Equal(DelimiterKind.Whitespace, DelimiterDetector.Detect(lines));
        }

        [Fact]
        public void Detect_MultipleSpacesWithDecimalComma_PrefersWhitespaceOverComma()
        {
            // The commas here are decimal separators, not delimiters.
            var lines = new[]
            {
                "456789,12   4423456,78",
                "456800,00   4423460,00"
            };

            Assert.Equal(DelimiterKind.Whitespace, DelimiterDetector.Detect(lines));
        }

        [Fact]
        public void Detect_EmptyInput_FallsBackToWhitespace()
        {
            Assert.Equal(DelimiterKind.Whitespace, DelimiterDetector.Detect(new[] { "", "  " }));
        }

        [Fact]
        public void Split_Whitespace_HandlesMixedRuns()
        {
            string[] tokens = DelimiterDetector.Split("1  \t 456789.12   4423456.78", DelimiterKind.Whitespace);

            Assert.Equal(new[] { "1", "456789.12", "4423456.78" }, tokens);
        }

        [Fact]
        public void Split_Semicolon_TrimsAndDropsEmpties()
        {
            string[] tokens = DelimiterDetector.Split(" 1 ; 456789,12 ;; 4423456,78 ;", DelimiterKind.Semicolon);

            Assert.Equal(new[] { "1", "456789,12", "4423456,78" }, tokens);
        }
    }
}
