using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Options;
using UnityEngine;
using TUNING;

namespace ForbiddenTechnologyPack.Game.Buildings.EntropyDiverter {
    public sealed class BaiyeEntropyFluxDiverterConfig : IBuildingConfig {
        private const float LiquidCapacityKg = 10f;
        private const float ProtoMatterCapacityKg = 5f;

        private static readonly ConduitPortInfo ColdInputPort =
            new ConduitPortInfo(ConduitType.Liquid, new CellOffset(-1, 2));
        private static readonly ConduitPortInfo ColdOutputPort =
            new ConduitPortInfo(ConduitType.Liquid, new CellOffset(1, 2));

        public override BuildingDef CreateBuildingDef() {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                ModIdentity.EntropyFluxDiverterId,
                4, 4, "baiye_entropy_flux_diverter_kanim", 60, 180f,
                new[] { 600f, 300f, 100f },
                new[] { "RefinedMetal", "Ceramic", "Glass" },
                1600f, BuildLocationRule.OnFloor, BUILDINGS.DECOR.PENALTY.TIER2,
                NOISE_POLLUTION.NOISY.TIER4, 0.2f);
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive =
                3600f * ForbiddenTechOptions.Current.PowerMultiplier;
            buildingDef.SelfHeatKilowattsWhenActive =
                12f * ForbiddenTechOptions.Current.HeatMultiplier;
            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.ViewMode = OverlayModes.LiquidConduits.ID;
            buildingDef.PowerInputOffset = new CellOffset(0, 0);

            // Primary liquid pair is the hot side.
            buildingDef.InputConduitType = ConduitType.Liquid;
            buildingDef.OutputConduitType = ConduitType.Liquid;
            buildingDef.UtilityInputOffset = new CellOffset(-1, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(1, 0);
            buildingDef.LogicInputPorts = new List<LogicPorts.Port> {
                LogicPorts.Port.InputPort(LogicOperationalController.PORT_ID,
                    new CellOffset(0, 0),
                    STRINGS.BUILDINGS.PREFABS.BAIYEENTROPYFLUXDIVERTER.LOGIC_PORT.NAME,
                    STRINGS.BUILDINGS.PREFABS.BAIYEENTROPYFLUXDIVERTER.LOGIC_PORT.ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.BAIYEENTROPYFLUXDIVERTER.LOGIC_PORT.INACTIVE,
                    true, false)
            };
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject gameObject, Tag prefabTag) {
            gameObject.GetComponent<KPrefabID>().AddTag(
                RoomConstraints.ConstraintTags.IndustrialMachinery);

            var controller = gameObject.AddOrGet<EntropyFluxDiverterController>();
            controller.hotStorage = CreateLiquidStorage(gameObject);
            controller.coldStorage = CreateLiquidStorage(gameObject);
            controller.protoMatterStorage = FabricatorSupport.CreateSealedStorage(
                gameObject, ProtoMatterCapacityKg, true);
            FabricatorSupport.SetStorageFilters(controller.protoMatterStorage,
                new[] { ProtoMatterRegistration.Tag });

            AttachColdPorts(gameObject);

            var hotConsumer = gameObject.AddComponent<ConduitConsumer>();
            hotConsumer.conduitType = ConduitType.Liquid;
            hotConsumer.capacityTag = GameTags.Liquid;
            hotConsumer.capacityKG = LiquidCapacityKg;
            hotConsumer.consumptionRate = LiquidCapacityKg;
            hotConsumer.storage = controller.hotStorage;
            hotConsumer.alwaysConsume = true;
            hotConsumer.forceAlwaysSatisfied = true;
            hotConsumer.useSecondaryInput = false;
            controller.hotConsumer = hotConsumer;

            var coldConsumer = gameObject.AddComponent<ConduitConsumer>();
            coldConsumer.conduitType = ConduitType.Liquid;
            coldConsumer.capacityTag = GameTags.Liquid;
            coldConsumer.capacityKG = LiquidCapacityKg;
            coldConsumer.consumptionRate = LiquidCapacityKg;
            coldConsumer.storage = controller.coldStorage;
            coldConsumer.alwaysConsume = true;
            coldConsumer.forceAlwaysSatisfied = true;
            coldConsumer.useSecondaryInput = true;
            controller.coldConsumer = coldConsumer;

            var hotDispenser = gameObject.AddComponent<ConduitDispenser>();
            hotDispenser.conduitType = ConduitType.Liquid;
            hotDispenser.storage = controller.hotStorage;
            hotDispenser.alwaysDispense = true;
            hotDispenser.isOn = false;
            hotDispenser.useSecondaryOutput = false;
            controller.hotDispenser = hotDispenser;

            var coldDispenser = gameObject.AddComponent<ConduitDispenser>();
            coldDispenser.conduitType = ConduitType.Liquid;
            coldDispenser.storage = controller.coldStorage;
            coldDispenser.alwaysDispense = true;
            coldDispenser.isOn = false;
            coldDispenser.useSecondaryOutput = true;
            controller.coldDispenser = coldDispenser;

            var delivery = gameObject.AddOrGet<ManualDeliveryKG>();
            delivery.SetStorage(controller.protoMatterStorage);
            delivery.RequestedItemTag = ProtoMatterRegistration.Tag;
            delivery.capacity = ProtoMatterCapacityKg;
            delivery.refillMass = 1f;

            gameObject.AddOrGet<CopyBuildingSettings>();
            gameObject.AddOrGet<Prioritizable>();
        }

        public override void DoPostConfigureComplete(GameObject gameObject) {
            gameObject.AddOrGet<Operational>();
            gameObject.AddOrGet<ForbiddenTechDevice>();
            gameObject.AddOrGet<LogicOperationalController>();
            gameObject.AddOrGetDef<PoweredActiveController.Def>();
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject gameObject) {
            base.DoPostConfigurePreview(def, gameObject);
            AttachColdPorts(gameObject);
        }

        public override void DoPostConfigureUnderConstruction(GameObject gameObject) {
            base.DoPostConfigureUnderConstruction(gameObject);
            AttachColdPorts(gameObject);
        }

        private static Storage CreateLiquidStorage(GameObject gameObject) {
            var storage = FabricatorSupport.CreateSealedStorage(gameObject,
                LiquidCapacityKg, true);
            storage.allowItemRemoval = true;
            storage.allowUIItemRemoval = true;
            FabricatorSupport.SetStorageFilters(storage, new[] { GameTags.Liquid });
            return storage;
        }

        private static void AttachColdPorts(GameObject gameObject) {
            gameObject.AddOrGet<ConduitSecondaryInput>().portInfo = ColdInputPort;
            gameObject.AddOrGet<ConduitSecondaryOutput>().portInfo = ColdOutputPort;
        }
    }
}
