using System;

namespace SmartMepRouter.Core.Spatial
{
    /// <summary>
    /// Represents integer coordinates of a discrete cell in the rectilinear grid.
    /// </summary>
    public readonly struct GridCell : IEquatable<GridCell>
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public GridCell(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static GridCell operator +(GridCell a, (int dx, int dy, int dz) offset) =>
            new GridCell(a.X + offset.dx, a.Y + offset.dy, a.Z + offset.dz);

        public bool Equals(GridCell other) => X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object? obj) => obj is GridCell other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X * 73856093;
                hash = (hash ^ (Y * 19349663)) ^ (Z * 83492791);
                return hash;
            }
        }

        public static bool operator ==(GridCell left, GridCell right) => left.Equals(right);

        public static bool operator !=(GridCell left, GridCell right) => !left.Equals(right);

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
