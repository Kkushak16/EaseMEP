using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Spatial;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class SpatialHash3DTests
    {
        [Fact]
        public void SpatialHash3D_KeyHashingAndEquality_AreDeterministic()
        {
            var k1 = new SpatialKey(2, -3, 5);
            var k2 = new SpatialKey(2, -3, 5);
            var k3 = new SpatialKey(3, -3, 5);

            Assert.Equal(k1, k2);
            Assert.NotEqual(k1, k3);
            Assert.Equal(k1.GetHashCode(), k2.GetHashCode());
        }

        [Fact]
        public void SpatialHash3D_InsertAndQuery_RetrievesOverlappingObstacles()
        {
            var hash = new SpatialHash3D<string>(cellSizeFeet: 5.0);

            var bounds1 = new Aabb(new Vec3(0, 0, 0), new Vec3(4, 4, 4));
            var bounds2 = new Aabb(new Vec3(10, 10, 10), new Vec3(14, 14, 14));

            hash.Insert("obs1", bounds1);
            hash.Insert("obs2", bounds2);

            Assert.Equal(2, hash.ItemCount);

            // Query overlapping only obs1
            var results = new HashSet<string>();
            hash.Query(new Aabb(new Vec3(2, 2, 2), new Vec3(3, 3, 3)), results);

            Assert.Contains("obs1", results);
            Assert.DoesNotContain("obs2", results);

            // Query overlapping only obs2
            results.Clear();
            hash.Query(new Aabb(new Vec3(11, 11, 11), new Vec3(12, 12, 12)), results);

            Assert.Contains("obs2", results);
            Assert.DoesNotContain("obs1", results);

            // Query empty region
            results.Clear();
            hash.Query(new Aabb(new Vec3(50, 50, 50), new Vec3(55, 55, 55)), results);
            Assert.Empty(results);
        }

        [Fact]
        public void CollisionVerifier_WithSpatialHash_AcceleratesVerification()
        {
            var obstacles = new List<IObstacle>();

            // Create a grid of 50 columns
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    obstacles.Add(new BoxObstacle(
                        $"col_{x}_{y}",
                        new Aabb(new Vec3(x * 10, y * 10, 0), new Vec3(x * 10 + 2, y * 10 + 2, 15))));
                }
            }

            var spatialIndex = CollisionVerifier.BuildSpatialIndex(obstacles, bucketSizeFeet: 5.0);
            Assert.Equal(50, spatialIndex.ItemCount);

            // Clear path passing between columns at y=5
            var clearPath = new List<Vec3>
            {
                new Vec3(-5, 5, 8),
                new Vec3(105, 5, 8)
            };

            bool isClear = CollisionVerifier.IsPathCollisionFree(clearPath, spatialIndex);
            Assert.True(isClear);

            // Path clashing with column (0,0) at (1, 1, 8)
            var clashingPath = new List<Vec3>
            {
                new Vec3(-5, 1, 8),
                new Vec3(15, 1, 8)
            };

            bool isClashing = !CollisionVerifier.IsPathCollisionFree(clashingPath, spatialIndex);
            Assert.True(isClashing);
        }
    }
}
