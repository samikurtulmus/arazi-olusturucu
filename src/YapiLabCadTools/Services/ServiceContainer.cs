using YapiLabCadTools.Core.Parsing;
using YapiLabCadTools.Drawing;

namespace YapiLabCadTools.Services
{
    /// <summary>
    /// Hand-rolled composition root. Deliberately not an external DI container:
    /// the plugin must ship as a single DLL, and three services do not justify one.
    /// Future modules register their services here and receive them via constructor
    /// injection, keeping every class testable against the interfaces.
    /// </summary>
    public sealed class ServiceContainer
    {
        /// <summary>The application-wide container instance.</summary>
        public static ServiceContainer Instance { get; } = new();

        /// <summary>Coordinate text parser.</summary>
        public ICoordinateParser Parser { get; }

        /// <summary>AutoCAD drawing engine.</summary>
        public IDrawingService DrawingService { get; }

        /// <summary>Encoding-tolerant text file reader.</summary>
        public IFileService FileService { get; }

        private ServiceContainer()
        {
            Parser = new SmartCoordinateParser();
            DrawingService = new PolylineDrawingService(new LayerService());
            FileService = new FileService();
        }
    }
}
