using System;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Spatial
{
    /// <summary>
    /// Represents an axis-aligned box obstacle in Revit internal decimal feet.
    /// </summary>
    public class BoxObstacle : IObstacle
    {
        public string Id { get; }
        public Aabb Bounds { get; }

        public BoxObstacle(string id, Aabb bounds)
        {
            Id = id ?? Guid.NewGuid().ToString("N");
            Bounds = bounds;
        }

        public BoxObstacle(string id, Vec3 min, Vec3 max)
            : this(id, new Aabb(min, max))
        {
        }

        public bool IntersectsCell(Aabb cellAabb)
        {
            return Bounds.Intersects(cellAabb);
        }

        public bool IntersectsSegment(Vec3 p1, Vec3 p2)
        {
            // Ray/Segment slab intersection test against AABB
            Vec3 dir = p2 - p1;
            double len = dir.Length();
            if (len < Vec3.Tolerance)
            {
                return Bounds.Contains(p1);
            }

            double tMin = 0.0;
            double tMax = 1.0;

            // X axis
            if (!ClipAxis(p1.X, dir.X, Bounds.Min.X, Bounds.Max.X, ref tMin, ref tMax))
                return false;

            // Y axis
            if (!ClipAxis(p1.Y, dir.Y, Bounds.Min.Y, Bounds.Max.Y, ref tMin, ref tMax))
                return false;

            // Z axis
            if (!ClipAxis(p1.Z, dir.Z, Bounds.Min.Z, Bounds.Max.Z, ref tMin, ref tMax))
                return false;

            return tMin <= tMax;
        }

        public IObstacle Inflate(double marginFeet)
        {
            return new BoxObstacle($"{Id}_inflated", Bounds.Inflate(marginFeet));
        }

        private static bool ClipAxis(double start, double delta, double min, double max, ref double tMin, ref double tMax)
        {
            if (Math.Abs(delta) < 1e-9)
            {
                return start >= min - 1e-7 && start <= max + 1e-7;
            }

            double invDelta = 1.0 / delta;
            double t1 = (min - start) * invDelta;
            double t2 = (max - start) * invDelta;

            if (t1 > t2)
            {
                double temp = t1;
                t1 = t2;
                t2 = temp;
            }

            tMin = Math.Max(tMin, t1);
            tMax = Math.Min(tMax, t2);

            return tMin <= tMax;
        }
    }
}
