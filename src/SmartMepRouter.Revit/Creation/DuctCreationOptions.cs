using Autodesk.Revit.DB;
using SmartMepRouter.Core.Connectors;
using CoreUnits = SmartMepRouter.Core.Geometry.Units;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Parameters and element types required to generate native connected Revit ducts and fittings.
    /// All dimensions are in Revit internal decimal feet.
    /// </summary>
    public class DuctCreationOptions
    {
        /// <summary>Revit ElementId of the MEPSystemType (Supply Air, Return Air, etc.).</summary>
        public ElementId SystemTypeId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Revit ElementId of the DuctType family symbol.</summary>
        public ElementId DuctTypeId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Revit ElementId of the reference Level hosting the duct elements.</summary>
        public ElementId LevelId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Cross-sectional shape (Rectangular, Round, Oval).</summary>
        public ConnectorShape Shape { get; set; } = ConnectorShape.Rectangular;

        /// <summary>Cross-sectional width in decimal feet (default 300 mm).</summary>
        public double WidthFeet { get; set; } = CoreUnits.MmToFeet(300.0);

        /// <summary>Cross-sectional height in decimal feet (default 200 mm).</summary>
        public double HeightFeet { get; set; } = CoreUnits.MmToFeet(200.0);

        /// <summary>Cross-sectional diameter in decimal feet (for round ductwork, default 300 mm).</summary>
        public double DiameterFeet { get; set; } = CoreUnits.MmToFeet(300.0);

        /// <summary>Optional start connector on an existing equipment/terminal element to connect to.</summary>
        public Connector? StartTerminalConnector { get; set; }

        /// <summary>Optional end connector on an existing equipment/terminal element to connect to.</summary>
        public Connector? EndTerminalConnector { get; set; }
    }
}
