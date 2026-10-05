using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Revit.Extraction
{
    /// <summary>
    /// Encapsulates the results of obstacle extraction from Revit host and linked documents.
    /// </summary>
    public class ExtractionResult
    {
        /// <summary>
        /// All extracted and inflated obstacles ready for spatial indexing and collision checking.
        /// </summary>
        public IReadOnlyList<IObstacle> Obstacles { get; }

        /// <summary>
        /// Informational or diagnostic warnings produced during extraction (e.g. unloaded links).
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Number of obstacles extracted from the host document.
        /// </summary>
        public int HostObstacleCount { get; }

        /// <summary>
        /// Number of obstacles extracted from linked documents.
        /// </summary>
        public int LinkObstacleCount { get; }

        /// <summary>
        /// Total number of obstacles extracted.
        /// </summary>
        public int TotalObstacleCount => Obstacles.Count;

        /// <summary>
        /// The search corridor bounding box in decimal feet.
        /// </summary>
        public Aabb CorridorBounds { get; }

        /// <summary>
        /// Total time taken for extraction and processing.
        /// </summary>
        public TimeSpan Duration { get; }

        public ExtractionResult(
            IReadOnlyList<IObstacle> obstacles,
            IReadOnlyList<string> warnings,
            int hostCount,
            int linkCount,
            Aabb corridorBounds,
            TimeSpan duration)
        {
            Obstacles = obstacles ?? new List<IObstacle>();
            Warnings = warnings ?? new List<string>();
            HostObstacleCount = hostCount;
            LinkObstacleCount = linkCount;
            CorridorBounds = corridorBounds;
            Duration = duration;
        }
    }
}
