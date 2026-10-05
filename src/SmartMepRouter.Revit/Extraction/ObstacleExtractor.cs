using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Revit.Extraction
{
    /// <summary>
    /// Primary coordinator for extracting and inflating obstacles from host and linked Revit documents.
    /// Operates entirely on the Revit API thread (Rule R-A2).
    /// </summary>
    public static class ObstacleExtractor
    {
        /// <summary>
        /// Extracts all obstacles within the search corridor defined by start and end points.
        /// </summary>
        /// <param name="doc">Active Revit host document.</param>
        /// <param name="start">Routing start point in decimal feet.</param>
        /// <param name="end">Routing end point in decimal feet.</param>
        /// <param name="options">Extraction and inflation configuration options.</param>
        /// <returns>An <see cref="ExtractionResult"/> containing all obstacles, warnings, and metrics.</returns>
        public static ExtractionResult Extract(Document doc, XYZ start, XYZ end, ExtractionOptions options)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (end == null) throw new ArgumentNullException(nameof(end));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var stopwatch = Stopwatch.StartNew();
            var warnings = new List<string>();

            // 1. Build search corridor outline
            Outline corridorOutline = SearchCorridorBuilder.BuildCorridorOutline(
                start,
                end,
                options.CorridorMarginFeet,
                options.PlenumMinZFeet,
                options.PlenumMaxZFeet);

            Aabb corridorAabb = SearchCorridorBuilder.ToAabb(corridorOutline);

            // 2. Extract host document obstacles
            List<IObstacle> hostObstacles = HostObstacleExtractor.Extract(doc, corridorOutline, options);

            // 3. Extract linked model obstacles (if enabled)
            List<IObstacle> linkObstacles = LinkObstacleExtractor.Extract(doc, corridorOutline, options, warnings);

            // 4. Combine all obstacles
            var allObstacles = new List<IObstacle>(hostObstacles.Count + linkObstacles.Count);
            allObstacles.AddRange(hostObstacles);
            allObstacles.AddRange(linkObstacles);

            stopwatch.Stop();

            return new ExtractionResult(
                allObstacles,
                warnings,
                hostObstacles.Count,
                linkObstacles.Count,
                corridorAabb,
                stopwatch.Elapsed);
        }
    }
}
