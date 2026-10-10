using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Resolves required Revit cable tray family types and reference levels (Phase 12).
    /// </summary>
    public static class CableTraySystemResolver
    {
        /// <summary>
        /// Resolves the reference Level for cable tray placement.
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

            Level? bestLevel = levels.LastOrDefault(lvl => lvl.Elevation <= zElevation + 0.1) ?? levels.First();
            return bestLevel.Id;
        }

        /// <summary>
        /// Resolves a valid CableTrayType symbol in the document.
        /// </summary>
        public static ElementId ResolveCableTrayType(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var trayTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(CableTrayType))
                .Cast<CableTrayType>()
                .ToList();

            if (trayTypes.Count == 0)
                return ElementId.InvalidElementId;

            // Prefer Channel or Ladder tray type
            CableTrayType? preferred = trayTypes.FirstOrDefault(ct =>
                ct.Name.IndexOf("Channel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ct.Name.IndexOf("Ladder", StringComparison.OrdinalIgnoreCase) >= 0) ?? trayTypes.First();

            return preferred.Id;
        }
    }
}
