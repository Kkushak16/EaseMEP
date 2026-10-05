using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Result of generating native Revit ducts and elbow fittings.
    /// </summary>
    public class DuctCreationResult
    {
        public bool Success { get; }
        public IReadOnlyList<ElementId> DuctIds { get; }
        public IReadOnlyList<ElementId> FittingIds { get; }
        public string? ErrorMessage { get; }
        public int TotalElementsCreated => DuctIds.Count + FittingIds.Count;

        private DuctCreationResult(bool success, IReadOnlyList<ElementId> ductIds, IReadOnlyList<ElementId> fittingIds, string? errorMessage)
        {
            Success = success;
            DuctIds = ductIds;
            FittingIds = fittingIds;
            ErrorMessage = errorMessage;
        }

        public static DuctCreationResult Succeeded(IReadOnlyList<ElementId> ductIds, IReadOnlyList<ElementId> fittingIds)
        {
            return new DuctCreationResult(true, ductIds, fittingIds, null);
        }

        public static DuctCreationResult Failed(string errorMessage)
        {
            return new DuctCreationResult(false, new List<ElementId>(), new List<ElementId>(), errorMessage);
        }
    }
}
