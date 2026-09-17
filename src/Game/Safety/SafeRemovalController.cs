using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Analyzer;
using ForbiddenTechnologyPack.Game.Buildings.AnnihilationReactor;
using ForbiddenTechnologyPack.Game.Buildings.Compiler;
using ForbiddenTechnologyPack.Game.Buildings.Crusher;
using ForbiddenTechnologyPack.Game.Buildings.EntropyDiverter;
using ForbiddenTechnologyPack.Game.Buildings.Reconstructor;
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
            ModIdentity.MatterCompilerId,
            ModIdentity.MatterReconstructorId,
            ModIdentity.EntropyFluxDiverterId,
            ModIdentity.MatterAnnihilationReactorId
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
            ProcessBuildings(FindAllObjects<MatterReconstructor>(), report);
            ProcessEntropyDiverters(FindAllObjects<EntropyFluxDiverterController>(), report);
            ProcessAnnihilationReactors(
                FindAllObjects<MatterAnnihilationReactorController>(), report);
            var remainingProtoMatter = ConvertProtoMatter(report);
            report.Finish(CountRemainingCustomObjects(remainingProtoMatter));
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
                ClearTechUnlocks(db.Techs.TryGet(ModIdentity.ResearchId));
                ClearTechUnlocks(db.Techs.TryGet(ModIdentity.ProtoFieldResearchId));
            }
        }

        internal static bool IsForbiddenFabricator(ComplexFabricator fabricator) {
            return fabricator is MatterAnalyzer || fabricator is MassCrusher ||
                fabricator is MatterCompiler || fabricator is MatterReconstructor;
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

                DestroyBuilding(fabricator, report);
            }
        }

        private static void ProcessEntropyDiverters(EntropyFluxDiverterController[] buildings,
                SafeRemovalReport report) {
            if (buildings == null) {
                return;
            }
            for (var index = 0; index < buildings.Length; index++) {
                var controller = buildings[index];
                if (controller == null) {
                    continue;
                }
                controller.PrepareForSafeRemoval();
                DropStorage(controller.hotStorage, report, true);
                DropStorage(controller.coldStorage, report, true);
                DropStorage(controller.protoMatterStorage, report, true);
                DestroyBuilding(controller, report);
            }
        }

        private static void ProcessAnnihilationReactors(
                MatterAnnihilationReactorController[] buildings,
                SafeRemovalReport report) {
            if (buildings == null) {
                return;
            }
            for (var index = 0; index < buildings.Length; index++) {
                var controller = buildings[index];
                if (controller == null) {
                    continue;
                }
                controller.PrepareForSafeRemoval();
                DropStorage(controller.coolantStorage, report, true);
                DropStorage(controller.protoMatterStorage, report, true);
                DestroyBuilding(controller, report);
            }
        }

        private static void DestroyBuilding(Component component, SafeRemovalReport report) {
            var deconstructable = component == null ? null : component.GetComponent<Deconstructable>();
            if (deconstructable != null && !deconstructable.HasBeenDestroyed) {
                deconstructable.ForceDestroyAndGetMaterials();
                report.RecordRemovedBuilding();
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

        private static int ConvertProtoMatter(SafeRemovalReport report) {
            var remaining = 0;
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
                GameObject replacement;
                try {
                    replacement = ElementLoader.FindElementByHash(SimHashes.IgneousRock)
                        .substance.SpawnResource(position, mass, temperature, diseaseIndex,
                            diseaseCount, prevent_merge: true);
                } catch (System.Exception exception) {
                    remaining++;
                    LogReplacementFailure(primary,
                        "the native resource spawn threw an exception", exception);
                    continue;
                }

                if (replacement == null) {
                    remaining++;
                    LogReplacementFailure(primary,
                        "the native resource spawn returned null", null);
                    continue;
                }

                try {
                    Util.KDestroyGameObject(primary.gameObject);
                } catch (System.Exception exception) {
                    remaining++;
                    LogReplacementFailure(primary,
                        "the original custom object could not be scheduled for destruction",
                        exception);
                    try {
                        Util.KDestroyGameObject(replacement);
                    } catch (System.Exception rollbackException) {
                        Debug.LogError(
                            "[ForbiddenTechnologyPack] Failed to remove the unused Igneous Rock " +
                            "replacement after retaining Proto-Matter: " + rollbackException);
                    }
                    continue;
                }

                report.RecordConvertedObject(mass);
            }

            return remaining;
        }

        private static int CountRemainingCustomObjects(int remainingProtoMatter) {
            var remaining = remainingProtoMatter;
            remaining += CountLiveBuildings(FindAllObjects<MatterAnalyzer>());
            remaining += CountLiveBuildings(FindAllObjects<MassCrusher>());
            remaining += CountLiveBuildings(FindAllObjects<MatterCompiler>());
            remaining += CountLiveBuildings(FindAllObjects<MatterReconstructor>());
            remaining += CountLiveBuildings(FindAllObjects<EntropyFluxDiverterController>());
            remaining += CountLiveBuildings(
                FindAllObjects<MatterAnnihilationReactorController>());
            return remaining;
        }

        private static void ClearTechUnlocks(Tech tech) {
            if (tech != null) {
                tech.unlockedItemIDs.Clear();
            }
        }

        private static void LogReplacementFailure(PrimaryElement primary, string reason,
                System.Exception exception) {
            var objectName = primary == null || primary.gameObject == null
                ? "<unknown>" : primary.gameObject.name;
            var detail = exception == null ? string.Empty : ": " + exception;
            Debug.LogError(
                "[ForbiddenTechnologyPack] Failed to replace Proto-Matter object '" +
                objectName + "' with native Igneous Rock because " + reason +
                ". The original object was retained and will remain in the cleanup count" +
                detail);
        }

        private static T[] FindAllObjects<T>() where T : Object {
            return Object.FindObjectsByType<T>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static int CountLiveBuildings<T>(T[] buildings) where T : Component {
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
