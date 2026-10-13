using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Options;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Registration {
    [HarmonyPatch(typeof(Database.Techs), "Load")]
    internal static class ForbiddenResearchRegistration {
        private static void Prefix(Database.Techs __instance, TextAsset tree_file) {
            if (__instance == null || tree_file == null) {
                return;
            }

            var tree = new ResourceTreeLoader<ResourceTreeNode>(tree_file);
            Register(__instance, tree);
            RegisterEnergy(__instance, tree);
        }

        private static void Register(Database.Techs __instance,
                ResourceTreeLoader<ResourceTreeNode> tree) {
            if (__instance == null || tree == null ||
                    __instance.TryGet(ModIdentity.ResearchId) != null) {
                return;
            }

            var plan = RegistrationPolicy.Create(ForbiddenTechOptions.Current);
            if (!plan.HasAnyBuildings) {
                return;
            }

            var prerequisite = __instance.TryGet("MatterDeconstruction");
            if (prerequisite == null) {
                prerequisite = __instance.TryGet("HighTempForging");
                Debug.LogWarning("[ForbiddenTechnologyPack] MatterDeconstruction tech was not found; falling back to HighTempForging.");
            }
            if (prerequisite == null) {
                Debug.LogError("[ForbiddenTechnologyPack] Could not find a valid prerequisite for forbidden matter engineering.");
                return;
            }

            var node = CreateNode(tree, prerequisite.Id);
            if (node == null) {
                Debug.LogError("[ForbiddenTechnologyPack] Could not create a research node beside the selected prerequisite.");
                return;
            }

            var costs = new Dictionary<string, float> {
                { "basic", 120f },
                { "advanced", 80f }
            };
            var tech = new Tech(ModIdentity.ResearchId,
                plan.BuildingIds.Where(id => id != ModIdentity.AnnihilationReactorId && id != ModIdentity.EnergyCondenserId).ToList(), __instance, costs);
            tech.costsByResearchTypeID.Clear();
            foreach (var cost in costs) {
                tech.costsByResearchTypeID[cost.Key] = cost.Value;
            }

            tech.SetNode(node, string.Empty);
            tech.requiredTech.Add(prerequisite);
            prerequisite.unlockedTech.Add(tech);
            tech.AddSearchTerms(global::STRINGS.RESEARCH.TECHS.BAIYEFORBIDDENMATTERENGINEERING.SEARCH_TERMS);
        }

        private static void Postfix(Database.Techs __instance) {
            ConnectTech(__instance, ModIdentity.ResearchId);
            ConnectTech(__instance, ModIdentity.MassEnergyResearchId);
        }
        private static void ConnectTech(Database.Techs __instance, string id) {
            var tech = __instance == null ? null : __instance.TryGet(id);
            var techNode = GetNode(tech);
            if (techNode == null) {
                return;
            }

            var prerequisite = id == ModIdentity.MassEnergyResearchId ? __instance.TryGet(ModIdentity.ResearchId) :
                __instance.TryGet("MatterDeconstruction") ?? __instance.TryGet("HighTempForging");
            var prerequisiteNode = GetNode(prerequisite);
            if (prerequisiteNode == null) {
                return;
            }

            tech.SetNode(techNode, prerequisite.category);
            if (!tech.requiredTech.Contains(prerequisite)) {
                tech.requiredTech.Add(prerequisite);
            }
            if (!prerequisite.unlockedTech.Contains(tech)) {
                prerequisite.unlockedTech.Add(tech);
            }
            if (!prerequisiteNode.references.Contains(techNode)) {
                prerequisiteNode.references.Add(techNode);
            }
            if (!prerequisiteNode.edges.Exists(edge => edge.target == techNode)) {
                prerequisiteNode.edges.Add(new ResourceTreeNode.Edge(prerequisiteNode, techNode,
                    ResourceTreeNode.Edge.EdgeType.BezierEdge));
            }
        }

        private static ResourceTreeNode GetNode(Tech tech) {
            return tech == null ? null : Traverse.Create(tech).Field<ResourceTreeNode>("node").Value;
        }

        private static ResourceTreeNode CreateNode(ResourceTreeLoader<ResourceTreeNode> tree,
                string prerequisiteId) {
            ResourceTreeNode prerequisiteNode = null;
            var rightEdge = float.MinValue;
            var occupied = new HashSet<string>();
            foreach (var existingNode in tree) {
                if (existingNode == null) {
                    continue;
                }

                rightEdge = System.Math.Max(rightEdge, existingNode.nodeX + existingNode.width);
                occupied.Add(existingNode.nodeX + ":" + existingNode.nodeY);
                if (existingNode.Id == prerequisiteId) {
                    prerequisiteNode = existingNode;
                }
            }

            if (prerequisiteNode == null) {
                return null;
            }

            var candidateX = rightEdge + prerequisiteNode.width;
            while (occupied.Contains(candidateX + ":" + prerequisiteNode.nodeY)) {
                candidateX += prerequisiteNode.width;
            }

            return new ResourceTreeNode {
                Id = ModIdentity.ResearchId,
                Name = ModIdentity.ResearchId,
                nodeX = candidateX,
                nodeY = prerequisiteNode.nodeY,
                width = prerequisiteNode.width,
                height = prerequisiteNode.height
            };
        }

        private static void RegisterEnergy(Database.Techs techs, ResourceTreeLoader<ResourceTreeNode> tree) {
            if (techs.TryGet(ModIdentity.MassEnergyResearchId) != null) return;
            var ids = RegistrationPolicy.Create(ForbiddenTechOptions.Current).BuildingIds.Where(
                id => id == ModIdentity.AnnihilationReactorId || id == ModIdentity.EnergyCondenserId).ToList();
            if (ids.Count == 0) return;
            var prerequisite = techs.TryGet(ModIdentity.ResearchId);
            var prerequisiteNode = GetNode(prerequisite);
            if (prerequisite == null || prerequisiteNode == null) return;
            var node = new ResourceTreeNode {
                Id = ModIdentity.MassEnergyResearchId, Name = ModIdentity.MassEnergyResearchId,
                nodeX = prerequisiteNode.nodeX + 2f * prerequisiteNode.width, nodeY = prerequisiteNode.nodeY,
                width = prerequisiteNode.width, height = prerequisiteNode.height
            };
            var costs = new Dictionary<string, float> { { "basic", 180f }, { "advanced", 160f } };
            var tech = new Tech(ModIdentity.MassEnergyResearchId, ids, techs, costs);
            tech.costsByResearchTypeID.Clear();
            foreach (var cost in costs) tech.costsByResearchTypeID[cost.Key] = cost.Value;
            tech.SetNode(node, string.Empty); tech.requiredTech.Add(prerequisite); prerequisite.unlockedTech.Add(tech);
            tech.AddSearchTerms(STRINGS.RESEARCH.TECHS.BAIYEFORBIDDENMASSENERGYENGINEERING.SEARCH_TERMS);
        }
    }
}
