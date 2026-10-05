using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Spatial
{
    /// <summary>
    /// Result of an independent geometric collision verification.
    /// </summary>
    public class CollisionVerificationResult
    {
        public bool HasCollisions => CollidingObstacleIds.Count > 0;
        public List<string> CollidingObstacleIds { get; }
        public List<string> Details { get; }

        public CollisionVerificationResult()
        {
            CollidingObstacleIds = new List<string>();
            Details = new List<string>();
        }

        public void AddCollision(string obstacleId, int segmentIndex, Vec3 p1, Vec3 p2)
        {
            if (!CollidingObstacleIds.Contains(obstacleId))
            {
                CollidingObstacleIds.Add(obstacleId);
            }
            Details.Add($"Segment [{segmentIndex}] ({p1} -> {p2}) intersects obstacle '{obstacleId}'.");
        }
    }

    /// <summary>
    /// Independent exact collision verifier required by Rule R-T2.
    /// Evaluates continuous line segments against exact obstacle geometries (AABB and OBB).
    /// </summary>
    public static class CollisionVerifier
    {
        /// <summary>
        /// Verifies that no segment of the route polyline intersects any of the specified obstacles.
        /// </summary>
        /// <param name="waypoints">Waypoints in decimal feet.</param>
        /// <param name="obstacles">Obstacles to test against.</param>
        /// <returns>A <see cref="CollisionVerificationResult"/> detailing any detected collisions.</returns>
        public static CollisionVerificationResult Verify(IReadOnlyList<Vec3> waypoints, IEnumerable<IObstacle> obstacles)
        {
            var result = new CollisionVerificationResult();

            if (waypoints == null || waypoints.Count < 2 || obstacles == null)
                return result;

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vec3 p1 = waypoints[i];
                Vec3 p2 = waypoints[i + 1];

                foreach (var obstacle in obstacles)
                {
                    if (obstacle.IntersectsSegment(p1, p2))
                    {
                        result.AddCollision(obstacle.Id, i, p1, p2);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if the entire path is completely free of collisions with all obstacles.
        /// </summary>
        public static bool IsPathCollisionFree(IReadOnlyList<Vec3> waypoints, IEnumerable<IObstacle> obstacles)
        {
            return !Verify(waypoints, obstacles).HasCollisions;
        }
    }
}
