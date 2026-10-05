using System;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Search;

namespace SmartMepRouter.Core.Metrics
{
    /// <summary>
    /// Encapsulates execution and geometric metrics for a single routing run (FR-14).
    /// </summary>
    public class RunMetrics
    {
        public string RunId { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string SceneId { get; set; } = "";
        public string Algorithm { get; set; } = "SmartMepAStar";
        public double CellSizeFeet { get; set; }
        public int BlockedCells { get; set; }
        public int NodesExpanded { get; set; }
        public double PathLengthFeet { get; set; }
        public double PathLengthMm => Units.FeetToMm(PathLengthFeet);
        public int BendCount { get; set; }
        public int SegmentCount { get; set; }
        public bool Success { get; set; }
        public RoutingStatus Status { get; set; }
        public int CollisionCount { get; set; }
        public double SearchTimeMs { get; set; }
        public double VoxelizeTimeMs { get; set; }
        public double TotalTimeMs { get; set; }
    }
}
