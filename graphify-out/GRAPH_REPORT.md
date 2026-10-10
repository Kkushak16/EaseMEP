# Graph Report - EaseMEP  (2026-10-11)

## Corpus Check
- 108 files · ~50,452 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1224 nodes · 2361 edges · 81 communities (73 shown, 8 thin omitted)
- Extraction: 81% EXTRACTED · 19% INFERRED · 0% AMBIGUOUS · INFERRED: 439 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `fdda6a0a`
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
- ConnectorInfo
- .RoutePlainAStar
- Architecture — Smart MEP Router
- SpatialHash3D
- Aabb
- SceneDto
- ExtractionOptions
- UserSettings
- RoutingController
- Phases — Smart MEP Router
- PRD — Smart MEP Router
- Router
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
- RoutingStatus
- CollisionVerificationResult
- Memory — Smart MEP Router
- .IsPathCollisionFree
- CostModel
- .Extract
- .RoundTrip_PreservesValue
- Rules — Smart MEP Router
- Direction3D
- ExtractionResult
- IObstacle
- .CreatePipeRoute
- Smart MEP Router — Master
- .CompressCollinear
- DuctCreationResult
- RevitActionHandler
- App
- PickedConnector
- Tokens.Light.xaml
- ObstacleDto
- .BoxObstacle_IntersectsCellAndSegment
- .Show
- DuctSpec
- SmartMepRouter.UI.Settings
- RoutingFailuresPreprocessor
- 🎬 3. Three-Minute Demonstration Video Script
- ObbObstacle
- SceneLoader
- EaseMEP — Investor & Customer Pitch Notes
- Tokens.Dark.xaml
- PipeSpec
- .Align
- .RouteWithWidening
- .GenerateScene
- CorridorWideningOptions
- .ResolveDuctType
- PipeCreationOptions
- .ResolveLevel
- CableTrayCreationOptions
- .CreateCableTrayRoute
- .GetConnectors
- CableTraySpec
- MetricsWriter
- ExtractionAndConnectorTests
- .CreateDuctRoute
- ConnectorShape
- .Benchmark_RunsOn50Scenes_VerifiesZeroCollisionsAndAxisAlignedLegs
- MepDiscipline
- .Main

## God Nodes (most connected - your core abstractions)
1. `Vec3` - 132 edges
2. `Aabb` - 77 edges
3. `SparseGrid` - 68 edges
4. `SmartMepRouter.Core.Geometry` - 52 edges
5. `Router` - 48 edges
6. `MainViewModel` - 46 edges
7. `BoxObstacle` - 39 edges
8. `SmartMepRouter.Core.Spatial` - 38 edges
9. `Direction3D` - 37 edges
10. `Window` - 35 edges

## Surprising Connections (you probably didn't know these)
- `RouterTests` --references--> `Aabb`  [EXTRACTED]
  tests/SmartMepRouter.Core.Tests/RouterTests.cs → src/SmartMepRouter.Core/Geometry/Aabb.cs
- `FabricationConstraintsTests` --references--> `Aabb`  [EXTRACTED]
  tests/SmartMepRouter.Core.Tests/FabricationConstraintsTests.cs → src/SmartMepRouter.Core/Geometry/Aabb.cs
- `Window` --references--> `MainViewModel`  [INFERRED]
  src/SmartMepRouter.UI/Views/MainView.xaml → src/SmartMepRouter.UI/ViewModels/MainViewModel.cs
- `ConnectorInfo` --references--> `ConnectorShape`  [EXTRACTED]
  src/SmartMepRouter.Core/Connectors/ConnectorInfo.cs → src/SmartMepRouter.Core/Connectors/ConnectorShape.cs
- `ConnectorInfo` --references--> `MepDiscipline`  [EXTRACTED]
  src/SmartMepRouter.Core/Connectors/ConnectorInfo.cs → src/SmartMepRouter.Core/Connectors/MepDiscipline.cs

## Import Cycles
- None detected.

## Communities (81 total, 8 thin omitted)

### Community 0 - "SparseGrid"
Cohesion: 0.11
Nodes (21): HashSet, CancellationToken, IReadOnlyList, GridCell, X, Y, Z, Dictionary (+13 more)

