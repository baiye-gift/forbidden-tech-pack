using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using UnityEngine;
using TUNING;

namespace ForbiddenTechnologyPack.Game.Buildings.MassEnergy {
    public sealed class BaiyeProtoAnnihilationReactorConfig : IBuildingConfig {
        public override BuildingDef CreateBuildingDef() {
            var def = MassEnergyBuildingSupport.Create(ModIdentity.AnnihilationReactorId, 5,
                "baiye_proto_annihilation_reactor_kanim", MassEnergyBalance.OperatingWatts);
            def.RequiresPowerOutput = true; def.GeneratorWattageRating = MassEnergyBalance.GrossWatts;
            def.GeneratorBaseCapacity = MassEnergyBalance.GeneratorCapacityJoules; def.PowerOutputOffset = new CellOffset(0, 0);
            return def;
        }
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefabTag) {
            var prefab = go.GetComponent<KPrefabID>(); prefab.AddTag(RoomConstraints.ConstraintTags.IndustrialMachinery);
            prefab.AddTag(RoomConstraints.ConstraintTags.PowerBuilding); prefab.AddTag(RoomConstraints.ConstraintTags.GeneratorType);
            go.AddOrGet<Generator>().connectedTags = new Tag[0];
            var fuel = FabricatorSupport.CreateSealedStorage(go, 120f, true);
            FabricatorSupport.SetStorageFilters(fuel, new[] { ProtoMatterRegistration.Tag });
            fuel.allowItemRemoval = fuel.allowUIItemRemoval = true;
            var fetch = go.AddOrGet<ManualDeliveryKG>(); fetch.SetStorage(fuel);
            fetch.RequestedItemTag = ProtoMatterRegistration.Tag; fetch.capacity = 120; fetch.refillMass = 30;
            fetch.choreTypeIDHash = Db.Get().ChoreTypes.FetchCritical.IdHash;
            AttachInput(go);
            var rail = go.AddOrGet<SolidConduitConsumer>(); rail.storage = fuel;
            rail.capacityTag = ProtoMatterRegistration.Tag; rail.capacityKG = 120;
            rail.useSecondaryInput = true; rail.alwaysConsume = true;
            MassEnergyBuildingSupport.Cooling(go, MassEnergyBalance.OperatingWatts);
            go.AddOrGet<ProtoAnnihilationReactor>().fuel = fuel;
            if (DlcManager.IsExpansion1Active()) {
                var radiation = go.AddOrGet<RadiationEmitter>();
                radiation.emitType = RadiationEmitter.RadiationEmitterType.Constant;
                radiation.emitRadiusX = 4; radiation.emitRadiusY = 4;
                radiation.radiusProportionalToRads = false; radiation.emitRads = 500;
                radiation.emissionOffset = new Vector3(0, 2, 0);
            }
        }
        public override void DoPostConfigureComplete(GameObject go) { MassEnergyBuildingSupport.Complete(go); }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go) { base.DoPostConfigurePreview(def, go); AttachInput(go); }
        public override void DoPostConfigureUnderConstruction(GameObject go) { base.DoPostConfigureUnderConstruction(go); AttachInput(go); }
        private static void AttachInput(GameObject go) {
            go.AddOrGet<ConduitSecondaryInput>().portInfo = new ConduitPortInfo(ConduitType.Solid, new CellOffset(-2, 0));
        }
    }
}
