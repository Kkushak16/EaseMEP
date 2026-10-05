using System.Collections.Generic;

namespace SmartMepRouter.Core.Scenes
{
    public class SceneDto
    {
        public string Id { get; set; } = "";
        public string Units { get; set; } = "feet";
        public EndpointDto Start { get; set; } = new EndpointDto();
        public EndpointDto End { get; set; } = new EndpointDto();
        public DuctDto Duct { get; set; } = new DuctDto();
        public BoundsDto Corridor { get; set; } = new BoundsDto();
        public List<ObstacleDto> Obstacles { get; set; } = new List<ObstacleDto>();
    }

    public class EndpointDto
    {
        public double[] Pos { get; set; } = new double[3];
        public double[]? Dir { get; set; }
    }

    public class DuctDto
    {
        public double W { get; set; } = 1.0;
        public double H { get; set; } = 0.67;
        public double Insulation { get; set; } = 0.0833;
        public double Clearance { get; set; } = 0.328;
    }

    public class BoundsDto
    {
        public double[] Min { get; set; } = new double[3];
        public double[] Max { get; set; } = new double[3];
    }

    public class ObstacleDto
    {
        public string Type { get; set; } = "box"; // "box" or "obb"
        public string Id { get; set; } = "";
        public double[]? Min { get; set; }
        public double[]? Max { get; set; }
        public double[]? Center { get; set; }
        public double[]? Half { get; set; }
        public double? Yaw { get; set; }
    }
}