### Community 1 - "SmartMepRouter.Core.Geometry"
Cohesion: 0.07
Nodes (14): SmartMepRouter.Core.Metrics, SmartMepRouter.Core.Tests, SmartMepRouter.Core.Connectors, SmartMepRouter.Bench, SmartMepRouter.Core.Post, SmartMepRouter.Core.Scenes, SmartMepRouter.Revit.Extraction, SmartMepRouter.Core.Geometry (+6 more)

### Community 2 - "MainViewModel"
Cohesion: 0.05
Nodes (35): INotifyPropertyChanged, MainViewModel, Allow45DegreeBends, Banner, CanGenerate, CanPick, CanPreview, ClearanceError (+27 more)

### Community 3 - "Design — Smart MEP Router"
Cohesion: 0.07
Nodes (26): 10. Figma setup (optional, for the portfolio case study), 11. What not to do, 1. What the reference design is doing, 2. Window frame, 3.1 Dark, 3.2 Light, 3.3 Contrast notes, 3.4 Mapping from the reference light palette (+18 more)

### Community 4 - "SmartMepRouter.Revit.csproj"
Cohesion: 0.09
Nodes (21): net8.0, netstandard2.0, coverlet.collector (6.0.0), Microsoft.NET.Test.Sdk (17.8.0), Nice3point.Revit.Api.RevitAPI (2024.3.60), Nice3point.Revit.Api.RevitAPIUI (2024.3.60), System.Text.Json (8.0.5), xunit (2.6.5) (+13 more)

### Community 5 - "Window"
Cohesion: 0.06
Nodes (30): Banner, CanGenerate, CanPreview, ClearanceError, ClearanceMarginMm, EndConnectorText, GenerateButtonText, HasBanner (+22 more)

### Community 6 - "RunMetrics"
Cohesion: 0.11
Nodes (19): DateTime, RunMetrics, Algorithm, BendCount, BlockedCells, CellSizeFeet, CollisionCount, NodesExpanded (+11 more)

### Community 7 - "RouteResult"
Cohesion: 0.15
Nodes (13): FailureReport, IReadOnlyList, RouteResult, BendCount, BlockingElementIds, IsSuccess, LengthFeet, Message (+5 more)

### Community 8 - "ConnectorInfo"
Cohesion: 0.17
Nodes (12): ConnectorInfo, DiameterFeet, Direction, Discipline, HeightFeet, Id, OwnerElementId, Position (+4 more)

### Community 9 - ".RoutePlainAStar"
Cohesion: 0.11
Nodes (18): IComparer, InvalidOperationException, PlainNode, SearchNode, BaselineRouter, PlainNode, Cell, F (+10 more)

### Community 10 - "Architecture — Smart MEP Router"
Cohesion: 0.11
Nodes (17): 10. Preview, 11. Threading and events, 12. Settings and logging, 13. Error taxonomy, 14. Build and versioning, 15. Testing, 16. Performance notes, 1. Principles (+9 more)

### Community 11 - "SpatialHash3D"
Cohesion: 0.07
Nodes (25): IEquatable, NodeRecord, G, ParentIndex, State, SearchNode, F, G (+17 more)

### Community 12 - "Aabb"
Cohesion: 0.14
Nodes (10): CancellationTokenSource, Aabb, Center, Extents, Max, Min, Size, Volume (+2 more)

### Community 13 - "SceneDto"
Cohesion: 0.11
Nodes (20): List, BoundsDto, Max, Min, DuctDto, Clearance, H, Insulation (+12 more)

### Community 14 - "ExtractionOptions"
Cohesion: 0.10
Nodes (21): ElementId, ISet, ExtractionOptions, ClearanceFeet, CorridorMarginFeet, DiameterFeet, DuctHeightFeet, DuctWidthFeet (+13 more)

### Community 15 - "UserSettings"
Cohesion: 0.12
Nodes (14): CancelEventArgs, SettingsStore, UserSettings, ClearanceMarginMm, Discipline, IncludeCeilings, IncludeColumns, IncludeFraming (+6 more)

### Community 16 - "RoutingController"
Cohesion: 0.19
Nodes (8): ExternalEvent, Action, Connector, Document, IReadOnlyList, UIApplication, RoutingController, Task

### Community 17 - "Phases — Smart MEP Router"
Cohesion: 0.12
Nodes (16): Backlog (v1.2 and v2), Dependency overview, Phase 0 — Environment and skeleton (about 1 week), Phase 10 — Packaging and demo (about 1 week), Phase 11 — 45° Bends and Diagonal Offsets (about 2 weeks), Phase 12 — Multi-Discipline MEP: Pipes & Cable Trays (v1.1), Phase 1 — Core router, offline (about 2 weeks), Phase 2 — Obstacles, scenes, benchmark harness (about 2 weeks) (+8 more)

