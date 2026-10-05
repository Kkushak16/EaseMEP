using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Metrics;
using SmartMepRouter.Core.Scenes;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Bench
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("        SmartMEP Router — Phase 2 Benchmark Suite & Comparative Analysis         ");
            Console.WriteLine("================================================================================");

            const int sceneCount = 50;
            Console.WriteLine($"[1/3] Generating {sceneCount} synthetic benchmark scenes (columns, OBBs, walls, beams)...");
            var suite = SceneGenerator.GenerateBenchmarkSuite(sceneCount, startSeed: 5000);

            var smartRouter = new Router(CostModel.FewerBends);
            var baselineRouter = new BaselineRouter();

            int smartSolved = 0;
            int baselineSolved = 0;
            double smartTotalLength = 0;
            double baselineTotalLength = 0;
            int smartTotalBends = 0;
            int baselineTotalBends = 0;
            double smartTotalTimeMs = 0;
            double baselineTotalTimeMs = 0;
            int totalCollisions = 0;

            var metricsList = new List<RunMetrics>();

            Console.WriteLine($"[2/3] Executing routing benchmark across {sceneCount} scenes...");

            for (int i = 0; i < suite.Count; i++)
            {
                var scene = suite[i];
                double cellSize = 1.0; // 1 foot discrete cells

                // Inflate obstacles
                double inflation = scene.Duct.TotalInflationFeet;
                var inflated = new List<IObstacle>();
                foreach (var obs in scene.Obstacles)
                {
                    inflated.Add(obs.Inflate(inflation));
                }

                // Grid setup
                var grid = new SparseGrid(scene.StartPos, cellSize, scene.CorridorBounds);
                foreach (var obs in inflated)
                {
                    grid.BlockObstacle(obs);
                }

                // Run SmartMEP Router
                var sw = Stopwatch.StartNew();
                RouteResult smartRes = smartRouter.Route(grid, scene.StartPos, scene.EndPos, scene.StartDir, scene.EndDir);
                sw.Stop();

                // Run Baseline (Plain A*)
                RouteResult baseRes = baselineRouter.RoutePlainAStar(grid, scene.StartPos, scene.EndPos);

                int collisionsOnScene = 0;
                if (smartRes.IsSuccess)
                {
                    smartSolved++;
                    smartTotalLength += smartRes.LengthFeet;
                    smartTotalBends += smartRes.BendCount;
                    smartTotalTimeMs += smartRes.SearchTimeMs;

                    var verification = CollisionVerifier.Verify(smartRes.Points, inflated);
                    if (verification.HasCollisions)
                    {
                        collisionsOnScene += verification.CollidingObstacleIds.Count;
                        totalCollisions += collisionsOnScene;
                    }
                }

                if (baseRes.IsSuccess)
                {
                    baselineSolved++;
                    baselineTotalLength += baseRes.LengthFeet;
                    baselineTotalBends += baseRes.BendCount;
                    baselineTotalTimeMs += baseRes.SearchTimeMs;
                }

                metricsList.Add(new RunMetrics
                {
                    SceneId = scene.Id,
                    Algorithm = "SmartMEP_FewerBends",
                    CellSizeFeet = cellSize,
                    BlockedCells = grid.BlockedCount,
                    NodesExpanded = smartRes.NodesExpanded,
                    PathLengthFeet = smartRes.LengthFeet,
                    BendCount = smartRes.BendCount,
                    SegmentCount = smartRes.SegmentCount,
                    Success = smartRes.IsSuccess,
                    Status = smartRes.Status,
                    CollisionCount = collisionsOnScene,
                    SearchTimeMs = smartRes.SearchTimeMs,
                    TotalTimeMs = sw.Elapsed.TotalMilliseconds
                });
            }

            Console.WriteLine("[3/3] Benchmark execution completed!");
            Console.WriteLine();
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine(" METRIC                           | SmartMEP (Fabrication A*) | Plain A* Baseline ");
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Scenes Evaluated                | {sceneCount,25} | {sceneCount,17} ");
            Console.WriteLine($" Solved Routes                   | {smartSolved,15} ({smartSolved * 100.0 / sceneCount,4:F0}%) | {baselineSolved,10} ({baselineSolved * 100.0 / sceneCount,4:F0}%) ");
            Console.WriteLine($" Total Collisions (Verifier)     | {totalCollisions,25} |               N/A ");
            Console.WriteLine($" Avg Path Length (ft)            | {(smartSolved > 0 ? smartTotalLength / smartSolved : 0),25:F2} | {(baselineSolved > 0 ? baselineTotalLength / baselineSolved : 0),17:F2} ");
            Console.WriteLine($" Avg Path Length (mm)            | {(smartSolved > 0 ? Units.FeetToMm(smartTotalLength / smartSolved) : 0),25:F0} | {(baselineSolved > 0 ? Units.FeetToMm(baselineTotalLength / baselineSolved) : 0),17:F0} ");
            Console.WriteLine($" Avg Bend Count (90° Fittings)   | {(smartSolved > 0 ? (double)smartTotalBends / smartSolved : 0),25:F2} | {(baselineSolved > 0 ? (double)baselineTotalBends / baselineSolved : 0),17:F2} ");
            Console.WriteLine($" Avg Search Time (ms)            | {(smartSolved > 0 ? smartTotalTimeMs / smartSolved : 0),25:F2} | {(baselineSolved > 0 ? baselineTotalTimeMs / baselineSolved : 0),17:F2} ");
            Console.WriteLine("--------------------------------------------------------------------------------");

            // Write sample run metrics JSON to current directory
            string samplePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample_benchmark_run.json");
            if (metricsList.Count > 0)
            {
                MetricsWriter.WriteToFile(metricsList[0], samplePath);
                Console.WriteLine($"Sample run metrics JSON written to: {samplePath}");
            }
            Console.WriteLine("================================================================================");
        }
    }
}
