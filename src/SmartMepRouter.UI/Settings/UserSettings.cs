using System.Runtime.Serialization;

namespace SmartMepRouter.UI.Settings
{
    /// <summary>Dialog settings persisted between sessions (FR-15). Distances are millimetres (UI boundary).</summary>
    [DataContract]
    public class UserSettings
    {
        [DataMember] public bool IncludeColumns { get; set; } = true;
        [DataMember] public bool IncludeFraming { get; set; } = true;
        [DataMember] public bool IncludeWalls { get; set; } = true;
        [DataMember] public bool IncludeCeilings { get; set; }
        [DataMember] public double ClearanceMarginMm { get; set; } = 100.0;
        [DataMember] public string Optimization { get; set; } = "Low Fitting Count (Fewer Bends)";
        /// <summary>"Dark" or "Light".</summary>
        [DataMember] public string Theme { get; set; } = "Dark";
        [DataMember] public bool IsRectangular { get; set; } = true;
        [DataMember] public string Discipline { get; set; } = "Duct";
    }
}
