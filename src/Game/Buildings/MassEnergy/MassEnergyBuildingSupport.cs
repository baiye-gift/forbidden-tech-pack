using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Buildings.Compiler;
using UnityEngine;
using TUNING;

namespace ForbiddenTechnologyPack.Game.Buildings.MassEnergy {
    internal static class MassEnergyBuildingSupport {
        internal static BuildingDef Create(string id, int size, string anim, float watts) {
            var def = BuildingTemplates.CreateBuildingDef(id, size, size, anim, 100, 240f,
                new[] { 800f, 400f, 200f }, new[] { "RefinedMetal", "Ceramic", "Glass" },
                2400f, BuildLocationRule.OnFloor, BUILDINGS.DECOR.PENALTY.TIER2, NOISE_POLLUTION.NOISY.TIER6, .2f);
            def.RequiresPowerInput = true; def.EnergyConsumptionWhenActive = watts;
            def.SelfHeatKilowattsWhenActive = 0; def.ExhaustKilowattsWhenActive = 0;
            def.ViewMode = OverlayModes.Power.ID; def.PowerInputOffset = new CellOffset(0, 0);
            def.InputConduitType = ConduitType.Liquid; def.OutputConduitType = ConduitType.Liquid;
            def.UtilityInputOffset = new CellOffset(-1, 0); def.UtilityOutputOffset = new CellOffset(1, 0);
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            return def;
        }
        internal static void Cooling(GameObject go, float heatWatts) {
            var cold = FabricatorSupport.CreateSealedStorage(go, 10f, true);
            var hot = FabricatorSupport.CreateSealedStorage(go, 10f, true);
            FabricatorSupport.SetStorageFilters(cold, new[] { GameTags.Liquid });
            FabricatorSupport.SetStorageFilters(hot, new[] { GameTags.Liquid });
            cold.allowItemRemoval = hot.allowItemRemoval = true;
            cold.allowUIItemRemoval = hot.allowUIItemRemoval = true;
            var consumer = go.AddOrGet<ConduitConsumer>(); consumer.conduitType = ConduitType.Liquid;
            consumer.storage = cold; consumer.capacityTag = GameTags.Liquid; consumer.capacityKG = 10;
            consumer.consumptionRate = 10; consumer.alwaysConsume = true; consumer.forceAlwaysSatisfied = true;
            var dispenser = go.AddOrGet<ConduitDispenser>(); dispenser.conduitType = ConduitType.Liquid;
            dispenser.storage = hot; dispenser.elementFilter = null; dispenser.alwaysDispense = true; dispenser.isOn = true;
            var controller = go.AddOrGet<CoolantController>(); controller.storage = cold;
            controller.heatedStorage = hot; controller.heatJoulesPerSecond = heatWatts;
        }
        internal static void Complete(GameObject go) {
            go.AddOrGet<Operational>(); go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<FabricatorPowerBudget>(); go.AddOrGet<MassEnergyStatus>();
            go.AddOrGet<CopyBuildingSettings>(); go.AddOrGet<Prioritizable>();
        }
    }
}
