using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Revit.Preview
{
    /// <summary>
    /// Draws the previewed route as temporary model lines (DirectShape polyline) and removes them again.
    /// ASSUMPTION: DirectShape wireframe is used instead of TemporaryGraphicsManager because the latter's
    /// availability on both target API families is unverified (R-V1). The preview is always deleted before
    /// generation and when the dialog closes.
    /// </summary>
    public sealed class RoutePreviewDrawer
    {
        private ElementId? _previewId;

        /// <summary>Replaces any existing preview with a polyline through <paramref name="points"/> (decimal feet).</summary>
        public void Show(Document doc, IReadOnlyList<Vec3> points)
        {
            using (var trans = new Transaction(doc, "Smart MEP Router — Preview Path"))
            {
                trans.Start();
                DeleteInTransaction(doc);

                var geometry = new List<GeometryObject>();
                for (int i = 0; i < points.Count - 1; i++)
                {
                    XYZ a = new XYZ(points[i].X, points[i].Y, points[i].Z);
                    XYZ b = new XYZ(points[i + 1].X, points[i + 1].Y, points[i + 1].Z);
                    if (a.DistanceTo(b) > 0.001) geometry.Add(Line.CreateBound(a, b));
                }

                if (geometry.Count > 0)
                {
                    var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                    ds.SetShape(geometry);
                    ds.Name = "SmartMepRouter_Preview";
                    _previewId = ds.Id;
                }

                trans.Commit();
            }
        }

        /// <summary>Removes the preview if present. Safe to call repeatedly.</summary>
        public void Clear(Document doc)
        {
            if (_previewId == null) return;

            using (var trans = new Transaction(doc, "Smart MEP Router — Clear Preview"))
            {
                trans.Start();
                DeleteInTransaction(doc);
                trans.Commit();
            }
        }

        private void DeleteInTransaction(Document doc)
        {
            if (_previewId != null && doc.GetElement(_previewId) != null)
            {
                doc.Delete(_previewId);
            }
            _previewId = null;
        }
    }
}
