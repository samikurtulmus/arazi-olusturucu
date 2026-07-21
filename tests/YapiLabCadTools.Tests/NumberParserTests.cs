using Xunit;
using YapiLabCadTools.Core.Parsing;

namespace YapiLabCadTools.Tests
{
    public class NumberParserTests
    {
        [Theory]
        [InlineData("1234.56", 1234.56)]
        [InlineData("1234,56", 1234.56)]
        [InlineData("1.234.567,89", 1234567.89)]
        [InlineData("1,234,567.89", 1234567.89)]
        [InlineData("1,234,567", 1234567)]
        [InlineData("4423456.78", 4423456.78)]
        [InlineData("4423456,78", 4423456.78)]
        [InlineData("-42", -42)]
        [InlineData("+7", 7)]
        [InlineData("0", 0)]
        [InlineData("  123,45  ", 123.45)]
        // A single comma is a decimal comma by (Turkish) convention:
        [InlineData("1,234", 1.234)]
        public void TryParse_ValidNumbers(string token, double expected)
        {
            bool ok = NumberParser.TryParse(token, out double value);

            Assert.True(ok);
            Assert.Equal(expected, value, 9);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc")]
        [InlineData("12a")]
        [InlineData("P1")]
        [InlineData("12 34")]
        [InlineData("--5")]
        [InlineData("1;2")]
        public void TryParse_InvalidTokens(string? token)
        {
            Assert.False(NumberParser.TryParse(token, out _));
        }

        [Theory]
        [InlineData("123,45", true)]
        [InlineData("1.234.567,89", true)]
        [InlineData("123.45", false)]
        [InlineData("1,234,567.89", false)]
        [InlineData("1234", false)]
        [InlineData("abc", false)]
        public void UsesDecimalComma_Detection(string token, bool expected)
        {
            Assert.Equal(expected, NumberParser.UsesDecimalComma(token));
        }
    }
}
