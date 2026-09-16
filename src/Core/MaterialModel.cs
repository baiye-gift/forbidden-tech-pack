using System;
using System.Collections.Generic;

namespace ForbiddenTechnologyPack.Core {
    public enum MaterialTier {
        Common,
        OreOrOrganic,
        Industrial,
        Rare,
        Endgame
    }

    public sealed class ElementDescriptor {
        public ElementDescriptor(string id, string state, IEnumerable<string> tags, bool isStorable,
                bool isActive, bool isSpecial) {
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException("Element ID is required.", "id");
            }

            Id = id;
            State = state ?? string.Empty;
            Tags = new HashSet<string>(tags ?? new string[0], StringComparer.OrdinalIgnoreCase);
            IsStorable = isStorable;
            IsActive = isActive;
            IsSpecial = isSpecial;
        }

        public string Id { get; private set; }
        public string State { get; private set; }
        public ISet<string> Tags { get; private set; }
        public bool IsStorable { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsSpecial { get; private set; }

        public static ElementDescriptor Solid(string id, string tag, bool isStorable = true, bool isActive = true) {
            return new ElementDescriptor(id, "Solid", new[] { tag }, isStorable, isActive, false);
        }

        public static ElementDescriptor Solid(string id, params string[] tags) {
            return new ElementDescriptor(id, "Solid", tags, true, true, false);
        }

        public static ElementDescriptor SpecialSolid(string id, params string[] tags) {
            return new ElementDescriptor(id, "Solid", tags, true, true, true);
        }

        public static ElementDescriptor NonSolid(string id, params string[] tags) {
            return new ElementDescriptor(id, "Liquid", tags, true, true, false);
        }
    }

    public sealed class MaterialRule {
        public MaterialRule(string elementId, MaterialTier tier, float protoMatterPerKg) {
            if (string.IsNullOrWhiteSpace(elementId)) {
                throw new ArgumentException("Element ID is required.", "elementId");
            }
            if (float.IsNaN(protoMatterPerKg) || float.IsInfinity(protoMatterPerKg) || protoMatterPerKg <= 0f) {
                throw new ArgumentOutOfRangeException("protoMatterPerKg");
            }

            ElementId = elementId;
            Tier = tier;
            ProtoMatterPerKg = protoMatterPerKg;
        }

        public string ElementId { get; private set; }
        public MaterialTier Tier { get; private set; }
        public float ProtoMatterPerKg { get; private set; }

        public static MaterialRule ForTier(string elementId, MaterialTier tier) {
            return new MaterialRule(elementId, tier, CostForTier(tier));
        }

        public static float CostForTier(MaterialTier tier) {
            switch (tier) {
            case MaterialTier.Common:
                return 1.25f;
            case MaterialTier.OreOrOrganic:
                return 1.5f;
            case MaterialTier.Industrial:
                return 3f;
            case MaterialTier.Rare:
                return 6f;
            case MaterialTier.Endgame:
                return 12f;
            default:
                throw new ArgumentOutOfRangeException("tier");
            }
        }
    }
}
