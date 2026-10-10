using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SmartMepRouter.Core.Connectors;
using SmartMepRouter.Core.Geometry;
using SmartMepRouter.Core.Post;
using SmartMepRouter.Core.Search;
using SmartMepRouter.Core.Spatial;
using SmartMepRouter.Revit.Creation;
using SmartMepRouter.Revit.Extraction;
using SmartMepRouter.Revit.Preview;
using SmartMepRouter.UI.ViewModels;
using SmartMepRouter.UI.Views;
using CoreUnits = SmartMepRouter.Core.Geometry.Units;

namespace SmartMepRouter.Revit.Controller
{
    /// <summary>
    /// Connects the modeless <see cref="MainView"/> to Revit. Every Revit API call runs inside the
    /// <see cref="RevitActionHandler"/> on the API thread (R-A2); the path search itself is Revit-free and
    /// runs on a worker task so the UI stays responsive and cancellable (R-U3).
    /// </summary>
    public sealed class RoutingController
    {
        /// <summary>Grid cell edge in decimal feet (about 152 mm). ASSUMPTION: final value is decided by the Phase 7 benchmark.</summary>
        private const double CellSizeFeet = 0.5;

        private readonly MainView _view;
        private readonly MainViewModel _vm;
        private readonly RevitActionHandler _handler = new RevitActionHandler();
        private readonly ExternalEvent _event;
        private readonly RoutePreviewDrawer _preview = new RoutePreviewDrawer();

        private PickedConnector? _start;
        private PickedConnector? _end;
        private IReadOnlyList<Vec3>? _route;
        private Document? _routeDocument;
        private CancellationTokenSource? _cts;

        public RoutingController(MainView view)
        {
            _view = view;
            _vm = view.ViewModel;
            _event = ExternalEvent.Create(_handler);

            _handler.ActionFailed += ex => Ui(() =>
            {
                _vm.IsBusy = false;
                _vm.IsPicking = false;
                _vm.Banner = string.Empty;
                _vm.AppendStatus("Unexpected error: " + ex.Message);
            });

            _view.PickStartRequested += (s, e) => Run(app => Pick(app, isStart: true));
            _view.PickEndRequested += (s, e) => Run(app => Pick(app, isStart: false));
            _view.PreviewRequested += (s, e) => OnPreviewClicked();
            _view.GenerateRequested += (s, e) => Run(Generate);
            _vm.PreviewInvalidated += (s, e) => { _route = null; Run(ClearPreview); };
            _view.Closed += (s, e) => { _cts?.Cancel(); Run(ClearPreview); };
        }

        private void Run(Action<UIApplication> action)
        {
            _handler.Enqueue(action);
            _event.Raise();
        }

        private void Ui(Action action) => _view.Dispatcher.BeginInvoke(action);

        // ---------------------------------------------------------------- pick

        private void Pick(UIApplication app, bool isStart)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            string which = isStart ? "start" : "end";

            _vm.IsPicking = true;
            _vm.Banner = $"Select {which} connector in the view. Esc to cancel.";
            try
            {
                Reference r = uidoc.Selection.PickObject(ObjectType.Element, new ConnectorPickFilter(), $"Select {which} connector");
                Element? element = uidoc.Document.GetElement(r);
                ConnectorInfo? info = element == null ? null : ConnectorReader.GetNearestConnector(element, r.GlobalPoint);

                if (element == null || info == null || !int.TryParse(info.Id, out int connectorId))
                {
                    _vm.AppendStatus($"No usable connector found on the picked element. Pick a duct, terminal or equipment with connectors.");
                    return;
                }

                var picked = new PickedConnector(element.Id, connectorId, info, $"ID: {element.Id} ({element.Name})");
                if (isStart)
                {
                    _start = picked;
                    _vm.StartConnectorText = picked.Label;
                    _vm.IsStartPicked = true;
                }
                else
                {
                    _end = picked;
                    _vm.EndConnectorText = picked.Label;
                    _vm.IsEndPicked = true;
                }
                _vm.AppendStatus($"Picked {which} connector {picked.Label}");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                _vm.AppendStatus($"Pick {which} cancelled.");
            }
            finally
            {
                _vm.Banner = string.Empty;
                _vm.IsPicking = false;
            }
        }

        // ------------------------------------------------------------- preview

        private void OnPreviewClicked()
        {
            if (_vm.IsBusy)
            {
                _cts?.Cancel();
                _vm.AppendStatus("Cancelling search…");
                return;
            }
            Run(StartPreview);
        }

