using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Revit.Extraction
{
    /// <summary>
    /// Extracts and inflates obstacles from linked Revit models (RevitLinkInstance) within the search corridor.
    /// Handles coordinate transformations from link space to host project space.
    /// Emits warnings for unloaded linked models (FR-05).
    /// </summary>
    public static class LinkObstacleExtractor
    {
        /// <summary>
        /// Extracts obstacles from all loaded linked models intersecting the corridor.
        /// </summary>
        /// <param name="hostDoc">The active host Revit document.</param>
        /// <param name="hostCorridorOutline">The search corridor in host coordinates.</param>
        /// <param name="options">Extraction and inflation configuration.</param>
        /// <param name="warnings">Output collection receiving diagnostic warnings (e.g. unloaded links).</param>
        /// <returns>A list of inflated obstacles in host coordinates.</returns>
        public static List<IObstacle> Extract(
            Document hostDoc,
            Outline hostCorridorOutline,
            ExtractionOptions options,
            List<string> warnings)
        {
            if (hostDoc == null) throw new ArgumentNullException(nameof(hostDoc));
            if (hostCorridorOutline == null) throw new ArgumentNullException(nameof(hostCorridorOutline));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (warnings == null) throw new ArgumentNullException(nameof(warnings));

            var obstacles = new List<IObstacle>();

            if (!options.IncludeLinkedModels)
                return obstacles;

            ICollection<BuiltInCategory> categories = options.GetSelectedCategories();
            if (categories.Count == 0)
                return obstacles;

            var catFilter = new ElementMulticategoryFilter(categories);
            var geomOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = false,
                IncludeNonVisibleObjects = false
            };

            double inflationFeet = options.ComputeTotalInflationFeet();

            var linkInstances = new FilteredElementCollector(hostDoc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>();

            foreach (RevitLinkInstance linkInst in linkInstances)
            {
                Document? linkDoc = linkInst.GetLinkDocument();

                if (linkDoc == null)
                {
                    // FR-05: Warn if a linked model is not loaded
                    warnings.Add($"Linked model '{linkInst.Name}' is not loaded and cannot be checked for obstacles.");
                    continue;
                }

                try
                {
                    Transform linkTransform = linkInst.GetTotalTransform();
                    Transform invTransform = linkTransform.Inverse;

                    // Transform corridor outline into link model's local coordinate system
                    Outline linkCorridor = SearchCorridorBuilder.TransformOutline(hostCorridorOutline, invTransform);
                    var linkBboxFilter = new BoundingBoxIntersectsFilter(linkCorridor);

                    var linkCollector = new FilteredElementCollector(linkDoc)
                        .WherePasses(catFilter)
                        .WherePasses(linkBboxFilter)
                        .WhereElementIsNotElementType();

                    string linkPrefix = $"link_{linkInst.Id}_";

                    foreach (Element linkElem in linkCollector)
                    {
                        try
                        {
                            IObstacle? obs = GeometryUtils.CreateObstacleFromElement(linkElem, geomOptions, linkTransform, linkPrefix);
                            if (obs != null)
                            {
                                obstacles.Add(obs.Inflate(inflationFeet));
                            }
                        }
                        catch
                        {
                            // Skip individually malformed elements in the link
                        }
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Error reading linked model '{linkInst.Name}': {ex.Message}");
                }
            }

            return obstacles;
        }
    }
}
