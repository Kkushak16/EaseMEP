using System;
using System.Collections.Generic;
using System.Threading;
using SmartMepRouter.Core.Diagnostics;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Configuration options for automated search corridor widening retry (Phase 8).
    /// </summary>
    public class CorridorWideningOptions
    {
        /// <summary>
        /// Maximum number of widening retries on NoPath or CorridorLimitReached. Default is 2.
        /// </summary>
        public int MaxRetries { get; set; } = 2;

        /// <summary>
        /// Expansion distance in decimal feet applied to all corridor boundary faces per retry.
        /// Default is 5.0 ft (~1500 mm).
        /// </summary>
        public double ExpansionMarginFeet { get; set; } = 5.0;

        /// <summary>
        /// If true, automatically attempts wider search corridors when initial corridor has no route.
        /// </summary>
        public bool EnableWideningOnNoPath { get; set; } = true;
    }

    /// <summary>
    /// Coordinates A* routing with automated corridor widening retries when initial search
    /// bounds are too constrained to navigate around large obstacles.
    /// </summary>
    public class CorridorWideningRouter
    {
        private readonly Router _innerRouter;
        private readonly CorridorWideningOptions _options;

        public CorridorWideningRouter(Router? innerRouter = null, CorridorWideningOptions? options = null)
        {
            _innerRouter = innerRouter ?? new Router();
            _options = options ?? new CorridorWideningOptions();
        }

        /// <summary>
        /// Attempts to route between endpoints. If the initial corridor fails with NoPath
        /// and widening is enabled, expands the corridor bounds and retries up to MaxRetries times.
        /// </summary>
        public RouteResult RouteWithWidening(
            Vec3 startPos,
            Vec3 endPos,
            IReadOnlyList<IObstacle> obstacles,
            Aabb initialCorridorBounds,
            double cellSizeFeet,
            Direction3D startDir = Direction3D.None,
            Direction3D endDir = Direction3D.None,
            bool enableDirectional = false,
            double ductWidthFeet = 0.0,
            double ductHeightFeet = 0.0,
            double clearanceAndInsulationFeet = 0.0,
            CancellationToken cancellationToken = default)
        {
            Aabb currentBounds = initialCorridorBounds;
            int retriesAttempted = 0;
            RouteResult lastResult;

            while (true)
            {
                var grid = new SparseGrid(startPos, cellSizeFeet, currentBounds, enableDirectional: enableDirectional);

                foreach (IObstacle obstacle in obstacles)
                {
                    if (enableDirectional)
                    {
                        grid.BlockObstacleDirectional(obstacle, clearanceAndInsulationFeet, ductWidthFeet, ductHeightFeet);
                    }
                    else
                    {
                        grid.BlockObstacle(obstacle);
                    }
                }

                lastResult = _innerRouter.Route(grid, startPos, endPos, startDir, endDir, cancellationToken);

                // Exit immediately on success or user cancellation
                if (lastResult.IsSuccess || lastResult.Status == RoutingStatus.Cancelled)
                {
                    return lastResult;
                }

                // If endpoints themselves are blocked, widening the corridor cannot help
                if (lastResult.Status == RoutingStatus.StartBlocked || lastResult.Status == RoutingStatus.EndBlocked)
                {
                    return lastResult;
                }

                // Check if widening retry should be attempted
                if (!_options.EnableWideningOnNoPath || retriesAttempted >= _options.MaxRetries)
                {
                    break;
                }

                // Widen corridor bounds and retry
                retriesAttempted++;
                currentBounds = currentBounds.Inflate(_options.ExpansionMarginFeet);
            }

            // If we exhausted retries, preserve the diagnostic failure report with retry count
            if (lastResult.FailureReport != null)
            {
                var updatedReport = new FailureReport(
                    lastResult.FailureReport.ReasonCode,
                    lastResult.FailureReport.Message + $" (Attempted {retriesAttempted} corridor widening retries)",
                    lastResult.FailureReport.BlockingElementIds,
                    lastResult.FailureReport.SuggestedRemedy,
                    lastResult.FailureReport.ClosestPointReached,
                    lastResult.FailureReport.RemainingDistanceFeet,
                    lastResult.NodesExpanded,
                    lastResult.SearchTimeMs,
                    lastResult.FailureReport.HitCorridorBoundary,
                    retriesAttempted);

                return RouteResult.FromFailureReport(lastResult.Status, updatedReport, lastResult.NodesExpanded, lastResult.SearchTimeMs);
            }

            return lastResult;
        }
    }
}