        private void StartPreview(UIApplication app)
        {
            if (_start == null || _end == null) return;

            Document doc = app.ActiveUIDocument.Document;
            _preview.Clear(doc);
            _route = null;
            _vm.IsPreviewReady = false;
            _vm.IsBusy = true;

            ConnectorInfo s = _start.Info;
            ConnectorInfo e = _end.Info;

            // Revit-side work: extract obstacles (needs the API thread).
            ExtractionOptions options = BuildExtractionOptions(s);
            options.ExcludedElementIds.Add(_start.OwnerId);
            options.ExcludedElementIds.Add(_end.OwnerId);
            ExtractionResult extraction = ObstacleExtractor.Extract(doc, s.Position.ToXyz(), e.Position.ToXyz(), options);
            foreach (string warning in extraction.Warnings)
            {
                _vm.AppendStatus("Warning: " + warning);
                if (warning.Contains("not loaded") || warning.Contains("Error reading linked"))
                {
                    _vm.Banner = warning;
                }
            }
            _vm.AppendStatus($"{extraction.TotalObstacleCount} obstacles ({extraction.HostObstacleCount} host, {extraction.LinkObstacleCount} linked) in {extraction.Duration.TotalMilliseconds:F0} ms");

            CostModel cost = _vm.SelectedOptimization == MainViewModel.OptimizationShortest
                ? CostModel.ShortestLength
                : CostModel.FewerBends;
            cost.Allow45DegreeBends = _vm.Allow45DegreeBends;
            cost.ApplyCellSize(CellSizeFeet);
            Direction3D startDir = Direction3DExtensions.FromVector(s.Direction);
            Direction3D endDir = Direction3DExtensions.FromVector(e.Direction);

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            bool isRect = _vm.IsRectangular;
            double ductW = options.DuctWidthFeet;
            double ductH = options.DuctHeightFeet;
            double clearanceAndInsulation = options.ClearanceFeet + options.InsulationFeet;

            // Revit-free search with corridor widening retry on a worker task (R-U3, Phase 8).
            Task.Run(() =>
            {
                var wideningRouter = new CorridorWideningRouter(
                    new Router(cost),
                    new CorridorWideningOptions { MaxRetries = 2, ExpansionMarginFeet = 5.0, EnableWideningOnNoPath = true });

                return wideningRouter.RouteWithWidening(
                    s.Position,
                    e.Position,
                    extraction.Obstacles,
                    extraction.CorridorBounds,
                    CellSizeFeet,
                    startDir,
                    endDir,
                    enableDirectional: isRect,
                    ductWidthFeet: ductW,
                    ductHeightFeet: ductH,
                    clearanceAndInsulationFeet: clearanceAndInsulation,
                    cancellationToken: token);
            }, token).ContinueWith(task => Ui(() => OnSearchDone(task, extraction.Obstacles, doc, s.Position, e.Position)));
        }

        private void OnSearchDone(Task<RouteResult> task, IReadOnlyList<IObstacle> obstacles, Document doc, Vec3 startPos, Vec3 endPos)
        {
            _vm.IsBusy = false;

            if (task.IsFaulted || task.IsCanceled)
            {
                _vm.AppendStatus("Search failed: " + (task.Exception?.GetBaseException().Message ?? "cancelled"));
                return;
            }

            RouteResult result = task.Result;
            if (!result.IsSuccess)
            {
                if (result.FailureReport != null)
                {
                    _vm.AppendStatus(result.FailureReport.FormattedSummary());
                    if (result.BlockingElementIds.Count > 0)
                    {
                        _vm.Banner = $"Obstructed by element(s): {string.Join(", ", result.BlockingElementIds)}";
                    }
                }
                else
                {
                    _vm.AppendStatus($"{result.Status}: {result.Message}");
                }
                return;
            }

            List<Vec3> aligned = EndpointAligner.Align(result.Points, startPos, endPos, obstacles);

            // R-T2: independent exact collision check before anything reaches the Revit layer.
            if (!CollisionVerifier.IsPathCollisionFree(aligned, obstacles))
            {
                _vm.AppendStatus("Route rejected: independent collision check failed. Increase clearance or move the connectors.");
                return;
            }

            // Fitting feasibility check (FR-16)
            var feasibility = FittingFeasibility.Verify(aligned, CoreUnits.MmToFeet(150.0));
            if (!feasibility.IsFeasible)
            {
                foreach (var v in feasibility.Violations)
                {
                    _vm.AppendStatus("Fabrication warning: " + v);
                }
            }

            _route = aligned;
            _routeDocument = doc;
            _vm.AppendStatus($"Route solved in {result.SearchTimeMs:F0} ms: {aligned.Count - 1} segments, {PathSimplifier.CountBends(aligned)} elbows, {UnitsText(PathSimplifier.ComputeTotalLength(aligned))}, 0 collisions");

            Run(app =>
            {
                if (_route == null) return; // invalidated while queued
                _preview.Show(app.ActiveUIDocument.Document, _route);
                _vm.IsPreviewReady = true;
            });
        }

