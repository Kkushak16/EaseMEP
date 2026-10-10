using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SmartMepRouter.Revit.Controller;
using SmartMepRouter.UI.Views;

namespace SmartMepRouter.Revit.Commands
{
    /// <summary>
    /// Entry command invoked from the Revit Ribbon.
    /// Manages the lifecycle of the modeless Smart MEP Router dialog.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RoutingCommand : IExternalCommand
    {
        private static MainView? _activeWindow;
        private static RoutingController? _activeController;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_activeWindow == null || !_activeWindow.IsLoaded)
                {
                    _activeWindow = new MainView();

                    // Set Revit main window as the owner of the modeless dialog
                    IntPtr revitWindowHandle = commandData.Application.MainWindowHandle;
                    if (revitWindowHandle != IntPtr.Zero)
                    {
                        var helper = new WindowInteropHelper(_activeWindow)
                        {
                            Owner = revitWindowHandle
                        };
                    }

                    _activeController = new RoutingController(_activeWindow);
                    _activeWindow.Closed += (sender, args) => { _activeWindow = null; _activeController = null; };
                    _activeWindow.Show();
                }
                else
                {
                    if (_activeWindow.WindowState == System.Windows.WindowState.Minimized)
                    {
                        _activeWindow.WindowState = System.Windows.WindowState.Normal;
                    }
                    _activeWindow.Activate();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = "Failed to launch Smart MEP Router: " + ex.Message;
                return Result.Failed;
            }
        }
    }
}
