using System;
using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Recipes;
using ForbiddenTechnologyPack.Game.Save;
using ForbiddenTechnologyPack.Game.Elements;
using HarmonyLib;

namespace ForbiddenTechnologyPack.Game.Buildings.Analyzer {
    public sealed class MatterAnalyzer : ComplexFabricator {
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
            var saveData = ForbiddenTechSaveData.Instance;
            if (saveData == null) {
                return;
            }

            var rules = ElementCatalogAdapter.Rules.Values;
            var activeIds = new HashSet<string>(RecipeRegistry.AnalyzerRecipes.Keys,
                StringComparer.Ordinal);
            var visibleIds = AnalyzerPolicy.VisibleRuleIds(rules, saveData.GetUnlockState(), activeIds);
            MatterAnalyzerRecipeList.Set(this, visibleIds.Where(RecipeRegistry.AnalyzerRecipes.ContainsKey)
                .Select(id => RecipeRegistry.AnalyzerRecipes[id]).ToList());
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

    [HarmonyPatch(typeof(ComplexFabricator), "CompleteWorkingOrder")]
    internal static class MatterAnalyzerCompletionPatch {
        private static void Prefix(ComplexFabricator __instance, ref ComplexRecipe __state) {
            if (__instance is MatterAnalyzer) {
                __state = __instance.CurrentWorkingOrder;
            }
        }

        private static void Postfix(ComplexFabricator __instance, ComplexRecipe __state) {
            var analyzer = __instance as MatterAnalyzer;
            if (analyzer == null || __state == null || __state.ingredients == null ||
                    __state.ingredients.Length == 0 || ForbiddenTechSaveData.Instance == null) {
                return;
            }

            ForbiddenTechSaveData.Instance.Unlock(__state.ingredients[0].material);
            analyzer.RefreshRecipes();
        }
    }

    internal static class MatterAnalyzerRecipeList {
        private static readonly System.Reflection.FieldInfo RecipeListField =
            AccessTools.Field(typeof(ComplexFabricator), "recipe_list");

        internal static void Set(ComplexFabricator fabricator, List<ComplexRecipe> recipes) {
            if (RecipeListField == null) {
                throw new MissingFieldException(typeof(ComplexFabricator).FullName, "recipe_list");
            }
            RecipeListField.SetValue(fabricator, recipes);
        }
    }
}
