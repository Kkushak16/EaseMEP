using System;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Scenes;
using SmartMepRouter.Core.Spatial;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class ObstacleTests
    {
        [Fact]
        public void BoxObstacle_IntersectsCellAndSegment()
        {
            var box = new BoxObstacle("box-1", new Vec3(2, 2, 0), new Vec3(4, 4, 4));

            // Cell inside / overlapping
            var insideCell = new Aabb(new Vec3(2.5, 2.5, 1), new Vec3(3.5, 3.5, 2));
            Assert.True(box.IntersectsCell(insideCell));

            // Cell outside
            var outsideCell = new Aabb(new Vec3(5, 5, 0), new Vec3(6, 6, 1));
            Assert.False(box.IntersectsCell(outsideCell));

            // Segment passing through the box
            Assert.True(box.IntersectsSegment(new Vec3(0, 3, 2), new Vec3(6, 3, 2)));

            // Segment missing the box
            Assert.False(box.IntersectsSegment(new Vec3(0, 0, 2), new Vec3(6, 0, 2)));
        }

        [Fact]
        public void ObbObstacle_Rotated45Degrees_PerformsExactSatDetection()
        {
            // OBB at (10, 10, 0) rotated by 45 degrees
            double yaw = Math.PI / 4.0;
            var obb = ObbObstacle.FromYaw("obb-1", new Vec3(10, 10, 5), new Vec3(2, 1, 5), yaw);

            // Center of OBB should intersect
            var centerCell = new Aabb(new Vec3(9.5, 9.5, 4.5), new Vec3(10.5, 10.5, 5.5));
            Assert.True(obb.IntersectsCell(centerCell));

            // Cell far outside
            var farCell = new Aabb(new Vec3(0, 0, 0), new Vec3(1, 1, 1));
            Assert.False(obb.IntersectsCell(farCell));

            // Segment passing straight through the rotated OBB center
            Assert.True(obb.IntersectsSegment(new Vec3(5, 10, 5), new Vec3(15, 10, 5)));

            // Segment passing close by but outside the rotated diamond shape
            Assert.False(obb.IntersectsSegment(new Vec3(10, 14, 5), new Vec3(15, 14, 5)));
        }

        [Fact]
        public void Inflation_IncludesClearanceInsulationAndDuctHalfExtent()
        {
            var duct = new DuctSpec(widthFeet: 1.0, heightFeet: 0.67, insulationFeet: 0.0833, clearanceFeet: 0.328);

            // Expected inflation = 0.328 + 0.0833 + max(1.0, 0.67)/2 = 0.328 + 0.0833 + 0.5 = 0.9113 ft
            double expectedInflation = 0.328 + 0.0833 + 0.5;
            Assert.Equal(expectedInflation, duct.TotalInflationFeet, precision: 4);

            var box = new BoxObstacle("col", new Vec3(10, 10, 0), new Vec3(12, 12, 10));
            var inflated = (BoxObstacle)box.Inflate(duct.TotalInflationFeet);

            Assert.Equal(10.0 - duct.TotalInflationFeet, inflated.Bounds.Min.X, precision: 4);
            Assert.Equal(12.0 + duct.TotalInflationFeet, inflated.Bounds.Max.X, precision: 4);
        }

        [Fact]
        public void CollisionVerifier_DetectsIntersectingSegments()
        {
            var box = new BoxObstacle("wall", new Vec3(4, 0, 0), new Vec3(6, 10, 10));
            var obstacles = new IObstacle[] { box };

            // Polyline that penetrates the wall: (0,5,5) -> (10,5,5)
            var badRoute = new[] { new Vec3(0, 5, 5), new Vec3(10, 5, 5) };
            var badResult = CollisionVerifier.Verify(badRoute, obstacles);
            Assert.True(badResult.HasCollisions);
            Assert.Contains("wall", badResult.CollidingObstacleIds);

            // Polyline that detours around the wall: (0,5,5) -> (0,12,5) -> (10,12,5) -> (10,5,5)
            var goodRoute = new[]
            {
                new Vec3(0, 5, 5),
                new Vec3(0, 12, 5),
                new Vec3(10, 12, 5),
                new Vec3(10, 5, 5)
            };
            var goodResult = CollisionVerifier.Verify(goodRoute, obstacles);
            Assert.False(goodResult.HasCollisions);
            Assert.Empty(goodResult.CollidingObstacleIds);
        }
    }
}
