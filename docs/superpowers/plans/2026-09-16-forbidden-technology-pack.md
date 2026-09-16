# Forbidden Technology Pack Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build one configurable Oxygen Not Included mod containing the solid-material Matter Compiler production chain: Matter Analyzer, Mass Crusher, Matter Compiler, and the transportable Proto-Matter element.

**Architecture:** Keep balance, classification, conversion, configuration resolution, and unlock rules in game-independent C# so they can be tested without launching ONI. Thin game adapters translate ONI `Element`, save, fabricator, conduit, automation, research, and configuration APIs into those core interfaces; all buildings and future modules ship in one package under stable IDs.

**Tech Stack:** C# 7.3, .NET Framework compiler, Oxygen Not Included APIVersion 2 assemblies, Harmony 2, PLib 4.25.0 (`POptions`, localization, building helpers), Newtonsoft.Json, PowerShell build/test scripts, ILRepack 2.0.48, kanimal-SE 1.3.31.

**Spec:** `docs/superpowers/specs/2026-09-16-forbidden-technology-pack-design.md`

## Global Constraints

- Ship one mod with static ID `Baiye.ForbiddenTechnologyPack`; do not split buildings into separate mods or require a separately enabled library mod.
- Support the base game and every DLC combination accepted by the current game build; DLC-only elements are optional data, never hard dependencies.
- First release compiles solid elements only; liquids, gases, living entities, eggs, seeds, food, artifacts, quest items, and neutronium remain out of scope.
- Keep published IDs stable: `BaiyeForbiddenProtoMatter`, `BaiyeMatterAnalyzer`, `BaiyeMassCrusher`, `BaiyeMatterCompiler`, and `BaiyeForbiddenMatterEngineering`.
- Default to the Strong preset: 90% recovery, 100% recipe cost, 100% power/heat, advanced materials enabled, 10 kg samples consumed.
- Preserve inputs, outputs, and batch progress on power loss, automation disable, cooling failure, blocked output, cancellation, save/load, and deconstruction.
- Never allow compile-then-crush conversion to increase Proto-Matter.
- Disabling a building hides future construction but does not delete existing instances; uninstall requires the safe-removal workflow.
- Do not modify ONI installation files or another mod. Build only against read-only assemblies under the supplied `-GamePath`.
- Public README and release archives must not contain local absolute paths, personal presets, logs, backups, or development-only material.
- Pin PLib 4.25.0, ILRepack 2.0.48, and kanimal-SE 1.3.31; verify package hashes before extraction.

---

## File Map

The implementation creates these focused units:

- `src/Core/ModIdentity.cs`: stable public IDs and semantic version.
- `src/Core/PackOptions.cs`: presets, custom values, clamping, and immutable resolved settings.
- `src/Core/MaterialModel.cs`: game-independent element descriptors, material tiers, and recipe rules.
- `src/Core/MaterialClassifier.cs`: allow/deny policy and tier assignment.
- `src/Core/ConversionMath.cs`: mass/cost calculations and round-trip safety checks.
- `src/Core/UnlockState.cs`: versioned colony unlock set and migration.
- `src/Core/RecipePlan.cs`: game-independent analyzer, crusher, and compiler recipe amounts.
- `src/Core/AnalyzerPolicy.cs`, `CrusherPolicy.cs`, `CompilerRecipeFilter.cs`: building decisions that can be tested without ONI.
- `src/Core/RegistrationPolicy.cs`: module/building visibility decisions.
- `src/Game/ModEntryPoint.cs`: PLib/Harmony initialization and options registration.
- `src/Game/Options/ForbiddenTechOptions.cs`: PLib options UI mapped to `PackOptions`.
- `src/Game/Elements/ProtoMatterRegistration.cs`: Proto-Matter substance and SimHash registration.
- `src/Game/Elements/ElementCatalogAdapter.cs`: cached ONI `Element` to core descriptor mapping.
- `src/Game/Save/ForbiddenTechSaveData.cs`: colony serialization and unlock events.
- `src/Game/Recipes/RecipeRegistry.cs`: analyzer, crusher, and compiler `ComplexRecipe` generation.
- `src/Game/Buildings/Analyzer/*`: analyzer config and completion handler.
- `src/Game/Buildings/Crusher/*`: crusher config, storage, rail ports, and fabricator behavior.
- `src/Game/Buildings/Compiler/*`: compiler config, filtered recipe list, rail ports, liquid cooling, and pause states.
- `src/Game/Registration/BuildingRegistration.cs`: plan screen, custom research, and module/building enable switches.
- `src/Game/Safety/SafeRemovalController.cs`: one-shot Proto-Matter conversion and cleanup report.
- `src/Game/Localization/STRINGS.cs`: English and Simplified Chinese strings.
- `packaging/elements/BaiyeForbiddenProtoMatter.yaml`: element physical definition.
- `assets/scml/*`: editable original animation sources.
- `packaging/anim/*`: compiled KAnim artifacts.
- `tests/*.cs`: game-independent executable test suites.
- `restore-deps.ps1`, `build.ps1`, `test.ps1`, `install.ps1`, `verify-package.ps1`: reproducible local workflow.

---

### Task 1: Reproducible Build, Test, and Package Skeleton

**Files:**
- Create: `.gitignore`
- Create: `restore-deps.ps1`
- Create: `build.ps1`
- Create: `test.ps1`
- Create: `install.ps1`
- Create: `verify-package.ps1`
- Create: `packaging/mod.yaml`
- Create: `packaging/mod_info.yaml`
- Create: `src/Core/ModIdentity.cs`
- Create: `src/Game/ModEntryPoint.cs`
- Create: `tests/TestHarness.cs`
- Create: `tests/CoreTestProgram.cs`
- Create: `tests/IdentityTests.cs`

**Interfaces:**
- Produces: `ModIdentity.StaticId`, `ModIdentity.Version`, `ModIdentity.ProtoMatterId`, and three stable building IDs.
- Produces: `build.ps1 -GamePath <path>`, `test.ps1`, `install.ps1 -GamePath <path>`, and `verify-package.ps1` commands used by every later task.

- [ ] **Step 1: Write the failing identity test and minimal test harness**

```csharp
// tests/IdentityTests.cs
using ForbiddenTechnologyPack.Core;

internal static class IdentityTests {
    public static void Run() {
        AssertEx.Equal("Baiye.ForbiddenTechnologyPack", ModIdentity.StaticId, "static id");
        AssertEx.Equal("BaiyeForbiddenProtoMatter", ModIdentity.ProtoMatterId, "element id");
        AssertEx.Equal("BaiyeMatterAnalyzer", ModIdentity.MatterAnalyzerId, "analyzer id");
        AssertEx.Equal("BaiyeMassCrusher", ModIdentity.MassCrusherId, "crusher id");
        AssertEx.Equal("BaiyeMatterCompiler", ModIdentity.MatterCompilerId, "compiler id");
    }
}
```

`TestHarness.cs` must implement `Equal<T>`, `SequenceEqual<T>`, `True`, `False`, `Near`, and `Throws<TException>`. `CoreTestProgram.Main(string[] args)` maps suite names to `Run` methods, accepts `--suite All` or a comma-separated suite list, calls `IdentityTests.Run()`, prints `TOTAL: 5 passed`, and returns nonzero after the first failed assertion. `test.ps1` declares `param([string[]]$Suite = @('All'))`, compiles all core/test sources, runs the C# suites selected by `-Suite`, and dispatches named PowerShell validation suites when they are added later.

