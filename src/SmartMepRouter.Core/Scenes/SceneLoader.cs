using System;
using System.IO;
using System.Text.Json;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Scenes
{
    /// <summary>
    /// Loads and saves routing scenes to/from JSON representations.
    /// </summary>
    public static class SceneLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static SceneDefinition FromJson(string json)
        {
            var dto = JsonSerializer.Deserialize<SceneDto>(json, JsonOptions);
            if (dto == null)
                throw new InvalidOperationException("Failed to deserialize scene JSON.");

            return FromDto(dto);
        }

        public static string ToJson(SceneDefinition scene)
        {
            var dto = ToDto(scene);
            return JsonSerializer.Serialize(dto, JsonOptions);
        }

        public static SceneDefinition LoadFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public static void SaveToFile(SceneDefinition scene, string filePath)
        {
            string json = ToJson(scene);
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filePath, json);
        }

        public static SceneDefinition FromDto(SceneDto dto)
        {
            Vec3 startPos = ToVec3(dto.Start.Pos);
            Direction3D startDir = dto.Start.Dir != null ? Direction3DExtensions.FromVector(ToVec3(dto.Start.Dir)) : Direction3D.None;

            Vec3 endPos = ToVec3(dto.End.Pos);
            Direction3D endDir = dto.End.Dir != null ? Direction3DExtensions.FromVector(ToVec3(dto.End.Dir)) : Direction3D.None;

            Aabb corridor = new Aabb(ToVec3(dto.Corridor.Min), ToVec3(dto.Corridor.Max));

            var duct = new DuctSpec(dto.Duct.W, dto.Duct.H, dto.Duct.Insulation, dto.Duct.Clearance);
            var scene = new SceneDefinition(dto.Id, startPos, endPos, corridor, startDir, endDir, duct);

            foreach (var obsDto in dto.Obstacles)
            {
                if (obsDto.Type.Equals("obb", StringComparison.OrdinalIgnoreCase))
                {
                    Vec3 center = ToVec3(obsDto.Center ?? new double[3]);
                    Vec3 half = ToVec3(obsDto.Half ?? new double[3]);
                    double yaw = obsDto.Yaw ?? 0.0;
                    scene.Obstacles.Add(ObbObstacle.FromYaw(obsDto.Id, center, half, yaw));
                }
                else
                {
                    Vec3 min = ToVec3(obsDto.Min ?? new double[3]);
                    Vec3 max = ToVec3(obsDto.Max ?? new double[3]);
                    scene.Obstacles.Add(new BoxObstacle(obsDto.Id, min, max));
                }
            }

            return scene;
        }

        public static SceneDto ToDto(SceneDefinition scene)
        {
            var dto = new SceneDto
            {
                Id = scene.Id,
                Units = "feet",
                Start = new EndpointDto
                {
                    Pos = new[] { scene.StartPos.X, scene.StartPos.Y, scene.StartPos.Z },
                    Dir = scene.StartDir != Direction3D.None ? ToArray(scene.StartDir.ToUnitVector()) : null
                },
                End = new EndpointDto
                {
                    Pos = new[] { scene.EndPos.X, scene.EndPos.Y, scene.EndPos.Z },
                    Dir = scene.EndDir != Direction3D.None ? ToArray(scene.EndDir.ToUnitVector()) : null
                },
                Duct = new DuctDto
                {
                    W = scene.Duct.WidthFeet,
                    H = scene.Duct.HeightFeet,
                    Insulation = scene.Duct.InsulationFeet,
                    Clearance = scene.Duct.ClearanceFeet
                },
                Corridor = new BoundsDto
                {
                    Min = new[] { scene.CorridorBounds.Min.X, scene.CorridorBounds.Min.Y, scene.CorridorBounds.Min.Z },
                    Max = new[] { scene.CorridorBounds.Max.X, scene.CorridorBounds.Max.Y, scene.CorridorBounds.Max.Z }
                }
            };

            foreach (var obs in scene.Obstacles)
            {
                if (obs is ObbObstacle obb)
                {
                    // Compute yaw angle around Z
                    double yaw = Math.Atan2(obb.AxisX.Y, obb.AxisX.X);
                    dto.Obstacles.Add(new ObstacleDto
                    {
                        Type = "obb",
                        Id = obb.Id,
                        Center = ToArray(obb.Center),
                        Half = ToArray(obb.HalfExtents),
                        Yaw = Math.Round(yaw, 4)
                    });
                }
                else if (obs is BoxObstacle box)
                {
                    dto.Obstacles.Add(new ObstacleDto
                    {
                        Type = "box",
                        Id = box.Id,
                        Min = ToArray(box.Bounds.Min),
                        Max = ToArray(box.Bounds.Max)
                    });
                }
            }

            return dto;
        }

        private static Vec3 ToVec3(double[]? arr)
        {
            if (arr == null || arr.Length < 3) return Vec3.Zero;
            return new Vec3(arr[0], arr[1], arr[2]);
        }

        private static double[] ToArray(Vec3 v) => new[] { v.X, v.Y, v.Z };
    }
}
