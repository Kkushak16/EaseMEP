# EaseMEP — Smart MEP Router for Autodesk Revit

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Autodesk%20Revit%202024--2027-blue?style=for-the-badge&logo=autodesk" alt="Revit 2024-2027" />
  <img src="https://img.shields.io/badge/.NET%20Framework-4.8%20%7C%208.0-purple?style=for-the-badge&logo=dotnet" alt=".NET 4.8 / 8.0" />
  <img src="https://img.shields.io/badge/Core%20Tests-82%20Passed%20(100%25)-success?style=for-the-badge&logo=checkmarx" alt="Tests 82 Passed" />
  <img src="https://img.shields.io/badge/Benchmark%20Success-53%2F53%20(100%25)-brightgreen?style=for-the-badge" alt="Benchmark 100%" />
  <img src="https://img.shields.io/badge/Collisions-0-brightgreen?style=for-the-badge" alt="0 Collisions" />
  <img src="https://img.shields.io/badge/Paper%20Gate-GO%20(Auton%20in%20Constr)-orange?style=for-the-badge" alt="Paper Gate GO" />
</p>

<p align="center">
  <img src="docs/images/easemep_ui.png" alt="EaseMEP UI - Smart MEP Router Interface" width="92%" />
</p>

---

## 🚀 Overview

**EaseMEP (Smart MEP Router)** is a high-performance compiled C# add-in for **Autodesk Revit** that automatically computes and generates collision-free, fabrication-aware duct routes through complex structural and MEP spatial environments.

Given a start connector (e.g. an AHU discharge) and an end connector (e.g. an air terminal), EaseMEP analyzes the host model and all linked structural/architectural models, plans an optimal rectilinear route respecting fabrication constraints, and instantiates **native, connected Revit ducts and elbow fittings** in a single undoable transaction.

---

## ✨ Key Features

- ⚡ **Fabrication-Aware 3D A\* Search Engine**
  - Custom zero-allocation `MinHeap<T>` priority queue compatible with both `.NET 4.8` and `.NET 8`.
  - Enforces mandatory straight run lengths between fittings (`minRunCells`) and connector stubs (`stubCells`) **directly inside the state space**, preventing unbuildable micro-segments.
  - Collinear compression and 90° bend minimization cost model.

- 🛡️ **Zero-Collision Geometric Guarantee**
  - Dual spatial representation supporting **Axis-Aligned Bounding Boxes (`BoxObstacle`)** and exact **15-Axis Separating Axis Theorem (SAT) Oriented Bounding Boxes (`ObbObstacle`)** for rotated framing, columns, and angled walls.
  - Strict conservative duct & insulation inflation ($clearance + insulation + \frac{\max(w,h)}{2}$) per **FR-17**.
  - Independent segment-level collision verification engine (Rule **R-T2**).

- 🔗 **Full Linked Model Obstacle Coordination**
  - Deep inspection of `RevitLinkInstance` documents with automatic coordinate space mapping (`linkTransform.Inverse`).
  - Seamless handling of rotated or translated structural links without spatial distortion.
  - Diagnostic warnings for unloaded or missing linked models (**FR-05**).

- 🔄 **Atomic Transaction Safety & Undo**
  - All geometry and connectivity changes are wrapped inside an outer `TransactionGroup` with automated rollback on failure (**Rule R-A5**).
  - Integrated `IFailuresPreprocessor` suppressing non-fatal Revit warnings while rolling back cleanly on unrecoverable errors.
  - A failed route never leaves orphaned elements in the user's model.

- 🎯 **Endpoint Alignment & Connector Snapping**
  - Snaps discrete lattice waypoints to exact floating-point connector ports while preserving strict 90° orthogonality and re-verifying obstacle clearance.

- 🏗️ **Multi-Discipline MEP Generation (Phase 12 / v1.1)**
  - Full native generation for **HVAC Ducts**, **Mechanical / Plumbing Piping**, and **Electrical Cable Trays**.
  - Automatic discipline identification from Revit connector domains (`DomainHvac`, `DomainPiping`, `DomainCableTrayConduit`).
  - Native Revit elbow fitting insertion across all three disciplines in an atomic transaction.

- 📊 **Built-In Scientific Benchmark Suite**
  - Headless 50-scene synthetic benchmark testing maze-like obstacles, tight structural gaps, and rotated OBBs.
  - Produces structured JSON run metrics (`RunMetrics.cs`) comparing against plain rectilinear A*.

---

## 🏛️ System Architecture

