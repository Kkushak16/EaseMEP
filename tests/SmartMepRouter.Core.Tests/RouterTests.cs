using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class RouterTests
    {
        private readonly double _cellSize = 1.0; // 1 foot cell size for clear test coordinates
        private readonly Aabb _corridor = new Aabb(new Vec3(-20, -20, -10), new Vec3(40, 40, 20));

        [Fact]
        public void Route_OpenSpace_FindsDirectStraightRoute()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);
            var router = new Router();

            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(10, 0, 0);

            RouteResult result = router.Route(grid, start, goal);

            Assert.True(result.IsSuccess);
            Assert.Equal(RoutingStatus.Success, result.Status);
            Assert.Equal(10.0, result.LengthFeet, precision: 4);
            Assert.Equal(0, result.BendCount);
            Assert.Equal(2, result.Points.Count);
            Assert.Equal(start, result.Points[0]);
            Assert.Equal(goal, result.Points[1]);
        }

        [Fact]
        public void Route_SingleWall_FindsDetourAroundObstacle()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);
            var router = new Router();

            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(10, 0, 0);

            // Block a vertical wall directly on X=5 from Y=-2 to Y=2
            for (int y = -2; y <= 2; y++)
            {
                grid.SetBlocked(new GridCell(5, y, 0), true);
            }

            RouteResult result = router.Route(grid, start, goal);

            Assert.True(result.IsSuccess);
            Assert.True(result.BendCount >= 2);

            // Verify none of the waypoints or grid cells traverse the wall
            foreach (Vec3 point in result.Points)
            {
                GridCell cell = grid.WorldToCell(point);
                Assert.False(grid.IsBlocked(cell), $"Point {point} falls on a blocked cell {cell}.");
            }
        }

        [Fact]
        public void Route_UShapedTrap_EscapesTrapAndReachesGoal()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);
            var router = new Router();

            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(12, 0, 0);

            // U-shaped trap facing the start (-X):
            // Back wall at X=6 from Y=-3 to Y=3
            for (int y = -3; y <= 3; y++)
            {
                grid.SetBlocked(new GridCell(6, y, 0), true);
            }
            // Top arm at Y=3 from X=2 to X=6
            for (int x = 2; x <= 6; x++)
            {
                grid.SetBlocked(new GridCell(x, 3, 0), true);
            }
            // Bottom arm at Y=-3 from X=2 to X=6
            for (int x = 2; x <= 6; x++)
            {
                grid.SetBlocked(new GridCell(x, -3, 0), true);
            }

            RouteResult result = router.Route(grid, start, goal);

            Assert.True(result.IsSuccess);
            Assert.Equal(RoutingStatus.Success, result.Status);
            Assert.Equal(start, result.Points[0]);
            Assert.Equal(goal, result.Points[result.Points.Count - 1]);
        }

        [Fact]
        public void Route_UnreachableGoal_ReturnsNoPath()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);
            var router = new Router();

            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(10, 0, 0);
            GridCell goalCell = grid.WorldToCell(goal);

            // Completely encircle the goal cell with blocked cells
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;
                        grid.SetBlocked(new GridCell(goalCell.X + dx, goalCell.Y + dy, goalCell.Z + dz), true);
                    }
                }
            }

            RouteResult result = router.Route(grid, start, goal);

            Assert.False(result.IsSuccess);
            Assert.Equal(RoutingStatus.NoPath, result.Status);
            Assert.Empty(result.Points);
        }

        [Fact]
        public void Route_BlockedStartAndEnd_ReturnsAccurateStatus()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);
            var router = new Router();

            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(10, 0, 0);

            // Test start blocked
            grid.SetBlocked(grid.WorldToCell(start), true);
            RouteResult result1 = router.Route(grid, start, goal);
            Assert.Equal(RoutingStatus.StartBlocked, result1.Status);

            // Unblock start, block goal
            grid.SetBlocked(grid.WorldToCell(start), false);
            grid.SetBlocked(grid.WorldToCell(goal), true);
            RouteResult result2 = router.Route(grid, start, goal);
            Assert.Equal(RoutingStatus.EndBlocked, result2.Status);
        }

        [Fact]
        public void Route_DeterministicOutput_ProducesIdenticalWaypointsAcrossMultipleRuns()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);
            var router = new Router();

            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(8, 8, 0);

            // Place scattered obstacles creating multiple potential paths
            grid.SetBlocked(new GridCell(4, 4, 0), true);
            grid.SetBlocked(new GridCell(4, 3, 0), true);
            grid.SetBlocked(new GridCell(3, 4, 0), true);

            RouteResult firstRun = router.Route(grid, start, goal);
            Assert.True(firstRun.IsSuccess);

            for (int iteration = 0; iteration < 10; iteration++)
            {
                RouteResult subsequentRun = router.Route(grid, start, goal);
                Assert.Equal(firstRun.Points.Count, subsequentRun.Points.Count);
                Assert.Equal(firstRun.LengthFeet, subsequentRun.LengthFeet, precision: 6);
                Assert.Equal(firstRun.BendCount, subsequentRun.BendCount);

                for (int i = 0; i < firstRun.Points.Count; i++)
                {
                    Assert.Equal(firstRun.Points[i], subsequentRun.Points[i]);
                }
            }
        }

        [Fact]
        public void Route_ConnectorStubs_EnforcesDepartureAndArrivalDirections()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);
            var router = new Router();

            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(6, 6, 0);

            // Start departs along +Y (East is goal, but duct MUST leave North)
            // Goal connector faces West (-X), so duct enters heading East (+X)
            RouteResult result = router.Route(
                grid,
                start,
                goal,
                startDir: Direction3D.PositiveY,
                endDir: Direction3D.NegativeX);

            Assert.True(result.IsSuccess);
            Assert.True(result.Points.Count >= 3);

            // First leg must be in direction +Y
            Vec3 firstLeg = result.Points[1] - result.Points[0];
            Assert.True(firstLeg.Y > 0);
            Assert.Equal(0, firstLeg.X, precision: 5);

            // Last leg must arrive along +X
            Vec3 lastLeg = result.Points[result.Points.Count - 1] - result.Points[result.Points.Count - 2];
            Assert.True(lastLeg.X > 0);
            Assert.Equal(0, lastLeg.Y, precision: 5);
        }
    }
}
