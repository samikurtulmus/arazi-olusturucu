using System.Linq;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace YapiLabCadTools.Drawing
{
    /// <inheritdoc cref="ILayerService" />
    public sealed class LayerService : ILayerService
    {
        /// <summary>Characters AutoCAD does not allow in symbol table names.</summary>
        private static readonly char[] InvalidChars = { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', ',', '=', '`' };

        /// <summary>Default color (ACI 3, green) for newly created layers.</summary>
        private const short DefaultColorIndex = 3;

        /// <inheritdoc />
        public string EnsureLayer(Transaction transaction, Database database, string requestedName)
        {
            string name = Sanitize(requestedName);
            if (name.Length == 0)
            {
                return "0";
            }

            var layerTable = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (layerTable.Has(name))
            {
                return name;
            }

            layerTable.UpgradeOpen();
            var record = new LayerTableRecord
            {
                Name = name,
                Color = Color.FromColorIndex(ColorMethod.ByAci, DefaultColorIndex)
            };
            layerTable.Add(record);
            transaction.AddNewlyCreatedDBObject(record, true);
            return name;
        }

        private static string Sanitize(string requestedName)
        {
            string trimmed = (requestedName ?? string.Empty).Trim();
            var cleaned = new string(trimmed.Where(c => !InvalidChars.Contains(c)).ToArray());
            const int maxLayerNameLength = 255;
            return cleaned.Length > maxLayerNameLength ? cleaned[..maxLayerNameLength] : cleaned;
        }
    }
}
