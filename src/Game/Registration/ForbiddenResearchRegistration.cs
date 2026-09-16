using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Options;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Registration {
    [HarmonyPatch(typeof(Database.Techs), "Init")]
    internal static class ForbiddenResearchRegistration {
        private static bool registered;

        private static void Postfix(Database.Techs __instance) {
            if (registered || __instance == null) {
                return;
            }

            var plan = RegistrationPolicy.Create(ForbiddenTechOptions.Current);
            if (!plan.HasAnyBuildings) {
                return;
            }

            var costs = new Dictionary<string, float> {
                { "basic", 120f },
                { "advanced", 80f }
            };
            var tech = new Tech(ModIdentity.ResearchId, plan.BuildingIds, __instance, costs);

            var prerequisiteId = __instance.TryGet("MatterDeconstruction") != null
                ? "MatterDeconstruction"
                : "HighTempForging";
            if (prerequisiteId == "HighTempForging") {
                Debug.LogWarning("[ForbiddenTechnologyPack] MatterDeconstruction tech was not found; falling back to HighTempForging.");
            }

            if (__instance.TryGet(prerequisiteId) != null) {
                __instance.AddPrerequisite(tech, prerequisiteId);
            } else {
                Debug.LogError("[ForbiddenTechnologyPack] Could not find a valid prerequisite for forbidden matter engineering.");
            }

            registered = true;
        }
    }
}
