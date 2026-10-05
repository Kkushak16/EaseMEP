using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Post;
using SmartMepRouter.Core.Spatial;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class EndpointAlignerTests
    {
        [Fact]
        public void Align_StraightSegment_SnapsStartAndEnd()
        {
            var corners = new List<Vec3> { new Vec3(0, 0, 0), new Vec3(10, 0, 0) };
            var exactStart = new Vec3(0.12, 0.0, 0.0);
            var exactEnd = new Vec3(10.35, 0.0, 0.0);

            var aligned = EndpointAligner.Align(corners, exactStart, exactEnd);

            Assert.Equal(2, aligned.Count);
            Assert.Equal(exactStart, aligned[0]);
            Assert.Equal(exactEnd, aligned[1]);
        }

        [Fact]
        public void Align_TwoBendRoute_SnapsFirstAndLastLegOrthogonally()
        {
            // Initial lattice route: (0,0,0) -> (5,0,0) -> (5,5,0) -> (10,5,0)
            var corners = new List<Vec3>
            {
                new Vec3(0, 0, 0),
                new Vec3(5, 0, 0),
                new Vec3(5, 5, 0),
                new Vec3(10, 5, 0)
            };

            // Exact connectors with floating point offsets
            var exactStart = new Vec3(0.0, 0.25, 0.0);
            var exactEnd = new Vec3(10.5, 5.35, 0.0);

            var aligned = EndpointAligner.Align(corners, exactStart, exactEnd);

            Assert.Equal(4, aligned.Count);
            Assert.Equal(exactStart, aligned[0]);
            Assert.Equal(exactEnd, aligned[aligned.Count - 1]);

            // First leg (from exactStart to c1) should move strictly along X:
            // Y and Z of c1 must match exactStart.Y and exactStart.Z
            Assert.Equal(exactStart.Y, aligned[1].Y, 4);
            Assert.Equal(exactStart.Z, aligned[1].Z, 4);

            // Last leg (from c2 to exactEnd) should move strictly along X:
            // Y and Z of c2 must match exactEnd.Y and exactEnd.Z
            Assert.Equal(exactEnd.Y, aligned[2].Y, 4);
            Assert.Equal(exactEnd.Z, aligned[2].Z, 4);
        }

        [Fact]
        public void Align_WhenCollisionCreated_RollsBackToOriginalRoute()
        {
            var corners = new List<Vec3>
            {
                new Vec3(0, 0, 0),
                new Vec3(5, 0, 0),
                new Vec3(5, 5, 0)
            };

            var exactStart = new Vec3(0, 0, 0);
            // Move end into an obstacle
            var exactEnd = new Vec3(5, 5.5, 0);

            // Obstacle blocking (5, 5.5, 0)
            var obstacles = new List<IObstacle>
            {
                new BoxObstacle("blocker", new Vec3(4.8, 5.2, -1), new Vec3(5.2, 5.8, 1))
            };

            var result = EndpointAligner.Align(corners, exactStart, exactEnd, obstacles);

            // Should revert to original corners since aligned path intersects blocker
            Assert.Equal(corners[0], result[0]);
            Assert.Equal(corners[corners.Count - 1], result[result.Count - 1]);
        }
    }
}
