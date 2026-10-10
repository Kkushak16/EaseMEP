# Graph Report - EaseMEP  (2026-10-10)

## Corpus Check
- 96 files · ~45,637 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1102 nodes · 2116 edges · 68 communities (62 shown, 6 thin omitted)
- Extraction: 81% EXTRACTED · 19% INFERRED · 0% AMBIGUOUS · INFERRED: 398 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `6116d67c`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- SparseGrid
- SmartMepRouter.Core.Geometry
- MainViewModel
- Design — Smart MEP Router
- SmartMepRouter.Revit.csproj
- Window
- RunMetrics
- RouteResult
- ObbObstacle
- MinHeap
- Architecture — Smart MEP Router
- RouteState
- Aabb
- SceneDto
- ExtractionOptions
- UserSettings
- RoutingController
- Phases — Smart MEP Router
- PRD — Smart MEP Router
- .Route
- EaseMEP — Smart MEP Router for Autodesk Revit
- SceneDefinition
- FailureReasonCode
- .Execute
- Vec3
- SpaceMetrics
- .CreateObstacleFromElement
- DuctCreationOptions
- MainView
- .VisualizeObstacles
- .ResolveDuctType
- .Verify
- Memory — Smart MEP Router
- .Verify
- CostModel
- .Extract
- .MmToFeet
- Rules — Smart MEP Router
- Direction3D
- ExtractionResult
- IObstacle
- .CreateDuctRoute
- Smart MEP Router — Master
- .RoutePlainAStar
- DuctCreationResult
- RevitActionHandler
- App
- PickedConnector
- Tokens.Light.xaml
- ObstacleDto
- BoxObstacle
- .Show
- DuctSpec
- SmartMepRouter.UI.Settings
- RoutingFailuresPreprocessor
- 🎬 3. Three-Minute Demonstration Video Script
- ConnectorShape
- SceneLoader
- EaseMEP — Investor & Customer Pitch Notes
- Tokens.Dark.xaml
- DuctDto
- .RunAblationStudy
- .BlockObstacleDirectional
- CorridorWideningOptions
- PlainNode
- .SceneLoader_RoundTripJson_PreservesAllProperties

## God Nodes (most connected - your core abstractions)
1. `Vec3` - 123 edges
2. `Aabb` - 69 edges
3. `SparseGrid` - 66 edges
4. `SmartMepRouter.Core.Geometry` - 47 edges
5. `Router` - 46 edges
6. `MainViewModel` - 40 edges
7. `Direction3D` - 37 edges
8. `BoxObstacle` - 36 edges
9. `SmartMepRouter.Core.Spatial` - 35 edges
10. `GridCell` - 32 edges

## Surprising Connections (you probably didn't know these)
- `RouterTests` --references--> `Aabb`  [EXTRACTED]
  tests/SmartMepRouter.Core.Tests/RouterTests.cs → src/SmartMepRouter.Core/Geometry/Aabb.cs
- `FabricationConstraintsTests` --references--> `Aabb`  [EXTRACTED]
  tests/SmartMepRouter.Core.Tests/FabricationConstraintsTests.cs → src/SmartMepRouter.Core/Geometry/Aabb.cs
- `Window` --references--> `MainViewModel`  [INFERRED]
  src/SmartMepRouter.UI/Views/MainView.xaml → src/SmartMepRouter.UI/ViewModels/MainViewModel.cs
- `ConnectorInfo` --references--> `ConnectorShape`  [EXTRACTED]
  src/SmartMepRouter.Core/Connectors/ConnectorInfo.cs → src/SmartMepRouter.Core/Connectors/ConnectorShape.cs
- `ConnectorInfo` --references--> `Vec3`  [EXTRACTED]
  src/SmartMepRouter.Core/Connectors/ConnectorInfo.cs → src/SmartMepRouter.Core/Geometry/Vec3.cs

## Import Cycles
- None detected.

## Communities (68 total, 6 thin omitted)

### Community 0 - "SparseGrid"
Cohesion: 0.11
Nodes (20): HashSet, IEquatable, IReadOnlyList, GridCell, X, Y, Z, IReadOnlyList (+12 more)

