using System;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Represents the search state in the 3D rectilinear routing graph.
    /// Encapsulates cell position, incoming direction, and the count of consecutive straight moves.
    /// </summary>
    public readonly struct RouteState : IEquatable<RouteState>
    {
        public GridCell Cell { get; }
        public Direction3D Direction { get; }
        public int RunLength { get; }

        public RouteState(GridCell cell, Direction3D direction, int runLength)
        {
            Cell = cell;
            Direction = direction;
            RunLength = runLength;
        }

        public bool Equals(RouteState other) =>
            Cell.Equals(other.Cell) &&
            Direction == other.Direction &&
            RunLength == other.RunLength;

        public override bool Equals(object? obj) => obj is RouteState other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Cell.GetHashCode();
                hash = (hash * 397) ^ (int)Direction;
                hash = (hash * 397) ^ RunLength;
                return hash;
            }
        }

        public static bool operator ==(RouteState left, RouteState right) => left.Equals(right);

        public static bool operator !=(RouteState left, RouteState right) => !left.Equals(right);

        public override string ToString() => $"State[{Cell}, Dir={Direction}, Run={RunLength}]";
    }
}
