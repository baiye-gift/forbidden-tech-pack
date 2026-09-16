# Forbidden Technology Pack Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Repair the confirmed research, DLC/material classification, fabricator logistics, safe-removal, animation, and CI defects so the first Matter Compilation module is safe to test in every supported ONI content mode.

**Architecture:** Preserve the existing core/game-adapter split. Put deterministic category and acceptance rules in `src/Core`, keep ONI API calls in narrow `src/Game` adapters, and characterize the current game assembly wherever behavior depends on private or version-sensitive APIs. Every production fix begins with a regression test that fails for the confirmed defect.

**Tech Stack:** C# 7.3, current locally installed Oxygen Not Included APIVersion 2 assemblies, Harmony 2, PLib 4.25.0, PowerShell 7, ILRepack, kanimal-SE, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-16-forbidden-technology-pack-design.md`

## Global Constraints

- Keep one mod and preserve all published IDs and serialized field names.
- Support the base game and DLC IDs `EXPANSION1_ID`, `DLC2_ID`, `DLC3_ID`, `DLC4_ID`, and `DLC5_ID` without making any DLC a hard dependency.
- Never accept food, seeds, eggs, creatures, artifacts, quest items, neutronium, liquids, gases, disabled elements, or unavailable DLC elements.
- Do not lose mass, temperature, disease data, stored items, or active-batch progress.
- Safe removal must leave no Proto-Matter prefab, custom element, or custom building, including inactive objects.
- Use test-first red/green cycles and commit each task independently.
- Local game-dependent tests take an explicit game path; hosted CI runs only portable suites and must not depend on `D:\steam`.
- Do not publish, push, merge, or alter unrelated user files.

---

### Task 1: Register a Real Research Tree Node

**Files:**
- Modify: `src/Game/Registration/ForbiddenResearchRegistration.cs`
- Create: `tests/ResearchRegistrationRuntimeTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Consumes: `ModIdentity.ResearchId`, `RegistrationPolicy.Create`, the `Database.Techs.Load(TextAsset)` lifecycle, and current `ResourceTreeNode` geometry.
- Produces: one surviving `Tech` with a non-null node, prerequisite relation, valid category, costs, and enabled building IDs.

- [ ] **Step 1: Write the failing runtime contract**

  Build the raw mod assembly and compile a reflection probe that requires the registration Harmony patch to target `Database.Techs.Load`, requires a node-construction helper, supplies literal prerequisite nodes, and asserts the returned custom node has ID `BaiyeForbiddenMatterEngineering`, positive dimensions, and a position outside the existing node bounds. It must fail against the current `Techs.Init` postfix.

- [ ] **Step 2: Run the focused suite and verify RED**

  Run `./test.ps1 -Suite ResearchRegistrationRuntimeTests` and confirm failure states that registration does not target `Techs.Load` or does not create a node.

- [ ] **Step 3: Implement the minimal registration fix**

  Move registration to a `Database.Techs.Load` prefix so the tech exists before the base method calculates tiers and removes node-less resources. Parse the provided tree with `ResourceTreeLoader<ResourceTreeNode>`, select `MatterDeconstruction` or `HighTempForging`, create a distinct node to the right of the existing tree with the same dimensions and row as the prerequisite, call `Tech.SetNode`, and link `requiredTech`/`unlockedTech`. In a postfix, copy the prerequisite category and add a visible edge using the actual prerequisite node. Guard by `__instance.TryGet(ModIdentity.ResearchId)` rather than a process-global static boolean.

- [ ] **Step 4: Run GREEN and regression suites**

  Run the focused suite, then `./test.ps1 -Suite All`.

- [ ] **Step 5: Commit**

  Commit as `fix: register forbidden research tree node`.

---

### Task 2: Correct DLC and Material Classification

**Files:**
- Modify: `src/Core/MaterialClassifier.cs`
- Modify: `src/Game/Elements/ElementCatalogAdapter.cs`
- Modify: `tests/MaterialClassifierTests.cs`
- Create: `tests/ElementCatalogRuntimeTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Consumes: actual ONI element categories/tags and `DlcManager.IsContentSubscribed(string)`.
- Produces: correct tiers for Copper Ore/Iron Ore, Algae/Slime, Dirt/Fertilizer, common raw minerals, industrial, rare, and endgame solids; newer DLCs remain eligible only when subscribed.

- [ ] **Step 1: Add failing classifier cases**

  Add literal descriptors matching current game data: `Cuprite` with `Metal` and `Ore` must be `OreOrOrganic`; `Algae` with `Organics` must be `OreOrOrganic`; `Dirt` with `Farmable` and a fertilizer-like item with `Agriculture` must be `Common`. Existing deny-list cases must remain denied.