- [ ] **Step 2: Run the test to verify it fails**

Run: `./test.ps1`

Expected: FAIL because `test.ps1` and `ModIdentity` do not exist.

- [ ] **Step 3: Add pinned dependency restoration and stable identity code**

`restore-deps.ps1` downloads and verifies these exact artifacts:

```powershell
$packages = @(
    @{ Name='PLib'; Uri='https://api.nuget.org/v3-flatcontainer/plib/4.25.0/plib.4.25.0.nupkg'; Sha256='5E0042E65BA9401682E9FFDA4E637E5083D4433814A26E52804FBE9BC1758324' },
    @{ Name='ILRepack'; Uri='https://api.nuget.org/v3-flatcontainer/ilrepack/2.0.48/ilrepack.2.0.48.nupkg'; Sha256='799017B829A6ED69FAC0D4FC0A874A4A6A9951F46D73A3CCE20BA016796B949F' },
    @{ Name='kanimal'; Uri='https://github.com/skairunner/kanimal-SE/releases/download/1.3.31/Windows.NET.dependent.zip'; Sha256='363C62CD38B7FDD5E14FAD7AF7D187CB94BCD61D5FA0EC7FAEBB4A9FB5413AAE' }
)
```

Extract `lib/net48/PLib.dll` to `lib/PLib.dll`, `tools/ILRepack.exe` plus its runtime config to `tools/`, and `kanimal-cli.exe` to `tools/`. Keep downloaded archives and generated tool directories ignored by Git.

```csharp
// src/Core/ModIdentity.cs
namespace ForbiddenTechnologyPack.Core {
    public static class ModIdentity {
        public const string StaticId = "Baiye.ForbiddenTechnologyPack";
        public const string Version = "0.1.0";
        public const string ProtoMatterId = "BaiyeForbiddenProtoMatter";
        public const string MatterAnalyzerId = "BaiyeMatterAnalyzer";
        public const string MassCrusherId = "BaiyeMassCrusher";
        public const string MatterCompilerId = "BaiyeMatterCompiler";
        public const string ResearchId = "BaiyeForbiddenMatterEngineering";
    }
}
```

- [ ] **Step 4: Add build and packaging scripts**

`build.ps1` must:

1. Validate `OxygenNotIncluded_Data\Managed` and `lib\PLib.dll`.
2. Compile every `src\*.cs` recursively with `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, `/langversion:7.3`, `/warn:4`, and references to `Assembly-CSharp`, `Assembly-CSharp-firstpass`, `0Harmony`, `Newtonsoft.Json`, `UnityEngine`, required Unity modules, `netstandard`, and PLib.
3. Write an intermediate `obj\ForbiddenTechnologyPack.raw.dll`.
4. Run ILRepack with the raw DLL as primary, merge only `PLib.dll`, and resolve game assemblies through `/lib:<Managed>`.
5. Copy metadata, `elements`, `anim`, and translations into `dist\ForbiddenTechnologyPack`.

`packaging/mod.yaml`:

```yaml
title: "禁忌科技建筑包 / Forbidden Technology Pack"
description: "一个可配置的强力建筑包。首发模块提供物质解析、粉碎与固体编译生产链。"
staticID: Baiye.ForbiddenTechnologyPack
```

`packaging/mod_info.yaml`:

```yaml
supportedContent: ALL
minimumSupportedBuild: 744825
APIVersion: 2
version: 0.1.0
```

- [ ] **Step 5: Add the minimal entry point**

```csharp
public sealed class ModEntryPoint : KMod.UserMod2 {
    public override void OnLoad(HarmonyLib.Harmony harmony) {
        base.OnLoad(harmony);
        PeterHan.PLib.Core.PUtil.InitLibrary(true);
        UnityEngine.Debug.Log("[ForbiddenTechnologyPack] 0.1.0 loaded");
    }
}
```

- [ ] **Step 6: Run scaffold verification**

Run:

```powershell
./restore-deps.ps1
./test.ps1
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
./verify-package.ps1
```

Expected: tests report `TOTAL: 5 passed`; package verification finds one merged `ForbiddenTechnologyPack.dll`, both YAML files, no standalone `PLib.dll`, and no absolute local path in package text files.

- [ ] **Step 7: Commit**

```powershell
git add .gitignore restore-deps.ps1 build.ps1 test.ps1 install.ps1 verify-package.ps1 packaging src tests
git commit -m "build: scaffold forbidden technology pack"
```

---

### Task 2: Configuration Presets and PLib Options Page

**Files:**
- Create: `src/Core/PackOptions.cs`
- Create: `src/Game/Options/ForbiddenTechOptions.cs`
- Modify: `src/Game/ModEntryPoint.cs`
- Create: `tests/PackOptionsTests.cs`
- Modify: `tests/CoreTestProgram.cs`

**Interfaces:**
- Produces: `ResolvedOptions PackOptions.Resolve(RawOptions raw)`.
- Produces: `ForbiddenTechOptions.Current` refreshed from `POptions.ReadSettings<ForbiddenTechOptions>()`.
- Consumes: stable IDs from `ModIdentity`.

- [ ] **Step 1: Write failing preset and clamp tests**

```csharp
public static void Run() {
    var strong = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Strong });
    AssertEx.Near(0.90f, strong.RecoveryRate, 0.0001f, "strong recovery");
    AssertEx.Near(1.00f, strong.CostMultiplier, 0.0001f, "strong cost");
    AssertEx.True(strong.AllowEndgame, "strong endgame");
    AssertEx.True(strong.ConsumeSamples, "strong sample use");

    var extreme = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Extreme });
    AssertEx.Near(0.25f, extreme.CostMultiplier, 0.0001f, "extreme cost");
    AssertEx.False(extreme.ConsumeSamples, "extreme sample use");

    var custom = PackOptions.Resolve(new RawOptions {
        Preset = BalancePreset.Custom, RecoveryRate = 4f,
        CostMultiplier = -1f, PowerMultiplier = 0f, HeatMultiplier = 99f
    });
    AssertEx.Near(1f, custom.RecoveryRate, 0.0001f, "recovery clamp");
    AssertEx.Near(0.05f, custom.CostMultiplier, 0.0001f, "cost clamp");
    AssertEx.Near(0.10f, custom.PowerMultiplier, 0.0001f, "power clamp");
    AssertEx.Near(10f, custom.HeatMultiplier, 0.0001f, "heat clamp");
}
```

- [ ] **Step 2: Run the targeted test and verify failure**

Run: `./test.ps1 -Suite PackOptionsTests`

Expected: FAIL because `PackOptions` and related types do not exist.

- [ ] **Step 3: Implement pure preset resolution**

Define `BalancePreset { Balanced, Strong, Extreme, Custom }`, mutable `RawOptions`, and immutable `ResolvedOptions`. Use these exact preset values:

```csharp
case BalancePreset.Balanced: return new ResolvedOptions(0.75f, 1.50f, 1.25f, 1.25f, false, true);
case BalancePreset.Extreme:  return new ResolvedOptions(1.00f, 0.25f, 0.50f, 0.50f, true, false);
case BalancePreset.Strong:   return new ResolvedOptions(0.90f, 1.00f, 1.00f, 1.00f, true, true);
```

For Custom, clamp recovery to `0.05–1.00`, cost to `0.05–20.00`, power to `0.10–10.00`, and heat to `0.00–10.00`. Include module and per-building booleans plus Industrial, Rare, and Endgame material switches.

Module and per-building booleans are copied from `RawOptions` for every preset; choosing a balance preset changes numerical/material defaults but never silently re-enables a building the player disabled.

- [ ] **Step 4: Add the PLib options adapter**

Decorate persisted properties with `[JsonProperty]`, `[Option]`, and `[Limit]`. `ForbiddenTechOptions.Load()` reads settings, converts them to `RawOptions`, resolves them, and stores the result in `Current`. Register the page in `OnLoad`:

```csharp
POptions.RegisterOptions(typeof(ForbiddenTechOptions));
PLocalization.Register();
ForbiddenTechOptions.Load();
```

The Safe Removal option is not a persistent boolean checkbox; register a dedicated action button in Task 10 so an accidental options edit cannot begin destructive conversion.

- [ ] **Step 5: Run tests and compile the game assembly**

Run:

```powershell
./test.ps1 -Suite PackOptionsTests
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
```

Expected: preset suite passes and the PLib attributes compile against the pinned dependency.

- [ ] **Step 6: Commit**

```powershell
git add src/Core/PackOptions.cs src/Game/Options tests
git commit -m "feat: add configurable balance presets"
```

---

### Task 3: Material Classification and Conversion Safety

**Files:**
- Create: `src/Core/MaterialModel.cs`
- Create: `src/Core/MaterialClassifier.cs`
- Create: `src/Core/ConversionMath.cs`
- Create: `tests/MaterialClassifierTests.cs`
- Create: `tests/ConversionMathTests.cs`
- Modify: `tests/CoreTestProgram.cs`

**Interfaces:**
- Produces: `bool MaterialClassifier.TryCreateRule(ElementDescriptor element, ResolvedOptions options, out MaterialRule rule)`.
- Produces: `float ConversionMath.CrusherOutputKg(float inputKg, ResolvedOptions options)`.
- Produces: `float ConversionMath.CompilerInputKg(float outputKg, MaterialRule rule, ResolvedOptions options)`.
- Produces: `bool ConversionMath.IsRoundTripSafe(MaterialRule rule, ResolvedOptions options)`.

- [ ] **Step 1: Write failing classification tests**

```csharp
var dirt = ElementDescriptor.Solid("Dirt", "Agricultural", "BuildableRaw");
AssertEx.True(MaterialClassifier.TryCreateRule(dirt, strong, out var dirtRule), "dirt allowed");
AssertEx.Equal(MaterialTier.Common, dirtRule.Tier, "dirt tier");
AssertEx.Near(1.25f, dirtRule.ProtoMatterPerKg, 0.0001f, "dirt cost");

