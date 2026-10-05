using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Revit.Preview
{
    /// <summary>
    /// Debug visualizer to render obstacle bounding boxes and OBB wireframes in Revit views (Phase 4 debug view).
    /// Allows visual confirmation of alignment and link transformations against the real model.
    /// </summary>
    public static class ObstacleVisualizer
    {
        private static readonly List<ElementId> _debugElementIds = new List<ElementId>();

        /// <summary>
        /// Clears all previously generated debug obstacle visualization elements.
        /// </summary>
        public static void ClearDebugVisuals(Document doc)
        {
            if (doc == null || _debugElementIds.Count == 0) return;

            using (var trans = new Transaction(doc, "Clear Obstacle Debug Visuals"))
            {
                trans.Start();
                foreach (ElementId id in _debugElementIds)
                {
                    if (doc.GetElement(id) != null)
                    {
                        doc.Delete(id);
                    }
                }
                trans.Commit();
            }

            _debugElementIds.Clear();
        }

        /// <summary>
        /// Creates wireframe or bounding box DirectShape elements for each extracted obstacle.
        /// Useful for visual alignment checks during Phase 4 testing.
        /// </summary>
        /// <param name="doc">The active host Revit document.</param>
        /// <param name="obstacles">List of extracted obstacles to visualize.</param>
        /// <returns>Number of debug visual elements created.</returns>
        public static int VisualizeObstacles(Document doc, IReadOnlyList<IObstacle> obstacles)
        {
            if (doc == null || obstacles == null || obstacles.Count == 0) return 0;

            ClearDebugVisuals(doc);

            int count = 0;
            using (var trans = new Transaction(doc, "Create Obstacle Debug Visuals"))
            {
                trans.Start();

                var categoryId = new ElementId(BuiltInCategory.OST_GenericModel);

                foreach (IObstacle obs in obstacles)
                {
                    try
                    {
                        var curves = new List<Curve>();
                        GetWireframeCurves(obs, curves);

                        if (curves.Count > 0)
                        {
                            var shapeObjects = new List<GeometryObject>(curves.Count);
                            for (int i = 0; i < curves.Count; i++)
                            {
                                shapeObjects.Add(curves[i]);
                            }

                            var ds = DirectShape.CreateElement(doc, categoryId);
                            ds.SetShape(shapeObjects);
                            ds.Name = $"Obstacle_{obs.Id}";
                            _debugElementIds.Add(ds.Id);
                            count++;
                        }
                    }
                    catch
                    {
                        // Skip if a curve cannot be formed
                    }
                }

                trans.Commit();
            }

            return count;
        }

        private static void GetWireframeCurves(IObstacle obstacle, List<Curve> curves)
        {
            Aabb bounds = obstacle.Bounds;
            Vec3 min = bounds.Min;
            Vec3 max = bounds.Max;

            XYZ c0 = new XYZ(min.X, min.Y, min.Z);
            XYZ c1 = new XYZ(max.X, min.Y, min.Z);
            XYZ c2 = new XYZ(max.X, max.Y, min.Z);
            XYZ c3 = new XYZ(min.X, max.Y, min.Z);

            XYZ c4 = new XYZ(min.X, min.Y, max.Z);
            XYZ c5 = new XYZ(max.X, min.Y, max.Z);
            XYZ c6 = new XYZ(max.X, max.Y, max.Z);
            XYZ c7 = new XYZ(min.X, max.Y, max.Z);

            // Bottom rectangle
            AddLine(c0, c1, curves);
            AddLine(c1, c2, curves);
            AddLine(c2, c3, curves);
            AddLine(c3, c0, curves);

            // Top rectangle
            AddLine(c4, c5, curves);
            AddLine(c5, c6, curves);
            AddLine(c6, c7, curves);
            AddLine(c7, c4, curves);

            // Vertical pillars
            AddLine(c0, c4, curves);
            AddLine(c1, c5, curves);
            AddLine(c2, c6, curves);
            AddLine(c3, c7, curves);
        }

        private static void AddLine(XYZ p1, XYZ p2, List<Curve> curves)
        {
            if (p1.DistanceTo(p2) > 0.001)
            {
                curves.Add(Line.CreateBound(p1, p2));
            }
        }
    }
}
