# Matter Reconstructor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Phase-2 Matter Reconstructor as a 4×4 Forbidden Technology fabricator that consumes 1000 kg of a tier-matched reality substrate plus Proto-Matter to produce exactly 1000 kg of an analyzed target material.

**Architecture:** Keep deterministic reconstruction math and tier mapping in `src/Core`, then adapt those rules into ONI `ComplexRecipe` objects in `src/Game/Recipes`. The building follows the existing ComplexFabricator patterns, reuses `ForbiddenTechDevice`, and is registered only after its real `IBuildingConfig` exists. Research, localization, safe removal, assets, and runtime contracts are updated in the same branch without changing published Phase-1 IDs.

**Tech Stack:** C# 7.3, Oxygen Not Included `Assembly-CSharp.dll`, Harmony, PLib, PowerShell contract tests, ONI ComplexFabricator/Storage/SolidConduit APIs, SCML/KAnim asset pipeline.

**Spec:** `docs/superpowers/specs/2026-09-17-matter-reconstructor-design.md`

## Global Constraints

- Stable building ID is `BaiyeMatterReconstructor`; stable Phase-2 research ID is `BaiyeForbiddenProtoFieldEngineering`.
- Base recipe invariant: `1000 kg reality substrate + X kg Proto-Matter -> 1000 kg target material`; Proto-Matter never contributes product mass.
- Reconstruction targets must be active `ElementCatalogAdapter.Rules` entries and must already be analyzed/unlocked.
- Tier ladder: Common<-Common 50 kg, OreOrOrganic<-Common 75 kg, Industrial<-OreOrOrganic 100 kg, Rare<-Industrial 200 kg, Endgame<-Rare 300 kg, multiplied by `CostMultiplier`.
- Building: 4×4, floor, 180 s construction, 600 kg RefinedMetal + 200 kg Ceramic + 100 kg Glass, 4800 W × `PowerMultiplier`, 24 kDTU/s × `HeatMultiplier`, 120 s recipe time.
- Inputs use one solid conveyor input and manual delivery; output uses one solid conveyor output. Input capacity 2200 kg; output capacity 1100 kg.
- `ForbiddenTechDevice` is mandatory; interference pauses through `Operational` without clearing queue or storages.
- Disabling `ReconstructorEnabled` hides future registration but must never destroy existing instances or storage.
- Do not hand-edit `packaging/anim` or `dist`; use `assets/scml`, `assets/animation-manifest.json`, and `build-assets.ps1`.
- Preserve the verified global `STRINGS` localization root even though current game builds emit non-fatal CS0437 warnings.
- Full game-dependent verification remains `./test.ps1 -Suite All -GamePath <ONI path>` on the user's local installation.

---

### Task 1: Reconstruction Core Policy

**Files:**
- Create: `src/Core/ReconstructionPolicy.cs`
- Create: `tests/ReconstructionPolicyTests.cs`
- Modify: `tests/TestRunner.cs`

**Interfaces:**
- Produces: `MaterialTier ReconstructionPolicy.RequiredSubstrateTier(MaterialTier targetTier)`
- Produces: `float ReconstructionPolicy.BaseProtoMatterKg(MaterialTier targetTier)`
- Produces: `ReconstructionPlan ReconstructionPolicy.CreatePlan(MaterialRule targetRule, ResolvedOptions options)`
- Produces immutable `ReconstructionPlan` with `TargetElementId`, `TargetTier`, `SubstrateTier`, `SubstrateKg`, `ProtoMatterKg`, `ProductKg`, and `TimeSeconds`.

- [ ] **Step 1: Write failing Core tests** covering all five tier mappings, exact 1000 kg substrate/product equality, Proto-Matter base costs, `CostMultiplier`, invalid/null input, non-finite/non-positive multiplier rejection, and the invariant that Proto-Matter never increases product mass.

- [ ] **Step 2: Run Portable tests and verify red**

Run: `./test.ps1 -Suite Portable`

Expected: FAIL because `ReconstructionPolicy` / `ReconstructionPlan` do not exist.

- [ ] **Step 3: Implement minimal Core policy** using only Core types; no Unity, `ElementLoader`, or game assembly dependencies.

- [ ] **Step 4: Run Portable tests and verify green**

Run: `./test.ps1 -Suite Portable`

Expected: PASS with all reconstruction policy assertions.

- [ ] **Step 5: Commit**

```bash
git add src/Core/ReconstructionPolicy.cs tests/ReconstructionPolicyTests.cs tests/TestRunner.cs
git commit -m "feat(phase2): add reconstruction core policy"
```

### Task 2: Reconstruction Recipe Adapter and Analyzed-Target Filtering

