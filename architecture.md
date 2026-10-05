# Architecture — Smart MEP Router

## 1. Principles

1. **Core is Revit-free.** All search and geometry live in a plain class library that can be unit-tested and benchmarked without Revit.
2. **Revit is an adapter.** Extraction and creation are thin layers around Core.
3. **One source tree, two runtimes.** `net48` (Revit 2024 and older) and `net8.0-windows` (Revit 2025+).
4. **Everything measurable.** Every run emits metrics.
5. **Conservative geometry.** When unsure, block the cell. Measure the cost of being conservative.

## 2. Solution layout

```
SmartMepRouter.sln
src/
  SmartMepRouter.Core/            netstandard2.0, no Revit references
    Geometry/    Vec3, Aabb, Obb, Segment, Units
    Spatial/     IObstacle, BoxObstacle, ObbObstacle, MeshObstacle (later),
                 SparseGrid, SpatialHash
    Search/      MinHeap<T>, RouteState, CostModel, Router, RouteResult
    Post/        PathSimplifier, EndpointAligner, FittingValidator
    Metrics/     RunMetrics, MetricsWriter (JSON)
  SmartMepRouter.Revit/           net48;net8.0-windows, references Revit API
    App.cs                        IExternalApplication (ribbon)
    Commands/                     IExternalCommand entry
    Extraction/                   host + link obstacle readers, connector reader
    Build/                        duct + fitting creation, transactions
    Preview/                      temporary graphics
    Events/                       ExternalEvent handlers
    Compat/                       #if wrappers for version differences
  SmartMepRouter.UI/              net48;net8.0-windows, WPF, ViewModels
tests/
  SmartMepRouter.Core.Tests/      net8.0, xUnit
  SmartMepRouter.Bench/           net8.0 console, scene runner
scenes/                           JSON benchmark scenes
addin/                            .addin manifest templates
```

**Why `netstandard2.0` for Core:** it loads in both runtimes. Consequence: `PriorityQueue<,>` is **not available** on `net48`. Use the project's own `MinHeap<T>`.

## 3. End-to-end pipeline

```
[UI: user picks] -> [API thread: read context] -> [Core DTO snapshot]
      -> [Build grid + mark obstacles] -> [Search] -> [Post-process]
      -> [Preview (API thread)] -> [User confirms]
      -> [API thread: Transaction -> ducts -> fittings -> connect]
      -> [Metrics + status]
```

- **Read on the API thread:** `FilteredElementCollector`, geometry, connectors.
- **Search can run on a background thread** because Core holds only plain data.
- **Write on the API thread** through an `ExternalEvent`. The window is **modeless**; do not use the Hide / ShowDialog loop.

## 4. Coordinates and units

- Core uses **Revit internal units (decimal feet)**. No conversion inside Core.
- Conversion happens only at the UI boundary (mm in the dialog) via `Units`.
- Unit API differs by version (`UnitTypeId` since 2021; older `DisplayUnitType`). Wrap in `Compat/`.
- Tolerances: read `Application.ShortCurveTolerance` (about 0.0026 ft; verify) and use it as the floor for segment length and endpoint matching.

## 5. Obstacle model

```csharp
interface IObstacle {
    Aabb Bounds { get; }
    bool Intersects(Aabb cell);        // conservative
}
```

| Stage | Shape | Notes |
|---|---|---|
| v1 | `BoxObstacle` (AABB) | Fast, wasteful for rotated elements |
| v1 | `ObbObstacle` | From the solid's oriented extent; SAT test against cell boxes |
| v1.1 | `MeshObstacle` | Triangulated solid (`Face.Triangulate()` or solid tessellation) with a BVH, triangle-box SAT |

**Not used:** the planar half-space test from early notes. It is valid only for convex solids with planar faces and misses round columns.

**Inflation (FR-17):**
`inflate = clearance + insulation + duct half-extent`.
v1 uses an isotropic conservative value `max(width, height) / 2`. Later: direction-dependent inflation (horizontal run uses `width/2` in plan and `height/2` in elevation), implemented as separate blocked sets per axis.

**Sources of obstacles**
1. Host document: `BoundingBoxIntersectsFilter` plus category filter, `WhereElementIsNotElementType()`.
2. **Linked documents:** for each `RevitLinkInstance`, get `GetLinkDocument()` (null if not loaded: warn), collect inside the search outline transformed into link space, transform geometry with the link's total transform.

## 6. Grid

- **v1: corridor-limited sparse grid.** A dictionary or hash set of blocked cells in a bounding corridor around start/end plus margin and plenum height range. No full-floor dense array.
- Cell size: default about 100 to 150 mm, user-adjustable. Connector positions drive grid origin: **origin = start connector position** so the start is a lattice point.
- On failure, widen the corridor and retry once or twice before reporting `NoPath`.
- **Later:** octree / multi-resolution (fine near structure, coarse in open space).

## 7. Search

**State:** `(x, y, z, dir, run)` where `dir` is the incoming direction and `run` is straight cells since the last turn, capped at `minRunCells`.

