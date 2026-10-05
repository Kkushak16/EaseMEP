using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Scenes
{
    /// <summary>
    /// Represents a complete routing benchmark or production scene definition in decimal feet.
    /// </summary>
    public class SceneDefinition
    {
        public string Id { get; set; } = "scene-default";
        public Vec3 StartPos { get; set; }
        public Direction3D StartDir { get; set; } = Direction3D.None;
        public Vec3 EndPos { get; set; }
        public Direction3D EndDir { get; set; } = Direction3D.None;
        public Aabb CorridorBounds { get; set; }
        public DuctSpec Duct { get; set; } = new DuctSpec();
        public List<IObstacle> Obstacles { get; set; } = new List<IObstacle>();

        public SceneDefinition() { }

        public SceneDefinition(
            string id,
            Vec3 startPos,
            Vec3 endPos,
            Aabb corridorBounds,
            Direction3D startDir = Direction3D.None,
            Direction3D endDir = Direction3D.None,
            DuctSpec? duct = null)
        {
            Id = id;
            StartPos = startPos;
            EndPos = endPos;
            CorridorBounds = corridorBounds;
            StartDir = startDir;
            EndDir = endDir;
            Duct = duct ?? new DuctSpec();
            Obstacles = new List<IObstacle>();
        }
    }
}
