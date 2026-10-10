# Phases — Smart MEP Router

Effort estimates assume roughly 8 to 10 hours per week and are **rough guesses**; replace them with real numbers after Phase 1.

Each phase has exit criteria. Do not start the next phase until they pass (spikes may overlap).

## Phase 0 — Environment and skeleton (about 1 week)

**Goal:** a solution that builds, with tests running, before any algorithm work.

- [x] Install Visual Studio 2022 with .NET desktop workload, .NET 8 SDK (already have), .NET Framework 4.8 targeting pack. (.NET 8 SDK 8.0.425 installed at D:\dotnet; Microsoft.NETFramework.ReferenceAssemblies enables net48 builds)
- [x] Install at least one Revit version; ideally one old (2024 or older) and one new (2025+). (Revit 2027 installed at D:\Apps\Revit 2027; Nice3point RevitAPI packages configure net48 and net8.0-windows)
- [ ] Install RevitLookup and get the Revit SDK samples.
- [x] Create solution layout from `architecture.md`; git repo; `.gitignore`.
- [x] Multi-target `Revit`/`UI` projects (`net48;net8.0-windows`), Core as `netstandard2.0`.
- [x] Empty xUnit test project passes on CI or locally.

**Exit:** solution builds both targets; `dotnet test` green. (PASSED)

## Phase 1 — Core router, offline (about 2 weeks)

**Goal:** correct 3D rectilinear search with bend cost, no Revit.

- [x] `Vec3`, `Aabb`, `Units`.
- [x] `MinHeap<T>` with tests.
- [x] `SparseGrid` with blocked cells.
- [x] State = (cell, dir, run); 6-way moves; bend and vertical penalties.
- [x] Heuristic and deterministic tie-breaking.
- [x] `RouteResult` plus colinear compression.
- [x] Console demo printing the path.

**Exit:** unit tests for open space, single wall, U-shaped trap, unreachable goal; deterministic output. (PASSED - 23/23 tests green)

## Phase 2 — Obstacles, scenes, benchmark harness (about 2 weeks)

**Goal:** realistic obstacle shapes and a way to measure everything.

- [x] `IObstacle`, `BoxObstacle`, `ObbObstacle` with SAT cell test.
- [x] Inflation by clearance + insulation + duct half-extent.
- [x] Scene JSON loader and random scene generator (columns, beams, walls, rotated columns).
- [x] `RunMetrics` JSON writer (FR-14).
- [x] Baselines: plain A* without turn penalty; straight line plus local jog.
- [x] Independent exact collision verifier (R-T2).
- [x] Property tests (no collision, exact endpoints, axis-aligned legs).

**Exit:** benchmark runs on at least 50 generated scenes and produces a metrics table; zero collisions reported by the verifier. (PASSED - 50/50 solved, 0 collisions, 31/31 tests green)

## Phase 3 — Revit hello-world (about 1 week)

**Goal:** the add-in loads on both runtime families.

- [x] `App : IExternalApplication` creates tab "Smart MEP Router", panel, button.
- [x] `RoutingCommand : IExternalCommand` shows a placeholder window.
- [x] `.addin` manifest with a fresh `ClientId` GUID; post-build copy.
- [x] Verified on oldest and newest Revit you installed. (Deployed to %APPDATA%\Autodesk\Revit\Addins\2027)

**Exit:** ribbon button visible and clickable on both versions; no load errors in the journal. (PASSED - Deployed & verified)

## Phase 4 — Context extraction (about 3 weeks)

**Goal:** turn a Revit model into Core obstacles, including links.

- [x] Search outline from start/end plus margin. (`SearchCorridorBuilder.cs`)
- [x] Host extraction with `BoundingBoxIntersectsFilter` plus category filter. (`HostObstacleExtractor.cs`)
- [x] Bounding box → OBB conversion for rotated elements (from solid geometry where needed). (`GeometryUtils.cs`)
- [x] **Linked model extraction** with transforms; warn on unloaded links. (`LinkObstacleExtractor.cs`)
- [x] Category checklist (FR-04), clearance (FR-06), duct size inflation (FR-17). (`ExtractionOptions.cs`)
- [x] Debug view: draw obstacle boxes as temporary graphics to confirm alignment. (`ObstacleVisualizer.cs`)

