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
        public double WeightVertical { get; set; } = 4.0;
        public int MinRunCells { get; set; } = 1;
        public int StartStubCells { get; set; } = 1;
        public int EndStubCells { get; set; } = 1;

        public static CostModel Default => new CostModel();

        /// <summary>
        /// Preset that heavily penalizes bends to generate routes with minimal fittings.
        /// </summary>
        public static CostModel FewerBends => new CostModel
        {
            WeightLength = 1.0,
            WeightBend = 25.0,
            WeightVertical = 10.0,
            MinRunCells = 2,
            StartStubCells = 1,
            EndStubCells = 1
        };

        /// <summary>
        /// Preset that prioritizes shortest overall length over bend count.
        /// </summary>
        public static CostModel ShortestLength => new CostModel
        {
            WeightLength = 1.0,
            WeightBend = 2.0,
            WeightVertical = 2.0,
            MinRunCells = 1,
            StartStubCells = 1,
            EndStubCells = 1
        };
    }
}
