using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Spatial
{
    /// <summary>
    /// Computes spatial efficiency and over-approximation volume metrics
    /// comparing exact OBBs against conservative AABBs (Phase 9 requirement).
    /// </summary>
    public static class WastedSpaceAnalyzer
    {
        public class SpaceMetrics
        {
            public int ObstacleCount { get; set; }
            public int RotatedObstacleCount { get; set; }
            public double ExactVolumeCuFt { get; set; }
            public double AabbVolumeCuFt { get; set; }
            public double WastedVolumeCuFt { get; set; }
            public double WastedVolumePercent { get; set; }
            public double AverageOverApproximationRatio { get; set; }
        }

        /// <summary>
        /// Analyzes a collection of obstacles and computes the wasted volume caused by
        /// enclosing oriented obstacles inside axis-aligned bounding boxes.
        /// </summary>
        public static SpaceMetrics Analyze(IEnumerable<IObstacle> obstacles)
        {
            var metrics = new SpaceMetrics();
            double totalRatio = 0.0;

            foreach (var obs in obstacles)
            {
                metrics.ObstacleCount++;
                double aabbVol = obs.Bounds.Volume;
                double exactVol = aabbVol;

                if (obs is ObbObstacle obb)
                {
                    metrics.RotatedObstacleCount++;
                    exactVol = 8.0 * obb.HalfExtents.X * obb.HalfExtents.Y * obb.HalfExtents.Z;
                }

                metrics.ExactVolumeCuFt += exactVol;
                metrics.AabbVolumeCuFt += aabbVol;

                double wasted = Math.Max(0.0, aabbVol - exactVol);
                metrics.WastedVolumeCuFt += wasted;

                if (exactVol > 1e-6)
                {
                    totalRatio += (aabbVol / exactVol);
                }
                else
                {
                    totalRatio += 1.0;
                }
            }

            if (metrics.AabbVolumeCuFt > 1e-6)
            {
                metrics.WastedVolumePercent = (metrics.WastedVolumeCuFt / metrics.AabbVolumeCuFt) * 100.0;
            }

            if (metrics.ObstacleCount > 0)
            {
                metrics.AverageOverApproximationRatio = totalRatio / metrics.ObstacleCount;
            }

            return metrics;
        }

        /// <summary>
        /// Computes the volume of an AABB in cubic feet.
        /// </summary>
        public static double GetVolume(Aabb aabb)
        {
            return (aabb.Max.X - aabb.Min.X) *
                   (aabb.Max.Y - aabb.Min.Y) *
                   (aabb.Max.Z - aabb.Min.Z);
        }
    }
}
