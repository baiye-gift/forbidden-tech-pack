using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Options;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Elements {
    public static class ElementCatalogAdapter {
        private static readonly ISet<string> EndgameIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "TempConductorSolid", "Tungsten", "SuperInsulator", "Isoresin"
        };
        private static readonly ISet<string> RareIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Diamond", "EnrichedUranium", "Radium", "UraniumOre", "Iridium", "Niobium", "Fullerene"
        };
        private static readonly ISet<string> IndustrialTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "RefinedMetal", "ManufacturedMaterial", "Plastic", "Rubber", "Glass", "Steel"
        };
        private static readonly ISet<string> OreOrOrganicTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "MetalOre", "Organic", "ConsumableOre"
        };
        private static readonly ISet<string> CommonTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "BuildableRaw", "Agricultural", "RawMineral"
        };
        private static IReadOnlyDictionary<string, MaterialRule> rules =
            new ReadOnlyDictionary<string, MaterialRule>(new Dictionary<string, MaterialRule>(StringComparer.Ordinal));
        private static bool wasBuilt;

        public static IReadOnlyDictionary<string, MaterialRule> Rules {
            get { return rules; }
        }

        public static void Build() {
            if (wasBuilt) {
                return;
            }

            var candidates = new Dictionary<string, MaterialRule>(StringComparer.Ordinal);
            foreach (var element in ElementLoader.elements) {
                if (element == null || element.id == ProtoMatterRegistration.Hash) {
                    continue;
                }

                var descriptor = ToDescriptor(element);
                MaterialRule rule;
                if (MaterialClassifier.TryCreateRule(descriptor, ForbiddenTechOptions.Current, out rule)) {
                    candidates[element.id.ToString()] = rule;
                }
            }

            var unsafeIds = new HashSet<string>(ConversionMath.ValidateAll(candidates.Values,
                ForbiddenTechOptions.Current), StringComparer.Ordinal);
            foreach (var unsafeId in unsafeIds) {
                if (string.IsNullOrEmpty(unsafeId)) {
                    continue;
                }

                if (candidates.Remove(unsafeId)) {
                    Debug.LogWarning("[ForbiddenTechnologyPack] Excluded unsafe Proto-Matter rule: " + unsafeId);
                }
            }

            rules = new ReadOnlyDictionary<string, MaterialRule>(candidates);
            wasBuilt = true;
        }

        private static ElementDescriptor ToDescriptor(Element element) {
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddTag(tags, element.materialCategory);
            if (element.oreTags != null) {
                foreach (var oreTag in element.oreTags) {
                    AddTag(tags, oreTag);
                }
            }
            AddClassificationTags(tags, element.id.ToString());

            return new ElementDescriptor(element.id.ToString(), element.IsSolid ? "Solid" : element.state.ToString(),
                tags, !element.disabled, IsDlcActive(element.dlcId), IsSpecial(element));
        }

        private static void AddTag(ISet<string> tags, Tag tag) {
            if (tag.IsValid) {
                tags.Add(tag.Name);
            }
        }

        private static void AddClassificationTags(ISet<string> tags, string elementId) {
            if (EndgameIds.Contains(elementId)) {
                tags.Add("Endgame");
            }
            if (RareIds.Contains(elementId)) {
                tags.Add("Rare");
            }
            AddCanonicalTag(tags, IndustrialTags, "Industrial");
            AddCanonicalTag(tags, OreOrOrganicTags, "OreOrOrganic");
            AddCanonicalTag(tags, CommonTags, "Common");
        }

        private static void AddCanonicalTag(ISet<string> tags, ISet<string> sourceTags, string canonicalTag) {
            foreach (var tag in sourceTags) {
                if (tags.Contains(tag)) {
                    tags.Add(canonicalTag);
                    return;
                }
            }
        }

        private static bool IsDlcActive(string dlcId) {
#pragma warning disable 0618
            return string.IsNullOrEmpty(dlcId) || DlcManager.IsContentActive(dlcId);
#pragma warning restore 0618
        }

        private static bool IsSpecial(Element element) {
            return element.id == SimHashes.Unobtanium;
        }
    }

    [HarmonyPatch(typeof(ElementLoader), "Load")]
    public static class ElementCatalogElementLoaderPatch {
        [HarmonyPostfix]
        public static void Postfix() {
            ElementCatalogAdapter.Build();
        }
    }
}