### Community 18 - "PRD — Smart MEP Router"
Cohesion: 0.13
Nodes (14): 10. Competitive and research context, 11. Risks, 12. Assumptions, 13. Acceptance criteria for v1, 1. Summary, 2. Problem, 3. Users, 4. Goals and non-goals (+6 more)

### Community 19 - "Router"
Cohesion: 0.28
Nodes (7): CorridorWideningRouter, Router, BoxObstacle, Bounds, Id, Fact, HardeningAndFailureTests

### Community 20 - "EaseMEP — Smart MEP Router for Autodesk Revit"
Cohesion: 0.12
Nodes (15): 1-Click Install (Revit 2024–2027), 📊 Benchmark Scoreboard, 🛠️ Build & Rapid Installation, 📖 Documentation & Guides, EaseMEP — Smart MEP Router for Autodesk Revit, ✨ Key Features, 📜 License & Compliance, Manual Build Solution (+7 more)

### Community 21 - "SceneDefinition"
Cohesion: 0.17
Nodes (12): IReadOnlyList, RealWorldScenarios, List, SceneDefinition, CorridorBounds, Duct, EndDir, EndPos (+4 more)

### Community 22 - "FailureReasonCode"
Cohesion: 0.07
Nodes (28): FailureReasonCode, Cancelled, CollisionCheckFailed, ConnectorMismatch, CorridorLimitReached, EndBlocked, EndStubBlocked, FittingFeasibilityFailed (+20 more)

### Community 23 - ".Execute"
Cohesion: 0.20
Nodes (8): SmartMepRouter.Revit.Commands, SmartMepRouter.UI.Views, ElementSet, ExternalCommandData, IExternalCommand, MainView, Result, RoutingCommand

### Community 24 - "Vec3"
Cohesion: 0.17
Nodes (6): Vec3, X, Y, Z, Fact, GeometryTests

### Community 25 - "SpaceMetrics"
Cohesion: 0.15
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

### Community 30 - "RoutingStatus"
Cohesion: 0.17
Nodes (10): RoutingStatus, Cancelled, ConnectorMismatch, EndBlocked, FittingFailed, NoPath, StartBlocked, Success (+2 more)

### Community 31 - "CollisionVerificationResult"
Cohesion: 0.33
Nodes (5): List, CollisionVerificationResult, CollidingObstacleIds, Details, HasCollisions

### Community 32 - "Memory — Smart MEP Router"
Cohesion: 0.18
Nodes (10): 1. Current state, 2. Environment, 3. Decisions, 4. Known pitfalls and corrections to earlier notes, 5. Verified resources (opened during search), 6. Unverified leads (check before use), 7. Open questions, 8. Session log (+2 more)

### Community 33 - ".IsPathCollisionFree"
Cohesion: 0.25
Nodes (5): IEnumerable, IReadOnlyList, CollisionVerifier, Fact, FortyFiveDegreeRoutingTests

### Community 34 - "CostModel"
Cohesion: 0.11
Nodes (16): CostModel, Allow45DegreeBends, Default, EndStubCells, EndStubLengthFeet, FewerBends, MaxNodesToExpand, MinRunCells (+8 more)

### Community 35 - ".Extract"
Cohesion: 0.25
Nodes (7): Document, XYZ, ObstacleExtractor, Outline, Transform, XYZ, SearchCorridorBuilder

### Community 36 - ".RoundTrip_PreservesValue"
Cohesion: 0.28
Nodes (4): InlineData, Fact, UnitsTests, Theory

