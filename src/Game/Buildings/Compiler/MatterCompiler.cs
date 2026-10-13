using System;
using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Recipes;
using ForbiddenTechnologyPack.Game.Save;
using HarmonyLib;

namespace ForbiddenTechnologyPack.Game.Buildings.Compiler {
    public sealed class MatterCompiler : ComplexFabricator {
        private bool pendingRecipeRefresh;
        private ForbiddenTechSaveData subscribedSave;
        private readonly HashSet<SimHashes> dispensableProducts = new HashSet<SimHashes>();

        private static readonly Operational.Flag RecipeListStable =
            new Operational.Flag("CompilerRecipeListStable", Operational.Flag.Type.Requirement);
        private static readonly Operational.Flag RecipesAvailable =
            new Operational.Flag("CompilerRecipesAvailable", Operational.Flag.Type.Requirement);
        internal static readonly Operational.Flag OutputSpace =
            new Operational.Flag("CompilerOutputSpace", Operational.Flag.Type.Requirement);

        protected override void OnSpawn() {
            var hadSavedIngredients = buildStorage != null && buildStorage.MassStored() > 0f;
            base.OnSpawn();
            operational.SetFlag(RecipeListStable, true);
            SubscribeToUnlocks();
            ApplyRecipeFilter(hadSavedIngredients || OrderProgress > 0f);
            RefreshOutputSpace();
            Subscribe((int)GameHashes.DeconstructComplete, OnDeconstructComplete);
        }

        protected override void OnCleanUp() {
            Unsubscribe((int)GameHashes.DeconstructComplete, OnDeconstructComplete);
            UnsubscribeFromUnlocks();
            base.OnCleanUp();
        }

        public override void Sim1000ms(float dt) {
            SubscribeToUnlocks();
            if (pendingRecipeRefresh && CurrentWorkingOrder == null) {
                ApplyRecipeFilter(false);
            }
            // U59 ValidateNextOrder reads recipe_list[0] even when nothing is unlocked.
            if (CurrentWorkingOrder == null && GetRecipes().Length == 0) {
                return;
            }
            base.Sim1000ms(dt);
        }

        protected override float ComputeWorkProgress(float dt, ComplexRecipe recipe) {
            var power = GetComponent<FabricatorPowerBudget>();
            var cooling = GetComponent<CoolantController>();
            if (recipe == null || !HasIntactActiveBatch() || !HasOutputSpace(recipe) ||
                    power == null || cooling == null) {
                if (power != null) power.Clear();
                return 0f;
            }
            var seconds = power.TakeWorkSeconds(Math.Min(dt,
                Math.Max(0f, 1f - OrderProgress) * recipe.time));
            // Heat is committed exactly once for the same paid work interval.
            if (seconds <= 0f || !cooling.TryProcessWork(seconds)) {
                power.Clear();
                return 0f;
            }
            return seconds / recipe.time;
        }

        public override void CompleteWorkingOrder() {
            if (CurrentWorkingOrder == null || !HasIntactActiveBatch() ||
                    !HasOutputSpace(CurrentWorkingOrder)) {
                return;
            }
            EnsureCurrentRecipeCanCompleteOnce();
            var refreshAfterBatch = pendingRecipeRefresh;
            if (refreshAfterBatch) {
                operational.SetFlag(RecipeListStable, false);
            }

            base.CompleteWorkingOrder();

            if (refreshAfterBatch) {
                ApplyRecipeFilter(false);
                operational.SetFlag(RecipeListStable, true);
            }
        }

        protected override bool HasIngredients(ComplexRecipe recipe, Storage storage) {
            return base.HasIngredients(recipe, storage) && HasOutputSpace(recipe);
        }

        internal void RequestRecipeRefresh() {
            if (CurrentWorkingOrder != null) {
                pendingRecipeRefresh = true;
                return;
            }
            ApplyRecipeFilter(false);
        }

        internal bool HasIntactActiveBatch() {
            var recipe = CurrentWorkingOrder;
            if (recipe == null || recipe.ingredients == null || buildStorage == null) {
                return false;
            }
            for (var index = 0; index < recipe.ingredients.Length; index++) {
                var ingredient = recipe.ingredients[index];
                if (ingredient.amount - buildStorage.GetAmountAvailable(ingredient.material) >=
                        PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) {
                    return false;
                }
            }
            return true;
        }

        internal bool CanDispenseProduct(SimHashes elementId) {
            return dispensableProducts.Contains(elementId);
        }

        internal void RefreshOutputSpace() {
            var recipe = CurrentWorkingOrder ?? GetRecipes().FirstOrDefault(
                candidate => GetRecipeQueueCount(candidate) != 0);
            var hasRoom = recipe == null || HasOutputSpace(recipe);
            if (operational != null && operational.GetFlag(OutputSpace) != hasRoom) {
                operational.SetFlag(OutputSpace, hasRoom);
                SetQueueDirty();
            }
        }

