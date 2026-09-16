using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    internal static class FabricatorSupport {
        private static readonly System.Reflection.FieldInfo RecipeListField =
            AccessTools.Field(typeof(ComplexFabricator), "recipe_list");
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

            RecipeListField.SetValue(fabricator, recipes == null ? new ComplexRecipe[0] : recipes.ToArray());
            fabricator.SetQueueDirty();
        }
    }
}
