using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using CoreUnits = SmartMepRouter.Core.Geometry.Units;

namespace SmartMepRouter.Revit.Extraction
{
    /// <summary>
    /// Configuration options for obstacle extraction from host and linked Revit documents.
    /// All distance and size values are maintained in Revit internal decimal feet.
    /// </summary>
    public class ExtractionOptions
    {
        /// <summary>Include structural columns (OST_StructuralColumns).</summary>
        public bool IncludeStructuralColumns { get; set; } = true;

        /// <summary>Include structural framing / beams (OST_StructuralFraming).</summary>
        public bool IncludeStructuralFraming { get; set; } = true;

        /// <summary>Include walls (OST_Walls).</summary>
        public bool IncludeWalls { get; set; } = true;

        /// <summary>Include floors (OST_Floors).</summary>
        public bool IncludeFloors { get; set; } = true;

        /// <summary>Include existing ducts (OST_DuctCurves).</summary>
        public bool IncludeDucts { get; set; } = true;

        /// <summary>Include existing pipes (OST_PipeCurves).</summary>
        public bool IncludePipes { get; set; } = true;

        /// <summary>Include ceilings (OST_Ceilings, optional per FR-04).</summary>
        public bool IncludeCeilings { get; set; } = false;

        /// <summary>Include elements from linked Revit models (FR-05, default on).</summary>
        public bool IncludeLinkedModels { get; set; } = true;

        /// <summary>Host elements never extracted as obstacles, e.g. the owners of the picked start/end connectors.</summary>
        public ISet<ElementId> ExcludedElementIds { get; } = new HashSet<ElementId>();

        /// <summary>Search corridor lateral margin in decimal feet (default 500 mm).</summary>
        public double CorridorMarginFeet { get; set; } = CoreUnits.MmToFeet(500.0);

        /// <summary>User clearance margin in decimal feet (FR-06, default 50 mm).</summary>
        public double ClearanceFeet { get; set; } = CoreUnits.MmToFeet(50.0);

        /// <summary>Duct external insulation thickness in decimal feet (default 25 mm).</summary>
        public double InsulationFeet { get; set; } = CoreUnits.MmToFeet(25.0);

        /// <summary>Target duct cross-sectional width in decimal feet (default 300 mm).</summary>
        public double DuctWidthFeet { get; set; } = CoreUnits.MmToFeet(300.0);

        /// <summary>Target duct cross-sectional height in decimal feet (default 200 mm).</summary>
        public double DuctHeightFeet { get; set; } = CoreUnits.MmToFeet(200.0);

        /// <summary>Optional override for minimum corridor elevation (Z) in decimal feet.</summary>
        public double? PlenumMinZFeet { get; set; }

        /// <summary>Optional override for maximum corridor elevation (Z) in decimal feet.</summary>
        public double? PlenumMaxZFeet { get; set; }

        /// <summary>Target duct shape profile (Rectangular or Round).</summary>
        public SmartMepRouter.Core.Connectors.ConnectorShape Shape { get; set; } = SmartMepRouter.Core.Connectors.ConnectorShape.Rectangular;

        /// <summary>Target duct cross-sectional diameter in decimal feet (for round ductwork, default 300 mm).</summary>
        public double DiameterFeet { get; set; } = CoreUnits.MmToFeet(300.0);

        /// <summary>
        /// Computes the total isotropic inflation margin per FR-17:
        /// clearance + insulation + duct half-extent.
        /// </summary>
        public double ComputeTotalInflationFeet()
        {
            double ductHalfExtent = Shape == SmartMepRouter.Core.Connectors.ConnectorShape.Round
                ? DiameterFeet * 0.5
                : Math.Max(DuctWidthFeet, DuctHeightFeet) * 0.5;
            return ClearanceFeet + InsulationFeet + ductHalfExtent;
        }

        /// <summary>
        /// Returns the collection of selected BuiltInCategory identifiers.
        /// </summary>
        public ICollection<BuiltInCategory> GetSelectedCategories()
        {
            var categories = new List<BuiltInCategory>();

            if (IncludeStructuralColumns)
                categories.Add(BuiltInCategory.OST_StructuralColumns);
            if (IncludeStructuralFraming)
                categories.Add(BuiltInCategory.OST_StructuralFraming);
            if (IncludeWalls)
                categories.Add(BuiltInCategory.OST_Walls);
            if (IncludeFloors)
                categories.Add(BuiltInCategory.OST_Floors);
            if (IncludeDucts)
                categories.Add(BuiltInCategory.OST_DuctCurves);
            if (IncludePipes)
                categories.Add(BuiltInCategory.OST_PipeCurves);
            if (IncludeCeilings)
                categories.Add(BuiltInCategory.OST_Ceilings);

            return categories;
        }
    }
}
