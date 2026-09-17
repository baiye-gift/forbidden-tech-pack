# Phase 2 Proto-Matter Field Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the phase-2 proto-matter field system plus Matter Reconstructor, Entropy Flux Diverter, and Matter Annihilation Reactor without regressing phase-1 save safety or gameplay.

**Architecture:** Keep deterministic rules in `src/Core`, put ONI/Unity behavior in focused `src/Game` components, and route all proto-matter interference through a common `ForbiddenTechDevice` receiver plus one range-event manager. Existing phase-1 buildings join the same interference contract; ordinary ONI and third-party buildings are never patched for interference.

**Tech Stack:** C#, ONI/Klei building APIs, Harmony, PLib Options, Spriter/KAnim assets, existing PowerShell build/test/package scripts.

**Spec:** `docs/phase-2-protomatter-field-development.md`

## Global Constraints

- Keep static mod ID `Baiye.ForbiddenTechnologyPack` unchanged.
- Keep phase-1 IDs and save behavior unchanged.
- Add phase-2 buildings to the existing single PLib options page.
- Disable switches hide registration but must not invalidate already-built instances.
- Proto-matter interference may affect only this mod's forbidden devices and duplicant status effects; do not patch all ordinary buildings.
- No per-frame full-map scans for interference.
- Generated `packaging/anim` and `dist` outputs are never hand-edited.
- Every new building participates in safe removal.
- Steam/in-colony validation is required before phase 2 is marked complete.

---

### Task 1: Phase-2 IDs, options, registration, and research contract

**Files:**
- Modify: `src/Core/ModIdentity.cs`
- Modify: `src/Core/PackOptions.cs`
- Modify: `src/Core/RegistrationPolicy.cs`
- Modify: `src/Game/Options/ForbiddenTechOptions.cs`
- Modify: `src/Game/Registration/BuildingRegistration.cs`
- Modify: `src/Game/Registration/ForbiddenResearchRegistration.cs`
- Modify: `tests/PackOptionsTests.cs`
- Modify: `tests/RegistrationPolicyTests.cs`
- Modify: `tests/CoreTestProgram.cs`
- Modify: `src/Game/Localization/STRINGS.cs`
- Modify: `packaging/translations/zh.po`
- Modify: `packaging/translations/en.po`

**Interfaces:**
- Produces stable IDs `BaiyeMatterReconstructor`, `BaiyeEntropyFluxDiverter`, `BaiyeMatterAnnihilationReactor`, and research `BaiyeForbiddenProtoFieldEngineering`.
- Produces `ResolvedOptions.ReconstructorEnabled`, `EntropyDiverterEnabled`, and `AnnihilationReactorEnabled`.

- [ ] **Step 1: Add failing option and registration tests**

```csharp
var options = PackOptions.Resolve(new RawOptions {
    Preset = BalancePreset.Strong,
    ReconstructorEnabled = true,
    EntropyDiverterEnabled = true,
    AnnihilationReactorEnabled = true
});
var plan = RegistrationPolicy.Create(options);
Assert.True(plan.IsEnabled(ModIdentity.MatterReconstructorId));
Assert.True(plan.IsEnabled(ModIdentity.EntropyFluxDiverterId));
Assert.True(plan.IsEnabled(ModIdentity.MatterAnnihilationReactorId));
```

- [ ] **Step 2: Run focused tests and verify failure**

```powershell
./test.ps1 -Suite PackOptionsTests
./test.ps1 -Suite RegistrationPolicyTests
```

Expected: compile/test failure because phase-2 option properties and IDs do not exist.

- [ ] **Step 3: Add IDs and option plumbing**

```csharp
public const string MatterReconstructorId = "BaiyeMatterReconstructor";
public const string EntropyFluxDiverterId = "BaiyeEntropyFluxDiverter";
public const string MatterAnnihilationReactorId = "BaiyeMatterAnnihilationReactor";
public const string ProtoFieldResearchId = "BaiyeForbiddenProtoFieldEngineering";
```

Add matching raw/resolved option booleans and Chinese PLib toggles; preserve existing preset numeric behavior.

- [ ] **Step 4: Extend registration and add a phase-2 research node**

Register enabled phase-2 buildings in the build menu and create `BaiyeForbiddenProtoFieldEngineering` with `BaiyeForbiddenMatterEngineering` as prerequisite. Do not rename the phase-1 research node.

- [ ] **Step 5: Run tests**