        private void ClearPreview(UIApplication app)
        {
            Document? doc = app.ActiveUIDocument?.Document;
            if (doc != null) _preview.Clear(doc);
        }

        // ------------------------------------------------------------ generate

        private void Generate(UIApplication app)
        {
            if (_route == null || _start == null || _end == null) return;

            Document doc = app.ActiveUIDocument.Document;
            if (!ReferenceEquals(doc, _routeDocument))
            {
                _vm.AppendStatus("The active document changed since the preview. Run Preview Path again.");
                return;
            }

            _vm.IsBusy = true;
            try
            {
                _preview.Clear(doc);

                ConnectorInfo s = _start.Info;
                ConnectorShape shape = _vm.IsRectangular ? ConnectorShape.Rectangular : ConnectorShape.Round;
                var options = new DuctCreationOptions
                {
                    Shape = shape,
                    WidthFeet = s.WidthFeet > 0 ? s.WidthFeet : CoreUnits.MmToFeet(300.0),
                    HeightFeet = s.HeightFeet > 0 ? s.HeightFeet : CoreUnits.MmToFeet(200.0),
                    DiameterFeet = s.DiameterFeet > 0 ? s.DiameterFeet : CoreUnits.MmToFeet(300.0),
                    StartTerminalConnector = ResolveConnector(doc, _start),
                    EndTerminalConnector = ResolveConnector(doc, _end)
                };

                DuctCreationResult result = DuctBuilder.CreateDuctRoute(doc, _route, options);
                if (result.Success)
                {
                    _vm.AppendStatus($"Created {result.DuctIds.Count} ducts and {result.FittingIds.Count} fittings. Ctrl+Z undoes them in one step.");
                    foreach (var warning in result.Warnings)
                    {
                        _vm.AppendStatus("Warning: " + warning);
                    }
                    _route = null;
                    _vm.IsPreviewReady = false;
                }
                else
                {
                    string failureMsg = "Generation failed, model unchanged: " + result.ErrorMessage;
                    if (result.FailingElementIds.Count > 0)
                    {
                        failureMsg += $" (Failing Element IDs: {string.Join(", ", result.FailingElementIds)})";
                    }
                    _vm.AppendStatus(failureMsg);
                    // Keep the route so the user can retry; redraw the preview we just cleared.
                    _preview.Show(doc, _route);
                }
            }
            finally
            {
                _vm.IsBusy = false;
            }
        }

        // ------------------------------------------------------------- helpers

        private ExtractionOptions BuildExtractionOptions(ConnectorInfo start)
        {
            ConnectorShape shape = _vm.IsRectangular ? ConnectorShape.Rectangular : ConnectorShape.Round;
            double width = start.WidthFeet > 0 ? start.WidthFeet : CoreUnits.MmToFeet(300.0);
            double height = start.HeightFeet > 0 ? start.HeightFeet : CoreUnits.MmToFeet(200.0);
            double diameter = start.DiameterFeet > 0 ? start.DiameterFeet : CoreUnits.MmToFeet(300.0);
            if (shape == ConnectorShape.Round)
            {
                width = diameter;
                height = diameter;
            }

            return new ExtractionOptions
            {
                Shape = shape,
                IncludeStructuralColumns = _vm.IncludeColumns,
                IncludeStructuralFraming = _vm.IncludeFraming,
                IncludeWalls = _vm.IncludeWalls,
                IncludeCeilings = _vm.IncludeCeilings,
                ClearanceFeet = CoreUnits.MmToFeet(_vm.ClearanceMarginMm),
                DuctWidthFeet = width,
                DuctHeightFeet = height,
                DiameterFeet = diameter
            };
        }

        private static Connector? ResolveConnector(Document doc, PickedConnector picked)
        {
            Element? owner = doc.GetElement(picked.OwnerId);
            ConnectorSet? connectors = owner is MEPCurve curve
                ? curve.ConnectorManager?.Connectors
                : (owner as FamilyInstance)?.MEPModel?.ConnectorManager?.Connectors;
            if (connectors == null) return null;

            foreach (Connector c in connectors)
            {
                if (c.Id == picked.ConnectorId) return c;
            }
            return null;
        }

        private static string UnitsText(double feet) => $"{CoreUnits.FeetToMm(feet) / 1000.0:F2} m";
    }
}
