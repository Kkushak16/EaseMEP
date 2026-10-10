using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI.Selection;

namespace SmartMepRouter.Revit.Controller
{
    /// <summary>Allows picking only elements that expose MEP connectors (ducts, equipment, terminals).</summary>
    public sealed class ConnectorPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is MEPCurve) return true;
            return elem is FamilyInstance fi && fi.MEPModel?.ConnectorManager != null;
        }

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
