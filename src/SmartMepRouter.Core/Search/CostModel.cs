using System;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// Configures the objective cost weights and constraints for the 3D A* router.
    /// Total cost g = wLength * length + wBend * bends + wVertical * verticalBends.
    /// </summary>
    public class CostModel
    {
        public double WeightLength { get; set; } = 1.0;
        public double WeightBend { get; set; } = 8.0;
        public double WeightBend45 { get; set; } = 4.0;
        public double WeightVertical { get; set; } = 4.0;
        public bool Allow45DegreeBends { get; set; } = false;
        public int MinRunCells { get; set; } = 1;
        public int StartStubCells { get; set; } = 1;
        public int EndStubCells { get; set; } = 1;

        /// <summary>
        /// Maximum number of search nodes to expand before aborting with search budget exceeded (FR-13).
        /// Default is 150,000 nodes.
        /// </summary>
        public int MaxNodesToExpand { get; set; } = 150000;

        /// <summary>
        /// Optional physical minimum straight length between fittings in decimal feet (FR-16).
        /// When set greater than 0, <see cref="ApplyCellSize"/> calibrates <see cref="MinRunCells"/>.
        /// </summary>
        public double MinStraightLengthFeet { get; set; } = 0.0;

        /// <summary>
        /// Optional physical straight departure stub length in decimal feet (FR-18).
        /// </summary>
        public double StartStubLengthFeet { get; set; } = 0.0;

        /// <summary>
        /// Optional physical straight arrival stub length in decimal feet (FR-18).
        /// </summary>
        public double EndStubLengthFeet { get; set; } = 0.0;

        /// <summary>
        /// Calibrates cell run constraints based on the grid cell size in decimal feet.
        /// </summary>
        public void ApplyCellSize(double cellSizeFeet)
        {
            if (cellSizeFeet <= 0) return;

            if (MinStraightLengthFeet > 0)
            {
                MinRunCells = Math.Max(1, (int)Math.Ceiling(MinStraightLengthFeet / cellSizeFeet));
            }

            if (StartStubLengthFeet > 0)
            {
                StartStubCells = Math.Max(1, (int)Math.Ceiling(StartStubLengthFeet / cellSizeFeet));
            }

            if (EndStubLengthFeet > 0)
            {
                EndStubCells = Math.Max(1, (int)Math.Ceiling(EndStubLengthFeet / cellSizeFeet));
            }
        }

        public static CostModel Default => new CostModel();

        /// <summary>
        /// Preset that heavily penalizes bends to generate routes with minimal fittings (FR-09).
        /// </summary>
        public static CostModel FewerBends => new CostModel
        {
            WeightLength = 1.0,
            WeightBend = 25.0,
            WeightBend45 = 12.0,
            WeightVertical = 10.0,
            MinRunCells = 2,
            StartStubCells = 1,
            EndStubCells = 1
        };

        /// <summary>
        /// Preset that prioritizes shortest overall length over bend count (FR-09).
        /// </summary>
        public static CostModel ShortestLength => new CostModel
        {
            WeightLength = 1.0,
            WeightBend = 1.5,
            WeightBend45 = 0.75,
            WeightVertical = 1.5,
            MinRunCells = 1,
            StartStubCells = 1,
            EndStubCells = 1
        };

        /// <summary>
        /// Creates an independent copy of this cost model configuration.
        /// </summary>
        public CostModel Clone()
        {
            return new CostModel
            {
                WeightLength = WeightLength,
                WeightBend = WeightBend,
                WeightBend45 = WeightBend45,
                WeightVertical = WeightVertical,
                Allow45DegreeBends = Allow45DegreeBends,
                MinRunCells = MinRunCells,
                StartStubCells = StartStubCells,
                EndStubCells = EndStubCells,
                MaxNodesToExpand = MaxNodesToExpand,
                MinStraightLengthFeet = MinStraightLengthFeet,
                StartStubLengthFeet = StartStubLengthFeet,
                EndStubLengthFeet = EndStubLengthFeet
            };
        }
    }
}