- [ ] **Step 2: Add a failing game-API contract**

  Build the mod and use reflection/IL metadata to require `ElementCatalogAdapter.IsDlcActive` to call `DlcManager.IsContentSubscribed`, not obsolete `IsContentActive`. Confirm the current game assembly exposes DLC2 through DLC5 and `IsContentActive` rejects them.

- [ ] **Step 3: Run focused RED suites**

  Run the material classifier suite and `ElementCatalogRuntimeTests`; record the expected category and obsolete-API failures.

- [ ] **Step 4: Implement exact canonical mappings**

  Recognize `Metal` plus `Ore`, `Organics`, and `ConsumableOre` as `OreOrOrganic`; recognize `Farmable`, `Agriculture`, `BuildableRaw`, and `RawMineral` as `Common`. Replace `DlcManager.IsContentActive` with `DlcManager.IsContentSubscribed` while retaining vanilla empty-ID behavior.

- [ ] **Step 5: Run GREEN and full suite**

  Run both focused suites and `./test.ps1 -Suite All`.

- [ ] **Step 6: Commit**

  Commit as `fix: support current dlc material catalog`.

---

### Task 3: Make Analyzer Completion and Crusher Rail Input Safe

**Files:**
- Modify: `src/Game/Buildings/Analyzer/MatterAnalyzer.cs`
- Modify: `src/Game/Buildings/Crusher/MassCrusherConfig.cs`
- Modify: `tests/AnalyzerRecipeRuntimeTests.ps1`
- Modify: `tests/CrusherConfigContractTests.ps1`

**Interfaces:**
- Produces: analyzer completion never replaces a recipe list while another order is active; crusher rail consumer leaves non-crushable packets on the rail.

- [ ] **Step 1: Extend the analyzer runtime test and verify RED**

  Require `MatterAnalyzer` to override `CompleteWorkingOrder`, capture the completed recipe itself, and coordinate a private `Operational.Flag` so base completion cannot start the next order before unlock-driven recipe refresh. Require `RefreshRecipes` to defer when `CurrentWorkingOrder` is non-null. The test must fail against the Harmony postfix implementation.

- [ ] **Step 2: Extend the crusher contract and verify RED**

  Require a Harmony prefix for `SolidConduitConsumer.ConduitUpdate` scoped to `MassCrusher`, reading the packet at the configured input cell and returning false when `ElementCatalogAdapter.Rules` has no crushable rule for its element. Require valid packets and empty cells to continue into the original method.

- [ ] **Step 3: Implement analyzer completion coordination**

  Replace the global `ComplexFabricator.CompleteWorkingOrder` Harmony patch with a `MatterAnalyzer.CompleteWorkingOrder` override. Use a requirement flag initialized true on spawn; set it false around base completion, unlock the captured ingredient after completion, refresh only when no order is active, and restore the flag in `finally`. Preserve the unlock event subscription for external changes.

- [ ] **Step 4: Implement crusher rail filtering**

  Add a narrowly scoped `MassCrusherRailInputPatch` parallel to the compiler input patch. Compute the crusher input cell from its building offset, inspect the actual pickupable element, and skip the original consumer update for elements that are absent from the catalog or rejected by `CrusherPolicy.CanCrush`.

- [ ] **Step 5: Run focused and full suites**

  Run `AnalyzerRecipeRuntimeTests`, `CrusherConfigContractTests`, and then all suites.

- [ ] **Step 6: Commit**

  Commit as `fix: stabilize analyzer and crusher logistics`.

---

### Task 4: Replace Custom Prefabs During Safe Removal