```powershell
./test.ps1 -Suite PackOptionsTests
./test.ps1 -Suite RegistrationPolicyTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Core src/Game/Options src/Game/Registration src/Game/Localization packaging/translations tests
git commit -m "feat(phase2): register proto-matter field technology"
```

---

### Task 2: Common forbidden-device interference system

**Files:**
- Create: `src/Core/ProtoMatterInterferencePolicy.cs`
- Create: `src/Game/Interference/ForbiddenTechDevice.cs`
- Create: `src/Game/Interference/ProtoMatterInterferenceManager.cs`
- Modify: `src/Game/Buildings/Analyzer/MatterAnalyzerConfig.cs`
- Modify: `src/Game/Buildings/Crusher/MassCrusherConfig.cs`
- Modify: `src/Game/Buildings/Compiler/MatterCompilerConfig.cs`
- Modify runtime components for Analyzer/Crusher/Compiler as required to pause safely.
- Create: `tests/ProtoMatterInterferencePolicyTests.cs`
- Modify: `tests/CoreTestProgram.cs`

**Interfaces:**
- `ProtoMatterInterferencePolicy.IsInsideRadius(int sourceX, int sourceY, int targetX, int targetY, int radius)` provides deterministic range rules.
- `ForbiddenTechDevice.SetInterference(string sourceId, bool active)` is idempotent and supports more than one active source.
- `ForbiddenTechDevice.IsInterfered` is the only state other forbidden buildings need to query.

- [ ] **Step 1: Write pure range/idempotency tests**

```csharp
Assert.True(ProtoMatterInterferencePolicy.IsInsideRadius(0, 0, 3, 4, 5));
Assert.False(ProtoMatterInterferencePolicy.IsInsideRadius(0, 0, 6, 0, 5));
```

Also test duplicate activation/deactivation does not underflow active-source state.

- [ ] **Step 2: Run the new test and verify failure**

```powershell
./test.ps1 -Suite ProtoMatterInterferencePolicyTests
```

- [ ] **Step 3: Implement pure policy and receiver component**

```csharp
public bool IsInterfered { get { return activeSources.Count > 0; } }
```

Use a source-ID set rather than a single boolean so overlapping reactor fields can safely coexist.

- [ ] **Step 4: Implement event manager**

On interference creation/removal, query the affected grid region once, select objects with `ForbiddenTechDevice`, and call `SetInterference`. Do not schedule a full-map scan every frame.

- [ ] **Step 5: Attach receiver to phase-1 buildings**

Analyzer, Crusher, and Compiler must pause safely while `IsInterfered`; no input/output deletion, queue clearing, or recipe replacement while active work is in progress.

- [ ] **Step 6: Run core and contract tests**

```powershell
./test.ps1 -Suite ProtoMatterInterferencePolicyTests
./test.ps1 -Suite All
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Core/ProtoMatterInterferencePolicy.cs src/Game/Interference src/Game/Buildings tests
git commit -m "feat(phase2): add proto-matter interference system"
```

---

### Task 3: Matter Reconstructor

**Files:**
- Create: `src/Core/MatterReconstructionPolicy.cs`
- Create: `src/Game/Buildings/Reconstructor/MatterReconstructorConfig.cs`
- Create: `src/Game/Buildings/Reconstructor/MatterReconstructorController.cs`
- Create: `src/Game/Recipes/ReconstructionRecipeRegistry.cs`
- Create: `tests/MatterReconstructionPolicyTests.cs`
- Modify: `tests/CoreTestProgram.cs`
- Modify: localization files
- Create: `assets/scml/matter_reconstructor/*`
- Modify: `assets/animation-manifest.json`
- Modify: safe-removal registration/controller

**Interfaces:**
- Consumes analyzed-material state already used by the compiler.
- Produces explicit whitelist recipes with ordinary input + proto-matter cost -> ordinary output.
- Implements `ForbiddenTechDevice` interference pause behavior.

- [ ] **Step 1: Write policy tests for allowed conversion and mass accounting**

```csharp
var recipe = MatterReconstructionPolicy.CreateRecipe(sourceMass: 1000f, protoMatterMass: 100f, outputMass: 1000f);
Assert.True(recipe.IsMassSafe);
```

Also reject negative mass, zero proto-matter cost, and outputs that exceed documented mass bounds.

- [ ] **Step 2: Verify test failure**

```powershell
./test.ps1 -Suite MatterReconstructionPolicyTests
```

- [ ] **Step 3: Implement deterministic policy and whitelist registry**

Do not generate every possible element pair. Use named recipe groups whose outputs require prior analysis.

