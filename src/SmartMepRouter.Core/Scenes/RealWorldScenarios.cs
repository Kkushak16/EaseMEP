using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Scenes
{
    /// <summary>
    /// Provides curated, realistic BIM scenarios reflecting actual building coordination
    /// challenges for Phase 9 evaluation (FR-05, FR-16, FR-17).
    /// </summary>
    public static class RealWorldScenarios
    {
        /// <summary>
        /// Scenario 1: Long structural corridor with transverse ceiling beams and concrete columns.
        /// Reflects typical linked structural coordination where duct must step under structural framing.
        /// </summary>
        public static SceneDefinition CreateStructuralCorridorScenario()
        {
            var bounds = new Aabb(new Vec3(-5, -8, 0), new Vec3(85, 8, 14));
            var scene = new SceneDefinition(
                "real_structural_corridor",
                new Vec3(0, 0, 8),
                new Vec3(80, 0, 8),
                bounds,
                startDir: Direction3D.PositiveX,
                endDir: Direction3D.NegativeX,
                duct: DuctSpec.Rectangular(2.0, 1.0, clearanceFeet: Units.MmToFeet(100.0))); // 600x300mm

            // Concrete columns along corridor sides
            for (int x = 20; x <= 60; x += 20)
            {
                scene.Obstacles.Add(new BoxObstacle($"col_north_{x}", new Aabb(new Vec3(x - 1, 4, 0), new Vec3(x + 1, 6, 14))));
                scene.Obstacles.Add(new BoxObstacle($"col_south_{x}", new Aabb(new Vec3(x - 1, -6, 0), new Vec3(x + 1, -4, 14))));
            }

            // Transverse structural steel beams hanging from ceiling (Z from 8.5 to 12.0)
            int[] beamX = { 15, 35, 55, 70 };
            for (int i = 0; i < beamX.Length; i++)
            {
                int bx = beamX[i];
                scene.Obstacles.Add(new BoxObstacle($"beam_{bx}", new Aabb(new Vec3(bx - 0.75, -8, 8.5), new Vec3(bx + 0.75, 8, 12))));
            }

            return scene;
        }

        /// <summary>
        /// Scenario 2: Congested ceiling plenum with angled cable trays (OBBs) and existing pipe banks.
        /// Demonstrates 15-axis SAT and directional inflation navigating tight clearance corridors.
        /// </summary>
        public static SceneDefinition CreateCongestedPlenumScenario()
        {
            var bounds = new Aabb(new Vec3(-5, -15, 0), new Vec3(55, 15, 14));
            var scene = new SceneDefinition(
                "real_congested_plenum",
                new Vec3(0, 8, 9),
                new Vec3(50, -8, 9),
                bounds,
                startDir: Direction3D.PositiveX,
                endDir: Direction3D.NegativeX,
                duct: DuctSpec.Rectangular(1.5, 1.0, clearanceFeet: Units.MmToFeet(75.0))); // 450x300mm

            // Existing chilled water pipe bank running at X = 25, Z from 7.5 to 10.5, with slot at Y in [1, 5]
            scene.Obstacles.Add(new BoxObstacle("pipe_bank_north", new Aabb(new Vec3(24, 5, 7.5), new Vec3(26, 15, 10.5))));
            scene.Obstacles.Add(new BoxObstacle("pipe_bank_south", new Aabb(new Vec3(24, -15, 7.5), new Vec3(26, 0, 10.5))));

            // Angled cable tray crossing plenum at 30 degrees (modeled as rotated OBB)
            double rad = 30.0 * Math.PI / 180.0;
            var obbTray = new ObbObstacle(
                "cable_tray_obb",
                center: new Vec3(12, 0, 9),
                halfExtents: new Vec3(1.0, 12.0, 0.5),
                axisX: new Vec3(Math.Cos(rad), Math.Sin(rad), 0),
                axisY: new Vec3(-Math.Sin(rad), Math.Cos(rad), 0),
                axisZ: new Vec3(0, 0, 1));
            scene.Obstacles.Add(obbTray);

            // Ceiling hanger rod barrier
            scene.Obstacles.Add(new BoxObstacle("hanger_grid", new Aabb(new Vec3(35, -4, 6), new Vec3(37, 4, 12))));

            return scene;
        }

        /// <summary>
        /// Scenario 3: Mechanical equipment room with AHU discharge connector, equipment pads,
        /// acoustic barriers, and vertical connection to riser.
        /// </summary>
        public static SceneDefinition CreateMechanicalRoomScenario()
        {
            var bounds = new Aabb(new Vec3(-5, -5, 0), new Vec3(35, 30, 16));
            var scene = new SceneDefinition(
                "real_mech_room_ahu",
                new Vec3(0, 0, 4),      // AHU discharge outlet at 4 ft elevation facing +X
                new Vec3(25, 22, 12),    // Main vertical distribution riser facing -Z
                bounds,
                startDir: Direction3D.PositiveX,
                endDir: Direction3D.NegativeZ,
                duct: DuctSpec.Round(1.5, clearanceFeet: Units.MmToFeet(100.0))); // 450mm round spiral duct

            // AHU unit body itself (positioned behind the discharge connector)
            scene.Obstacles.Add(new BoxObstacle("ahu_body", new Aabb(new Vec3(-14, -6, 0), new Vec3(-2.0, 6, 8))));

            // Concrete equipment pads and boiler units
            scene.Obstacles.Add(new BoxObstacle("boiler_1", new Aabb(new Vec3(6, 4, 0), new Vec3(14, 14, 7))));
            scene.Obstacles.Add(new BoxObstacle("pump_skid", new Aabb(new Vec3(16, 2, 0), new Vec3(22, 10, 4))));

            // Acoustic attenuator wall with access gap
            scene.Obstacles.Add(new BoxObstacle("acoustic_wall_lower", new Aabb(new Vec3(14, 15, 0), new Vec3(16, 28, 9))));

            return scene;
        }

        /// <summary>
        /// Returns all real-world benchmark scenarios.
        /// </summary>
        public static IReadOnlyList<SceneDefinition> GetAllScenarios()
        {
            return new[]
            {
                CreateStructuralCorridorScenario(),
                CreateCongestedPlenumScenario(),
                CreateMechanicalRoomScenario()
            };
        }
    }
}
