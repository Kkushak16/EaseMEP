using System;
using System.Collections.Generic;
using System.Linq;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Diagnostics
{
    /// <summary>
    /// Comprehensive failure diagnostic report providing actionable root-cause analysis,
    /// specific reason codes, and likely blocking element IDs (FR-13).
    /// </summary>
    public class FailureReport
    {
        public FailureReasonCode ReasonCode { get; }
        public string Message { get; }
        public IReadOnlyList<string> BlockingElementIds { get; }
        public string SuggestedRemedy { get; }
        public Vec3? ClosestPointReached { get; }
        public double? RemainingDistanceFeet { get; }
        public int NodesExpanded { get; }
        public double ElapsedMs { get; }
        public bool HitCorridorBoundary { get; }
        public int RetryAttempts { get; }

        public bool HasBlockingElements => BlockingElementIds.Count > 0;

        public FailureReport(
            FailureReasonCode reasonCode,
            string message,
            IReadOnlyList<string>? blockingElementIds = null,
            string? suggestedRemedy = null,
            Vec3? closestPointReached = null,
            double? remainingDistanceFeet = null,
            int nodesExpanded = 0,
            double elapsedMs = 0.0,
            bool hitCorridorBoundary = false,
            int retryAttempts = 0)
        {
            ReasonCode = reasonCode;
            Message = message ?? string.Empty;
            BlockingElementIds = blockingElementIds ?? Array.Empty<string>();
            SuggestedRemedy = suggestedRemedy ?? DefaultRemedy(reasonCode, BlockingElementIds);
            ClosestPointReached = closestPointReached;
            RemainingDistanceFeet = remainingDistanceFeet;
            NodesExpanded = nodesExpanded;
            ElapsedMs = elapsedMs;
            HitCorridorBoundary = hitCorridorBoundary;
            RetryAttempts = retryAttempts;
        }

        /// <summary>
        /// Formats a complete multi-line diagnostic string suitable for user status panels.
        /// </summary>
        public string FormattedSummary()
        {
            var lines = new List<string>
            {
                $"[Failure Code: {ReasonCode}] {Message}"
            };

            if (BlockingElementIds.Count > 0)
            {
                lines.Add($"Blocking Element IDs: {string.Join(", ", BlockingElementIds)}");
            }

            if (!string.IsNullOrWhiteSpace(SuggestedRemedy))
            {
                lines.Add($"Suggested Action: {SuggestedRemedy}");
            }

            if (ClosestPointReached.HasValue && RemainingDistanceFeet.HasValue)
            {
                lines.Add($"Search progress: Reached {ClosestPointReached.Value}, {RemainingDistanceFeet.Value:F1} ft remaining to goal.");
            }

            if (RetryAttempts > 0)
            {
                lines.Add($"Corridor widening retries attempted: {RetryAttempts}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string DefaultRemedy(FailureReasonCode code, IReadOnlyList<string> ids)
        {
            string idStr = ids.Count > 0 ? $" [{string.Join(", ", ids)}]" : string.Empty;
            switch (code)
            {
                case FailureReasonCode.StartBlocked:
                    return $"Move start connector or decrease clearance margin around obstructing element(s){idStr}.";
                case FailureReasonCode.EndBlocked:
                    return $"Move destination connector or decrease clearance margin around obstructing element(s){idStr}.";
                case FailureReasonCode.StartStubBlocked:
                    return $"Ensure straight clearance space along the start connector departure axis, or adjust obstacle{idStr}.";
                case FailureReasonCode.EndStubBlocked:
                    return $"Ensure straight clearance space along the destination connector arrival axis, or adjust obstacle{idStr}.";
                case FailureReasonCode.NoPath:
                    return $"No route found. Try increasing search corridor margin or reviewing obstructing elements{idStr}.";
                case FailureReasonCode.CorridorLimitReached:
                    return "Search reached corridor boundaries. Enable corridor widening retry or increase lateral margin.";
                case FailureReasonCode.SearchBudgetExceeded:
                    return "Search exceeded maximum node budget. Increase budget or simplify scene geometry.";
                case FailureReasonCode.Cancelled:
                    return "Operation cancelled by user.";
                case FailureReasonCode.FittingFeasibilityFailed:
                    return "Route geometry too tight for minimum straight fitting length (150 mm). Move connectors further apart.";
                case FailureReasonCode.CollisionCheckFailed:
                    return "Post-routing verification detected a collision with obstacle geometry. Increase clearance margin.";
                case FailureReasonCode.ConnectorMismatch:
                    return "Verify that start and end connectors have matching shapes and compatible MEP system types.";
                case FailureReasonCode.TransactionFailed:
                    return "Revit transaction rolled back. Check fitting family types and connector alignments.";
                default:
                    return string.Empty;
            }
        }
    }
}
