using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Metrics;
using SmartMepRouter.Core.Scenes;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;
using Xunit;
using Xunit.Abstractions;

namespace SmartMepRouter.Core.Tests
{
    public class PropertyAndBenchmarkTests
    {
        private readonly ITestOutputHelper _output;

        public PropertyAndBenchmarkTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Benchmark_RunsOn50Scenes_VerifiesZeroCollisionsAndAxisAlignedLegs()
        {
            const int sceneCount = 50;
            var suite = SceneGenerator.GenerateBenchmarkSuite(count: sceneCount, startSeed: 2000);
            var router = new Router(CostModel.Default);

            int solvedCount = 0;
            int totalBends = 0;
            double totalLength = 0;
            int totalCollisions = 0;

            for (int i = 0; i < suite.Count; i++)
            {
                var scene = suite[i];
                double cellSize = 1.0; // 1 ft cell size for benchmark speed

                // Inflate obstacles by duct total margin (FR-17)
                double inflation = scene.Duct.TotalInflationFeet;
                var inflatedObstacles = new List<IObstacle>();
                foreach (var obs in scene.Obstacles)
                {
                    inflatedObstacles.Add(obs.Inflate(inflation));
                }

                // Build sparse grid and voxelize inflated obstacles
                var grid = new SparseGrid(scene.StartPos, cellSize, scene.CorridorBounds);
                foreach (var obs in inflatedObstacles)
                {
                    grid.BlockObstacle(obs);
                }

                RouteResult result = router.Route(
                    grid,
                    scene.StartPos,
                    scene.EndPos,
                    scene.StartDir,
                    scene.EndDir);

                if (result.IsSuccess)
                {
                    solvedCount++;
                    totalBends += result.BendCount;
                    totalLength += result.LengthFeet;

                    // Property 1: Exact endpoints
                    Assert.Equal(scene.StartPos, result.Points[0]);
                    Assert.Equal(scene.EndPos, result.Points[result.Points.Count - 1]);

                    // Property 2: Axis-aligned rectilinear legs
                    for (int s = 0; s < result.Points.Count - 1; s++)
                    {
                        Vec3 d = result.Points[s + 1] - result.Points[s];
                        int nonZeroAxes = 0;
                        if (Math.Abs(d.X) > Vec3.Tolerance) nonZeroAxes++;
                        if (Math.Abs(d.Y) > Vec3.Tolerance) nonZeroAxes++;
                        if (Math.Abs(d.Z) > Vec3.Tolerance) nonZeroAxes++;

                        Assert.True(nonZeroAxes <= 1, $"Segment {s} ({result.Points[s]} -> {result.Points[s + 1]}) is not axis-aligned!");
                    }

                    // Property 3: Independent exact collision verification (R-T2)
                    var verification = CollisionVerifier.Verify(result.Points, inflatedObstacles);
                    if (verification.HasCollisions)
                    {
                        totalCollisions += verification.CollidingObstacleIds.Count;
                    }
                    Assert.False(verification.HasCollisions, $"Scene {scene.Id} route collided with: {string.Join(", ", verification.CollidingObstacleIds)}");

                    // Property 4: Metrics record generated successfully
                    var metrics = new RunMetrics
                    {
                        SceneId = scene.Id,
                        CellSizeFeet = cellSize,
                        BlockedCells = grid.BlockedCount,
                        NodesExpanded = result.NodesExpanded,
                        PathLengthFeet = result.LengthFeet,
                        BendCount = result.BendCount,
                        SegmentCount = result.SegmentCount,
                        Success = true,
                        Status = result.Status,
                        CollisionCount = verification.CollidingObstacleIds.Count,
                        SearchTimeMs = result.SearchTimeMs
                    };

                    string json = MetricsWriter.ToJson(metrics);
                    Assert.False(string.IsNullOrWhiteSpace(json));
                }
            }

            _output.WriteLine($"[Benchmark Summary on {sceneCount} scenes]");
            _output.WriteLine($"  Solved:            {solvedCount}/{sceneCount} ({solvedCount * 100.0 / sceneCount:F1}%)");
            _output.WriteLine($"  Total Collisions:  {totalCollisions} (MUST BE 0)");
            _output.WriteLine($"  Average Bends:     {(solvedCount > 0 ? (double)totalBends / solvedCount : 0):F2}");
            _output.WriteLine($"  Average Length:    {(solvedCount > 0 ? totalLength / solvedCount : 0):F2} ft");

            Assert.Equal(0, totalCollisions);
            Assert.True(solvedCount >= 40, $"Expected at least 40/50 solved scenes, but got {solvedCount}.");
        }

        [Fact]
        public void Comparison_SmartMepRouter_HasFewerBendsThanPlainAStar()
        {
            var scene = SceneGenerator.GenerateScene(1234);
            double cellSize = 1.0;

            var inflatedObstacles = new List<IObstacle>();
            foreach (var obs in scene.Obstacles)
            {
                inflatedObstacles.Add(obs.Inflate(scene.Duct.TotalInflationFeet));
            }

            var grid = new SparseGrid(scene.StartPos, cellSize, scene.CorridorBounds);
            foreach (var obs in inflatedObstacles)
            {
                grid.BlockObstacle(obs);
            }

            var smartRouter = new Router(CostModel.FewerBends);
            var baselineRouter = new BaselineRouter();

            RouteResult smartResult = smartRouter.Route(grid, scene.StartPos, scene.EndPos);
            RouteResult plainResult = baselineRouter.RoutePlainAStar(grid, scene.StartPos, scene.EndPos);

            if (smartResult.IsSuccess && plainResult.IsSuccess)
            {
                _output.WriteLine($"SmartMEP bends: {smartResult.BendCount}, Plain A* bends: {plainResult.BendCount}");
                Assert.True(smartResult.BendCount <= plainResult.BendCount,
                    $"Expected SmartMEP ({smartResult.BendCount} bends) <= Plain A* ({plainResult.BendCount} bends)");
            }
        }
    }
}
