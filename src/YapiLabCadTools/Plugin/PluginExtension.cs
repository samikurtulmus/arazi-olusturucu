using System.Text;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace YapiLabCadTools.Plugin
{
    /// <summary>Plugin lifecycle: runs once when the DLL is NETLOADed.</summary>
    public sealed class PluginExtension : IExtensionApplication
    {
        /// <inheritdoc />
        public void Initialize()
        {
            // Needed for Windows-1254 (Turkish ANSI) coordinate files on .NET 8.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var editor = AcApp.DocumentManager.MdiActiveDocument?.Editor;
            editor?.WriteMessage(
                "\nYapıLab CAD Tools yüklendi. Başlatmak için YAPILAB (veya YL) yazın.\n");
        }

        /// <inheritdoc />
        public void Terminate()
        {
            // Nothing to clean up: AutoCAD disposes the modeless window with the process.
        }
    }
}
