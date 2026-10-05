using System;
using SmartMepRouter.Core.Geometry;

namespace SmartMepRouter.Core.Spatial
{
    /// <summary>
    /// Represents an Oriented Bounding Box (OBB) obstacle in Revit internal decimal feet.
    /// Supports exact Separating Axis Theorem (SAT) cell collision checks and segment intersection.
    /// </summary>
    public class ObbObstacle : IObstacle
    {
        public string Id { get; }
        public Vec3 Center { get; }
        public Vec3 HalfExtents { get; }
        public Vec3 AxisX { get; }
        public Vec3 AxisY { get; }
        public Vec3 AxisZ { get; }
        public Aabb Bounds { get; }

        public ObbObstacle(string id, Vec3 center, Vec3 halfExtents, Vec3 axisX, Vec3 axisY, Vec3 axisZ)
        {
            Id = id ?? Guid.NewGuid().ToString("N");
            Center = center;
            HalfExtents = new Vec3(Math.Abs(halfExtents.X), Math.Abs(halfExtents.Y), Math.Abs(halfExtents.Z));
            AxisX = axisX.Normalize();
            AxisY = axisY.Normalize();
            AxisZ = axisZ.Normalize();
            Bounds = ComputeBoundingAabb(center, HalfExtents, AxisX, AxisY, AxisZ);
        }

        /// <summary>
        /// Creates an OBB with a rotation around the vertical Z axis (yaw angle in radians).
        /// </summary>
        public static ObbObstacle FromYaw(string id, Vec3 center, Vec3 halfExtents, double yawRadians)
        {
            double cos = Math.Cos(yawRadians);
            double sin = Math.Sin(yawRadians);
            Vec3 axisX = new Vec3(cos, sin, 0);
            Vec3 axisY = new Vec3(-sin, cos, 0);
            Vec3 axisZ = new Vec3(0, 0, 1);
            return new ObbObstacle(id, center, halfExtents, axisX, axisY, axisZ);
        }

        public bool IntersectsCell(Aabb cellAabb)
        {
            // Early out: broadphase AABB test
            if (!Bounds.Intersects(cellAabb))
                return false;

            // Full 3D Separating Axis Theorem (SAT) test: 15 axes
            Vec3 cellCenter = cellAabb.Center;
            Vec3 cellHalf = cellAabb.Extents;
            Vec3 translation = cellCenter - Center;

            // Box A = Cell (standard axes), Box B = this OBB
            double[,] r = new double[3, 3];
            double[,] absR = new double[3, 3];

            Vec3[] bAxes = { AxisX, AxisY, AxisZ };

            for (int i = 0; i < 3; i++)
            {
                r[0, i] = Dot(Vec3.UnitX, bAxes[i]);
                r[1, i] = Dot(Vec3.UnitY, bAxes[i]);
                r[2, i] = Dot(Vec3.UnitZ, bAxes[i]);

                for (int j = 0; j < 3; j++)
                {
                    absR[j, i] = Math.Abs(r[j, i]) + 1e-7;
                }
            }

            double[] t = { translation.X, translation.Y, translation.Z };

            // 1. Test Cell axes: L = A0, A1, A2
            for (int i = 0; i < 3; i++)
            {
                double ra = GetCoord(cellHalf, i);
                double rb = HalfExtents.X * absR[i, 0] + HalfExtents.Y * absR[i, 1] + HalfExtents.Z * absR[i, 2];
                if (Math.Abs(t[i]) > ra + rb)
                    return false;
            }

            // 2. Test OBB axes: L = B0, B1, B2
            for (int i = 0; i < 3; i++)
            {
                double ra = cellHalf.X * absR[0, i] + cellHalf.Y * absR[1, i] + cellHalf.Z * absR[2, i];
                double rb = GetCoord(HalfExtents, i);
                double projectedT = Math.Abs(t[0] * r[0, i] + t[1] * r[1, i] + t[2] * r[2, i]);
                if (projectedT > ra + rb)
                    return false;
            }

            // 3. Test 9 cross-product axes: L = A_i x B_j
            // A0 x B0
            if (CheckCrossAxis(t[2] * r[1, 0] - t[1] * r[2, 0],
                cellHalf.Y * absR[2, 0] + cellHalf.Z * absR[1, 0],
                HalfExtents.Y * absR[0, 2] + HalfExtents.Z * absR[0, 1])) return false;

            // A0 x B1
            if (CheckCrossAxis(t[2] * r[1, 1] - t[1] * r[2, 1],
                cellHalf.Y * absR[2, 1] + cellHalf.Z * absR[1, 1],
                HalfExtents.X * absR[0, 2] + HalfExtents.Z * absR[0, 0])) return false;

            // A0 x B2
            if (CheckCrossAxis(t[2] * r[1, 2] - t[1] * r[2, 2],
                cellHalf.Y * absR[2, 2] + cellHalf.Z * absR[1, 2],
                HalfExtents.X * absR[0, 1] + HalfExtents.Y * absR[0, 0])) return false;

            // A1 x B0
            if (CheckCrossAxis(t[0] * r[2, 0] - t[2] * r[0, 0],
                cellHalf.X * absR[2, 0] + cellHalf.Z * absR[0, 0],
                HalfExtents.Y * absR[1, 2] + HalfExtents.Z * absR[1, 1])) return false;

            // A1 x B1
            if (CheckCrossAxis(t[0] * r[2, 1] - t[2] * r[0, 1],
                cellHalf.X * absR[2, 1] + cellHalf.Z * absR[0, 1],
                HalfExtents.X * absR[1, 2] + HalfExtents.Z * absR[1, 0])) return false;

            // A1 x B2
            if (CheckCrossAxis(t[0] * r[2, 2] - t[2] * r[0, 2],
                cellHalf.X * absR[2, 2] + cellHalf.Z * absR[0, 2],
                HalfExtents.X * absR[1, 1] + HalfExtents.Y * absR[1, 0])) return false;

            // A2 x B0
            if (CheckCrossAxis(t[1] * r[0, 0] - t[0] * r[1, 0],
                cellHalf.X * absR[1, 0] + cellHalf.Y * absR[0, 0],
                HalfExtents.Y * absR[2, 2] + HalfExtents.Z * absR[2, 1])) return false;

            // A2 x B1
            if (CheckCrossAxis(t[1] * r[0, 1] - t[0] * r[1, 1],
                cellHalf.X * absR[1, 1] + cellHalf.Y * absR[0, 1],
                HalfExtents.X * absR[2, 2] + HalfExtents.Z * absR[2, 0])) return false;

            // A2 x B2
            if (CheckCrossAxis(t[1] * r[0, 2] - t[0] * r[1, 2],
                cellHalf.X * absR[1, 2] + cellHalf.Y * absR[0, 2],
                HalfExtents.X * absR[2, 1] + HalfExtents.Y * absR[2, 0])) return false;

            return true;
        }