### Community 1 - "SmartMepRouter.Core.Geometry"
Cohesion: 0.07
Nodes (14): SmartMepRouter.Core.Metrics, SmartMepRouter.Core.Tests, SmartMepRouter.Core.Connectors, SmartMepRouter.Bench, SmartMepRouter.Core.Post, SmartMepRouter.Core.Scenes, SmartMepRouter.Revit.Extraction, SmartMepRouter.Core.Geometry (+6 more)

### Community 2 - "MainViewModel"
Cohesion: 0.06
Nodes (30): INotifyPropertyChanged, MainViewModel, Allow45DegreeBends, Banner, CanGenerate, CanPick, CanPreview, ClearanceError (+22 more)

### Community 3 - "Design — Smart MEP Router"
Cohesion: 0.07
Nodes (26): 10. Figma setup (optional, for the portfolio case study), 11. What not to do, 1. What the reference design is doing, 2. Window frame, 3.1 Dark, 3.2 Light, 3.3 Contrast notes, 3.4 Mapping from the reference light palette (+18 more)

### Community 4 - "SmartMepRouter.Revit.csproj"
Cohesion: 0.09
Nodes (21): net8.0, netstandard2.0, coverlet.collector (6.0.0), Microsoft.NET.Test.Sdk (17.8.0), Nice3point.Revit.Api.RevitAPI (2024.3.60), Nice3point.Revit.Api.RevitAPIUI (2024.3.60), System.Text.Json (8.0.5), xunit (2.6.5) (+13 more)

### Community 5 - "Window"
Cohesion: 0.07
Nodes (26): Banner, CanGenerate, CanPreview, ClearanceError, ClearanceMarginMm, EndConnectorText, HasBanner, HasClearanceError (+18 more)

### Community 6 - "RunMetrics"
Cohesion: 0.09
Nodes (21): DateTime, JsonSerializerOptions, MetricsWriter, RunMetrics, Algorithm, BendCount, BlockedCells, CellSizeFeet (+13 more)

### Community 7 - "RouteResult"
Cohesion: 0.09
Nodes (23): FailureReport, IReadOnlyList, RouteResult, BendCount, BlockingElementIds, IsSuccess, LengthFeet, Message (+15 more)

### Community 8 - "ObbObstacle"
Cohesion: 0.07
Nodes (27): ConnectorSet, ConnectorInfo, DiameterFeet, Direction, HeightFeet, Id, OwnerElementId, Position (+19 more)

### Community 9 - "MinHeap"
Cohesion: 0.23
Nodes (7): InvalidOperationException, IComparer, MinHeap, Capacity, Count, Fact, MinHeapTests

### Community 10 - "Architecture — Smart MEP Router"
Cohesion: 0.11
Nodes (17): 10. Preview, 11. Threading and events, 12. Settings and logging, 13. Error taxonomy, 14. Build and versioning, 15. Testing, 16. Performance notes, 1. Principles (+9 more)

### Community 11 - "RouteState"
Cohesion: 0.12
Nodes (14): NodeRecord, G, ParentIndex, State, SearchNode, F, G, NodeIndex (+6 more)

### Community 12 - "Aabb"
Cohesion: 0.15
Nodes (9): Aabb, Center, Extents, Max, Min, Size, Volume, Fact (+1 more)

### Community 13 - "SceneDto"
Cohesion: 0.14
Nodes (15): List, BoundsDto, Max, Min, EndpointDto, Dir, Pos, SceneDto (+7 more)

### Community 14 - "ExtractionOptions"
Cohesion: 0.10
Nodes (21): ISet, ElementId, ExtractionOptions, ClearanceFeet, CorridorMarginFeet, DiameterFeet, DuctHeightFeet, DuctWidthFeet (+13 more)

### Community 15 - "UserSettings"
Cohesion: 0.13
Nodes (13): CancelEventArgs, SettingsStore, UserSettings, ClearanceMarginMm, IncludeCeilings, IncludeColumns, IncludeFraming, IncludeWalls (+5 more)

### Community 16 - "RoutingController"
Cohesion: 0.20
Nodes (9): CancellationTokenSource, ExternalEvent, Action, Connector, Document, IReadOnlyList, UIApplication, RoutingController (+1 more)

