using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using SmartMepRouter.Core.Connectors;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Resolves required Revit system types, duct family types, and reference levels
    /// from existing connectors or document defaults.
    /// </summary>
    public static class DuctSystemResolver
    {
        /// <summary>
        /// Resolves the reference Level for duct placement.
        /// </summary>
        public static ElementId ResolveLevel(Document doc, Connector? connector, double zElevation)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            if (connector?.Owner != null && connector.Owner.LevelId != ElementId.InvalidElementId)
            {
                return connector.Owner.LevelId;
            }

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(lvl => lvl.Elevation)
                .ToList();

            if (levels.Count == 0)
                return ElementId.InvalidElementId;

            // Find closest level at or below the duct elevation, or lowest level
            Level? bestLevel = levels.LastOrDefault(lvl => lvl.Elevation <= zElevation + 0.1) ?? levels.First();
            return bestLevel.Id;
        }

        /// <summary>
        /// Resolves a valid MEPSystemType (Supply Air, Return Air, etc.) for ductwork generation.
        /// </summary>
        public static ElementId ResolveSystemType(Document doc, Connector? connector)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            if (connector?.MEPSystem != null)
            {
                ElementId typeId = connector.MEPSystem.GetTypeId();
                if (typeId != ElementId.InvalidElementId)
                    return typeId;
            }

            var systemTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(MEPSystemType))
                .Cast<MEPSystemType>()
                .ToList();

            if (systemTypes.Count == 0)
                return ElementId.InvalidElementId;

            // Prefer Supply Air
            MEPSystemType? preferred = systemTypes.FirstOrDefault(st =>
                st.Name.IndexOf("Supply", StringComparison.OrdinalIgnoreCase) >= 0) ?? systemTypes.First();

            return preferred.Id;
        }

        /// <summary>
        /// Resolves a valid DuctType symbol in the document matching the desired profile shape.
        /// </summary>
        public static ElementId ResolveDuctType(Document doc, ConnectorShape shape)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var ductTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(DuctType))
                .Cast<DuctType>()
                .ToList();

            if (ductTypes.Count == 0)
                return ElementId.InvalidElementId;

            if (shape == ConnectorShape.Round)
            {
                DuctType? roundType = ductTypes.FirstOrDefault(dt =>
                    dt.Name.IndexOf("Round", StringComparison.OrdinalIgnoreCase) >= 0);
                if (roundType != null) return roundType.Id;
            }
            else
            {
                DuctType? rectType = ductTypes.FirstOrDefault(dt =>
                    dt.Name.IndexOf("Rectangular", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dt.Name.IndexOf("Mitered", StringComparison.OrdinalIgnoreCase) >= 0);
                if (rectType != null) return rectType.Id;
            }

            // Fallback to default element type id or first available
            ElementId defaultId = doc.GetDefaultElementTypeId(ElementTypeGroup.DuctType);
            if (defaultId != ElementId.InvalidElementId)
                return defaultId;

            return ductTypes.First().Id;
        }
    }
}
