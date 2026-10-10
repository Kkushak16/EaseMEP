using System;
using SmartMepRouter.Core.Connectors;

namespace SmartMepRouter.Core.Scenes
{
    /// <summary>
    /// Specifies cross-sectional dimensions and clearance parameters for a duct run in decimal feet.
    /// </summary>
    public class DuctSpec
    {
        public ConnectorShape Shape { get; set; } = ConnectorShape.Rectangular;
        public double WidthFeet { get; set; } = 1.0;
        public double HeightFeet { get; set; } = 0.67;
        public double DiameterFeet { get; set; } = 1.0;
        public double InsulationFeet { get; set; } = 0.0833; // 1 inch = 0.0833 ft (approx 25 mm)
        public double ClearanceFeet { get; set; } = 0.328;   // 100 mm = 0.328 ft

        public bool IsRound => Shape == ConnectorShape.Round;

        public double ClearanceAndInsulationFeet => ClearanceFeet + InsulationFeet;

        /// <summary>
        /// Total isotropic inflation margin: clearance + insulation + duct half-extent (FR-17).
        /// </summary>
        public double TotalInflationFeet
        {
            get
            {
                double halfExtent = IsRound
                    ? DiameterFeet * 0.5
                    : Math.Max(WidthFeet, HeightFeet) * 0.5;
                return ClearanceFeet + InsulationFeet + halfExtent;
            }
        }

        public DuctSpec() { }

        public DuctSpec(double widthFeet, double heightFeet, double insulationFeet, double clearanceFeet)
        {
            Shape = ConnectorShape.Rectangular;
            WidthFeet = widthFeet;
            HeightFeet = heightFeet;
            InsulationFeet = insulationFeet;
            ClearanceFeet = clearanceFeet;
        }

        public static DuctSpec Rectangular(double widthFeet, double heightFeet, double clearanceFeet = 0.328, double insulationFeet = 0.0833)
        {
            return new DuctSpec(widthFeet, heightFeet, insulationFeet, clearanceFeet);
        }

        public static DuctSpec Round(double diameterFeet, double clearanceFeet = 0.328, double insulationFeet = 0.0833)
        {
            return new DuctSpec
            {
                Shape = ConnectorShape.Round,
                DiameterFeet = diameterFeet,
                WidthFeet = diameterFeet,
                HeightFeet = diameterFeet,
                InsulationFeet = insulationFeet,
                ClearanceFeet = clearanceFeet
            };
        }
    }
}
