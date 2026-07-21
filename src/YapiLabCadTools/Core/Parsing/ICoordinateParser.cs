using YapiLabCadTools.Core.Models;

namespace YapiLabCadTools.Core.Parsing
{
    /// <summary>Parses free-form coordinate text into structured rows.</summary>
    public interface ICoordinateParser
    {
        /// <summary>
        /// Parses the given text. Never throws: malformed lines are reported as invalid
        /// rows inside the result.
        /// </summary>
        /// <param name="text">Raw text (pasted, typed or read from a file).</param>
        /// <param name="layoutOverride">
        /// Optional manual column layout; <see cref="ColumnLayout.Auto"/> lets the parser decide.
        /// </param>
        ParseResult Parse(string? text, ColumnLayout layoutOverride = ColumnLayout.Auto);
    }
}
