# Design — Smart MEP Router

## 1. What the reference design is doing

The reference (`ui_for_revit.png`) is a compact, text-dense tool dialog, not a full dashboard. It has:

- A title bar: brand bolt, **"Smart MEP Router – Auto Detour Engine"**, window controls.
- Three numbered sections with uppercase labels: **1. CONNECTORS**, **2. OBSTACLE FILTERING**, **3. ROUTING GEOMETRY PREFERENCES**.
- Two side-by-side outlined buttons: *Pick Start Connector*, *Pick End Connector*, each with a monospaced ID line beneath (`ID: 394821 (AHU-01 Out)`).
- A 2×2 checkbox grid (columns, shear walls, framing, ceiling grid).
- One numeric field: *Wall/Column Clearance Margin [100] mm*.
- Radio pair (Rectangular / Round) and two dropdowns (Allowed Bends, Optimization).
- A footer bar with two actions: **Preview Path (Lines)** (secondary) and **✓ Generate Ducts** (primary).
- Monospaced look, thin single-line borders, no shadows, no illustrations.

This spec keeps that structure and character, and adds a dark/light token system, states, and a status log (from the earlier plan) as a clearly marked v1.1 extension.

**Design character:** quiet, engineering-tool, thin borders, hierarchy through spacing and weight instead of heavy cards.

## 2. Window frame

| Property | Value |
|---|---|
| Type | Modeless WPF window owned by the Revit main window |
| Default size | 440 × 560 px (min 420 × 520); resizable vertically |
| Outer padding | 16 px |
| Section gap | 16 px |
| Spacing system | 4 / 8 / 12 / 16 / 24 px |
| Title bar height | 40 px |
| Footer height | 64 px |

```
┌──────────────────────────────────────────────┐
│ ⚡ Smart MEP Router – Auto Detour Engine [–][✕]│  title 40
├──────────────────────────────────────────────┤
│ 1  CONNECTORS                                 │
│ [ Pick Start Connector ] [ Pick End Connector ]│
│ ID: 394821 (AHU-01 Out)  ID: 395104 (Diff-12) │
│                                               │
│ 2  OBSTACLE FILTERING                         │
│ ☑ Structural Columns     ☑ Shear Walls        │
│ ☑ Structural Framing     ☐ Ceiling Grid       │
│ Clearance Margin  [ 100 ] mm                  │
│                                               │
│ 3  ROUTING GEOMETRY PREFERENCES               │
│ Duct Shape   (●) Rectangular  ( ) Round       │
│ Allowed Bends [ 90° Orthogonal Only       ▾ ] │
│ Optimization  [ Low Fitting Count         ▾ ] │
│                                               │
│ (v1.1) STATUS LOG                             │
├──────────────────────────────────────────────┤
│ [ Preview Path ]            [ ✓ Generate Ducts ]│  footer 64
└──────────────────────────────────────────────┘
```

Window starts near the Revit viewport's right side and remembers its last position.

## 3. Color tokens

Two modes: **Light** (from the reference palette) and **Dark** (true dark system, not an inversion). Window follows Revit's theme by default; a setting overrides it (Light / Dark / Follow Revit). Revit's theme API availability varies by version, so verify and fall back to the setting.

### 3.1 Dark

| Token | Hex |
|---|---|
| `Bg/App` | `#0B0C0F` |
| `Bg/Title` | `#090A0D` |
| `Bg/Workspace` | `#0E1014` |
| `Surface/Default` | `#14161B` |
| `Surface/Elevated` | `#191C22` |
| `Surface/Hover` | `#1E2229` |
| `Border/Default` | `#292D35` |
| `Border/Strong` | `#363B45` |
| `Text/Primary` | `#F5F7FA` |
| `Text/Secondary` | `#A7ADB8` |
| `Text/Tertiary` | `#707784` |
| `Text/Disabled` | `#4D535E` |
| `Accent/Primary` | `#8B7CFF` |
| `Accent/Hover` | `#A397FF` |
| `Accent/Subtle` | `#211E3A` |
| `Status/Success` | `#42D392` |
| `Status/Warning` | `#F4B860` |
| `Status/Error` | `#F26D78` |
| `Status/Info` | `#5DA9FF` |
| `Brand/Bolt` | `#F4B860` (matches the bolt in the mockup) |

### 3.2 Light

| Token | Hex | Note |
|---|---|---|
| `Bg/App` | `#FCFCFD` | reference |
| `Surface/Default` | `#FFFFFF` | proposed |
| `Surface/Hover` | `#F1F1F4` | proposed |
| `Border/Default` | `#E3E4E8` | proposed |
| `Border/Strong` | `#B8BAC2` | reference |
| `Text/Primary` | `#0C0C0C` | reference |
| `Text/Secondary` | `#3A3D40` | reference |
| `Text/Tertiary` | `#5D5D5E` | reference |
| `Accent/Primary` | `#766E7F` | reference muted purple-grey; consider a stronger accent such as `#5B4BDB` if it feels too faint |
| `Accent/Subtle` | `#ECE9FA` | proposed |

