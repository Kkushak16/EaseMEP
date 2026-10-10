using System;
using System.Collections.Generic;
using System.Threading;
using SmartMepRouter.Core.Diagnostics;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class HardeningAndFailureTests
    {
        [Fact]
        public void StartBlocked_InsideObstacle_ReportsStartBlockedWithObstacleId()
        {
            var corridor = new Aabb(new Vec3(-10, -10, -10), new Vec3(30, 10, 10));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            // Obstacle covering start point (0, 0, 0)
            var obstacle = new BoxObstacle("col_A1", new Aabb(new Vec3(-1, -1, -1), new Vec3(1, 1, 1)));
            grid.BlockObstacle(obstacle);

            var router = new Router();
            RouteResult result = router.Route(grid, Vec3.Zero, new Vec3(20, 0, 0));

            Assert.False(result.IsSuccess);
            Assert.Equal(RoutingStatus.StartBlocked, result.Status);
            Assert.NotNull(result.FailureReport);
            Assert.Equal(FailureReasonCode.StartBlocked, result.FailureReport!.ReasonCode);
            Assert.Contains("col_A1", result.BlockingElementIds);
            Assert.False(string.IsNullOrWhiteSpace(result.FailureReport.SuggestedRemedy));
        }

        [Fact]
        public void StartStubBlocked_AlongDepartureAxis_ReportsStartStubBlockedWithObstacleId()
        {
            var corridor = new Aabb(new Vec3(-10, -10, -10), new Vec3(30, 10, 10));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            // Start (0, 0, 0) is free, but stub of 3 cells along +X collides at X = 2
            var obstacle = new BoxObstacle("beam_B2", new Aabb(new Vec3(1.6, -1, -1), new Vec3(2.4, 1, 1)));
            grid.BlockObstacle(obstacle);

            var cost = new CostModel { StartStubCells = 3 };
            var router = new Router(cost);
            RouteResult result = router.Route(grid, Vec3.Zero, new Vec3(20, 0, 0), startDir: Direction3D.PositiveX);

            Assert.False(result.IsSuccess);
            Assert.Equal(RoutingStatus.StartBlocked, result.Status);
            Assert.NotNull(result.FailureReport);
            Assert.Equal(FailureReasonCode.StartStubBlocked, result.FailureReport!.ReasonCode);
            Assert.Contains("beam_B2", result.BlockingElementIds);
        }

        [Fact]
        public void EndBlocked_InsideObstacle_ReportsEndBlockedWithObstacleId()
        {
            var corridor = new Aabb(new Vec3(-10, -10, -10), new Vec3(30, 10, 10));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            // Obstacle covering goal point (20, 0, 0)
            var obstacle = new BoxObstacle("wall_W3", new Aabb(new Vec3(19, -1, -1), new Vec3(21, 1, 1)));
            grid.BlockObstacle(obstacle);

            var router = new Router();
            RouteResult result = router.Route(grid, Vec3.Zero, new Vec3(20, 0, 0));

            Assert.False(result.IsSuccess);
            Assert.Equal(RoutingStatus.EndBlocked, result.Status);
            Assert.NotNull(result.FailureReport);
            Assert.Equal(FailureReasonCode.EndBlocked, result.FailureReport!.ReasonCode);
            Assert.Contains("wall_W3", result.BlockingElementIds);
        }

        [Fact]
        public void EndStubBlocked_AlongArrivalAxis_ReportsEndStubBlockedWithObstacleId()
        {
            var corridor = new Aabb(new Vec3(-10, -10, -10), new Vec3(30, 10, 10));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            // Goal at (20, 0, 0) is free, but end connector normal is +X (so route arrives along -X, from X=19, 18, 17)
            // Stub of 3 cells collides at X = 18
            var obstacle = new BoxObstacle("pipe_P4", new Aabb(new Vec3(17.6, -1, -1), new Vec3(18.4, 1, 1)));
            grid.BlockObstacle(obstacle);

            var cost = new CostModel { EndStubCells = 3 };
            var router = new Router(cost);
            RouteResult result = router.Route(grid, Vec3.Zero, new Vec3(20, 0, 0), endDir: Direction3D.NegativeX);

            Assert.False(result.IsSuccess);
            Assert.Equal(RoutingStatus.EndBlocked, result.Status);
            Assert.NotNull(result.FailureReport);
            Assert.Equal(FailureReasonCode.EndStubBlocked, result.FailureReport!.ReasonCode);
            Assert.Contains("pipe_P4", result.BlockingElementIds);
        }

        [Fact]
        public void NoPath_CompleteWallBlockingCorridor_ReportsNoPathAndClosestApproach()
        {
            var corridor = new Aabb(new Vec3(-10, -5, -5), new Vec3(30, 5, 5));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            // Wall blocking entire corridor cross-section at X = 10
            var wall = new BoxObstacle("barrier_99", new Aabb(new Vec3(9.5, -6, -6), new Vec3(10.5, 6, 6)));
            grid.BlockObstacle(wall);

            var router = new Router();
            RouteResult result = router.Route(grid, Vec3.Zero, new Vec3(20, 0, 0));

            Assert.False(result.IsSuccess);
            Assert.Equal(RoutingStatus.NoPath, result.Status);
            Assert.NotNull(result.FailureReport);
            Assert.Contains("barrier_99", result.BlockingElementIds);
            Assert.NotNull(result.FailureReport!.ClosestPointReached);
            Assert.True(result.FailureReport.RemainingDistanceFeet > 0);
            // Closest approach should be near X = 9
            Assert.InRange(result.FailureReport.ClosestPointReached!.Value.X, 7.0, 10.0);
        }

        [Fact]
        public void CorridorWidening_TightCorridorFails_WidenedCorridorSucceeds()
        {
            // Wall at X = 10 blocks Y in [-2.5, 2.5], but has clear opening at Y = 4
            var wall = new BoxObstacle("wall_partial", new Aabb(new Vec3(9.5, -2.5, -5), new Vec3(10.5, 2.5, 5)));
            var obstacles = new List<IObstacle> { wall };

            // Initial corridor is tight: Y in [-2.0, 2.0] -> impassable
            var tightCorridor = new Aabb(new Vec3(-5, -2.0, -2), new Vec3(25, 2.0, 2));

            // Without widening: fails
            var innerRouter = new Router();
            var noWideningRouter = new CorridorWideningRouter(
                innerRouter,
                new CorridorWideningOptions { EnableWideningOnNoPath = false });

            RouteResult tightResult = noWideningRouter.RouteWithWidening(
                Vec3.Zero,
                new Vec3(20, 0, 0),
                obstacles,
                tightCorridor,
                1.0);

            Assert.False(tightResult.IsSuccess);
            Assert.Equal(RoutingStatus.NoPath, tightResult.Status);

            // With widening (expansion margin 3.0 ft per retry):
            // Retry 1 expands Y to [-5.0, 5.0], allowing route to navigate around wall at Y = 3 or 4
            var wideningRouter = new CorridorWideningRouter(
                innerRouter,
                new CorridorWideningOptions { MaxRetries = 2, ExpansionMarginFeet = 3.0, EnableWideningOnNoPath = true });

            RouteResult widenedResult = wideningRouter.RouteWithWidening(
                Vec3.Zero,
                new Vec3(20, 0, 0),
                obstacles,
                tightCorridor,
                1.0);

            Assert.True(widenedResult.IsSuccess);
            Assert.True(widenedResult.Points.Count >= 2);
            // Path must detour into Y > 2.5 to bypass the wall
            bool detoured = false;
            foreach (var pt in widenedResult.Points)
            {
                if (Math.Abs(pt.Y) >= 2.5) detoured = true;
            }
            Assert.True(detoured);
        }

        [Fact]
        public void CorridorWidening_ImpassableScene_ExhaustsRetriesGracefully()
        {
            // Fully impassable wall spanning infinite dimensions in cross-section
            var wall = new BoxObstacle("infinite_wall", new Aabb(new Vec3(9.5, -100, -100), new Vec3(10.5, 100, 100)));
            var obstacles = new List<IObstacle> { wall };
            var corridor = new Aabb(new Vec3(-5, -5, -5), new Vec3(25, 5, 5));

            var wideningRouter = new CorridorWideningRouter(
                new Router(),
                new CorridorWideningOptions { MaxRetries = 2, ExpansionMarginFeet = 5.0, EnableWideningOnNoPath = true });

            RouteResult result = wideningRouter.RouteWithWidening(
                Vec3.Zero,
                new Vec3(20, 0, 0),
                obstacles,
                corridor,
                1.0);

            Assert.False(result.IsSuccess);
            Assert.Equal(RoutingStatus.NoPath, result.Status);
            Assert.NotNull(result.FailureReport);
            Assert.Equal(2, result.FailureReport!.RetryAttempts);
        }

        [Fact]
        public void SearchBudget_ExceededBudget_StopsEarlyWithoutRunaway()
        {
            var corridor = new Aabb(new Vec3(-100, -100, -100), new Vec3(500, 500, 500));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            // Cost model with tiny budget of 25 nodes
            var cost = new CostModel { MaxNodesToExpand = 25 };
            var router = new Router(cost);

            RouteResult result = router.Route(grid, Vec3.Zero, new Vec3(300, 300, 300));

            Assert.False(result.IsSuccess);
            Assert.Equal(RoutingStatus.NoPath, result.Status);
            Assert.NotNull(result.FailureReport);
            Assert.Equal(FailureReasonCode.SearchBudgetExceeded, result.FailureReport!.ReasonCode);
            Assert.True(result.NodesExpanded >= 25);
        }

        [Fact]
        public void Cancellation_MidSearch_AbortsCleanlyWithinMilliseconds()
        {
            var corridor = new Aabb(new Vec3(-50, -50, -50), new Vec3(150, 150, 50));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancelled token

            grid.BlockAabb(new Aabb(new Vec3(50, -50, -50), new Vec3(51, 150, 50)));
            RouteResult result = new Router().Route(grid, Vec3.Zero, new Vec3(100, 0, 0), Direction3D.None, Direction3D.None, cts.Token);

            Assert.Equal(RoutingStatus.Cancelled, result.Status);
            Assert.NotNull(result.FailureReport);
            Assert.Equal(FailureReasonCode.Cancelled, result.FailureReport!.ReasonCode);
        }

        [Fact]
        public void FormattedSummary_ProducesStructuredDiagnosticReport()
        {
            var report = new FailureReport(
                FailureReasonCode.StartBlocked,
                "Start connector inside obstacle",
                new[] { "col_1029" },
                "Move connector 150 mm north",
                closestPointReached: new Vec3(5, 0, 0),
                remainingDistanceFeet: 15.0,
                nodesExpanded: 42,
                elapsedMs: 12.5,
                retryAttempts: 1);

            string summary = report.FormattedSummary();

            Assert.Contains("[Failure Code: StartBlocked]", summary);
            Assert.Contains("col_1029", summary);
            Assert.Contains("Move connector 150 mm north", summary);
            Assert.Contains("15.0 ft remaining", summary);
            Assert.Contains("Corridor widening retries attempted: 1", summary);
        }

        [Fact]
        public void MultiObstacleChokePoint_ReportsAllBlockingIdsAtBottleneck()
        {
            var corridor = new Aabb(new Vec3(-5, -5, -5), new Vec3(25, 5, 5));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            // Choke point at X = 10 formed by two obstacles: top half and bottom half
            var obsTop = new BoxObstacle("column_top_101", new Aabb(new Vec3(9.5, -5, 0), new Vec3(10.5, 5, 5)));
            var obsBottom = new BoxObstacle("column_bottom_102", new Aabb(new Vec3(9.5, -5, -5), new Vec3(10.5, 5, 0)));
            grid.BlockObstacle(obsTop);
            grid.BlockObstacle(obsBottom);

            var router = new Router();
            RouteResult result = router.Route(grid, Vec3.Zero, new Vec3(20, 0, 0));

            Assert.False(result.IsSuccess);
            Assert.Equal(RoutingStatus.NoPath, result.Status);
            Assert.NotNull(result.FailureReport);
            // Both obstacles should be identified in the blocking element list
            Assert.True(result.BlockingElementIds.Contains("column_top_101") || result.BlockingElementIds.Contains("column_bottom_102"));
        }

        [Fact]
        public void LargeCorridor_PerformancePass_SolvesRapidly()
        {
            // 50m (~164 ft) long corridor with multiple staggered columns
            var corridor = new Aabb(new Vec3(-10, -15, -10), new Vec3(180, 15, 10));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            // Place 10 staggered column obstacles along the route
            for (int x = 15; x < 150; x += 15)
            {
                double yOffset = (x % 30 == 0) ? -2.0 : 2.0;
                var col = new BoxObstacle($"col_x{x}", new Aabb(new Vec3(x - 1, yOffset - 2, -5), new Vec3(x + 1, yOffset + 2, 5)));
                grid.BlockObstacle(col);
            }

            var router = new Router(CostModel.Default);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            RouteResult result = router.Route(grid, Vec3.Zero, new Vec3(160, 0, 0));
            sw.Stop();

            Assert.True(result.IsSuccess);
            Assert.True(result.Points.Count >= 2);
            // Must finish in well under 500 ms (typically < 30 ms)
            Assert.True(sw.ElapsedMilliseconds < 500, $"Large corridor search took {sw.ElapsedMilliseconds} ms, exceeding 500 ms threshold.");
        }
    }
}