var copperOre = ElementDescriptor.Solid("Cuprite", "Metal", "MetalOre");
AssertEx.Equal(MaterialTier.OreOrOrganic,
    MaterialClassifier.CreateRule(copperOre, strong).Tier, "ore tier");

AssertEx.False(MaterialClassifier.TryCreateRule(
    ElementDescriptor.SpecialSolid("Unobtanium"), strong, out _), "neutronium denied");
AssertEx.False(MaterialClassifier.TryCreateRule(
    ElementDescriptor.NonSolid("Water"), strong, out _), "liquid denied");
```

Also cover food, seed, egg, artifact, quest, creature, disabled element, inactive DLC element, industrial, rare, and endgame switches.

- [ ] **Step 2: Write failing round-trip property tests**

```csharp
foreach (MaterialTier tier in Enum.GetValues(typeof(MaterialTier))) {
    var rule = MaterialRule.ForTier("test-" + tier, tier);
    AssertEx.True(ConversionMath.IsRoundTripSafe(rule, strong), "safe " + tier);
    float proto = ConversionMath.CrusherOutputKg(100f, strong);
    float rebuilt = proto / (rule.ProtoMatterPerKg * strong.CostMultiplier);
    AssertEx.True(rebuilt < 100f, "strict loss " + tier);
}
```

- [ ] **Step 3: Run the suites and verify failure**

Run: `./test.ps1 -Suite MaterialClassifierTests,ConversionMathTests`

Expected: FAIL because the material model is absent.

- [ ] **Step 4: Implement descriptors, deny rules, tiers, and costs**

Use an explicit deny ID set containing `Unobtanium`, plus deny tags `Food`, `Seed`, `Egg`, `Creature`, `Artifact`, `QuestItem`, and `Noncrushable`. Require `State == "Solid"`, `IsStorable`, `IsActive`, and `!IsSpecial`.

Tier costs are exactly `1.25`, `1.5`, `3`, `6`, and `12` kg Proto-Matter per kg output. Apply the most restrictive matching tag in this order: Endgame, Rare, Industrial, OreOrOrganic, Common. Unknown safe solids are rejected rather than classified as Common.

- [ ] **Step 5: Implement startup catalog validation**

`ConversionMath.ValidateAll(IEnumerable<MaterialRule>, ResolvedOptions)` returns rule IDs whose compile/crush round trip is not strictly lossy. The game adapter must omit and log these rules; it must not reduce costs silently.

- [ ] **Step 6: Run tests**

Run: `./test.ps1`

Expected: all identity, options, classifier, and conversion tests pass.

- [ ] **Step 7: Commit**

```powershell
git add src/Core/MaterialModel.cs src/Core/MaterialClassifier.cs src/Core/ConversionMath.cs tests
git commit -m "feat: define safe material conversion rules"
```

---

### Task 4: Proto-Matter Element and Cached ONI Element Catalog

**Files:**
- Create: `packaging/elements/BaiyeForbiddenProtoMatter.yaml`
- Create: `src/Game/Elements/ProtoMatterRegistration.cs`
- Create: `src/Game/Elements/ElementCatalogAdapter.cs`
- Create: `src/Game/Localization/STRINGS.cs`
- Create: `tests/ElementYamlTests.ps1`
- Modify: `test.ps1`

**Interfaces:**
- Produces: `ProtoMatterRegistration.Hash`, `ProtoMatterRegistration.Tag`, and `RegisterSubstance(...)`.
- Produces: `IReadOnlyDictionary<string, MaterialRule> ElementCatalogAdapter.Rules` built once after elements load.
- Consumes: `MaterialClassifier`, `ConversionMath`, and `ForbiddenTechOptions.Current`.

- [ ] **Step 1: Write a failing YAML validation test**

```powershell
$yaml = Get-Content -Raw -LiteralPath 'packaging\elements\BaiyeForbiddenProtoMatter.yaml'
$required = @('elementId: BaiyeForbiddenProtoMatter','state: Solid','isDisabled: false','dlcId: ""')
foreach ($token in $required) {
    if (-not $yaml.Contains($token)) { throw "Missing element token: $token" }
}
```

The test also rejects `AnyBuildable`, `Metal`, `RawMineral`, `Food`, and any reachable solid-to-liquid transition target.

- [ ] **Step 2: Run the YAML suite and verify failure**

Run: `./test.ps1 -Suite ElementYamlTests`

Expected: FAIL because the element definition does not exist.

- [ ] **Step 3: Add the physical element definition**

Use these fixed values:

```yaml
elements:
  - elementId: BaiyeForbiddenProtoMatter
    maxMass: 20000
    specificHeatCapacity: 1.0
    thermalConductivity: 0.1
    solidSurfaceAreaMultiplier: 1
    liquidSurfaceAreaMultiplier: 1
    gasSurfaceAreaMultiplier: 1
    lowTemp: 1
    highTemp: 9999
    lowTempTransitionTarget: SolidCarbonDioxide
    highTempTransitionTarget: MoltenTungsten
    defaultTemperature: 293.15
    defaultMass: 100
    molarMass: 100
    toxicity: 0
    lightAbsorptionFactor: 0.5
    radiationAbsorptionFactor: 0.5
    radiationPer1000Mass: 0
    tags:
      - BaiyeProtoMatter
    isDisabled: false
    state: Solid
    localizationID: STRINGS.ELEMENTS.BAIYEFORBIDDENPROTOMATTER.NAME
    description: STRINGS.ELEMENTS.BAIYEFORBIDDENPROTOMATTER.DESC
    dlcId: ""
