using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Scenes
{
    /// <summary>
    /// Deterministic generator for synthetic MEP routing benchmark scenes.
    /// Produces realistic layouts containing structural columns, rotated columns (OBB),
    /// overhead beams, and partition walls.
    /// </summary>
    public static class SceneGenerator
    {
        public static SceneDefinition GenerateScene(int seed, string? id = null)
        {
            var rng = new Random(seed);

            string sceneId = id ?? $"scene-seed-{seed:D4}";

            // Room extents (feet)
            double roomX = rng.Next(25, 45);
            double roomY = rng.Next(16, 30);
            double roomZ = 12.0;

            var corridor = new Aabb(new Vec3(-2, -2, 0), new Vec3(roomX + 2, roomY + 2, roomZ));

            // Start AHU on left wall (+X direction)
            double startY = rng.Next(2, (int)roomY - 2);
            double elevation = rng.Next(8, 10);
            Vec3 startPos = new Vec3(0, startY, elevation);
            Direction3D startDir = Direction3D.PositiveX;

            // End terminal on right ceiling (-Z or -Y normal)
            double endY = rng.Next(2, (int)roomY - 2);
            Vec3 endPos = new Vec3(roomX, endY, elevation);
            Direction3D endDir = Direction3D.PositiveX;

            var duct = new DuctSpec(widthFeet: 1.0, heightFeet: 0.67, insulationFeet: 0.0833, clearanceFeet: 0.328);
            var scene = new SceneDefinition(sceneId, startPos, endPos, corridor, startDir, endDir, duct);

            double safeRadius = 3.0; // Keep start/end points clear of direct obstacle overlap

            // 1. Generate structural columns (AABB and OBB)
            int columnCount = rng.Next(2, 6);
            for (int i = 0; i < columnCount; i++)
            {
                double cx = rng.Next(6, (int)roomX - 6);
                double cy = rng.Next(3, (int)roomY - 3);
                Vec3 colPos = new Vec3(cx, cy, elevation);

                if (colPos.DistanceTo(startPos) < safeRadius || colPos.DistanceTo(endPos) < safeRadius)
                    continue;

                double colW = 1.5;
                double colD = 1.5;
                bool isRotated = rng.NextDouble() > 0.5;

                if (isRotated)
                {
                    double yaw = (rng.NextDouble() * 60.0 - 30.0) * (Math.PI / 180.0);
                    scene.Obstacles.Add(ObbObstacle.FromYaw($"col-obb-{i}", colPos, new Vec3(colW * 0.5, colD * 0.5, 6.0), yaw));
                }
                else
                {
                    Vec3 half = new Vec3(colW * 0.5, colD * 0.5, 6.0);
                    scene.Obstacles.Add(new BoxObstacle($"col-box-{i}", new Aabb(colPos - half, colPos + half)));
                }
            }

            // 2. Generate transverse beam (overhead obstacle)
            if (rng.NextDouble() > 0.3)
            {
                double beamX = rng.Next(8, (int)roomX - 8);
                double beamH = 1.5; // Deep beam hanging down from ceiling
                Vec3 beamMin = new Vec3(beamX - 0.75, 0, roomZ - beamH);
                Vec3 beamMax = new Vec3(beamX + 0.75, roomY, roomZ);
                scene.Obstacles.Add(new BoxObstacle("beam-transverse", beamMin, beamMax));
            }

            // 3. Generate wall barrier with an opening
            if (rng.NextDouble() > 0.4)
            {
                double wallX = rng.Next(10, (int)roomX - 10);
                double openingY = rng.Next(4, (int)roomY - 4);
                double openingWidth = 5.0;

                // Wall section 1
                if (openingY > 1.0)
                {
                    scene.Obstacles.Add(new BoxObstacle("wall-part-1",
                        new Vec3(wallX - 0.5, 0, 0),
                        new Vec3(wallX + 0.5, openingY, roomZ)));
                }

                // Wall section 2
                if (openingY + openingWidth < roomY)
                {
                    scene.Obstacles.Add(new BoxObstacle("wall-part-2",
                        new Vec3(wallX - 0.5, openingY + openingWidth, 0),
                        new Vec3(wallX + 0.5, roomY, roomZ)));
                }
            }

            return scene;
        }

        public static List<SceneDefinition> GenerateBenchmarkSuite(int count = 50, int startSeed = 1000)
        {
            var suite = new List<SceneDefinition>(count);
            for (int i = 0; i < count; i++)
            {
                suite.Add(GenerateScene(startSeed + i));
            }
            return suite;
        }
    }
}
