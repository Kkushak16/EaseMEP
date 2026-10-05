using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Post;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Implements deterministic 3D rectilinear A* routing with turn penalties,
    /// vertical change penalties, and minimum straight run constraints.
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
        /// <returns>A <see cref="RouteResult"/> containing status and simplified corner waypoints.</returns>
        public RouteResult Route(
            SparseGrid grid,
            Vec3 startPos,
            Vec3 endPos,
            Direction3D startDir = Direction3D.None,
            Direction3D endDir = Direction3D.None)
        {
            var stopwatch = Stopwatch.StartNew();

            GridCell startCell = grid.WorldToCell(startPos);
            GridCell goalCell = grid.WorldToCell(endPos);

            if (grid.IsBlocked(startCell))
            {
                stopwatch.Stop();
                return RouteResult.Failure(RoutingStatus.StartBlocked, searchTimeMs: stopwatch.Elapsed.TotalMilliseconds);
            }

            if (grid.IsBlocked(goalCell))
            {
                stopwatch.Stop();
                return RouteResult.Failure(RoutingStatus.EndBlocked, searchTimeMs: stopwatch.Elapsed.TotalMilliseconds);
            }

            if (startCell == goalCell)
            {
                stopwatch.Stop();
                var singlePointList = new List<Vec3> { startPos };
                return new RouteResult(RoutingStatus.Success, singlePointList, 0.0, 0, 1, stopwatch.Elapsed.TotalMilliseconds);
            }

            // Expected approach direction at goal is opposite to outward connector vector
            Direction3D expectedGoalArrivalDir = Direction3D.None;
            if (endDir != Direction3D.None)
            {
                var (edx, edy, edz) = endDir.ToOffset();
                expectedGoalArrivalDir = Direction3DExtensions.FromVector(new Vec3(-edx, -edy, -edz));
            }

            var openHeap = new MinHeap<SearchNode>(256, new SearchNodeComparer());
            var closedCost = new Dictionary<RouteState, double>();
            var nodePool = new List<NodeRecord>(512);

            long sequenceCounter = 0;

            // Initialize start state
            RouteState initialState = new RouteState(startCell, startDir, startDir != Direction3D.None ? 1 : 0);
            double initialH = ComputeHeuristic(startCell, goalCell, grid.CellSizeFeet);

            int startNodeIndex = nodePool.Count;
            nodePool.Add(new NodeRecord(initialState, -1, 0.0));

            openHeap.Push(new SearchNode(initialState, 0.0, initialH, sequenceCounter++, startNodeIndex));
            closedCost[initialState] = 0.0;

            int nodesExpanded = 0;
            int goalNodeIndex = -1;

            while (openHeap.Count > 0)
            {
                SearchNode current = openHeap.Pop();
                RouteState currState = current.State;

                // If a strictly cheaper route to this state was already processed, skip
                if (closedCost.TryGetValue(currState, out double bestG) && current.G > bestG + 1e-7)
                    continue;

                nodesExpanded++;

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

                // Explore 6 cardinal directions
                for (int i = 0; i < Direction3DExtensions.AllCardinal.Length; i++)
                {
                    Direction3D nextDir = Direction3DExtensions.AllCardinal[i];

                    // 1. Prevent 180-degree immediate reversal
                    if (currState.Direction != Direction3D.None && nextDir.IsOpposite(currState.Direction))
                        continue;

                    bool isTurn = currState.Direction != Direction3D.None && nextDir != currState.Direction;

                    // 2. Enforce start connector stub
                    if (currState.Cell == startCell && startDir != Direction3D.None && nextDir != startDir)
                        continue;

                    // 3. Enforce minimum straight length between turns
                    if (isTurn && currState.RunLength < _costModel.MinRunCells)
                        continue;

                    // Calculate next cell position
                    var offset = nextDir.ToOffset();
                    GridCell nextCell = currState.Cell + offset;

                    // 4. Obstacle & corridor collision check
                    if (grid.IsBlocked(nextCell))
                        continue;

                    // Compute transition run length
                    int nextRun = (!isTurn && currState.Direction != Direction3D.None)
                        ? Math.Min(currState.RunLength + 1, _costModel.MinRunCells)
                        : 1;

                    // Calculate movement cost
                    double moveCost = _costModel.WeightLength * grid.CellSizeFeet;
                    if (isTurn)
                    {
                        moveCost += _costModel.WeightBend;
                        if (nextDir.IsVertical() || currState.Direction.IsVertical())
                        {
                            moveCost += _costModel.WeightVertical;
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
                return RouteResult.Failure(RoutingStatus.NoPath, nodesExpanded: nodesExpanded, searchTimeMs: elapsedMs);
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

        private double ComputeHeuristic(GridCell a, GridCell b, double cellSizeFeet)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            int dz = Math.Abs(a.Z - b.Z);
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