### 3.3 Contrast notes

- Dark `Text/Tertiary` (`#707784`) on `Surface/Default` is about 4:1, **below 4.5:1**. Use it only for non-essential hints; use `Text/Secondary` for anything the user must read. Recheck with a contrast tool.
- Dark `Accent/Primary` on `Surface/Default` is about 5.5:1.
- Light `Border/Strong` (`#B8BAC2`) on white is for borders and disabled states only, never text.
- Never convey state by color alone: pair with an icon or text.

### 3.4 Mapping from the reference light palette

| Reference | Dark equivalent |
|---|---|
| `#FCFCFD` | `#0B0C0F` |
| `#B8BAC2` | `#707784` |
| `#0C0C0C` | `#F5F7FA` |
| `#3A3D40` | `#A7ADB8` |
| `#5D5D5E` | `#707784` |
| `#766E7F` | `#8B7CFF` |

## 4. Typography

| Role | Size | Weight | Font |
|---|---|---|---|
| Window title | 13 px | 600 | UI font |
| Section label (uppercase, tracked) | 11 px | 600 | UI font |
| Body / control text | 13 px | 400 | UI font |
| Button text | 13 px | 500 | UI font |
| Caption / hint | 12 px | 400 | UI font |
| IDs, coordinates, log lines | 12 px | 400 | Mono font |

- **UI font:** Segoe UI (always present, safe in Revit). Inter/Geist only if you embed them and accept the packaging cost; keep Segoe UI as fallback.
- **Mono font:** Cascadia Mono with Consolas fallback. This reproduces the monospaced feel of the mockup for IDs, values and the log.
- Line heights: body 20, caption 16, heading 24.

## 5. Shape, borders, elevation

| Element | Radius |
|---|---|
| Controls (inputs, dropdowns, buttons) | 6 px |
| Cards / groups | 8 px |
| Window | system default (OS-drawn) |

- Borders: 1 px `Border/Default`. Strong border on hover and focus ring.
- No drop shadows inside the dialog. The reference is flat.
- No glass effects, no gradients.

## 6. Components

### 6.1 Section header
`1  CONNECTORS` — number in a 20 px circle (`Accent/Subtle` fill, `Accent/Primary` text), label uppercase 11 px, 8 px gap, 1 px divider under the row.

### 6.2 Pick button (outlined)
- Size: fills half the row, height 32 px.
- Default: transparent fill, 1 px `Border/Strong`, text `Text/Primary`.
- Hover: `Surface/Hover`. Pressed: `Surface/Elevated`. Focus: 2 px `Accent/Primary` ring.
- Picked state: left check icon, border `Status/Success`.
- ID line below: Mono 12 px `Text/Secondary`, e.g. `ID: 394821 (AHU-01 Out)`. Empty state: `Not selected` in `Text/Tertiary`.

### 6.3 Checkbox
16 px box, 4 px radius, 1 px `Border/Strong`. Checked: `Accent/Primary` fill with white check. Label 13 px. Disabled: `Text/Disabled`.

### 6.4 Numeric input
Height 32 px, right-aligned value, unit suffix `mm` in `Text/Tertiary`. Validation: invalid shows `Status/Error` border and a message; accepts only positive numbers; default 100.

### 6.5 Radio group
16 px circle, selected inner dot `Accent/Primary`. Horizontal layout with 16 px gap.

### 6.6 Dropdown
Height 32 px, chevron right, same border as inputs. Popup uses `Surface/Elevated`, item hover `Surface/Hover`, selected item `Accent/Subtle`.

Options:
- Allowed Bends: `90° Orthogonal Only` (v1), `45° and 90°` (v1.1, disabled until supported).
- Optimization: `Low Fitting Count (Fewer Bends)`, `Shortest Length`.

### 6.7 Footer buttons
- **Preview Path (Lines):** secondary, outlined, left.
- **✓ Generate Ducts:** primary, `Accent/Primary` fill, text `#0B0C0F` or white (check contrast), right. Disabled until both connectors are picked and a valid preview exists.
- Height 36 px, min width 120 px.

### 6.8 Status log (v1.1 extension)
Collapsible area above the footer, `Surface/Default`, mono 12 px lines:

```
12:04:11  Route solved in 142 ms
12:04:11  4 segments, 3 elbows, 0 collisions
```

Level icons: info, warning, error with tokens `Status/*`. Max height 120 px, scrolls.

### 6.9 Progress and cancel
During search the primary button becomes `Cancel`, a 2 px indeterminate bar sits at the footer top edge in `Accent/Primary`.

## 7. States and behavior

| State | Pick Start/End | Preview | Generate |
|---|---|---|---|
| Initial | enabled | disabled | disabled |
| Both picked | enabled | enabled | disabled |
| Previewing (search running) | disabled | Cancel | disabled |
| Preview ready | enabled | enabled (re-run) | enabled |
| Generating | disabled | disabled | disabled, spinner |
| Failed | enabled | enabled | disabled; error shown in log |

