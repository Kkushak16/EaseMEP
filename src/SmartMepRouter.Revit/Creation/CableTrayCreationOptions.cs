using Autodesk.Revit.DB;
using CoreUnits = SmartMepRouter.Core.Geometry.Units;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Parameters and element types required to generate native connected Revit cable trays and fittings (Phase 12).
    /// All dimensions are in Revit internal decimal feet.
    /// </summary>
    public class CableTrayCreationOptions
    {
        /// <summary>Revit ElementId of the CableTrayType family symbol.</summary>
        public ElementId CableTrayTypeId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Revit ElementId of the reference Level hosting the cable tray elements.</summary>
        public ElementId LevelId { get; set; } = ElementId.InvalidElementId;

        /// <summary>Cable tray width in decimal feet (default 300 mm / ~1 ft).</summary>
        public double WidthFeet { get; set; } = CoreUnits.MmToFeet(300.0);

        /// <summary>Cable tray height in decimal feet (default 100 mm / ~0.33 ft).</summary>
        public double HeightFeet { get; set; } = CoreUnits.MmToFeet(100.0);

        /// <summary>Optional start connector on existing equipment/panel to connect to.</summary>
        public Connector? StartTerminalConnector { get; set; }

        /// <summary>Optional end connector on existing equipment/panel to connect to.</summary>
        public Connector? EndTerminalConnector { get; set; }
    }
}
