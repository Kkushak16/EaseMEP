using System;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Scenes;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class SceneTests
    {
        [Fact]
        public void SceneLoader_RoundTripJson_PreservesAllProperties()
        {
            var scene = new SceneDefinition(
                "room-test-01",
                new Vec3(0, 5, 9),
                new Vec3(30, 15, 9),
                new Aabb(new Vec3(-5, -5, 0), new Vec3(40, 25, 12)),
                Direction3D.PositiveX,
                Direction3D.PositiveX,
                new DuctSpec(1.2, 0.8, 0.1, 0.35));

            scene.Obstacles.Add(new BoxObstacle("col-1", new Vec3(10, 4, 0), new Vec3(12, 6, 12)));
            scene.Obstacles.Add(ObbObstacle.FromYaw("col-rot", new Vec3(20, 10, 6), new Vec3(1, 1, 6), 0.5));

            string json = SceneLoader.ToJson(scene);
            Assert.False(string.IsNullOrWhiteSpace(json));

            SceneDefinition restored = SceneLoader.FromJson(json);

            Assert.Equal(scene.Id, restored.Id);
            Assert.Equal(scene.StartPos, restored.StartPos);
            Assert.Equal(scene.EndPos, restored.EndPos);
            Assert.Equal(scene.StartDir, restored.StartDir);
            Assert.Equal(scene.EndDir, restored.EndDir);
            Assert.Equal(scene.Duct.WidthFeet, restored.Duct.WidthFeet, precision: 4);
            Assert.Equal(scene.Duct.ClearanceFeet, restored.Duct.ClearanceFeet, precision: 4);
            Assert.Equal(2, restored.Obstacles.Count);
        }

        [Fact]
        public void SceneGenerator_WithFixedSeed_IsDeterministic()
        {
            var scene1 = SceneGenerator.GenerateScene(42);
            var scene2 = SceneGenerator.GenerateScene(42);

            Assert.Equal(scene1.StartPos, scene2.StartPos);
            Assert.Equal(scene1.EndPos, scene2.EndPos);
            Assert.Equal(scene1.Obstacles.Count, scene2.Obstacles.Count);

            for (int i = 0; i < scene1.Obstacles.Count; i++)
            {
                Assert.Equal(scene1.Obstacles[i].Bounds.Min, scene2.Obstacles[i].Bounds.Min);
                Assert.Equal(scene1.Obstacles[i].Bounds.Max, scene2.Obstacles[i].Bounds.Max);
            }
        }
    }
}
