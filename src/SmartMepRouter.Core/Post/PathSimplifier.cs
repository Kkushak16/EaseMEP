using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Post
{
    /// <summary>
    /// Compresses a sequence of route vertices by eliminating redundant collinear points.
    /// Preserves only corner points and endpoints.
    /// </summary>
    public static class PathSimplifier
    {
        /// <summary>
        /// Compresses collinear points into corner vertices.
        /// </summary>
        /// <param name="points">List of waypoints in decimal feet.</param>
        /// <returns>Simplified polyline containing only essential corner points.</returns>
        public static List<Vec3> CompressCollinear(IReadOnlyList<Vec3> points)
        {
            if (points == null || points.Count <= 2)
                return points != null ? new List<Vec3>(points) : new List<Vec3>();

            var result = new List<Vec3> { points[0] };

            for (int i = 1; i < points.Count - 1; i++)
            {
                Vec3 prev = result[result.Count - 1];
                Vec3 curr = points[i];
                Vec3 next = points[i + 1];

                Vec3 v1 = curr - prev;
                Vec3 v2 = next - curr;

                double len1 = v1.Length();
                double len2 = v2.Length();

                if (len1 < Vec3.Tolerance)
                    continue;

                if (len2 < Vec3.Tolerance)
                    continue;

                Vec3 dir1 = v1 / len1;
                Vec3 dir2 = v2 / len2;

                // If directions are not equal, this is a turn / corner
                if (!dir1.Equals(dir2))
                {
                    result.Add(curr);
                }
            }

            result.Add(points[points.Count - 1]);
            return result;
        }

        /// <summary>
        /// Computes the total polyline length in decimal feet.
        /// </summary>
        public static double ComputeTotalLength(IReadOnlyList<Vec3> points)
        {
            if (points == null || points.Count < 2)
                return 0.0;

            double total = 0.0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                total += points[i].DistanceTo(points[i + 1]);
            }
            return total;
        }

        /// <summary>
        /// Counts the number of bends (turns between non-collinear segments) in the polyline.
        /// </summary>
        public static int CountBends(IReadOnlyList<Vec3> corners)
        {
            if (corners == null || corners.Count <= 2)
                return 0;

            int bends = 0;
            for (int i = 1; i < corners.Count - 1; i++)
            {
                Vec3 v1 = corners[i] - corners[i - 1];
                Vec3 v2 = corners[i + 1] - corners[i];

                double len1 = v1.Length();
                double len2 = v2.Length();

                if (len1 > Vec3.Tolerance && len2 > Vec3.Tolerance)
                {
                    Vec3 dir1 = v1 / len1;
                    Vec3 dir2 = v2 / len2;
                    if (!dir1.Equals(dir2))
                    {
                        bends++;
                    }
                }
            }
            return bends;
        }
    }
}
