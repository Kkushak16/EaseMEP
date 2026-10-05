using System;
using System.Collections.Generic;
using System.Diagnostics;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Post;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Implements baseline routing algorithms for comparative benchmarking:
    /// 1. Plain A* without bend/turn penalties (academic standard baseline).
    /// 2. Direct rectilinear Manhattan jog (simplest heuristic baseline).
    /// </summary>
    public class BaselineRouter
    {
        /// <summary>
        /// Plain A* search with zero turn penalty and no minimum run constraint.
        /// Finds the shortest grid path, typically resulting in staircase zigzags.
        /// </summary>
        public RouteResult RoutePlainAStar(SparseGrid grid, Vec3 startPos, Vec3 endPos)
        {
            var stopwatch = Stopwatch.StartNew();

            GridCell startCell = grid.WorldToCell(startPos);
            GridCell goalCell = grid.WorldToCell(endPos);

            if (grid.IsBlocked(startCell))
                return RouteResult.Failure(RoutingStatus.StartBlocked);
            if (grid.IsBlocked(goalCell))
                return RouteResult.Failure(RoutingStatus.EndBlocked);

            if (startCell == goalCell)
            {
                return new RouteResult(RoutingStatus.Success, new List<Vec3> { startPos }, 0.0, 0, 1, stopwatch.Elapsed.TotalMilliseconds);
            }

            var openHeap = new MinHeap<PlainNode>(256, new PlainNodeComparer());
            var closedG = new Dictionary<GridCell, double>();
            var parentMap = new Dictionary<GridCell, GridCell>();

            long seq = 0;
            openHeap.Push(new PlainNode(startCell, 0.0, Manhattan(startCell, goalCell) * grid.CellSizeFeet, seq++));
            closedG[startCell] = 0.0;

            int nodesExpanded = 0;
            bool found = false;

            while (openHeap.Count > 0)
            {
                PlainNode curr = openHeap.Pop();

                if (closedG.TryGetValue(curr.Cell, out double best) && curr.G > best + 1e-7)
                    continue;

                nodesExpanded++;

                if (curr.Cell == goalCell)
                {
                    found = true;
                    break;
                }

                for (int i = 0; i < Direction3DExtensions.AllCardinal.Length; i++)
                {
                    Direction3D dir = Direction3DExtensions.AllCardinal[i];
                    var (dx, dy, dz) = dir.ToOffset();
                    GridCell next = curr.Cell + (dx, dy, dz);

                    if (grid.IsBlocked(next))
                        continue;

                    double newG = curr.G + grid.CellSizeFeet;
                    if (!closedG.TryGetValue(next, out double exG) || newG < exG - 1e-7)
                    {
                        closedG[next] = newG;
                        parentMap[next] = curr.Cell;
                        double f = newG + Manhattan(next, goalCell) * grid.CellSizeFeet;
                        openHeap.Push(new PlainNode(next, newG, f, seq++));
                    }
                }
            }

            stopwatch.Stop();
            double ms = stopwatch.Elapsed.TotalMilliseconds;

            if (!found)
                return RouteResult.Failure(RoutingStatus.NoPath, nodesExpanded: nodesExpanded, searchTimeMs: ms);

            var rawPoints = new List<Vec3>();
            GridCell trace = goalCell;
            rawPoints.Add(grid.CellToWorld(trace));

            while (trace != startCell)
            {
                trace = parentMap[trace];
                rawPoints.Add(grid.CellToWorld(trace));
            }

            rawPoints.Reverse();
            rawPoints[0] = startPos;
            rawPoints[rawPoints.Count - 1] = endPos;

            List<Vec3> corners = PathSimplifier.CompressCollinear(rawPoints);
            double length = PathSimplifier.ComputeTotalLength(corners);
            int bends = PathSimplifier.CountBends(corners);

            return new RouteResult(RoutingStatus.Success, corners, length, bends, nodesExpanded, ms);
        }

        private static int Manhattan(GridCell a, GridCell b) =>
            Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);

        private readonly struct PlainNode
        {
            public GridCell Cell { get; }
            public double G { get; }
            public double F { get; }
            public long Seq { get; }

            public PlainNode(GridCell cell, double g, double f, long seq)
            {
                Cell = cell;
                G = g;
                F = f;
                Seq = seq;
            }
        }

        private sealed class PlainNodeComparer : IComparer<PlainNode>
        {
            public int Compare(PlainNode x, PlainNode y)
            {
                int fC = x.F.CompareTo(y.F);
                if (fC != 0) return fC;
                int gC = y.G.CompareTo(x.G);
                if (gC != 0) return gC;
                return x.Seq.CompareTo(y.Seq);
            }
        }
    }
}
