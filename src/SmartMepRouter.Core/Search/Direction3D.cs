using System;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Represents 3D directions including the 6 cardinal directions and 12 planar 45-degree diagonals
    /// used in standard HVAC ductwork fabrication (horizontal XY, vertical XZ, vertical YZ).
    /// </summary>
    public enum Direction3D : byte
    {
        None = 0,

        // 6 Cardinal directions (90° orthogonal)
        PositiveX = 1,
        NegativeX = 2,
        PositiveY = 3,
        NegativeY = 4,
        PositiveZ = 5,
        NegativeZ = 6,

        // 4 Horizontal 45° planar diagonals (XY plane)
        PosXPosY = 7,
        PosXNegY = 8,
        NegXPosY = 9,
        NegXNegY = 10,

        // 4 Vertical 45° planar diagonals (XZ plane - elevation offsets along X)
        PosXPosZ = 11,
        PosXNegZ = 12,
        NegXPosZ = 13,
        NegXNegZ = 14,

        // 4 Vertical 45° planar diagonals (YZ plane - elevation offsets along Y)
        PosYPosZ = 15,
        PosYNegZ = 16,
        NegYPosZ = 17,
        NegYNegZ = 18
    }

    /// <summary>
    /// Extension methods and transition rules for <see cref="Direction3D"/>.
    /// </summary>
    public static class Direction3DExtensions
    {
        public const double Sqrt2 = 1.4142135623730950488;
        public const double InvSqrt2 = 0.7071067811865475244;

        public static readonly Direction3D[] AllCardinal = new[]
        {
            Direction3D.PositiveX,
            Direction3D.NegativeX,
            Direction3D.PositiveY,
            Direction3D.NegativeY,
            Direction3D.PositiveZ,
            Direction3D.NegativeZ
        };

        public static readonly Direction3D[] AllPlanarDiagonal = new[]
        {
            // XY horizontal
            Direction3D.PosXPosY,
            Direction3D.PosXNegY,
            Direction3D.NegXPosY,
            Direction3D.NegXNegY,
            // XZ vertical
            Direction3D.PosXPosZ,
            Direction3D.PosXNegZ,
            Direction3D.NegXPosZ,
            Direction3D.NegXNegZ,
            // YZ vertical
            Direction3D.PosYPosZ,
            Direction3D.PosYNegZ,
            Direction3D.NegYPosZ,
            Direction3D.NegYNegZ
        };

        public static readonly Direction3D[] AllDirections = new[]
        {
            Direction3D.PositiveX,
            Direction3D.NegativeX,
            Direction3D.PositiveY,
            Direction3D.NegativeY,
            Direction3D.PositiveZ,
            Direction3D.NegativeZ,
            Direction3D.PosXPosY,
            Direction3D.PosXNegY,
            Direction3D.NegXPosY,
            Direction3D.NegXNegY,
            Direction3D.PosXPosZ,
            Direction3D.PosXNegZ,
            Direction3D.NegXPosZ,
            Direction3D.NegXNegZ,
            Direction3D.PosYPosZ,
            Direction3D.PosYNegZ,
            Direction3D.NegYPosZ,
            Direction3D.NegYNegZ
        };

        public static bool IsDiagonal(this Direction3D dir)
        {
            return (byte)dir >= 7 && (byte)dir <= 18;
        }

        public static bool IsVertical(this Direction3D dir)
        {
            var (_, _, dz) = dir.ToOffset();
            return dz != 0;
        }

        public static double StepDistanceRatio(this Direction3D dir)
        {
            return dir.IsDiagonal() ? Sqrt2 : 1.0;
        }

        public static bool IsOpposite(this Direction3D a, Direction3D b)
        {
            if (a == Direction3D.None || b == Direction3D.None) return false;
            var (ax, ay, az) = a.ToOffset();
            var (bx, by, bz) = b.ToOffset();
            return (ax == -bx && ay == -by && az == -bz);
        }

        public static (int dx, int dy, int dz) ToOffset(this Direction3D dir)
        {
            switch (dir)
            {
                case Direction3D.PositiveX: return (1, 0, 0);
                case Direction3D.NegativeX: return (-1, 0, 0);
                case Direction3D.PositiveY: return (0, 1, 0);
                case Direction3D.NegativeY: return (0, -1, 0);
                case Direction3D.PositiveZ: return (0, 0, 1);
                case Direction3D.NegativeZ: return (0, 0, -1);

                // XY planar diagonals
                case Direction3D.PosXPosY: return (1, 1, 0);
                case Direction3D.PosXNegY: return (1, -1, 0);
                case Direction3D.NegXPosY: return (-1, 1, 0);
                case Direction3D.NegXNegY: return (-1, -1, 0);

                // XZ planar diagonals
                case Direction3D.PosXPosZ: return (1, 0, 1);
                case Direction3D.PosXNegZ: return (1, 0, -1);
                case Direction3D.NegXPosZ: return (-1, 0, 1);
                case Direction3D.NegXNegZ: return (-1, 0, -1);

                // YZ planar diagonals
                case Direction3D.PosYPosZ: return (0, 1, 1);
                case Direction3D.PosYNegZ: return (0, 1, -1);
                case Direction3D.NegYPosZ: return (0, -1, 1);
                case Direction3D.NegYNegZ: return (0, -1, -1);

                default: return (0, 0, 0);
            }
        }

        /// <summary>
        /// Returns the two orthogonal flanking cell offsets that must be free of obstacles to prevent corner cutting
        /// when moving along this diagonal direction. Returns zeroes for cardinal directions.
        /// </summary>
        public static ((int dx, int dy, int dz) f1, (int dx, int dy, int dz) f2) GetFlankingOffsets(this Direction3D dir)
        {
            switch (dir)
            {
                // XY plane: flanking cells are (dx, 0, 0) and (0, dy, 0)
                case Direction3D.PosXPosY: return ((1, 0, 0), (0, 1, 0));
                case Direction3D.PosXNegY: return ((1, 0, 0), (0, -1, 0));
                case Direction3D.NegXPosY: return ((-1, 0, 0), (0, 1, 0));
                case Direction3D.NegXNegY: return ((-1, 0, 0), (0, -1, 0));

                // XZ plane: flanking cells are (dx, 0, 0) and (0, 0, dz)
                case Direction3D.PosXPosZ: return ((1, 0, 0), (0, 0, 1));
                case Direction3D.PosXNegZ: return ((1, 0, 0), (0, 0, -1));
                case Direction3D.NegXPosZ: return ((-1, 0, 0), (0, 0, 1));
                case Direction3D.NegXNegZ: return ((-1, 0, 0), (0, 0, -1));

                // YZ plane: flanking cells are (0, dy, 0) and (0, 0, dz)
                case Direction3D.PosYPosZ: return ((0, 1, 0), (0, 0, 1));
                case Direction3D.PosYNegZ: return ((0, 1, 0), (0, 0, -1));
                case Direction3D.NegYPosZ: return ((0, -1, 0), (0, 0, 1));
                case Direction3D.NegYNegZ: return ((0, -1, 0), (0, 0, -1));

                default: return ((0, 0, 0), (0, 0, 0));
            }
        }

        public static Vec3 ToUnitVector(this Direction3D dir)
        {
            var (dx, dy, dz) = dir.ToOffset();
            if (dir.IsDiagonal())
            {
                return new Vec3(dx * InvSqrt2, dy * InvSqrt2, dz * InvSqrt2);
            }
            return new Vec3(dx, dy, dz);
        }

        /// <summary>
        /// Calculates the angle in degrees between two 3D directions.
        /// </summary>
        public static double AngleDegrees(Direction3D from, Direction3D to)
        {
            if (from == Direction3D.None || to == Direction3D.None) return 0.0;
            if (from == to) return 0.0;

            Vec3 u1 = from.ToUnitVector();
            Vec3 u2 = to.ToUnitVector();

            double dot = Math.Max(-1.0, Math.Min(1.0, u1.Dot(u2)));
            return Math.Acos(dot) * (180.0 / Math.PI);
        }

        /// <summary>
        /// Validates if a transition between two directions corresponds to a standard HVAC duct elbow
        /// (strictly 0° straight run, 45° standard elbow, or 90° standard elbow).
        /// Forbids non-coplanar twists, acute angles (&gt;90° bend), and non-standard angles.
        /// </summary>
        public static bool IsValidHVACTransition(Direction3D from, Direction3D to, out double angleDeg)
        {
            angleDeg = 0.0;
            if (from == Direction3D.None) return true;
            if (from == to) return true;

            if (from.IsOpposite(to)) return false;

            angleDeg = AngleDegrees(from, to);

            // Straight run
            if (angleDeg < 1.0) return true;

            // Standard 45° elbow (44.0° to 46.0°)
            if (Math.Abs(angleDeg - 45.0) < 1.0)
            {
                // In rectangular ductwork, ensure both directions share a common plane
                return AreCoplanar(from, to);
            }

            // Standard 90° elbow (89.0° to 91.0°)
            if (Math.Abs(angleDeg - 90.0) < 1.0)
            {
                return AreCoplanar(from, to);
            }

            return false;
        }

        /// <summary>
        /// Ensures two directions lie in one of the primary Cartesian planes (XY, XZ, or YZ).
        /// Prevents out-of-plane torsional twisting in rectangular ductwork.
        /// </summary>
        public static bool AreCoplanar(Direction3D a, Direction3D b)
        {
            var (ax, ay, az) = a.ToOffset();
            var (bx, by, bz) = b.ToOffset();

            // XY plane (both have dz == 0)
            if (az == 0 && bz == 0) return true;

            // XZ plane (both have dy == 0)
            if (ay == 0 && by == 0) return true;

            // YZ plane (both have dx == 0)
            if (ax == 0 && bx == 0) return true;

            return false;
        }

        public static Direction3D FromVector(Vec3 vec)
        {
            double absX = Math.Abs(vec.X);
            double absY = Math.Abs(vec.Y);
            double absZ = Math.Abs(vec.Z);

            if (absX >= absY && absX >= absZ)
                return vec.X >= 0 ? Direction3D.PositiveX : Direction3D.NegativeX;
            if (absY >= absX && absY >= absZ)
                return vec.Y >= 0 ? Direction3D.PositiveY : Direction3D.NegativeY;
            return vec.Z >= 0 ? Direction3D.PositiveZ : Direction3D.NegativeZ;
        }
    }
}
