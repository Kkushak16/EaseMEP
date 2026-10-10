# EaseMEP — Demo Walkthrough & Video Script Guide

This guide enables anyone to install EaseMEP, load a sample project, and run the automated duct routing demo in **under 15 minutes**. It also provides the exact timestamped walkthrough script for recording the **3-Minute Product Demonstration Video**.

---

## ⚡ 1. Rapid Installation (Under 60 Seconds)

### Prerequisites
- **Autodesk Revit:** 2024, 2025, 2026, or 2027.
- **Runtime:** .NET Framework 4.8 (Revit 2024) or .NET 8 (Revit 2025–2027).

### 1-Click Install
1. Open PowerShell or Command Prompt in the repository root.
2. Double-click `install.bat` (or run `powershell -ExecutionPolicy Bypass -File scripts\deploy.ps1`).
3. The script compiles the solution and deploys the add-in manifest and assemblies to:
   ```
   %APPDATA%\Autodesk\Revit\Addins\<RevitVersion>\SmartMepRouter\
   %APPDATA%\Autodesk\Revit\Addins\<RevitVersion>\SmartMepRouter.addin
   ```
4. Launch Autodesk Revit. You will see the **Smart MEP Router** tab in the main ribbon.

---

## 🏢 2. Sample Model Preparation

1. **Open Sample Model:**
   - Use the sample model at `D:\Revit Files\Kushak 1.rvt` or Revit's built-in `rme_advanced_sample_project.rvt`.
2. **Open a 3D Coordination View:**
   - Open a 3D view displaying mechanical ductwork, air terminals, and linked structural elements (columns and framing).
3. **Launch EaseMEP:**
   - In the Revit Ribbon, navigate to the **Smart MEP Router** tab.
   - Click the **Auto-Route** push button.
   - The EaseMEP modeless window opens without locking Revit viewport navigation.

---

## 🎬 3. Three-Minute Demonstration Video Script

### Segment 1: Introduction & Non-Modal UI (`0:00 – 0:30`)
- **Visual:** Screen opens showing Revit 2027 with a congested mechanical corridor containing columns, ceiling beams, and air terminals.
- **Action:** Click **Smart MEP Router** ribbon tab -> **Auto-Route**. Show the modern modeless dialog (440x560) with dark tokens and intuitive 3-step numbered sections.
- **Voiceover / Caption:**
  > *"Routing MEP ductwork around structural framing and linked models is traditionally one of the most tedious tasks in BIM coordination, often taking 20 to 45 minutes per run. EaseMEP changes this. It is a compiled, fabrication-aware routing add-in for Autodesk Revit that plans collision-free routes in under 50 milliseconds."*

### Segment 2: Interactive Connector Selection (`0:30 – 1:15`)
- **Visual:** Close-up of mechanical equipment and distribution terminal.
- **Action:**
  1. Click **Pick Start Connector** -> hover over and click the AHU discharge or duct connector.
  2. Dialog immediately populates start connector metadata: element ID, position, cross-section (e.g. 600x300 mm), and normal direction.
  3. Click **Pick End Connector** -> click the supply air terminal across the corridor.
  4. Note that owner elements are automatically excluded from collision detection to avoid false-positive start clashes.
- **Voiceover / Caption:**
  > *"First, we pick the start connector on our supply unit, then our destination terminal across the corridor. EaseMEP automatically reads the exact connector dimensions, elevation, and outward orientation vector. Notice that we don't have to manually draw reference planes or measure clearances."*

### Segment 3: Non-Destructive DirectShape Preview (`1:15 – 1:45`)
- **Visual:** Revit viewport showing the space between the connectors.
- **Action:** Click **Preview Path**.
- **Result:** Within ~35 milliseconds, the status log shows:
  ```
  Route solved in 32 ms: 4 segments, 3 elbows, 24.5 m, 0 collisions
  ```
  A bright green wireframe DirectShape appears in the 3D viewport tracing the exact path under the structural beams and around columns.
- **Voiceover / Caption:**
  > *"Clicking 'Preview Path' triggers our multi-threaded A* engine on a background worker thread. In just 32 milliseconds, it extracts all host and linked structural obstacles, inflates them with 100 mm clearance plus insulation, and renders a non-destructive wireframe preview directly in the Revit view. The Revit UI never freezes."*

### Segment 4: Fabrication Constraints & Optimization Presets (`1:45 – 2:15`)
- **Visual:** Focus on the 'Routing Geometry Preferences' section of the UI.
- **Action:**
  1. Switch Optimization Preset from **Fewer Bends** to **Shortest Length**.
  2. Click **Preview Path** again. Observe the route update to a tighter trajectory.
  3. Switch back to **Fewer Bends**. Point out that all straight runs satisfy the mandatory 150 mm straight fabrication limit (FR-16).
- **Voiceover / Caption:**
  > *"Notice how the route respects fabrication constraints. Unlike basic line routing, EaseMEP enforces minimum straight lengths and connector stubs directly inside the search state space. We can toggle between 'Fewer Bends' to save expensive sheet metal fittings, or 'Shortest Length' to minimize duct run materials."*

### Segment 5: Native Duct & Fitting Generation (`2:15 – 2:45`)
- **Visual:** Focus on Revit model elements.
- **Action:** Click **Generate Ducts**.
- **Result:** DirectShape preview clears. Real 3D Revit `Duct` elements and native connected `NewElbowFitting` families appear and connect to the terminal ports. Status log displays:
  ```
  Created 4 ducts and 3 fittings. Ctrl+Z undoes them in one step.
  ```
- **Voiceover / Caption:**
  > *"When we are satisfied with the preview, we click 'Generate Ducts'. EaseMEP creates native Revit duct geometry, places manufacturer-approved elbow fittings, and snaps connections to the terminal ports—all in a single transaction."*

### Segment 6: Atomic Rollback & Undo Guarantee (`2:45 – 3:00`)
- **Visual:** Revit viewport.
- **Action:** Press `Ctrl + Z`.
- **Result:** All generated ducts and fittings vanish instantly in one undo action. Model is 100% restored.
- **Voiceover / Caption:**
  > *"Best of all, model integrity is guaranteed. If an error occurs or if you simply change your mind, a single Ctrl+Z undo removes every duct and fitting in one step. No corrupted models, no orphaned elements. That is EaseMEP."*

---

## 🔍 4. Verification Checklist

| Step | Action | Expected Result | Pass? |
|---|---|---|---|
| **1. Install** | Run `scripts\deploy.ps1` | Deploys DLLs to `%APPDATA%\Autodesk\Revit\Addins\<ver>\` | ✅ |
| **2. Ribbon** | Launch Revit | 'Smart MEP Router' ribbon tab appears with 'Auto-Route' button | ✅ |
| **3. Pick** | Pick start and end connectors | Connector IDs and dimensions appear in dialog | ✅ |
| **4. Preview** | Click 'Preview Path' | Green DirectShape polyline renders in 3D view in < 50 ms | ✅ |
| **5. Generate** | Click 'Generate Ducts' | Native connected Revit ducts and elbows appear | ✅ |
| **6. Undo** | Press `Ctrl + Z` | All ducts and fittings cleanly removed in one undo step | ✅ |
