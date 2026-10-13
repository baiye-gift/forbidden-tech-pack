using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using UnityEngine;
using TUNING;

namespace ForbiddenTechnologyPack.Game.Buildings.MassEnergy {
    public sealed class BaiyeEnergyMatterCondenserConfig : IBuildingConfig {
        public override BuildingDef CreateBuildingDef() {
            return MassEnergyBuildingSupport.Create(ModIdentity.EnergyCondenserId, 4,
                "baiye_energy_matter_condenser_kanim", MassEnergyBalance.CondenserWatts);
        }
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag) {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            var fabricator = go.AddOrGet<EnergyMatterCondenser>(); fabricator.duplicantOperated = false;
            fabricator.showProgressBar = true; fabricator.heatedTemperature = 293.15f;
            fabricator.sideScreenStyle = ComplexFabricatorSideScreen.StyleSetting.ListQueueHybrid; fabricator.storeProduced = true;
            fabricator.inStorage = FabricatorSupport.CreateSealedStorage(go, 1, false);
            fabricator.buildStorage = FabricatorSupport.CreateSealedStorage(go, 1, false);
            fabricator.outStorage = FabricatorSupport.CreateSealedStorage(go, 20, true);
            FabricatorSupport.SetStorageFilters(fabricator.outStorage, new[] { ProtoMatterRegistration.Tag });
            fabricator.outStorage.allowItemRemoval = fabricator.outStorage.allowUIItemRemoval = true;
            MassEnergyBuildingSupport.Cooling(go, MassEnergyBalance.CondenserHeatWatts);
            AttachOutput(go);
            var rail = go.AddOrGet<SolidConduitDispenser>(); rail.storage = fabricator.outStorage;
            rail.elementFilter = new[] { ProtoMatterRegistration.Hash }; rail.solidOnly = true;
            rail.alwaysDispense = true; rail.useSecondaryOutput = true;
            go.AddOrGet<FabricatorIngredientStatusManager>();
        }
        public override void DoPostConfigureComplete(GameObject go) {
            go.AddOrGet<ComplexFabricatorWorkable>(); MassEnergyBuildingSupport.Complete(go);
        }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go) { base.DoPostConfigurePreview(def, go); AttachOutput(go); }
        public override void DoPostConfigureUnderConstruction(GameObject go) { base.DoPostConfigureUnderConstruction(go); AttachOutput(go); }
        private static void AttachOutput(GameObject go) {
            go.AddOrGet<ConduitSecondaryOutput>().portInfo = new ConduitPortInfo(ConduitType.Solid, new CellOffset(1, 0));
        }
    }
}