- [ ] **Step 4: Implement ONI building**

Use the existing fabricator pattern. Add power, automation, prioritizable, copy-settings, storage and `ForbiddenTechDevice`. Interference pauses work without destroying the order.

- [ ] **Step 5: Add localization and KAnim source assets**

Include NAME/DESC/EFFECT/status strings and UI animation timeline/icon.

- [ ] **Step 6: Add safe removal**

Stored proto-matter and ordinary input must be released/converted according to existing safe-removal rules.

- [ ] **Step 7: Run tests/build/assets**

```powershell
./test.ps1 -Suite All
./build-assets.ps1
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
./verify-package.ps1
```

Expected: all automated gates PASS.

- [ ] **Step 8: Commit**

```bash
git add src tests assets packaging docs/test-matrix.md
git commit -m "feat(phase2): add matter reconstructor"
```

---

### Task 4: Entropy Flux Diverter

**Files:**
- Create: `src/Core/EntropyFluxPolicy.cs`
- Create: `src/Game/Buildings/EntropyDiverter/EntropyFluxDiverterConfig.cs`
- Create: `src/Game/Buildings/EntropyDiverter/EntropyFluxDiverterController.cs`
- Create: `tests/EntropyFluxPolicyTests.cs`
- Modify: `tests/CoreTestProgram.cs`
- Modify: localization/assets/manifest/safe-removal files

**Interfaces:**
- First version accepts two liquid streams only.
- `EntropyFluxPolicy` computes allowed energy transfer without inventing or deleting energy.
- Controller consumes proto-matter and power only while an actual transfer is performed.

- [ ] **Step 1: Write energy/mass conservation tests**

```csharp
var result = EntropyFluxPolicy.Transfer(hotMass, hotShc, hotTemp, coldMass, coldShc, coldTemp, requestedDtu);
Assert.NearlyEqual(result.DtuRemovedFromHot, result.DtuAddedToCold);
Assert.NearlyEqual(hotMass, result.HotMass);
Assert.NearlyEqual(coldMass, result.ColdMass);
```

Test freeze/boil guard limits and zero-transfer cases.

- [ ] **Step 2: Verify failure**

```powershell
./test.ps1 -Suite EntropyFluxPolicyTests
```

- [ ] **Step 3: Implement pure heat-transfer policy**

Clamp requested transfer before either stream crosses its safe phase boundary. Return an explicit safe-transfer result rather than mutating game objects in Core.

- [ ] **Step 4: Implement dual-liquid building**

Use explicit hot/cold input and output ports. If an output is blocked, stop processing and keep source mass intact. Under interference, stop active transfer.

- [ ] **Step 5: Add localization, KAnim, config/build/research integration, and safe removal**

- [ ] **Step 6: Run all automated gates**

```powershell
./test.ps1 -Suite All
./build-assets.ps1
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
./verify-package.ps1
```

- [ ] **Step 7: Commit**

```bash
git add src tests assets packaging docs/test-matrix.md
git commit -m "feat(phase2): add entropy flux diverter"
```

---

### Task 5: Matter Annihilation Reactor state machine and loss policy

**Files:**
- Create: `src/Core/AnnihilationReactorPolicy.cs`
- Create: `tests/AnnihilationReactorPolicyTests.cs`
- Modify: `tests/CoreTestProgram.cs`

**Interfaces:**
- Produces deterministic states: `Offline`, `Charging`, `Stable`, `Fluctuating`, `Critical`, `Decohered`, `CoolingLockout`.
- Produces bounded proto-matter loss and heat-pulse calculations.

- [ ] **Step 1: Write state-transition tests**

```csharp
Assert.Equal(ReactorState.Charging, policy.Next(ReactorState.Offline, startRequested));
Assert.Equal(ReactorState.Fluctuating, policy.Next(ReactorState.Stable, coolingInsufficient));
Assert.Equal(ReactorState.Critical, policy.Next(ReactorState.Fluctuating, instabilityPersists));
Assert.Equal(ReactorState.Decohered, policy.Next(ReactorState.Critical, recoveryFailed));
```

Also test successful recovery and cooling lockout release.

- [ ] **Step 2: Write loss/heat bounds tests**

Loss must never exceed the current reaction batch; heat pulse must be finite and non-negative.

- [ ] **Step 3: Run and verify failure**

```powershell
./test.ps1 -Suite AnnihilationReactorPolicyTests
```

- [ ] **Step 4: Implement the minimal deterministic policy**