### Community 17 - "Phases — Smart MEP Router"
Cohesion: 0.12
Nodes (15): Backlog (v1.1 and v2), Dependency overview, Phase 0 — Environment and skeleton (about 1 week), Phase 10 — Packaging and demo (about 1 week), Phase 11 — 45° Bends and Diagonal Offsets (about 2 weeks), Phase 1 — Core router, offline (about 2 weeks), Phase 2 — Obstacles, scenes, benchmark harness (about 2 weeks), Phase 3 — Revit hello-world (about 1 week) (+7 more)

### Community 18 - "PRD — Smart MEP Router"
Cohesion: 0.13
Nodes (14): 10. Competitive and research context, 11. Risks, 12. Assumptions, 13. Acceptance criteria for v1, 1. Summary, 2. Problem, 3. Users, 4. Goals and non-goals (+6 more)

### Community 19 - ".Route"
Cohesion: 0.23
Nodes (8): CancellationToken, IReadOnlyList, CorridorWideningRouter, CancellationToken, Router, FailureReport, Fact, HardeningAndFailureTests

### Community 20 - "EaseMEP — Smart MEP Router for Autodesk Revit"
Cohesion: 0.12
Nodes (15): 1-Click Install (Revit 2024–2027), 📊 Benchmark Scoreboard, 🛠️ Build & Rapid Installation, 📖 Documentation & Guides, EaseMEP — Smart MEP Router for Autodesk Revit, ✨ Key Features, 📜 License & Compliance, Manual Build Solution (+7 more)

### Community 21 - "SceneDefinition"
Cohesion: 0.18
Nodes (10): List, SceneDefinition, CorridorBounds, Duct, EndDir, EndPos, Id, Obstacles (+2 more)

### Community 22 - "FailureReasonCode"
Cohesion: 0.07
Nodes (28): FailureReasonCode, Cancelled, CollisionCheckFailed, ConnectorMismatch, CorridorLimitReached, EndBlocked, EndStubBlocked, FittingFeasibilityFailed (+20 more)

### Community 23 - ".Execute"
Cohesion: 0.20
Nodes (8): SmartMepRouter.Revit.Commands, SmartMepRouter.UI.Views, ElementSet, ExternalCommandData, IExternalCommand, MainView, Result, RoutingCommand

### Community 24 - "Vec3"
Cohesion: 0.12
Nodes (6): Vec3, X, Y, Z, Fact, GeometryTests

### Community 25 - "SpaceMetrics"
Cohesion: 0.17
Nodes (11): SpaceMetrics, IEnumerable, SpaceMetrics, AabbVolumeCuFt, AverageOverApproximationRatio, ExactVolumeCuFt, ObstacleCount, RotatedObstacleCount (+3 more)

### Community 26 - ".CreateObstacleFromElement"
Cohesion: 0.20
Nodes (10): BoundingBoxXYZ, FamilyInstance, GeometryElement, Options, Solid, Element, List, Transform (+2 more)

### Community 27 - "DuctCreationOptions"
Cohesion: 0.17
Nodes (12): Connector, ElementId, DuctCreationOptions, DiameterFeet, DuctTypeId, EndTerminalConnector, HeightFeet, LevelId (+4 more)

### Community 28 - "MainView"
Cohesion: 0.23
Nodes (7): CanPick, RoutedEventArgs, PickEndButton, PickStartButton, MainView, ViewModel, Button

### Community 29 - ".VisualizeObstacles"
Cohesion: 0.36
Nodes (7): Curve, Document, ElementId, IReadOnlyList, List, XYZ, ObstacleVisualizer

### Community 30 - ".ResolveDuctType"
Cohesion: 0.27
Nodes (7): DuctType, Level, MEPSystemType, Connector, Document, ElementId, DuctSystemResolver

### Community 31 - ".Verify"
Cohesion: 0.09
Nodes (18): IReadOnlyList, List, EndpointAligner, IReadOnlyList, List, PathSimplifier, IEnumerable, IReadOnlyList (+10 more)

### Community 32 - "Memory — Smart MEP Router"
Cohesion: 0.18
Nodes (10): 1. Current state, 2. Environment, 3. Decisions, 4. Known pitfalls and corrections to earlier notes, 5. Verified resources (opened during search), 6. Unverified leads (check before use), 7. Open questions, 8. Session log (+2 more)

### Community 33 - ".Verify"
Cohesion: 0.32
Nodes (6): IReadOnlyList, FittingFeasibility, FittingFeasibilityResult, IsFeasible, ShortestSegmentLengthFeet, Violations

