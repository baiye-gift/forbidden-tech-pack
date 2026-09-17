using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;

namespace ForbiddenTechnologyPack.Game.Elements {
    public static class ReconstructionSubstrateTags {
        public static readonly Tag Common = new Tag("BaiyeRealitySubstrateCommon");
        public static readonly Tag OreOrOrganic = new Tag("BaiyeRealitySubstrateOreOrOrganic");
        public static readonly Tag Industrial = new Tag("BaiyeRealitySubstrateIndustrial");
        public static readonly Tag Rare = new Tag("BaiyeRealitySubstrateRare");

        public static Tag ForTier(MaterialTier targetTier) {
            return ForSubstrateTier(MatterReconstructionPolicy.RequiredSubstrateTier(targetTier));
        }

        public static Tag ForSubstrateTier(MaterialTier substrateTier) {
            switch (substrateTier) {
            case MaterialTier.Common:
                return Common;
            case MaterialTier.OreOrOrganic:
                return OreOrOrganic;
            case MaterialTier.Industrial:
                return Industrial;
            case MaterialTier.Rare:
                return Rare;
            default:
                throw new ArgumentOutOfRangeException("substrateTier");
            }
        }

        public static void AttachToElement(Element element, MaterialTier elementTier) {
            if (element == null || element.id == ProtoMatterRegistration.Hash ||
                    elementTier == MaterialTier.Endgame) {
                return;
            }

            var tag = ForSubstrateTier(elementTier);
            var tags = new List<Tag>();
            if (element.oreTags != null) {
                for (var index = 0; index < element.oreTags.Length; index++) {
                    var existing = element.oreTags[index];
                    if (existing.IsValid) {
                        if (string.Equals(existing.Name, tag.Name, StringComparison.Ordinal)) {
                            return;
                        }
                        tags.Add(existing);
                    }
                }
            }

            tags.Add(tag);
            element.oreTags = tags.ToArray();
        }
    }
}