### Community 37 - "Rules — Smart MEP Router"
Cohesion: 0.20
Nodes (9): A. Architecture rules, B. Coding rules (C#), C. Verification rules (anti-hallucination), D. Testing rules, E. Git and workflow, F. UI rules, G. Definition of done (per task), H. Instructions for AI assistants (+1 more)

### Community 38 - "Direction3D"
Cohesion: 0.06
Nodes (32): dx, dy, dz, f1, f2, IReadOnlyList, FittingFeasibility, FittingFeasibilityResult (+24 more)

### Community 39 - "ExtractionResult"
Cohesion: 0.20
Nodes (10): IReadOnlyList, ExtractionResult, CorridorBounds, Duration, HostObstacleCount, LinkObstacleCount, Obstacles, TotalObstacleCount (+2 more)

### Community 40 - "IObstacle"
Cohesion: 0.11
Nodes (14): BuiltInCategory, ICollection, RevitLinkInstance, IObstacle, Bounds, Id, Document, List (+6 more)

### Community 41 - ".CreatePipeRoute"
Cohesion: 0.15
Nodes (13): Pipe, PipeType, PipingSystemType, Connector, Document, IReadOnlyList, XYZ, PipeBuilder (+5 more)

### Community 42 - "Smart MEP Router — Master"
Cohesion: 0.22
Nodes (8): 1. One-line summary, 2. Status, 3. Document map, 4. Scope, 5. Glossary, 6. Open questions, 7. How to start every work session, Smart MEP Router — Master

### Community 43 - ".CompressCollinear"
Cohesion: 0.31
Nodes (5): IReadOnlyList, List, PathSimplifier, Fact, PathSimplifierTests

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

### Community 51 - ".Show"
Cohesion: 0.52
Nodes (4): Document, ElementId, IReadOnlyList, RoutePreviewDrawer

### Community 52 - "DuctSpec"
Cohesion: 0.20
Nodes (10): DuctSpec, ClearanceAndInsulationFeet, ClearanceFeet, DiameterFeet, HeightFeet, InsulationFeet, IsRound, Shape (+2 more)

### Community 54 - "RoutingFailuresPreprocessor"
Cohesion: 0.18
Nodes (9): FailureProcessingResult, FailuresAccessor, IFailuresPreprocessor, ElementId, List, RoutingFailuresPreprocessor, DismissedWarnings, ErrorDescriptions (+1 more)

### Community 55 - "🎬 3. Three-Minute Demonstration Video Script"
Cohesion: 0.14
Nodes (13): 1-Click Install, ⚡ 1. Rapid Installation (Under 60 Seconds), 🏢 2. Sample Model Preparation, 🎬 3. Three-Minute Demonstration Video Script, 🔍 4. Verification Checklist, EaseMEP — Demo Walkthrough & Video Script Guide, Prerequisites, Segment 1: Introduction & Non-Modal UI (`0:00 – 0:30`) (+5 more)

### Community 56 - "ObbObstacle"
Cohesion: 0.18
Nodes (8): ObbObstacle, AxisX, AxisY, AxisZ, Bounds, Center, HalfExtents, Id

### Community 57 - "SceneLoader"
Cohesion: 0.23
Nodes (4): JsonSerializerOptions, SceneLoader, Fact, SceneTests

### Community 58 - "EaseMEP — Investor & Customer Pitch Notes"
Cohesion: 0.17
Nodes (11): 1. In-Search Fabrication Enforcement (FR-16, FR-18), 2. 15-Axis SAT Oriented Bounding Boxes (FR-04), 3. Linked Model Coordinate Awareness (FR-05), 4. Anisotropic Travel-Axis Inflation (FR-17), 5. Atomic Rollback & Error Transparency (FR-11, FR-13), 🚀 Commercial Readiness, EaseMEP — Investor & Customer Pitch Notes, 🎯 Executive Summary (+3 more)

### Community 60 - "PipeSpec"
Cohesion: 0.20
Nodes (8): PipeSpec, ClearanceAndInsulationFeet, ClearanceFeet, DiameterFeet, InsulationFeet, TotalInflationFeet, Fact, MultiDisciplineRoutingTests

### Community 61 - ".Align"
Cohesion: 0.31
Nodes (5): IReadOnlyList, List, EndpointAligner, Fact, EndpointAlignerTests

### Community 63 - ".RouteWithWidening"
Cohesion: 0.24
Nodes (5): CancellationToken, IReadOnlyList, FailureReport, Fact, EvaluationAndAblationTests

### Community 66 - "CorridorWideningOptions"
Cohesion: 0.50
Nodes (4): CorridorWideningOptions, EnableWideningOnNoPath, ExpansionMarginFeet, MaxRetries

### Community 67 - ".ResolveDuctType"
Cohesion: 0.27
Nodes (7): DuctType, MEPSystemType, Connector, Document, ElementId, Level, DuctSystemResolver

### Community 68 - "PipeCreationOptions"
Cohesion: 0.20
Nodes (10): Connector, ElementId, PipeCreationOptions, DiameterFeet, EndTerminalConnector, LevelId, PipeTypeId, Slope (+2 more)

### Community 69 - ".ResolveLevel"
Cohesion: 0.28
Nodes (6): CableTrayType, Connector, Document, ElementId, Level, CableTraySystemResolver

### Community 70 - "CableTrayCreationOptions"
Cohesion: 0.22
Nodes (9): Connector, ElementId, CableTrayCreationOptions, CableTrayTypeId, EndTerminalConnector, HeightFeet, LevelId, StartTerminalConnector (+1 more)

### Community 71 - ".CreateCableTrayRoute"
Cohesion: 0.29
Nodes (6): CableTray, Connector, Document, IReadOnlyList, XYZ, CableTrayBuilder

### Community 72 - ".GetConnectors"
Cohesion: 0.31
Nodes (6): ConnectorSet, Connector, Element, List, XYZ, ConnectorReader

### Community 73 - "CableTraySpec"
Cohesion: 0.33
Nodes (5): CableTraySpec, ClearanceFeet, HeightFeet, TotalInflationFeet, WidthFeet

### Community 76 - ".CreateDuctRoute"
Cohesion: 0.33
Nodes (5): Connector, Document, IReadOnlyList, XYZ, DuctBuilder

### Community 77 - "ConnectorShape"
Cohesion: 0.33
Nodes (5): ConnectorShape, Other, Oval, Rectangular, Round

### Community 78 - ".Benchmark_RunsOn50Scenes_VerifiesZeroCollisionsAndAxisAlignedLegs"
Cohesion: 0.50
Nodes (3): ITestOutputHelper, Fact, PropertyAndBenchmarkTests

### Community 79 - "MepDiscipline"
Cohesion: 0.40
Nodes (4): MepDiscipline, CableTray, Duct, Pipe

## Knowledge Gaps
- **507 isolated node(s):** `Id`, `Position`, `Direction`, `Shape`, `WidthFeet` (+502 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **8 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Vec3` connect `Vec3` to `SparseGrid`, `RouteResult`, `ConnectorInfo`, `.RoutePlainAStar`, `SpatialHash3D`, `Aabb`, `SceneDto`, `RoutingController`, `Router`, `SceneDefinition`, `FailureReasonCode`, `SpaceMetrics`, `.CreateObstacleFromElement`, `CollisionVerificationResult`, `.IsPathCollisionFree`, `.Extract`, `Direction3D`, `IObstacle`, `.CreatePipeRoute`, `.CompressCollinear`, `.BoxObstacle_IntersectsCellAndSegment`, `.Show`, `ObbObstacle`, `SceneLoader`, `PipeSpec`, `.Align`, `.RouteWithWidening`, `.GenerateScene`, `.CreateCableTrayRoute`, `ExtractionAndConnectorTests`, `.CreateDuctRoute`?**
  _High betweenness centrality (0.353) - this node is a cross-community bridge._
- **Why does `RoutingController` connect `RoutingController` to `SmartMepRouter.Core.Geometry`, `MainViewModel`, `.RoundTrip_PreservesValue`, `Aabb`, `RevitActionHandler`, `PickedConnector`, `.Show`, `.Execute`, `Vec3`, `MainView`?**
  _High betweenness centrality (0.158) - this node is a cross-community bridge._
- **Why does `MainViewModel` connect `MainViewModel` to `Window`, `UserSettings`, `RoutingController`, `MepDiscipline`, `SmartMepRouter.UI.Settings`, `MainView`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._
- **Are the 62 inferred relationships involving `Vec3` (e.g. with `.CreateCongestedPlenumScenario()` and `.CreateMechanicalRoomScenario()`) actually correct?**
  _`Vec3` has 62 INFERRED edges - model-reasoned connections that need verification._
- **Are the 32 inferred relationships involving `Aabb` (e.g. with `.CreateCongestedPlenumScenario()` and `.CreateMechanicalRoomScenario()`) actually correct?**
  _`Aabb` has 32 INFERRED edges - model-reasoned connections that need verification._
- **Are the 37 inferred relationships involving `SparseGrid` (e.g. with `.RouteWithWidening()` and `.Main()`) actually correct?**
  _`SparseGrid` has 37 INFERRED edges - model-reasoned connections that need verification._
- **Are the 39 inferred relationships involving `Router` (e.g. with `.StartPreview()` and `.Main()`) actually correct?**
  _`Router` has 39 INFERRED edges - model-reasoned connections that need verification._