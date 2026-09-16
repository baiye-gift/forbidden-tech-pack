using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    internal static class FabricatorSupport {
        private static readonly System.Reflection.FieldInfo RecipeListField =
            AccessTools.Field(typeof(ComplexFabricator), "recipe_list");
        private static readonly System.Reflection.FieldInfo RecipeQueueCountsField =
            AccessTools.Field(typeof(ComplexFabricator), "recipeQueueCounts");
        private static readonly System.Reflection.FieldInfo OpenOrderCountsField =
            AccessTools.Field(typeof(ComplexFabricator), "openOrderCounts");
        private static readonly System.Reflection.FieldInfo NextOrderIndexField =
            AccessTools.Field(typeof(ComplexFabricator), "nextOrderIdx");
        private static readonly System.Reflection.FieldInfo NextOrderIsWorkableField =
            AccessTools.Field(typeof(ComplexFabricator), "nextOrderIsWorkable");
        private static readonly System.Reflection.FieldInfo LastWorkingRecipeField =
            AccessTools.Field(typeof(ComplexFabricator), "lastWorkingRecipe");
        private static readonly System.Reflection.FieldInfo ShouldSaveItemsField =
            AccessTools.Field(typeof(Storage), "shouldSaveItems");

        internal static Storage CreateSealedStorage(GameObject gameObject, float capacityKg,
                bool showInUi) {
            var storage = gameObject.AddComponent<Storage>();
            storage.capacityKg = capacityKg;
            storage.showInUI = showInUi;
            if (ShouldSaveItemsField == null) {
                throw new MissingFieldException(typeof(Storage).FullName, "shouldSaveItems");
            }
            ShouldSaveItemsField.SetValue(storage, true);
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
            return storage;
        }

        internal static void SetStorageFilters(Storage storage, IEnumerable<Tag> filters) {
            if (storage == null) {
                throw new ArgumentNullException("storage");
            }

            storage.storageFilters = filters == null ? new List<Tag>() : filters.ToList();
        }

        internal static void SetRecipeList(ComplexFabricator fabricator, IEnumerable<ComplexRecipe> recipes) {
            if (fabricator == null) {
                throw new ArgumentNullException("fabricator");
            }
            if (RecipeListField == null) {
                throw new MissingFieldException(typeof(ComplexFabricator).FullName, "recipe_list");
            }

            if (fabricator.CurrentWorkingOrder != null) {
                throw new InvalidOperationException("A fabricator recipe list cannot change during an active batch.");
            }

            var recipeArray = recipes == null ? new ComplexRecipe[0] : recipes.ToArray();
            EnsureRecipeReflectionFields();
            var queueCounts = (Dictionary<string, int>)RecipeQueueCountsField.GetValue(fabricator);
            for (var index = 0; index < recipeArray.Length; index++) {
                if (!queueCounts.ContainsKey(recipeArray[index].id)) {
                    queueCounts.Add(recipeArray[index].id, 0);
                }
            }

            RecipeListField.SetValue(fabricator, recipeArray);
            OpenOrderCountsField.SetValue(fabricator,
                Enumerable.Repeat(0, recipeArray.Length).ToList());
            NextOrderIndexField.SetValue(fabricator, 0);
            NextOrderIsWorkableField.SetValue(fabricator, false);
            fabricator.SetQueueDirty();
        }

        internal static string GetLastWorkingRecipeId(ComplexFabricator fabricator) {
            if (fabricator == null) {
                throw new ArgumentNullException("fabricator");
            }
            if (LastWorkingRecipeField == null) {
                throw new MissingFieldException(typeof(ComplexFabricator).FullName, "lastWorkingRecipe");
            }

            return (string)LastWorkingRecipeField.GetValue(fabricator);
        }

        private static void EnsureRecipeReflectionFields() {
            if (RecipeQueueCountsField == null) {
                throw new MissingFieldException(typeof(ComplexFabricator).FullName, "recipeQueueCounts");
            }
            if (OpenOrderCountsField == null) {
                throw new MissingFieldException(typeof(ComplexFabricator).FullName, "openOrderCounts");
            }
            if (NextOrderIndexField == null) {
                throw new MissingFieldException(typeof(ComplexFabricator).FullName, "nextOrderIdx");
            }
            if (NextOrderIsWorkableField == null) {
                throw new MissingFieldException(typeof(ComplexFabricator).FullName, "nextOrderIsWorkable");
            }
        }
    }
}
