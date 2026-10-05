# Rules — Smart MEP Router

Keywords **MUST**, **SHOULD**, **MAY** are used in their usual sense. These rules apply to you and to any AI assistant working on the repo.

## A. Architecture rules

- **R-A1** Core MUST NOT reference `RevitAPI`, `RevitAPIUI` or WPF.
- **R-A2** Revit API calls MUST run on the API thread only. Use `ExternalEvent` from the modeless window.
- **R-A3** Core MUST use Revit internal units (decimal feet). Conversion happens only in `Units` at the UI boundary.
- **R-A4** Version-specific Revit API code MUST live in `Compat/` behind `#if`. No `#if` scattered in feature code.
- **R-A5** Model changes MUST happen inside a `TransactionGroup`; any failure MUST roll back everything.
- **R-A6** Do not use `PriorityQueue<,>` in Core (not available on `net48`). Use `MinHeap<T>`.
- **R-A7** UI resources MUST be scoped to the window, not `Application.Resources`.

## B. Coding rules (C#)

- **R-B1** Nullable reference types on. No warnings in Core.
- **R-B2** Public APIs in Core have XML doc comments stating units and coordinate frame.
- **R-B3** No static mutable state. Pass dependencies in.
- **R-B4** Search code MUST be deterministic: same scene and settings give the same path. Break ties explicitly.
- **R-B5** No allocations in the search inner loop (struct states, pooled structures).
- **R-B6** Catch exceptions only where you can handle them; map to the error taxonomy in `architecture.md`.
- **R-B7** Names: `PascalCase` types and methods, `_camelCase` private fields, suffix distances with unit when ambiguous (`clearanceFeet`, `clearanceMm`).
- **R-B8** One class per file. Files under about 400 lines; split otherwise.

## C. Verification rules (anti-hallucination)

- **R-V1** Every Revit API call MUST be checked against the SDK documentation, RevitLookup, or a compiled sample for the target versions before it is relied on. Signatures in earlier chat notes are leads, not facts (the `Duct.Create` argument order is a known suspect).
- **R-V2** Every library, repository, paper or dataset MUST be opened and confirmed before it is listed as a resource. Unconfirmed items go to "Unverified leads" in `memory.md`.
- **R-V3** Performance, time-saving and ROI numbers MUST come from your own measurements. Never state "X minutes to Y seconds" without a benchmark.
- **R-V4** Claims about other tools (what they do or fail at) MUST be tested or sourced before they appear in a pitch or paper.
- **R-V5** When something is assumed, write `ASSUMPTION:` in the code comment or doc and add it to `memory.md`.

## D. Testing rules

- **R-T1** Every Core feature ships with unit tests; every search change reruns the benchmark.
- **R-T2** A path MUST be re-verified by an independent exact collision check before it is passed to the Revit layer.
- **R-T3** Bugs get a failing test first, then a fix.
- **R-T4** Revit manual checklist is run before every tagged release, on the oldest and newest tested Revit versions.

## E. Git and workflow

- **R-G1** Branches: `main` (stable), `dev`, `feature/<short-name>`.
- **R-G2** Conventional commits: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `perf:`, `chore:`.
- **R-G3** No binaries, `.rvt` or large models in git. Keep sample models outside the repo; store download links in `memory.md`.
- **R-G4** Respect licenses of sample data and papers; cite sources in `memory.md`.
- **R-G5** Update `memory.md` at the end of every session (state, decisions, next action).

## F. UI rules

- **R-U1** Follow `design.md` tokens. No hard-coded colors in XAML outside the theme dictionaries.
- **R-U2** Every control has a keyboard path, a visible focus state and a disabled state.
- **R-U3** Never block the UI thread during search; show progress and allow cancel.
- **R-U4** Error messages state what happened, why, and what the user can change.

## G. Definition of done (per task)

1. Code compiles on both targets.
2. Unit tests pass; benchmark not regressed (or regression explained).
3. Rules A to F satisfied.
4. Docs updated if behavior changed.
5. `memory.md` updated.

## H. Instructions for AI assistants

1. Read `master.md`, `memory.md`, the active phase in `phases.md`, then this file.
2. Do not invent API signatures, repo names, datasets or numbers. If unsure, say so and propose how to verify.
3. Propose the smallest change that completes the task; do not widen scope beyond the active phase.
4. If a request conflicts with these rules, say which rule and ask.
5. Record any decision or discovered pitfall in `memory.md`.
