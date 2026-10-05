using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Post
{
    /// <summary>
    /// Aligns route endpoints and adjacent intermediate corners to exact connector coordinates
    /// while preserving strict rectilinear orthogonality and re-verifying obstacle collisions (R-T2).
    /// All coordinates are in Revit internal decimal feet.
    /// </summary>
    public static class EndpointAligner
    {
        /// <summary>
        /// Aligns a simplified polyline route to exact start and end connector positions.
        /// </summary>
        /// <param name="corners">Compressed corner vertices from path search.</param>
        /// <param name="exactStart">Exact start connector position in decimal feet.</param>
        /// <param name="exactEnd">Exact end connector position in decimal feet.</param>
        /// <param name="obstacles">Obstacle set for collision re-verification.</param>
        /// <returns>A new list of aligned corners if collision-free, or original corners if adjustment fails.</returns>
        public static List<Vec3> Align(
            IReadOnlyList<Vec3> corners,
            Vec3 exactStart,
            Vec3 exactEnd,
            IReadOnlyList<IObstacle>? obstacles = null)
        {
            if (corners == null || corners.Count < 2)
            {
                return new List<Vec3> { exactStart, exactEnd };
            }

            var aligned = new List<Vec3>(corners);
            aligned[0] = exactStart;
            aligned[aligned.Count - 1] = exactEnd;

            if (aligned.Count == 2)
            {
                // Single straight segment
                if (obstacles != null && !CollisionVerifier.IsPathCollisionFree(aligned, obstacles))
                {
                    return new List<Vec3>(corners);
                }
                return aligned;
            }

            // Align first intermediate corner with exact start position along departure axis
            Vec3 seg0 = corners[1] - corners[0];
            aligned[1] = AlignIntermediateCorner(exactStart, aligned[1], seg0);

            // Align last intermediate corner with exact end position along arrival axis
            int n = aligned.Count;
            Vec3 segLast = corners[n - 1] - corners[n - 2];
            aligned[n - 2] = AlignIntermediateCorner(exactEnd, aligned[n - 2], -segLast);

            // If 4 or more points, ensure intermediate segments between index 1 and n-2 remain orthogonal
            if (aligned.Count == 4)
            {
                // For a 3-segment route: start -> c1 -> c2 -> end
                // Middle segment (c1 -> c2) must also be rectilinear
                AdjustMiddleSegment(aligned);
            }

            // Simplify collinear points that may have collapsed during alignment
            List<Vec3> result = PathSimplifier.CompressCollinear(aligned);

            // Independent collision re-verification (Rule R-T2)
            if (obstacles != null && !CollisionVerifier.IsPathCollisionFree(result, obstacles))
            {
                // Return original route if alignment created a collision
                return new List<Vec3>(corners);
            }

            return result;
        }

        private static Vec3 AlignIntermediateCorner(Vec3 exactPoint, Vec3 corner, Vec3 segmentDir)
        {
            double absX = Math.Abs(segmentDir.X);
            double absY = Math.Abs(segmentDir.Y);
            double absZ = Math.Abs(segmentDir.Z);

            if (absX >= absY && absX >= absZ)
            {
                // Segment is along X: retain corner.X, snap Y and Z to exactPoint
                return new Vec3(corner.X, exactPoint.Y, exactPoint.Z);
            }
            if (absY >= absX && absY >= absZ)
            {
                // Segment is along Y: retain corner.Y, snap X and Z to exactPoint
                return new Vec3(exactPoint.X, corner.Y, exactPoint.Z);
            }
            // Segment is along Z: retain corner.Z, snap X and Y to exactPoint
            return new Vec3(exactPoint.X, exactPoint.Y, corner.Z);
        }

        private static void AdjustMiddleSegment(List<Vec3> pts)
        {
            // pts: [0]=start, [1]=c1, [2]=c2, [3]=end
            // Determine primary axis of middle segment
            Vec3 midDir = pts[2] - pts[1];
            double absX = Math.Abs(midDir.X);
            double absY = Math.Abs(midDir.Y);
            double absZ = Math.Abs(midDir.Z);

            if (absX >= absY && absX >= absZ)
            {
                // Middle segment is primarily along X: equalize Y and Z
                pts[2] = new Vec3(pts[2].X, pts[1].Y, pts[1].Z);
            }
            else if (absY >= absX && absY >= absZ)
            {
                // Middle segment is primarily along Y: equalize X and Z
                pts[2] = new Vec3(pts[1].X, pts[2].Y, pts[1].Z);
            }
            else
            {
                // Middle segment is primarily along Z: equalize X and Y
                pts[2] = new Vec3(pts[1].X, pts[1].Y, pts[2].Z);
            }
        }
    }
}
