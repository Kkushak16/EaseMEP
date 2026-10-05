using System;
using SmartMepRouter.Core.Geometry;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class GeometryTests
    {
        [Fact]
        public void Vec3_ArithmeticOperations()
        {
            var a = new Vec3(1, 2, 3);
            var b = new Vec3(4, 5, 6);

            Assert.Equal(new Vec3(5, 7, 9), a + b);
            Assert.Equal(new Vec3(-3, -3, -3), a - b);
            Assert.Equal(new Vec3(2, 4, 6), a * 2.0);
            Assert.Equal(new Vec3(0.5, 1.0, 1.5), a / 2.0);
        }

        [Fact]
        public void Vec3_DistancesAndNormalization()
        {
            var a = new Vec3(0, 0, 0);
            var b = new Vec3(3, 4, 0);

            Assert.Equal(5.0, a.DistanceTo(b), precision: 5);
            Assert.Equal(7.0, a.ManhattanDistanceTo(b), precision: 5);

            Vec3 normalized = b.Normalize();
            Assert.Equal(1.0, normalized.Length(), precision: 5);
            Assert.Equal(0.6, normalized.X, precision: 5);
            Assert.Equal(0.8, normalized.Y, precision: 5);
        }

        [Fact]
        public void Aabb_ContainsAndIntersects()
        {
            var box1 = new Aabb(new Vec3(0, 0, 0), new Vec3(10, 10, 10));
            var box2 = new Aabb(new Vec3(5, 5, 5), new Vec3(15, 15, 15));
            var box3 = new Aabb(new Vec3(20, 20, 20), new Vec3(30, 30, 30));

            Assert.True(box1.Contains(new Vec3(5, 5, 5)));
            Assert.False(box1.Contains(new Vec3(15, 5, 5)));

            Assert.True(box1.Intersects(box2));
            Assert.False(box1.Intersects(box3));
        }

        [Fact]
        public void Aabb_InflateExpandsBounds()
        {
            var box = new Aabb(new Vec3(2, 2, 2), new Vec3(4, 4, 4));
            var inflated = box.Inflate(1.0);

            Assert.Equal(new Vec3(1, 1, 1), inflated.Min);
            Assert.Equal(new Vec3(5, 5, 5), inflated.Max);
        }
    }
}
