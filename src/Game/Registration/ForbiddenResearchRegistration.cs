using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Options;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Registration {
    [HarmonyPatch(typeof(Database.Techs), "Load")]
    internal static class ForbiddenResearchRegistration {
        private static readonly string[] ImplementedPhase2BuildingIds = {
            ModIdentity.MatterReconstructorId,
            ModIdentity.EntropyFluxDiverterId
        };

        private static void Prefix(Database.Techs __instance, TextAsset tree_file) {
            if (__instance == null || tree_file == null) {
                return;
            }

            Register(__instance, new ResourceTreeLoader<ResourceTreeNode>(tree_file));
        }

        private static void Register(Database.Techs __instance,
                ResourceTreeLoader<ResourceTreeNode> tree) {
            if (__instance == null || tree == null) {
                return;
            }

            var plan = RegistrationPolicy.Create(ForbiddenTechOptions.Current);
            if (!plan.HasAnyBuildings) {
                return;
            }

            var phase1 = __instance.TryGet(ModIdentity.ResearchId);
            if (phase1 == null) {
                phase1 = RegisterPhase1(__instance, tree, plan);
            }
            if (phase1 == null) {
                return;
            }

            RegisterPhase2(__instance, tree, phase1, plan);
        }

        private static Tech RegisterPhase1(Database.Techs techs,
                ResourceTreeLoader<ResourceTreeNode> tree, RegistrationPlan plan) {
            var prerequisite = techs.TryGet("MatterDeconstruction");
            if (prerequisite == null) {
                prerequisite = techs.TryGet("HighTempForging");
                Debug.LogWarning("[ForbiddenTechnologyPack] MatterDeconstruction tech was not found; falling back to HighTempForging.");
            }
            if (prerequisite == null) {
                Debug.LogError("[ForbiddenTechnologyPack] Could not find a valid prerequisite for forbidden matter engineering.");
                return null;
            }

            var node = CreateNode(tree, prerequisite.Id);
            if (node == null) {
                Debug.LogError("[ForbiddenTechnologyPack] Could not create a research node beside the selected prerequisite.");
                return null;
            }

            var costs = new Dictionary<string, float> {
                { "basic", 120f },
                { "advanced", 80f }
            };
            var tech = new Tech(ModIdentity.ResearchId,
                new List<string>(plan.Phase1BuildingIds), techs, costs);
            tech.costsByResearchTypeID.Clear();
            foreach (var cost in costs) {
                tech.costsByResearchTypeID[cost.Key] = cost.Value;
            }

            tech.SetNode(node, string.Empty);
            tech.requiredTech.Add(prerequisite);
            prerequisite.unlockedTech.Add(tech);
            tech.AddSearchTerms(global::STRINGS.RESEARCH.TECHS.BAIYEFORBIDDENMATTERENGINEERING.SEARCH_TERMS);
            return tech;
        }

        private static void RegisterPhase2(Database.Techs techs,
                ResourceTreeLoader<ResourceTreeNode> tree, Tech phase1,
                RegistrationPlan plan) {
            if (techs.TryGet(ModIdentity.ProtoFieldResearchId) != null ||
                    !ForbiddenTechOptions.Current.ModuleEnabled) {
                return;
            }

            var unlocked = BuildImplementedPhase2Unlocks(plan);
            if (unlocked.Count == 0) {
                return;
            }

            var node = CreateDependentNode(tree, phase1, ModIdentity.ProtoFieldResearchId);
            if (node == null) {
                Debug.LogError("[ForbiddenTechnologyPack] Could not create the Proto-Matter Field Engineering research node.");
                return;
            }

            var costs = new Dictionary<string, float> {
                { "basic", 160f },
                { "advanced", 120f },
                { "nuclear", 40f }
            };
            var tech = new Tech(ModIdentity.ProtoFieldResearchId, unlocked, techs, costs);
            tech.costsByResearchTypeID.Clear();
            foreach (var cost in costs) {
                tech.costsByResearchTypeID[cost.Key] = cost.Value;
            }

            tech.SetNode(node, string.Empty);
            tech.requiredTech.Add(phase1);
            phase1.unlockedTech.Add(tech);
            tech.AddSearchTerms(global::STRINGS.RESEARCH.TECHS.BAIYEFORBIDDENPROTOFIELDENGINEERING.SEARCH_TERMS);
        }

        private static List<string> BuildImplementedPhase2Unlocks(RegistrationPlan plan) {
            var enabled = new HashSet<string>(plan.Phase2BuildingIds, StringComparer.Ordinal);
            var result = new List<string>();
            for (var index = 0; index < ImplementedPhase2BuildingIds.Length; index++) {
                var id = ImplementedPhase2BuildingIds[index];
                if (enabled.Contains(id)) {
                    result.Add(id);
                }
            }
            return result;
        }

        private static void Postfix(Database.Techs __instance) {
            if (__instance == null) {
                return;
            }

            var phase1 = __instance.TryGet(ModIdentity.ResearchId);
            var prerequisite = __instance.TryGet("MatterDeconstruction") ??
                __instance.TryGet("HighTempForging");
            EnsureVisibleLink(phase1, prerequisite);

            var phase2 = __instance.TryGet(ModIdentity.ProtoFieldResearchId);
            EnsureVisibleLink(phase2, phase1);
        }

        private static void EnsureVisibleLink(Tech tech, Tech prerequisite) {
            var techNode = GetNode(tech);
            var prerequisiteNode = GetNode(prerequisite);
            if (techNode == null || prerequisiteNode == null) {
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

        // Kept as the two-argument helper because the runtime contract probes this exact boundary.
        private static ResourceTreeNode CreateNode(ResourceTreeLoader<ResourceTreeNode> tree,
                string prerequisiteId) {
            return CreateNodeForResearch(tree, prerequisiteId, ModIdentity.ResearchId);
        }

        private static ResourceTreeNode CreateNodeForResearch(
                ResourceTreeLoader<ResourceTreeNode> tree, string prerequisiteId,
                string researchId) {
            ResourceTreeNode prerequisiteNode = null;
            var rightEdge = float.MinValue;
            var occupied = new HashSet<string>();
            foreach (var existingNode in tree) {
                if (existingNode == null) {
                    continue;
                }

                rightEdge = Math.Max(rightEdge, existingNode.nodeX + existingNode.width);
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
                Id = researchId,
                Name = researchId,
                nodeX = candidateX,
                nodeY = prerequisiteNode.nodeY,
                width = prerequisiteNode.width,
                height = prerequisiteNode.height
            };
        }

        private static ResourceTreeNode CreateDependentNode(
                ResourceTreeLoader<ResourceTreeNode> tree, Tech prerequisite,
                string researchId) {
            var prerequisiteNode = GetNode(prerequisite);
            if (tree == null || prerequisiteNode == null) {
                return null;
            }

            var occupied = new HashSet<string>();
            foreach (var existingNode in tree) {
                if (existingNode != null) {
                    occupied.Add(existingNode.nodeX + ":" + existingNode.nodeY);
                }
            }

            var candidateX = prerequisiteNode.nodeX + prerequisiteNode.width;
            while (occupied.Contains(candidateX + ":" + prerequisiteNode.nodeY)) {
                candidateX += prerequisiteNode.width;
            }

            return new ResourceTreeNode {
                Id = researchId,
                Name = researchId,
                nodeX = candidateX,
                nodeY = prerequisiteNode.nodeY,
                width = prerequisiteNode.width,
                height = prerequisiteNode.height
            };
        }
    }
}
