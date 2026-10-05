namespace SmartMepRouter.Core.Connectors
{
    /// <summary>
    /// Supported profile geometry shapes for MEP duct/pipe connectors.
    /// </summary>
    public enum ConnectorShape
    {
        /// <summary>Rectangular cross section.</summary>
        Rectangular,

        /// <summary>Round / circular cross section.</summary>
        Round,

        /// <summary>Oval cross section.</summary>
        Oval,

        /// <summary>Unspecified or unsupported profile shape.</summary>
        Other
    }
}
