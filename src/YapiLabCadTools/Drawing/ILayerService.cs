using Autodesk.AutoCAD.DatabaseServices;

namespace YapiLabCadTools.Drawing
{
    /// <summary>Ensures target layers exist before entities are placed on them.</summary>
    public interface ILayerService
    {
        /// <summary>
        /// Returns a usable layer name, creating the layer inside the given transaction
        /// when it does not exist. Invalid characters are stripped from the requested
        /// name; an unusable name falls back to layer "0".
        /// </summary>
        string EnsureLayer(Transaction transaction, Database database, string requestedName);
    }
}
