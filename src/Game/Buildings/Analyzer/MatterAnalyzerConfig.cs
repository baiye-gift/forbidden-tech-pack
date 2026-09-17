using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using UnityEngine;
using TUNING;

namespace ForbiddenTechnologyPack.Game.Buildings.Analyzer {
    public sealed class BaiyeMatterAnalyzerConfig : IBuildingConfig {
        public override BuildingDef CreateBuildingDef() {
            var buildingDef = BuildingTemplates.CreateBuildingDef(ModIdentity.MatterAnalyzerId,
                3, 3, "baiye_matter_analyzer_kanim", 30, 120f,
                new[] { 400f, 100f }, new[] { "RefinedMetal", "Glass" },
                1600f, BuildLocationRule.OnFloor, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NONE,
                0.2f);
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = 1200f;
            buildingDef.SelfHeatKilowattsWhenActive = 4f;
            buildingDef.ExhaustKilowattsWhenActive = 0f;
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.PowerInputOffset = new CellOffset(0, 0);
            buildingDef.LogicInputPorts = new List<LogicPorts.Port> {
                LogicPorts.Port.InputPort(LogicOperationalController.PORT_ID, new CellOffset(0, 0),
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANALYZER.LOGIC_PORT.NAME,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANALYZER.LOGIC_PORT.ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.BAIYEMATTERANALYZER.LOGIC_PORT.INACTIVE,
                    true, false)
            };
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject gameObject, Tag prefabTag) {
            var fabricator = gameObject.AddOrGet<MatterAnalyzer>();
            fabricator.heatedTemperature = 293.15f;
            fabricator.sideScreenStyle = ComplexFabricatorSideScreen.StyleSetting.ListQueueHybrid;
            fabricator.storeProduced = true;
            fabricator.inStorage = FabricatorSupport.CreateSealedStorage(gameObject, 100f, true);
            fabricator.buildStorage = FabricatorSupport.CreateSealedStorage(gameObject, 100f, false);
            fabricator.outStorage = FabricatorSupport.CreateSealedStorage(gameObject, 100f, true);
            fabricator.outStorage.allowItemRemoval = true;
            fabricator.outStorage.allowUIItemRemoval = true;

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
