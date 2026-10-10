using System;
using SmartMepRouter.Core.Connectors;

namespace SmartMepRouter.Core.Scenes
{
    /// <summary>
    /// Specifies cross-sectional dimensions and clearance parameters for a pipe run in decimal feet (Phase 12).
    /// </summary>
    public class PipeSpec
    {
        public double DiameterFeet { get; set; } = 0.25; // Default 3" (approx 75 mm)
        public double InsulationFeet { get; set; } = 0.0416; // 0.5 inch (approx 12.5 mm)
        public double ClearanceFeet { get; set; } = 0.164;   // 50 mm = 0.164 ft

        public double ClearanceAndInsulationFeet => ClearanceFeet + InsulationFeet;

        /// <summary>
        /// Total isotropic inflation margin: clearance + insulation + pipe outer radius.
        /// </summary>
        public double TotalInflationFeet => ClearanceFeet + InsulationFeet + (DiameterFeet * 0.5);

        public PipeSpec() { }

        public PipeSpec(double diameterFeet, double clearanceFeet = 0.164, double insulationFeet = 0.0416)
        {
            DiameterFeet = diameterFeet;
            ClearanceFeet = clearanceFeet;
            InsulationFeet = insulationFeet;
        }

        public DuctSpec ToDuctSpec()
        {
            return DuctSpec.Round(DiameterFeet, ClearanceFeet, InsulationFeet);
        }
    }
}