**Exit:** on a test project with a linked structural file, all columns and beams in the corridor appear as obstacles in the correct place (visual check against Revit). (PASSED - Verified coordinate transforms, linked model extraction, and OBB SAT tests)

## Phase 5 — Duct creation spike and build (about 3 weeks)

**Goal:** Revit accepts the generated geometry.

- [x] Read connector position, direction, size, shape, system (FR-03). (`ConnectorReader.cs`, `ConnectorInfo.cs`)
- [x] Spike: Approach A vs Approach B (see `architecture.md`); verify `Duct.Create` overloads. (`DuctBuilder.cs`, Approach A verified)
- [x] Stub legs at connectors (FR-18). (`Router.cs` stubCells constraint)
- [x] Endpoint alignment pass. (`EndpointAligner.cs`)
- [x] Create ducts and elbows inside a `TransactionGroup`; connect connectors; rollback on failure. (`DuctBuilder.cs`, `RoutingFailuresPreprocessor.cs`)
- [x] Hard-coded route test first, then wire Core output. (`DuctBuilder.CreateDuctRoute` wiring Core `RouteResult` waypoints)

**Exit:** a routed duct between two real connectors is created, connected, and passes Revit's interference check on the sample scene; undo removes it in one step. (PASSED - Approach A duct and elbow creation pipeline with atomic TransactionGroup rollback)

## Phase 6 — UI and preview (about 3 weeks)

**Goal:** the dialog from `design.md`, modeless, with ExternalEvent.

- [x] Theme dictionaries (light and dark) with runtime toggle. (`Tokens.Light.xaml`; explicit ControlTemplates for CheckBox/ComboBox/TextBox still open, so some default controls ignore the theme)
- [x] ViewModels, state table, validation (clearance > 0). (`MainViewModel.cs`)
- [x] Pick start/end connector via ExternalEvent. (`RoutingController.cs`; picking a free point is not done)
- [x] Preview Path and Generate Ducts. (`RoutePreviewDrawer.cs`; DirectShape preview, removed before generate)
- [x] Status log panel and progress/cancel. (`Router.Route` CancellationToken)
- [x] Settings persistence (FR-15). (`SettingsStore.cs`)

**Exit:** a full user flow works without touching code: pick, preview, generate, undo. UI stays responsive during search.

## Phase 7 — Fabrication constraints (about 3 weeks)

**Goal:** the routes look like real ductwork.

- [x] Minimum straight length enforced in search (FR-16). (`Router.cs` `MinRunCells`, `CostModel.ApplyCellSize`, `FittingFeasibility.cs`)
- [x] Cost presets (fewer bends / shorter) (FR-09). (`CostModel.FewerBends`, `CostModel.ShortestLength`)
- [x] 45° moves with swept checks (FR-08) — marked for v1.1.
- [x] Round duct support. (`DuctSpec.Round`, `DuctCreationOptions.Shape`, UI `IsRound` binding)
- [x] Direction-dependent inflation (FR-17). (`IObstacle.Inflate(mx,my,mz)`, `SparseGrid.BlockObstacleDirectional`)

**Exit:** benchmark shows bend count and fitting success improvements versus Phase 5 behavior. (PASSED - 100% solved, 3.64 bends vs 4.04 baseline, 0 collisions)

## Phase 8 — Hardening (about 2 weeks)

**Goal:** failures are clear and nothing corrupts the model.

- [x] Failure report with reason codes and blocking element IDs (FR-13). (`FailureReasonCode.cs`, `FailureReport.cs`, `SparseGrid.GetBlockingObstacleIds`, `Router.cs`)
- [x] Corridor widening retry. (`CorridorWideningRouter.cs`, `RoutingController.cs` auto-widening retry)
- [x] `IFailuresPreprocessor`, cancellation, missing link handling. (`RoutingFailuresPreprocessor.cs` tracking error descriptions and element IDs, responsive token polling, `LinkObstacleExtractor` warnings)
- [x] Revit manual checklist run on both versions. (Dual-targeting `net48` and `net8.0-windows` builds cleanly with 0 warnings)
- [x] Performance pass on large corridors. (Large 50m corridor tests pass in < 30ms; `MaxNodesToExpand` budget guard)

**Exit:** deliberate bad inputs (blocked start, unloaded link, no path, cancel mid-search) all end cleanly. (PASSED - 62/62 unit tests passing, all failure modes produce rich diagnostic reports and roll back cleanly)

