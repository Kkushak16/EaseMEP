using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Search;

namespace SmartMepRouter.Core.Spatial
{
    /// <summary>
    /// Represents a sparse, corridor-bounded 3D rectilinear grid.
    /// Uses decimal feet for spatial dimensions and aligns origin to the start connector.
    /// Tracks blocking obstacle IDs per cell for diagnostic failure reporting (FR-13).
    /// </summary>
    public class SparseGrid
    {
        private readonly HashSet<GridCell> _blockedCells;
        private readonly HashSet<GridCell>? _blockedCellsX;
        private readonly HashSet<GridCell>? _blockedCellsY;
        private readonly HashSet<GridCell>? _blockedCellsZ;
        private readonly Dictionary<GridCell, List<string>> _cellObstacleIds;

        public Vec3 Origin { get; }
        public double CellSizeFeet { get; }
        public Aabb CorridorBounds { get; }
        public bool HasDirectionalBlocking { get; }

        public int BlockedCount => _blockedCells.Count;
        public int BlockedCountX => _blockedCellsX?.Count ?? _blockedCells.Count;
        public int BlockedCountY => _blockedCellsY?.Count ?? _blockedCells.Count;
        public int BlockedCountZ => _blockedCellsZ?.Count ?? _blockedCells.Count;

        /// <summary>
        /// Initializes a new instance of <see cref="SparseGrid"/>.
        /// </summary>
        /// <param name="origin">Origin point in decimal feet (typically start connector position).</param>
        /// <param name="cellSizeFeet">Side length of each cubic grid cell in decimal feet.</param>
        /// <param name="corridorBounds">Bounding box in decimal feet within which cells are valid.</param>
        /// <param name="enableDirectional">If true, allocates separate blocked cell sets per movement axis (FR-17).</param>
        public SparseGrid(Vec3 origin, double cellSizeFeet, Aabb corridorBounds, bool enableDirectional = false)
        {
            if (cellSizeFeet <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(cellSizeFeet), "Cell size must be strictly positive.");

            Origin = origin;
            CellSizeFeet = cellSizeFeet;
            CorridorBounds = corridorBounds;
            _blockedCells = new HashSet<GridCell>();
            _cellObstacleIds = new Dictionary<GridCell, List<string>>();

            HasDirectionalBlocking = enableDirectional;
            if (enableDirectional)
            {
                _blockedCellsX = new HashSet<GridCell>();
                _blockedCellsY = new HashSet<GridCell>();
                _blockedCellsZ = new HashSet<GridCell>();
            }
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
        /// When directional blocking is enabled, checks the specific movement axis blocked set.
        /// </summary>
        public bool IsBlocked(GridCell cell, Direction3D direction = Direction3D.None)
        {
            if (!IsInsideCorridor(cell))
                return true;

            if (HasDirectionalBlocking && direction != Direction3D.None)
            {
                switch (direction)
                {
                    case Direction3D.PositiveX:
                    case Direction3D.NegativeX:
                        return _blockedCellsX!.Contains(cell);

                    case Direction3D.PositiveY:
                    case Direction3D.NegativeY:
                        return _blockedCellsY!.Contains(cell);

                    case Direction3D.PositiveZ:
                    case Direction3D.NegativeZ:
                        return _blockedCellsZ!.Contains(cell);

                    // XY planar diagonals (cross-section occupies both X and Y inflation sets)
                    case Direction3D.PosXPosY:
                    case Direction3D.PosXNegY:
                    case Direction3D.NegXPosY:
                    case Direction3D.NegXNegY:
                        return _blockedCellsX!.Contains(cell) || _blockedCellsY!.Contains(cell);

                    // XZ planar diagonals
                    case Direction3D.PosXPosZ:
                    case Direction3D.PosXNegZ:
                    case Direction3D.NegXPosZ:
                    case Direction3D.NegXNegZ:
                        return _blockedCellsX!.Contains(cell) || _blockedCellsZ!.Contains(cell);

                    // YZ planar diagonals
                    case Direction3D.PosYPosZ:
                    case Direction3D.PosYNegZ:
                    case Direction3D.NegYPosZ:
                    case Direction3D.NegYNegZ:
                        return _blockedCellsY!.Contains(cell) || _blockedCellsZ!.Contains(cell);
                }
            }

            return _blockedCells.Contains(cell);
        }

        /// <summary>
        /// Retrieves the list of obstacle IDs that contributed to blocking the given cell (FR-13).
        /// </summary>
        public IReadOnlyList<string> GetBlockingObstacleIds(GridCell cell)
        {
            if (_cellObstacleIds.TryGetValue(cell, out var ids))
            {
                return ids;
            }

            if (!IsInsideCorridor(cell))
            {
                return new[] { "CORRIDOR_BOUNDARY" };
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Returns true if the cell lies directly on or within one cell of the corridor bounds.
        /// </summary>
        public bool IsCellOnCorridorBoundary(GridCell cell)
        {
            Vec3 pos = CellToWorld(cell);
            double tol = CellSizeFeet * 1.05;
            return Math.Abs(pos.X - CorridorBounds.Min.X) <= tol ||
                   Math.Abs(pos.X - CorridorBounds.Max.X) <= tol ||
                   Math.Abs(pos.Y - CorridorBounds.Min.Y) <= tol ||
                   Math.Abs(pos.Y - CorridorBounds.Max.Y) <= tol ||
                   Math.Abs(pos.Z - CorridorBounds.Min.Z) <= tol ||
                   Math.Abs(pos.Z - CorridorBounds.Max.Z) <= tol;
        }

        /// <summary>
        /// Explicitly marks a cell as blocked or unblocked.
        /// </summary>
        public void SetBlocked(GridCell cell, bool blocked = true, string? obstacleId = null)
        {
            if (blocked)
            {
                _blockedCells.Add(cell);
                if (!string.IsNullOrEmpty(obstacleId))
                {
                    RecordCellObstacle(cell, obstacleId!);
                }
            }
            else
            {
                _blockedCells.Remove(cell);
                _cellObstacleIds.Remove(cell);
            }
        }

        /// <summary>
        /// Marks all grid cells intersecting the given AABB as blocked.
        /// </summary>
        public void BlockAabb(Aabb box, string? obstacleId = null)
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
                            if (!string.IsNullOrEmpty(obstacleId))
                            {
                                RecordCellObstacle(cell, obstacleId!);
                            }
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
            BlockObstacleToSet(obstacle, _blockedCells);
        }

        /// <summary>
        /// Voxelizes an obstacle with direction-dependent inflation margins per travel axis (FR-17).
        /// For X travel: cross-section is in YZ plane (width along Y, height along Z).
        /// For Y travel: cross-section is in XZ plane (width along X, height along Z).
        /// For Z travel: cross-section is in XY plane (width along X, height along Y).
        /// </summary>
        public void BlockObstacleDirectional(
            IObstacle rawObstacle,
            double clearanceAndInsulationFeet,
            double widthFeet,
            double heightFeet)
        {
            if (!HasDirectionalBlocking)
            {
                // Fallback to isotropic conservative inflation
                double isotropicMargin = clearanceAndInsulationFeet + Math.Max(widthFeet, heightFeet) * 0.5;
                BlockObstacle(rawObstacle.Inflate(isotropicMargin));
                return;
            }

            double halfW = widthFeet * 0.5;
            double halfH = heightFeet * 0.5;
            double c = clearanceAndInsulationFeet;

            // 1. Move along X: cross section has halfW along Y, halfH along Z
            IObstacle obsX = rawObstacle.Inflate(c, c + halfW, c + halfH);
            BlockObstacleToSet(obsX, _blockedCellsX!);

            // 2. Move along Y: cross section has halfW along X, halfH along Z
            IObstacle obsY = rawObstacle.Inflate(c + halfW, c, c + halfH);
            BlockObstacleToSet(obsY, _blockedCellsY!);

            // 3. Move along Z: cross section has halfW along X, halfH along Y
            IObstacle obsZ = rawObstacle.Inflate(c + halfW, c + halfH, c);
            BlockObstacleToSet(obsZ, _blockedCellsZ!);

            // Conservative isotropic fallback for general checks
            double maxHalf = Math.Max(halfW, halfH);
            IObstacle obsIso = rawObstacle.Inflate(c + maxHalf);
            BlockObstacleToSet(obsIso, _blockedCells);
        }

        private void BlockObstacleToSet(IObstacle obstacle, HashSet<GridCell> targetSet)
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
                                targetSet.Add(cell);
                                if (!string.IsNullOrEmpty(obstacle.Id))
                                {
                                    RecordCellObstacle(cell, obstacle.Id);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RecordCellObstacle(GridCell cell, string obstacleId)
        {
            if (!_cellObstacleIds.TryGetValue(cell, out var list))
            {
                list = new List<string>(1);
                _cellObstacleIds[cell] = list;
            }
            if (!list.Contains(obstacleId))
            {
                list.Add(obstacleId);
            }
        }
    }
}
