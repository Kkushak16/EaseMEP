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
        /// Exits immediately upon detecting the first collision without allocating collision reports.
        /// </summary>
        public static bool IsPathCollisionFree(IReadOnlyList<Vec3> waypoints, IEnumerable<IObstacle> obstacles)
        {
            if (waypoints == null || waypoints.Count < 2 || obstacles == null)
                return true;

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vec3 p1 = waypoints[i];
                Vec3 p2 = waypoints[i + 1];

                foreach (var obstacle in obstacles)
                {
                    if (obstacle.IntersectsSegment(p1, p2))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Builds a broadphase 3D spatial hash index over an obstacle collection (inspired by ofxSpatialHash and Bowerbird).
        /// </summary>
        public static SpatialHash3D<IObstacle> BuildSpatialIndex(IEnumerable<IObstacle> obstacles, double bucketSizeFeet = 5.0)
        {
            var hash = new SpatialHash3D<IObstacle>(bucketSizeFeet);
            if (obstacles != null)
            {
                foreach (var obs in obstacles)
                {
                    hash.Insert(obs, obs.Bounds);
                }
            }
            return hash;
        }

        /// <summary>
        /// Spatial-hash accelerated collision verification for large BIM models with hundreds or thousands of obstacles.
        /// Reduces broadphase test complexity from O(M * N) to O(M * k) where k is the number of local bucket obstacles.
        /// </summary>
        public static bool IsPathCollisionFree(IReadOnlyList<Vec3> waypoints, SpatialHash3D<IObstacle> spatialIndex)
        {
            if (waypoints == null || waypoints.Count < 2 || spatialIndex == null)
                return true;

            var candidates = new HashSet<IObstacle>();

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vec3 p1 = waypoints[i];
                Vec3 p2 = waypoints[i + 1];

                candidates.Clear();
                spatialIndex.QuerySegment(p1, p2, candidates);

                foreach (var obstacle in candidates)
                {
                    if (obstacle.IntersectsSegment(p1, p2))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
