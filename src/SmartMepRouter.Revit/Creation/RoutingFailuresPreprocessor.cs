using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Failure preprocessor that dismisses benign warnings (such as system connectivity notices)
    /// during duct and fitting creation, while cleanly aborting on fatal errors.
    /// </summary>
    public class RoutingFailuresPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failureMessages = failuresAccessor.GetFailureMessages();

            foreach (FailureMessageAccessor failure in failureMessages)
            {
                FailureSeverity severity = failure.GetSeverity();

                if (severity == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
                else if (severity == FailureSeverity.Error)
                {
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
