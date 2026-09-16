using System;
using System.Collections.Generic;

namespace ForbiddenTechnologyPack.Core {
    public static class MaterialClassifier {
        private static readonly HashSet<string> DeniedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Unobtanium"
        };

        private static readonly string[] DeniedTags = {
            "Food", "Seed", "Egg", "Creature", "Artifact", "QuestItem", "Noncrushable"
        };

        public static bool TryCreateRule(ElementDescriptor element, ResolvedOptions options, out MaterialRule rule) {
            rule = null;
            if (element == null || options == null || !IsEligible(element)) {
                return false;
            }

            MaterialTier tier;
            if (!TryGetTier(element, out tier) || !IsTierEnabled(tier, options)) {
                return false;
            }

            rule = MaterialRule.ForTier(element.Id, tier);
            return true;
        }

        public static MaterialRule CreateRule(ElementDescriptor element, ResolvedOptions options) {
            MaterialRule rule;
            if (!TryCreateRule(element, options, out rule)) {
                var id = element == null ? "<null>" : element.Id;
                throw new ArgumentException("Element cannot be converted: " + id, "element");
            }

            return rule;
        }

        private static bool IsEligible(ElementDescriptor element) {
            if (!string.Equals(element.State, "Solid", StringComparison.OrdinalIgnoreCase) ||
                    !element.IsStorable || !element.IsActive || element.IsSpecial || DeniedIds.Contains(element.Id)) {
                return false;
            }

            foreach (var deniedTag in DeniedTags) {
                if (element.Tags.Contains(deniedTag)) {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetTier(ElementDescriptor element, out MaterialTier tier) {
            if (element.Tags.Contains("Endgame")) {
                tier = MaterialTier.Endgame;
                return true;
            }
            if (element.Tags.Contains("Rare")) {
                tier = MaterialTier.Rare;
                return true;
            }
            if (element.Tags.Contains("Industrial")) {
                tier = MaterialTier.Industrial;
                return true;
            }
            if (element.Tags.Contains("OreOrOrganic") || element.Tags.Contains("MetalOre") ||
                    element.Tags.Contains("Organic")) {
                tier = MaterialTier.OreOrOrganic;
                return true;
            }
            if (element.Tags.Contains("Common") || element.Tags.Contains("Agricultural") ||
                    element.Tags.Contains("BuildableRaw")) {
                tier = MaterialTier.Common;
                return true;
            }

            tier = default(MaterialTier);
            return false;
        }

        private static bool IsTierEnabled(MaterialTier tier, ResolvedOptions options) {
            switch (tier) {
            case MaterialTier.Industrial:
                return options.AllowIndustrial;
            case MaterialTier.Rare:
                return options.AllowRare;
            case MaterialTier.Endgame:
                return options.AllowEndgame;
            default:
                return true;
            }
        }
    }
}
