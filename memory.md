# Memory — Smart MEP Router

Living file. Read at the start of every session, update at the end. Newest entries on top within each section.

## 1. Current state

- **Stage:** Phase 5 complete (Duct.Create overloads verified, Approach A explicit duct & elbow fitting generation with NewElbowFitting, endpoint alignment with collision re-check, single TransactionGroup with full rollback on failure, IFailuresPreprocessor warning suppression, 40/40 tests green, deployed to Revit 2027); ready for Phase 6.
- **Next action:** Phase 6 — UI and preview (connect modeless dialog via ExternalEvent, user pick start/end, non-destructive preview with temporary graphics, progress/cancel, status log).
- **Blocked on:** nothing. All unit tests green (40/40). Benchmark 50/50 solved (100%). Solution builds both runtimes cleanly with 0 warnings.

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
| D-009 | Paper decision deferred to Phase 9 gate | Topic is studied already; novelty must be proven |
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
