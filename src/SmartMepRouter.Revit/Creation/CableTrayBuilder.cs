using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Revit.Extraction;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Generates native, connected Revit cable trays and elbow fittings from Core route waypoints (Phase 12).
    /// Enforces single-operation TransactionGroup commit with full rollback on any failure (Rule R-A5).
    /// </summary>
    public static class CableTrayBuilder
    {
        /// <summary>
        /// Creates connected cable trays and elbow fittings inside a TransactionGroup.
        /// </summary>
        /// <param name="doc">The active host Revit document.</param>
        /// <param name="routePoints">Collinear-compressed, endpoint-aligned 3D route vertices in decimal feet.</param>
        /// <param name="options">Cable tray types, sizes, reference level, and optional terminal connectors.</param>
        /// <returns>A <see cref="DuctCreationResult"/> with created element IDs or error explanation.</returns>
        public static DuctCreationResult CreateCableTrayRoute(
            Document doc,
            IReadOnlyList<Vec3> routePoints,
            CableTrayCreationOptions options)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (routePoints == null || routePoints.Count < 2)
            {
                return DuctCreationResult.Failed("Route requires at least 2 distinct waypoints to construct cable trays.");
            }
            if (options == null) throw new ArgumentNullException(nameof(options));

            // Resolve required Revit ElementIds if unspecified
            ElementId levelId = options.LevelId != ElementId.InvalidElementId
                ? options.LevelId
                : CableTraySystemResolver.ResolveLevel(doc, options.StartTerminalConnector, routePoints[0].Z);

            if (levelId == ElementId.InvalidElementId)
            {
                return DuctCreationResult.Failed("Could not resolve a valid Level for cable tray placement.");
            }

            ElementId trayTypeId = options.CableTrayTypeId != ElementId.InvalidElementId
                ? options.CableTrayTypeId
                : CableTraySystemResolver.ResolveCableTrayType(doc);

            if (trayTypeId == ElementId.InvalidElementId)
            {
                return DuctCreationResult.Failed("Could not resolve a valid CableTrayType symbol in the document.");
            }

            var createdTrayIds = new List<ElementId>();
            var createdFittingIds = new List<ElementId>();
            var warnings = new List<string>();

            using (var tg = new TransactionGroup(doc, "Smart MEP Router — Generate Cable Trays"))
            {
                tg.Start();

                try
                {
                    using (var trans = new Transaction(doc, "Create Cable Trays and Elbows"))
                    {
                        var failuresPreprocessor = new RoutingFailuresPreprocessor();
                        var failureHandling = trans.GetFailureHandlingOptions();
                        failureHandling.SetFailuresPreprocessor(failuresPreprocessor);
                        failureHandling.SetClearAfterRollback(true);
                        trans.SetFailureHandlingOptions(failureHandling);

                        trans.Start();

                        var createdTrays = new List<CableTray>();

                        // 1. Create straight cable tray segments
                        for (int i = 0; i < routePoints.Count - 1; i++)
                        {
                            XYZ p1 = routePoints[i].ToXyz();
                            XYZ p2 = routePoints[i + 1].ToXyz();

                            if (p1.DistanceTo(p2) < 0.01)
                                continue;

                            CableTray tray = CableTray.Create(doc, trayTypeId, p1, p2, levelId);

                            // Apply cross-sectional width and height
                            tray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM)?.Set(options.WidthFeet);
                            tray.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM)?.Set(options.HeightFeet);

                            createdTrays.Add(tray);
                            createdTrayIds.Add(tray.Id);
                        }

                        if (createdTrays.Count == 0)
                        {
                            trans.RollBack();
                            tg.RollBack();
                            return DuctCreationResult.Failed("No cable tray segments could be instantiated along the route.");
                        }

                        doc.Regenerate();

                        // 2. Insert elbow fittings at corners
                        for (int i = 0; i < createdTrays.Count - 1; i++)
                        {
                            CableTray tray1 = createdTrays[i];
                            CableTray tray2 = createdTrays[i + 1];
                            XYZ corner = routePoints[i + 1].ToXyz();

                            Connector? c1 = GetClosestConnector(tray1, corner);
                            Connector? c2 = GetClosestConnector(tray2, corner);

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
                        if (options.StartTerminalConnector != null && createdTrays.Count > 0)
                        {
                            Connector? startTrayConn = GetClosestConnector(createdTrays[0], routePoints[0].ToXyz());
                            if (startTrayConn != null && !startTrayConn.IsConnected)
                            {
                                try
                                {
                                    startTrayConn.ConnectTo(options.StartTerminalConnector);
                                }
                                catch (Exception ex)
                                {
                                    warnings.Add($"Start terminal connector could not be linked: {ex.Message}");
                                }
                            }
                        }

                        if (options.EndTerminalConnector != null && createdTrays.Count > 0)
                        {
                            Connector? endTrayConn = GetClosestConnector(createdTrays[createdTrays.Count - 1], routePoints[routePoints.Count - 1].ToXyz());
                            if (endTrayConn != null && !endTrayConn.IsConnected)
                            {
                                try
                                {
                                    endTrayConn.ConnectTo(options.EndTerminalConnector);
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
                    return DuctCreationResult.Succeeded(createdTrayIds, createdFittingIds, warnings);
                }
                catch (Exception ex)
                {
                    tg.RollBack();
                    return DuctCreationResult.Failed($"Cable tray generation failed: {ex.Message}");
                }
            }
        }

        private static Connector? GetClosestConnector(CableTray tray, XYZ targetPoint)
        {
            if (tray?.ConnectorManager == null) return null;

            Connector? closest = null;
            double minDistanceSq = double.MaxValue;

            foreach (Connector conn in tray.ConnectorManager.Connectors)
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
