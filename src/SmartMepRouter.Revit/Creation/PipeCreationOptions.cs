using Autodesk.Revit.DB;
using CoreUnits = SmartMepRouter.Core.Geometry.Units;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Parameters and element types required to generate native connected Revit pipes and fittings (Phase 12).
    /// All dimensions are in Revit internal decimal feet.
    /// </summary>
    public class PipeCreationOptions
    {
        /// <summary>Revit ElementId of the PipingSystemType (Hydronic Supply, Domestic Water, etc.).</summary>
        public ElementId SystemTypeId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Revit ElementId of the PipeType family symbol.</summary>
        public ElementId PipeTypeId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Revit ElementId of the reference Level hosting the pipe elements.</summary>
        public ElementId LevelId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Pipe outer diameter in decimal feet (default 75 mm / ~3 inches).</summary>
        public double DiameterFeet { get; set; } = CoreUnits.MmToFeet(75.0);

        /// <summary>Optional slope percentage (0.0 for pressurized piping; e.g. 0.01 for 1% gravity drainage).</summary>
        public double Slope { get; set; } = 0.0;

        /// <summary>Optional start connector on an existing equipment/fixture element to connect to.</summary>
        public Connector? StartTerminalConnector { get; set; }

        /// <summary>Optional end connector on an existing equipment/fixture element to connect to.</summary>
        public Connector? EndTerminalConnector { get; set; }
    }
}
