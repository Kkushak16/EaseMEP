using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Connectors;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Post;
using SmartMepRouter.Core.Scenes;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class MultiDisciplineRoutingTests
    {
        [Fact]
        public void ConnectorInfo_DisciplineProperty_DefaultsAndCustom()
        {
            var defaultConn = new ConnectorInfo();
            Assert.Equal(MepDiscipline.Duct, defaultConn.Discipline);

            var pipeConn = new ConnectorInfo(
                "p-01",
                new Vec3(10, 5, 8),
                new Vec3(1, 0, 0),
                ConnectorShape.Round,
                0.33,
                0.33,
                0.33,
                MepDiscipline.Pipe);
            Assert.Equal(MepDiscipline.Pipe, pipeConn.Discipline);
            Assert.Equal(ConnectorShape.Round, pipeConn.Shape);

            var trayConn = new ConnectorInfo(
                "t-01",
                new Vec3(0, 0, 10),
                new Vec3(0, 1, 0),
                ConnectorShape.Rectangular,
                1.5,
                0.5,
                1.5,
                MepDiscipline.CableTray);
            Assert.Equal(MepDiscipline.CableTray, trayConn.Discipline);
            Assert.Equal(ConnectorShape.Rectangular, trayConn.Shape);
        }

        [Fact]
        public void PipeSpec_DimensionsAndInflation_ComputedAccurately()
        {
            double diameter = Units.MmToFeet(100.0); // 100mm pipe (~4 inches)
            double clearance = Units.MmToFeet(50.0); // 50mm clearance
            double insulation = Units.MmToFeet(25.0); // 25mm insulation

            var pipeSpec = new PipeSpec(diameter, clearance, insulation);

            Assert.Equal(diameter, pipeSpec.DiameterFeet, 4);
            Assert.Equal(clearance, pipeSpec.ClearanceFeet, 4);
            Assert.Equal(insulation, pipeSpec.InsulationFeet, 4);
            Assert.Equal(clearance + insulation, pipeSpec.ClearanceAndInsulationFeet, 4);

            double expectedTotalInflation = clearance + insulation + (diameter * 0.5);
            Assert.Equal(expectedTotalInflation, pipeSpec.TotalInflationFeet, 4);

            DuctSpec asDuct = pipeSpec.ToDuctSpec();
            Assert.True(asDuct.IsRound);
            Assert.Equal(diameter, asDuct.DiameterFeet, 4);
        }

        [Fact]
        public void CableTraySpec_DimensionsAndInflation_ComputedAccurately()
        {
            double width = Units.MmToFeet(450.0);  // 450mm tray width
            double height = Units.MmToFeet(100.0); // 100mm tray height
            double clearance = Units.MmToFeet(75.0); // 75mm maintenance clearance

            var traySpec = new CableTraySpec(width, height, clearance);

            Assert.Equal(width, traySpec.WidthFeet, 4);
            Assert.Equal(height, traySpec.HeightFeet, 4);
            Assert.Equal(clearance, traySpec.ClearanceFeet, 4);

            double expectedTotalInflation = clearance + (Math.Max(width, height) * 0.5);
            Assert.Equal(expectedTotalInflation, traySpec.TotalInflationFeet, 4);

            DuctSpec asDuct = traySpec.ToDuctSpec();
            Assert.False(asDuct.IsRound);
            Assert.Equal(width, asDuct.WidthFeet, 4);
            Assert.Equal(height, asDuct.HeightFeet, 4);
        }

        [Fact]
        public void PipeRoute_3DRoutingAroundObstacles_SolvesWithZeroCollisions()
        {
            // Plan pipe route from boiler/chiller connection around a concrete column to a riser
            var start = new Vec3(2.0, 5.0, 8.0);
            var end = new Vec3(20.0, 5.0, 14.0);

            // Obstacle right in the direct path: Column at X=10, Y=5
            var obstacles = new List<IObstacle>
            {
                new BoxObstacle("col-01", new Aabb(new Vec3(8.0, 3.5, 0.0), new Vec3(12.0, 6.5, 20.0)))
            };

            var pipeSpec = new PipeSpec(Units.MmToFeet(100.0), Units.MmToFeet(50.0), Units.MmToFeet(25.0));
            DuctSpec ductSpec = pipeSpec.ToDuctSpec();

            var corridor = new Aabb(new Vec3(-5, -5, 0), new Vec3(30, 20, 25));
            double cellSize = 1.0;
            var grid = new SparseGrid(start, cellSize, corridor);

            foreach (var obs in obstacles)
            {
                grid.BlockObstacle(obs.Inflate(ductSpec.TotalInflationFeet));
            }

            var cost = new CostModel
            {
                Allow45DegreeBends = true,
                WeightLength = 1.0,
                WeightBend = 8.0,
                WeightBend45 = 4.0,
                MinRunCells = 1,
                StartStubCells = 1,
                EndStubCells = 1
            };

            var router = new Router(cost);
            RouteResult result = router.Route(grid, start, end, Direction3D.PositiveX, Direction3D.NegativeX);

            Assert.True(result.IsSuccess, $"Pipe routing failed: {result.Message}");
            Assert.NotNull(result.Points);
            Assert.True(result.Points.Count >= 2);

            // Independent collision verification (Rule R-T2)
            bool collisionFree = CollisionVerifier.IsPathCollisionFree(result.Points, obstacles);
            Assert.True(collisionFree, "Pipe path collided with obstacles!");

            // Fitting angle validation
            bool validAngles = FittingFeasibility.ValidateElbowAngles(result.Points, allow45Degree: true, out var violations);
            Assert.True(validAngles, $"Invalid pipe elbow angles: {string.Join("; ", violations)}");
        }

        [Fact]
        public void CableTrayRoute_3DCorridorRouting_SolvesWithFewerBends()
        {
            // Route electrical cable tray down a corridor around two structural beams
            var start = new Vec3(0.0, 10.0, 12.0);
            var end = new Vec3(30.0, 10.0, 12.0);

            // Beams crossing the corridor at ceiling level
            var obstacles = new List<IObstacle>
            {
                new BoxObstacle("beam-1", new Aabb(new Vec3(8.0, 0.0, 11.0), new Vec3(10.0, 20.0, 14.0))),
                new BoxObstacle("beam-2", new Aabb(new Vec3(18.0, 0.0, 11.0), new Vec3(20.0, 20.0, 14.0)))
            };

            var traySpec = new CableTraySpec(Units.MmToFeet(400.0), Units.MmToFeet(100.0), Units.MmToFeet(50.0));
            DuctSpec ductSpec = traySpec.ToDuctSpec();

            var corridor = new Aabb(new Vec3(-5, 0, 0), new Vec3(35, 20, 20));
            double cellSize = 1.0;
            var grid = new SparseGrid(Vec3.Zero, cellSize, corridor);

            foreach (var obs in obstacles)
            {
                grid.BlockObstacle(obs.Inflate(ductSpec.TotalInflationFeet));
            }

            var cost = CostModel.FewerBends;
            var router = new Router(cost);
            RouteResult result = router.Route(grid, start, end, Direction3D.PositiveX, Direction3D.NegativeX);

            Assert.True(result.IsSuccess, $"Cable tray routing failed: {result.Message}");
            Assert.NotNull(result.Points);

            // Independent collision verification
            bool collisionFree = CollisionVerifier.IsPathCollisionFree(result.Points, obstacles);
            Assert.True(collisionFree, "Cable tray path collided with beams!");

            // Simplification retains collinearity
            var simplified = PathSimplifier.CompressCollinear(result.Points);
            Assert.True(simplified.Count <= result.Points.Count);
        }
    }
}
