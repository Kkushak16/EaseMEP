namespace SmartMepRouter.Core.Geometry
{
    /// <summary>
    /// Utility for conversions between metric display units (millimeters) and Revit internal units (decimal feet).
    /// Revit internal coordinate frame is right-handed decimal feet.
    /// </summary>
    public static class Units
    {
        /// <summary>
        /// Exact factor for feet to millimeters conversion (1 foot = 304.8 mm).
        /// </summary>
        public const double MmPerFoot = 304.8;

        /// <summary>
        /// Converts decimal feet to millimeters.
        /// </summary>
        /// <param name="feet">Distance in decimal feet.</param>
        /// <returns>Distance in millimeters.</returns>
        public static double FeetToMm(double feet) => feet * MmPerFoot;

        /// <summary>
        /// Converts millimeters to decimal feet.
        /// </summary>
        /// <param name="mm">Distance in millimeters.</param>
        /// <returns>Distance in decimal feet.</returns>
        public static double MmToFeet(double mm) => mm / MmPerFoot;
    }
}
