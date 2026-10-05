using System;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Represents the 6 cardinal directions along the Cartesian axes in right-handed 3D space.
    /// </summary>
    public enum Direction3D : byte
    {
        None = 0,
        PositiveX = 1,
        NegativeX = 2,
        PositiveY = 3,
        NegativeY = 4,
        PositiveZ = 5,
        NegativeZ = 6
    }

    /// <summary>
    /// Extension methods for <see cref="Direction3D"/>.
    /// </summary>
    public static class Direction3DExtensions
    {
        public static readonly Direction3D[] AllCardinal = new[]
        {
            Direction3D.PositiveX,
            Direction3D.NegativeX,
            Direction3D.PositiveY,
            Direction3D.NegativeY,
            Direction3D.PositiveZ,
            Direction3D.NegativeZ
        };

        public static bool IsOpposite(this Direction3D a, Direction3D b)
        {
            return (a == Direction3D.PositiveX && b == Direction3D.NegativeX) ||
                   (a == Direction3D.NegativeX && b == Direction3D.PositiveX) ||
                   (a == Direction3D.PositiveY && b == Direction3D.NegativeY) ||
                   (a == Direction3D.NegativeY && b == Direction3D.PositiveY) ||
                   (a == Direction3D.PositiveZ && b == Direction3D.NegativeZ) ||
                   (a == Direction3D.NegativeZ && b == Direction3D.PositiveZ);
        }

        public static bool IsVertical(this Direction3D dir)
        {
            return dir == Direction3D.PositiveZ || dir == Direction3D.NegativeZ;
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
                default: return (0, 0, 0);
            }
        }

        public static Vec3 ToUnitVector(this Direction3D dir)
        {
            var (dx, dy, dz) = dir.ToOffset();
            return new Vec3(dx, dy, dz);
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