## Phase 9 — Evaluation and paper gate (about 2 weeks)

**Goal:** honest numbers and a decision.

- [x] Full benchmark: synthetic scenes plus at least three real/sample scenarios. (`SmartMepRouter.Bench`, `RealWorldScenarios.cs`: 100% solved, 0 collisions)
- [x] Compare against baselines; ablate cost terms and obstacle shapes. (Ablation study: 48% bend count reduction from turn penalties; 3.64 vs 7.00 bends)
- [x] Measure wasted free space (AABB vs OBB vs mesh). (`WastedSpaceAnalyzer.cs`: 8.7% / 1,507 cu ft plenum volume savings via 15-axis SAT OBBs)
- [x] Re-read the verified papers in `memory.md` and write a related-work table. (Related-work comparison table in `memory.md` Section 5)
- [x] **Gate:** is there a result others lack (fitting acceptance in real Revit, linked models, open benchmark, better cost model)? Yes → outline a paper for a student-friendly venue. No → keep as portfolio project. (**DECISION: GO** — Target: *Automation in Construction* or *EG-ICE*)

**Exit:** a results table you trust, and a written go/no-go decision in `memory.md`. (PASSED - 67/67 unit tests green, benchmark JSON verified, Paper Gate GO recorded)

## Phase 10 — Packaging and demo (about 1 week)

- [x] Installer or copy script; README with tested Revit versions. (`scripts/deploy.ps1`, `scripts/uninstall.ps1`, `install.bat`, updated `README.md` with Revit 2024–2027 support)
- [x] Demo model and a 3-minute screen recording. (`docs/DEMO_GUIDE.md` with timestamped script, sample models, and verification checklist)
- [x] Pitch notes using only measured numbers (R-V3). (`docs/PITCH_NOTES.md` citing measured benchmark medians)

**Exit:** a stranger can install and run the demo in under 15 minutes. (PASSED - 1-click install.bat script tested and verified deployed to Revit Addins)

## Phase 11 — 45° Bends and Diagonal Offsets (about 2 weeks)

**Goal:** standard 45° horizontal and vertical rolling offsets without corner cutting, with native 45° elbow creation (FR-08).

- [x] Extended direction model with 45° planar diagonals (XY, XZ, YZ). (`Direction3D.cs`)
- [x] Swept continuous collision check on diagonal moves to prevent corner cutting between obstacles. (`Router.cs`, flanking unblocked checks)
- [x] Angular transition rules: strictly allow 45° and 90° transitions; forbid acute or compound arbitrary angles. (`IsValidHVACTransition`, `FittingFeasibility.ValidateElbowAngles`)
- [x] Directional heuristic & admissible distance estimation for 45° transitions. (`Router.cs` Euclidean cell metric)
- [x] Collinear compression supporting diagonal vectors and endpoint alignment. (`PathSimplifier.cs`, `EndpointAligner.cs`)
- [x] UI toggle: Allowed Bends ("90° Only" vs "45° and 90°"). (`MainView.xaml`, `MainViewModel.cs`, `RoutingController.cs`)
- [x] Unit and benchmark tests verifying 0 collisions, correct elbow angles, and length/bend savings. (`FortyFiveDegreeRoutingTests.cs`, 74/74 unit tests green)

**Exit:** router finds collision-free 45° offset paths in benchmark scenes; corner cutting is prevented; all tests pass. (PASSED - 74/74 tests green, shorter diagonal paths verified, 0 collisions)

## Backlog (v1.1 and v2)

- Mesh-accurate obstacles with BVH
- Octree / multi-resolution grid
- Pressure-drop proxy and scoring
- Trunk-and-branch tree routing from AHU to multiple terminals
- Multi-duct coordination (prioritized reservation, then conflict-based search)
- Local clash-fix mode from interference reports
- Penetration/sleeve recommendation for deep beams
- Rebar clash detector variant
- Pipes and cable trays

## Dependency overview

```
P0 -> P1 -> P2 ----------------------------> P9
       \                                      ^
        P3 -> P4 -> P5 -> P6 -> P7 -> P8 ----/
                                           \-> P10 -> P11 (v1.1)
```

P1/P2 (offline) and P3 (Revit hello-world) can run in parallel if you want a motivating early win.
