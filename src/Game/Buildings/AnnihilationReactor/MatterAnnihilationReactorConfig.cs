using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Options;
using UnityEngine;
using TUNING;

namespace ForbiddenTechnologyPack.Game.Buildings.AnnihilationReactor {
    public sealed class BaiyeMatterAnnihilationReactorConfig : IBuildingConfig {
        private const float CoolantCapacityKg = 10f;
        private const float ProtoMatterCapacityKg = 50f;

        public override BuildingDef CreateBuildingDef() {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                ModIdentity.MatterAnnihilationReactorId, 7, 6,
                "baiye_matter_annihilation_reactor_kanim", 100, 300f,
                new[] { 1200f, 800f, 400f, 200f },
                new[] { "RefinedMetal", "Steel", "Ceramic", "Glass" },
                2400f, BuildLocationRule.OnFloor, BUILDINGS.DECOR.PENALTY.TIER3,
                NOISE_POLLUTION.NOISY.TIER6, 0.2f);

            // Constraint power and generated power deliberately use separate connectors.
            // This preserves the no-black-start rule even after the reactor reaches Stable.
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive =
                12000f * ForbiddenTechOptions.Current.PowerMultiplier;
            buildingDef.PowerInputOffset = new CellOffset(-2, 0);

            buildingDef.RequiresPowerOutput = true;
            buildingDef.GeneratorWattageRating = 40000f;
            buildingDef.GeneratorBaseCapacity = 80000f;
            buildingDef.PowerOutputOffset = new CellOffset(2, 0);

            buildingDef.SelfHeatKilowattsWhenActive = 0f;
            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.OverheatTemperature = 523.15f;
            buildingDef.ViewMode = OverlayModes.Power.ID;

            buildingDef.InputConduitType = ConduitType.Liquid;
            buildingDef.OutputConduitType = ConduitType.Liquid;
            buildingDef.UtilityInputOffset = new CellOffset(-1, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(1, 0);

            buildingDef.LogicInputPorts = new List<LogicPorts.Port> {
                LogicPorts.Port.InputPort(LogicOperationalController.PORT_ID,
                    new CellOffset(0, 0),
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANNIHILATIONREACTOR.LOGIC_PORT.NAME,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANNIHILATIONREACTOR.LOGIC_PORT.ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANNIHILATIONREACTOR.LOGIC_PORT.INACTIVE,
                    true, false)
            };
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject gameObject, Tag prefabTag) {
            gameObject.GetComponent<KPrefabID>().AddTag(
                RoomConstraints.ConstraintTags.IndustrialMachinery);

            var controller = gameObject.AddOrGet<MatterAnnihilationReactorController>();
            controller.coolantStorage = FabricatorSupport.CreateSealedStorage(
                gameObject, CoolantCapacityKg, true);
            controller.coolantStorage.allowItemRemoval = true;
            controller.coolantStorage.allowUIItemRemoval = true;
            FabricatorSupport.SetStorageFilters(controller.coolantStorage,
                new[] { GameTags.Liquid });

            controller.protoMatterStorage = FabricatorSupport.CreateSealedStorage(
                gameObject, ProtoMatterCapacityKg, true);
            FabricatorSupport.SetStorageFilters(controller.protoMatterStorage,
                new[] { ProtoMatterRegistration.Tag });

            var coolantConsumer = gameObject.AddComponent<ConduitConsumer>();
            coolantConsumer.conduitType = ConduitType.Liquid;
            coolantConsumer.capacityTag = GameTags.Liquid;
            coolantConsumer.capacityKG = CoolantCapacityKg;
            coolantConsumer.consumptionRate = CoolantCapacityKg;
            coolantConsumer.storage = controller.coolantStorage;
            coolantConsumer.alwaysConsume = true;
            coolantConsumer.forceAlwaysSatisfied = true;
            coolantConsumer.useSecondaryInput = false;
            controller.coolantConsumer = coolantConsumer;

            var coolantDispenser = gameObject.AddComponent<ConduitDispenser>();
            coolantDispenser.conduitType = ConduitType.Liquid;
            coolantDispenser.storage = controller.coolantStorage;
            coolantDispenser.alwaysDispense = true;
            coolantDispenser.isOn = false;
            coolantDispenser.useSecondaryOutput = false;
            controller.coolantDispenser = coolantDispenser;

            var delivery = gameObject.AddOrGet<ManualDeliveryKG>();
            delivery.SetStorage(controller.protoMatterStorage);
            delivery.RequestedItemTag = ProtoMatterRegistration.Tag;
            delivery.capacity = ProtoMatterCapacityKg;
            delivery.refillMass = 5f;

            var generator = gameObject.AddOrGet<Generator>();
            generator.powerDistributionOrder = 8;
            generator.showConnectedConsumerStatusItems = false;
            controller.generator = generator;

            gameObject.AddOrGet<CopyBuildingSettings>();
            gameObject.AddOrGet<Prioritizable>();
        }

        public override void DoPostConfigureComplete(GameObject gameObject) {
            gameObject.AddOrGet<Operational>();
            gameObject.AddOrGet<ForbiddenTechDevice>();
            gameObject.AddOrGet<LogicOperationalController>();
            gameObject.AddOrGetDef<PoweredActiveController.Def>();
        }
    }
}
