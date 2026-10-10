# 🛠️ EaseMEP — Installation & Working Guide

Welcome to the complete installation, deployment, and operational guide for **EaseMEP (Smart MEP Router)** for Autodesk® Revit®.

---

<p align="center">
  <img src="images/easemep_ui.png" alt="EaseMEP UI - Smart MEP Router" width="90%" />
</p>

---

## 📑 Table of Contents
1. [System Requirements & Prerequisites](#1-system-requirements--prerequisites)
2. [Installation Guide](#2-installation-guide)
   - [Method A: 1-Click Automated Installer (Recommended)](#method-a-1-click-automated-installer-recommended)
   - [Method B: PowerShell Script Deployment](#method-b-powershell-script-deployment)
   - [Method C: Manual Build & Deployment](#method-c-manual-build--deployment)
3. [First Launch & Ribbon Verification](#3-first-launch--ribbon-verification)
4. [Step-by-Step Working Guide](#4-step-by-step-working-guide)
   - [Step 1: Open Your Revit Model & Launch EaseMEP](#step-1-open-your-revit-model--launch-easemep)
   - [Step 2: Pick Start & End Connectors](#step-2-pick-start--end-connectors)
   - [Step 3: Configure Obstacle Filtering & Clearance](#step-3-configure-obstacle-filtering--clearance)
   - [Step 4: Select Routing Preferences & Discipline](#step-4-select-routing-preferences--discipline)
   - [Step 5: 3D Preview Path](#step-5-3d-preview-path)
   - [Step 6: Generate Native Connected Elements](#step-6-generate-native-connected-elements)
   - [Step 7: 1-Click Atomic Undo](#step-7-1-click-atomic-undo)
5. [Multi-Discipline Support (Ducts, Pipes, Cable Trays)](#5-multi-discipline-support)
6. [Failure Diagnostics & Obstacle Pinpointing](#6-failure-diagnostics--obstacle-pinpointing)
7. [Troubleshooting & FAQ](#7-troubleshooting--faq)

---

## 1. System Requirements & Prerequisites

| Requirement | Supported Specifications |
|---|---|
| **Operating System** | Windows 10 (64-bit) or Windows 11 (64-bit) |
| **Autodesk Revit** | Revit 2024, Revit 2025, Revit 2026, or Revit 2027 |
| **.NET SDK** | .NET 8.0 SDK (8.0.400 or later) |
| **Target Frameworks** | `.NET Framework 4.8` (for Revit 2024) / `.NET 8.0-windows` (for Revit 2025–2027) |
| **Hardware** | 16 GB RAM minimum, 32 GB recommended for large linked structural BIM models |

> [!NOTE]
> EaseMEP builds as a multi-targeted solution. It automatically compiles for `.NET 4.8` when targeting Revit 2024 and `.NET 8.0` for Revit 2025–2027.

---

## 2. Installation Guide

### Method A: 1-Click Automated Installer (Recommended)

1. **Close any running instances of Autodesk Revit**.
2. Open the repository root folder.
3. Double-click **`install.bat`** (or right-click and choose **Run as administrator** if your Windows environment restricts PowerShell script execution).
4. The batch script will automatically:
   - Restore and build `SmartMepRouter.sln` across all targets.
   - Detect installed Revit versions (`2024`, `2025`, `2026`, `2027`).
   - Copy the `SmartMepRouter.addin` manifest into `%APPDATA%\Autodesk\Revit\Addins\<Year>\`.
   - Copy the required binaries and dependencies into `%APPDATA%\Autodesk\Revit\Addins\<Year>\SmartMepRouter\`.
5. When the script displays `Deployment completed successfully!`, press any key to close the terminal.

---

### Method B: PowerShell Script Deployment

If you prefer terminal control or want to target a specific Revit version:

```powershell
# Deploy to ALL installed Revit versions (2024, 2025, 2026, 2027)
powershell -ExecutionPolicy Bypass -File scripts\deploy.ps1 -RevitVersion all

# Deploy strictly to Revit 2027
powershell -ExecutionPolicy Bypass -File scripts\deploy.ps1 -RevitVersion 2027

# Deploy strictly to Revit 2025 in Release configuration
powershell -ExecutionPolicy Bypass -File scripts\deploy.ps1 -RevitVersion 2025 -Configuration Release
```

---

### Method C: Manual Build & Deployment

If you are developing or compiling in a customized CI/CD pipeline:

1. **Compile the solution**:
   ```powershell
   dotnet build SmartMepRouter.sln -c Debug
   ```

2. **Locate the output binaries**:
   - For Revit 2024: `src\SmartMepRouter.Revit\bin\Debug\net48\`
   - For Revit 2025–2027: `src\SmartMepRouter.Revit\bin\Debug\net8.0-windows\`

3. **Deploy the files**:
   - Create folder: `%APPDATA%\Autodesk\Revit\Addins\<VERSION>\SmartMepRouter\`
   - Copy all `.dll` and `.pdb` files from the respective output directory into the `SmartMepRouter` folder.
   - Copy `addin\SmartMepRouter.addin` directly into `%APPDATA%\Autodesk\Revit\Addins\<VERSION>\`.

---

## 3. First Launch & Ribbon Verification

1. Launch **Autodesk Revit**.
2. On first launch, Revit will display an add-in security prompt:
   - Click **"Always Load"** for `Smart MEP Router`.
3. In any project or sample model, navigate to the Revit Ribbon at the top:
   - You will see a new tab: **`Smart MEP Router`**.
   - Inside the **`Routing Engine`** panel, you will find the **`Auto-Route`** button.

```
+-------------------------------------------------------------------------+
| Architecture | Structure | Systems | ... |  Smart MEP Router [TAB]      |
+-------------------------------------------------------------------------+
                                           |  [ Auto-Route ]              |
                                           |  Routing Engine Panel        |
                                           +------------------------------+
```

---

## 4. Step-by-Step Working Guide

### Step 1: Open Your Revit Model & Launch EaseMEP
- Open a 3D view or floor plan view containing MEP equipment, terminal units, or duct/pipe risers.
- Click **Auto-Route** in the **Smart MEP Router** ribbon tab.
- The modeless EaseMEP dark-theme window will appear over your Revit viewport. Because the window is modeless, you can freely rotate, pan, and zoom your Revit view while EaseMEP is open!

---

### Step 2: Pick Start & End Connectors
1. In the **CONNECTORS** section, click **Pick Start Connector**.
   - Hover over your source equipment (e.g., AHU discharge, fan coil, pipe manifold, electrical tray).
   - Click directly on the connector port. EaseMEP highlights the port and extracts:
     - Physical 3D coordinate $(X, Y, Z)$
     - Outward normal vector (direction)
     - Width and height (or diameter)
     - System classification & discipline
2. Click **Pick End Connector**.
   - Click on the destination connector (e.g., air terminal inlet, plumbing fixture, riser).
3. Both connector buttons will show green checkmarks and the connector names once picked.

---

### Step 3: Configure Obstacle Filtering & Clearance
1. **Host & Link Categories**:
   - Check or uncheck obstacles you wish to route around:
     - ☑ **Structural Columns** (Concrete & Steel)
     - ☑ **Structural Framing** (Beams, Girders, Joists)
     - ☑ **Walls** (Architectural & Structural)
     - ☑ **Cable Trays & Conduits**
     - ☑ **Pipes & Ducts**
2. **Clearance Margin**:
   - Specify the mandatory spatial buffer (default: **25 mm** or **1 inch**).
   - EaseMEP inflates all obstacles by $Clearance + Insulation + \frac{\max(w, h)}{2}$ so the physical duct never touches framing or finishes.
3. **Linked Models**:
   - Linked architectural and structural IFC/RVT files are automatically extracted and transformed into the host coordinate space!

---

### Step 4: Select Routing Preferences & Discipline
1. **Discipline**:
   - **Duct**: Generates native HVAC ducts and elbows.
   - **Pipe**: Generates mechanical and plumbing pipes and fittings.
   - **Cable Tray**: Generates electrical ladder/solid cable trays and fittings.
   *(Note: Picking a connector automatically defaults to its native discipline!)*
2. **Shape**:
   - Choose **Rectangular** or **Round**.
3. **Allowed Bends**:
   - **45° & 90°**: Enables diagonal transitions around angled structural braces and walls.
   - **90° Only**: Restricts routing to strict rectilinear Manhattan turns.
4. **Optimization Goal**:
   - **Low Fitting Count** *(Recommended)*: Prioritizes fewer elbows to minimize fabrication cost and pressure drop.
   - **Shortest Length**: Prioritizes minimum total linear run distance.

---

### Step 5: 3D Preview Path
- Click the **Preview Path** button in the lower-right corner.
- EaseMEP runs the 3D A* search engine in a background thread without freezing Revit.
- The console log reports the search time (typically **5 ms – 35 ms**), bend count, and clearance verification.
- A temporary wireframe centerline is displayed directly in your Revit 3D view showing the proposed route before any elements are committed.

---

### Step 6: Generate Native Connected Elements
- Click **✓ Generate Ducts** (or **Generate Pipes** / **Generate Cable Trays**).
- EaseMEP enters an atomic Revit transaction:
  1. Instantiates straight duct/pipe/tray segments.
  2. Aligns segment centerlines with the exact connector positions.
  3. Inserts native Revit elbow fittings using `NewElbowFitting`.
  4. Connects the start and end fittings to your equipment connector ports.
- The route is now a fully connected, pressure-calculating Revit MEP system!

---

### Step 7: 1-Click Atomic Undo
- If you want to revert or try a different clearance setting:
  - Simply press **Ctrl + Z** in Revit (or click Undo).
  - The entire multi-segment run and all fittings are rolled back cleanly in a single step. No orphaned fittings or disconnected segments are ever left behind.

---

## 5. Multi-Discipline Support

EaseMEP natively supports all three core MEP disciplines:

```
┌────────────────────────────────────────────────────────────────────────┐
│                               EaseMEP                                  │
├─────────────────────┬──────────────────────────┬───────────────────────┤
│      HVAC DUCTS     │      MEP PIPING          │     CABLE TRAYS       │
├─────────────────────┼──────────────────────────┼───────────────────────┤
│ • Rectangular Ducts │ • Chilled Water Pipes    │ • Ladder Cable Trays  │
│ • Round Ducts       │ • Domestic Water Pipes   │ • Channel Trays       │
│ • Rectangular Elbows│ • Hydronic Heating       │ • Solid-Bottom Trays  │
│ • Round Miter Elbows│ • Threaded/Welded Elbows │ • 90° / 45° Fittings  │
└─────────────────────┴──────────────────────────┴───────────────────────┘
```

When you pick a start connector, EaseMEP automatically detects whether it belongs to `DomainHvac`, `DomainPiping`, or `DomainCableTrayConduit` and adjusts the UI buttons and generation engine accordingly.

---

## 6. Failure Diagnostics & Obstacle Pinpointing

If a route is physically impossible (e.g., equipment is enclosed in a solid concrete shaft with no opening):

1. **Root-Cause Element Pinpointing**:
   - The status log highlights the exact Revit Element ID and category causing the blockage:
     ```
     [BLOCKED] Path obstructed by Structural Framing: Beam 500x300 (Id: 489211).
     ```
2. **Corridor Expansion**:
   - EaseMEP will automatically attempt a wider detour corridor before reporting failure.
3. **Selection Highlight**:
   - You can copy the element ID and use Revit's **Manage > Select by ID** to inspect the clash immediately.

---

## 7. Troubleshooting & FAQ

#### Q: Revit displays "Could not load file or assembly 'SmartMepRouter.Revit.dll'" on startup.
- **Cause**: Windows SmartScreen or antivirus marked the downloaded DLL as blocked.
- **Fix**: Open Windows File Explorer, navigate to `%APPDATA%\Autodesk\Revit\Addins\<Year>\SmartMepRouter\`, right-click each `.dll`, choose **Properties**, and check the **Unblock** checkbox if present.

#### Q: The script reports "Access Denied" when running `install.bat`.
- **Cause**: Revit was open during installation and locked the DLL files in memory.
- **Fix**: Close Revit completely via Task Manager, then run `install.bat` again.

#### Q: The Smart MEP Router tab does not appear in Revit.
- **Cause**: The `.addin` manifest is missing from `%APPDATA%\Autodesk\Revit\Addins\<Year>\`.
- **Fix**: Verify that `SmartMepRouter.addin` is present in `%APPDATA%\Autodesk\Revit\Addins\<Year>\` and points to `SmartMepRouter\SmartMepRouter.Revit.dll`.

#### Q: Why does connector picking fail on certain equipment?
- **Cause**: The family instance might not have an active MEP connector port, or the active view has MEP categories hidden.
- **Fix**: Ensure you are in a 3D view or Mechanical Plan view where **Duct Accessories**, **Mechanical Equipment**, or **Air Terminals** are visible in `Visibility/Graphics (VG)`.

#### Q: How can I run the automated unit tests?
- Open PowerShell in the repository root and run:
  ```powershell
  dotnet test SmartMepRouter.sln
  ```
  All **82 core tests** (math, 3D spatial hashing, SAT collision verifier, search engine, endpoint snapping) will execute and report 100% green.

---

*EaseMEP — Engineered for zero-collision automated MEP fabrication.*
