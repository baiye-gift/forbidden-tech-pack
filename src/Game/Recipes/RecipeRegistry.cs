using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
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
        public static ComplexRecipe CondenserRecipe { get; private set; }

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
            CondenserRecipe = CreateCondenserRecipe();
            wasBuilt = true;
        }
        private static ComplexRecipe CreateCondenserRecipe() {
            var condensed = new ComplexRecipe.RecipeElement(ProtoMatterRegistration.Tag, 1f);
            condensed.temperatureOperation = ComplexRecipe.RecipeElement.TemperatureOperation.Heated;
            var recipe = CreateRecipe("BaiyeCondenseProtoMatter", new ComplexRecipe.RecipeElement[0],
                new[] { condensed }, ModIdentity.EnergyCondenserId, (float)MassEnergyBalance.CondenseSecondsPerKg, 0);
            recipe.description = STRINGS.UI.MASSENERGY.CONDENSE_RECIPE;
            return recipe;
        }

        private static ComplexRecipe CreateAnalyzerRecipe(RecipePlan plan, int sortOrder) {
            var input = new ComplexRecipe.RecipeElement(new Tag(plan.ElementId), plan.AnalyzerInputKg);
            input.doNotConsume = plan.AnalyzerInputDoNotConsume;
            var metadataResult = new ComplexRecipe.RecipeElement(new Tag(plan.ElementId), 1f);
            var recipe = CreateRecipe(plan.AnalyzerId, new[] { input }, new[] { metadataResult },
                ModIdentity.MatterAnalyzerId, plan.AnalyzerTimeSeconds, sortOrder);
            recipe.nameDisplay = ComplexRecipe.RecipeNameDisplay.Ingredient;
            recipe.description = string.Format(STRINGS.UI.MATTER.ANALYZE,
                GetLocalizedElementName(plan.ElementId), plan.AnalyzerInputKg,
                plan.AnalyzerInputDoNotConsume ? STRINGS.UI.MATTER.RETURNED : STRINGS.UI.MATTER.CONSUMED);
            return recipe;
        }

        private static ComplexRecipe CreateCrusherRecipe(RecipePlan plan, int sortOrder) {
            var recipe = CreateRecipe(plan.CrusherId,
                new[] { new ComplexRecipe.RecipeElement(new Tag(plan.ElementId), plan.CrusherInputKg) },
                new[] { new ComplexRecipe.RecipeElement(ProtoMatterRegistration.Tag, plan.CrusherOutputKg) },
                ModIdentity.MassCrusherId, plan.CrusherTimeSeconds, sortOrder);
            recipe.nameDisplay = ComplexRecipe.RecipeNameDisplay.Ingredient;
            recipe.description = string.Format(STRINGS.UI.MATTER.CRUSH, plan.CrusherInputKg,
                GetLocalizedElementName(plan.ElementId), plan.CrusherOutputKg,
                plan.CrusherInputKg - plan.CrusherOutputKg);
            return recipe;
        }

        private static ComplexRecipe CreateCompilerRecipe(RecipePlan plan, int sortOrder) {
            var recipe = CreateRecipe(plan.CompilerId,
                new[] { new ComplexRecipe.RecipeElement(ProtoMatterRegistration.Tag, plan.CompilerInputKg) },
                new[] { new ComplexRecipe.RecipeElement(new Tag(plan.ElementId), plan.CompilerOutputKg) },
                ModIdentity.MatterCompilerId, plan.CompilerTimeSeconds, sortOrder);
            recipe.description = string.Format(STRINGS.UI.MATTER.COMPILE, plan.CompilerOutputKg,
                GetLocalizedElementName(plan.ElementId), plan.CompilerInputKg);
            return recipe;
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
            try {
                var element = ElementLoader.FindElementByHash(ComputeSdbmLower(elementId));
                return element == null || string.IsNullOrEmpty(element.name) ? elementId : element.name;
            } catch (TypeInitializationException exception) when (exception.InnerException is SecurityException) {
                // Command-line probes cannot initialize Unity's streaming-assets ECall.
                return elementId;
            }
        }

        private static SimHashes ComputeSdbmLower(string value) {
            unchecked {
                uint hash = 0;
                for (var index = 0; index < value.Length; index++) {
                    var character = char.ToLowerInvariant(value[index]);
                    hash = character + (hash << 6) + (hash << 16) - hash;
                }
                return (SimHashes)hash;
            }
        }
    }
}
