using System;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Recipes;
using HarmonyLib;

namespace ForbiddenTechnologyPack.Game.Buildings.Crusher {
    public sealed class MassCrusher : ComplexFabricator {
        internal static readonly Operational.Flag OutputSpace =
            new Operational.Flag("OutputSpace", Operational.Flag.Type.Requirement);

        protected override void OnSpawn() {
            base.OnSpawn();
            FabricatorSupport.SetRecipeList(this, RecipeRegistry.CrusherRecipes.Values);
            Subscribe((int)GameHashes.DeconstructComplete, OnDeconstructComplete);
            RefreshOutputSpace();
        }

        protected override void OnCleanUp() {
            Unsubscribe((int)GameHashes.DeconstructComplete, OnDeconstructComplete);
            base.OnCleanUp();
        }

        private void OnDeconstructComplete(object data) {
            DropStorage(inStorage);
            DropStorage(buildStorage);
            DropStorage(outStorage);
        }

        internal void RefreshOutputSpace() {
            var operational = GetComponent<Operational>();
            if (operational == null) {
                return;
            }

            var hasOutputSpace = HasOutputSpace();
            if (operational.GetFlag(OutputSpace) != hasOutputSpace) {
                operational.SetFlag(OutputSpace, hasOutputSpace);
                SetQueueDirty();
            }
        }

        private bool HasOutputSpace() {
            var recipe = CurrentWorkingOrder ?? NextOrder;
            if (recipe == null) {
                return true;
            }
            if (recipe.ingredients == null || recipe.ingredients.Length == 0 ||
                    recipe.results == null || recipe.results.Length == 0 || outStorage == null) {
                return false;
            }

            var ingredient = recipe.ingredients[0];
            var result = recipe.results[0];
            return CrusherPolicy.CanCrush(ingredient.material.Name) &&
                CrusherPolicy.CanStart(ingredient.amount, result.amount, outStorage.RemainingCapacity());
        }

        private static void DropStorage(Storage storage) {
            if (storage != null) {
                storage.DropAll(true, true, UnityEngine.Vector3.zero, true, null);
            }
        }
    }

    [HarmonyPatch(typeof(ComplexFabricator), "Sim200ms")]
    internal static class MassCrusherOutputSpacePatch {
        private static void Postfix(ComplexFabricator __instance) {
            var crusher = __instance as MassCrusher;
            if (crusher != null) {
                crusher.RefreshOutputSpace();
            }
        }
    }
}
