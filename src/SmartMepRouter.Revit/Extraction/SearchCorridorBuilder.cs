using System;
using Autodesk.Revit.DB;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Revit.Extraction
{
    /// <summary>
    /// Builds and transforms spatial search corridor bounding boxes in Revit internal decimal feet.
    /// Used to constrain element filtering to the region between routing endpoints.
    /// </summary>
    public static class SearchCorridorBuilder
    {
        /// <summary>
        /// Builds an axis-aligned corridor <see cref="Outline"/> encompassing start and end points
        /// expanded by a lateral margin and vertical plenum boundaries.
        /// </summary>
        /// <param name="start">Routing start position in decimal feet.</param>
        /// <param name="end">Routing end position in decimal feet.</param>
        /// <param name="marginFeet">Lateral expansion margin in decimal feet.</param>
        /// <param name="minZ">Optional lower vertical elevation in decimal feet.</param>
        /// <param name="maxZ">Optional upper vertical elevation in decimal feet.</param>
        /// <returns>A Revit <see cref="Outline"/> defining the spatial filter volume.</returns>
        public static Outline BuildCorridorOutline(XYZ start, XYZ end, double marginFeet, double? minZ = null, double? maxZ = null)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (end == null) throw new ArgumentNullException(nameof(end));

            double minX = Math.Min(start.X, end.X) - marginFeet;
            double maxX = Math.Max(start.X, end.X) + marginFeet;
            double minY = Math.Min(start.Y, end.Y) - marginFeet;
            double maxY = Math.Max(start.Y, end.Y) + marginFeet;

            double lowerZ = minZ.HasValue ? minZ.Value : (Math.Min(start.Z, end.Z) - marginFeet);
            double upperZ = maxZ.HasValue ? maxZ.Value : (Math.Max(start.Z, end.Z) + marginFeet);

            if (lowerZ > upperZ)
            {
                double temp = lowerZ;
                lowerZ = upperZ;
                upperZ = temp;
            }

            return new Outline(new XYZ(minX, minY, lowerZ), new XYZ(maxX, maxY, upperZ));
        }

        /// <summary>
        /// Transforms an <see cref="Outline"/> from one coordinate system to another
        /// (e.g. host to link space via linkTransform.Inverse).
        /// </summary>
        /// <param name="outline">Source outline in original coordinate space.</param>
        /// <param name="transform">Transformation to apply.</param>
        /// <returns>A new <see cref="Outline"/> enclosing the transformed volume.</returns>
        public static Outline TransformOutline(Outline outline, Transform transform)
        {
            if (outline == null) throw new ArgumentNullException(nameof(outline));
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            XYZ min = outline.MinimumPoint;
            XYZ max = outline.MaximumPoint;

            XYZ[] corners = new XYZ[8]
            {
                new XYZ(min.X, min.Y, min.Z),
                new XYZ(max.X, min.Y, min.Z),
                new XYZ(min.X, max.Y, min.Z),
                new XYZ(max.X, max.Y, min.Z),
                new XYZ(min.X, min.Y, max.Z),
                new XYZ(max.X, min.Y, max.Z),
                new XYZ(min.X, max.Y, max.Z),
                new XYZ(max.X, max.Y, max.Z)
            };

            double tMinX = double.MaxValue, tMinY = double.MaxValue, tMinZ = double.MaxValue;
            double tMaxX = double.MinValue, tMaxY = double.MinValue, tMaxZ = double.MinValue;

            for (int i = 0; i < 8; i++)
            {
                XYZ transformedPt = transform.OfPoint(corners[i]);

                if (transformedPt.X < tMinX) tMinX = transformedPt.X;
                if (transformedPt.Y < tMinY) tMinY = transformedPt.Y;
                if (transformedPt.Z < tMinZ) tMinZ = transformedPt.Z;

                if (transformedPt.X > tMaxX) tMaxX = transformedPt.X;
                if (transformedPt.Y > tMaxY) tMaxY = transformedPt.Y;
                if (transformedPt.Z > tMaxZ) tMaxZ = transformedPt.Z;
            }

            return new Outline(new XYZ(tMinX, tMinY, tMinZ), new XYZ(tMaxX, tMaxY, tMaxZ));
        }

        /// <summary>
        /// Converts a Revit <see cref="Outline"/> to a Core <see cref="Aabb"/>.
        /// </summary>
        public static Aabb ToAabb(Outline outline)
        {
            if (outline == null) throw new ArgumentNullException(nameof(outline));
            XYZ min = outline.MinimumPoint;
            XYZ max = outline.MaximumPoint;
            return new Aabb(new Vec3(min.X, min.Y, min.Z), new Vec3(max.X, max.Y, max.Z));
        }
    }
}
