using System;
using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Recipes;
using ForbiddenTechnologyPack.Game.Save;
using HarmonyLib;

namespace ForbiddenTechnologyPack.Game.Buildings.Reconstructor {
    public sealed class MatterReconstructor : ComplexFabricator {
        private bool pendingRecipeRefresh;
        private readonly HashSet<SimHashes> dispensableProducts = new HashSet<SimHashes>();

        private static readonly Operational.Flag RecipeListStable =
            new Operational.Flag("ReconstructorRecipeListStable", Operational.Flag.Type.Requirement);
        private static readonly Operational.Flag RecipesAvailable =
            new Operational.Flag("ReconstructorRecipesAvailable", Operational.Flag.Type.Requirement);
        private static readonly Operational.Flag OutputSpace =
            new Operational.Flag("ReconstructorOutputSpace", Operational.Flag.Type.Requirement);

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

        public override void CompleteWorkingOrder() {
            EnsureCurrentRecipeCanCompleteOnce();
            var refreshAfterBatch = pendingRecipeRefresh;
            if (refreshAfterBatch) {
                operational.SetFlag(RecipeListStable, false);
            }

            base.CompleteWorkingOrder();
            RefreshOutputSpace();

            if (refreshAfterBatch) {
                ApplyRecipeFilter(false);
                operational.SetFlag(RecipeListStable, true);
            }
        }

        protected override bool HasIngredients(ComplexRecipe recipe, Storage storage) {
            var hasOutputSpace = HasOutputSpace(recipe);
            operational.SetFlag(OutputSpace, hasOutputSpace);
            return hasOutputSpace && base.HasIngredients(recipe, storage);
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

            var visibleIds = ReconstructionRecipeAdapter.SelectUnlockedIds(
                saveData.GetUnlockState()).ToList();
            var savedRecipeId = includeSavedBatch
                ? FabricatorSupport.GetLastWorkingRecipeId(this)
                : null;
            if (!string.IsNullOrEmpty(savedRecipeId)) {
                var savedPair = RecipeRegistry.ReconstructorRecipes.FirstOrDefault(
                    pair => pair.Value.id == savedRecipeId);
                if (!string.IsNullOrEmpty(savedPair.Key) && !visibleIds.Contains(savedPair.Key)) {
                    visibleIds.Add(savedPair.Key);
                    pendingRecipeRefresh = true;
                }
            }

            var recipes = visibleIds.Where(RecipeRegistry.ReconstructorRecipes.ContainsKey)
                .Select(id => RecipeRegistry.ReconstructorRecipes[id]).ToArray();
            FabricatorSupport.SetRecipeList(this, recipes);
            UpdateProductFilters(visibleIds);
            operational.SetFlag(RecipesAvailable, recipes.Length > 0);
            RefreshOutputSpace();
            if (!includeSavedBatch) {
                pendingRecipeRefresh = false;
            }
        }

        private void RefreshOutputSpace() {
            if (operational == null) {
                return;
            }
            var recipe = CurrentWorkingOrder ?? NextOrder;
            var hasOutputSpace = recipe == null || HasOutputSpace(recipe);
            if (operational.GetFlag(OutputSpace) != hasOutputSpace) {
                operational.SetFlag(OutputSpace, hasOutputSpace);
                SetQueueDirty();
            }
        }

        private bool HasOutputSpace(ComplexRecipe recipe) {
            if (recipe == null || recipe.results == null || outStorage == null) {
                return false;
            }

            var resultMass = recipe.results.Sum(result => result.amount);
            return outStorage.RemainingCapacity() >= resultMass;
        }

        private void UpdateProductFilters(IEnumerable<string> visibleIds) {
            var idArray = visibleIds.ToArray();
            FabricatorSupport.SetStorageFilters(outStorage,
                idArray.Select(id => new Tag(id)));
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
            if (ForbiddenTechSaveData.Instance != null) {
                ForbiddenTechSaveData.Instance.UnlocksChanged += RequestRecipeRefresh;
            }
        }

        private void UnsubscribeFromUnlocks() {
            if (ForbiddenTechSaveData.Instance != null) {
                ForbiddenTechSaveData.Instance.UnlocksChanged -= RequestRecipeRefresh;
            }
        }

        private void OnDeconstructComplete(object data) {
            DropStorage(inStorage);
            DropStorage(buildStorage);
            DropStorage(outStorage);
        }

        private static void DropStorage(Storage storage) {
            if (storage != null) {
                storage.DropAll(true, true, UnityEngine.Vector3.zero, true, null);
            }
        }
    }

    [HarmonyPatch(typeof(SolidConduitDispenser), "FindSuitableItem")]
    internal static class MatterReconstructorRailOutputPatch {
        private static void Postfix(SolidConduitDispenser __instance, ref Pickupable __result) {
            var reconstructor = __instance.GetComponent<MatterReconstructor>();
            if (reconstructor != null && __result != null && __result.PrimaryElement != null &&
                    !reconstructor.CanDispenseProduct(__result.PrimaryElement.ElementID)) {
                __result = null;
            }
        }
    }

    [HarmonyPatch(typeof(ComplexFabricator), "CancelWorkingOrder")]
    internal static class MatterReconstructorCancellationPatch {
        private static bool Prefix(ComplexFabricator __instance) {
            var reconstructor = __instance as MatterReconstructor;
            return reconstructor == null || !reconstructor.HasIntactActiveBatch();
        }
    }
}