```

- [ ] **Step 4: Register the Proto-Matter substance before `ElementLoader.Load`**

Patch `ElementLoader.Load(ref Hashtable substanceList, Dictionary<string, SubstanceTable> substanceTablesByDlc)` with a prefix. Define `Hash` as `(SimHashes)global::Hash.SDBMLower(ModIdentity.ProtoMatterId)`, create a solid substance via `ModUtil.CreateSubstance`, and add it once to the vanilla substance table. Until Task 11 replaces the visual, use the base game's tungsten solid KAnim/material and tint it purple; do not patch `Enum.GetValues`, `Enum.Parse`, or `Enum.ToString` globally.

- [ ] **Step 5: Build the cached game catalog**

After `ElementLoader.Load`, iterate `ElementLoader.elements` once. Map `Element.id`, `Element.IsSolid`, `Element.disabled`, `Element.dlcId`, `Element.materialCategory`, and `Element.oreTags` to `ElementDescriptor`. Mark only elements whose DLC is active through `DlcManager.IsContentActive(element.dlcId)`; treat an empty DLC ID as active.

Store accepted rules in a dictionary keyed by `element.id.ToString()`. Exclude Proto-Matter itself. Run `ConversionMath.ValidateAll` and remove unsafe rules with one log entry per rejected ID.

- [ ] **Step 6: Add English and Simplified Chinese element strings**

Register names and descriptions through `PLocalization.Register()` and include `translations/zh.po` in packaging. The Chinese name is `原质`; the English name is `Proto-Matter`.

- [ ] **Step 7: Run tests and build**

Run:

```powershell
./test.ps1
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
```

Expected: YAML validation passes and the custom element registration compiles without global enum patches.

- [ ] **Step 8: Commit**

```powershell
git add packaging/elements src/Game/Elements src/Game/Localization tests test.ps1
git commit -m "feat: register transportable proto matter"
```

---

### Task 5: Colony Unlock Persistence and Dynamic Recipe Registry

**Files:**
- Create: `src/Core/UnlockState.cs`
- Create: `src/Core/RecipePlan.cs`
- Create: `src/Game/Save/ForbiddenTechSaveData.cs`
- Create: `src/Game/Recipes/RecipeRegistry.cs`
- Create: `tests/UnlockStateTests.cs`
- Create: `tests/RecipeRegistryTests.cs`
- Modify: `tests/CoreTestProgram.cs`

**Interfaces:**
- Produces: `bool UnlockState.Unlock(string elementId)`, `bool IsUnlocked(string elementId)`, and `IReadOnlyList<string> ActiveUnlocked(ISet<string> activeRuleIds)`.
- Produces: `ForbiddenTechSaveData.Instance`, `Unlock(Tag)`, `IsUnlocked(Tag)`, and `event Action UnlocksChanged`.
- Produces: `RecipePlanFactory.Create(MaterialRule, ResolvedOptions)` for pure amount/timing decisions.
- Produces: `RecipeRegistry.AnalyzerRecipes`, `CrusherRecipes`, and `CompilerRecipes` keyed by element ID by translating each `RecipePlan` to `ComplexRecipe`.

- [ ] **Step 1: Write failing migration and inactive-DLC tests**

```csharp
var state = UnlockState.FromSerialized(0, new[] { "Iron", "Iron", "Niobium" });
AssertEx.Equal(1, state.Version, "migrated version");
AssertEx.Equal(2, state.ElementIds.Count, "deduplicated ids");
AssertEx.True(state.Unlock("Diamond"), "new unlock changed state");
AssertEx.False(state.Unlock("Diamond"), "repeat unlock unchanged");

var active = state.ActiveUnlocked(new HashSet<string> { "Iron", "Diamond" });
AssertEx.SequenceEqual(new[] { "Diamond", "Iron" }, active, "inactive DLC id retained but hidden");
AssertEx.True(state.IsUnlocked("Niobium"), "inactive id retained");

var common = RecipePlanFactory.Create(MaterialRule.ForTier("Dirt", MaterialTier.Common), strong);
AssertEx.Near(10f, common.AnalyzerInputKg, 0.0001f, "sample mass");
AssertEx.Near(90f, common.CrusherOutputKg, 0.0001f, "crusher output");
AssertEx.Near(125f, common.CompilerInputKg, 0.0001f, "common compiler input");
AssertEx.Near(100f, common.CompilerOutputKg, 0.0001f, "common batch");

