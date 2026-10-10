using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Spatial
{
    /// <summary>
    /// Discrete 3D spatial hash key for bucket indexing.
    /// Implemented as a readonly struct to guarantee zero GC allocations during lookup.
    /// </summary>
    public readonly struct SpatialKey : IEquatable<SpatialKey>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public SpatialKey(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(SpatialKey other) => X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object? obj) => obj is SpatialKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X * 73856093;
                hash = (hash ^ (Y * 19349663));
                hash = (hash ^ (Z * 83492791));
                return hash;
            }
        }

        public override string ToString() => $"({X}, {Y}, {Z})";
    }

    /// <summary>
    /// High-performance uniform 3D spatial hashing acceleration structure (inspired by ofxSpatialHash and Bowerbird).
    /// Provides O(1) broadphase obstacle lookup for collision testing and dynamic multi-run space reservation.
    /// </summary>
    /// <typeparam name="T">Payload type stored in spatial buckets (e.g. <see cref="IObstacle"/> or route element).</typeparam>
    public class SpatialHash3D<T> where T : notnull
    {
        private readonly double _cellSize;
        private readonly double _invCellSize;
        private readonly Dictionary<SpatialKey, List<T>> _buckets;
        private readonly Dictionary<T, Aabb> _itemBounds;

        /// <summary>Cell edge dimension in decimal feet.</summary>
        public double CellSize => _cellSize;

        /// <summary>Total number of unique spatial buckets currently active.</summary>
        public int BucketCount => _buckets.Count;

        /// <summary>Total number of registered items.</summary>
        public int ItemCount => _itemBounds.Count;

        public SpatialHash3D(double cellSizeFeet = 5.0)
        {
            if (cellSizeFeet <= 0.0) throw new ArgumentOutOfRangeException(nameof(cellSizeFeet), "Cell size must be strictly positive.");
            _cellSize = cellSizeFeet;
            _invCellSize = 1.0 / cellSizeFeet;
            _buckets = new Dictionary<SpatialKey, List<T>>(128);
            _itemBounds = new Dictionary<T, Aabb>(64);
        }

        /// <summary>
        /// Converts world coordinates to a discrete bucket key.
        /// </summary>
        public SpatialKey WorldToKey(Vec3 point)
        {
            return new SpatialKey(
                (int)Math.Floor(point.X * _invCellSize),
                (int)Math.Floor(point.Y * _invCellSize),
                (int)Math.Floor(point.Z * _invCellSize));
        }

        /// <summary>
        /// Inserts an item into all spatial buckets overlapping its bounding box.
        /// </summary>
        public void Insert(T item, Aabb bounds)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _itemBounds[item] = bounds;

            int minX = (int)Math.Floor(bounds.Min.X * _invCellSize);
            int maxX = (int)Math.Floor(bounds.Max.X * _invCellSize);
            int minY = (int)Math.Floor(bounds.Min.Y * _invCellSize);
            int maxY = (int)Math.Floor(bounds.Max.Y * _invCellSize);
            int minZ = (int)Math.Floor(bounds.Min.Z * _invCellSize);
            int maxZ = (int)Math.Floor(bounds.Max.Z * _invCellSize);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        var key = new SpatialKey(x, y, z);
                        if (!_buckets.TryGetValue(key, out var list))
                        {
                            list = new List<T>(4);
                            _buckets[key] = list;
                        }
                        if (!list.Contains(item))
                        {
                            list.Add(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Queries all candidate items within buckets overlapping the query bounding box.
        /// Deduplicates items using the provided result set.
        /// </summary>
        public void Query(Aabb queryBounds, ISet<T> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            int minX = (int)Math.Floor(queryBounds.Min.X * _invCellSize);
            int maxX = (int)Math.Floor(queryBounds.Max.X * _invCellSize);
            int minY = (int)Math.Floor(queryBounds.Min.Y * _invCellSize);
            int maxY = (int)Math.Floor(queryBounds.Max.Y * _invCellSize);
            int minZ = (int)Math.Floor(queryBounds.Min.Z * _invCellSize);
            int maxZ = (int)Math.Floor(queryBounds.Max.Z * _invCellSize);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        var key = new SpatialKey(x, y, z);
                        if (_buckets.TryGetValue(key, out var list))
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                results.Add(list[i]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Queries all candidate items overlapping the bounding box of a 3D segment.
        /// </summary>
        public void QuerySegment(Vec3 p1, Vec3 p2, ISet<T> results, double margin = 0.0)
        {
            var bounds = new Aabb(
                new Vec3(Math.Min(p1.X, p2.X) - margin, Math.Min(p1.Y, p2.Y) - margin, Math.Min(p1.Z, p2.Z) - margin),
                new Vec3(Math.Max(p1.X, p2.X) + margin, Math.Max(p1.Y, p2.Y) + margin, Math.Max(p1.Z, p2.Z) + margin));

            Query(bounds, results);
        }

        /// <summary>
        /// Clears all spatial buckets and stored items.
        /// </summary>
        public void Clear()
        {
            _buckets.Clear();
            _itemBounds.Clear();
        }
    }
}
