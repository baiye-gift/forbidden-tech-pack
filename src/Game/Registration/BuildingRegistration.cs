using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Options;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Registration {
    internal static class BuildingRegistration {
        // This list is intentionally limited to BuildingConfig implementations that exist in
        // the current assembly. Planned Phase-2 IDs live in RegistrationPlan separately so a
        // future building cannot break startup before its config class is implemented.
        private static readonly string[] AllBuildingIds = {
            ModIdentity.MatterAnalyzerId,
            ModIdentity.MassCrusherId,
            ModIdentity.MatterCompilerId,
            ModIdentity.MatterReconstructorId,
            ModIdentity.EntropyFluxDiverterId,
            ModIdentity.MatterAnnihilationReactorId
        };

        internal static void Register() {
            var plan = RegistrationPolicy.Create(ForbiddenTechOptions.Current);
            var enabled = new HashSet<string>(plan.BuildingIds, StringComparer.Ordinal);
            for (var index = 0; index < AllBuildingIds.Length; index++) {
                var buildingId = AllBuildingIds[index];
                var buildingDef = Assets.GetBuildingDef(buildingId);
                if (buildingDef != null) {
                    buildingDef.ShowInBuildMenu = enabled.Contains(buildingId);
                }
                if (enabled.Contains(buildingId)) {
                    ModUtil.AddBuildingToPlanScreen(GetPlanCategory(buildingId), buildingId);
                }
            }
        }

        private static string GetPlanCategory(string buildingId) {
            if (string.Equals(buildingId, ModIdentity.MatterAnnihilationReactorId,
                    StringComparison.Ordinal)) {
                return "Power";
            }
            return "Refining";
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    internal static class GeneratedBuildingsRegistrationPatch {
        private static void Prefix() {
            BuildingRegistration.Register();
        }
    }
}