        public bool IntersectsSegment(Vec3 p1, Vec3 p2)
        {
            // Transform segment into OBB's local coordinate system
            Vec3 localP1 = WorldToLocal(p1);
            Vec3 localP2 = WorldToLocal(p2);

            // Test against local AABB [-HalfExtents, +HalfExtents]
            var localBounds = new Aabb(-HalfExtents, HalfExtents);
            var box = new BoxObstacle(Id, localBounds);
            return box.IntersectsSegment(localP1, localP2);
        }

        public IObstacle Inflate(double marginFeet)
        {
            Vec3 delta = new Vec3(marginFeet, marginFeet, marginFeet);
            return new ObbObstacle($"{Id}_inflated", Center, HalfExtents + delta, AxisX, AxisY, AxisZ);
        }

        public Vec3 WorldToLocal(Vec3 worldPt)
        {
            Vec3 d = worldPt - Center;
            return new Vec3(
                Dot(d, AxisX),
                Dot(d, AxisY),
                Dot(d, AxisZ));
        }

        private static bool CheckCrossAxis(double dist, double rA, double rB)
        {
            return Math.Abs(dist) > rA + rB;
        }

        private static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        private static double GetCoord(Vec3 v, int index)
        {
            switch (index)
            {
                case 0: return v.X;
                case 1: return v.Y;
                default: return v.Z;
            }
        }

        private static Aabb ComputeBoundingAabb(Vec3 center, Vec3 halfExtents, Vec3 ax, Vec3 ay, Vec3 az)
        {
            double hx = halfExtents.X * Math.Abs(ax.X) + halfExtents.Y * Math.Abs(ay.X) + halfExtents.Z * Math.Abs(az.X);
            double hy = halfExtents.X * Math.Abs(ax.Y) + halfExtents.Y * Math.Abs(ay.Y) + halfExtents.Z * Math.Abs(az.Y);
            double hz = halfExtents.X * Math.Abs(ax.Z) + halfExtents.Y * Math.Abs(ay.Z) + halfExtents.Z * Math.Abs(az.Z);

            Vec3 extents = new Vec3(hx, hy, hz);
            return new Aabb(center - extents, center + extents);
        }
    }
}