        private void ApplyRecipeFilter(bool includeSavedBatch) {
            if (CurrentWorkingOrder != null) {
                pendingRecipeRefresh = true;
                return;
            }

            var saveData = ForbiddenTechSaveData.Instance;
            if (saveData == null) {
                pendingRecipeRefresh = true;
                return;
            }

            var activeIds = new HashSet<string>(RecipeRegistry.CompilerRecipes.Keys,
                StringComparer.Ordinal);
            var visibleIds = CompilerRecipeFilter.SelectIds(ElementCatalogAdapter.Rules.Values,
                saveData.GetUnlockState(), activeIds).ToList();

            var savedRecipeId = includeSavedBatch
                ? FabricatorSupport.GetLastWorkingRecipeId(this)
                : null;
            if (!string.IsNullOrEmpty(savedRecipeId)) {
                var savedPair = RecipeRegistry.CompilerRecipes.FirstOrDefault(
                    pair => pair.Value.id == savedRecipeId);
                if (!string.IsNullOrEmpty(savedPair.Key) && !visibleIds.Contains(savedPair.Key)) {
                    visibleIds.Add(savedPair.Key);
                    pendingRecipeRefresh = true;
                }
            }

            var recipes = visibleIds.Where(RecipeRegistry.CompilerRecipes.ContainsKey)
                .Select(id => RecipeRegistry.CompilerRecipes[id]).ToArray();
            FabricatorSupport.SetRecipeList(this, recipes);
            UpdateProductFilters();
            operational.SetFlag(RecipesAvailable, recipes.Length > 0);
            if (!includeSavedBatch) {
                pendingRecipeRefresh = false;
            }
        }

        private bool HasOutputSpace(ComplexRecipe recipe) {
            if (recipe == null || recipe.results == null || outStorage == null) {
                return false;
            }

            var resultMass = recipe.results.Sum(result => result.amount);
            return CompilerRecipeFilter.HasOutputCapacity(resultMass,
                outStorage.RemainingCapacity());
        }

        private void UpdateProductFilters() {
            // Recipe visibility is an unlock rule, not a rule for removing old products.
            var idArray = RecipeRegistry.CompilerRecipes.Keys.ToArray();
            var productTags = idArray.Select(id => new Tag(id)).ToArray();
            FabricatorSupport.SetStorageFilters(outStorage, productTags);
            dispensableProducts.Clear();
            foreach (var elementId in idArray) {
                dispensableProducts.Add((SimHashes)global::Hash.SDBMLower(elementId));
            }
            var dispenser = GetComponent<SolidConduitDispenser>();
            if (dispenser != null) {
                dispenser.elementFilter = dispensableProducts.ToArray();
            }
        }

        private void EnsureCurrentRecipeCanCompleteOnce() {
            var recipe = CurrentWorkingOrder;
            if (recipe != null && GetRecipeQueueCount(recipe) == 0) {
                SetRecipeQueueCount(recipe, 1);
            }
        }

        private void SubscribeToUnlocks() {
            if (subscribedSave == ForbiddenTechSaveData.Instance) return;
            UnsubscribeFromUnlocks();
            subscribedSave = ForbiddenTechSaveData.Instance;
            if (subscribedSave == null) return;
            subscribedSave.UnlocksChanged += RequestRecipeRefresh;
            pendingRecipeRefresh = true;
        }

        private void UnsubscribeFromUnlocks() {
            if (subscribedSave != null) subscribedSave.UnlocksChanged -= RequestRecipeRefresh;
            subscribedSave = null;
        }

        private void OnDeconstructComplete(object data) {
            DropStorage(inStorage);
            DropStorage(buildStorage);
            DropStorage(outStorage);
            var coolant = GetComponent<CoolantController>();
            DropStorage(coolant == null ? null : coolant.storage);
            DropStorage(coolant == null ? null : coolant.heatedStorage);
        }

        private static void DropStorage(Storage storage) {
            if (storage != null) {
                storage.DropAll(true, true, UnityEngine.Vector3.zero, true, null);
            }
        }
    }

    [HarmonyPatch(typeof(ComplexFabricator), "Sim200ms")]
    internal static class MatterCompilerOutputSpacePatch {
        private static void Prefix(ComplexFabricator __instance) {
            var compiler = __instance as MatterCompiler;
            if (compiler != null) compiler.RefreshOutputSpace();
        }
    }

    [HarmonyPatch(typeof(SolidConduitConsumer), "ConduitUpdate")]
    internal static class MatterCompilerRailInputPatch {
        private static bool Prefix(SolidConduitConsumer __instance) {
            if ((__instance.GetComponent<MatterCompiler>() == null &&
                    __instance.GetComponent<MassEnergy.ProtoAnnihilationReactor>() == null) || !__instance.IsConnected) {
                return true;
            }

            var building = __instance.GetComponent<Building>();
            if (building == null) {
                return true;
            }
            var inputCell = Grid.OffsetCell(building.NaturalBuildingCell(), new CellOffset(-2, 0));
            var contents = global::Game.Instance.solidConduitFlow.GetContents(inputCell);
            if (!contents.pickupableHandle.IsValid()) {
                return true;
            }
            var pickupable = global::Game.Instance.solidConduitFlow.GetPickupable(contents.pickupableHandle);
            return pickupable == null || pickupable.PrimaryElement == null ||
                pickupable.PrimaryElement.ElementID == ProtoMatterRegistration.Hash;
        }
    }

    [HarmonyPatch(typeof(SolidConduitDispenser), "FindSuitableItem")]
    internal static class MatterCompilerRailOutputPatch {
        private static void Postfix(SolidConduitDispenser __instance, ref Pickupable __result) {
            var compiler = __instance.GetComponent<MatterCompiler>();
            if (compiler != null && __result != null && __result.PrimaryElement != null &&
                    !compiler.CanDispenseProduct(__result.PrimaryElement.ElementID)) {
                __result = null;
            }
        }
    }

}
