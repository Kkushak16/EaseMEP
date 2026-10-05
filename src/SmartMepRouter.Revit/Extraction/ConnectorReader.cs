using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using SmartMepRouter.Core.Connectors;

namespace SmartMepRouter.Revit.Extraction
{
    /// <summary>
    /// Reads MEP connector properties and geometry from Revit elements (FR-03).
    /// All spatial coordinates and dimensions are in decimal feet.
    /// </summary>
    public static class ConnectorReader
    {
        /// <summary>
        /// Retrieves all MEP connectors hosted on the specified element.
        /// </summary>
        public static List<ConnectorInfo> GetConnectors(Element element)
        {
            var results = new List<ConnectorInfo>();
            if (element == null) return results;

            ConnectorSet? connectorSet = GetConnectorSet(element);
            if (connectorSet == null || connectorSet.IsEmpty)
                return results;

            foreach (Connector conn in connectorSet)
            {
                if (conn == null) continue;

                var info = ExtractConnectorInfo(conn, element.Id.ToString());
                if (info != null)
                {
                    results.Add(info);
                }
            }

            return results;
        }

        /// <summary>
        /// Finds the connector on an element closest to a user-picked 3D point.
        /// </summary>
        public static ConnectorInfo? GetNearestConnector(Element element, XYZ pickPoint)
        {
            if (element == null || pickPoint == null) return null;

            List<ConnectorInfo> connectors = GetConnectors(element);
            if (connectors.Count == 0) return null;

            ConnectorInfo? closest = null;
            double minDistanceSq = double.MaxValue;

            foreach (ConnectorInfo conn in connectors)
            {
                XYZ origin = conn.Position.ToXyz();
                double distSq = origin.DistanceTo(pickPoint);
                if (distSq < minDistanceSq)
                {
                    minDistanceSq = distSq;
                    closest = conn;
                }
            }

            return closest;
        }

        private static ConnectorSet? GetConnectorSet(Element element)
        {
            if (element is FamilyInstance famInst && famInst.MEPModel != null)
            {
                return famInst.MEPModel.ConnectorManager?.Connectors;
            }

            if (element is MEPCurve mepCurve)
            {
                return mepCurve.ConnectorManager?.Connectors;
            }

            // Fallback: check if the element has an MEPModel property via reflection / standard access
            return null;
        }

        private static ConnectorInfo? ExtractConnectorInfo(Connector conn, string ownerId)
        {
            try
            {
                XYZ origin = conn.Origin;
                XYZ direction = conn.CoordinateSystem?.BasisZ ?? XYZ.BasisZ;

                ConnectorShape shape = ConnectorShape.Rectangular;
                double width = 0.0;
                double height = 0.0;
                double diameter = 0.0;

                switch (conn.Shape)
                {
                    case ConnectorProfileType.Rectangular:
                        shape = ConnectorShape.Rectangular;
                        width = conn.Width;
                        height = conn.Height;
                        break;

                    case ConnectorProfileType.Round:
                        shape = ConnectorShape.Round;
                        diameter = conn.Radius * 2.0;
                        width = diameter;
                        height = diameter;
                        break;

                    case ConnectorProfileType.Oval:
                        shape = ConnectorShape.Oval;
                        width = conn.Width;
                        height = conn.Height;
                        diameter = Math.Max(width, height);
                        break;

                    default:
                        shape = ConnectorShape.Other;
                        break;
                }

                string systemTypeIdStr = string.Empty;
                if (conn.MEPSystem != null)
                {
                    systemTypeIdStr = conn.MEPSystem.GetTypeId()?.ToString() ?? conn.MEPSystem.Id.ToString();
                }

                return new ConnectorInfo
                {
                    Id = conn.Id.ToString(),
                    Position = origin.ToVec3(),
                    Direction = direction.ToVec3(),
                    Shape = shape,
                    WidthFeet = width,
                    HeightFeet = height,
                    DiameterFeet = diameter,
                    SystemTypeId = systemTypeIdStr,
                    OwnerElementId = ownerId
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
