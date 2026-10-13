using System;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Buildings.Compiler;
using ForbiddenTechnologyPack.Game.Recipes;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Buildings.MassEnergy {
    public sealed class EnergyMatterCondenser : ComplexFabricator {
        internal static readonly Operational.Flag OutputSpace = new Operational.Flag("CondenserOutputSpace", Operational.Flag.Type.Requirement);
        protected override void OnSpawn() {
            base.OnSpawn();
            FabricatorSupport.SetRecipeList(this, new[] { RecipeRegistry.CondenserRecipe });
            Subscribe((int)GameHashes.DeconstructComplete, OnDeconstruct);
            RefreshOutputSpace();
        }
        protected override void OnCleanUp() {
            Unsubscribe((int)GameHashes.DeconstructComplete, OnDeconstruct);
            base.OnCleanUp();
        }
        protected override float ComputeWorkProgress(float dt, ComplexRecipe recipe) {
            var power = GetComponent<FabricatorPowerBudget>(); var coolant = GetComponent<CoolantController>();
            if (recipe == null || power == null || coolant == null || outStorage.RemainingCapacity() < 1f) {
                if (power != null) power.Clear(); return 0;
            }
            var seconds = power.TakeWorkSeconds(Math.Min(dt, (1f - OrderProgress) * recipe.time));
            if (seconds <= 0 || !coolant.TryProcessWork(seconds)) { power.Clear(); return 0; }
            return seconds / recipe.time;
        }
        public override void CompleteWorkingOrder() {
            if (CurrentWorkingOrder == null || OrderProgress < 1f || outStorage.RemainingCapacity() < 1f) return;
            base.CompleteWorkingOrder();
        }
        protected override bool HasIngredients(ComplexRecipe recipe, Storage storage) {
            return recipe != null && outStorage.RemainingCapacity() >= 1f;
        }
        internal void RefreshOutputSpace() {
            var hasRoom = outStorage != null && outStorage.RemainingCapacity() >= 1f;
            if (operational != null && operational.GetFlag(OutputSpace) != hasRoom) {
                operational.SetFlag(OutputSpace, hasRoom); SetQueueDirty();
            }
        }
        public string StateText { get {
            if (outStorage.RemainingCapacity() < 1f) return STRINGS.UI.MATTER.OUTPUT_FULL;
            var coolant = GetComponent<CoolantController>();
            if (coolant != null && !coolant.IsReady) return coolant.StateText;
            if (!operational.IsOperational) return STRINGS.UI.MATTER.NOT_OPERATIONAL;
            return CurrentWorkingOrder == null ? (string)STRINGS.UI.MATTER.CHOOSE :
                string.Format(STRINGS.UI.MASSENERGY.CONDENSING, OrderProgress * 100f);
        } }
        public string DetailText { get { var coolant = GetComponent<CoolantController>();
            return string.Format(STRINGS.UI.MASSENERGY.CONDENSER_DETAIL, OrderProgress * 18f, outStorage.MassStored()) +
                "\n" + (coolant == null ? "" : coolant.DetailText); } }
        private void OnDeconstruct(object data) {
            foreach (var storage in GetComponents<Storage>()) storage.DropAll(true, true, Vector3.zero, true, null);
        }
    }
    [HarmonyPatch(typeof(ComplexFabricator), "Sim200ms")]
    internal static class CondenserOutputSpacePatch {
        private static void Prefix(ComplexFabricator __instance) {
            var condenser = __instance as EnergyMatterCondenser;
            if (condenser != null) condenser.RefreshOutputSpace();
        }
    }
}
