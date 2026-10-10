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
    public class FortyFiveDegreeRoutingTests
    {
        [Fact]
        public void Direction3D_PlanarDiagonals_HaveSqrt2DistanceRatio()
        {
            foreach (var dir in Direction3DExtensions.AllPlanarDiagonal)
            {
                Assert.True(dir.IsDiagonal());
                Assert.InRange(dir.StepDistanceRatio(), 1.414, 1.415);

                Vec3 unit = dir.ToUnitVector();
                Assert.InRange(unit.Length(), 0.9999, 1.0001);
            }
        }

        [Fact]
        public void Direction3D_TransitionRules_AllowStrictly0_45_90Degrees()
        {
            // 0 degree (straight)
            Assert.True(Direction3DExtensions.IsValidHVACTransition(Direction3D.PositiveX, Direction3D.PositiveX, out double angle0));
            Assert.Equal(0.0, angle0, 1);

            // 45 degree coplanar (cardinal to diagonal)
            Assert.True(Direction3DExtensions.IsValidHVACTransition(Direction3D.PositiveX, Direction3D.PosXPosY, out double angle45));
            Assert.Equal(45.0, angle45, 1);

            // 45 degree coplanar (diagonal to cardinal)
            Assert.True(Direction3DExtensions.IsValidHVACTransition(Direction3D.PosXPosY, Direction3D.PositiveY, out double angle45_2));
            Assert.Equal(45.0, angle45_2, 1);

            // 90 degree coplanar (cardinal to cardinal)
            Assert.True(Direction3DExtensions.IsValidHVACTransition(Direction3D.PositiveX, Direction3D.PositiveY, out double angle90));
            Assert.Equal(90.0, angle90, 1);

            // 90 degree coplanar (diagonal to diagonal: (+1, +1) to (-1, +1))
            Assert.True(Direction3DExtensions.IsValidHVACTransition(Direction3D.PosXPosY, Direction3D.NegXPosY, out double angle90_diag));
            Assert.Equal(90.0, angle90_diag, 1);

            // 180 degree reversal -> Forbidden
            Assert.False(Direction3DExtensions.IsValidHVACTransition(Direction3D.PositiveX, Direction3D.NegativeX, out _));
            Assert.False(Direction3DExtensions.IsValidHVACTransition(Direction3D.PosXPosY, Direction3D.NegXNegY, out _));

            // 135 degree obtuse bend -> Forbidden
            Assert.False(Direction3DExtensions.IsValidHVACTransition(Direction3D.PositiveX, Direction3D.NegXPosY, out _));

            // Non-coplanar twist (e.g. +X to PosYPosZ) -> Forbidden
            Assert.False(Direction3DExtensions.IsValidHVACTransition(Direction3D.PositiveX, Direction3D.PosYPosZ, out _));
        }

        [Fact]
        public void Router_With45DegreeBends_PreventsCornerCuttingBetweenObstacles()
        {
            // Scenario: two obstacles touch at corner (x=3, y=1) and (x=2, y=2)
            // A diagonal jump from (2, 1) to (3, 2) must be blocked because flanking cells are blocked!
            double cellSize = 1.0;
            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(5, 5, 0);

            var corridor = new Aabb(new Vec3(-2, -2, -2), new Vec3(8, 8, 4));
            var grid = new SparseGrid(start, cellSize, corridor);

            // Flanking obstacle blocks the corner
            var obs1 = new BoxObstacle("CornerObs1", new Aabb(new Vec3(2.5, 0.5, -1), new Vec3(3.5, 1.5, 1)));
            var obs2 = new BoxObstacle("CornerObs2", new Aabb(new Vec3(1.5, 1.5, -1), new Vec3(2.5, 2.5, 1)));
            grid.BlockObstacle(obs1);
            grid.BlockObstacle(obs2);

            var cost = new CostModel
            {
                Allow45DegreeBends = true,
                MinRunCells = 1,
                StartStubCells = 1,
                EndStubCells = 1
            };

            var router = new Router(cost);
            var result = router.Route(grid, start, goal, Direction3D.PositiveX, Direction3D.PositiveY);

            Assert.Equal(RoutingStatus.Success, result.Status);

            // Exact segment collision check must confirm ZERO collisions
            var obstacles = new List<IObstacle> { obs1, obs2 };
            var verif = CollisionVerifier.Verify(result.Points, obstacles);
            Assert.False(verif.HasCollisions, "Path must not cut corner between touching obstacles.");
        }

        [Fact]
        public void Router_FindsCollisionFreeHorizontal45DegreeOffset()
        {
            double cellSize = 1.0;
            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(20, 4, 0);

            var corridor = new Aabb(new Vec3(-2, -5, -2), new Vec3(25, 10, 5));
            var grid = new SparseGrid(start, cellSize, corridor);

            // Place an obstacle centered on the Y=0 straight path
            var obstacle = new BoxObstacle("Pillar", new Aabb(new Vec3(6, -2, -2), new Vec3(10, 2, 4)));
            grid.BlockObstacle(obstacle);

            var cost = new CostModel
            {
                Allow45DegreeBends = true,
                WeightBend = 10.0,
                WeightBend45 = 4.0,
                MinRunCells = 1,
                StartStubCells = 1,
                EndStubCells = 1
            };

            var router = new Router(cost);
            var result = router.Route(grid, start, goal, Direction3D.PositiveX, Direction3D.PositiveX);

            Assert.Equal(RoutingStatus.Success, result.Status);
            Assert.True(result.Points.Count >= 4);

            // Verify independent segment collision check
            var obstacles = new List<IObstacle> { obstacle };
            Assert.True(CollisionVerifier.IsPathCollisionFree(result.Points, obstacles));

            // Verify that the route utilizes 45-degree angles
            bool contains45DegreeAngle = false;
            for (int i = 1; i < result.Points.Count - 1; i++)
            {
                Vec3 v1 = (result.Points[i] - result.Points[i - 1]).Normalize();
                Vec3 v2 = (result.Points[i + 1] - result.Points[i]).Normalize();
                double dot = v1.Dot(v2);
                double angleDeg = Math.Acos(Math.Max(-1.0, Math.Min(1.0, dot))) * (180.0 / Math.PI);

                if (Math.Abs(angleDeg - 45.0) < 1.0)
                {
                    contains45DegreeAngle = true;
                    break;
                }
            }

            Assert.True(contains45DegreeAngle, "Expected route to use at least one 45-degree offset bend.");
        }

        [Fact]
        public void Router_FindsCollisionFreeVertical45DegreeRollingOffset()
        {
            double cellSize = 1.0;
            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(20, 0, 4);

            var corridor = new Aabb(new Vec3(-2, -5, -2), new Vec3(25, 5, 10));
            var grid = new SparseGrid(start, cellSize, corridor);

            // Obstacle blocking low elevation (Z=0) at X=6..10
            var obstacle = new BoxObstacle("FloorSlabOrBeam", new Aabb(new Vec3(6, -3, -1), new Vec3(10, 3, 2)));
            grid.BlockObstacle(obstacle);

            var cost = new CostModel
            {
                Allow45DegreeBends = true,
                WeightBend = 10.0,
                WeightBend45 = 4.0,
                WeightVertical = 4.0,
                MinRunCells = 1,
                StartStubCells = 1,
                EndStubCells = 1
            };

            var router = new Router(cost);
            var result = router.Route(grid, start, goal, Direction3D.PositiveX, Direction3D.PositiveX);

            Assert.Equal(RoutingStatus.Success, result.Status);
            Assert.True(CollisionVerifier.IsPathCollisionFree(result.Points, new[] { obstacle }));

            // Check for vertical 45-degree bend
            bool hasVertical45 = false;
            for (int i = 1; i < result.Points.Count - 1; i++)
            {
                Vec3 v1 = (result.Points[i] - result.Points[i - 1]).Normalize();
                Vec3 v2 = (result.Points[i + 1] - result.Points[i]).Normalize();
                double dot = v1.Dot(v2);
                double angleDeg = Math.Acos(Math.Max(-1.0, Math.Min(1.0, dot))) * (180.0 / Math.PI);

                if (Math.Abs(angleDeg - 45.0) < 1.0 && (Math.Abs(v1.Z) > 0.01 || Math.Abs(v2.Z) > 0.01))
                {
                    hasVertical45 = true;
                    break;
                }
            }

            Assert.True(hasVertical45, "Expected vertical 45-degree climb/drop bend.");
        }

        [Fact]
        public void Router_45DegreeOffsets_AreShorterThanStrict90DegreeDogleg()
        {
            double cellSize = 1.0;
            Vec3 start = new Vec3(0, 0, 0);
            Vec3 goal = new Vec3(20, 6, 0);

            var corridor = new Aabb(new Vec3(-2, -5, -2), new Vec3(25, 12, 5));

            var obstacle = new BoxObstacle("Obstacle", new Aabb(new Vec3(7, -2, -2), new Vec3(11, 2, 4)));

            // 1. Strict 90-degree routing
            var grid90 = new SparseGrid(start, cellSize, corridor);
            grid90.BlockObstacle(obstacle);
            var cost90 = CostModel.ShortestLength;
            cost90.Allow45DegreeBends = false;
            var router90 = new Router(cost90);
            var result90 = router90.Route(grid90, start, goal, Direction3D.PositiveX, Direction3D.PositiveX);

            // 2. 45-degree routing
            var grid45 = new SparseGrid(start, cellSize, corridor);
            grid45.BlockObstacle(obstacle);
            var cost45 = CostModel.ShortestLength;
            cost45.Allow45DegreeBends = true;
            var router45 = new Router(cost45);
            var result45 = router45.Route(grid45, start, goal, Direction3D.PositiveX, Direction3D.PositiveX);

            Assert.Equal(RoutingStatus.Success, result90.Status);
            Assert.Equal(RoutingStatus.Success, result45.Status);

            // 45-degree routing hypotenuse sqrt(2) is strictly shorter than Manhattan dx + dy
            Assert.True(result45.LengthFeet < result90.LengthFeet,
                $"Expected 45° route length ({result45.LengthFeet:F2} ft) to be strictly shorter than 90° dogleg ({result90.LengthFeet:F2} ft).");

            // Also verify all elbows in 45-degree route conform to standard angles
            bool anglesValid = FittingFeasibility.ValidateElbowAngles(result45.Points, allow45Degree: true, out var violations);
            Assert.True(anglesValid, string.Join("; ", violations));
        }

        [Fact]
        public void FittingFeasibility_ValidateElbowAngles_RejectsNonStandardAngles()
        {
            var compliant45 = new List<Vec3>
            {
                new Vec3(0, 0, 0),
                new Vec3(5, 0, 0),
                new Vec3(10, 5, 0), // 45 degree bend
                new Vec3(15, 5, 0)  // 45 degree return
            };
            Assert.True(FittingFeasibility.ValidateElbowAngles(compliant45, allow45Degree: true, out _));
            Assert.False(FittingFeasibility.ValidateElbowAngles(compliant45, allow45Degree: false, out var v90Only));
            Assert.NotEmpty(v90Only);

            var nonStandard = new List<Vec3>
            {
                new Vec3(0, 0, 0),
                new Vec3(5, 0, 0),
                new Vec3(10, 2, 0), // ~21.8 degrees
                new Vec3(15, 2, 0)
            };
            Assert.False(FittingFeasibility.ValidateElbowAngles(nonStandard, allow45Degree: true, out var violations));
            Assert.NotEmpty(violations);
        }
    }
}
