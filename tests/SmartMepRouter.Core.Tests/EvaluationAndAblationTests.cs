using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Post;
using SmartMepRouter.Core.Scenes;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class EvaluationAndAblationTests
    {
        [Fact]
        public void RealWorld_StructuralCorridor_SolvesCollisionFreeUnderBeams()
        {
            var scene = RealWorldScenarios.CreateStructuralCorridorScenario();
            var grid = new SparseGrid(scene.StartPos, 1.0, scene.CorridorBounds, enableDirectional: true);
            double clearAndInsul = scene.Duct.ClearanceAndInsulationFeet;

            foreach (var obs in scene.Obstacles)
            {
                grid.BlockObstacleDirectional(obs, clearAndInsul, scene.Duct.WidthFeet, scene.Duct.HeightFeet);
            }

            var cost = CostModel.ShortestLength;
            cost.ApplyCellSize(1.0);
            var router = new Router(cost);

            RouteResult result = router.Route(grid, scene.StartPos, scene.EndPos, scene.StartDir, scene.EndDir);

            Assert.True(result.IsSuccess);
            Assert.True(result.Points.Count >= 2);
            var verification = CollisionVerifier.Verify(result.Points, scene.Obstacles);
            Assert.False(verification.HasCollisions);

            var feasibility = FittingFeasibility.Verify(result.Points, Units.MmToFeet(150.0));
            Assert.True(feasibility.IsFeasible);
        }

        [Fact]
        public void RealWorld_CongestedPlenum_ThreadsSlotAroundObbCableTray()
        {
            var scene = RealWorldScenarios.CreateCongestedPlenumScenario();
            var grid = new SparseGrid(scene.StartPos, 1.0, scene.CorridorBounds, enableDirectional: true);
            double clearAndInsul = scene.Duct.ClearanceAndInsulationFeet;

            foreach (var obs in scene.Obstacles)
            {
                grid.BlockObstacleDirectional(obs, clearAndInsul, scene.Duct.WidthFeet, scene.Duct.HeightFeet);
            }

            var cost = CostModel.ShortestLength;
            cost.ApplyCellSize(1.0);
            var router = new Router(cost);

            RouteResult result = router.Route(grid, scene.StartPos, scene.EndPos, scene.StartDir, scene.EndDir);

            Assert.True(result.IsSuccess);
            var verification = CollisionVerifier.Verify(result.Points, scene.Obstacles);
            Assert.False(verification.HasCollisions);
        }

        [Fact]
        public void RealWorld_MechanicalRoom_ConnectsAhuToRiserCollisionFree()
        {
            var scene = RealWorldScenarios.CreateMechanicalRoomScenario();
            var grid = new SparseGrid(scene.StartPos, 1.0, scene.CorridorBounds, enableDirectional: true);
            double clearAndInsul = scene.Duct.ClearanceAndInsulationFeet;

            foreach (var obs in scene.Obstacles)
            {
                grid.BlockObstacleDirectional(obs, clearAndInsul, scene.Duct.WidthFeet, scene.Duct.HeightFeet);
            }

            var cost = CostModel.ShortestLength;
            cost.ApplyCellSize(1.0);
            var router = new Router(cost);

            RouteResult result = router.Route(grid, scene.StartPos, scene.EndPos, scene.StartDir, scene.EndDir);

            Assert.True(result.IsSuccess);
            var verification = CollisionVerifier.Verify(result.Points, scene.Obstacles);
            Assert.False(verification.HasCollisions);
            Assert.True(result.BendCount <= 4);
        }

        [Fact]
        public void Ablation_TurnPenalty_ReducesBendCount()
        {
            var corridor = new Aabb(new Vec3(-10, -10, -10), new Vec3(50, 50, 50));
            var grid = new SparseGrid(Vec3.Zero, 1.0, corridor);

            // Staggered column obstacles forcing bends
            grid.BlockAabb(new Aabb(new Vec3(9, -5, -5), new Vec3(11, 20, 50)));
            grid.BlockAabb(new Aabb(new Vec3(19, 10, -5), new Vec3(21, 45, 50)));

            var plainModel = new CostModel { WeightLength = 1.0, WeightBend = 0.0, MinRunCells = 1 };
            var penaltyModel = CostModel.FewerBends;

            var plainRouter = new Router(plainModel);
            var penaltyRouter = new Router(penaltyModel);

            RouteResult plainRes = plainRouter.Route(grid, Vec3.Zero, new Vec3(30, 30, 0));
            RouteResult penaltyRes = penaltyRouter.Route(grid, Vec3.Zero, new Vec3(30, 30, 0));

            Assert.True(plainRes.IsSuccess);
            Assert.True(penaltyRes.IsSuccess);
            Assert.True(penaltyRes.BendCount <= plainRes.BendCount);
        }

        [Fact]
        public void WastedSpaceAnalyzer_MeasuresVolumeOverApproximation()
        {
            double rad = 45.0 * Math.PI / 180.0;
            // 45-degree rotated box of dimensions 2 x 10 x 2 ft
            var obb = new ObbObstacle(
                "rot_45",
                center: new Vec3(0, 0, 0),
                halfExtents: new Vec3(1.0, 5.0, 1.0),
                axisX: new Vec3(Math.Cos(rad), Math.Sin(rad), 0),
                axisY: new Vec3(-Math.Sin(rad), Math.Cos(rad), 0),
                axisZ: new Vec3(0, 0, 1));

            var obstacles = new List<IObstacle> { obb };
            var metrics = WastedSpaceAnalyzer.Analyze(obstacles);

            Assert.Equal(1, metrics.ObstacleCount);
            Assert.Equal(1, metrics.RotatedObstacleCount);
            // Exact volume = 8 * 1 * 5 * 1 = 40 cu ft
            Assert.Equal(40.0, metrics.ExactVolumeCuFt, 2);
            // Enclosing AABB at 45 deg is larger than 40 cu ft
            Assert.True(metrics.AabbVolumeCuFt > metrics.ExactVolumeCuFt);
            Assert.True(metrics.WastedVolumeCuFt > 0.0);
            Assert.True(metrics.WastedVolumePercent > 0.0);
            Assert.True(metrics.AverageOverApproximationRatio > 1.0);
        }
    }
}
