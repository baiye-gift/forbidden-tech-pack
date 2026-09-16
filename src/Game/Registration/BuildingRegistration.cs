using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Options;
using HarmonyLib;

namespace ForbiddenTechnologyPack.Game.Registration {
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    internal static class BuildingRegistration {
        private static readonly string[] AllBuildingIds = {
            ModIdentity.MatterAnalyzerId,
            ModIdentity.MassCrusherId,
            ModIdentity.MatterCompilerId
        };

        private static readonly HashSet<string> AddedMenuEntries =
            new HashSet<string>(StringComparer.Ordinal);

        private static void Postfix() {
            var plan = RegistrationPolicy.Create(ForbiddenTechOptions.Current);
            var enabled = new HashSet<string>(plan.BuildingIds, StringComparer.Ordinal);

            for (var index = 0; index < AllBuildingIds.Length; index++) {
                var buildingDef = Assets.GetBuildingDef(AllBuildingIds[index]);
                if (buildingDef != null) {
                    buildingDef.ShowInBuildMenu = enabled.Contains(AllBuildingIds[index]);
                }
            }

            for (var index = 0; index < plan.BuildingIds.Count; index++) {
                var buildingId = plan.BuildingIds[index];
                if (AddedMenuEntries.Add(buildingId)) {
                    ModUtil.AddBuildingToPlanScreen("Refining", buildingId);
                }
            }
        }
    }
}
