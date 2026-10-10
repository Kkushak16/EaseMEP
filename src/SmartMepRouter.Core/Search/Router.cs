using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SmartMepRouter.Core.Diagnostics;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Post;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Implements deterministic 3D rectilinear A* routing with turn penalties,
    /// vertical change penalties, minimum straight run constraints, and comprehensive
    /// failure diagnostic reporting (FR-13, FR-16, FR-18).
    /// Operates entirely in Revit internal decimal feet with zero Revit API references.
    /// </summary>
    public class Router
    {
        private readonly CostModel _costModel;

        public Router(CostModel? costModel = null)
        {
            _costModel = costModel ?? CostModel.Default;
        }

        /// <summary>
        /// Solves for a collision-free rectilinear route between start and end.
        /// </summary>
        /// <param name="grid">The sparse obstacle grid containing blocked cells.</param>
        /// <param name="startPos">Start position in decimal feet.</param>
        /// <param name="endPos">End position in decimal feet.</param>
        /// <param name="startDir">Required departure direction at start connector, or None.</param>
        /// <param name="endDir">Required outward normal at end connector (route arrives along -endDir), or None.</param>
        /// <param name="cancellationToken">Optional token; when signalled the search returns <see cref="RoutingStatus.Cancelled"/>.</param>
        /// <returns>A <see cref="RouteResult"/> containing status, simplified corner waypoints, or diagnostic failure report.</returns>
        public RouteResult Route(
            SparseGrid grid,
            Vec3 startPos,
            Vec3 endPos,
            Direction3D startDir = Direction3D.None,
            Direction3D endDir = Direction3D.None,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            GridCell startCell = grid.WorldToCell(startPos);
            GridCell goalCell = grid.WorldToCell(endPos);

            // Expected approach direction at goal is opposite to outward connector vector
            Direction3D expectedGoalArrivalDir = Direction3D.None;
            if (endDir != Direction3D.None)
            {
                var (edx, edy, edz) = endDir.ToOffset();
                expectedGoalArrivalDir = Direction3DExtensions.FromVector(new Vec3(-edx, -edy, -edz));
            }

            // 1. Check if start cell itself is blocked (FR-13)
            if (grid.IsBlocked(startCell, startDir))
            {
                stopwatch.Stop();
                var blockingIds = grid.GetBlockingObstacleIds(startCell);
                var report = new FailureReport(
                    FailureReasonCode.StartBlocked,
                    "The starting connector or point is inside an obstacle or clearance zone.",
                    blockingIds,
                    nodesExpanded: 0,
                    elapsedMs: stopwatch.Elapsed.TotalMilliseconds);
                return RouteResult.FromFailureReport(RoutingStatus.StartBlocked, report, nodesExpanded: 0, searchTimeMs: stopwatch.Elapsed.TotalMilliseconds);
            }

            // 2. Check if departure stub along start connector direction is blocked (FR-18)
            if (startDir != Direction3D.None && _costModel.StartStubCells > 0)
            {
                var (sdx, sdy, sdz) = startDir.ToOffset();
                for (int k = 1; k <= _costModel.StartStubCells; k++)
                {
                    GridCell stubCell = new GridCell(startCell.X + sdx * k, startCell.Y + sdy * k, startCell.Z + sdz * k);
                    if (grid.IsBlocked(stubCell, startDir))
                    {
                        stopwatch.Stop();
                        var blockingIds = grid.GetBlockingObstacleIds(stubCell);
                        var report = new FailureReport(
                            FailureReasonCode.StartStubBlocked,
                            $"Departure stub leg along start connector direction collides with an obstacle {k} cell(s) from connector.",
                            blockingIds,
                            nodesExpanded: 0,
                            elapsedMs: stopwatch.Elapsed.TotalMilliseconds);
                        return RouteResult.FromFailureReport(RoutingStatus.StartBlocked, report, nodesExpanded: 0, searchTimeMs: stopwatch.Elapsed.TotalMilliseconds);
                    }
                }
            }

            // 3. Check if goal cell itself is blocked (FR-13)
            if (grid.IsBlocked(goalCell, expectedGoalArrivalDir))
            {
                stopwatch.Stop();
                var blockingIds = grid.GetBlockingObstacleIds(goalCell);
                var report = new FailureReport(
                    FailureReasonCode.EndBlocked,
                    "The destination connector or point is inside an obstacle or clearance zone.",
                    blockingIds,
                    nodesExpanded: 0,
                    elapsedMs: stopwatch.Elapsed.TotalMilliseconds);
                return RouteResult.FromFailureReport(RoutingStatus.EndBlocked, report, nodesExpanded: 0, searchTimeMs: stopwatch.Elapsed.TotalMilliseconds);
            }

            // 4. Check if arrival stub along destination connector direction is blocked (FR-18)
            if (expectedGoalArrivalDir != Direction3D.None && _costModel.EndStubCells > 0)
            {
                var (adx, ady, adz) = expectedGoalArrivalDir.ToOffset();
                for (int k = 1; k <= _costModel.EndStubCells; k++)
                {
                    GridCell stubCell = new GridCell(goalCell.X - adx * k, goalCell.Y - ady * k, goalCell.Z - adz * k);
                    if (grid.IsBlocked(stubCell, expectedGoalArrivalDir))
                    {
                        stopwatch.Stop();
                        var blockingIds = grid.GetBlockingObstacleIds(stubCell);
                        var report = new FailureReport(
                            FailureReasonCode.EndStubBlocked,
                            $"Arrival stub leg along destination connector direction collides with an obstacle {k} cell(s) before connector.",
                            blockingIds,
                            nodesExpanded: 0,
                            elapsedMs: stopwatch.Elapsed.TotalMilliseconds);
                        return RouteResult.FromFailureReport(RoutingStatus.EndBlocked, report, nodesExpanded: 0, searchTimeMs: stopwatch.Elapsed.TotalMilliseconds);
                    }
                }
            }

            // 5. Degenerate case: start equals goal
            if (startCell == goalCell)
            {
                stopwatch.Stop();
                var singlePointList = new List<Vec3> { startPos };
                return new RouteResult(RoutingStatus.Success, singlePointList, 0.0, 0, 1, stopwatch.Elapsed.TotalMilliseconds);
            }

            var openHeap = new MinHeap<SearchNode>(256, new SearchNodeComparer());
            var closedCost = new Dictionary<RouteState, double>();
            var nodePool = new List<NodeRecord>(512);

            long sequenceCounter = 0;

            // Initialize start state
            RouteState initialState = new RouteState(startCell, startDir, 0);
            double initialH = ComputeHeuristic(startCell, goalCell, grid.CellSizeFeet);

            int startNodeIndex = nodePool.Count;
            nodePool.Add(new NodeRecord(initialState, -1, 0.0));

            openHeap.Push(new SearchNode(initialState, 0.0, initialH, sequenceCounter++, startNodeIndex));
            closedCost[initialState] = 0.0;

            int nodesExpanded = 0;
            int goalNodeIndex = -1;

            int minManhattanDist = Math.Abs(startCell.X - goalCell.X) + Math.Abs(startCell.Y - goalCell.Y) + Math.Abs(startCell.Z - goalCell.Z);
            GridCell closestCell = startCell;
            int maxBudget = _costModel.MaxNodesToExpand > 0 ? _costModel.MaxNodesToExpand : 150000;

            while (openHeap.Count > 0)
            {
                SearchNode current = openHeap.Pop();
                RouteState currState = current.State;

                // Track closest approach to goal for failure diagnostics
                int dist = Math.Abs(currState.Cell.X - goalCell.X) + Math.Abs(currState.Cell.Y - goalCell.Y) + Math.Abs(currState.Cell.Z - goalCell.Z);
                if (dist < minManhattanDist)
                {
                    minManhattanDist = dist;
                    closestCell = currState.Cell;
                }

                // If a strictly cheaper route to this state was already processed, skip
                if (closedCost.TryGetValue(currState, out double bestG) && current.G > bestG + 1e-7)
                    continue;

                nodesExpanded++;

                // Responsive cancellation check
                if ((nodesExpanded & 255) == 0 && cancellationToken.IsCancellationRequested)
                {
                    stopwatch.Stop();
                    var cancelReport = new FailureReport(
                        FailureReasonCode.Cancelled,
                        "The routing operation was cancelled by the user.",
                        nodesExpanded: nodesExpanded,
                        elapsedMs: stopwatch.Elapsed.TotalMilliseconds);
                    return RouteResult.FromFailureReport(RoutingStatus.Cancelled, cancelReport, nodesExpanded: nodesExpanded, searchTimeMs: stopwatch.Elapsed.TotalMilliseconds);
                }

                // Search budget protection
                if (nodesExpanded >= maxBudget)
                {
                    stopwatch.Stop();
                    Vec3 closestPt = grid.CellToWorld(closestCell);
                    double remainingDist = closestPt.DistanceTo(endPos);
                    var blocking = GetBlockingObstaclesNear(grid, closestCell, goalCell);
                    var budgetReport = new FailureReport(
                        FailureReasonCode.SearchBudgetExceeded,
                        $"Search exceeded node expansion budget ({maxBudget} nodes).",
                        blocking,
                        closestPointReached: closestPt,
                        remainingDistanceFeet: remainingDist,
                        nodesExpanded: nodesExpanded,
                        elapsedMs: stopwatch.Elapsed.TotalMilliseconds,
                        hitCorridorBoundary: grid.IsCellOnCorridorBoundary(closestCell));
                    return RouteResult.FromFailureReport(RoutingStatus.NoPath, budgetReport, nodesExpanded: nodesExpanded, searchTimeMs: stopwatch.Elapsed.TotalMilliseconds);
                }

                // Goal check
                if (currState.Cell == goalCell)
                {
                    bool endDirMatches = (expectedGoalArrivalDir == Direction3D.None) ||
                                         (currState.Direction == expectedGoalArrivalDir);

                    bool endStubMatches = (expectedGoalArrivalDir == Direction3D.None) ||
                                          (currState.RunLength >= _costModel.EndStubCells);

                    if (endDirMatches && endStubMatches)
                    {
                        goalNodeIndex = current.NodeIndex;
                        break;
                    }
                }

                // Explore candidate directions (6 cardinal or 18 with 45-degree planar diagonals)
                Direction3D[] candidateDirections = _costModel.Allow45DegreeBends
                    ? Direction3DExtensions.AllDirections
                    : Direction3DExtensions.AllCardinal;

                for (int i = 0; i < candidateDirections.Length; i++)
                {
                    Direction3D nextDir = candidateDirections[i];

                    // 1. Prevent 180-degree immediate reversal and enforce standard HVAC transition angles
                    if (currState.Direction != Direction3D.None && nextDir.IsOpposite(currState.Direction))
                        continue;

                    if (!Direction3DExtensions.IsValidHVACTransition(currState.Direction, nextDir, out double angleDeg))
                        continue;

                    bool isTurn = currState.Direction != Direction3D.None && nextDir != currState.Direction;

                    // 2. Enforce start connector stub (FR-18)
                    if (currState.Cell == startCell && startDir != Direction3D.None && nextDir != startDir)
                        continue;

                    if (isTurn && startDir != Direction3D.None && currState.Direction == startDir && currState.RunLength < _costModel.StartStubCells)
                        continue;

                    // 3. Enforce minimum straight length between turns (FR-16)
                    if (isTurn && currState.RunLength < _costModel.MinRunCells)
                        continue;

                    // Calculate next cell position
                    var offset = nextDir.ToOffset();
                    GridCell nextCell = currState.Cell + offset;

                    // 4. Obstacle & corridor collision check (supports directional inflation)
                    if (grid.IsBlocked(nextCell, nextDir))
                        continue;

                    // 5. Flanking swept collision check for diagonal moves (prevents corner-cutting between obstacles)
                    if (nextDir.IsDiagonal())
                    {
                        var (f1Offset, f2Offset) = nextDir.GetFlankingOffsets();
                        GridCell f1 = currState.Cell + f1Offset;
                        GridCell f2 = currState.Cell + f2Offset;
                        if (grid.IsBlocked(f1, nextDir) || grid.IsBlocked(f2, nextDir))
                            continue;
                    }

                    // Compute transition run length, capped at maximum required constraint
                    int maxRunToTrack = Math.Max(_costModel.MinRunCells, Math.Max(_costModel.StartStubCells, _costModel.EndStubCells));
                    int nextRun = (!isTurn && currState.Direction != Direction3D.None)
                        ? Math.Min(currState.RunLength + 1, maxRunToTrack)
                        : 1;

                    // Calculate movement cost
                    double stepRatio = nextDir.StepDistanceRatio();
                    double moveCost = _costModel.WeightLength * (stepRatio * grid.CellSizeFeet);
                    if (isTurn)
                    {
                        if (Math.Abs(angleDeg - 45.0) < 1.0)
                        {
                            moveCost += _costModel.WeightBend45;
                            if (nextDir.IsVertical() || currState.Direction.IsVertical())
                            {
                                moveCost += _costModel.WeightVertical * 0.5;
                            }
                        }
                        else
                        {
                            moveCost += _costModel.WeightBend;
                            if (nextDir.IsVertical() || currState.Direction.IsVertical())
                            {
                                moveCost += _costModel.WeightVertical;
                            }
                        }
                    }

                    double newG = current.G + moveCost;
                    RouteState nextState = new RouteState(nextCell, nextDir, nextRun);

                    if (!closedCost.TryGetValue(nextState, out double existingG) || newG < existingG - 1e-7)
                    {
                        closedCost[nextState] = newG;
                        double nextH = ComputeHeuristic(nextCell, goalCell, grid.CellSizeFeet);

                        int nextIndex = nodePool.Count;
                        nodePool.Add(new NodeRecord(nextState, current.NodeIndex, newG));

                        openHeap.Push(new SearchNode(nextState, newG, newG + nextH, sequenceCounter++, nextIndex));
                    }
                }
            }

            stopwatch.Stop();
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            if (goalNodeIndex == -1)
            {
                Vec3 closestWorld = grid.CellToWorld(closestCell);
                double remainingDist = closestWorld.DistanceTo(endPos);
                bool hitBoundary = grid.IsCellOnCorridorBoundary(closestCell);
                var blockingObstacles = GetBlockingObstaclesNear(grid, closestCell, goalCell);

                var reasonCode = hitBoundary ? FailureReasonCode.CorridorLimitReached : FailureReasonCode.NoPath;
                var report = new FailureReport(
                    reasonCode,
                    hitBoundary
                        ? "Search reached corridor boundaries without finding a route. Expanding corridor margin may resolve the path."
                        : "No collision-free route could be found within the search corridor.",
                    blockingObstacles,
                    closestPointReached: closestWorld,
                    remainingDistanceFeet: remainingDist,
                    nodesExpanded: nodesExpanded,
                    elapsedMs: elapsedMs,
                    hitCorridorBoundary: hitBoundary);

                return RouteResult.FromFailureReport(RoutingStatus.NoPath, report, nodesExpanded: nodesExpanded, searchTimeMs: elapsedMs);
            }

            // Reconstruct path
            var rawPoints = new List<Vec3>();
            int trace = goalNodeIndex;
            while (trace != -1)
            {
                NodeRecord record = nodePool[trace];
                rawPoints.Add(grid.CellToWorld(record.State.Cell));
                trace = record.ParentIndex;
            }

            rawPoints.Reverse();

            // Ensure exact connector start and end positions
            if (rawPoints.Count > 0)
            {
                rawPoints[0] = startPos;
                rawPoints[rawPoints.Count - 1] = endPos;
            }

            // Simplify collinear points into clean corners
            List<Vec3> corners = PathSimplifier.CompressCollinear(rawPoints);
            double totalLength = PathSimplifier.ComputeTotalLength(corners);
            int bends = PathSimplifier.CountBends(corners);

            return new RouteResult(
                RoutingStatus.Success,
                corners,
                totalLength,
                bends,
                nodesExpanded,
                elapsedMs);
        }

        private static IReadOnlyList<string> GetBlockingObstaclesNear(SparseGrid grid, GridCell fromCell, GridCell toCell)
        {
            var ids = new HashSet<string>();
            int dx = Math.Sign(toCell.X - fromCell.X);
            int dy = Math.Sign(toCell.Y - fromCell.Y);
            int dz = Math.Sign(toCell.Z - fromCell.Z);

            (int ox, int oy, int oz)[] directions =
            {
                (dx, 0, 0),
                (0, dy, 0),
                (0, 0, dz),
                (dx, dy, 0),
                (dx, 0, dz),
                (0, dy, dz)
            };

            foreach (var (ox, oy, oz) in directions)
            {
                if (ox == 0 && oy == 0 && oz == 0) continue;
                GridCell neighbor = new GridCell(fromCell.X + ox, fromCell.Y + oy, fromCell.Z + oz);
                var cellIds = grid.GetBlockingObstacleIds(neighbor);
                foreach (var id in cellIds)
                {
                    if (id != "CORRIDOR_BOUNDARY")
                        ids.Add(id);
                }
            }

            // Check ray cells towards goal up to 8 steps
            int curX = fromCell.X;
            int curY = fromCell.Y;
            int curZ = fromCell.Z;
            for (int step = 0; step < 8; step++)
            {
                if (curX != toCell.X) curX += dx;
                else if (curY != toCell.Y) curY += dy;
                else if (curZ != toCell.Z) curZ += dz;
                else break;

                GridCell rayCell = new GridCell(curX, curY, curZ);
                var cellIds = grid.GetBlockingObstacleIds(rayCell);
                foreach (var id in cellIds)
                {
                    if (id != "CORRIDOR_BOUNDARY")
                        ids.Add(id);
                }
                if (ids.Count >= 10) break;
            }

            return ids.ToList();
        }

        private double ComputeHeuristic(GridCell a, GridCell b, double cellSizeFeet)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            int dz = Math.Abs(a.Z - b.Z);

            if (_costModel.Allow45DegreeBends)
            {
                // Euclidean metric in cell space is guaranteed admissible when planar diagonal moves are enabled
                double euclidCells = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                return _costModel.WeightLength * euclidCells * cellSizeFeet;
            }

            return _costModel.WeightLength * (dx + dy + dz) * cellSizeFeet;
        }

        private readonly struct SearchNode
        {
            public RouteState State { get; }
            public double G { get; }
            public double F { get; }
            public long Sequence { get; }
            public int NodeIndex { get; }

            public SearchNode(RouteState state, double g, double f, long sequence, int nodeIndex)
            {
                State = state;
                G = g;
                F = f;
                Sequence = sequence;
                NodeIndex = nodeIndex;
            }
        }

        private sealed class SearchNodeComparer : IComparer<SearchNode>
        {
            public int Compare(SearchNode x, SearchNode y)
            {
                int fCompare = x.F.CompareTo(y.F);
                if (fCompare != 0)
                    return fCompare;

                // Tie-breaker 1: Prefer higher G (closer to goal)
                int gCompare = y.G.CompareTo(x.G);
                if (gCompare != 0)
                    return gCompare;

                // Tie-breaker 2: Deterministic FIFO sequence order
                return x.Sequence.CompareTo(y.Sequence);
            }
        }

        private readonly struct NodeRecord
        {
            public RouteState State { get; }
            public int ParentIndex { get; }
            public double G { get; }

            public NodeRecord(RouteState state, int parentIndex, double g)
            {
                State = state;
                ParentIndex = parentIndex;
                G = g;
            }
        }
    }
}