**Files:**
- Modify: `src/Game/Recipes/RecipeRegistry.cs`
- Create: `src/Game/Recipes/ReconstructionRecipeAdapter.cs`
- Create: `tests/ReconstructionRecipeSourceContractTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Consumes: `ReconstructionPolicy.CreatePlan(...)` from Task 1.
- Produces: `IReadOnlyDictionary<string, ComplexRecipe> RecipeRegistry.ReconstructorRecipes`.
- Produces: `ReconstructionRecipeAdapter.RebuildUnlockedRecipes()` that only exposes active catalog elements already present in the Phase-1 analysis save data.
- Uses stable substrate tags `BaiyeRealitySubstrateCommon`, `BaiyeRealitySubstrateOreOrOrganic`, `BaiyeRealitySubstrateIndustrial`, and `BaiyeRealitySubstrateRare`; Endgame targets consume Rare substrate and no separate Endgame substrate tag is required for v1.

- [ ] **Step 1: Write failing source contract** asserting the reconstructor recipe dictionary exists, recipe results are exactly 1000 kg target material, Proto-Matter is a separate consumed ingredient, recipes are bound only to `BaiyeMatterReconstructor`, and filtering consults persisted analyzed materials rather than exposing every catalog rule.

- [ ] **Step 2: Run Portable tests and verify red**

Run: `./test.ps1 -Suite Portable`

Expected: FAIL because reconstruction recipe sources do not exist.

- [ ] **Step 3: Implement adapter and registry integration**. Keep recipe generation separate from `RecipeRegistry.Build()` so unlock changes can refresh reconstructor recipes without rebuilding the entire element catalog. Never replace a running fabricator's current recipe; the runtime component in Task 4 will defer refresh while work is active.

- [ ] **Step 4: Run Portable tests and verify green**

Run: `./test.ps1 -Suite Portable`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Game/Recipes/RecipeRegistry.cs src/Game/Recipes/ReconstructionRecipeAdapter.cs tests/ReconstructionRecipeSourceContractTests.ps1 test.ps1
git commit -m "feat(phase2): add reconstruction recipe adapter"
```

### Task 3: Material Tier Tags for Reality Substrates

**Files:**
- Modify: `src/Game/Elements/ElementCatalogAdapter.cs`
- Create: `src/Game/Elements/ReconstructionSubstrateTags.cs`
- Create: `tests/ReconstructionSubstrateTagContractTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Consumes: existing `MaterialClassifier` / `MaterialRule.Tier` classification.
- Produces: stable ONI `Tag` values for each substrate tier and attaches exactly one reconstruction substrate tag to eligible solid element prefabs after element loading.

- [ ] **Step 1: Write failing contract** asserting stable tag IDs, no Proto-Matter tagging, and mapping from material tier to the expected substrate tag.

- [ ] **Step 2: Run Portable tests and verify red**.

- [ ] **Step 3: Implement tag helper and adapter hook** without creating a second classification system. Elements excluded from `ElementCatalogAdapter.Rules` receive no reconstruction substrate tag.

- [ ] **Step 4: Run Portable tests and verify green**.

- [ ] **Step 5: Commit** with `feat(phase2): tag reconstruction substrates`.

### Task 4: Matter Reconstructor Building and Runtime Fabricator

**Files:**
- Create: `src/Game/Buildings/Reconstructor/MatterReconstructorConfig.cs`
- Create: `src/Game/Buildings/Reconstructor/MatterReconstructor.cs`
- Create: `tests/ReconstructorConfigSourceContractTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Consumes: `RecipeRegistry.ReconstructorRecipes`, substrate tags, `ProtoMatterRegistration.Tag`, and `ForbiddenTechDevice`.
- Produces: real `IBuildingConfig` for `BaiyeMatterReconstructor` and a `ComplexFabricator` subclass that safely refreshes recipes after new material analyses.

- [ ] **Step 1: Write failing config/runtime contract** asserting 4×4 footprint, 4800 W base draw, 24 kDTU/s base heat, 4×4 animation ID, 2200 kg input storage, 1100 kg output storage, one solid input/output, power + automation, `ForbiddenTechDevice`, and no coolant conduit.

- [ ] **Step 2: Run Portable tests and verify red**.

- [ ] **Step 3: Implement `MatterReconstructor` runtime** following Matter Compiler's safe recipe-refresh pattern: keep the active recipe/work order stable, refresh only when idle, drop storages on deconstruction, and set an output-space `Operational.Flag` so a blocked rail never destroys finished product.

- [ ] **Step 4: Implement `MatterReconstructorConfig`** using `ComplexFabricator`, sealed storages, manual delivery-compatible filters, `SolidConduitConsumer`, `SolidConduitDispenser`, `LogicOperationalController`, `PoweredActiveController.Def`, `CopyBuildingSettings`, `Prioritizable`, and `ForbiddenTechDevice`.

- [ ] **Step 5: Run Portable tests and verify green**.

- [ ] **Step 6: Commit** with `feat(phase2): implement matter reconstructor building`.

### Task 5: Registration, Phase-2 Research, Localization, and Safe Removal

