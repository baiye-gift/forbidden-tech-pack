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

- [ ] Write failing tests for energy equality, mass identity, equilibrium clamp, both phase guards, zero gradient, invalid/nonfinite values and proportional Proto-Matter cost.
- [ ] Run `./test.ps1 -Suite EntropyFluxPolicyTests` and verify RED because policy does not exist.
- [ ] Implement the pure policy with no Unity/game types.
- [ ] Run focused suite and Portable suite; verify GREEN.
- [ ] Commit `feat(phase2): add entropy flux policy`.

### Task 2: Dual-liquid game building

**Files:**
- Create: `src/Game/Buildings/EntropyDiverter/EntropyFluxDiverterConfig.cs`
- Create: `src/Game/Buildings/EntropyDiverter/EntropyFluxDiverterController.cs`
- Create: `tests/EntropyDiverterSourceContractTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Config creates two liquid conduit pairs and dedicated hot/cold/Proto-Matter storages.
- Controller persists `processedPair`, mutates two liquid packets exactly once, consumes proportional Proto-Matter, enables outputs after processing and observes `ForbiddenTechDevice`.

- [ ] Add source contract that requires 4×4 config, primary+secondary liquid ports, operational/logic/power, Proto storage, `ForbiddenTechDevice` and serialized processed-pair guard.
- [ ] Verify source contract RED before game files exist.
- [ ] Implement config/controller minimally against existing compiler conduit/storage patterns.
- [ ] Add contract to Portable suite and verify GREEN.
- [ ] Commit `feat(phase2): add entropy flux diverter runtime`.

### Task 3: Registration, localization and safe removal

**Files:**
- Modify: `src/Game/Registration/BuildingRegistration.cs`
- Modify: `src/Game/Registration/ForbiddenResearchRegistration.cs`
- Modify: `src/Game/Localization/STRINGS.cs`
- Modify: `packaging/translations/en.po`
- Modify: `packaging/translations/zh.po`
- Modify: `src/Game/Safety/SafeRemovalController.cs`
- Modify: `tests/SafeRemovalRuntimeTests.ps1` only where fixture coverage needs the new building.

- [ ] Add Diverter to implemented-buildings registration and Phase-2 research unlock list only when enabled.
- [ ] Add English/global STRINGS and PO entries for building, logic and operating statuses.
- [ ] Add Diverter storage release/counting to safe removal and its runtime fixture.
- [ ] Run Portable/available contracts.
- [ ] Commit `feat(phase2): integrate entropy flux diverter`.

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

- [ ] Create field-coupler source art and idle/working/off/ui timelines using the existing encoded-PNG restoration path.
- [ ] Register KAnim source mapping and manifest entry.
- [ ] Add asset-source contract to Portable suite.
- [ ] Run GitHub Portable CI; if green, record `PORTABLE_VERIFIED`; otherwise record exact blocker.
- [ ] Leave local ONI DLL/conduit/in-game rows pending for Codex/user final validation.
- [ ] Commit progress checkpoint.