var endgame = RecipePlanFactory.Create(MaterialRule.ForTier("Isoresin", MaterialTier.Endgame), strong);
AssertEx.Near(120f, endgame.CompilerInputKg, 0.0001f, "endgame compiler input");
AssertEx.Near(10f, endgame.CompilerOutputKg, 0.0001f, "endgame batch");
```

- [ ] **Step 2: Run the targeted tests and verify failure**

Run: `./test.ps1 -Suite UnlockStateTests,RecipeRegistryTests`

Expected: FAIL because unlock persistence and recipe descriptions are absent.

- [ ] **Step 3: Implement versioned pure unlock state**

Store IDs using `HashSet<string>(StringComparer.Ordinal)`. Serialization output is sorted ordinally for deterministic saves. Version `0` migrates by removing blank and duplicate IDs; current version is `1`. Never delete an ID solely because its DLC is inactive.

- [ ] **Step 4: Attach serialized save data to `Game`**

`ForbiddenTechSaveData` is a `[SerializationConfig(MemberSerialization.OptIn)]` `KMonoBehaviour` with `[Serialize] int dataVersion` and `[Serialize] List<string> unlockedElementIds`. Patch `Game.OnPrefabInit` to `AddOrGet<ForbiddenTechSaveData>()`. Rebuild the pure `UnlockState` in `OnSpawn` and persist the sorted list immediately after a new unlock.

- [ ] **Step 5: Generate deterministic recipes**

For every catalog rule, `RecipePlanFactory` creates these IDs and numeric fields without referencing game assemblies:

```csharp
string AnalyzerId(string id) => "BaiyeMatterAnalyze_" + id;
string CrusherId(string id)  => "BaiyeMatterCrush_" + id;
string CompilerId(string id) => "BaiyeMatterCompile_" + id;
```

- Analyzer: 10 kg input when `ConsumeSamples`, otherwise mark the input `doNotConsume = true`; no physical output; 30 seconds.
- Crusher: 100 kg input and `100 * RecoveryRate` kg Proto-Matter output; 40 seconds.
- Compiler: 100 kg output for Common/OreOrOrganic/Industrial, 10 kg for Rare/Endgame; Proto-Matter input is `outputKg * rule.ProtoMatterPerKg * CostMultiplier`; base time is `30 seconds * rule.ProtoMatterPerKg`.

`RecipeRegistry` translates the returned plans with `ComplexRecipeManager.MakeRecipeID` plus `ComplexRecipe` and tags each recipe with exactly one fabricator building ID. Recipe dictionaries must be sorted by localized element name before assigning `sortOrder`.

- [ ] **Step 6: Test and build**

Run:

```powershell
./test.ps1
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
```

Expected: unlock migration, inactive DLC retention, recipe amounts, and round-trip assertions pass.

- [ ] **Step 7: Commit**

```powershell
git add src/Core/UnlockState.cs src/Game/Save src/Game/Recipes tests
git commit -m "feat: persist discoveries and generate conversion recipes"
```

---

### Task 6: Matter Analyzer Building

**Files:**
- Create: `src/Game/Buildings/Analyzer/MatterAnalyzerConfig.cs`
- Create: `src/Game/Buildings/Analyzer/MatterAnalyzer.cs`
- Create: `src/Game/Buildings/Common/FabricatorSupport.cs`
- Create: `src/Core/AnalyzerPolicy.cs`
- Create: `tests/AnalyzerPolicyTests.cs`

**Interfaces:**
- Consumes: `RecipeRegistry.AnalyzerRecipes`, `ForbiddenTechSaveData.Unlock(Tag)`, options, and stable analyzer ID.
- Produces: a 3×3, 1.2 kW, 4 kDTU/s `ComplexFabricator` whose available recipes are exactly active, locked material samples.

- [ ] **Step 1: Write failing analyzer policy tests**

```csharp
var rules = new[] { Rule("Iron"), Rule("Diamond"), Rule("Niobium") };
var unlocked = UnlockState.FromSerialized(1, new[] { "Iron" });
var visible = AnalyzerPolicy.VisibleRuleIds(rules, unlocked,
    new HashSet<string> { "Iron", "Diamond" });
AssertEx.SequenceEqual(new[] { "Diamond" }, visible, "only active locked samples");
AssertEx.True(AnalyzerPolicy.ShouldConsumeSample(strong), "strong consumes");
AssertEx.False(AnalyzerPolicy.ShouldConsumeSample(extreme), "extreme preserves");
```

- [ ] **Step 2: Run the test and verify failure**

Run: `./test.ps1 -Suite AnalyzerPolicyTests`

Expected: FAIL because `AnalyzerPolicy` is absent.

- [ ] **Step 3: Implement analyzer building definition**

Use `BuildingTemplates.CreateBuildingDef` with width 3, height 3, animation `baiye_matter_analyzer_kanim`, 1.2 kW active consumption, 4 kDTU/s self heat, floor placement, automation input, and refined metal plus glass construction ingredients. Configure `ComplexFabricator`, three storages, `FabricatorIngredientStatusManager`, `CopyBuildingSettings`, `Prioritizable`, and a manual-work chore.

- [ ] **Step 4: Filter recipe availability and unlock on completion**

`MatterAnalyzer : ComplexFabricator` refreshes `recipe_list` from `AnalyzerPolicy.VisibleRuleIds` on spawn and whenever `UnlocksChanged` fires. Patch `ComplexFabricator.CompleteWorkingOrder()` with a prefix/postfix pair: the prefix captures `Get_CurrentWorkingOrder` into `__state` only when `__instance is MatterAnalyzer`; the postfix reads the first ingredient tag from that captured recipe, calls `ForbiddenTechSaveData.Unlock`, then refreshes the list. A repeated recipe completion is idempotent, and an empty recipe result list does not suppress the unlock.

- [ ] **Step 5: Add analyzer strings and temporary animation mapping**

Add Chinese and English building name, description, effect, status text, and automation-port strings. For the playable logic prototype, use the existing `supermaterial_refinery_kanim`; Task 11 replaces this exact config field with `baiye_matter_analyzer_kanim` after the original asset is compiled.

- [ ] **Step 6: Run tests and compile**

Run:

```powershell
./test.ps1 -Suite AnalyzerPolicyTests
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
```

Expected: policy tests pass; game assembly exposes `BaiyeMatterAnalyzerConfig` without missing references.

- [ ] **Step 7: Commit**

```powershell
git add src/Core/AnalyzerPolicy.cs src/Game/Buildings/Analyzer src/Game/Buildings/Common src/Game/Localization tests
git commit -m "feat: add matter analyzer building"
```

---

### Task 7: Mass Crusher Building and Rail Logistics

**Files:**
- Create: `src/Game/Buildings/Crusher/MassCrusherConfig.cs`
- Create: `src/Game/Buildings/Crusher/MassCrusher.cs`
- Create: `src/Core/CrusherPolicy.cs`
- Create: `tests/CrusherPolicyTests.cs`
- Modify: `src/Game/Buildings/Common/FabricatorSupport.cs`

**Interfaces:**
- Consumes: `RecipeRegistry.CrusherRecipes`, Proto-Matter tag, and resolved options.
- Produces: a 4×4, 2.4 kW, 40 kDTU/s fabricator with manual/rail input and manual/rail output.

- [ ] **Step 1: Write failing crusher policy tests**

```csharp
AssertEx.Near(90f, CrusherPolicy.OutputKg(100f, strong), 0.0001f, "strong batch");
AssertEx.Near(75f, CrusherPolicy.OutputKg(100f, balanced), 0.0001f, "balanced batch");
AssertEx.False(CrusherPolicy.CanCrush(ModIdentity.ProtoMatterId), "proto matter denied");
AssertEx.True(CrusherPolicy.CanStart(100f, 90f, 100f), "space available");
AssertEx.False(CrusherPolicy.CanStart(100f, 90f, 89.99f), "output blocked");
```

- [ ] **Step 2: Run the test and verify failure**

Run: `./test.ps1 -Suite CrusherPolicyTests`

Expected: FAIL because crusher policy is absent.

- [ ] **Step 3: Implement crusher definition and storages**

Create a floor building using the temporary base-game `rockrefinery_kanim`, 4×4 size, 2.4 kW, 40 kDTU/s, automation input, refined metal/ceramic construction ingredients, and `ComplexFabricatorSideScreen.StyleSetting.ListQueueHybrid`. Task 11 replaces only the animation field with `baiye_mass_crusher_kanim`.

Use separate `inStorage`, `buildStorage`, and `outStorage`; set every storage to save contents. `inStorage` accepts catalog material tags but never Proto-Matter. `outStorage` accepts only Proto-Matter and has enough capacity for one completed batch plus one rail packet.

- [ ] **Step 4: Add solid rail ports**

Attach `SolidConduitConsumer` to input storage with `capacityKG = 1000f`, `alwaysConsume = true`, and a secondary-input-free conveyor offset. Attach `SolidConduitDispenser` to output storage with `alwaysDispense = true`, `solidOnly = true`, and `elementFilter` containing only Proto-Matter. Add matching `BuildingDef` utility offsets.

- [ ] **Step 5: Enforce capacity-before-consumption behavior**

Before starting a work order, calculate expected Proto-Matter output and require free output capacity. If blocked, clear the operational requirement named `OutputSpace`; preserve the queued recipe and input. When space becomes available, restore the flag and refresh the queue. Deconstruction uses `Storage.DropAll` for all three storages.

- [ ] **Step 6: Run tests and compile**

Run:

```powershell
./test.ps1 -Suite CrusherPolicyTests,ConversionMathTests
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
```

Expected: policy and conversion tests pass; conveyor components compile against current game assemblies.

- [ ] **Step 7: Commit**

```powershell
git add src/Core/CrusherPolicy.cs src/Game/Buildings/Crusher src/Game/Buildings/Common src/Game/Localization tests
git commit -m "feat: add rail-enabled mass crusher"
```

---

### Task 8: Matter Compiler, Unlock Filtering, and Safe Cooling Loop

**Files:**
- Create: `src/Core/CoolingMath.cs`
- Create: `src/Core/CompilerRecipeFilter.cs`
- Create: `src/Game/Buildings/Compiler/MatterCompilerConfig.cs`
- Create: `src/Game/Buildings/Compiler/MatterCompiler.cs`
- Create: `src/Game/Buildings/Compiler/CoolantController.cs`
- Create: `tests/CoolingMathTests.cs`
- Create: `tests/CompilerPolicyTests.cs`

**Interfaces:**
- Consumes: compiler recipes, current unlock state, Proto-Matter input, power/heat multipliers, and ONI conduit APIs.
- Produces: a 5×5, 9.6 kW fabricator with Proto-Matter rail/manual input, product rail/manual output, liquid coolant input/output, and unlock-filtered recipes.
- Produces: `CoolingDecision CoolingMath.Evaluate(CoolantPacket packet, float heatJoules)`.

- [ ] **Step 1: Write failing cooling tests**

```csharp
var water = new CoolantPacket(10f, 293.15f, 373.15f, 4.179f);
var safe = CoolingMath.Evaluate(water, 160000f);
AssertEx.True(safe.CanAcceptHeat, "water accepts heat");
AssertEx.Near(296.98f, safe.OutputKelvin, 0.02f, "water output temperature");