**Files:**
- Modify: `src/Game/Registration/BuildingRegistration.cs`
- Modify: `src/Game/Registration/ForbiddenResearchRegistration.cs`
- Modify: `src/Game/Safety/SafeRemovalController.cs`
- Modify: `src/Game/Localization/STRINGS.cs`
- Modify: `packaging/translations/zh.po`
- Modify: `packaging/translations/en.po`
- Modify: `src/Game/Options/ForbiddenTechOptions.cs`
- Create: `tests/ReconstructorIntegrationSourceContractTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Produces the first actually implemented Phase-2 building in the build menu and `BaiyeForbiddenProtoFieldEngineering` research.
- Phase-2 research prerequisite is Phase-1 `BaiyeForbiddenMatterEngineering` and cost is 160 basic + 120 advanced + 40 nuclear research points.
- Safe removal recognizes `MatterReconstructor` as a forbidden fabricator, returns ordinary substrate/output unchanged, converts stored Proto-Matter through the existing cleanup path, and removes the building.

- [ ] **Step 1: Write failing integration contract** for menu registration, research node creation/unlock, Chinese/English strings, safe removal inclusion, and the renamed config label `物质工程成本倍率` / equivalent English translation while retaining the serialized property name `CostMultiplier`.

- [ ] **Step 2: Run Portable tests and verify red**.

- [ ] **Step 3: Add implemented building ID to `BuildingRegistration.AllBuildingIds`**; do not add unfinished entropy diverter/reactor configs.

- [ ] **Step 4: Register `BaiyeForbiddenProtoFieldEngineering`** only when at least one implemented+enabled Phase-2 building exists, with Phase-1 research as prerequisite and only implemented+enabled Phase-2 items in its unlock list.

- [ ] **Step 5: Extend safe removal** to process/count `MatterReconstructor` and hide both Phase-1 and Phase-2 research/build entries after successful cleanup.

- [ ] **Step 6: Add localization** for building NAME/DESC/EFFECT, logic port, status messages, Phase-2 research name/description/search terms, recipe language, and configuration wording.

- [ ] **Step 7: Run Portable tests and verify green**.

- [ ] **Step 8: Commit** with `feat(phase2): register matter reconstructor gameplay integration`.

### Task 6: KAnim Source Assets and Package Contracts

**Files:**
- Create: `assets/scml/matter-reconstructor/` source PNG/SCML files
- Modify: `assets/animation-manifest.json`
- Modify or extend: `tests/AssetSourceContractTests.ps1`
- Generated only by scripts: `packaging/anim/*`, `dist/ForbiddenTechnologyPack/*`

**Interfaces:**
- Produces `baiye_matter_reconstructor_kanim` with `idle`, `working_pre`, `working_loop`, `working_pst`, and `off` timelines plus build-menu icon data.

- [ ] **Step 1: Add failing asset contract** requiring the new source module, manifest entry, exact required animation names, and menu icon timeline.

- [ ] **Step 2: Run `./test.ps1 -Suite Portable` and verify red**.

- [ ] **Step 3: Add source art/SCML and manifest** in the established generated-asset style. Do not edit generated KAnim files by hand.

- [ ] **Step 4: Run `./build-assets.ps1` and package verification locally where the animation compiler is available**.

- [ ] **Step 5: Run Portable tests and verify green**.

- [ ] **Step 6: Commit** with `feat(assets): add matter reconstructor animation`.

### Task 7: Full Integration Verification and Test Matrix

**Files:**
- Modify: `docs/test-matrix.md`
- Modify: `docs/phase-2-protomatter-field-development.md` only if implementation details differ from the phase-level baseline.

**Interfaces:**
- Verifies all prior tasks together against the user's installed ONI assemblies.

- [ ] **Step 1: Run Portable suite**

Run: `./test.ps1 -Suite Portable`

Expected: zero failures.

- [ ] **Step 2: Run full game-dependent suite on the user's ONI install**

Run: `./test.ps1 -Suite All -GamePath "D:\steam\steamapps\common\OxygenNotIncluded"`

Expected: zero failures and package DLL generated.

- [ ] **Step 3: Run build/package**

Run: `./build.ps1 -GamePath "D:\steam\steamapps\common\OxygenNotIncluded"`

Expected: `Wrote ...ForbiddenTechnologyPack.dll` and `Built package: ...dist\ForbiddenTechnologyPack`; known CS0437 `STRINGS` warnings remain non-fatal.

- [ ] **Step 4: Install and in-game validate** after a full ONI restart: research node visible after Phase-1 research, reconstructor menu/icon valid, manual + rail substrate/Proto-Matter input, 1000 kg output mass, analyzed-target filtering, automation/power/interference pause-resume, blocked output, save/reload mid-work, deconstruction, disabled-config save safety, and safe removal.

- [ ] **Step 5: Update `docs/test-matrix.md`** with static/runtime results separately from still-pending DLC/content-mode rows.

- [ ] **Step 6: Commit** with `test(phase2): verify matter reconstructor integration`.

## Plan Self-Review

- Spec coverage: mass semantics, tier ladder, analyzed-target requirement, building parameters, logistics, interference, research, config, save behavior, safe removal, DLC behavior, animation, localization, and verification all map to explicit tasks.
- Placeholder scan: no implementation step relies on TBD/TODO or an unspecified future helper.
- Type consistency: `ReconstructionPlan`, `ReconstructionPolicy`, `RecipeRegistry.ReconstructorRecipes`, `ReconstructionRecipeAdapter`, stable substrate tags, `MatterReconstructor`, and `BaiyeForbiddenProtoFieldEngineering` have one naming scheme throughout the plan.
