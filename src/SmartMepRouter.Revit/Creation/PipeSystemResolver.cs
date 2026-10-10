using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Resolves required Revit piping system types, pipe family types, and reference levels (Phase 12).
    /// </summary>
    public static class PipeSystemResolver
    {
        /// <summary>
        /// Resolves the reference Level for pipe placement.
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
        /// Resolves a valid PipingSystemType (Hydronic Supply, Domestic Cold Water, etc.) for pipe generation.
        /// </summary>
        public static ElementId ResolvePipingSystemType(Document doc, Connector? connector)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            if (connector?.MEPSystem != null)
            {
                ElementId typeId = connector.MEPSystem.GetTypeId();
                if (typeId != ElementId.InvalidElementId)
                    return typeId;
            }

            var systemTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .ToList();

            if (systemTypes.Count == 0)
                return ElementId.InvalidElementId;

            // Prefer Hydronic Supply or Domestic Water
            PipingSystemType? preferred = systemTypes.FirstOrDefault(st =>
                st.Name.IndexOf("Hydronic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                st.Name.IndexOf("Supply", StringComparison.OrdinalIgnoreCase) >= 0 ||
                st.Name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0) ?? systemTypes.First();

            return preferred.Id;
        }

        /// <summary>
        /// Resolves a valid PipeType symbol in the document.
        /// </summary>
        public static ElementId ResolvePipeType(Document doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var pipeTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(PipeType))
                .Cast<PipeType>()
                .ToList();

            if (pipeTypes.Count == 0)
                return ElementId.InvalidElementId;

            // Prefer Standard or Copper pipe type
            PipeType? preferred = pipeTypes.FirstOrDefault(pt =>
                pt.Name.IndexOf("Standard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                pt.Name.IndexOf("Copper", StringComparison.OrdinalIgnoreCase) >= 0) ?? pipeTypes.First();

            return preferred.Id;
        }
    }
}
