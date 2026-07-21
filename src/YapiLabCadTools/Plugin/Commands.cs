using Autodesk.AutoCAD.Runtime;
using YapiLabCadTools.Services;
using YapiLabCadTools.UI;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace YapiLabCadTools.Plugin
{
    /// <summary>
    /// AutoCAD command surface. Commands stay thin: they only resolve services and
    /// show the UI — all real work lives in the Core/Drawing/UI layers.
    /// </summary>
    public class Commands
    {
        private static MainForm? _mainForm;

        /// <summary>Opens the YapıLab coordinate window (modeless, singleton).</summary>
        [CommandMethod("YAPILAB", CommandFlags.Modal)]
        public void ShowYapiLab()
        {
            if (_mainForm is null || _mainForm.IsDisposed)
            {
                ServiceContainer services = ServiceContainer.Instance;
                _mainForm = new MainForm(services.Parser, services.DrawingService, services.FileService);
                AcApp.ShowModelessDialog(_mainForm);
            }
            else
            {
                _mainForm.Show();
                _mainForm.Activate();
            }
        }

        /// <summary>Short alias for <see cref="ShowYapiLab"/>.</summary>
        [CommandMethod("YL", CommandFlags.Modal)]
        public void ShowYapiLabShort() => ShowYapiLab();
    }
}
