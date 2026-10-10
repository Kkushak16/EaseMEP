using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Spatial
{
    /// <summary>
    /// Represents an obstacle in 3D space with collision testing capabilities against grid cells and line segments.
    /// All coordinates and dimensions are in Revit internal decimal feet.
    /// </summary>
    public interface IObstacle
    {
        /// <summary>
        /// Unique identifier or element description of the obstacle.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Axis-aligned bounding box encompassing the entire obstacle in decimal feet.
        /// </summary>
        Aabb Bounds { get; }

        /// <summary>
        /// Conservative intersection test between a cubic grid cell and this obstacle.
        /// </summary>
        /// <param name="cellAabb">The bounding box of the grid cell in decimal feet.</param>
        /// <returns>True if the cell intersects or is inside the obstacle.</returns>
        bool IntersectsCell(Aabb cellAabb);

        /// <summary>
        /// Exact geometric intersection test between a 3D line segment and this obstacle.
        /// Used by the independent collision verification engine.
        /// </summary>
        /// <param name="p1">Segment start in decimal feet.</param>
        /// <param name="p2">Segment end in decimal feet.</param>
        /// <returns>True if the segment intersects the obstacle volume.</returns>
        bool IntersectsSegment(Vec3 p1, Vec3 p2);

        /// <summary>
        /// Returns an inflated copy of this obstacle incorporating clearance, insulation, and duct half-extent.
        /// </summary>
        /// <param name="marginFeet">Total isotropic inflation margin in decimal feet.</param>
        /// <returns>A new inflated obstacle.</returns>
        IObstacle Inflate(double marginFeet);

        /// <summary>
        /// Returns an anisotropically inflated copy of this obstacle with independent X, Y, and Z margins.
        /// Used for direction-dependent inflation (FR-17).
        /// </summary>
        /// <param name="marginX">Inflation margin along X axis in decimal feet.</param>
        /// <param name="marginY">Inflation margin along Y axis in decimal feet.</param>
        /// <param name="marginZ">Inflation margin along Z axis in decimal feet.</param>
        /// <returns>A new anisotropically inflated obstacle.</returns>
        IObstacle Inflate(double marginX, double marginY, double marginZ);

        /// <summary>
        /// Returns an anisotropically inflated copy of this obstacle with vector margins.
        /// </summary>
        /// <param name="marginsFeet">Vector of inflation margins in decimal feet.</param>
        /// <returns>A new anisotropically inflated obstacle.</returns>
        IObstacle Inflate(Vec3 marginsFeet);
    }
}
