using System;

namespace SmartMepRouter.Core.Geometry
{
    /// <summary>
    /// Represents an immutable 3D vector or point in Revit internal decimal feet.
    /// Right-handed coordinate frame: +X East, +Y North, +Z Up.
    /// </summary>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public const double Tolerance = 1e-6;

        public static readonly Vec3 Zero = new Vec3(0, 0, 0);
        public static readonly Vec3 UnitX = new Vec3(1, 0, 0);
        public static readonly Vec3 UnitY = new Vec3(0, 1, 0);
        public static readonly Vec3 UnitZ = new Vec3(0, 0, 1);

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Vec3"/> with coordinates in decimal feet.
        /// </summary>
        public Vec3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vec3 operator -(Vec3 a) => new Vec3(-a.X, -a.Y, -a.Z);

        public static Vec3 operator *(Vec3 a, double scalar) => new Vec3(a.X * scalar, a.Y * scalar, a.Z * scalar);

        public static Vec3 operator *(double scalar, Vec3 a) => new Vec3(a.X * scalar, a.Y * scalar, a.Z * scalar);

        public static Vec3 operator /(Vec3 a, double scalar)
        {
            if (Math.Abs(scalar) < Tolerance)
                throw new DivideByZeroException("Cannot divide Vec3 by zero.");
            return new Vec3(a.X / scalar, a.Y / scalar, a.Z / scalar);
        }

        public static bool operator ==(Vec3 a, Vec3 b) => a.Equals(b);

        public static bool operator !=(Vec3 a, Vec3 b) => !a.Equals(b);

        public double LengthSquared() => X * X + Y * Y + Z * Z;

        public double Length() => Math.Sqrt(LengthSquared());

        public double DistanceTo(Vec3 other) => (this - other).Length();

        public double ManhattanDistanceTo(Vec3 other) =>
            Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Z - other.Z);

        public Vec3 Normalize()
        {
            double len = Length();
            if (len < Tolerance)
                return Zero;
            return this / len;
        }

        public bool Equals(Vec3 other) =>
            Math.Abs(X - other.X) <= Tolerance &&
            Math.Abs(Y - other.Y) <= Tolerance &&
            Math.Abs(Z - other.Z) <= Tolerance;

        public override bool Equals(object? obj) => obj is Vec3 other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)(Math.Round(X / Tolerance) * 397);
                hash = (hash * 397) ^ (int)(Math.Round(Y / Tolerance) * 397);
                hash = (hash * 397) ^ (int)(Math.Round(Z / Tolerance) * 397);
                return hash;
            }
        }

        public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4}) ft";
    }
}
