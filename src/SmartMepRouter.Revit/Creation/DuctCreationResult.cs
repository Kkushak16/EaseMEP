using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Result of generating native Revit ducts and elbow fittings with failure diagnostics (Phase 8).
    /// </summary>
    public class DuctCreationResult
    {
        public bool Success { get; }
        public IReadOnlyList<ElementId> DuctIds { get; }
        public IReadOnlyList<ElementId> FittingIds { get; }
        public string? ErrorMessage { get; }
        public IReadOnlyList<string> ErrorDescriptions { get; }
        public IReadOnlyList<ElementId> FailingElementIds { get; }
        public IReadOnlyList<string> Warnings { get; }
        public int TotalElementsCreated => DuctIds.Count + FittingIds.Count;

        private DuctCreationResult(
            bool success,
            IReadOnlyList<ElementId> ductIds,
            IReadOnlyList<ElementId> fittingIds,
            string? errorMessage,
            IReadOnlyList<string>? errorDescriptions = null,
            IReadOnlyList<ElementId>? failingElementIds = null,
            IReadOnlyList<string>? warnings = null)
        {
            Success = success;
            DuctIds = ductIds;
            FittingIds = fittingIds;
            ErrorMessage = errorMessage;
            ErrorDescriptions = errorDescriptions ?? Array.Empty<string>();
            FailingElementIds = failingElementIds ?? Array.Empty<ElementId>();
            Warnings = warnings ?? Array.Empty<string>();
        }

        public static DuctCreationResult Succeeded(
            IReadOnlyList<ElementId> ductIds,
            IReadOnlyList<ElementId> fittingIds,
            IReadOnlyList<string>? warnings = null)
        {
            return new DuctCreationResult(true, ductIds, fittingIds, null, warnings: warnings);
        }

        public static DuctCreationResult Failed(
            string errorMessage,
            IReadOnlyList<string>? errorDescriptions = null,
            IReadOnlyList<ElementId>? failingElementIds = null,
            IReadOnlyList<string>? warnings = null)
        {
            return new DuctCreationResult(
                false,
                new List<ElementId>(),
                new List<ElementId>(),
                errorMessage,
                errorDescriptions,
                failingElementIds,
                warnings);
        }
    }
}