- v1 directions: 6 axis directions. v1.1 adds in-plane and vertical 45° directions (with swept checks to prevent corner cutting).
- A turn is allowed only when `run >= minRunCells`. This enforces the minimum straight length **inside the search**, not as an afterthought.
- Start: the first move must follow the start connector direction for a stub of at least `stubCells`. End: arrive along the reverse of the end connector direction with a stub.

**Cost:**
`g = wLen * length + wBend * bends + wVert * verticalChanges + wNear * proximity`
(`wNear` optional, discourages hugging structure.)

**Heuristic:** `wLen * Manhattan(distance to goal)`. This is admissible and consistent for 6-way moves, so the first goal pop is optimal under the model.

**Presets:** "Fewer bends" raises `wBend`; "Shorter length" lowers it.

**Output:** ordered corner list plus search statistics (nodes expanded, time).

## 8. Post-processing

1. **Colinear compression** to corner vertices.
2. **Endpoint alignment:** the first and last legs must pass exactly through the connector positions. Fix the free coordinates of intermediate corners to match, then re-check collisions. If the re-check fails, retry search with an adjusted lattice offset.
3. **Fitting feasibility check:** each leg length at least the fitting requirement plus tolerance. Because the search enforces `minRunCells`, this is a safety net, not the main mechanism.
4. **Collision verification** of the final polyline against inflated obstacles with an exact segment test (not coarse sampling).

## 9. Revit creation

Spike in Phase 5, then pick one:

- **Approach A (explicit):** create each duct, then create each fitting by connecting the nearest connector pair (`Document.Create.NewElbowFitting`).
- **Approach B (routing preferences):** create ducts using connector-based overloads and let Revit place fittings per the duct type's routing preferences.

> The `Duct.Create` call in early notes used an argument order that must be verified. Check the actual overloads in the Revit SDK / RevitLookup / API docs for your target versions before coding.

Transaction structure:

```
TransactionGroup "Smart MEP Router"
  Transaction "Create ducts"
  Transaction "Create fittings"
  on any error: RollBack the group
```

Add an `IFailuresPreprocessor` that dismisses warnings and aborts on errors.

## 10. Preview

- Options: `DirectContext3D` server, or `TemporaryGraphicsManager` where available (availability depends on Revit version; verify). Fallback: temporary model lines created and rolled back.
- The preview shows the polyline, bend markers and the clearance zone. No database changes.

## 11. Threading and events

- Window is modeless. Buttons raise an `ExternalEvent` with a request object.
- Handler executes on the API thread, returns a result object, UI updates through `Dispatcher`.
- Long search runs on a worker thread with a `CancellationToken`. Handler for building waits for the finished `RouteResult`.

## 12. Settings and logging

- Settings: JSON at `%AppData%\SmartMepRouter\settings.json`.
- Metrics: one JSON per run at `%AppData%\SmartMepRouter\runs\`.
- Metrics schema (minimum): run id, Revit version, scene id, grid size, blocked cells, nodes expanded, time (extract, grid, search, post, build), path length, bend count, success flag, failure code.

## 13. Error taxonomy

`NoPath`, `StartBlocked`, `EndBlocked`, `ConnectorMismatch`, `LinkNotLoaded`, `FittingFailed`, `TransactionFailed`, `Cancelled`, `UnsupportedGeometry`.

Every code maps to a user-facing message in the status log.

## 14. Build and versioning

- Multi-target projects. Per-version Revit API references via configuration (for example `Debug R24`, `Debug R25`) or community NuGet packages for the Revit API (check which exist for your versions).
- Post-build step copies the DLLs and `.addin` to `%AppData%\Autodesk\Revit\Addins\<year>\`.
- Reference `RevitAPI.dll` and `RevitAPIUI.dll` with Copy Local = false.
- Dependency conflicts with other add-ins in the same Revit process are possible; keep third-party dependencies minimal and scope WPF resources to the window, not `Application.Resources`.

## 15. Testing

| Level | What |
|---|---|
| Unit | Geometry, heap, cost, state transitions |
| Property | Random scenes: path never enters inflated obstacle; endpoints exact; all legs axis-aligned (v1); all legs at least min length |
| Golden | Fixed JSON scenes with expected bend count and length |
| Benchmark | Baseline comparison on metrics from `prd.md` |
| Revit manual | Checklist on sample models: generate, interference check, undo, link missing, cancel |

**Scene JSON (sketch):**

```json
{
  "id": "room-01",
  "units": "feet",
  "start": { "pos": [0,0,9], "dir": [1,0,0] },
  "end":   { "pos": [40,12,9], "dir": [-1,0,0] },
  "duct":  { "w": 1.0, "h": 0.67, "insulation": 0.08 },
  "obstacles": [ { "type": "obb", "center": [20,6,9], "half": [1,1,5], "yaw": 0.6 } ]
}
```

## 16. Performance notes

- Only voxelize the corridor; clip obstacle iteration with a spatial hash.
- Cache inflated obstacle bounds per run.
- Avoid per-cell allocations in the search loop; use struct states and a pooled heap.
- Targets are set after the first benchmark, not before.