- Picking hides nothing; the window is modeless, so the user clicks in the Revit view while the dialog stays visible. Show a banner "Select start connector in the view. Esc to cancel."
- Changing any setting after a preview invalidates the preview.
- Failure messages use the error taxonomy in `architecture.md`, with the reason and what to change.

## 8. Accessibility

- Full keyboard path: Tab order follows reading order; Enter triggers the default action; Esc cancels pick mode.
- Visible focus ring on every control.
- Minimum hit target 28 × 28 px (32 px for buttons and inputs).
- Contrast per section 3.3.
- Control names set for screen readers (`AutomationProperties.Name`).
- Do not rely on color alone.

## 9. WPF implementation

```
UI/
  Themes/
    Tokens.Dark.xaml      colors and brushes
    Tokens.Light.xaml
    Controls.xaml         styles/templates for Button, CheckBox, RadioButton,
                          ComboBox, TextBox, ToggleButton, ScrollBar
  Views/MainView.xaml
  ViewModels/MainViewModel.cs
```

Token example:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
  <Color x:Key="BgApp">#0B0C0F</Color>
  <Color x:Key="SurfaceDefault">#14161B</Color>
  <Color x:Key="BorderDefault">#292D35</Color>
  <Color x:Key="TextPrimary">#F5F7FA</Color>
  <Color x:Key="AccentPrimary">#8B7CFF</Color>
  <SolidColorBrush x:Key="Bg.App" Color="{StaticResource BgApp}"/>
  <SolidColorBrush x:Key="Surface.Default" Color="{StaticResource SurfaceDefault}"/>
  <SolidColorBrush x:Key="Border.Default" Color="{StaticResource BorderDefault}"/>
  <SolidColorBrush x:Key="Text.Primary" Color="{StaticResource TextPrimary}"/>
  <SolidColorBrush x:Key="Accent.Primary" Color="{StaticResource AccentPrimary}"/>
</ResourceDictionary>
```

Rules:
- Use `DynamicResource` for brushes so the theme can switch at runtime.
- Merge dictionaries into the **window's** `Resources`, never `Application.Resources` (Revit's process is shared with other add-ins).
- Default WPF controls ignore dark themes; write explicit `ControlTemplate`s. Third-party theme libraries are possible but increase dependency-conflict risk; evaluate before adopting.

## 10. Figma setup (optional, for the portfolio case study)

```
Smart MEP Router
├── 00 Foundations      Colors, Typography, Spacing, Radius
├── 01 Components       Buttons, Inputs, Checkbox, Radio, Dropdown, Section header, Log
├── 02 Dialog           Light, Dark
└── 03 States           Initial, Picked, Previewing, Ready, Failed
```

Variables (with Light and Dark modes): `Color/Background/App`, `Color/Surface/Default`, `Color/Surface/Elevated`, `Color/Border/Default`, `Color/Border/Strong`, `Color/Text/Primary|Secondary|Tertiary`, `Color/Accent/Primary|Hover|Subtle`, `Color/Status/Success|Warning|Error|Info`.

Frame: 440 × 560 for the dialog, shown inside a 1440 × 900 Revit-style backdrop for context.

### Figma AI prompt

> Create a high-fidelity dark-mode and light-mode design of a compact Revit add-in dialog called "Smart MEP Router – Auto Detour Engine". Frame 440×560. Title bar with an amber lightning-bolt mark and window controls. Three numbered sections with uppercase labels: 1 CONNECTORS (two outlined buttons "Pick Start Connector" and "Pick End Connector", each with a monospaced ID line beneath, e.g. "ID: 394821 (AHU-01 Out)"), 2 OBSTACLE FILTERING (2×2 checkbox grid: Structural Columns, Shear Walls, Structural Framing, Ceiling Grid; numeric field "Clearance Margin 100 mm"), 3 ROUTING GEOMETRY PREFERENCES (radio: Rectangular / Round; dropdown "Allowed Bends: 90° Orthogonal Only"; dropdown "Optimization: Low Fitting Count (Fewer Bends)"). Footer with a secondary "Preview Path" button and a primary "✓ Generate Ducts" button. Optional collapsible status log with monospaced lines.
> Style: flat, engineering-tool, thin 1px borders, no shadows, no gradients, no glassmorphism. Dark tokens: background #0B0C0F, surface #14161B, elevated #191C22, border #292D35, text #F5F7FA / #A7ADB8 / #707784, accent #8B7CFF, success #42D392, warning #F4B860, error #F26D78. Fonts: Segoe UI and Cascadia Mono. Radius 6px controls, 8px groups, 8px spacing system. Include hover, focus, disabled, error and picked states. Create reusable components and Figma variables with Light and Dark modes.

## 11. What not to do

- Do not invert colors blindly.
- No large illustrations or hero areas; this is a tool dialog.
- No more than one accent color plus status colors.
- No modal dialog that blocks picking in the Revit view.
- Avoid rounded corners above 8 px; the reference is restrained.
