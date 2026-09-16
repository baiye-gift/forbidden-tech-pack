using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Options;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Recipes {
    public static class RecipeRegistry {
        private static readonly IReadOnlyDictionary<string, ComplexRecipe> EmptyRecipes =
            new ReadOnlyDictionary<string, ComplexRecipe>(new Dictionary<string, ComplexRecipe>(StringComparer.Ordinal));

        private static bool wasBuilt;

        public static IReadOnlyDictionary<string, ComplexRecipe> AnalyzerRecipes { get; private set; } = EmptyRecipes;
        public static IReadOnlyDictionary<string, ComplexRecipe> CrusherRecipes { get; private set; } = EmptyRecipes;
        public static IReadOnlyDictionary<string, ComplexRecipe> CompilerRecipes { get; private set; } = EmptyRecipes;

        public static void Build() {
            if (wasBuilt) {
                return;
            }

            var analyzerRecipes = new Dictionary<string, ComplexRecipe>(StringComparer.Ordinal);
            var crusherRecipes = new Dictionary<string, ComplexRecipe>(StringComparer.Ordinal);
            var compilerRecipes = new Dictionary<string, ComplexRecipe>(StringComparer.Ordinal);
            var orderedRules = ElementCatalogAdapter.Rules.Values
                .OrderBy(rule => GetLocalizedElementName(rule.ElementId), StringComparer.CurrentCulture)
                .ThenBy(rule => rule.ElementId, StringComparer.Ordinal)
                .ToList();

            for (var index = 0; index < orderedRules.Count; index++) {
                var rule = orderedRules[index];
                RecipePlan plan;
                try {
                    plan = RecipePlanFactory.Create(rule, ForbiddenTechOptions.Current);
                } catch (ArgumentException exception) {
                    Debug.LogWarning("[ForbiddenTechnologyPack] Excluded unsafe recipe for " + rule.ElementId + ": " +
                        exception.Message);
                    continue;
                }

                analyzerRecipes.Add(rule.ElementId, CreateAnalyzerRecipe(plan, index));
                crusherRecipes.Add(rule.ElementId, CreateCrusherRecipe(plan, index));
                compilerRecipes.Add(rule.ElementId, CreateCompilerRecipe(plan, index));
            }

            AnalyzerRecipes = Freeze(analyzerRecipes);
            CrusherRecipes = Freeze(crusherRecipes);
            CompilerRecipes = Freeze(compilerRecipes);
            wasBuilt = true;
        }

        private static ComplexRecipe CreateAnalyzerRecipe(RecipePlan plan, int sortOrder) {
            var input = new ComplexRecipe.RecipeElement(new Tag(plan.ElementId), plan.AnalyzerInputKg);
            input.doNotConsume = plan.AnalyzerInputDoNotConsume;
            var metadataResult = new ComplexRecipe.RecipeElement(new Tag(plan.ElementId), 1f);
            var recipe = CreateRecipe(plan.AnalyzerId, new[] { input }, new[] { metadataResult },
                ModIdentity.MatterAnalyzerId, plan.AnalyzerTimeSeconds, sortOrder);
            recipe.nameDisplay = ComplexRecipe.RecipeNameDisplay.Ingredient;
            return recipe;
        }

        private static ComplexRecipe CreateCrusherRecipe(RecipePlan plan, int sortOrder) {
            return CreateRecipe(plan.CrusherId,
                new[] { new ComplexRecipe.RecipeElement(new Tag(plan.ElementId), plan.CrusherInputKg) },
                new[] { new ComplexRecipe.RecipeElement(ProtoMatterRegistration.Tag, plan.CrusherOutputKg) },
                ModIdentity.MassCrusherId, plan.CrusherTimeSeconds, sortOrder);
        }

        private static ComplexRecipe CreateCompilerRecipe(RecipePlan plan, int sortOrder) {
            return CreateRecipe(plan.CompilerId,
                new[] { new ComplexRecipe.RecipeElement(ProtoMatterRegistration.Tag, plan.CompilerInputKg) },
                new[] { new ComplexRecipe.RecipeElement(new Tag(plan.ElementId), plan.CompilerOutputKg) },
                ModIdentity.MatterCompilerId, plan.CompilerTimeSeconds, sortOrder);
        }

        private static ComplexRecipe CreateRecipe(string recipePrefix, ComplexRecipe.RecipeElement[] ingredients,
                ComplexRecipe.RecipeElement[] results, string fabricatorId, float timeSeconds, int sortOrder) {
            var recipeId = ComplexRecipeManager.MakeRecipeID(recipePrefix, ingredients, results);
            var recipe = new ComplexRecipe(recipeId, ingredients, results);
            recipe.time = timeSeconds;
            recipe.fabricators = new List<Tag> { new Tag(fabricatorId) };
            recipe.sortOrder = sortOrder;
            return recipe;
        }

        private static IReadOnlyDictionary<string, ComplexRecipe> Freeze(Dictionary<string, ComplexRecipe> recipes) {
            return new ReadOnlyDictionary<string, ComplexRecipe>(recipes);
        }

        private static string GetLocalizedElementName(string elementId) {
            var element = ElementLoader.FindElementByHash((SimHashes)global::Hash.SDBMLower(elementId));
            return element == null || string.IsNullOrEmpty(element.name) ? elementId : element.name;
        }
    }
}
