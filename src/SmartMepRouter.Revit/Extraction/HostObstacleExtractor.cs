using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Revit.Extraction
{
    /// <summary>
    /// Extracts and inflates obstacles from the active host Revit document within the search corridor.
    /// </summary>
    public static class HostObstacleExtractor
    {
        /// <summary>
        /// Extracts obstacles belonging to the configured categories inside the corridor outline.
        /// </summary>
        /// <param name="doc">The active host Revit document.</param>
        /// <param name="corridorOutline">The search corridor boundary in host coordinates.</param>
        /// <param name="options">Extraction and inflation configuration.</param>
        /// <returns>A list of inflated obstacles.</returns>
        public static List<IObstacle> Extract(Document doc, Outline corridorOutline, ExtractionOptions options)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (corridorOutline == null) throw new ArgumentNullException(nameof(corridorOutline));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var obstacles = new List<IObstacle>();
            ICollection<BuiltInCategory> categories = options.GetSelectedCategories();

            if (categories.Count == 0)
                return obstacles;

            var catFilter = new ElementMulticategoryFilter(categories);
            var bboxFilter = new BoundingBoxIntersectsFilter(corridorOutline);

            var collector = new FilteredElementCollector(doc)
                .WherePasses(catFilter)
                .WherePasses(bboxFilter)
                .WhereElementIsNotElementType();

            var geomOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = false,
                IncludeNonVisibleObjects = false
            };

            double inflationFeet = options.ComputeTotalInflationFeet();

            foreach (Element element in collector)
            {
                try
                {
                    IObstacle? obs = GeometryUtils.CreateObstacleFromElement(element, geomOptions, null, "host_");
                    if (obs != null)
                    {
                        IObstacle inflated = obs.Inflate(inflationFeet);
                        obstacles.Add(inflated);
                    }
                }
                catch
                {
                    // Continue extracting other elements if a single corrupted element fails
                }
            }

            return obstacles;
        }
    }
}