```mermaid
flowchart TD
    subgraph Revit_UI [Revit UI Layer]
        A[Pick Start & End Connectors] --> B[Modeless Dialog WPF]
    end

    subgraph Revit_Extraction [Context Extraction - API Thread]
        B -->|ExternalEvent| C[SearchCorridorBuilder]
        C --> D[HostObstacleExtractor]
        C --> E[LinkObstacleExtractor]
        D --> F[GeometryUtils: Solids to OBB / Box]
        E --> F
        F --> G[ExtractionResult]
    end

    subgraph Core_Engine [SmartMepRouter.Core - Thread-Safe & Revit-Free]
        G --> H[SparseGrid Voxelization]
        H --> I[Deterministic 3D A* Search]
        I --> J[PathSimplifier: Collinear Compression]
        J --> K[EndpointAligner: Snapping & Verification]
        K --> L[CollisionVerifier: R-T2 Exact SAT Check]
    end

    subgraph Revit_Creation [Revit Duct Creation - API Thread]
        L --> M[DuctBuilder: Approach A]
        M --> N[Create Straight Ducts: Duct.Create]
        N --> O[Create Elbow Fittings: NewElbowFitting]
        O --> P[Connect Terminal Ports]
        P --> Q[Commit TransactionGroup]
    end
```

---

## 📁 Repository Structure

```
EaseMEP/
├── addin/
│   └── SmartMepRouter.addin          # Revit add-in manifest
├── src/
│   ├── SmartMepRouter.Core/          # Pure .NET Standard 2.0 router engine (Zero Revit API references)
│   │   ├── Connectors/               # ConnectorInfo & ConnectorShape DTOs
│   │   ├── Geometry/                 # Vec3, Aabb, Units (decimal feet)
│   │   ├── Metrics/                  # RunMetrics & MetricsWriter (JSON)
│   │   ├── Post/                     # PathSimplifier & EndpointAligner
│   │   ├── Scenes/                   # Synthetic benchmark scene generators
│   │   ├── Search/                   # Deterministic A*, MinHeap, CostModel, Router
│   │   └── Spatial/                  # SparseGrid, BoxObstacle, ObbObstacle (SAT), CollisionVerifier
│   ├── SmartMepRouter.UI/            # Modeless dark-themed WPF interface (net48 & net8.0-windows)
│   │   ├── Themes/                   # Tokens.Dark.xaml (Design token system)
│   │   ├── ViewModels/               # MainViewModel & ObservableObject
│   │   └── Views/                    # MainView.xaml & window-scoped resources
│   └── SmartMepRouter.Revit/         # Revit add-in integration layer (net48 & net8.0-windows)
│       ├── App.cs                    # IExternalApplication (Ribbon Tab & PushButton)
│       ├── Commands/                 # RoutingCommand (IExternalCommand)
│       ├── Creation/                 # DuctBuilder, FailuresPreprocessor, DuctSystemResolver
│       ├── Extraction/               # Host & Link Obstacle Extractors, CorridorBuilder, ConnectorReader
│       └── Preview/                  # ObstacleVisualizer (Debug wireframe shapes)
├── tests/
│   ├── SmartMepRouter.Core.Tests/    # 40 xUnit tests covering math, SAT, A*, and extraction logic
│   └── SmartMepRouter.Bench/         # 50-scene comparative benchmark runner
├── architecture.md                   # Full architectural specification
├── design.md                         # UI tokens and WPF styling guidelines
├── master.md                         # Project index, scope, and glossary
├── memory.md                         # Project decisions, pitfalls, and verified literature
├── phases.md                         # Implementation roadmap and gate criteria
└── rules.md                          # Engineering and anti-hallucination rules
```

---

## 📊 Benchmark Scoreboard

Evaluated across **50 procedurally generated obstacle scenes** containing structural columns, deep beams, floor boundaries, and rotated obstacles:

| Metric | SmartMEP (Fabrication A\*) | Plain A\* Baseline | Improvement / Note |
|---|:---:|:---:|:---:|
| **Scenes Evaluated** | **50** | 50 | Full benchmark set |
| **Solved Routes** | **50 (100%)** | 50 (100%) | **100% completion** |
| **Total Collisions** | **0** | N/A | **Zero collisions verified** |
| **Average Bend Count** | **3.64** | 4.04 | **~10% fewer fittings** |
| **Average Path Length** | **50.32 ft** (15.3 m) | 46.32 ft (14.1 m) | Planned detour around obstacles |
| **Average Search Time** | **29 ms** | 1.2 ms | Real-time response (< 0.1 s) |

---

## 🛠️ Installation Guide

For complete step-by-step instructions and troubleshooting, see the [Full Installation & Working Guide](docs/INSTALLATION_AND_USAGE.md).

### Prerequisites
- **Operating System**: Windows 10/11 (64-bit).
- **.NET SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.400+).
- **Autodesk Revit**: Revit 2024 (.NET 4.8), 2025 (.NET 8), 2026 (.NET 8), or 2027 (.NET 8).

### Option 1: 1-Click Automated Installer (Fastest)
1. Close all active Revit sessions.
2. Double-click **`install.bat`** in the repository root.
   - Automatically compiles all multi-targeted assemblies (`net48` and `net8.0-windows`).
   - Registers `SmartMepRouter.addin` into `%APPDATA%\Autodesk\Revit\Addins\<Year>\`.
   - Deploys DLLs to `%APPDATA%\Autodesk\Revit\Addins\<Year>\SmartMepRouter\`.

### Option 2: PowerShell Automated Deployment
```powershell
# Deploy across all installed Revit editions (2024 - 2027)
powershell -ExecutionPolicy Bypass -File scripts\deploy.ps1 -RevitVersion all

