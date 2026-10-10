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
    public class FabricationConstraintsTests
    {
        private readonly double _cellSize = 1.0;
        private readonly Aabb _corridor = new Aabb(new Vec3(-20, -20, -10), new Vec3(40, 40, 20));

        [Fact]
        public void MinRunCells_EnforcesMinimumStraightLengthBetweenTurns()
        {
            // Place an L-shaped obstacle requiring a detour
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);
            grid.BlockAabb(new Aabb(new Vec3(4, -5, -2), new Vec3(5, 5, 2)));

            // Cost model with MinRunCells = 3: every segment after a turn must be >= 3 cells long
            var cost = new CostModel
            {
                WeightLength = 1.0,
                WeightBend = 5.0,
                MinRunCells = 3,
                StartStubCells = 1,
                EndStubCells = 1
            };

            var router = new Router(cost);
            RouteResult result = router.Route(grid, new Vec3(0, 0, 0), new Vec3(10, 0, 0));

            Assert.True(result.IsSuccess);
            Assert.True(result.Points.Count >= 4);

            // Check that every intermediate segment between corners has length >= 3.0 ft (3 cells)
            for (int i = 1; i < result.Points.Count - 2; i++)
            {
                double segLen = (result.Points[i + 1] - result.Points[i]).Length();
                Assert.True(segLen >= 3.0 - 1e-4, $"Segment {i} length {segLen} is less than required 3 cells");
            }
        }

        [Fact]
        public void StartStub_EnforcesMandatoryLeadOutAlongStartDirection()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);

            // Start connector pointing along +Y, but goal is along +X at (10, 0, 0)
            // Router must travel at least StartStubCells = 3 along +Y before turning towards +X
            var cost = new CostModel
            {
                WeightLength = 1.0,
                WeightBend = 5.0,
                MinRunCells = 1,
                StartStubCells = 3,
                EndStubCells = 1
            };

            var router = new Router(cost);
            RouteResult result = router.Route(grid, new Vec3(0, 0, 0), new Vec3(10, 0, 0), startDir: Direction3D.PositiveY);

            Assert.True(result.IsSuccess);

            // First leg must be along +Y and at least 3 cells (3 ft) long
            Vec3 firstLeg = result.Points[1] - result.Points[0];
            Assert.True(firstLeg.Y >= 3.0 - 1e-4, $"First leg Y displacement {firstLeg.Y} is less than required 3 cells");
            Assert.Equal(0.0, firstLeg.X, precision: 3);
            Assert.Equal(0.0, firstLeg.Z, precision: 3);
        }

        [Fact]
        public void EndStub_EnforcesMandatoryLeadInAlongEndDirection()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);

            // Goal connector normal is +Y (meaning route must arrive along -Y into the connector)
            // EndStubCells = 3 means the arrival leg along -Y must be at least 3 cells long
            var cost = new CostModel
            {
                WeightLength = 1.0,
                WeightBend = 5.0,
                MinRunCells = 1,
                StartStubCells = 1,
                EndStubCells = 3
            };

            var router = new Router(cost);
            RouteResult result = router.Route(
                grid,
                new Vec3(0, 0, 0),
                new Vec3(10, 0, 0),
                startDir: Direction3D.None,
                endDir: Direction3D.PositiveY);

            Assert.True(result.IsSuccess);

            // The last leg arrives at goal (10, 0, 0) along -Y from a Y >= 3 coordinate
            int lastIdx = result.Points.Count - 1;
            Vec3 lastPoint = result.Points[lastIdx];
            Vec3 secondLastPoint = result.Points[lastIdx - 1];

            Assert.Equal(10.0, lastPoint.X, precision: 3);
            Assert.Equal(0.0, lastPoint.Y, precision: 3);
            Assert.True(secondLastPoint.Y >= 3.0 - 1e-4, $"Arrival leg length {secondLastPoint.Y - lastPoint.Y} is less than 3 cells");
        }

        [Fact]
        public void CostPresets_FewerBendsVsShortestLength_ProducesDistinctRoutes()
        {
            var grid = new SparseGrid(new Vec3(0, 0, 0), _cellSize, _corridor);

            // Diagonal obstacle pattern where shorter route can zig-zag with 4 bends,
            // while fewer bends takes a single wide 2-bend detour
            grid.BlockAabb(new Aabb(new Vec3(3, -2, -2), new Vec3(4, 3, 2)));
            grid.BlockAabb(new Aabb(new Vec3(7, -3, -2), new Vec3(8, 2, 2)));

            var fewerBendsCost = CostModel.FewerBends;
            var shortestLengthCost = CostModel.ShortestLength;

            var routerFewer = new Router(fewerBendsCost);
            var routerShort = new Router(shortestLengthCost);

            RouteResult resFewer = routerFewer.Route(grid, new Vec3(0, 0, 0), new Vec3(12, 0, 0));
            RouteResult resShort = routerShort.Route(grid, new Vec3(0, 0, 0), new Vec3(12, 0, 0));

            Assert.True(resFewer.IsSuccess);
            Assert.True(resShort.IsSuccess);

            // Fewer bends preset must have fewer or equal bends
            Assert.True(resFewer.BendCount <= resShort.BendCount,
                $"FewerBends bends ({resFewer.BendCount}) should be <= ShortestLength bends ({resShort.BendCount})");

            // Shortest length preset must have <= total length
            Assert.True(resShort.LengthFeet <= resFewer.LengthFeet + 1e-4,
                $"ShortestLength length ({resShort.LengthFeet}) should be <= FewerBends length ({resFewer.LengthFeet})");
        }

        [Fact]
        public void DirectionDependentInflation_EnablesPassageThroughTightVerticalSlot()
        {
            // Corridor spanning Z from -2 to 6
            var corridor = new Aabb(new Vec3(-5, -5, -2), new Vec3(20, 10, 6));

            // Obstacle leaves a vertical slot of 1.5 ft between Z=1.5 and Z=3.0 (slot height = 1.5 ft)
            // Ceiling / top beam at Z >= 3.0
            var topObstacle = new BoxObstacle("top_beam", new Aabb(new Vec3(5, -5, 3.0), new Vec3(6, 10, 6.0)));
            // Floor / bottom duct at Z <= 1.0
            var bottomObstacle = new BoxObstacle("bottom_duct", new Aabb(new Vec3(5, -5, -2.0), new Vec3(6, 10, 1.0)));

            // Flat rectangular duct: Width = 3.0 ft, Height = 0.5 ft
            // Clearance = 0.1 ft, Insulation = 0.0 ft
            double ductW = 3.0;
            double ductH = 0.5;
            double clearance = 0.1;
            double cellSizeFt = 0.5;

            Vec3 start = new Vec3(0, 0, 2.0);
            Vec3 goal = new Vec3(12, 0, 2.0);

            // Case A: Isotropic conservative inflation:
            // Margin = clearance + max(W, H)/2 = 0.1 + 1.5 = 1.6 ft!
            // Top beam (Z=3.0) inflates down to 3.0 - 1.6 = 1.4 ft.
            // Bottom duct (Z=1.0) inflates up to 1.0 + 1.6 = 2.6 ft.
            // Notice: 2.6 ft > 1.4 ft, so the slot is completely blocked!
            var gridIsotropic = new SparseGrid(start, cellSizeFt, corridor, enableDirectional: false);
            gridIsotropic.BlockObstacle(topObstacle.Inflate(clearance + Math.Max(ductW, ductH) * 0.5));
            gridIsotropic.BlockObstacle(bottomObstacle.Inflate(clearance + Math.Max(ductW, ductH) * 0.5));

            var router = new Router(CostModel.Default);
            RouteResult resIsotropic = router.Route(gridIsotropic, start, goal);

            // Isotropic inflation blocks the straight path through the slot
            // Since top obstacle extends to Y=10 and -5, it can't bypass horizontally either
            Assert.False(resIsotropic.IsSuccess, "Isotropic inflation should fail because slot is falsely blocked");

            // Case B: Direction-dependent inflation (FR-17):
            // Moving along X: cross section is in YZ plane.
            // Margin in Z is only clearance + H/2 = 0.1 + 0.25 = 0.35 ft!
            // Top beam (Z=3.0) inflates down to 3.0 - 0.35 = 2.65 ft.
            // Bottom duct (Z=1.0) inflates up to 1.0 + 0.35 = 1.35 ft.
            // Free vertical channel between Z=1.35 and Z=2.65 ft (1.30 ft clear!)
            // The duct at Z=2.0 fits with plenty of room!
            var gridDirectional = new SparseGrid(start, cellSizeFt, corridor, enableDirectional: true);
            gridDirectional.BlockObstacleDirectional(topObstacle, clearance, ductW, ductH);
            gridDirectional.BlockObstacleDirectional(bottomObstacle, clearance, ductW, ductH);

            RouteResult resDirectional = router.Route(gridDirectional, start, goal);

            Assert.True(resDirectional.IsSuccess, "Direction-dependent inflation must successfully find the route through the tight vertical slot!");
            Assert.Equal(0, resDirectional.BendCount); // Direct straight path!
        }

        [Fact]
        public void FittingFeasibility_ValidatesStraightSegments()
        {
            var validCorners = new List<Vec3>
            {
                new Vec3(0, 0, 0),
                new Vec3(5, 0, 0),
                new Vec3(5, 5, 0),
                new Vec3(5, 5, 4)
            };

            var resultValid = FittingFeasibility.Verify(validCorners, minStraightLengthFeet: 1.0);
            Assert.True(resultValid.IsFeasible);
            Assert.Empty(resultValid.Violations);
            Assert.Equal(4.0, resultValid.ShortestSegmentLengthFeet, precision: 2);

            // Route with a tiny 0.1 ft jog (violates 1.0 ft requirement)
            var invalidCorners = new List<Vec3>
            {
                new Vec3(0, 0, 0),
                new Vec3(5, 0, 0),
                new Vec3(5, 0.1, 0),
                new Vec3(10, 0.1, 0)
            };

            var resultInvalid = FittingFeasibility.Verify(invalidCorners, minStraightLengthFeet: 1.0);
            Assert.False(resultInvalid.IsFeasible);
            Assert.Single(resultInvalid.Violations);
            Assert.Equal(0.1, resultInvalid.ShortestSegmentLengthFeet, precision: 2);
        }

        [Fact]
        public void RoundDuctSpec_ConfiguresIsotropicInflation()
        {
            var roundDuct = DuctSpec.Round(diameterFeet: 1.5, insulationFeet: 0.1, clearanceFeet: 0.2);

            Assert.True(roundDuct.IsRound);
            Assert.Equal(ConnectorShape.Round, roundDuct.Shape);
            Assert.Equal(1.5, roundDuct.DiameterFeet);
            Assert.Equal(1.5, roundDuct.WidthFeet);
            Assert.Equal(1.5, roundDuct.HeightFeet);

            // Total inflation = clearance (0.2) + insulation (0.1) + diameter/2 (0.75) = 1.05 ft
            Assert.Equal(1.05, roundDuct.TotalInflationFeet, precision: 4);
        }

        [Fact]
        public void ObbObstacle_AnisotropicInflation_CalculatesProjectedMargins()
        {
            // 45 degree yaw OBB with half extents (1, 1, 1)
            double yaw = Math.PI / 4.0;
            var obb = ObbObstacle.FromYaw("rotated_col", new Vec3(0, 0, 0), new Vec3(1, 1, 1), yaw);

            // Anisotropic inflation: X=1.0, Y=0.0, Z=0.5
            var inflated = (ObbObstacle)obb.Inflate(1.0, 0.0, 0.5);

            // AxisX = (cos(45), sin(45), 0), AxisY = (-sin(45), cos(45), 0)
            // dx = |cos| * 1.0 + |sin| * 0.0 = 0.7071
            // dy = |-sin| * 1.0 + |cos| * 0.0 = 0.7071
            // dz = |0| + |0| + |1| * 0.5 = 0.5
            Assert.Equal(1.0 + Math.Sqrt(0.5), inflated.HalfExtents.X, precision: 3);
            Assert.Equal(1.0 + Math.Sqrt(0.5), inflated.HalfExtents.Y, precision: 3);
            Assert.Equal(1.5, inflated.HalfExtents.Z, precision: 3);
        }
    }
}
