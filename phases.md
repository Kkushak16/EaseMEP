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

- [ ] Theme dictionaries (light and dark), control styles.
- [ ] ViewModels, commands, validation.
- [ ] Pick start/end (connector or point) via ExternalEvent.
- [ ] Preview Path (non-destructive) and Generate Ducts.
- [ ] Status log panel and progress/cancel.
- [ ] Settings persistence (FR-15).

**Exit:** a full user flow works without touching code: pick, preview, generate, undo. UI stays responsive during search.

## Phase 7 — Fabrication constraints (about 3 weeks)

**Goal:** the routes look like real ductwork.

- [ ] Minimum straight length enforced in search (FR-16).
- [ ] Cost presets (fewer bends / shorter) (FR-09).
- [ ] 45° moves with swept checks (FR-08) — mark as v1.1 if it slips.
- [ ] Round duct support if time allows.
- [ ] Direction-dependent inflation.

**Exit:** benchmark shows bend count and fitting success improvements versus Phase 5 behavior.

## Phase 8 — Hardening (about 2 weeks)

**Goal:** failures are clear and nothing corrupts the model.

- [ ] Failure report with reason codes and blocking element IDs (FR-13).
- [ ] Corridor widening retry.
- [ ] `IFailuresPreprocessor`, cancellation, missing link handling.
- [ ] Revit manual checklist run on both versions.
- [ ] Performance pass on large corridors.

**Exit:** deliberate bad inputs (blocked start, unloaded link, no path, cancel mid-search) all end cleanly.

## Phase 9 — Evaluation and paper gate (about 2 weeks)

**Goal:** honest numbers and a decision.

- [ ] Full benchmark: synthetic scenes plus at least three real/sample scenarios.
- [ ] Compare against baselines; ablate cost terms and obstacle shapes.
- [ ] Measure wasted free space (AABB vs OBB vs mesh).
- [ ] Re-read the verified papers in `memory.md` and write a related-work table.
- [ ] **Gate:** is there a result others lack (fitting acceptance in real Revit, linked models, open benchmark, better cost model)? Yes → outline a paper for a student-friendly venue. No → keep as portfolio project.

**Exit:** a results table you trust, and a written go/no-go decision in `memory.md`.

## Phase 10 — Packaging and demo (about 1 week)

- [ ] Installer or copy script; README with tested Revit versions.
- [ ] Demo model and a 3-minute screen recording.
- [ ] Pitch notes using only measured numbers (R-V3).

**Exit:** a stranger can install and run the demo in under 15 minutes.

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
                                           \-> P10
```

P1/P2 (offline) and P3 (Revit hello-world) can run in parallel if you want a motivating early win.
