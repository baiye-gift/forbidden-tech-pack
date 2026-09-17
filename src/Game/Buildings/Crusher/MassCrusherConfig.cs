using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Options;
using HarmonyLib;
using UnityEngine;
using TUNING;

namespace ForbiddenTechnologyPack.Game.Buildings.Crusher {
    [HarmonyPatch(typeof(SolidConduitConsumer), "ConduitUpdate")]
    internal static class MassCrusherRailInputPatch {
        private static bool Prefix(SolidConduitConsumer __instance) {
            if (__instance.GetComponent<MassCrusher>() == null || !__instance.IsConnected) {
                return true;
            }

            var building = __instance.GetComponent<Building>();
            if (building == null) {
                return true;
            }
            var flow = global::Game.Instance.solidConduitFlow;
            var contents = flow.GetContents(building.GetUtilityInputCell());
            if (!contents.pickupableHandle.IsValid()) {
                return true;
            }
            var pickupable = flow.GetPickupable(contents.pickupableHandle);
            return pickupable != null && pickupable.PrimaryElement != null &&
                CanAcceptElement(pickupable.PrimaryElement.ElementID.ToString());
        }

        internal static bool CanAcceptElement(string elementId) {
            return !string.IsNullOrEmpty(elementId) &&
                ElementCatalogAdapter.Rules.ContainsKey(elementId) && CrusherPolicy.CanCrush(elementId);
        }
    }

    public sealed class BaiyeMassCrusherConfig : IBuildingConfig {
        private const float InputCapacityKg = 1000f;
        private const float OutputCapacityKg = 120f;

        public override BuildingDef CreateBuildingDef() {
            var buildingDef = BuildingTemplates.CreateBuildingDef(ModIdentity.MassCrusherId,
                4, 4, "baiye_mass_crusher_kanim", 30, 120f,
                new[] { 400f, 200f }, new[] { "RefinedMetal", "Ceramic" },
                1600f, BuildLocationRule.OnFloor, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NONE,
                0.2f);
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = 2400f * ForbiddenTechOptions.Current.PowerMultiplier;
            buildingDef.SelfHeatKilowattsWhenActive = 40f * ForbiddenTechOptions.Current.HeatMultiplier;
            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.PowerInputOffset = new CellOffset(0, 0);
            buildingDef.InputConduitType = ConduitType.Solid;
            buildingDef.OutputConduitType = ConduitType.Solid;
            buildingDef.UtilityInputOffset = new CellOffset(-1, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(2, 0);
            buildingDef.LogicInputPorts = new List<LogicPorts.Port> {
                LogicPorts.Port.InputPort(LogicOperationalController.PORT_ID, new CellOffset(0, 0),
                    STRINGS.BUILDINGS.PREFABS.BAIYEMASSCRUSHER.LOGIC_PORT.NAME,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMASSCRUSHER.LOGIC_PORT.ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMASSCRUSHER.LOGIC_PORT.INACTIVE,
                    true, false)
            };
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject gameObject, Tag prefabTag) {
            var fabricator = gameObject.AddOrGet<MassCrusher>();
            fabricator.heatedTemperature = 293.15f;
            fabricator.sideScreenStyle = ComplexFabricatorSideScreen.StyleSetting.ListQueueHybrid;
            fabricator.storeProduced = true;
            fabricator.inStorage = FabricatorSupport.CreateSealedStorage(gameObject, InputCapacityKg, true);
            fabricator.buildStorage = FabricatorSupport.CreateSealedStorage(gameObject, InputCapacityKg, false);
            fabricator.outStorage = FabricatorSupport.CreateSealedStorage(gameObject, OutputCapacityKg, true);
            fabricator.outStorage.allowItemRemoval = true;
            fabricator.outStorage.allowUIItemRemoval = true;
            FabricatorSupport.SetStorageFilters(fabricator.inStorage, ElementCatalogAdapter.Rules.Keys
                .Where(CrusherPolicy.CanCrush).Select(id => new Tag(id)));
            FabricatorSupport.SetStorageFilters(fabricator.outStorage, new[] { ProtoMatterRegistration.Tag });

            var consumer = gameObject.AddOrGet<SolidConduitConsumer>();
            consumer.storage = fabricator.inStorage;
            consumer.capacityKG = InputCapacityKg;
            consumer.alwaysConsume = true;
            consumer.useSecondaryInput = false;

            var dispenser = gameObject.AddOrGet<SolidConduitDispenser>();
            dispenser.storage = fabricator.outStorage;
            dispenser.alwaysDispense = true;
            dispenser.useSecondaryOutput = false;
            dispenser.solidOnly = true;
            dispenser.elementFilter = new[] { ProtoMatterRegistration.Hash };

            gameObject.AddOrGet<FabricatorIngredientStatusManager>();
            gameObject.AddOrGet<CopyBuildingSettings>();
            gameObject.AddOrGet<Prioritizable>();
        }

        public override void DoPostConfigureComplete(GameObject gameObject) {
            gameObject.AddOrGet<ComplexFabricatorWorkable>();
            gameObject.AddOrGet<Operational>();
            gameObject.AddOrGet<ForbiddenTechDevice>();
            gameObject.AddOrGet<LogicOperationalController>();
            gameObject.AddOrGetDef<PoweredActiveController.Def>();
        }
    }
}
