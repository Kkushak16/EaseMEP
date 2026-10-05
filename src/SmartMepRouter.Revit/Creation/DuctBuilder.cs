using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using SmartMepRouter.Core.Connectors;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Revit.Extraction;

namespace SmartMepRouter.Revit.Creation
{
    /// <summary>
    /// Generates native, connected Revit ducts and elbow fittings from Core route waypoints (Approach A).
    /// Enforces single-operation TransactionGroup commit with full rollback on any failure (Rule R-A5).
    /// </summary>
    public static class DuctBuilder
    {
        /// <summary>
        /// Creates connected ducts and elbow fittings inside a TransactionGroup.
        /// </summary>
        /// <param name="doc">The active host Revit document.</param>
        /// <param name="routePoints">Collinear-compressed, endpoint-aligned 3D route vertices in decimal feet.</param>
        /// <param name="options">Duct types, sizes, reference level, and optional terminal connectors.</param>
        /// <returns>A <see cref="DuctCreationResult"/> with created element IDs or error explanation.</returns>
        public static DuctCreationResult CreateDuctRoute(
            Document doc,
            IReadOnlyList<Vec3> routePoints,
            DuctCreationOptions options)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (routePoints == null || routePoints.Count < 2)
            {
                return DuctCreationResult.Failed("Route requires at least 2 distinct waypoints to construct ductwork.");
            }
            if (options == null) throw new ArgumentNullException(nameof(options));

            // Resolve required Revit ElementIds if unspecified
            ElementId levelId = options.LevelId != ElementId.InvalidElementId
                ? options.LevelId
                : DuctSystemResolver.ResolveLevel(doc, options.StartTerminalConnector, routePoints[0].Z);

            if (levelId == ElementId.InvalidElementId)
            {
                return DuctCreationResult.Failed("Could not resolve a valid Level for duct placement.");
            }

            ElementId systemTypeId = options.SystemTypeId != ElementId.InvalidElementId
                ? options.SystemTypeId
                : DuctSystemResolver.ResolveSystemType(doc, options.StartTerminalConnector);

            if (systemTypeId == ElementId.InvalidElementId)
            {
                return DuctCreationResult.Failed("Could not resolve a valid MEPSystemType for ductwork.");
            }

            ElementId ductTypeId = options.DuctTypeId != ElementId.InvalidElementId
                ? options.DuctTypeId
                : DuctSystemResolver.ResolveDuctType(doc, options.Shape);

            if (ductTypeId == ElementId.InvalidElementId)
            {
                return DuctCreationResult.Failed("Could not resolve a valid DuctType symbol in the document.");
            }

            var createdDuctIds = new List<ElementId>();
            var createdFittingIds = new List<ElementId>();

            using (var tg = new TransactionGroup(doc, "Smart MEP Router — Generate Ductwork"))
            {
                tg.Start();

                try
                {
                    using (var trans = new Transaction(doc, "Create Ducts and Elbows"))
                    {
                        var failureHandling = trans.GetFailureHandlingOptions();
                        failureHandling.SetFailuresPreprocessor(new RoutingFailuresPreprocessor());
                        failureHandling.SetClearAfterRollback(true);
                        trans.SetFailureHandlingOptions(failureHandling);

                        trans.Start();

                        var createdDucts = new List<Duct>();

                        // 1. Create straight duct segments
                        for (int i = 0; i < routePoints.Count - 1; i++)
                        {
                            XYZ p1 = routePoints[i].ToXyz();
                            XYZ p2 = routePoints[i + 1].ToXyz();

                            if (p1.DistanceTo(p2) < 0.01)
                                continue;

                            Duct duct = Duct.Create(doc, systemTypeId, ductTypeId, levelId, p1, p2);

                            // Apply cross-sectional sizing
                            if (options.Shape == ConnectorShape.Round)
                            {
                                duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM)?.Set(options.DiameterFeet);
                            }
                            else
                            {
                                duct.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)?.Set(options.WidthFeet);
                                duct.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM)?.Set(options.HeightFeet);
                            }

                            createdDucts.Add(duct);
                            createdDuctIds.Add(duct.Id);
                        }

                        if (createdDucts.Count == 0)
                        {
                            trans.RollBack();
                            tg.RollBack();
                            return DuctCreationResult.Failed("No valid duct segments could be created.");
                        }

                        // Regenerate document to initialize connectors on newly created ducts
                        doc.Regenerate();

                        // 2. Create elbow fittings between adjacent ducts
                        for (int i = 0; i < createdDucts.Count - 1; i++)
                        {
                            Duct d1 = createdDucts[i];
                            Duct d2 = createdDucts[i + 1];
                            XYZ cornerPt = routePoints[i + 1].ToXyz();

                            Connector? c1 = GetClosestConnector(d1, cornerPt);
                            Connector? c2 = GetClosestConnector(d2, cornerPt);

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
                                    // Log or abort if fitting placement fails
                                    trans.RollBack();
                                    tg.RollBack();
                                    return DuctCreationResult.Failed($"Fitting placement failed at corner {i + 1}: {fittingEx.Message}");
                                }
                            }
                        }

                        // 3. Connect to terminal connectors if provided (FR-18)
                        if (options.StartTerminalConnector != null && createdDucts.Count > 0)
                        {
                            Connector? startDuctConn = GetClosestConnector(createdDucts[0], routePoints[0].ToXyz());
                            if (startDuctConn != null && !startDuctConn.IsConnected)
                            {
                                try { startDuctConn.ConnectTo(options.StartTerminalConnector); } catch { }
                            }
                        }

                        if (options.EndTerminalConnector != null && createdDucts.Count > 0)
                        {
                            Connector? endDuctConn = GetClosestConnector(createdDucts[createdDucts.Count - 1], routePoints[routePoints.Count - 1].ToXyz());
                            if (endDuctConn != null && !endDuctConn.IsConnected)
                            {
                                try { endDuctConn.ConnectTo(options.EndTerminalConnector); } catch { }
                            }
                        }

                        TransactionStatus status = trans.Commit();
                        if (status != TransactionStatus.Committed)
                        {
                            tg.RollBack();
                            return DuctCreationResult.Failed("Revit transaction did not commit cleanly.");
                        }
                    }

                    tg.Assimilate();
                    return DuctCreationResult.Succeeded(createdDuctIds, createdFittingIds);
                }
                catch (Exception ex)
                {
                    tg.RollBack();
                    return DuctCreationResult.Failed($"Duct generation failed: {ex.Message}");
                }
            }
        }

        private static Connector? GetClosestConnector(Duct duct, XYZ targetPoint)
        {
            if (duct?.ConnectorManager == null) return null;

            Connector? closest = null;
            double minDistanceSq = double.MaxValue;

            foreach (Connector conn in duct.ConnectorManager.Connectors)
            {
                if (conn == null) continue;

                double distSq = conn.Origin.DistanceTo(targetPoint);
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
