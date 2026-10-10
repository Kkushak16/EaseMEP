# Smart MEP Router — Master

> Working name. Change freely; update `ClientId`, namespaces and the `.addin` file when you do.

## 1. One-line summary

A compiled C# add-in for Autodesk Revit that, given a start connector and an end connector, computes a collision-free, fabrication-aware duct route through the structural/MEP obstacle space and creates native, connected Revit ducts and fittings.

## 2. Status

| Item | Value |
|---|---|
| Stage | Phase 10 complete (v1.0 packaged and ready, 1-click deployment to Revit 2024–2027, 67/67 tests green, Paper Gate GO) |
| Type | Open-source / research paper target (Automation in Construction / EG-ICE) |
| v1 mode | **From-scratch routing**, point-to-point, single duct, single level |
| Revit targets | Old (.NET Framework 4.8, Revit 2024 and older) **and** new (.NET 8, Revit 2025–2027) |
| Dev machine | .NET 8 SDK (8.0.425) installed. Revit 2027 installed at `D:\Apps\Revit 2027` |

## 3. Document map

| File | Purpose | Read when |
|---|---|---|
| `master.md` | Index, scope, glossary | Always first |
| `prd.md` | What we build and why, requirements, acceptance | Planning features |
| `architecture.md` | How it is built: projects, algorithms, Revit integration | Writing code |
| `rules.md` | Coding rules, verification rules, definition of done | Every session |
| `phases.md` | Ordered build plan with exit criteria | Choosing next task |
| `memory.md` | Living state: decisions, pitfalls, resources, next action | Start and end of every session |
| `design.md` | UI/visual spec, tokens, WPF/Figma guidance | Building UI |

**Precedence if documents disagree:** `rules.md` > `architecture.md` > `prd.md` > `phases.md` > `master.md`. Log any resolved conflict in `memory.md`.

## 4. Scope

**In v1**
- Pick start and end connectors (or points) in the active model.
- Obstacles: structural columns, structural framing, walls, floors, existing ducts/pipes, **including elements in linked Revit models**.
- Rectangular duct, 90° bends, minimum straight length between fittings, user clearance, duct cross-section included in clearance.
- Non-destructive preview, then one-transaction generation with rollback on failure.
- Metrics log (JSON) for every run.

**Later (v1.1+)**
- 45° offsets, round duct, pressure-drop score, octree grid, mesh-accurate obstacles.

**v2 (not now)**
- Trunk-and-branch tree routing, multi-duct coordination, local clash-fix mode, rebar clash detector variant.

**Never in scope**
- Replacing the engineer's sizing and pressure calculations. The tool routes; it does not design the system.

## 5. Glossary

| Term | Meaning |
|---|---|
| MEP | Mechanical, Electrical, Plumbing |
| AHU | Air handling unit (duct source) |
| Connector | Revit port on an MEP element with position, direction, size, system |
| Fitting | Elbow, tee, transition between ducts |
| Plenum | Space above ceiling where ducts run |
| AABB / OBB | Axis-aligned / oriented bounding box |
| Voxel / cell | One cell of the discretized 3D grid |
| State | Search node = (cell, incoming direction, run length) |
| Stub | Mandatory straight leg leaving/entering a connector |
| Link | Another Revit file referenced into the host model |
| Core | The Revit-free router library |

## 6. Open questions

1. Which Revit version(s) can you install for testing (one old, one new)? Check Autodesk education access.
2. Do you have an MEP sample model with air terminals and an AHU, plus a structural model to link?
3. Paper or no paper: decide at the Phase 9 gate, not before.
4. Final product name and GUID.

## 7. How to start every work session

1. Read `memory.md` (state, next action).
2. Read the relevant phase in `phases.md`.
3. Obey `rules.md`.
4. End by updating `memory.md`.
