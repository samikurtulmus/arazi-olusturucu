namespace YapiLabCadTools.Services
{
    /// <summary>Reads coordinate text files with tolerant encoding handling.</summary>
    public interface IFileService
    {
        /// <summary>
        /// Reads a text file, detecting UTF-8/UTF-16 by content and falling back to
        /// Windows-1254 (Turkish ANSI) for legacy files.
        /// </summary>
        string ReadAllText(string path);
    }
}