### Community 34 - "CostModel"
Cohesion: 0.11
Nodes (16): CostModel, Allow45DegreeBends, Default, EndStubCells, EndStubLengthFeet, FewerBends, MaxNodesToExpand, MinRunCells (+8 more)

### Community 35 - ".Extract"
Cohesion: 0.25
Nodes (7): Document, XYZ, ObstacleExtractor, Outline, Transform, XYZ, SearchCorridorBuilder

### Community 36 - ".MmToFeet"
Cohesion: 0.24
Nodes (4): InlineData, Fact, UnitsTests, Theory

### Community 37 - "Rules — Smart MEP Router"
Cohesion: 0.20
Nodes (9): A. Architecture rules, B. Coding rules (C#), C. Verification rules (anti-hallucination), D. Testing rules, E. Git and workflow, F. UI rules, G. Definition of done (per task), H. Instructions for AI assistants (+1 more)

### Community 38 - "Direction3D"
Cohesion: 0.08
Nodes (26): dx, dy, dz, f1, f2, Direction3D, NegativeX, NegativeY (+18 more)

### Community 39 - "ExtractionResult"
Cohesion: 0.20
Nodes (10): IReadOnlyList, ExtractionResult, CorridorBounds, Duration, HostObstacleCount, LinkObstacleCount, Obstacles, TotalObstacleCount (+2 more)

### Community 40 - "IObstacle"
Cohesion: 0.11
Nodes (14): BuiltInCategory, ICollection, RevitLinkInstance, IObstacle, Bounds, Id, Document, List (+6 more)

### Community 41 - ".CreateDuctRoute"
Cohesion: 0.29
Nodes (6): Duct, Connector, Document, IReadOnlyList, XYZ, DuctBuilder

### Community 42 - "Smart MEP Router — Master"
Cohesion: 0.22
Nodes (8): 1. One-line summary, 2. Status, 3. Document map, 4. Scope, 5. Glossary, 6. Open questions, 7. How to start every work session, Smart MEP Router — Master

### Community 43 - ".RoutePlainAStar"
Cohesion: 0.22
Nodes (7): Dictionary, ITestOutputHelper, List, SceneGenerator, BaselineRouter, Fact, PropertyAndBenchmarkTests

### Community 44 - "DuctCreationResult"
Cohesion: 0.21
Nodes (11): ElementId, IReadOnlyList, DuctCreationResult, DuctIds, ErrorDescriptions, ErrorMessage, FailingElementIds, FittingIds (+3 more)

### Community 45 - "RevitActionHandler"
Cohesion: 0.36
Nodes (5): ConcurrentQueue, IExternalEventHandler, Action, UIApplication, RevitActionHandler

### Community 46 - "App"
Cohesion: 0.32
Nodes (5): SmartMepRouter.Revit, IExternalApplication, Result, App, UIControlledApplication

### Community 47 - "PickedConnector"
Cohesion: 0.12
Nodes (12): SmartMepRouter.Revit.Controller, ISelectionFilter, Reference, Element, XYZ, ConnectorPickFilter, ElementId, PickedConnector (+4 more)

### Community 49 - "ObstacleDto"
Cohesion: 0.25
Nodes (8): ObstacleDto, Center, Half, Id, Max, Min, Type, Yaw

### Community 50 - "BoxObstacle"
Cohesion: 0.16
Nodes (7): BoxObstacle, Bounds, Id, Fact, FortyFiveDegreeRoutingTests, Fact, ObstacleTests

### Community 51 - ".Show"
Cohesion: 0.52
Nodes (4): Document, ElementId, IReadOnlyList, RoutePreviewDrawer

### Community 52 - "DuctSpec"
Cohesion: 0.20
Nodes (10): DuctSpec, ClearanceAndInsulationFeet, ClearanceFeet, DiameterFeet, HeightFeet, InsulationFeet, IsRound, Shape (+2 more)

### Community 54 - "RoutingFailuresPreprocessor"
Cohesion: 0.20
Nodes (9): FailureProcessingResult, FailuresAccessor, IFailuresPreprocessor, ElementId, List, RoutingFailuresPreprocessor, DismissedWarnings, ErrorDescriptions (+1 more)

### Community 55 - "🎬 3. Three-Minute Demonstration Video Script"
Cohesion: 0.14
Nodes (13): 1-Click Install, ⚡ 1. Rapid Installation (Under 60 Seconds), 🏢 2. Sample Model Preparation, 🎬 3. Three-Minute Demonstration Video Script, 🔍 4. Verification Checklist, EaseMEP — Demo Walkthrough & Video Script Guide, Prerequisites, Segment 1: Introduction & Non-Modal UI (`0:00 – 0:30`) (+5 more)

### Community 56 - "ConnectorShape"
Cohesion: 0.33
Nodes (5): ConnectorShape, Other, Oval, Rectangular, Round

### Community 58 - "EaseMEP — Investor & Customer Pitch Notes"
Cohesion: 0.17
Nodes (11): 1. In-Search Fabrication Enforcement (FR-16, FR-18), 2. 15-Axis SAT Oriented Bounding Boxes (FR-04), 3. Linked Model Coordinate Awareness (FR-05), 4. Anisotropic Travel-Axis Inflation (FR-17), 5. Atomic Rollback & Error Transparency (FR-11, FR-13), 🚀 Commercial Readiness, EaseMEP — Investor & Customer Pitch Notes, 🎯 Executive Summary (+3 more)

### Community 60 - "DuctDto"
Cohesion: 0.40
Nodes (5): DuctDto, Clearance, H, Insulation, W

### Community 63 - ".BlockObstacleDirectional"
Cohesion: 0.23
Nodes (4): IReadOnlyList, RealWorldScenarios, Fact, EvaluationAndAblationTests

### Community 66 - "CorridorWideningOptions"
Cohesion: 0.50
Nodes (4): CorridorWideningOptions, EnableWideningOnNoPath, ExpansionMarginFeet, MaxRetries

### Community 67 - "PlainNode"
Cohesion: 0.18
Nodes (10): IComparer, PlainNode, SearchNode, PlainNode, Cell, F, G, Seq (+2 more)

## Knowledge Gaps
- **468 isolated node(s):** `Id`, `Position`, `Direction`, `Shape`, `WidthFeet` (+463 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **6 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Vec3` connect `Vec3` to `SparseGrid`, `SmartMepRouter.Core.Geometry`, `RouteResult`, `ObbObstacle`, `Aabb`, `UserSettings`, `RoutingController`, `.Route`, `SceneDefinition`, `FailureReasonCode`, `.CreateObstacleFromElement`, `.Verify`, `.Verify`, `.Extract`, `Direction3D`, `IObstacle`, `.CreateDuctRoute`, `.RoutePlainAStar`, `BoxObstacle`, `.Show`, `SceneLoader`, `.BlockObstacleDirectional`, `.SceneLoader_RoundTripJson_PreservesAllProperties`?**
  _High betweenness centrality (0.335) - this node is a cross-community bridge._
- **Why does `RoutingController` connect `RoutingController` to `SmartMepRouter.Core.Geometry`, `MainViewModel`, `.MmToFeet`, `RevitActionHandler`, `PickedConnector`, `.Show`, `.Execute`, `Vec3`, `MainView`?**
  _High betweenness centrality (0.171) - this node is a cross-community bridge._
- **Why does `MainViewModel` connect `MainViewModel` to `Window`, `UserSettings`, `RoutingController`, `SmartMepRouter.UI.Settings`, `MainView`?**
  _High betweenness centrality (0.093) - this node is a cross-community bridge._
- **Are the 57 inferred relationships involving `Vec3` (e.g. with `.CreateCongestedPlenumScenario()` and `.CreateMechanicalRoomScenario()`) actually correct?**
  _`Vec3` has 57 INFERRED edges - model-reasoned connections that need verification._
- **Are the 28 inferred relationships involving `Aabb` (e.g. with `.CreateCongestedPlenumScenario()` and `.CreateMechanicalRoomScenario()`) actually correct?**
  _`Aabb` has 28 INFERRED edges - model-reasoned connections that need verification._
- **Are the 35 inferred relationships involving `SparseGrid` (e.g. with `.RouteWithWidening()` and `.Main()`) actually correct?**
  _`SparseGrid` has 35 INFERRED edges - model-reasoned connections that need verification._
- **Are the 37 inferred relationships involving `Router` (e.g. with `.StartPreview()` and `.Main()`) actually correct?**
  _`Router` has 37 INFERRED edges - model-reasoned connections that need verification._