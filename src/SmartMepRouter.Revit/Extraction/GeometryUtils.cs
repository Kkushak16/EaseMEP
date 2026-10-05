using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Spatial;

namespace SmartMepRouter.Revit.Extraction
{
    /// <summary>
    /// Utility methods for geometric conversions between Revit API entities and Core spatial representations.
    /// All distance units are in decimal feet.
    /// </summary>
    public static class GeometryUtils
    {
        private const double AngleTolerance = 1e-4;

        /// <summary>
        /// Converts a Revit <see cref="XYZ"/> point/vector to a Core <see cref="Vec3"/>.
        /// </summary>
        public static Vec3 ToVec3(this XYZ xyz)
        {
            if (xyz == null) throw new ArgumentNullException(nameof(xyz));
            return new Vec3(xyz.X, xyz.Y, xyz.Z);
        }

        /// <summary>
        /// Converts a Core <see cref="Vec3"/> to a Revit <see cref="XYZ"/>.
        /// </summary>
        public static XYZ ToXyz(this Vec3 v)
        {
            return new XYZ(v.X, v.Y, v.Z);
        }

        /// <summary>
        /// Converts a Revit <see cref="BoundingBoxXYZ"/> to a Core <see cref="Aabb"/>.
        /// </summary>
        public static Aabb ToAabb(this BoundingBoxXYZ bbox)
        {
            if (bbox == null) throw new ArgumentNullException(nameof(bbox));
            return new Aabb(bbox.Min.ToVec3(), bbox.Max.ToVec3());
        }

        /// <summary>
        /// Extracts non-empty solid geometries from a Revit <see cref="Element"/>.
        /// </summary>
        public static List<Solid> ExtractSolids(Element element, Options options)
        {
            var solids = new List<Solid>();
            if (element == null) return solids;

            GeometryElement geomElem = element.get_Geometry(options);
            if (geomElem == null) return solids;

            CollectSolids(geomElem, solids);
            return solids;
        }

