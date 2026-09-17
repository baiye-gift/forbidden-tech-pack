using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Options;
using HarmonyLib;

namespace ForbiddenTechnologyPack.Game.Registration {
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    internal static class BuildingRegistration {
        // Keep this list limited to BuildingConfig implementations that actually exist in the game assembly.
        // Phase-2 IDs may already exist in the core registration plan before their BuildingConfig is implemented.
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

            foreach (var buildingId in AllBuildingIds) {
                var buildingDef = Assets.GetBuildingDef(buildingId);
                if (buildingDef == null) {
                    continue;
                }

                var showInMenu = enabled.Contains(buildingId);
                buildingDef.ShowInBuildMenu = showInMenu;
                if (showInMenu && AddedMenuEntries.Add(buildingId)) {
                    ModUtil.AddBuildingToPlanScreen("Refining", buildingId);
                }
            }
        }
    }
}