No Unity types or ElementLoader access in the pure policy.

- [ ] **Step 5: Run all core tests**

```powershell
./test.ps1 -Suite All
```

- [ ] **Step 6: Commit**

```bash
git add src/Core/AnnihilationReactorPolicy.cs tests
git commit -m "feat(phase2): define annihilation reactor state policy"
```

---

### Task 6: Matter Annihilation Reactor game implementation

**Files:**
- Create: `src/Game/Buildings/AnnihilationReactor/MatterAnnihilationReactorConfig.cs`
- Create: `src/Game/Buildings/AnnihilationReactor/MatterAnnihilationReactorController.cs`
- Create: `src/Game/Buildings/AnnihilationReactor/MatterAnnihilationReactorStates.cs` if the controller would otherwise become oversized
- Modify: `src/Game/Interference/ProtoMatterInterferenceManager.cs`
- Modify: localization/assets/manifest/safe-removal files
- Create: `assets/scml/matter_annihilation_reactor/*`

**Interfaces:**
- Consumes the pure reactor policy from Task 5.
- Emits range interference through `ProtoMatterInterferenceManager` only when decoherence occurs.
- Never directly damages or deletes ordinary buildings.

- [ ] **Step 1: Implement charge and stable-generation states**

The reactor must consume external power during charge/constraint establishment before it can enter net-generation mode.

- [ ] **Step 2: Implement cooling and stability signals**

Cooling failure moves the policy state through `Fluctuating` and `Critical`; automation/power transitions must not silently skip the policy.

- [ ] **Step 3: Implement decoherence event**

On `Decohered` transition:

```csharp
var lostMass = policy.CalculateProtoMatterLoss(currentBatchMass);
var pulse = policy.CalculateHeatPulse(lostMass);
interferenceManager.Activate(sourceInstanceId, cell, radius, duration);
```

Apply resource loss and heat once, not once per simulation tick.

- [ ] **Step 4: Implement cooling lockout and restart**

The building remains intact. It becomes restartable only after the documented temperature/stability condition is satisfied.

- [ ] **Step 5: Add visual states**

Provide idle/charge/working/unstable/decohere/locked UI and world animation states; decoherence uses field-collapse visuals, not a generic explosion.

- [ ] **Step 6: Add safe removal and serialization checks**

A save made in every reactor state must reload without invalid state or duplicate one-shot effects.

- [ ] **Step 7: Run all automated gates**

```powershell
./test.ps1 -Suite All
./build-assets.ps1
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
./verify-package.ps1
```

- [ ] **Step 8: Commit**

```bash
git add src tests assets packaging docs/test-matrix.md
git commit -m "feat(phase2): add matter annihilation reactor"
```

---

### Task 7: Phase-2 integration, safe removal, and real-game validation

**Files:**
- Modify: `docs/test-matrix.md`
- Modify packaging/build scripts only if a real second-stage packaging failure requires it
- Modify source only for defects reproduced during integration

**Interfaces:**
- Produces a distributable package whose automated and in-game evidence is recorded in the test matrix.

- [ ] **Step 1: Execute portable/core suite**

```powershell
./test.ps1 -Suite All
```

Expected: all tests PASS.

- [ ] **Step 2: Build assets and DLL/package**

```powershell
./build-assets.ps1
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
./verify-package.ps1
```

- [ ] **Step 3: Install locally and verify installed hashes match dist**

Use the repository's existing install/verification flow; do not manually copy only selected generated files.

- [ ] **Step 4: Fully restart ONI through Steam**

```powershell
Start-Process 'D:\steam\steam.exe' -ArgumentList '-applaunch','457140' -WindowStyle Hidden
```

- [ ] **Step 5: Execute colony gameplay matrix**

Verify research, build menu, icons, construction, operation, interference between forbidden buildings, unaffected ordinary buildings, save/reload, config disable behavior, and safe removal.

- [ ] **Step 6: Force one controlled reactor decoherence**

Confirm proto-matter loss, one heat pulse, forbidden-device interference, lockout/recovery, no ordinary-building damage, and no permanent reactor destruction.

- [ ] **Step 7: Check `Player.log`**

No repeating exception attributable to this mod is allowed.

- [ ] **Step 8: Update only actually executed test-matrix rows**

Do not mark unexecuted DLC/content-mode rows PASS.

- [ ] **Step 9: Final commit**

```bash
git add docs/test-matrix.md src tests assets packaging
git commit -m "test(phase2): validate proto-matter field integration"
```
