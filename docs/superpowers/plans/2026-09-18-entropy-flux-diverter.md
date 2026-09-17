# Entropy Flux Diverter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Phase-2 Entropy Flux Diverter as a dual-liquid, Proto-Matter-assisted energy-conserving thermal coupler.

**Architecture:** Keep transfer thermodynamics and catalyst cost in `src/Core/EntropyFluxPolicy.cs`; keep ONI storage, conduit, temperature mutation, operational gating and serialization in a focused `EntropyFluxDiverterController`. Reuse `ForbiddenTechDevice`, the existing single Phase-2 research node, safe-removal framework and asset pipeline.

**Tech Stack:** C# 7.3, ONI/Klei building and conduit APIs, Harmony/PLib project infrastructure, Spriter/KAnim, PowerShell contract tests.

**Spec:** `docs/superpowers/specs/2026-09-18-entropy-flux-diverter-design.md`

## Global Constraints

- Stable ID is `BaiyeEntropyFluxDiverter`.
- 4×4, floor-mounted, Refining category.
- 3600 W × PowerMultiplier and 12 kDTU/s × HeatMultiplier.
- Two 10 kg liquid buffers; primary hot side, secondary cold side.
- Proto-Matter catalyst is separate solid storage and is not product mass.
- Equal DTU must leave hot side and enter cold side.
- Maintain 1 K liquid-phase margin and do not overshoot thermal equilibrium.
- Maximum requested transfer is 4,000,000 DTU per processed pair.
- Proto-Matter cost is 0.025 kg per 1,000,000 DTU actually moved × CostMultiplier.
- Output blocking and save/reload must not repeat transfer or catalyst consumption.
- Interference prevents new transfer.

---

### Task 1: Pure entropy-transfer policy

**Files:**
- Create: `tests/EntropyFluxPolicyTests.cs`
- Modify: `tests/CoreTestProgram.cs`
- Create: `src/Core/EntropyFluxPolicy.cs`

**Interfaces:**
- Produces `EntropyFluxPolicy.Evaluate(...)` and `EntropyFluxPolicy.ProtoMatterCostKg(...)`.
- `EntropyFluxResult` exposes validity, transferred DTU, output temperatures and unchanged masses.

- [x] Write failing tests for energy equality, mass identity, equilibrium clamp, both phase guards, zero gradient, invalid/nonfinite values and proportional Proto-Matter cost.
- [x] Run focused test path and establish RED before policy implementation.
- [x] Implement the pure policy with no Unity/game types.
- [x] Verify focused/Portable behavior; later Phase-2 HEADs containing this policy passed Feature verification.
- [x] Core policy implementation is committed.

### Task 2: Dual-liquid game building

**Files:**
- Create: `src/Game/Buildings/EntropyDiverter/EntropyFluxDiverterConfig.cs`
- Create: `src/Game/Buildings/EntropyDiverter/EntropyFluxDiverterController.cs`
- Create: `tests/EntropyDiverterSourceContractTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Config creates two liquid conduit pairs and dedicated hot/cold/Proto-Matter storages.
- Controller persists `processedPair`, mutates two liquid packets exactly once, consumes proportional Proto-Matter, enables outputs after processing and observes `ForbiddenTechDevice`.

- [x] Add source contract that requires 4×4 config, primary+secondary liquid ports, operational/logic/power, Proto storage, `ForbiddenTechDevice` and serialized processed-pair guard.
- [x] Verify source contract boundary before runtime files were complete.
- [x] Implement config/controller against existing conduit/storage patterns.
- [x] Add contract to Portable suite and verify on later green Phase-2 CI.
- [x] Runtime implementation is committed (`1975147b`, `8afc6e14` are key anchors).

### Task 3: Registration, localization and safe removal

**Files:**
- Modify: `src/Game/Registration/BuildingRegistration.cs`
- Modify: `src/Game/Registration/ForbiddenResearchRegistration.cs`
- Modify: `src/Game/Localization/STRINGS.cs`
- Modify: `packaging/translations/en.po`
- Modify: `packaging/translations/zh.po`
- Modify: `src/Game/Safety/SafeRemovalController.cs`
- Modify: `tests/SafeRemovalRuntimeTests.ps1` only where fixture coverage needs the new building.

- [x] Add Diverter to implemented-buildings registration and Phase-2 research unlock list only when enabled.
- [x] Add English/global STRINGS and PO entries for building, logic and operating statuses.
- [x] Add Diverter storage release/counting to safe removal and its runtime fixture path.
- [x] Run available Portable/contracts on later green Phase-2 commits.
- [x] Integration commits exist (`42c4579d`, `af80c931`, `433daa35`, `72b06685`).

### Task 4: KAnim source and progress checkpoint

**Files:**
- Create: `assets/scml/entropy_flux_diverter/baiye_entropy_flux_diverter.scml`
- Create encoded PNG source files in same directory.
- Modify: `assets/animation-manifest.json`
- Modify: `build-assets.ps1`
- Create: `tests/EntropyDiverterAssetSourceContractTests.ps1`
- Modify: `test.ps1`
- Modify: `docs/phase-2-progress.md`
- Modify: `docs/test-matrix.md`

- [x] Create field-coupler source art and `off` / `idle` / `working` / `blocked` / `ui` timelines using encoded-PNG restoration.
- [x] Register KAnim source mapping and manifest entry.
- [x] Add asset-source contract to Portable suite.
- [ ] Confirm the newest entropy-asset HEAD passes GitHub Portable CI and then record `PORTABLE_VERIFIED`.
- [ ] Leave local ONI DLL/conduit/in-game rows pending for Codex/user final validation.
- [ ] Commit final progress checkpoint after CI conclusion.

## Current exact breakpoint

Do not redo Tasks 1-3. The source/runtime/integration work already exists. Current breakpoint is the final three checkboxes in Task 4: observe current CI, record status, then defer ONI DLL and in-game behavior to the final Codex/user validation pass.
