using System.IO;
using System.Text;

namespace YapiLabCadTools.Services
{
    /// <inheritdoc cref="IFileService" />
    public sealed class FileService : IFileService
    {
        static FileService()
        {
            // Windows-1254 (Turkish ANSI) lives in the CodePages provider on .NET 8.
            // Registering more than once is harmless.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <inheritdoc />
        public string ReadAllText(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);

            // Explicit BOMs first.
            if (bytes.Length >= 2)
            {
                if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                {
                    return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
                }

                if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                {
                    return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
                }
            }

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            }

            // No BOM: try strict UTF-8; invalid sequences mean a legacy Turkish ANSI file.
            try
            {
                return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(1254).GetString(bytes);
            }
        }
    }
}
