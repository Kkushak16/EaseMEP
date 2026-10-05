using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Post;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class PathSimplifierTests
    {
        [Fact]
        public void CompressCollinear_ReducesStraightSegmentsToCorners()
        {
            var raw = new List<Vec3>
            {
                new Vec3(0, 0, 0),
                new Vec3(1, 0, 0),
                new Vec3(2, 0, 0),
                new Vec3(3, 0, 0), // Turn here
                new Vec3(3, 1, 0),
                new Vec3(3, 2, 0), // Turn here
                new Vec3(3, 2, 1),
                new Vec3(3, 2, 2)
            };

            var corners = PathSimplifier.CompressCollinear(raw);

            Assert.Equal(4, corners.Count);
            Assert.Equal(new Vec3(0, 0, 0), corners[0]);
            Assert.Equal(new Vec3(3, 0, 0), corners[1]);
            Assert.Equal(new Vec3(3, 2, 0), corners[2]);
            Assert.Equal(new Vec3(3, 2, 2), corners[3]);

            int bends = PathSimplifier.CountBends(corners);
            Assert.Equal(2, bends);

            double totalLength = PathSimplifier.ComputeTotalLength(corners);
            Assert.Equal(7.0, totalLength, precision: 5);
        }
    }
}
