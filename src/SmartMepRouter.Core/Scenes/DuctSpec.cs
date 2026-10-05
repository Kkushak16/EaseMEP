using System;

namespace SmartMepRouter.Core.Scenes
{
    /// <summary>
    /// Specifies cross-sectional dimensions and clearance parameters for a duct run in decimal feet.
    /// </summary>
    public class DuctSpec
    {
        public double WidthFeet { get; set; } = 1.0;
        public double HeightFeet { get; set; } = 0.67;
        public double InsulationFeet { get; set; } = 0.0833; // 1 inch = 0.0833 ft (approx 25 mm)
        public double ClearanceFeet { get; set; } = 0.328;   // 100 mm = 0.328 ft

        /// <summary>
        /// Total isotropic inflation margin: clearance + insulation + duct half-extent (FR-17).
        /// </summary>
        public double TotalInflationFeet =>
            ClearanceFeet + InsulationFeet + Math.Max(WidthFeet, HeightFeet) * 0.5;

        public DuctSpec() { }

        public DuctSpec(double widthFeet, double heightFeet, double insulationFeet, double clearanceFeet)
        {
            WidthFeet = widthFeet;
            HeightFeet = heightFeet;
            InsulationFeet = insulationFeet;
            ClearanceFeet = clearanceFeet;
        }
    }
}