var nearBoiling = new CoolantPacket(10f, 372.5f, 373.15f, 4.179f);
AssertEx.False(CoolingMath.Evaluate(nearBoiling, 160000f).CanAcceptHeat,
    "packet may not cross transition");
AssertEx.Throws<ArgumentOutOfRangeException>(() =>
    CoolingMath.Evaluate(new CoolantPacket(0f, 293f, 373f, 4f), 1f));
```

- [ ] **Step 2: Write failing unlock-filter tests**

```csharp
var visible = CompilerRecipeFilter.SelectIds(
    new[] { Rule("Iron"), Rule("Diamond"), Rule("Niobium") },
    UnlockState.FromSerialized(1, new[] { "Iron", "Niobium" }),
    new HashSet<string> { "Iron", "Diamond" });
AssertEx.SequenceEqual(new[] { "Iron" }, visible, "unlocked active recipes only");
```

- [ ] **Step 3: Run the tests and verify failure**

Run: `./test.ps1 -Suite CoolingMathTests,CompilerPolicyTests`

Expected: FAIL because cooling and filtering logic are absent.

- [ ] **Step 4: Implement pure cooling math**

Use `deltaK = heatJoules / (massKg * specificHeatKJPerKgK * 1000)`. Reject zero/negative mass, nonpositive heat capacity, nonfinite values, or output temperatures greater than or equal to `highTransitionKelvin - 0.5`. Return the unchanged packet when heat is zero.

- [ ] **Step 5: Implement compiler definition and logistics**

Create a 5×5 floor building using the temporary base-game `supermaterial_refinery_kanim`, 9.6 kW active power multiplied by options, 160 kDTU/s heat load multiplied by options, automation input, heavy construction costs, Proto-Matter solid input, solid output, and primary liquid input/output offsets. Task 11 replaces only the animation field with `baiye_matter_compiler_kanim`.

Use separate input, build, output, and coolant storages. Solid consumer accepts only Proto-Matter. Solid dispenser emits only products from the current compiler recipe set. `ConduitConsumer` receives up to 10 kg/s of any liquid; `ConduitDispenser` returns the same liquid through the output.

- [ ] **Step 6: Filter recipes and preserve the active batch**

`MatterCompiler : ComplexFabricator` replaces `recipe_list` only when no batch is active. It selects active unlocked rules and listens to `UnlocksChanged`. When a recipe is removed by options or DLC state during a batch, finish that batch, then refresh; never cancel it or discard ingredients.

- [ ] **Step 7: Add coolant gating**

For each simulation second, request the configured heat load from `CoolantController`. Inspect the first coolant `PrimaryElement`, call `CoolingMath.Evaluate`, and update its temperature only when safe. If no safe packet exists, clear the operational requirement `SafeCoolant`, pause work, and show a localized status item. Restore the flag when a safe packet arrives. Never change the coolant element ID or mass.

- [ ] **Step 8: Protect output and deconstruction paths**

Require capacity for the entire result batch before work starts. Keep completed items in output storage when the conveyor is blocked. On deconstruction, drop all four storages at the building cell with original element, mass, temperature, and disease data.

- [ ] **Step 9: Run tests and compile**

Run:

```powershell
./test.ps1 -Suite CoolingMathTests,CompilerPolicyTests,ConversionMathTests
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
```

Expected: cooling boundaries, unlock filtering, and mass safety tests pass; all conduit and fabricator code compiles.

- [ ] **Step 10: Commit**

```powershell
git add src/Core/CoolingMath.cs src/Core/CompilerRecipeFilter.cs src/Game/Buildings/Compiler src/Game/Localization tests
git commit -m "feat: add cooled matter compiler"
```

---

### Task 9: Research Node, Build Menu, and Per-Building Enable Switches

**Files:**
- Create: `src/Game/Registration/BuildingRegistration.cs`
- Create: `src/Game/Registration/ForbiddenResearchRegistration.cs`
- Create: `src/Core/RegistrationPolicy.cs`
- Create: `tests/RegistrationPolicyTests.cs`
- Modify: `src/Game/Localization/STRINGS.cs`

**Interfaces:**
- Consumes: resolved module/building booleans and three building IDs.
- Produces: custom tech `BaiyeForbiddenMatterEngineering` and build-menu entries in Refinement.
- Produces: `RegistrationPlan RegistrationPolicy.Create(ResolvedOptions options)`.

- [ ] **Step 1: Write failing registration tests**

```csharp
var all = RegistrationPolicy.Create(strong);
AssertEx.SequenceEqual(new[] {
    ModIdentity.MatterAnalyzerId,
    ModIdentity.MassCrusherId,
    ModIdentity.MatterCompilerId
}, all.BuildingIds, "all buildings enabled");

