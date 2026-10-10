using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Diagnostics;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Represents the comprehensive outcome of a routing search.
    /// All coordinates and lengths are in Revit internal decimal feet.
    /// </summary>
    public class RouteResult
    {
        public RoutingStatus Status { get; }
        public IReadOnlyList<Vec3> Points { get; }
        public double LengthFeet { get; }
        public int BendCount { get; }
        public int SegmentCount { get; }
        public int NodesExpanded { get; }
        public double SearchTimeMs { get; }
        public string Message { get; }
        public FailureReport? FailureReport { get; }

        public bool IsSuccess => Status == RoutingStatus.Success;
        public IReadOnlyList<string> BlockingElementIds => FailureReport?.BlockingElementIds ?? Array.Empty<string>();

        public RouteResult(
            RoutingStatus status,
            IReadOnlyList<Vec3>? points = null,
            double lengthFeet = 0.0,
            int bendCount = 0,
            int nodesExpanded = 0,
            double searchTimeMs = 0.0,
            string? message = null,
            FailureReport? failureReport = null)
        {
            Status = status;
            Points = points ?? Array.Empty<Vec3>();
            LengthFeet = lengthFeet;
            BendCount = bendCount;
            SegmentCount = Points.Count > 1 ? Points.Count - 1 : 0;
            NodesExpanded = nodesExpanded;
            SearchTimeMs = searchTimeMs;
            FailureReport = failureReport;
            Message = message ?? failureReport?.Message ?? DefaultMessageForStatus(status);
        }

        public static RouteResult Failure(
            RoutingStatus status,
            string? message = null,
            int nodesExpanded = 0,
            double searchTimeMs = 0.0,
            FailureReport? failureReport = null)
        {
            return new RouteResult(status, null, 0.0, 0, nodesExpanded, searchTimeMs, message, failureReport);
        }

        public static RouteResult FromFailureReport(
            RoutingStatus status,
            FailureReport failureReport,
            int nodesExpanded = 0,
            double searchTimeMs = 0.0)
        {
            return new RouteResult(status, null, 0.0, 0, nodesExpanded, searchTimeMs, failureReport.Message, failureReport);
        }

        private static string DefaultMessageForStatus(RoutingStatus status)
        {
            switch (status)
            {
                case RoutingStatus.Success:
                    return "Collision-free route successfully computed.";
                case RoutingStatus.NoPath:
                    return "No valid route could be found within the search corridor.";
                case RoutingStatus.StartBlocked:
                    return "The starting connector or point is inside an obstacle or clearance zone.";
                case RoutingStatus.EndBlocked:
                    return "The destination connector or point is inside an obstacle or clearance zone.";
                case RoutingStatus.ConnectorMismatch:
                    return "The connectors have incompatible MEP system types or shapes.";
                case RoutingStatus.Cancelled:
                    return "The routing operation was cancelled by the user.";
                case RoutingStatus.FittingFailed:
                    return "Revit failed to generate one or more fittings along the computed route.";
                case RoutingStatus.TransactionFailed:
                    return "Revit transaction failed and model changes were rolled back.";
                case RoutingStatus.UnsupportedGeometry:
                    return "Encountered geometry not supported by the current routing engine.";
                default:
                    return "Routing operation ended with status: " + status;
            }
        }
    }
}
