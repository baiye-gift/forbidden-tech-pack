using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Options;
using ForbiddenTechnologyPack.Game.Recipes;
using UnityEngine;
using TUNING;

namespace ForbiddenTechnologyPack.Game.Buildings.Reconstructor {
    public sealed class BaiyeMatterReconstructorConfig : IBuildingConfig {
        private const float InputCapacityKg = 2200f;
        private const float OutputCapacityKg = 1100f;

        public override BuildingDef CreateBuildingDef() {
            var buildingDef = BuildingTemplates.CreateBuildingDef(ModIdentity.MatterReconstructorId,
                4, 4, "baiye_matter_reconstructor_kanim", 30, 180f,
                new[] { 600f, 200f, 100f }, new[] { "RefinedMetal", "Ceramic", "Glass" },
                1600f, BuildLocationRule.OnFloor, BUILDINGS.DECOR.PENALTY.TIER1,
                NOISE_POLLUTION.NOISY.TIER4, 0.2f);
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive =
                4800f * ForbiddenTechOptions.Current.PowerMultiplier;
            buildingDef.SelfHeatKilowattsWhenActive =
                24f * ForbiddenTechOptions.Current.HeatMultiplier;
            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.PowerInputOffset = new CellOffset(0, 0);
            buildingDef.InputConduitType = ConduitType.Solid;
            buildingDef.OutputConduitType = ConduitType.Solid;
            buildingDef.UtilityInputOffset = new CellOffset(-1, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(2, 0);
            buildingDef.LogicInputPorts = new List<LogicPorts.Port> {
                LogicPorts.Port.InputPort(LogicOperationalController.PORT_ID, new CellOffset(0, 0),
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERRECONSTRUCTOR.LOGIC_PORT.NAME,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERRECONSTRUCTOR.LOGIC_PORT.ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERRECONSTRUCTOR.LOGIC_PORT.INACTIVE,
                    true, false)
            };
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject gameObject, Tag prefabTag) {
            gameObject.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            var fabricator = gameObject.AddOrGet<MatterReconstructor>();
            fabricator.heatedTemperature = 293.15f;
            fabricator.sideScreenStyle = ComplexFabricatorSideScreen.StyleSetting.ListQueueHybrid;
            fabricator.storeProduced = true;
            fabricator.inStorage = FabricatorSupport.CreateSealedStorage(gameObject,
                InputCapacityKg, true);
            fabricator.buildStorage = FabricatorSupport.CreateSealedStorage(gameObject,
                InputCapacityKg, false);
            fabricator.outStorage = FabricatorSupport.CreateSealedStorage(gameObject,
                OutputCapacityKg, true);
            fabricator.outStorage.allowItemRemoval = true;
            fabricator.outStorage.allowUIItemRemoval = true;

            var inputFilters = new[] {
                ReconstructionSubstrateTags.Common,
                ReconstructionSubstrateTags.OreOrOrganic,
                ReconstructionSubstrateTags.Industrial,
                ReconstructionSubstrateTags.Rare,
                ProtoMatterRegistration.Tag
            };
            FabricatorSupport.SetStorageFilters(fabricator.inStorage, inputFilters);
            FabricatorSupport.SetStorageFilters(fabricator.buildStorage, inputFilters);
            FabricatorSupport.SetStorageFilters(fabricator.outStorage,
                RecipeRegistry.ReconstructorRecipes.Keys.Select(id => new Tag(id)));

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
            dispenser.elementFilter = CurrentProductHashes();

            gameObject.AddOrGet<FabricatorIngredientStatusManager>();
            gameObject.AddOrGet<CopyBuildingSettings>();
            gameObject.AddOrGet<Prioritizable>();
            gameObject.AddOrGet<ForbiddenTechDevice>();
        }

        public override void DoPostConfigureComplete(GameObject gameObject) {
            gameObject.AddOrGet<ComplexFabricatorWorkable>();
            gameObject.AddOrGet<Operational>();
            gameObject.AddOrGet<LogicOperationalController>();
            gameObject.AddOrGetDef<PoweredActiveController.Def>();
        }

        private static SimHashes[] CurrentProductHashes() {
            return RecipeRegistry.ReconstructorRecipes.Keys
                .Select(id => (SimHashes)global::Hash.SDBMLower(id)).ToArray();
        }
    }
}
