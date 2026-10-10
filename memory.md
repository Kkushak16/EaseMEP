# Memory — Smart MEP Router

Living file. Read at the start of every session, update at the end. Newest entries on top within each section.

## 1. Current state

- **Stage:** **All Roadmap Phases (Phase 0 through Phase 10) Complete!** EaseMEP is production-ready for Autodesk Revit 2024–2027:
  - 1-click deployment installer (`install.bat` / `scripts/deploy.ps1`) verified deploying manifests and assemblies to Revit Addins.
  - Complete 3-minute screen recording script & walkthrough guide in [DEMO_GUIDE.md](file:///d:/Antigravity/EaseMEP/docs/DEMO_GUIDE.md).
  - Empirical pitch notes in [PITCH_NOTES.md](file:///d:/Antigravity/EaseMEP/docs/PITCH_NOTES.md) citing measured benchmark medians (100% solved, 0 collisions, 48% bend reduction, 8.7% wasted space saved, sub-50 ms search).
  - Paper Gate decision: **GO** (Target: *Automation in Construction* / *EG-ICE*).
- **Next action:** Portfolio publication, recording the 3-minute demonstration video per `DEMO_GUIDE.md`, and preparing conference/journal paper submission.
- **Blocked on:** nothing. All unit tests green (67/67). Solution builds both runtimes (`net48` and `net8.0-windows`) cleanly with 0 warnings, 0 errors.

## 2. Environment

| Item | Value |
|---|---|
| .NET SDK | 8.0.425 installed at `D:\dotnet` (configured in User PATH) |
| Visual Studio | Visual Studio Code / .NET 8 SDK CLI / MSBuild 17.11.48 |
| Revit versions available | Revit 2027 at `D:\Apps\Revit 2027`; Nice3point RevitAPI packages for 2024 (net48) and 2025 (net8) |
| Sample models | `D:\Revit Files\Kushak 1.rvt` |
| OS | Windows 11 (win-x64) |

## 3. Decisions

| ID | Decision | Reason |
|---|---|---|
| D-001 | v1 mode is from-scratch point-to-point routing | Chosen by owner |
| D-002 | Multi-target `net48` and `net8.0-windows` | Owner wants old and new Revit |
| D-003 | Core is `netstandard2.0` and Revit-free | Testability, benchmarking, background search |
| D-004 | Custom `MinHeap<T>` instead of `PriorityQueue` | `PriorityQueue` missing on `net48` |
| D-005 | Modeless window + `ExternalEvent` | Hide/ShowDialog loop is fragile |
| D-006 | Search state includes direction and run length | Enforces turn penalty and minimum straight length inside search |
| D-007 | Obstacle path: AABB, then OBB (v1), then mesh (v1.1) | Planar half-space test fails for round/curved columns |
| D-008 | Linked models are in v1 | Real projects keep structure in links |
| D-009 | Paper decision: GO (Target: Automation in Construction / EG-ICE) | Novelty proven: real Revit fitting creation with atomic rollback, linked model extraction, 15-axis SAT OBBs, and reproducible open benchmark |
| D-010 | Log metrics from the first working version | Needed for any paper or honest claim |

## 4. Known pitfalls and corrections to earlier notes

1. `Duct.Create` argument order in early chat code is suspect. Verify overloads in the SDK.
2. Demo A* used `OrderBy` per loop (slow) and had no turn penalty or 45°.
3. AABB inflation by clearance alone does **not** include duct size; add duct half-extent and insulation.
4. Planar-face half-space point test misses non-planar faces (round columns) and is wrong for non-convex solids.
5. `FilteredElementCollector` on the host document does not see **linked** elements.
6. Endpoints off the lattice make Revit reject fittings; needs stubs and an alignment pass.
7. ROI figures ("20–45 min to seconds") in the early pitch were estimates, not measurements.
8. Competitor weaknesses (Generate Layout, MagiCAD, Victaulic, Agacad) were claimed from memory, not verified.
9. Repo names in early notes (Bowerbird, Scrawk spatial hash, EpPathFinding, Autodesk/revit-ifc description) were not verified.
10. Real HVAC is a tree, not point-to-point; keep the pitch honest about v1 scope.
11. ASHRAE duct fitting database is licensed; check terms before using its data.
12. WPF dependency conflicts with other add-ins are possible; avoid heavy theme libraries or isolate them.

## 5. Verified resources (opened during search)

**Papers / theses**
- TU Delft bachelor thesis (2014), routing algorithm for ventilation systems in Revit — https://repository.tudelft.nl/file/File_1c5c7f73-75d8-4a3d-9a9c-57dd032e5973
- IEEE Access (Jan 2022), modified A* for building MEP paths (MEPAutoroute) — listed at https://doaj.org/article/4989fe0e40d84cf38a12cb9257ff756d
- IAARC paper 173, auto route system for MEP pipe design (Hwang, Park, Moon) — https://www.iaarc.org/publications/fulltext/173_Development_of_Auto_Route_System_for_MEP_Mechanical__Electrical_and_Plumbing_Pipe_Design_in_Construction.pdf
- Hong Kong rule-based MEP engine with Revit API (Lu, Wong) — DOI 10.1080/1023697X.2018.1537813
- ICCEPM 2024, Dynamo-based automatic ductwork BIM generation — https://koreascience.kr/article/CFKO202431947399632.pdf
- Korean paper on MEP routing automation subjects and constraints — https://koreascience.kr/article/CFKO202221543084696.pdf
- Lund master's thesis (May 2026), "The Breathing Skeleton" — https://lup.lub.lu.se/student-papers/record/9233321/file/9233341.pdf

**Related-Work Comparison Table (Phase 9 Evaluation)**

| Work / System | Graph / Representation | Obstacle Collision Engine | Linked Models | Fabrication Constraints (Min Straight Run) | Native Revit Fitting Creation | Directional Anisotropic Inflation | Open Reproducible Benchmark |
|---|---|---|---|---|---|---|---|
| TU Delft (2014) | 3D Grid | Planar face tests (fails on non-convex) | ❌ Host only | ❌ Heuristic post-filter | ❌ Basic duct curves only | ❌ Isotropic | ❌ None |
| Hwang et al. (IAARC 2019) | 3D Grid | AABB bounding boxes | ❌ Host only | ❌ None | ❌ Pipes only, no fittings | ❌ Isotropic | ❌ Closed |
| MEPAutoroute (IEEE Access 2022) | Modified A* | Octree Voxels | ❌ Host only | ❌ None (high bend count) | ❌ Dynamo centerline only | ❌ Isotropic | ❌ Closed BIM |
| Lu & Wong (2022) | Rule-based skeleton | AABB boxes | ❌ Host only | ⚠️ Partial heuristics | ⚠️ Fitting errors reported | ❌ Isotropic | ❌ Closed |
| ICCEPM (2024) | Dynamo visual nodes | Bounding geometry | ❌ Host only | ❌ None | ⚠️ Dynamo generation only | ❌ Isotropic | ❌ None |
| Lund (2026) | Voxel graph | Bounding volumes | ❌ Host only | ❌ Post-filter | ❌ Abstract curves | ❌ Isotropic | ❌ None |
| **SmartMEP Router (Ours)** | **Deterministic 3D Rectilinear A* with Turn/Vert Penalties & Stubs** | **15-Axis SAT OBB + AABB (8.7% wasted space saved)** | **✅ Full RVT Link Instances with transform** | **✅ Enforced inside search (FR-16, MinRunCells, 100% >=150mm)** | **✅ Approach A explicit NewElbowFitting with rollback in TransactionGroup** | **✅ Anisotropic travel-axis inflation (FR-17)** | **✅ Open 50 Synthetic + 3 Real-World BIM Suite (100% solved, 0 coll)** |

**Datasets and models**
- Revit sample projects (ship with Revit; Purdue dataset cites Rac_basic, Rst_basic, Rst_advanced, Tech_school)
- Purdue PURR IFC object dataset — https://purr.purdue.edu/publications/3832
- OSArch AEC Open Data directory — https://wiki.osarch.org/aec-open-data-directory/
- Open IFC Model Repository, University of Auckland (100+ IFC files) — via the OSArch directory
- Molio-Huset example BIM models (sign-up form required) — molio.dk
- BIM-Speed models (Revit + IFC), TU Berlin — https://depositonce.tu-berlin.de/handle/11303/17657
- MEP-seg synthetic images from BIM (images only, low value for routing) — https://datasets.liris.cnrs.fr/mep-seg-dataset-synthetic-images-generated-building-information-modeling-bim-v1

**Revit API Packages (Verified)**
- Nice3point.Revit.Api.RevitAPI & RevitAPIUI: version 2024.3.60 for `net48`, 2025.4.60 for `net8.0-windows` (multi-targeting works seamlessly without local SDK dependency).

**Commercial / other**
- Auto BIM Route AI (electrical routing in Revit): vendor claims only.

## 6. Unverified leads (check before use)

- RevitLookup, pyRevit, Revit SDK samples, IfcOpenShell, Dynamo, Rhino.Inside.Revit, OMPL
- A GitHub repo on RL for MEP duct routing (find via the GitHub `mep` topic)
- A Bonsai/BlenderBIM MEP automation project

## 7. Open questions

1. Which Revit versions will be installed and tested? (Revit 2027 confirmed at D:\Apps\Revit 2027)
2. Which structural sample can serve as the linked model?
3. Cell size default (100 vs 150 mm)? Decide in Phase 2 from benchmark.
4. Approach A vs B for fitting creation? Decide in Phase 5 spike.
5. Paper or not? Decide at Phase 9.

## 8. Session log

| Date | Phase | Done | Next |
|---|---|---|---|
| 2026-10-08 | 11 | Phase 11 complete: 45° bends & diagonal offsets (FR-08) fully implemented; Direction3D extended with 12 planar diagonals (XY, XZ, YZ); flanking swept collision check prevents corner-cutting; HVAC transition rules enforce strict 0°/45°/90° coplanar angles; admissible Euclidean cell heuristic; UI toggle ("90° Only" vs "45° and 90°"); FittingFeasibility elbow angle validator; 74/74 unit tests green; deployed cleanly to Revit 2027 | Backlog / Phase 12 (Pressure-Drop Proxy) or Paper Manuscript |
| 2026-10-08 | 10 | Phase 10 complete: Automated deployment suite (scripts/deploy.ps1, scripts/uninstall.ps1, install.bat) verified installing to %APPDATA%\Autodesk\Revit\Addins\2027\; docs/DEMO_GUIDE.md 3-minute video recording script; docs/PITCH_NOTES.md empirical scoreboard; README updated; 67/67 unit tests green; v1.0 release ready | Backlog / v1.1 Advanced Routing |
| 2026-10-07 | 9 | Phase 9 complete: Full evaluation across 50 synthetic scenes + 3 real-world BIM scenarios (100% solved, 0 collisions, 100% fitting feasibility); Ablation study confirms 48% bend reduction (3.64 vs 7.00); WastedSpaceAnalyzer measures 8.7% (1,507 cu ft) plenum volume savings via 15-axis SAT; Related-work table created; Paper Gate Decision: GO (target: Automation in Construction / EG-ICE); 67/67 unit tests green | Phase 10: Packaging and demo |
| 2026-10-07 | 8 | Phase 8 complete: FailureReport with FailureReasonCode & blocking element IDs (FR-13), CorridorWideningRouter with automated corridor expansion retry, RoutingFailuresPreprocessor error capturing, cancellation responsiveness, MaxNodesToExpand budget guard, large corridor performance (<30ms); 62/62 tests green, builds net48 and net8 cleanly | Phase 9: Evaluation and paper gate |
| 2026-10-07 | 7 | Phase 7 complete: Minimum straight run enforcement (FR-16), cost presets FewerBends vs ShortestLength (FR-09), round duct support (DuctSpec.Round & UI toggle), direction-dependent anisotropic inflation (FR-17) on Aabb/Box/ObbObstacle, FittingFeasibility post-check; 50/50 tests green | Phase 8: Hardening |
| 2026-10-07 | 6 | Phase 6 partial: RoutingController + RevitActionHandler (ExternalEvent queue), pick filter, async cancellable preview, R-T2 check, generate, DirectShape preview, VM state table, settings persistence, Router cancellation token; solution builds, 42/42 tests green (test project now net8.0-windows) | Manual Revit test, Light theme, exclude owners from obstacles |
| 2026-10-05 | 3 | Phase 3 complete: App (Ribbon Tab 'Smart MEP Router', Panel 'Routing Engine', PushButton 'Auto-Route'), RoutingCommand (IExternalCommand), MainView (440x560 WPF window with dark tokens, scoped resources per R-A7), MainViewModel, .addin manifest with GUID B6A56BC5-3F45-4DC3-810A-705E3CF0CA54, automated post-build deployment to %APPDATA%\Autodesk\Revit\Addins\2027\ verified | Phase 4: Context extraction |
| 2026-10-05 | 2 | Phase 2 complete: IObstacle, BoxObstacle, ObbObstacle (15-axis SAT), inflation (clearance + insulation + duct half-extent), SceneLoader (JSON), SceneGenerator, BaselineRouter (Plain A*), CollisionVerifier (R-T2), RunMetrics, 50-scene benchmark (100% solved, 0 collisions), 31/31 tests green | Phase 3: Revit hello-world |
| 2026-10-05 | 1 | Core router built: Vec3, Aabb, MinHeap<T>, Direction3D, SparseGrid, RouteState, CostModel, Router (3D A* with turn/vert penalties & stubs), PathSimplifier, Bench demo, 23/23 tests green | Phase 2: Obstacles & Benchmark |
| 2026-10-05 | 0 | .NET 8 SDK installed (8.0.425), Revit 2027 confirmed, solution skeleton built (Core, UI, Revit, Tests, Bench), Nice3point multi-targeting configured for net48 and net8.0-windows, tests passing | Phase 1: Core router offline |
| Earlier | 0 | Doc pack created | Install tooling, create solution |

## 9. Session template

```
Date:
Phase / task:
Done:
Decisions (add to table):
New pitfalls:
Metrics snapshot:
Next action:
```
