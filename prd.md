# PRD — Smart MEP Router

## 1. Summary

Revit users route ducts by hand: draw straight segments, add elbows, avoid columns, beams and other trades, then fix clashes later. Smart MEP Router automates the first pass. The user picks two connectors; the add-in finds a clearance-respecting route, shows a preview, and builds native connected ducts and fittings.

## 2. Problem

- Manual routing is slow and repetitive, and structure often changes after MEP is drawn.
- Clashes are found late (coordination reviews) and fixed by hand.
- Constraints are layered: hard (structure), soft (preferred height, ceiling zone, maintenance access), engineering (fitting angles, minimum straight lengths, duct size plus insulation).
- Revit's own auto-layout is reported by practitioners as weak for coordination. **This is an unverified claim; confirm on your Revit version before using it in any pitch.**

## 3. Users

| Persona | Need |
|---|---|
| MEP modeler | Fast first-pass route that is already clear of structure |
| BIM coordinator | Fewer clash cycles, predictable output |
| Reviewer / evaluator (college, paper) | Reproducible method, metrics, honest limits |

## 4. Goals and non-goals

**Goals (v1)**
1. Route one duct between two connectors with zero collisions against selected obstacle categories (host and linked models).
2. Output native Revit ducts and fittings, connected, in one undoable operation.
3. Make results measurable (metrics log per run).
4. Work on one old (.NET Framework) and one new (.NET 8) Revit version.

**Non-goals (v1)**
- System-wide routing, tree routing, multi-duct coordination.
- Duct sizing, pressure calculation, energy analysis.
- Rebar, pipes, cable trays (design so they can be added).
- Cloud features, accounts, licensing.

## 5. User stories

- As a modeler, I pick an AHU outlet and a diffuser connector and get a preview of a clear route.
- As a modeler, I set clearance and allowed bends and see how they change the route.
- As a modeler, I click Generate and get connected ducts and fittings, and I can undo in one step.
- As a coordinator, when no route exists, I am told which obstacle blocks it and what to relax.
- As a developer, every run writes metrics so I can compare algorithm versions.

## 6. Functional requirements

| ID | Requirement | Phase |
|---|---|---|
| FR-01 | Ribbon tab "Smart MEP Router" with an Auto-Route button | 3 |
| FR-02 | Pick start and end: connector of an MEP element, or a point | 6 |
| FR-03 | Read connector position, direction (BasisZ), size, shape, system type | 5 |
| FR-04 | Obstacle category checklist: columns, framing, walls, floors, existing ducts/pipes, ceilings (optional) | 4 |
| FR-05 | "Include linked models" option, default on; warn if a link is not loaded | 4 |
| FR-06 | Clearance margin in mm | 4 |
| FR-07 | Duct type and system type chosen from connector/default; rectangular first, round in v1.1 | 5 |
| FR-08 | Allowed bends: 90° only (v1); 45° and 90° (v1.1) | 7 |
| FR-09 | Optimization preset: fewer bends / shorter length | 7 |
| FR-10 | Preview path non-destructively before generating | 6 |
| FR-11 | Generate: ducts, fittings, connections in one transaction, full rollback on failure | 5 |
| FR-12 | Status log: time, segment count, fitting count, result, warnings | 6 |
| FR-13 | Failure report: reason code and the likely blocking element IDs | 8 |
| FR-14 | JSON metrics file per run | 2 |
| FR-15 | Persist user settings between sessions | 6 |
| FR-16 | Minimum straight length between fittings enforced | 7 |
| FR-17 | Duct width/height and insulation added to clearance | 4 |
| FR-18 | Route leaves and enters connectors along their direction (stub) | 5 |

## 7. Non-functional requirements

- **Correctness:** a generated route must never intersect an included obstacle (conservative margin). Verified by Revit interference check on test models.
- **Responsiveness:** typical single-room route targets seconds, not minutes. Targets are calibrated in Phase 9; do not publish numbers before measuring.
- **Stability:** a failed run must leave the model unchanged.
- **UI thread:** Revit API calls only on the API thread; search may run on a background thread (Core has no Revit references).
- **Compatibility:** multi-target `net48` and `net8.0-windows`; one source tree.
- **Accessibility:** WCAG-minded contrast and keyboard operation in the dialog (see `design.md`).

## 8. UX

Defined in `design.md`. Baseline layout is the approved mockup: three numbered sections (Connectors, Obstacle Filtering, Routing Geometry Preferences) and a footer with Preview Path and Generate Ducts.

## 9. Success metrics (computed by the benchmark harness)

| Metric | Meaning |
|---|---|
| Success rate | Scenes where a valid route is found / scenes where one exists |
| Collision count | Must be 0 |
| Bend count | Fewer is better |
| Path length vs optimal | Ratio to a reference shortest rectilinear path |
| Wasted free space | Free volume wrongly blocked by conservative obstacle shapes |
| Search time and grid build time | Seconds |
| Fitting success rate | Fittings Revit accepts / fittings attempted |
| Revit interference check | Zero interferences on generated ducts |

## 10. Competitive and research context

Verified by search (read before claiming novelty):
- TU Delft bachelor thesis (2014), routing algorithm for ventilation systems in Revit.
- IEEE Access (2022), modified A* for MEP paths (MEPAutoroute).
- IAARC paper on auto-route for MEP piping (Hwang, Park, Moon).
- Hong Kong rule-based MEP engine using the Revit API (ceiling-zone analysis, then routing).
- ICCEPM 2024, Dynamo-based ductwork generation.
- Lund master's thesis (2026), generative routing with Manhattan MST.
- Commercial: Auto BIM Route AI (electrical branch/feeder routing in Revit). Its time-saving figures are the vendor's own claim.

**Unverified (from earlier notes, check before citing):** the capabilities and gaps of MagiCAD, Victaulic tools, Agacad tools and Revit "Generate Layout".

**Differentiation hypotheses** (each must be proven by benchmark):
1. Fabrication-aware output that Revit accepts (fitting success rate).
2. Linked-model obstacle handling on realistic projects.
3. Open benchmark and metrics.
4. Cost model combining bends, minimum segment length, and later pressure proxy.

## 11. Risks

| Risk | Mitigation |
|---|---|
| Revit rejects fittings for tiny misalignments | Endpoint alignment pass, tolerance from `Application.ShortCurveTolerance`, spike in Phase 5 |
| Linked models missed | Dedicated link extraction, test with a linked structural file |
| Conservative boxes block valid gaps | Add OBB then mesh obstacles; measure wasted space |
| Grid memory blow-up | Corridor-limited sparse grid first, octree later |
| API differences across versions | `#if` isolation layer, test on oldest and newest |
| Scope creep (trees, multi-duct) | Strict phase gates in `phases.md` |
| Inflated claims | Rule R-V3 in `rules.md` |

## 12. Assumptions

- A Windows machine with at least one Revit version installed.
- Sample models are available (Revit ships sample projects; open IFC/Revit datasets are listed in `memory.md`).
- Single level, ceiling plenum routing for v1.

## 13. Acceptance criteria for v1

1. On the synthetic benchmark set, zero collisions, success rate reported against baselines.
2. On at least three real Revit sample scenarios (including one with a linked structure), generated ducts pass Revit's interference check.
3. A failed route changes nothing in the model and reports a reason.
4. Works on the oldest and newest Revit versions you tested, with those versions listed in the README.
5. Metrics JSON produced for every run.
