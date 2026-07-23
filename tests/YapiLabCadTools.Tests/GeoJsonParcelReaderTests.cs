using System.Collections.Generic;
using System.Linq;
using Xunit;
using YapiLabCadTools.Core.Models;
using YapiLabCadTools.Core.Parsing;

namespace YapiLabCadTools.Tests
{
    public class GeoJsonParcelReaderTests
    {
        // Real TKGM "parsel sorgu" export: 671 Ada 1 Parsel — one exterior boundary ring plus
        // 10 interior (hole) rings for the small structures inside the parcel.
        private const string TkgmSample =
            "{\"features\":[{\"type\":\"Feature\",\"geometry\":{\"type\":\"Polygon\",\"coordinates\":" +
            "[[[28.80334,41.0687],[28.80306,41.06904],[28.80277,41.06938],[28.80275,41.06926],[28.80255,41.06928]," +
            "[28.80258,41.06943],[28.80274,41.06941],[28.80263,41.06955],[28.80232,41.0694],[28.80196,41.06923]," +
            "[28.80175,41.06926],[28.80124,41.0693],[28.80092,41.06933],[28.80042,41.06938],[28.8,41.06942]," +
            "[28.79959,41.06947],[28.79915,41.06952],[28.79875,41.06957],[28.79833,41.06962],[28.798,41.06956]," +
            "[28.79762,41.06938],[28.79716,41.06916],[28.79715,41.06915],[28.79716,41.06913],[28.79747,41.06875]," +
            "[28.79779,41.06837],[28.79811,41.06799],[28.79822,41.06785],[28.79843,41.06761],[28.79854,41.06747]," +
            "[28.79879,41.06717],[28.79905,41.06686],[28.7993,41.06656],[28.79979,41.06634],[28.80024,41.06615]," +
            "[28.80039,41.06613],[28.80034,41.06619],[28.80042,41.06622],[28.80047,41.06617],[28.80075,41.06638]," +
            "[28.80109,41.06668],[28.80128,41.06684],[28.80153,41.06706],[28.8017,41.06722],[28.80196,41.06745]," +
            "[28.80214,41.0676],[28.8024,41.06783],[28.80258,41.06799],[28.80297,41.06833],[28.80334,41.06865]," +
            "[28.80335,41.06868],[28.80334,41.0687]]," +
            "[[28.80309,41.06844],[28.80305,41.06849],[28.80296,41.06846],[28.80301,41.0684],[28.80309,41.06844]]," +
            "[[28.80264,41.06807],[28.8026,41.06812],[28.80251,41.06808],[28.80256,41.06803],[28.80264,41.06807]]," +
            "[[28.80221,41.06768],[28.80216,41.06773],[28.80208,41.06769],[28.80213,41.06764],[28.80221,41.06768]]," +
            "[[28.80177,41.06729],[28.80172,41.06735],[28.80164,41.06731],[28.80169,41.06725],[28.80177,41.06729]]," +
            "[[28.80133,41.06691],[28.80128,41.06697],[28.8012,41.06693],[28.80125,41.06688],[28.80133,41.06691]]," +
            "[[28.80088,41.06653],[28.80084,41.06659],[28.80076,41.06655],[28.8008,41.06649],[28.80088,41.06653]]," +
            "[[28.80006,41.06935],[28.80001,41.0694],[28.79993,41.06936],[28.79998,41.06931],[28.80006,41.06935]]," +
            "[[28.79965,41.06651],[28.79958,41.06659],[28.79953,41.06657],[28.79948,41.06662],[28.7994,41.06658]," +
            "[28.79945,41.06653],[28.79949,41.06655],[28.79956,41.06647],[28.79965,41.06651]]," +
            "[[28.7992,41.06947],[28.79915,41.06952],[28.79907,41.06948],[28.79912,41.06943],[28.7992,41.06947]]," +
            "[[28.79842,41.06951],[28.79835,41.06959],[28.79826,41.06954],[28.79833,41.06946],[28.79842,41.06951]]]}," +
            "\"properties\":{\"ParselNo\":\"1\",\"Alan\":\"116.316,76\",\"Ada\":\"671\"}}]," +
            "\"type\":\"FeatureCollection\",\"crs\":{\"type\":\"name\",\"properties\":{\"name\":\"EPSG:4326\"}}}";

        [Fact]
        public void NonJsonText_IsRejected()
        {
            Assert.False(GeoJsonParcelReader.TryConvert("1\t41.06\t28.80\n", out _));
        }

        [Fact]
        public void TkgmSample_ConvertsAndSurvivesTheFullPipeline()
        {
            bool converted = GeoJsonParcelReader.TryConvert(TkgmSample, out string coordinateText);
            Assert.True(converted);

            ParseResult parsed = new SmartCoordinateParser().Parse(coordinateText);
            Assert.Equal(0, parsed.ErrorCount);

            string[] labels = parsed.Rows.Select(r => r.LabelText ?? string.Empty).ToArray();
            int[] groupIndexes = PointGrouping.AssignGroupIndexes(labels);

            // 1 exterior boundary ring + 10 hole rings (buildings).
            Assert.Equal(11, groupIndexes.Distinct().Count());

            var groupSizes = groupIndexes
                .GroupBy(g => g)
                .OrderBy(g => g.Key)
                .Select(g => g.Count())
                .ToList();

            // The boundary ring must dwarf every hole ring, so the drawing service picks it as
            // the primary (parcel) shape rather than one of the small structures.
            Assert.True(groupSizes[0] > groupSizes.Skip(1).Max() * 4);

            // Each hole ring repeats its first point at the end in the source (GeoJSON rings are
            // always closed); after that duplicate is dropped, 5-coordinate holes become 4 unique
            // points and the one 9-coordinate hole becomes 8.
            List<int> holeSizes = groupSizes.Skip(1).OrderBy(s => s).ToList();
            Assert.Equal(new[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 8 }, holeSizes);
        }
    }
}
