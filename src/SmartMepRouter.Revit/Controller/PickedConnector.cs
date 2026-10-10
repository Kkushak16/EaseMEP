using Autodesk.Revit.DB;
using SmartMepRouter.Core.Connectors;

namespace SmartMepRouter.Revit.Controller
{
    /// <summary>A connector chosen by the user: Core description plus the Revit identity needed to reconnect later.</summary>
    public sealed class PickedConnector
    {
        public ElementId OwnerId { get; }
        public int ConnectorId { get; }
        public ConnectorInfo Info { get; }
        public string Label { get; }

        public PickedConnector(ElementId ownerId, int connectorId, ConnectorInfo info, string label)
        {
            OwnerId = ownerId;
            ConnectorId = connectorId;
            Info = info;
            Label = label;
        }
    }
}
