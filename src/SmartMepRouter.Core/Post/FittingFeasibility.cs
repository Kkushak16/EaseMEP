using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Post
{
    /// <summary>
    /// Post-processing safety verification for fabrication and fitting feasibility (FR-16).
    /// Verifies that every rectilinear segment has sufficient length to accommodate Revit fittings.
    /// Uses Revit's ShortCurveTolerance (approx 0.0026 ft) as the default geometric floor.
    /// </summary>
    public static class FittingFeasibility
    {
        public const double DefaultShortCurveToleranceFeet = 0.0026;

        /// <summary>
        /// Validates that all segments in the corner path satisfy minimum straight length constraints.
        /// </summary>
        /// <param name="corners">Compressed corner vertices in decimal feet.</param>
        /// <param name="minStraightLengthFeet">Required straight length between fittings in decimal feet.</param>
        /// <param name="toleranceFeet">Geometric tolerance below which Revit rejects curve creation.</param>
        /// <returns>A <see cref="FittingFeasibilityResult"/> with diagnostics.</returns>
        public static FittingFeasibilityResult Verify(
            IReadOnlyList<Vec3> corners,
            double minStraightLengthFeet,
            double toleranceFeet = DefaultShortCurveToleranceFeet)
        {
            if (corners == null || corners.Count < 2)
            {
                return new FittingFeasibilityResult(true, double.MaxValue, new List<string>());
            }

            var violations = new List<string>();
            double shortestLength = double.MaxValue;
            double effectiveMin = Math.Max(toleranceFeet, minStraightLengthFeet);

            for (int i = 0; i < corners.Count - 1; i++)
            {
                Vec3 p1 = corners[i];
                Vec3 p2 = corners[i + 1];
                double length = (p2 - p1).Length();

                if (length < shortestLength)
                {
                    shortestLength = length;
                }

                // Intermediate segments connect two elbow fittings, requiring minStraightLength
                // First and last segments connect to terminal connectors and elbows
                if (length < effectiveMin - toleranceFeet)
                {
                    violations.Add($"Segment {i} from ({p1.X:F2}, {p1.Y:F2}, {p1.Z:F2}) to ({p2.X:F2}, {p2.Y:F2}, {p2.Z:F2}) is {length:F3} ft, below required {effectiveMin:F3} ft.");
                }
            }

            bool isFeasible = violations.Count == 0;
            return new FittingFeasibilityResult(isFeasible, shortestLength, violations);
        }

        /// <summary>
        /// Validates that all elbow angles in the polyline adhere to standard HVAC duct fabrication angles
        /// (strictly 90°, or 45° and 90° when 45-degree bends are permitted) within a 2.5° tolerance.
        /// </summary>
        public static bool ValidateElbowAngles(
            IReadOnlyList<Vec3> corners,
            bool allow45Degree,
            out IReadOnlyList<string> angleViolations)
        {
            var violations = new List<string>();
            if (corners == null || corners.Count < 3)
            {
                angleViolations = violations;
                return true;
            }

            for (int i = 1; i < corners.Count - 1; i++)
            {
                Vec3 v1 = (corners[i] - corners[i - 1]).Normalize();
                Vec3 v2 = (corners[i + 1] - corners[i]).Normalize();
                double dot = Math.Max(-1.0, Math.Min(1.0, v1.Dot(v2)));
                double angleDeg = Math.Acos(dot) * (180.0 / Math.PI);

                bool is90 = Math.Abs(angleDeg - 90.0) < 2.5;
                bool is45 = allow45Degree && Math.Abs(angleDeg - 45.0) < 2.5;

                if (!is90 && !is45)
                {
                    violations.Add($"Elbow at vertex {i} ({corners[i].X:F2}, {corners[i].Y:F2}, {corners[i].Z:F2}) has non-standard angle {angleDeg:F1}°.");
                }
            }

            angleViolations = violations;
            return violations.Count == 0;
        }
    }

    /// <summary>
    /// Feasibility report for ductwork fitting placement.
    /// </summary>
    public class FittingFeasibilityResult
    {
        public bool IsFeasible { get; }
        public double ShortestSegmentLengthFeet { get; }
        public IReadOnlyList<string> Violations { get; }

        public FittingFeasibilityResult(bool isFeasible, double shortestSegmentLengthFeet, IReadOnlyList<string> violations)
        {
            IsFeasible = isFeasible;
            ShortestSegmentLengthFeet = shortestSegmentLengthFeet;
            Violations = violations ?? Array.Empty<string>();
        }
    }
}