**Files:**
- Modify: `src/Game/Safety/SafeRemovalController.cs`
- Create: `tests/SafeRemovalRuntimeTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Produces: inclusive inactive-object discovery and a replacement path that spawns native Igneous Rock with identical mass, temperature, disease index/count, then destroys the Proto-Matter GameObject.

- [ ] **Step 1: Write the failing runtime contract**

  Inspect the compiled method bodies and require safe-removal scans to use `Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)`. Require conversion to invoke the native Igneous Rock substance `SpawnResource` path and `Util.KDestroyGameObject`, and forbid `PrimaryElement.SetElement` as the conversion mechanism.

- [ ] **Step 2: Run focused RED**

  Run `./test.ps1 -Suite SafeRemovalRuntimeTests`; confirm it fails on inactive discovery and prefab replacement.

- [ ] **Step 3: Implement replacement conversion**

  Centralize inclusive object discovery. For each Proto-Matter object, capture world position, mass, temperature, disease index, and disease count; spawn an Igneous Rock resource from the native substance and destroy the original custom object. Count remaining custom objects with the same inclusive scans. Keep building deconstruction and storage drops unchanged except for inclusive discovery.

- [ ] **Step 4: Run GREEN and all suites**

  Run focused safe-removal tests, core safe-removal tests, and all suites. Confirm obsolete `FindObjectsOfType` warnings are gone.

- [ ] **Step 5: Commit**

  Commit as `fix: remove custom prefabs during safe cleanup`.

---

### Task 5: Complete Required Fabricator Animations

**Files:**
- Modify: `assets/animation-manifest.json`
- Modify: `assets/scml/matter_analyzer/baiye_matter_analyzer.scml`
- Modify: `assets/scml/mass_crusher/baiye_mass_crusher.scml`
- Modify: `assets/scml/matter_compiler/baiye_matter_compiler.scml`
- Modify: `tests/AssetSourceContractTests.ps1`

**Interfaces:**
- Produces: each `ComplexFabricatorSM` KAnim includes `off`, `idle`, `working_pre`, `working_loop`, `working_pst`, and `working_pst_complete`.

- [ ] **Step 1: Change expected animation contracts and verify RED**

  Add `working_pre` and `working_pst_complete` to all three building expectations and manifest expectations. Run `AssetSourceContractTests` and confirm missing-state failure.

- [ ] **Step 2: Add minimal original SCML states**

  Add unique animation IDs. `working_pre` is a non-looping transition into the existing working pose; `working_pst_complete` is a non-looping transition back to idle. Reuse existing source sprites/timelines without introducing generated binary-only sources.

- [ ] **Step 3: Compile and verify KAnim assets**

  Run the asset contract, `build-assets.ps1`, package verification, and all suites.

- [ ] **Step 4: Commit**

  Commit as `fix: complete fabricator animation states`.

---

### Task 6: Separate Portable CI From Local Game Integration Tests

**Files:**
- Modify: `test.ps1`
- Modify: `tests/AnalyzerAdapterContractTests.ps1`
- Modify: `tests/AnalyzerRecipeRuntimeTests.ps1`
- Modify: `tests/OptionsLocalizationRuntimeTests.ps1`
- Modify: new game-runtime tests from Tasks 1, 2, and 4
- Modify: `.github/workflows/feature-verification.yml`
- Modify: `tests/ReleaseWorkflowContractTests.ps1`

**Interfaces:**
- Produces: `./test.ps1 -Suite Portable` for hosted CI; `./test.ps1 -Suite All -GamePath <path>` for release/local integration.

- [ ] **Step 1: Write failing workflow contracts**

  Require hosted workflow to call `-Suite Portable`, forbid repository scripts from embedding `D:\steam`, and require every game-dependent suite to accept `-GamePath` from `test.ps1`.

- [ ] **Step 2: Run RED**

  Run `ReleaseWorkflowContractTests`; confirm failures identify hosted `All` and absolute game paths.

- [ ] **Step 3: Implement suite routing**

  Add a named portable suite containing core C# tests plus source/package/workflow tests that need no Klei assemblies. Add `GamePath` to `test.ps1`, pass it to game-dependent child scripts, and make those scripts validate the supplied path. Update GitHub Actions to use Portable; keep local All as the stronger release gate.

- [ ] **Step 4: Run GREEN locally**

  Run Portable without a game path and All with `D:\steam\steamapps\common\OxygenNotIncluded`. Both must pass.

- [ ] **Step 5: Commit**

  Commit as `ci: separate portable and game integration tests`.

---

### Task 7: Final Integration, Installation, and Game Validation

**Files:**
- Modify: `docs/test-matrix.md`
- Modify only if evidence requires: production/test files touched above

**Interfaces:**
- Produces: verified package installed at the one approved local mod path; startup log and UI evidence for this build; honest test-matrix status.

- [ ] **Step 1: Run final static gates**

  Run `git diff --check`, Portable, All with the explicit game path, `build.ps1`, `verify-package.ps1`, and `pack-release.ps1`. Record exact counts and warnings.

- [ ] **Step 2: Install and compare**

  Run `install.ps1` with the explicit game path and compare relative file lists and SHA256 hashes between `dist/ForbiddenTechnologyPack` and the installed directory.

- [ ] **Step 3: Run a reversible game startup test**

  Confirm ONI is not already running. Back up `mods.json` byte-for-byte, enable only this local mod for the active content mode, launch through Steam, wait for the main menu readiness log marker, inspect `Player.log` for exceptions from this mod, close the test process, and restore the original `mods.json` byte-for-byte.

- [ ] **Step 4: Inspect configuration and research evidence**

  Confirm the PLib options metadata remains Chinese. If an automated UI path is unavailable, leave visual UI and in-colony research/build-menu rows PENDING rather than claiming them.

- [ ] **Step 5: Update only evidence-backed matrix rows**

  Mark only executed checks PASS; keep unexecuted colony, production, save/reload, DLC-mode, and safe-removal scenarios PENDING.

- [ ] **Step 6: Commit**

  Commit as `test: record remediation verification evidence`.
