using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Options;
using ForbiddenTechnologyPack.Game.Recipes;
using UnityEngine;
using TUNING;

namespace ForbiddenTechnologyPack.Game.Buildings.Compiler {
    public sealed class BaiyeMatterCompilerConfig : IBuildingConfig {
        private const float IngredientCapacityKg = 12000f;
        private const float ProductCapacityKg = 120f;
        private const float CoolantCapacityKg = 10f;
        private const float BaseHeatJoulesPerSecond = 160000f;

        private static readonly ConduitPortInfo SolidInputPort =
            new ConduitPortInfo(ConduitType.Solid, new CellOffset(-2, 0));
        private static readonly ConduitPortInfo SolidOutputPort =
            new ConduitPortInfo(ConduitType.Solid, new CellOffset(2, 0));

        public override BuildingDef CreateBuildingDef() {
            var buildingDef = BuildingTemplates.CreateBuildingDef(ModIdentity.MatterCompilerId,
                5, 5, "baiye_matter_compiler_kanim", 60, 240f,
                new[] { 800f, 400f, 200f }, new[] { "RefinedMetal", "Ceramic", "Glass" },
                2400f, BuildLocationRule.OnFloor, BUILDINGS.DECOR.PENALTY.TIER2,
                NOISE_POLLUTION.NOISY.TIER6, 0.2f);
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive =
                9600f * ForbiddenTechOptions.Current.PowerMultiplier;
            // The controller transfers the full process heat to coolant instead of emitting it twice.
            buildingDef.SelfHeatKilowattsWhenActive = 0f;
            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.PowerInputOffset = new CellOffset(0, 0);
            buildingDef.InputConduitType = ConduitType.Liquid;
            buildingDef.OutputConduitType = ConduitType.Liquid;
            buildingDef.UtilityInputOffset = new CellOffset(-1, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(1, 0);
            buildingDef.LogicInputPorts = new List<LogicPorts.Port> {
                LogicPorts.Port.InputPort(LogicOperationalController.PORT_ID, new CellOffset(0, 0),
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERCOMPILER.LOGIC_PORT.NAME,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERCOMPILER.LOGIC_PORT.ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERCOMPILER.LOGIC_PORT.INACTIVE,
                    true, false)
            };
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject gameObject, Tag prefabTag) {
            gameObject.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            var fabricator = gameObject.AddOrGet<MatterCompiler>();
            fabricator.heatedTemperature = 293.15f;
            fabricator.sideScreenStyle = ComplexFabricatorSideScreen.StyleSetting.ListQueueHybrid;
            fabricator.storeProduced = true;
            fabricator.inStorage = FabricatorSupport.CreateSealedStorage(gameObject,
                IngredientCapacityKg, true);
            fabricator.buildStorage = FabricatorSupport.CreateSealedStorage(gameObject,
                IngredientCapacityKg, false);
            fabricator.outStorage = FabricatorSupport.CreateSealedStorage(gameObject,
                ProductCapacityKg, true);
            fabricator.outStorage.allowItemRemoval = true;
            fabricator.outStorage.allowUIItemRemoval = true;
            FabricatorSupport.SetStorageFilters(fabricator.inStorage,
                new[] { ProtoMatterRegistration.Tag });
            FabricatorSupport.SetStorageFilters(fabricator.buildStorage,
                new[] { ProtoMatterRegistration.Tag });

            var coolantStorage = FabricatorSupport.CreateSealedStorage(gameObject,
                CoolantCapacityKg, true);
            coolantStorage.allowItemRemoval = true;
            coolantStorage.allowUIItemRemoval = true;
            FabricatorSupport.SetStorageFilters(coolantStorage, new[] { GameTags.Liquid });

            AttachSolidPorts(gameObject);

            var solidConsumer = gameObject.AddOrGet<SolidConduitConsumer>();
            solidConsumer.storage = fabricator.inStorage;
            solidConsumer.capacityTag = ProtoMatterRegistration.Tag;
            solidConsumer.capacityKG = IngredientCapacityKg;
            solidConsumer.alwaysConsume = true;
            solidConsumer.useSecondaryInput = true;

            var solidDispenser = gameObject.AddOrGet<SolidConduitDispenser>();
            solidDispenser.storage = fabricator.outStorage;
            solidDispenser.alwaysDispense = true;
            solidDispenser.useSecondaryOutput = true;
            solidDispenser.solidOnly = true;
            solidDispenser.elementFilter = CurrentProductHashes();

            var coolantConsumer = gameObject.AddOrGet<ConduitConsumer>();
            coolantConsumer.conduitType = ConduitType.Liquid;
            coolantConsumer.capacityTag = GameTags.Liquid;
            coolantConsumer.capacityKG = CoolantCapacityKg;
            coolantConsumer.consumptionRate = CoolantCapacityKg;
            coolantConsumer.storage = coolantStorage;
            coolantConsumer.alwaysConsume = true;
            coolantConsumer.forceAlwaysSatisfied = true;

            var coolantDispenser = gameObject.AddOrGet<ConduitDispenser>();
            coolantDispenser.conduitType = ConduitType.Liquid;
            coolantDispenser.storage = coolantStorage;
            coolantDispenser.elementFilter = null;
            coolantDispenser.alwaysDispense = true;
            coolantDispenser.isOn = false;

            var coolantController = gameObject.AddOrGet<CoolantController>();
            coolantController.storage = coolantStorage;
            coolantController.heatJoulesPerSecond =
                BaseHeatJoulesPerSecond * ForbiddenTechOptions.Current.HeatMultiplier;

            gameObject.AddOrGet<FabricatorIngredientStatusManager>();
            gameObject.AddOrGet<CopyBuildingSettings>();
            gameObject.AddOrGet<Prioritizable>();
        }

        public override void DoPostConfigureComplete(GameObject gameObject) {
            gameObject.AddOrGet<ComplexFabricatorWorkable>();
            gameObject.AddOrGet<Operational>();
            gameObject.AddOrGet<LogicOperationalController>();
            gameObject.AddOrGetDef<PoweredActiveController.Def>();
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject gameObject) {
            base.DoPostConfigurePreview(def, gameObject);
            AttachSolidPorts(gameObject);
        }

        public override void DoPostConfigureUnderConstruction(GameObject gameObject) {
            base.DoPostConfigureUnderConstruction(gameObject);
            AttachSolidPorts(gameObject);
        }

        private static void AttachSolidPorts(GameObject gameObject) {
            gameObject.AddOrGet<ConduitSecondaryInput>().portInfo = SolidInputPort;
            gameObject.AddOrGet<ConduitSecondaryOutput>().portInfo = SolidOutputPort;
        }

        private static SimHashes[] CurrentProductHashes() {
            return RecipeRegistry.CompilerRecipes.Keys
                .Select(id => (SimHashes)global::Hash.SDBMLower(id)).ToArray();
        }
    }
}
