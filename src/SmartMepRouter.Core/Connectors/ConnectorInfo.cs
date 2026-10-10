using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Connectors
{
    /// <summary>
    /// Encapsulates geometric and mechanical properties of an MEP connector in Revit internal decimal feet.
    /// </summary>
    public class ConnectorInfo
    {
        /// <summary>Unique identifier or string ID for the connector.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Global position coordinates in decimal feet.</summary>
        public Vec3 Position { get; set; }

        /// <summary>Outward-pointing normal direction vector.</summary>
        public Vec3 Direction { get; set; }

        /// <summary>Geometric shape profile of the connector.</summary>
        public ConnectorShape Shape { get; set; } = ConnectorShape.Rectangular;

        /// <summary>Width in decimal feet (for rectangular or oval profiles).</summary>
        public double WidthFeet { get; set; }

        /// <summary>Height in decimal feet (for rectangular or oval profiles).</summary>
        public double HeightFeet { get; set; }

        /// <summary>Diameter in decimal feet (for round profiles).</summary>
        public double DiameterFeet { get; set; }

        /// <summary>MEP discipline category (Duct, Pipe, or CableTray).</summary>
        public MepDiscipline Discipline { get; set; } = MepDiscipline.Duct;

        /// <summary>MEP System classification or name (e.g. Supply Air, Return Air, Hydronic Supply).</summary>
        public string SystemClassification { get; set; } = string.Empty;

        /// <summary>Revit ElementId of the MEPSystemType, if available.</summary>
        public string SystemTypeId { get; set; } = string.Empty;

        /// <summary>Identifier of the owner element hosting this connector.</summary>
        public string OwnerElementId { get; set; } = string.Empty;

        /// <summary>Constructs a default connector descriptor.</summary>
        public ConnectorInfo() { }

        /// <summary>Constructs a connector descriptor with explicit coordinates in decimal feet.</summary>
        public ConnectorInfo(string id, Vec3 position, Vec3 direction, ConnectorShape shape, double widthFeet, double heightFeet, double diameterFeet, MepDiscipline discipline = MepDiscipline.Duct)
        {
            Id = id;
            Position = position;
            Direction = direction;
            Shape = shape;
            WidthFeet = widthFeet;
            HeightFeet = heightFeet;
            DiameterFeet = diameterFeet;
            Discipline = discipline;
        }
    }
}
