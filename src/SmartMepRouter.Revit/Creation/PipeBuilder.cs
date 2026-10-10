using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Revit.Extraction;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Generates native, connected Revit pipes and elbow fittings from Core route waypoints (Phase 12).
    /// Enforces single-operation TransactionGroup commit with full rollback on any failure (Rule R-A5).
    /// </summary>
    public static class PipeBuilder
    {
        /// <summary>
        /// Creates connected pipes and elbow fittings inside a TransactionGroup.
        /// </summary>
        /// <param name="doc">The active host Revit document.</param>
        /// <param name="routePoints">Collinear-compressed, endpoint-aligned 3D route vertices in decimal feet.</param>
        /// <param name="options">Pipe types, sizes, reference level, and optional terminal connectors.</param>
        /// <returns>A <see cref="DuctCreationResult"/> with created element IDs or error explanation.</returns>
        public static DuctCreationResult CreatePipeRoute(
            Document doc,
            IReadOnlyList<Vec3> routePoints,
            PipeCreationOptions options)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (routePoints == null || routePoints.Count < 2)
            {
                return DuctCreationResult.Failed("Route requires at least 2 distinct waypoints to construct piping.");
            }
            if (options == null) throw new ArgumentNullException(nameof(options));

            // Resolve required Revit ElementIds if unspecified
            ElementId levelId = options.LevelId != ElementId.InvalidElementId
                ? options.LevelId
                : PipeSystemResolver.ResolveLevel(doc, options.StartTerminalConnector, routePoints[0].Z);

            if (levelId == ElementId.InvalidElementId)
            {
                return DuctCreationResult.Failed("Could not resolve a valid Level for pipe placement.");
            }

            ElementId systemTypeId = options.SystemTypeId != ElementId.InvalidElementId
                ? options.SystemTypeId
                : PipeSystemResolver.ResolvePipingSystemType(doc, options.StartTerminalConnector);

            if (systemTypeId == ElementId.InvalidElementId)
            {
                return DuctCreationResult.Failed("Could not resolve a valid PipingSystemType for pipework.");
            }

            ElementId pipeTypeId = options.PipeTypeId != ElementId.InvalidElementId
                ? options.PipeTypeId
                : PipeSystemResolver.ResolvePipeType(doc);

            if (pipeTypeId == ElementId.InvalidElementId)
            {
                return DuctCreationResult.Failed("Could not resolve a valid PipeType symbol in the document.");
            }

            var createdPipeIds = new List<ElementId>();
            var createdFittingIds = new List<ElementId>();
            var warnings = new List<string>();

            using (var tg = new TransactionGroup(doc, "Smart MEP Router — Generate Piping"))
            {
                tg.Start();

                try
                {
                    using (var trans = new Transaction(doc, "Create Pipes and Elbows"))
                    {
                        var failuresPreprocessor = new RoutingFailuresPreprocessor();
                        var failureHandling = trans.GetFailureHandlingOptions();
                        failureHandling.SetFailuresPreprocessor(failuresPreprocessor);
                        failureHandling.SetClearAfterRollback(true);
                        trans.SetFailureHandlingOptions(failureHandling);

                        trans.Start();

                        var createdPipes = new List<Pipe>();

                        // 1. Create straight pipe segments
                        for (int i = 0; i < routePoints.Count - 1; i++)
                        {
                            XYZ p1 = routePoints[i].ToXyz();
                            XYZ p2 = routePoints[i + 1].ToXyz();

                            if (p1.DistanceTo(p2) < 0.01)
                                continue;

                            Pipe pipe = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, p1, p2);

                            // Apply cross-sectional diameter
                            pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.Set(options.DiameterFeet);

                            createdPipes.Add(pipe);
                            createdPipeIds.Add(pipe.Id);
                        }

                        if (createdPipes.Count == 0)
                        {
                            trans.RollBack();
                            tg.RollBack();
                            return DuctCreationResult.Failed("No pipe segments could be instantiated along the route.");
                        }

                        doc.Regenerate();

                        // 2. Insert elbow fittings at corners
                        for (int i = 0; i < createdPipes.Count - 1; i++)
                        {
                            Pipe pipe1 = createdPipes[i];
                            Pipe pipe2 = createdPipes[i + 1];
                            XYZ corner = routePoints[i + 1].ToXyz();

                            Connector? c1 = GetClosestConnector(pipe1, corner);
                            Connector? c2 = GetClosestConnector(pipe2, corner);

                            if (c1 != null && c2 != null)
                            {
                                try
                                {
                                    FamilyInstance elbow = doc.Create.NewElbowFitting(c1, c2);
                                    if (elbow != null)
                                    {
                                        createdFittingIds.Add(elbow.Id);
                                    }
                                }
                                catch (Exception fittingEx)
                                {
                                    trans.RollBack();
                                    tg.RollBack();
                                    return DuctCreationResult.Failed(
                                        $"Fitting placement failed at corner {i + 1}: {fittingEx.Message}",
                                        failuresPreprocessor.ErrorDescriptions,
                                        failuresPreprocessor.FailingElementIds);
                                }
                            }
                        }

                        // 3. Connect to terminal connectors if provided
                        if (options.StartTerminalConnector != null && createdPipes.Count > 0)
                        {
                            Connector? startPipeConn = GetClosestConnector(createdPipes[0], routePoints[0].ToXyz());
                            if (startPipeConn != null && !startPipeConn.IsConnected)
                            {
                                try
                                {
                                    startPipeConn.ConnectTo(options.StartTerminalConnector);
                                }
                                catch (Exception ex)
                                {
                                    warnings.Add($"Start terminal connector could not be linked: {ex.Message}");
                                }
                            }
                        }

                        if (options.EndTerminalConnector != null && createdPipes.Count > 0)
                        {
                            Connector? endPipeConn = GetClosestConnector(createdPipes[createdPipes.Count - 1], routePoints[routePoints.Count - 1].ToXyz());
                            if (endPipeConn != null && !endPipeConn.IsConnected)
                            {
                                try
                                {
                                    endPipeConn.ConnectTo(options.EndTerminalConnector);
                                }
                                catch (Exception ex)
                                {
                                    warnings.Add($"End terminal connector could not be linked: {ex.Message}");
                                }
                            }
                        }

                        TransactionStatus status = trans.Commit();
                        if (status != TransactionStatus.Committed)
                        {
                            tg.RollBack();
                            string detail = failuresPreprocessor.ErrorDescriptions.Count > 0
                                ? string.Join("; ", failuresPreprocessor.ErrorDescriptions)
                                : "Revit transaction did not commit cleanly.";
                            return DuctCreationResult.Failed(detail, failuresPreprocessor.ErrorDescriptions, failuresPreprocessor.FailingElementIds, warnings);
                        }
                    }

                    tg.Assimilate();
                    return DuctCreationResult.Succeeded(createdPipeIds, createdFittingIds, warnings);
                }
                catch (Exception ex)
                {
                    tg.RollBack();
                    return DuctCreationResult.Failed($"Pipe generation failed: {ex.Message}");
                }
            }
        }

        private static Connector? GetClosestConnector(Pipe pipe, XYZ targetPoint)
        {
            if (pipe?.ConnectorManager == null) return null;

            Connector? closest = null;
            double minDistanceSq = double.MaxValue;

            foreach (Connector conn in pipe.ConnectorManager.Connectors)
            {
                XYZ origin = conn.Origin;
                double dx = origin.X - targetPoint.X;
                double dy = origin.Y - targetPoint.Y;
                double dz = origin.Z - targetPoint.Z;
                double distSq = dx * dx + dy * dy + dz * dz;

                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    closest = conn;
                }
            }

            return closest;
        }
    }
}
