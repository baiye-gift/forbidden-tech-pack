# Matter Annihilation Reactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Phase-2 Matter Annihilation Reactor with external startup power, coolant-gated generation, deterministic instability/decoherence and localized Proto-Matter interference.

**Architecture:** Put all state transitions, timing gates, Proto-Matter loss and heat-pulse math in `src/Core/AnnihilationReactorPolicy.cs`. The game controller owns ONI power/coolant/storage/animation integration and calls the existing `ProtoMatterInterferenceManager` exactly once on a Decohered state entry. Persist reactor state locally on the building; do not add global reactor state to save data.

**Tech Stack:** C# 7.3, ONI/Klei power/conduit APIs, Harmony/PLib project infrastructure, Spriter/KAnim, PowerShell contract tests.

**Spec:** `docs/superpowers/specs/2026-09-18-matter-annihilation-reactor-design.md`

## Global Constraints

- Stable ID is `BaiyeMatterAnnihilationReactor`.
- 7×6, floor-mounted, Power category.
- 12 kW external constraint power × PowerMultiplier; 40 kW gross generation in Stable only.
- 30 s external-powered Charging before Stable; no black start.
- Stable Proto-Matter use 0.2 kg/s × CostMultiplier.
- Stable heat 1.2 MDTU/s × HeatMultiplier must enter coolant/building path, never disappear.
- States: Offline, Charging, Stable, Fluctuating, Critical, Decohered, CoolingLockout.
- Decoherence loses 35% available reaction Proto-Matter, produces 20,000,000 DTU/kg lost × HeatMultiplier, radius-12 interference for 60 s, then lockout; no explosion/permanent destruction/ordinary-building damage.
- One-shot decoherence consequences never replay on reload.

---

### Task 1: Deterministic state/loss policy

**Files:**
- Create: `tests/AnnihilationReactorPolicyTests.cs`
- Modify: `tests/CoreTestProgram.cs`
- Create: `src/Core/AnnihilationReactorPolicy.cs`

**Interfaces:**
- `ReactorState` enum.
- `AnnihilationReactorPolicy.Next(state, healthy, startRequested, stopRequested, elapsedSeconds)`.
- `CalculateProtoMatterLoss(availableKg)` and `CalculateHeatPulseDtu(lostKg, heatMultiplier)`.

- [ ] Add failing tests for every healthy/degraded/recovery timing edge plus invalid elapsed input.
- [ ] Add loss clamp and finite/non-negative heat-pulse tests.
- [ ] Verify RED.
- [ ] Implement minimal pure policy without ONI types.
- [ ] Verify focused + Portable GREEN.
- [ ] Commit `feat(phase2): define annihilation reactor policy`.

### Task 2: Reactor runtime and one-shot decoherence

**Files:**
- Create: `src/Game/Buildings/AnnihilationReactor/MatterAnnihilationReactorConfig.cs`
- Create: `src/Game/Buildings/AnnihilationReactor/MatterAnnihilationReactorController.cs`
- Create: `tests/AnnihilationReactorSourceContractTests.ps1`
- Modify: `test.ps1`
- Modify: `src/Game/Buildings/Common/ForbiddenTechDevice.cs` only if a read-only `IsInterfered` accessor is required by the controller.

**Interfaces:**
- Config provides external power input, generator capability, coolant loop, Proto-Matter storage, automation and `ForbiddenTechDevice`.
- Controller serializes state/time/charge/one-shot guard and emits interference through existing `ApplySource`/`RemoveSource`.

- [ ] Add RED source contract for 7×6 config, input power + generator intent, coolant storage/conduits, Proto storage, serialization, policy call and one-shot interference source.
- [ ] Expose read-only `ForbiddenTechDevice.IsInterfered` if needed; preserve existing Operational behavior for other buildings.
- [ ] Implement Config and Controller with state-machine authority and exactly-once Decohered side effects.
- [ ] Ensure controller removes its source on cleanup/safe removal and outside active field duration.
- [ ] Verify Portable contracts GREEN.
- [ ] Commit `feat(phase2): add annihilation reactor runtime`.

### Task 3: Registration, localization and safe removal

**Files:**
- Modify: `src/Game/Registration/BuildingRegistration.cs`
- Modify: `src/Game/Registration/ForbiddenResearchRegistration.cs`
- Modify: `src/Game/Localization/STRINGS.cs`
- Modify: `packaging/translations/en.po`
- Modify: `packaging/translations/zh.po`
- Modify: `src/Game/Safety/SafeRemovalController.cs`
- Modify: `tests/SafeRemovalRuntimeTests.ps1`

- [ ] Add reactor to implemented building map under Power category.
- [ ] Include it in Phase-2 research unlocks according to enable switch.
- [ ] Add localized building/logic/reactor-state text.
- [ ] Safe removal disables reactor, removes owned interference source, returns coolant/unconsumed Proto-Matter and counts the building.
- [ ] Extend test fixture/source contracts.
- [ ] Commit `feat(phase2): integrate annihilation reactor`.

### Task 4: KAnim and final development checkpoint

**Files:**
- Create: `assets/scml/matter_annihilation_reactor/baiye_matter_annihilation_reactor.scml`
- Create encoded PNG source files.
- Modify: `assets/animation-manifest.json`
- Modify: `build-assets.ps1`
- Create: `tests/AnnihilationReactorAssetSourceContractTests.ps1`
- Modify: `test.ps1`
- Modify: `docs/phase-2-progress.md`
- Modify: `docs/test-matrix.md`

- [ ] Create toroidal confinement field source art with idle/charge/working/unstable/decohere/locked/off/ui timelines.
- [ ] Register manifest/build mapping and asset contract.
- [ ] Run GitHub Portable CI and record actual result.
- [ ] Mark Reactor no higher than `PORTABLE_VERIFIED` until Codex/user compiles it against current ONI DLL.
- [ ] Update progress with exact HEAD and remaining Codex validation commands/cases.
- [ ] Commit final development checkpoint.