# Or deploy to a specific version (e.g. 2027)
powershell -ExecutionPolicy Bypass -File scripts\deploy.ps1 -RevitVersion 2027
```

### Option 3: Manual Compilation & Testing
```powershell
# Build solution across both net48 and net8.0-windows targets
dotnet build SmartMepRouter.sln -c Debug

# Run all 82 automated unit tests
dotnet test SmartMepRouter.sln

# Run 53-scene automated benchmark suite
dotnet run --project tests/SmartMepRouter.Bench
```

---

## 🎯 Step-by-Step Working Guide

EaseMEP runs as a lightweight, modeless add-in directly within your active Revit session:

### 1. Launch from the Ribbon
- In Autodesk Revit, click on the **Smart MEP Router** tab in the top ribbon.
- Click **Auto-Route** in the **Routing Engine** panel.
- The modeless dark-themed UI window opens without locking Revit's viewport.

### 2. Pick Source & Target Connectors
- Click **Pick Start Connector** and click any equipment port (e.g., AHU discharge, pump, electrical tray).
- Click **Pick End Connector** and click your destination (e.g., terminal unit, riser, diffuser).
- EaseMEP reads exact 3D coordinates, orientation vector, dimensions, and system domain.

### 3. Set Obstacle Filters & Clearance
- Toggle obstacle categories: **Structural Columns**, **Structural Framing (Beams)**, **Walls**, **Pipes**, or **Cable Trays**.
- Linked architectural and structural models are automatically transformed into host coordinates.
- Set **Clearance Margin** (default: `25 mm` / `1 in`). The router inflates obstacles by $Clearance + Insulation + \frac{\max(w,h)}{2}$.

### 4. Choose Geometry Preferences & Discipline
- **Discipline**: Auto-detects or switch between **Duct**, **Pipe**, and **Cable Tray**.
- **Shape**: Rectangular or Round.
- **Allowed Bends**: **45° & 90°** (smooth detours) or **90° Only** (strict Manhattan turns).
- **Optimization**: **Low Fitting Count** (minimizes fabrication cost) or **Shortest Length**.

### 5. 3D Preview Path
- Click **Preview Path** to compute and render a live wireframe centerline in your 3D view in under 30 ms without modifying your model.

### 6. Generate Elements & 1-Click Undo
- Click **✓ Generate Ducts** (or Pipes / Cable Trays).
- EaseMEP creates straight runs, inserts native Revit elbows (`NewElbowFitting`), and connects to equipment in an atomic transaction.
- Made a mistake? Press **Ctrl + Z** to cleanly roll back all generated elements in a single step with zero orphaned fittings.

---

## 📖 Documentation & Guides

- [Full Installation & Working Guide](docs/INSTALLATION_AND_USAGE.md) — Comprehensive user manual, multi-discipline details & troubleshooting
- [Demo Walkthrough & Video Script](docs/DEMO_GUIDE.md) — 3-minute video presentation guide
- [Pitch Notes & Measured Scoreboard](docs/PITCH_NOTES.md) — Value proposition and metrics
- [System Architecture](architecture.md) — Mathematical formulations and data pipeline
- [UI & Visual Tokens Design](design.md) — Design system specification
- [Product Requirements (PRD)](prd.md) — Functional and non-functional requirements
- [Phase Roadmap](phases.md) — Project milestones and phase gates

---

## 🧭 Roadmap & Completion Status

- [x] **Phase 0 — Environment & Multi-Target Skeleton** (`net48` & `net8.0-windows`)
- [x] **Phase 1 — Core Router & Fabrication A\*** (Deterministic 3D grid, turn penalties, stubs)
- [x] **Phase 2 — Spatial SAT Obstacles & 50-Scene Benchmark** (OBB SAT, CollisionVerifier, JSON metrics)
- [x] **Phase 3 — Revit Ribbon, Command & Modeless WPF Dialog** (Deployed to Revit 2027)
- [x] **Phase 4 — Context Extraction** (Host & Linked model extraction, OBB conversion, connector reader)
- [x] **Phase 5 — Duct Creation & Atomic Rollback** (Approach A `Duct.Create` + `NewElbowFitting` pipeline)
- [x] **Phase 6 — Interactive UI & Real-Time Preview** (Pick connectors via ExternalEvent, preview graphics)
- [x] **Phase 7 — Fabrication Constraints** (Directional inflation, min straight run, round duct support)
- [x] **Phase 8 — Hardening & Failure Diagnostics** (Root-cause blocking element ID reporting, corridor widening retry)
- [x] **Phase 9 — Paper & Evaluation Gate** (Real Revit BIM scenarios, ablations, Paper Gate GO)
- [x] **Phase 10 — Packaging & Demo** (1-click installer, demo video script, pitch notes)

---

## 📜 License & Compliance

Designed and built under clean-room engineering rules.
- **Rule R-A1**: Core contains zero Revit API dependencies.
- **Rule R-A3**: Decimal feet internally; unit conversion strictly at UI boundaries.
- **Rule R-A5**: Atomic transaction integrity with full rollback on error.
- **Rule R-T2**: Strict independent geometric collision verification.
