using System;
using SmartMepRouter.Core.Connectors;

namespace SmartMepRouter.Core.Scenes
{
    /// <summary>
    /// Specifies cross-sectional dimensions and clearance parameters for an electrical cable tray run in decimal feet (Phase 12).
    /// </summary>
    public class CableTraySpec
    {
        public double WidthFeet { get; set; } = 1.0;  // Default 300 mm = ~1 ft
        public double HeightFeet { get; set; } = 0.33; // Default 100 mm = ~0.33 ft
        public double ClearanceFeet { get; set; } = 0.25; // 75 mm clearance for tray access

        /// <summary>
        /// Total isotropic inflation margin: clearance + half max extent.
        /// </summary>
        public double TotalInflationFeet => ClearanceFeet + (Math.Max(WidthFeet, HeightFeet) * 0.5);

        public CableTraySpec() { }

        public CableTraySpec(double widthFeet, double heightFeet, double clearanceFeet = 0.25)
        {
            WidthFeet = widthFeet;
            HeightFeet = heightFeet;
            ClearanceFeet = clearanceFeet;
        }

        public DuctSpec ToDuctSpec()
        {
            return DuctSpec.Rectangular(WidthFeet, HeightFeet, ClearanceFeet, insulationFeet: 0.0);
        }
    }
}
