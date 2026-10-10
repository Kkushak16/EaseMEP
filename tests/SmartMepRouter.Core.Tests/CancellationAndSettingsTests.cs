using System.Threading;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;
using SmartMepRouter.UI.Settings;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class CancellationAndSettingsTests
    {
        [Fact]
        public void Route_CancelledToken_ReturnsCancelled()
        {
            var corridor = new Aabb(new Vec3(-50, -50, -50), new Vec3(150, 150, 50));
            var grid = new SparseGrid(new Vec3(0, 0, 0), 1.0, corridor);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Wall forces a long search so the 1024-expansion cancellation check is reached.
            grid.BlockAabb(new Aabb(new Vec3(50, -50, -50), new Vec3(51, 150, 50)));
            RouteResult result = new Router().Route(grid, new Vec3(0, 0, 0), new Vec3(100, 0, 0), Direction3D.None, Direction3D.None, cts.Token);

            Assert.Equal(RoutingStatus.Cancelled, result.Status);
        }

        [Fact]
        public void SettingsStore_RoundTripsAndFallsBackToDefaults()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smr-settings-" + System.Guid.NewGuid() + ".json");
            var store = new SettingsStore(path);

            Assert.Equal(100.0, store.Load().ClearanceMarginMm);

            store.Save(new UserSettings { ClearanceMarginMm = 75.0, IncludeCeilings = true });
            UserSettings loaded = store.Load();
            System.IO.File.Delete(path);

            Assert.Equal(75.0, loaded.ClearanceMarginMm);
            Assert.True(loaded.IncludeCeilings);
        }
    }
}
