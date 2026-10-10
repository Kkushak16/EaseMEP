using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Failure preprocessor that dismisses benign warnings (such as system connectivity notices)
    /// during duct and fitting creation, while capturing diagnostic details on fatal errors (Phase 8).
    /// </summary>
    public class RoutingFailuresPreprocessor : IFailuresPreprocessor
    {
        public List<string> ErrorDescriptions { get; } = new List<string>();
        public List<string> DismissedWarnings { get; } = new List<string>();
        public List<ElementId> FailingElementIds { get; } = new List<ElementId>();

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failureMessages = failuresAccessor.GetFailureMessages();

            foreach (FailureMessageAccessor failure in failureMessages)
            {
                FailureSeverity severity = failure.GetSeverity();
                string description = failure.GetDescriptionText();

                if (severity == FailureSeverity.Warning)
                {
                    DismissedWarnings.Add(description);
                    failuresAccessor.DeleteWarning(failure);
                }
                else if (severity == FailureSeverity.Error)
                {
                    ErrorDescriptions.Add(description);
                    foreach (ElementId id in failure.GetFailingElementIds())
                    {
                        if (!FailingElementIds.Contains(id))
                        {
                            FailingElementIds.Add(id);
                        }
                    }

                    if (failure.HasResolutions())
                    {
                        failuresAccessor.ResolveFailure(failure);
                    }
                    else
                    {
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                }
            }

            return FailureProcessingResult.Continue;
        }
    }
}
