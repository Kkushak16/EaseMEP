namespace SmartMepRouter.Core.Connectors
{
    /// <summary>
    /// Supported MEP disciplines for automated routing and element generation (Phase 12).
    /// </summary>
    public enum MepDiscipline
    {
        /// <summary>HVAC ductwork (rectangular, round, or oval).</summary>
        Duct,

        /// <summary>Plumbing and mechanical piping (round, hydronic or domestic).</summary>
        Pipe,

        /// <summary>Electrical cable containment (rectangular channel or ladder tray).</summary>
        CableTray
    }
}
