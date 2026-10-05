namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Error taxonomy and result codes for MEP routing operations.
    /// Maps to user-facing messages in accordance with architecture specifications.
    /// </summary>
    public enum RoutingStatus
    {
        Success = 0,
        NoPath = 1,
        StartBlocked = 2,
        EndBlocked = 3,
        ConnectorMismatch = 4,
        Cancelled = 5,
        FittingFailed = 6,
        TransactionFailed = 7,
        UnsupportedGeometry = 8
    }
}
