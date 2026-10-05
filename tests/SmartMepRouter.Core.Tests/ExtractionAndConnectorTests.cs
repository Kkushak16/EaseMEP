using System;
using SmartMepRouter.Core.Connectors;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Scenes;
using SmartMepRouter.Core.Spatial;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class ExtractionAndConnectorTests
    {
        [Fact]
        public void ConnectorInfo_PropertiesAndDefaults_AreValid()
        {
            var connector = new ConnectorInfo();
            Assert.Equal(string.Empty, connector.Id);
            Assert.Equal(ConnectorShape.Rectangular, connector.Shape);
            Assert.Equal(Vec3.Zero, connector.Position);
            Assert.Equal(Vec3.Zero, connector.Direction);
            Assert.Equal(0.0, connector.WidthFeet);
            Assert.Equal(0.0, connector.HeightFeet);
            Assert.Equal(0.0, connector.DiameterFeet);
        }

        [Fact]
        public void ConnectorInfo_CustomValues_RetainPrecision()
        {
            var pos = new Vec3(10.5, -20.25, 8.0);
            var dir = new Vec3(1, 0, 0);
            var conn = new ConnectorInfo("conn-01", pos, dir, ConnectorShape.Round, 1.5, 1.5, 1.5)
            {
                SystemClassification = "SupplyAir",
                SystemTypeId = "123456",
                OwnerElementId = "789012"
            };

            Assert.Equal("conn-01", conn.Id);
            Assert.Equal(pos, conn.Position);
            Assert.Equal(dir, conn.Direction);
            Assert.Equal(ConnectorShape.Round, conn.Shape);
            Assert.Equal(1.5, conn.DiameterFeet);
            Assert.Equal("SupplyAir", conn.SystemClassification);
            Assert.Equal("123456", conn.SystemTypeId);
            Assert.Equal("789012", conn.OwnerElementId);
        }

        [Fact]
        public void Aabb_FromPoints_EnclosesEndpointsWithMargin()
        {
            var p1 = new Vec3(0, 0, 0);
            var p2 = new Vec3(10, 20, 5);
            double margin = 2.0;

            var aabb = Aabb.FromPoints(p1, p2, margin);

            Assert.Equal(-2.0, aabb.Min.X, 3);
            Assert.Equal(-2.0, aabb.Min.Y, 3);
            Assert.Equal(-2.0, aabb.Min.Z, 3);

            Assert.Equal(12.0, aabb.Max.X, 3);
            Assert.Equal(22.0, aabb.Max.Y, 3);
            Assert.Equal(7.0, aabb.Max.Z, 3);

            Assert.True(aabb.Contains(p1));
            Assert.True(aabb.Contains(p2));
            Assert.True(aabb.Contains(new Vec3(5, 10, 2.5)));
        }

        [Fact]
        public void Aabb_FromPoints_WithCustomPlenumZ_AppliesVerticalBounds()
        {
            var p1 = new Vec3(2, 3, 5);
            var p2 = new Vec3(12, 15, 6);
            double margin = 1.0;
            double plenumMinZ = 4.0;
            double plenumMaxZ = 10.0;

            var aabb = Aabb.FromPoints(p1, p2, margin, plenumMinZ, plenumMaxZ);

            Assert.Equal(1.0, aabb.Min.X, 3);
            Assert.Equal(13.0, aabb.Max.X, 3);
            Assert.Equal(2.0, aabb.Min.Y, 3);
            Assert.Equal(16.0, aabb.Max.Y, 3);
            Assert.Equal(4.0, aabb.Min.Z, 3);
            Assert.Equal(10.0, aabb.Max.Z, 3);
        }

        [Fact]
        public void TransformedObb_SimulatingRotatedLinkInstance_DetectsCollision()
        {
            // Simulate an element inside a linked model that is rotated 45 degrees around Z
            // and translated to (10, 10, 0) in host coordinates
            double angle = Math.PI / 4.0; // 45 degrees
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);

            Vec3 hostCenter = new Vec3(10, 10, 5);
            Vec3 halfExtents = new Vec3(2, 1, 3); // 4 ft long by 2 ft wide column
            Vec3 axisX = new Vec3(cos, sin, 0);
            Vec3 axisY = new Vec3(-sin, cos, 0);
            Vec3 axisZ = new Vec3(0, 0, 1);

            var obb = new ObbObstacle("link_col_01", hostCenter, halfExtents, axisX, axisY, axisZ);

            // Center of the OBB must be blocked
            var centerCell = new Aabb(hostCenter - new Vec3(0.5, 0.5, 0.5), hostCenter + new Vec3(0.5, 0.5, 0.5));
            Assert.True(obb.IntersectsCell(centerCell));

            // Point along the 45-degree major axis should be blocked within extents
            Vec3 pointOnMajorAxis = hostCenter + axisX * 1.5;
            var axisCell = new Aabb(pointOnMajorAxis - new Vec3(0.1, 0.1, 0.1), pointOnMajorAxis + new Vec3(0.1, 0.1, 0.1));
            Assert.True(obb.IntersectsCell(axisCell));

            // A point that would be inside the AABB bounding box but OUTSIDE the rotated OBB corners
            // The OBB bounding box in XY has half width ~ 2*cos(45) + 1*sin(45) = 1.414 + 0.707 = 2.121
            // Point at (10 + 2.0, 10 - 2.0, 5) is inside the AABB but outside the diamond
            Vec3 cornerCandidate = new Vec3(hostCenter.X + 2.0, hostCenter.Y - 2.0, hostCenter.Z);
            var outsideCell = new Aabb(cornerCandidate - new Vec3(0.1, 0.1, 0.1), cornerCandidate + new Vec3(0.1, 0.1, 0.1));
            Assert.False(obb.IntersectsCell(outsideCell), "Rotated OBB must not block free corners inside its bounding box");

            // Segment passing directly through the OBB must register collision
            Vec3 segStart = hostCenter - new Vec3(5, 0, 0);
            Vec3 segEnd = hostCenter + new Vec3(5, 0, 0);
            Assert.True(obb.IntersectsSegment(segStart, segEnd));

            // Segment missing the OBB must return false
            Vec3 segMissStart = hostCenter + new Vec3(0, 5, 0);
            Vec3 segMissEnd = hostCenter + new Vec3(10, 5, 0);
            Assert.False(obb.IntersectsSegment(segMissStart, segMissEnd));
        }

        [Fact]
        public void DuctInflation_Formula_SatisfiesFR17()
        {
            // FR-17: total inflation = clearance + insulation + max(width, height) / 2
            double width = Units.MmToFeet(300.0);
            double height = Units.MmToFeet(200.0);
            double insulation = Units.MmToFeet(25.0);
            double clearance = Units.MmToFeet(50.0);

            var spec = new DuctSpec(width, height, insulation, clearance);

            double expectedDuctHalf = Math.Max(width, height) * 0.5;
            double expectedTotal = clearance + insulation + expectedDuctHalf;

            Assert.Equal(expectedTotal, spec.TotalInflationFeet, 6);

            // Test obstacle inflation
            var box = new BoxObstacle("box1", new Vec3(0, 0, 0), new Vec3(2, 2, 2));
            var inflated = box.Inflate(spec.TotalInflationFeet);

            Assert.Equal(box.Bounds.Min.X - spec.TotalInflationFeet, inflated.Bounds.Min.X, 6);
            Assert.Equal(box.Bounds.Max.X + spec.TotalInflationFeet, inflated.Bounds.Max.X, 6);
        }
    }
}
