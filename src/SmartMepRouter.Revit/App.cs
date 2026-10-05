using System;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.Revit.UI;

namespace SmartMepRouter.Revit
{
    /// <summary>
    /// External application entry point for the Smart MEP Router Revit add-in.
    /// Creates the ribbon tab, panel, and push buttons during Revit startup.
    /// </summary>
    public class App : IExternalApplication
    {
        public const string TabName = "Smart MEP Router";
        public const string PanelName = "Routing Engine";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // 1. Create Ribbon Tab
                try
                {
                    application.CreateRibbonTab(TabName);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    // Tab already exists
                }

                // 2. Create or find Ribbon Panel
                RibbonPanel? panel = null;
                List<RibbonPanel> existingPanels = application.GetRibbonPanels(TabName);
                foreach (RibbonPanel p in existingPanels)
                {
                    if (p.Name.Equals(PanelName, StringComparison.OrdinalIgnoreCase))
                    {
                        panel = p;
                        break;
                    }
                }

                if (panel == null)
                {
                    panel = application.CreateRibbonPanel(TabName, PanelName);
                }

                // 3. Register PushButton
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                var buttonData = new PushButtonData(
                    "SmartMepRouter_AutoRoute",
                    "Auto-Route",
                    assemblyPath,
                    "SmartMepRouter.Revit.Commands.RoutingCommand")
                {
                    ToolTip = "Computes collision-free duct routes and connects native Revit MEP elements.",
                    LongDescription = "Pick start and end connectors or points in any view. Evaluates obstacles (including linked structural models), computes fabrication-aware 90° routes, and builds connected ducts."
                };

                panel.AddItem(buttonData);

                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