        private static void CollectSolids(GeometryElement geomElem, List<Solid> solids)
        {
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    if (solid.Volume > 1e-6 && !solid.Faces.IsEmpty)
                    {
                        solids.Add(solid);
                    }
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    GeometryElement instanceGeom = geomInst.GetInstanceGeometry();
                    if (instanceGeom != null)
                    {
                        CollectSolids(instanceGeom, solids);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an <see cref="IObstacle"/> (either <see cref="ObbObstacle"/> or <see cref="BoxObstacle"/>)
        /// from a Revit element, taking into account element orientation and optional link transform.
        /// </summary>
        public static IObstacle? CreateObstacleFromElement(Element element, Options options, Transform? linkTransform = null, string? idPrefix = null)
        {
            if (element == null) return null;

            string elementIdStr = (idPrefix ?? "") + element.Id.ToString();

            // 1. Check for Wall orientation
            if (element is Wall wall && wall.Location is LocationCurve locCurve && locCurve.Curve is Line line)
            {
                XYZ dir = line.Direction;
                bool isRotated = Math.Abs(dir.X * dir.Y) > AngleTolerance;

                BoundingBoxXYZ wallBbox = wall.get_BoundingBox(null);
                double height = wallBbox != null ? Math.Max(wallBbox.Max.Z - wallBbox.Min.Z, 1.0) : 10.0;
                double baseZ = wallBbox != null ? wallBbox.Min.Z : line.GetEndPoint(0).Z;
                XYZ mid = (line.GetEndPoint(0) + line.GetEndPoint(1)) * 0.5;
                XYZ center = new XYZ(mid.X, mid.Y, baseZ + height * 0.5);
                double halfThickness = Math.Max(wall.Width * 0.5, 0.25);
                double halfLength = line.Length * 0.5;
                double halfHeight = height * 0.5;

                XYZ axisX = dir.Normalize();
                XYZ axisZ = XYZ.BasisZ;
                XYZ axisY = axisZ.CrossProduct(axisX).Normalize();

                if (linkTransform != null)
                {
                    center = linkTransform.OfPoint(center);
                    axisX = linkTransform.OfVector(axisX).Normalize();
                    axisY = linkTransform.OfVector(axisY).Normalize();
                    axisZ = linkTransform.OfVector(axisZ).Normalize();
                }

                if (isRotated || (linkTransform != null && IsTransformRotated(linkTransform)))
                {
                    Vec3 halfExtents = new Vec3(halfLength, halfThickness, halfHeight);
                    return new ObbObstacle(elementIdStr, center.ToVec3(), halfExtents, axisX.ToVec3(), axisY.ToVec3(), axisZ.ToVec3());
                }
            }

            // 2. Check for FamilyInstance (columns, framing) with instance transform
            if (element is FamilyInstance famInst)
            {
                Transform instTransform = famInst.GetTransform();
                bool isInstanceRotated = IsTransformRotated(instTransform);
                bool isLinkRotated = linkTransform != null && IsTransformRotated(linkTransform);

                if (isInstanceRotated || isLinkRotated)
                {
                    Transform totalTransform = linkTransform != null ? linkTransform.Multiply(instTransform) : instTransform;

                    // Compute local bounding box from symbol geometry or element bounding box
                    BoundingBoxXYZ? localBbox = GetLocalBoundingBox(famInst, options);
                    if (localBbox != null)
                    {
                        XYZ localCenter = (localBbox.Min + localBbox.Max) * 0.5;
                        XYZ localHalf = (localBbox.Max - localBbox.Min) * 0.5;

                        XYZ worldCenter = totalTransform.OfPoint(localCenter);
                        XYZ axisX = totalTransform.BasisX.Normalize();
                        XYZ axisY = totalTransform.BasisY.Normalize();
                        XYZ axisZ = totalTransform.BasisZ.Normalize();

                        Vec3 halfExtents = new Vec3(Math.Abs(localHalf.X), Math.Abs(localHalf.Y), Math.Abs(localHalf.Z));
                        return new ObbObstacle(elementIdStr, worldCenter.ToVec3(), halfExtents, axisX.ToVec3(), axisY.ToVec3(), axisZ.ToVec3());
                    }
                }
            }

            // 3. Fallback: Axis-aligned BoxObstacle from BoundingBoxXYZ
            BoundingBoxXYZ bbox = element.get_BoundingBox(null);
            if (bbox == null) return null;

            if (linkTransform != null)
            {
                if (IsTransformRotated(linkTransform))
                {
                    // If the link is rotated, represent the element box as an OBB in host space
                    XYZ localCenter = (bbox.Min + bbox.Max) * 0.5;
                    XYZ localHalf = (bbox.Max - bbox.Min) * 0.5;

                    XYZ worldCenter = linkTransform.OfPoint(localCenter);
                    XYZ axisX = linkTransform.BasisX.Normalize();
                    XYZ axisY = linkTransform.BasisY.Normalize();
                    XYZ axisZ = linkTransform.BasisZ.Normalize();

                    Vec3 halfExtents = new Vec3(Math.Abs(localHalf.X), Math.Abs(localHalf.Y), Math.Abs(localHalf.Z));
                    return new ObbObstacle(elementIdStr, worldCenter.ToVec3(), halfExtents, axisX.ToVec3(), axisY.ToVec3(), axisZ.ToVec3());
                }

                // Axis-aligned link: transform min/max directly
                XYZ tMin = linkTransform.OfPoint(bbox.Min);
                XYZ tMax = linkTransform.OfPoint(bbox.Max);
                Aabb hostAabb = new Aabb(
                    new Vec3(Math.Min(tMin.X, tMax.X), Math.Min(tMin.Y, tMax.Y), Math.Min(tMin.Z, tMax.Z)),
                    new Vec3(Math.Max(tMin.X, tMax.X), Math.Max(tMin.Y, tMax.Y), Math.Max(tMin.Z, tMax.Z)));
                return new BoxObstacle(elementIdStr, hostAabb);
            }

            return new BoxObstacle(elementIdStr, bbox.ToAabb());
        }

        private static bool IsTransformRotated(Transform t)
        {
            return Math.Abs(t.BasisX.Y) > AngleTolerance ||
                   Math.Abs(t.BasisX.Z) > AngleTolerance ||
                   Math.Abs(t.BasisY.X) > AngleTolerance ||
                   Math.Abs(t.BasisY.Z) > AngleTolerance ||
                   Math.Abs(t.BasisZ.X) > AngleTolerance ||
                   Math.Abs(t.BasisZ.Y) > AngleTolerance;
        }

        private static BoundingBoxXYZ? GetLocalBoundingBox(FamilyInstance famInst, Options options)
        {
            GeometryElement geomElem = famInst.get_Geometry(options);
            if (geomElem != null)
            {
                foreach (GeometryObject geomObj in geomElem)
                {
                    if (geomObj is GeometryInstance gi)
                    {
                        GeometryElement symbolGeom = gi.GetSymbolGeometry();
                        if (symbolGeom != null)
                        {
                            BoundingBoxXYZ sBbox = symbolGeom.GetBoundingBox();
                            if (sBbox != null) return sBbox;
                        }
                    }
                }
            }

            // Fallback to untransformed local bounding box estimate from world bbox
            BoundingBoxXYZ worldBbox = famInst.get_BoundingBox(null);
            if (worldBbox == null) return null;

            Transform inv = famInst.GetTransform().Inverse;
            XYZ p1 = inv.OfPoint(worldBbox.Min);
            XYZ p2 = inv.OfPoint(worldBbox.Max);

            var local = new BoundingBoxXYZ
            {
                Min = new XYZ(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Min(p1.Z, p2.Z)),
                Max = new XYZ(Math.Max(p1.X, p2.X), Math.Max(p1.Y, p2.Y), Math.Max(p1.Z, p2.Z))
            };
            return local;
        }
    }
}
