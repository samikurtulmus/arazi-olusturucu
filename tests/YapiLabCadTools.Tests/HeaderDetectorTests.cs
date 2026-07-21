using Xunit;
using YapiLabCadTools.Core.Parsing;

namespace YapiLabCadTools.Tests
{
    public class HeaderDetectorTests
    {
        [Theory]
        [InlineData("NO|Y|X")]
        [InlineData("Nokta No|Y|X")]
        [InlineData("POINT|NORTHING|EASTING")]
        [InlineData("Koordinat|No")]
        [InlineData("SAĞA|YUKARI")]
        [InlineData("PARSEL|KÖŞE|KOORDİNATLARI")]
        [InlineData("X:|Y:")]
        public void IsHeaderLine_RecognizesHeaders(string pipeSeparatedTokens)
        {
            Assert.True(HeaderDetector.IsHeaderLine(pipeSeparatedTokens.Split('|')));
        }

        [Theory]
        [InlineData("1|456789.12|4423456.78")]
        [InlineData("456789,12|4423456,78")]
        [InlineData("P1|456789.12|4423456.78")]
        public void IsHeaderLine_DoesNotFlagDataRows(string pipeSeparatedTokens)
        {
            Assert.False(HeaderDetector.IsHeaderLine(pipeSeparatedTokens.Split('|')));
        }
    }
}
