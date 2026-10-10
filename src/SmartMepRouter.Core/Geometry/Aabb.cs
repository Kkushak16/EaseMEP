using System;

namespace SmartMepRouter.Core.Geometry
{
    /// <summary>
    /// Represents an axis-aligned bounding box (AABB) in Revit internal decimal feet.
    /// </summary>
    public readonly struct Aabb : IEquatable<Aabb>
    {
        public Vec3 Min { get; }
        public Vec3 Max { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Aabb"/> from min and max points in decimal feet.
        /// Automatically normalizes coordinates if min > max.
        /// </summary>
        public Aabb(Vec3 min, Vec3 max)
        {
            Min = new Vec3(
                Math.Min(min.X, max.X),
                Math.Min(min.Y, max.Y),
                Math.Min(min.Z, max.Z));

            Max = new Vec3(
                Math.Max(min.X, max.X),
                Math.Max(min.Y, max.Y),
                Math.Max(min.Z, max.Z));
        }

        public Vec3 Center => (Min + Max) * 0.5;

        public Vec3 Extents => (Max - Min) * 0.5;

        public Vec3 Size => Max - Min;

        public double Volume => Math.Max(0.0, (Max.X - Min.X) * (Max.Y - Min.Y) * (Max.Z - Min.Z));

        /// <summary>
        /// Returns true if this AABB contains the specified point in decimal feet.
        /// </summary>
        public bool Contains(Vec3 point) =>
            point.X >= Min.X - Vec3.Tolerance && point.X <= Max.X + Vec3.Tolerance &&
            point.Y >= Min.Y - Vec3.Tolerance && point.Y <= Max.Y + Vec3.Tolerance &&
            point.Z >= Min.Z - Vec3.Tolerance && point.Z <= Max.Z + Vec3.Tolerance;

        /// <summary>
        /// Returns true if this AABB intersects with another AABB.
        /// </summary>
        public bool Intersects(Aabb other) =>
            Min.X <= other.Max.X + Vec3.Tolerance && Max.X >= other.Min.X - Vec3.Tolerance &&
            Min.Y <= other.Max.Y + Vec3.Tolerance && Max.Y >= other.Min.Y - Vec3.Tolerance &&
            Min.Z <= other.Max.Z + Vec3.Tolerance && Max.Z >= other.Min.Z - Vec3.Tolerance;

        /// <summary>
        /// Returns a new AABB inflated uniformly by the given margin in decimal feet.
        /// </summary>
        public Aabb Inflate(double marginFeet)
        {
            Vec3 delta = new Vec3(marginFeet, marginFeet, marginFeet);
            return new Aabb(Min - delta, Max + delta);
        }

        /// <summary>
        /// Returns a new AABB inflated independently on each axis by the given vector in decimal feet.
        /// </summary>
        public Aabb Inflate(Vec3 margins)
        {
            return new Aabb(Min - margins, Max + margins);
        }

        /// <summary>
        /// Returns a new AABB inflated along each axis individually in decimal feet.
        /// </summary>
        public Aabb Inflate(double marginXFeet, double marginYFeet, double marginZFeet)
        {
            Vec3 delta = new Vec3(marginXFeet, marginYFeet, marginZFeet);
            return new Aabb(Min - delta, Max + delta);
        }

        /// <summary>
        /// Constructs an AABB enclosing two points expanded by an optional margin in decimal feet.
        /// </summary>
        public static Aabb FromPoints(Vec3 p1, Vec3 p2, double marginFeet = 0.0, double? minZ = null, double? maxZ = null)
        {
            double minX = Math.Min(p1.X, p2.X) - marginFeet;
            double maxX = Math.Max(p1.X, p2.X) + marginFeet;
            double minY = Math.Min(p1.Y, p2.Y) - marginFeet;
            double maxY = Math.Max(p1.Y, p2.Y) + marginFeet;
            double z1 = minZ ?? (Math.Min(p1.Z, p2.Z) - marginFeet);
            double z2 = maxZ ?? (Math.Max(p1.Z, p2.Z) + marginFeet);
            return new Aabb(new Vec3(minX, minY, Math.Min(z1, z2)), new Vec3(maxX, maxY, Math.Max(z1, z2)));
        }

        public bool Equals(Aabb other) => Min.Equals(other.Min) && Max.Equals(other.Max);

        public override bool Equals(object? obj) => obj is Aabb other && Equals(other);

        public override int GetHashCode() => unchecked((Min.GetHashCode() * 397) ^ Max.GetHashCode());

        public override string ToString() => $"[Min: {Min}, Max: {Max}]";
    }
}
