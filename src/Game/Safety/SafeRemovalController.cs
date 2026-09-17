using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Analyzer;
using ForbiddenTechnologyPack.Game.Buildings.Compiler;
using ForbiddenTechnologyPack.Game.Buildings.Crusher;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Save;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Safety {
    public static class SafeRemovalController {
        internal static readonly Operational.Flag SafeRemovalAllowed =
            new Operational.Flag("ForbiddenTechSafeRemovalAllowed", Operational.Flag.Type.Requirement);

        private static readonly string[] ForbiddenBuildingIds = {
            ModIdentity.MatterAnalyzerId,
            ModIdentity.MassCrusherId,
            ModIdentity.MatterCompilerId
        };

        public static SafeRemovalReport Execute() {
            var report = new SafeRemovalReport();
            var saveData = ForbiddenTechSaveData.Instance;
            if (saveData == null || global::Game.Instance == null) {
                report.Finish(1);
                return report;
            }

            saveData.BeginSafeRemoval();

            ProcessBuildings(FindAllObjects<MatterAnalyzer>(), report);
            ProcessBuildings(FindAllObjects<MassCrusher>(), report);
            ProcessBuildings(FindAllObjects<MatterCompiler>(), report);
            ConvertProtoMatter(report);
            report.Finish(CountRemainingCustomObjects());
            saveData.SetSafeRemovalCompleted(report.IsComplete);
            if (report.IsComplete) {
                ApplyCompletedVisibility();
            }
            return report;
        }

        public static void ApplyCompletedVisibility() {
            for (var index = 0; index < ForbiddenBuildingIds.Length; index++) {
                var buildingDef = Assets.GetBuildingDef(ForbiddenBuildingIds[index]);
                if (buildingDef != null) {
                    buildingDef.ShowInBuildMenu = false;
                }
            }

            var db = Db.Get();
            if (db != null && db.Techs != null) {
                var tech = db.Techs.TryGet(ModIdentity.ResearchId);
                if (tech != null) {
                    tech.unlockedItemIDs.Clear();
                }
            }
        }

        internal static bool IsForbiddenFabricator(ComplexFabricator fabricator) {
            return fabricator is MatterAnalyzer || fabricator is MassCrusher ||
                fabricator is MatterCompiler;
        }

        internal static void Disable(ComplexFabricator fabricator) {
            if (fabricator == null) {
                return;
            }
            var operational = fabricator.GetComponent<Operational>();
            if (operational != null) {
                operational.SetFlag(SafeRemovalAllowed, false);
                fabricator.SetQueueDirty();
            }
        }

        private static void ProcessBuildings<T>(T[] buildings, SafeRemovalReport report)
                where T : ComplexFabricator {
            if (buildings == null) {
                return;
            }

            for (var index = 0; index < buildings.Length; index++) {
                var fabricator = buildings[index];
                if (fabricator == null) {
                    continue;
                }

                Disable(fabricator);
                DropStorage(fabricator.inStorage, report, true);
                DropStorage(fabricator.buildStorage, report, true);
                DropStorage(fabricator.outStorage, report, false);

                var compiler = fabricator as MatterCompiler;
                if (compiler != null) {
                    var coolant = compiler.GetComponent<CoolantController>();
                    DropStorage(coolant == null ? null : coolant.storage, report, true);
                }

                var deconstructable = fabricator.GetComponent<Deconstructable>();
                if (deconstructable != null && !deconstructable.HasBeenDestroyed) {
                    deconstructable.ForceDestroyAndGetMaterials();
                    report.RecordRemovedBuilding();
                }
            }
        }

        private static void DropStorage(Storage storage, SafeRemovalReport report,
                bool countAsReturnedInput) {
            if (storage == null) {
                return;
            }

            if (countAsReturnedInput) {
                var returned = 0;
                for (var index = 0; index < storage.items.Count; index++) {
                    var item = storage.items[index];
                    var primary = item == null ? null : item.GetComponent<PrimaryElement>();
                    if (primary != null && primary.ElementID != ProtoMatterRegistration.Hash) {
                        returned++;
                    }
                }
                report.RecordReturnedInputs(returned);
            }

            storage.DropAll(true, true, Vector3.zero, true, null);
        }

        private static void ConvertProtoMatter(SafeRemovalReport report) {
            var elements = FindAllObjects<PrimaryElement>();
            for (var index = 0; index < elements.Length; index++) {
                var primary = elements[index];
                if (primary == null || primary.ElementID != ProtoMatterRegistration.Hash) {
                    continue;
                }

                var position = primary.transform.GetPosition();
                var mass = primary.Mass;
                var temperature = primary.Temperature;
                var diseaseIndex = primary.DiseaseIdx;
                var diseaseCount = primary.DiseaseCount;
                ElementLoader.FindElementByHash(SimHashes.IgneousRock).substance.SpawnResource(
                    position, mass, temperature, diseaseIndex, diseaseCount, prevent_merge: true);
                Util.KDestroyGameObject(primary.gameObject);
                report.RecordConvertedObject(mass);
            }
        }

        private static int CountRemainingCustomObjects() {
            var remaining = 0;
            var elements = FindAllObjects<PrimaryElement>();
            for (var index = 0; index < elements.Length; index++) {
                if (elements[index] != null &&
                        elements[index].ElementID == ProtoMatterRegistration.Hash) {
                    remaining++;
                }
            }

            remaining += CountLiveBuildings(FindAllObjects<MatterAnalyzer>());
            remaining += CountLiveBuildings(FindAllObjects<MassCrusher>());
            remaining += CountLiveBuildings(FindAllObjects<MatterCompiler>());
            return remaining;
        }

        private static T[] FindAllObjects<T>() where T : Object {
            return Object.FindObjectsByType<T>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static int CountLiveBuildings<T>(T[] buildings) where T : ComplexFabricator {
            var count = 0;
            if (buildings == null) {
                return count;
            }
            for (var index = 0; index < buildings.Length; index++) {
                var building = buildings[index];
                if (building == null) {
                    continue;
                }
                var deconstructable = building.GetComponent<Deconstructable>();
                if (deconstructable == null || !deconstructable.HasBeenDestroyed) {
                    count++;
                }
            }
            return count;
        }
    }

    [HarmonyPatch(typeof(ComplexFabricator), "OnSpawn")]
    internal static class SafeRemovalFabricatorSpawnPatch {
        private static void Postfix(ComplexFabricator __instance) {
            if (!SafeRemovalController.IsForbiddenFabricator(__instance)) {
                return;
            }

            var saveData = ForbiddenTechSaveData.Instance;
            var allowed = saveData == null || !saveData.SafeRemovalStarted;
            var operational = __instance.GetComponent<Operational>();
            if (operational != null) {
                operational.SetFlag(SafeRemovalController.SafeRemovalAllowed, allowed);
                if (!allowed) {
                    __instance.SetQueueDirty();
                }
            }
        }
    }
}
