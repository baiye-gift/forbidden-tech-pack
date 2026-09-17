using System;
using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Recipes;
using ForbiddenTechnologyPack.Game.Save;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Buildings.Analyzer {
    public sealed class MatterAnalyzer : ComplexFabricator {
        private bool recipeRefreshPending;

        protected override void OnSpawn() {
            base.OnSpawn();
            SubscribeToUnlocks();
            RefreshRecipes();
        }

        protected override void OnCleanUp() {
            UnsubscribeFromUnlocks();
            base.OnCleanUp();
        }

        public void RefreshRecipes() {
            recipeRefreshPending = true;
            if (CurrentWorkingOrder != null) {
                return;
            }

            var saveData = ForbiddenTechSaveData.Instance;
            if (saveData == null) {
                return;
            }

            var rules = ElementCatalogAdapter.Rules.Values;
            var activeIds = new HashSet<string>(RecipeRegistry.AnalyzerRecipes.Keys,
                StringComparer.Ordinal);
            var visibleIds = AnalyzerPolicy.VisibleRuleIds(rules, saveData.GetUnlockState(), activeIds);
            MatterAnalyzerRecipeList.Set(this, visibleIds.Where(RecipeRegistry.AnalyzerRecipes.ContainsKey)
                .Select(id => RecipeRegistry.AnalyzerRecipes[id]).ToArray());
            recipeRefreshPending = false;
        }

        public override void Sim1000ms(float dt) {
            if (recipeRefreshPending && CurrentWorkingOrder == null) {
                RefreshRecipes();
            }
            // Base queue validation indexes recipe 0 even when no analyses remain.
            if (CurrentWorkingOrder == null && GetRecipes().Length == 0) {
                return;
            }
            base.Sim1000ms(dt);
        }

        public override void CompleteWorkingOrder() {
            var completedRecipe = CurrentWorkingOrder;
            if (completedRecipe != null) {
                // Base completion decrements this count, then may start another material.
                // Do not disable Operational: that cancels unrelated open orders.
                SetRecipeQueueCount(completedRecipe, 1);
            }
            base.CompleteWorkingOrder();

            if (completedRecipe != null && completedRecipe.ingredients != null &&
                    completedRecipe.ingredients.Length > 0 && ForbiddenTechSaveData.Instance != null) {
                ForbiddenTechSaveData.Instance.Unlock(completedRecipe.ingredients[0].material);
                RefreshRecipes();
            }
        }

        protected override List<GameObject> SpawnOrderProduct(ComplexRecipe recipe) {
            var spawnedProducts = new List<GameObject>();
            if (recipe == null || recipe.ingredients == null) {
                return spawnedProducts;
            }

            foreach (var ingredient in recipe.ingredients) {
                if (ingredient.doNotConsume) {
                    buildStorage.TransferMass(outStorage, ingredient.material, ingredient.amount,
                        true, true, true);
                } else {
                    buildStorage.ConsumeIgnoringDisease(ingredient.material, ingredient.amount);
                }
            }

            return spawnedProducts;
        }

        private void SubscribeToUnlocks() {
            if (ForbiddenTechSaveData.Instance != null) {
                ForbiddenTechSaveData.Instance.UnlocksChanged += RefreshRecipes;
            }
        }

        private void UnsubscribeFromUnlocks() {
            if (ForbiddenTechSaveData.Instance != null) {
                ForbiddenTechSaveData.Instance.UnlocksChanged -= RefreshRecipes;
            }
        }
    }

    internal static class MatterAnalyzerRecipeList {
        internal static void Set(ComplexFabricator fabricator, ComplexRecipe[] recipes) {
            FabricatorSupport.SetRecipeList(fabricator, recipes);
        }
    }

}
