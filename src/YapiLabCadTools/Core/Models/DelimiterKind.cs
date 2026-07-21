namespace YapiLabCadTools.Core.Models
{
    /// <summary>Column delimiter detected in the source text.</summary>
    public enum DelimiterKind
    {
        /// <summary>Any run of spaces and/or tabs (also covers "multiple spaces").</summary>
        Whitespace = 0,

        /// <summary>Tab character (TSV, Excel paste).</summary>
        Tab,

        /// <summary>Semicolon (Turkish-locale CSV).</summary>
        Semicolon,

        /// <summary>Comma (classic CSV).</summary>
        Comma
    }
}
