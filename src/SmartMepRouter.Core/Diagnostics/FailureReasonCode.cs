namespace SmartMepRouter.Core.Diagnostics
{
    /// <summary>
    /// Granular reason codes diagnosing why a routing or duct creation operation failed (FR-13).
    /// </summary>
    public enum FailureReasonCode
    {
        None = 0,

        /// <summary>
        /// Start connector or point is inside an obstacle or clearance boundary.
        /// </summary>
        StartBlocked = 1,

        /// <summary>
        /// Destination connector or point is inside an obstacle or clearance boundary.
        /// </summary>
        EndBlocked = 2,

        /// <summary>
        /// Departure stub leg along start connector direction collides with an obstacle.
        /// </summary>
        StartStubBlocked = 3,

        /// <summary>
        /// Arrival stub leg along end connector reverse direction collides with an obstacle.
        /// </summary>
        EndStubBlocked = 4,

        /// <summary>
        /// No collision-free path exists between endpoints within the active search corridor.
        /// </summary>
        NoPath = 5,

        /// <summary>
        /// Search reached the boundary of the corridor without reaching the destination.
        /// </summary>
        CorridorLimitReached = 6,

        /// <summary>
        /// Search node budget was exceeded without reaching destination.
        /// </summary>
        SearchBudgetExceeded = 7,

        /// <summary>
        /// Search was cancelled by user via CancellationToken.
        /// </summary>
        Cancelled = 8,

        /// <summary>
        /// Computed route violates minimum straight length or fitting placement criteria.
        /// </summary>
        FittingFeasibilityFailed = 9,

        /// <summary>
        /// Independent post-search collision verification detected a collision.
        /// </summary>
        CollisionCheckFailed = 10,

        /// <summary>
        /// Connectors have incompatible shapes, system types, or dimensions.
        /// </summary>
        ConnectorMismatch = 11,

        /// <summary>
        /// Revit API transaction failed and was rolled back cleanly.
        /// </summary>
        TransactionFailed = 12,

        /// <summary>
        /// Geometry or configuration is unsupported by the routing engine.
        /// </summary>
        UnsupportedGeometry = 13
    }
}
