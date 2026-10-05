using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Spatial
{
    /// <summary>
    /// Represents a sparse, corridor-bounded 3D rectilinear grid.
    /// Uses decimal feet for spatial dimensions and aligns origin to the start connector.
    /// </summary>
    public class SparseGrid
    {
        private readonly HashSet<GridCell> _blockedCells;

        public Vec3 Origin { get; }
        public double CellSizeFeet { get; }
        public Aabb CorridorBounds { get; }

        public int BlockedCount => _blockedCells.Count;

        /// <summary>
        /// Initializes a new instance of <see cref="SparseGrid"/>.
        /// </summary>
        /// <param name="origin">Origin point in decimal feet (typically start connector position).</param>
        /// <param name="cellSizeFeet">Side length of each cubic grid cell in decimal feet.</param>
        /// <param name="corridorBounds">Bounding box in decimal feet within which cells are valid.</param>
        public SparseGrid(Vec3 origin, double cellSizeFeet, Aabb corridorBounds)
        {
            if (cellSizeFeet <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(cellSizeFeet), "Cell size must be strictly positive.");

            Origin = origin;
            CellSizeFeet = cellSizeFeet;
            CorridorBounds = corridorBounds;
            _blockedCells = new HashSet<GridCell>();
        }

        /// <summary>
        /// Converts a continuous coordinate in decimal feet to integer grid cell coordinates.
        /// </summary>
        public GridCell WorldToCell(Vec3 world)
        {
            int x = (int)Math.Round((world.X - Origin.X) / CellSizeFeet);
            int y = (int)Math.Round((world.Y - Origin.Y) / CellSizeFeet);
            int z = (int)Math.Round((world.Z - Origin.Z) / CellSizeFeet);
            return new GridCell(x, y, z);
        }

        /// <summary>
        /// Converts integer grid cell coordinates to continuous world coordinates in decimal feet.
        /// </summary>
        public Vec3 CellToWorld(GridCell cell)
        {
            return new Vec3(
                Origin.X + cell.X * CellSizeFeet,
                Origin.Y + cell.Y * CellSizeFeet,
                Origin.Z + cell.Z * CellSizeFeet);
        }

        /// <summary>
        /// Returns true if the cell is inside the corridor bounding box.
        /// </summary>
        public bool IsInsideCorridor(GridCell cell)
        {
            Vec3 pos = CellToWorld(cell);
            return CorridorBounds.Contains(pos);
        }

        /// <summary>
        /// Checks if a cell is blocked or outside the allowed corridor bounds.
        /// </summary>
        public bool IsBlocked(GridCell cell)
        {
            if (!IsInsideCorridor(cell))
                return true;
            return _blockedCells.Contains(cell);
        }

        /// <summary>
        /// Explicitly marks a cell as blocked or unblocked.
        /// </summary>
        public void SetBlocked(GridCell cell, bool blocked = true)
        {
            if (blocked)
                _blockedCells.Add(cell);
            else
                _blockedCells.Remove(cell);
        }

        /// <summary>
        /// Marks all grid cells intersecting the given AABB as blocked.
        /// </summary>
        public void BlockAabb(Aabb box)
        {
            GridCell minCell = WorldToCell(box.Min);
            GridCell maxCell = WorldToCell(box.Max);

            int minX = Math.Min(minCell.X, maxCell.X);
            int maxX = Math.Max(minCell.X, maxCell.X);
            int minY = Math.Min(minCell.Y, maxCell.Y);
            int maxY = Math.Max(minCell.Y, maxCell.Y);
            int minZ = Math.Min(minCell.Z, maxCell.Z);
            int maxZ = Math.Max(minCell.Z, maxCell.Z);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        GridCell cell = new GridCell(x, y, z);
                        if (IsInsideCorridor(cell))
                        {
                            _blockedCells.Add(cell);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the bounding box of a specific cell in decimal feet.
        /// </summary>
        public Aabb GetCellBounds(GridCell cell)
        {
            double half = CellSizeFeet * 0.5;
            Vec3 center = CellToWorld(cell);
            return new Aabb(
                new Vec3(center.X - half, center.Y - half, center.Z - half),
                new Vec3(center.X + half, center.Y + half, center.Z + half));
        }

        /// <summary>
        /// Conservative voxelization of any obstacle into the sparse grid.
        /// Uses exact cell intersection (including 15-axis SAT for OBBs).
        /// </summary>
        public void BlockObstacle(IObstacle obstacle)
        {
            GridCell minCell = WorldToCell(obstacle.Bounds.Min);
            GridCell maxCell = WorldToCell(obstacle.Bounds.Max);

            int minX = Math.Min(minCell.X, maxCell.X);
            int maxX = Math.Max(minCell.X, maxCell.X);
            int minY = Math.Min(minCell.Y, maxCell.Y);
            int maxY = Math.Max(minCell.Y, maxCell.Y);
            int minZ = Math.Min(minCell.Z, maxCell.Z);
            int maxZ = Math.Max(minCell.Z, maxCell.Z);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        GridCell cell = new GridCell(x, y, z);
                        if (IsInsideCorridor(cell))
                        {
                            Aabb cellBounds = GetCellBounds(cell);
                            if (obstacle.IntersectsCell(cellBounds))
                            {
                                _blockedCells.Add(cell);
                            }
                        }
                    }
                }
            }
        }
    }
}