var disabledOptions = PackOptions.Resolve(new RawOptions {
    Preset = BalancePreset.Strong,
    ModuleEnabled = true,
    AnalyzerEnabled = true,
    CrusherEnabled = true,
    CompilerEnabled = false
});
var disabled = RegistrationPolicy.Create(disabledOptions);
AssertEx.False(disabled.BuildingIds.Contains(ModIdentity.MatterCompilerId), "compiler hidden");
AssertEx.True(disabled.BuildingIds.Contains(ModIdentity.MassCrusherId), "crusher remains");
```

- [ ] **Step 2: Run the test and verify failure**

Run: `./test.ps1 -Suite RegistrationPolicyTests`

Expected: FAIL because registration policy is absent.

- [ ] **Step 3: Register enabled building configs and menu entries**

Patch `GeneratedBuildings.LoadGeneratedBuildings`. For each enabled ID, call `ModUtil.AddBuildingToPlanScreen("Refining", id)` exactly once. Config-disabled buildings still keep their classes and prefabs loadable for existing saves; set `BuildingDef.ShowInBuildMenu = false` instead of preventing prefab creation.

- [ ] **Step 4: Create the terminal research node**

After `Database.Techs.Init`, create:

```csharp
var costs = new Dictionary<string, float> {
    { "basic", 120f }, { "advanced", 80f }
};
var tech = new Tech(ModIdentity.ResearchId, enabledBuildingIds, Db.Get().Techs, costs);
Db.Get().Techs.AddPrerequisite(tech, "MatterDeconstruction");
```

The `Tech` base constructor registers itself with the supplied `Database.Techs`; do not add it to the resource set a second time. Use only base and advanced research costs, so DLC-specific research is never mandatory. Add localized node name, description, and search terms. If `MatterDeconstruction` is absent in a future build, fall back to `HighTempForging` and log one warning.

- [ ] **Step 5: Keep existing instances functional**

Enable switches affect `ShowInBuildMenu`, menu insertion, tech unlocked IDs, and recipe creation for new orders. They never destroy prefabs or placed buildings. Existing buildings with a now-disabled module retain their components and stored material; the options page states that a restart is required.

- [ ] **Step 6: Run tests and compile**

Run:

```powershell
./test.ps1 -Suite RegistrationPolicyTests
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
```

Expected: registration policy passes and custom tech construction matches the current game assembly signatures.

- [ ] **Step 7: Commit**

```powershell
git add src/Core/RegistrationPolicy.cs src/Game/Registration src/Game/Localization tests
git commit -m "feat: register configurable buildings and research"
```

---

### Task 10: Safe Removal Workflow

**Files:**
- Create: `src/Core/SafeRemovalReport.cs`
- Create: `src/Game/Safety/SafeRemovalController.cs`
- Create: `src/Game/Safety/SafeRemovalDialog.cs`
- Create: `tests/SafeRemovalTests.cs`
- Modify: `src/Game/Options/ForbiddenTechOptions.cs`
- Modify: `src/Game/Localization/STRINGS.cs`

**Interfaces:**
- Produces: `SafeRemovalReport` with converted object count, converted mass, returned input count, removed building count, and remaining custom-object count.
- Produces: explicit two-confirmation UI action; no automatic cleanup during ordinary configuration changes.

- [ ] **Step 1: Write failing report and phase tests**

```csharp
var report = new SafeRemovalReport();
report.RecordConversion(25f);
report.RecordConversion(75f);
report.RecordReturnedInput(3);
report.RecordRemovedBuilding();
AssertEx.Equal(2, report.ConvertedObjects, "converted objects");
AssertEx.Near(100f, report.ConvertedMassKg, 0.001f, "converted mass");
AssertEx.Equal(3, report.ReturnedInputs, "returned inputs");
AssertEx.Equal(1, report.RemovedBuildings, "removed buildings");
AssertEx.False(report.IsComplete, "not complete before scan");
report.Finish(0);
AssertEx.True(report.IsComplete, "complete with zero remaining");
```

- [ ] **Step 2: Run the test and verify failure**

Run: `./test.ps1 -Suite SafeRemovalTests`

Expected: FAIL because the removal report is absent.

- [ ] **Step 3: Add the explicit confirmation flow**

The options screen exposes a button labeled `准备安全移除此 Mod`. First click opens a warning explaining that cleanup changes the current colony and requires a new save. The confirmation dialog requires a second click labeled `转换原质并停止建筑`; Cancel is the default focused action.

- [ ] **Step 4: Stop production and return inputs**

Set a serialized `safeRemovalStarted` flag on `ForbiddenTechSaveData`. All three building components observe it, reject new orders, cancel fetch chores, preserve completed outputs, and drop unprocessed non-Proto-Matter inputs. Analyzer samples and compiler target materials return unchanged. After every storage is emptied, call each instance's `Deconstructable.ForceDestroyAndGetMaterials()` and record the removed building, ensuring a save prepared for uninstall contains no custom building prefab.

- [ ] **Step 5: Convert Proto-Matter in place**

Perform one bounded one-shot scan using `UnityEngine.Object.FindObjectsOfType<PrimaryElement>()`. For each object whose `ElementID` equals Proto-Matter, record mass and call `SetElement(SimHashes.IgneousRock, true)` without changing mass, temperature, or disease fields. This covers loose piles, storages, rails, and building storage objects because each owns a `PrimaryElement`.

After conversion, scan again for Proto-Matter and the three custom building components. If any custom object remains, keep safe-removal mode active, show the count, and allow retry. Only a zero count marks completion and hides research/build entries.

- [ ] **Step 6: Add post-cleanup instructions**

The success dialog reports converted object count and mass, asks the player to save under a new name, reload that save, verify it, then disable the Mod. Do not claim that direct uninstall is safe before this sequence.

- [ ] **Step 7: Run tests and compile**

Run:

```powershell
./test.ps1 -Suite SafeRemovalTests
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
```

Expected: report tests pass and the controller compiles with `PrimaryElement.SetElement(SimHashes, bool)`.

- [ ] **Step 8: Commit**

```powershell
git add src/Core/SafeRemovalReport.cs src/Game/Safety src/Game/Options src/Game/Localization tests
git commit -m "feat: add safe removal workflow"
```

---

### Task 11: Original KAnim Assets and Complete Localization

**Files:**
- Create: `assets/scml/proto_matter/*`
- Create: `assets/scml/matter_analyzer/*`
- Create: `assets/scml/mass_crusher/*`
- Create: `assets/scml/matter_compiler/*`
- Create: `assets/animation-manifest.json`
- Create: `build-assets.ps1`
- Create: `packaging/translations/zh.po`
- Create: `packaging/translations/en.po`
- Modify: `build.ps1`
- Modify: `verify-package.ps1`
- Modify: `src/Game/Buildings/Analyzer/MatterAnalyzerConfig.cs`
- Modify: `src/Game/Buildings/Crusher/MassCrusherConfig.cs`
- Modify: `src/Game/Buildings/Compiler/MatterCompilerConfig.cs`

**Interfaces:**
- Produces: `baiye_proto_matter_kanim`, `baiye_matter_analyzer_kanim`, `baiye_mass_crusher_kanim`, and `baiye_matter_compiler_kanim`.
- Consumes: kanimal-SE 1.3.31 installed by `restore-deps.ps1`.

- [ ] **Step 1: Define and validate the animation manifest**

```json
{
  "baiye_proto_matter": ["idle"],
  "baiye_matter_analyzer": ["off", "idle", "working_loop", "working_pst", "overheat"],
  "baiye_mass_crusher": ["off", "idle", "working_loop", "working_pst", "blocked"],
  "baiye_matter_compiler": ["off", "idle", "working_loop", "working_pst", "no_coolant", "blocked"]
}
```

Extend `verify-package.ps1` to require one PNG, one `_anim.bytes`, and one `_build.bytes` for each manifest key and reject any filename containing a base-game animation name.

- [ ] **Step 2: Create original visual sources**

Use a consistent orthographic industrial style: dark metal body, purple energy core, cyan scan light, and black/yellow hazard markings. Create layered transparent PNG sprites and SCML timelines with these frame rates:

- Proto-Matter idle: 6-frame glow loop at 6 fps.
- Analyzer working: scan ring moves bottom-to-top over 24 frames at 12 fps.
- Crusher working: intake closes over 8 frames, 12-frame inward collapse loop, 8-frame output pulse.
- Compiler working: three rings spin at distinct speeds over a 24-frame loop; completion uses a 10-frame white-purple flash.
- Error states pulse red at 4 fps without changing the building silhouette.

Every building canvas uses its approved footprint at 100 pixels per cell and keeps ports visible in front-facing layers.

- [ ] **Step 3: Compile SCML to KAnim**

`build-assets.ps1` runs one command per asset:

```powershell
& '.\tools\kanimal-cli.exe' kanim '.\assets\scml\matter_analyzer\baiye_matter_analyzer.scml' -o '.\packaging\anim'
```

Repeat with the crusher, compiler, and Proto-Matter SCML paths. Fail if kanimal exits nonzero or any manifest animation name is absent from the compiled output.

- [ ] **Step 4: Replace every temporary animation**

Update substance registration to use `baiye_proto_matter_kanim`. Replace analyzer `supermaterial_refinery_kanim`, crusher `rockrefinery_kanim`, and compiler `supermaterial_refinery_kanim` with the final stable animation IDs from the manifest. No base-game animation file is copied into the package.

- [ ] **Step 5: Complete English and Simplified Chinese strings**

Cover Mod options, presets, building names/descriptions/effects, research, recipe descriptions, automation ports, blocked output, unsafe coolant, sample already analyzed, safe-removal warnings, progress, success, and failure. Run a key comparison script that fails when either PO file lacks a registered `STRINGS` key.

- [ ] **Step 6: Build and inspect assets**

Run:

```powershell
./build-assets.ps1
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
./verify-package.ps1
```

Expected: all original KAnim triplets and translation keys are present; no temporary animation remains.

- [ ] **Step 7: Commit**

```powershell
git add assets packaging/anim packaging/translations build-assets.ps1 build.ps1 verify-package.ps1 src/Game/Elements
git commit -m "feat: add original forbidden technology art"
```

---

### Task 12: In-Game Compatibility, Persistence, Performance, and Release Package

**Files:**
- Create: `docs/test-matrix.md`
- Create: `README.md`
- Create: `CHANGELOG.md`
- Create: `pack-release.ps1`
- Modify: `verify-package.ps1`

**Interfaces:**
- Consumes: the complete mod package.
- Produces: a verified local install and `release/ForbiddenTechnologyPack-0.1.0.zip`.

- [ ] **Step 1: Run all automated gates from a clean generated-output state**

Run:

```powershell
./restore-deps.ps1
./test.ps1
./build-assets.ps1
./build.ps1 -GamePath 'D:\steam\steamapps\common\OxygenNotIncluded'
./verify-package.ps1
```

Expected: every core/YAML/package test passes, compiler warnings are reviewed, and the final package contains one merged Mod DLL plus data/assets only.

- [ ] **Step 2: Install the local package without touching other mods**

`install.ps1` copies only `dist\ForbiddenTechnologyPack` to `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\local\ForbiddenTechnologyPack`. It refuses to overwrite a path that resolves outside that exact local mod directory and never edits `mods.json`.

- [ ] **Step 3: Execute the content-mode matrix**

Record game build, active content IDs, outcome, and Player.log excerpt in `docs/test-matrix.md` for:

1. Base game only.
2. Each DLC enabled alone where the game permits that mode.
3. All supported DLC enabled together.

For each mode verify main-menu load, new colony load, existing colony load, research node, three build menu entries, Proto-Matter resource entry, save, exit, and reload. Any missing-DLC exception fails the matrix.

- [ ] **Step 4: Execute building behavior cases**

In a disposable colony:

1. Analyze a common solid and confirm its compiler recipe appears after completion.
2. Confirm duplicate analysis is absent.
3. Feed the crusher manually and by rail; block its output and verify no mass loss.
4. Compile one common, industrial, rare, and endgame solid.
5. Interrupt each building by power loss and automation disable, then resume.
6. Feed near-transition coolant and verify pause without phase change or deleted liquid.
7. Block compiler output, save/reload, unblock, and verify exactly one result batch.
8. Deconstruct each building with partial inputs and verify full returned mass.

- [ ] **Step 5: Execute configuration and persistence cases**

Test Balanced, Strong, Extreme, and clamped Custom settings. Disable each building individually, restart, confirm it is hidden for new construction, and confirm an existing instance and its storage remain intact. Toggle a DLC off and on; confirm DLC recipes hide and reappear while the serialized unlock ID remains.

- [ ] **Step 6: Execute safe-removal recovery**

Create loose, stored, rail, crusher, and compiler Proto-Matter. Run safe removal, require zero remaining items, save under a new name, reload, disable the Mod, and load again. Record the report counts and confirm the colony contains equal-mass igneous rock and no missing-element error.

- [ ] **Step 7: Run a performance soak**

Operate 10 crushers and 10 compilers for 20 cycles at speed 3. Confirm Player.log has no repeating exception/warning, no per-frame full element scan, and no unbounded growth in queued recipes or stored objects. Catalog construction must occur once per game load; safe-removal object scans may occur only after explicit confirmation.

- [ ] **Step 8: Write concise public documentation**

`README.md` contains only purpose, requirements, installation, three buildings, configuration presets, safe removal warning, compatibility, and update expectations. Do not include the local development path, build commands, private test logs, or implementation narrative.

`CHANGELOG.md` for `0.1.0` lists the three buildings, Proto-Matter, base/all-DLC compatibility, presets, logistics, coolant safety, and safe removal.

- [ ] **Step 9: Create and verify the release ZIP**

`pack-release.ps1` rebuilds, verifies, creates `release/ForbiddenTechnologyPack-0.1.0.zip`, prints its SHA256, then extracts it to a unique temporary directory and reruns package verification against the extracted folder.

Expected archive root:

```text
ForbiddenTechnologyPack/
  ForbiddenTechnologyPack.dll
  mod.yaml
  mod_info.yaml
  elements/
  anim/
  translations/
```

- [ ] **Step 10: Final verification commit**

```powershell
git add README.md CHANGELOG.md docs/test-matrix.md pack-release.ps1 verify-package.ps1
git commit -m "release: verify forbidden technology pack 0.1.0"
git status --short
```

Expected: clean working tree. Do not publish to GitHub or Steam until the user separately authorizes publication.
