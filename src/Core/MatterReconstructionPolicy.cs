using System;

namespace ForbiddenTechnologyPack.Core {
    public sealed class ReconstructionMassEvaluation {
        public ReconstructionMassEvaluation(bool isValid, float sourceMass, float totalInputMass,
                float outputMass) {
            IsValid = isValid;
            SourceMass = sourceMass;
            TotalInputMass = totalInputMass;
            OutputMass = outputMass;
            IsMassSafe = isValid && outputMass <= sourceMass;
            UnreturnedMass = IsMassSafe ? totalInputMass - outputMass : 0f;
        }

        public bool IsValid { get; private set; }
        public bool IsMassSafe { get; private set; }
        public float SourceMass { get; private set; }
        public float TotalInputMass { get; private set; }
        public float OutputMass { get; private set; }
        public float UnreturnedMass { get; private set; }
    }

    public sealed class ReconstructionPlan {
        public ReconstructionPlan(string targetElementId, MaterialTier targetTier,
                MaterialTier substrateTier, float substrateKg, float protoMatterKg,
                float productKg, float timeSeconds) {
            TargetElementId = targetElementId;
            TargetTier = targetTier;
            SubstrateTier = substrateTier;
            SubstrateKg = substrateKg;
            ProtoMatterKg = protoMatterKg;
            ProductKg = productKg;
            TimeSeconds = timeSeconds;
        }

        public string TargetElementId { get; private set; }
        public MaterialTier TargetTier { get; private set; }
        public MaterialTier SubstrateTier { get; private set; }
        public float SubstrateKg { get; private set; }
        public float ProtoMatterKg { get; private set; }
        public float ProductKg { get; private set; }
        public float TimeSeconds { get; private set; }
    }

    public static class MatterReconstructionPolicy {
        private const float BatchMassKg = 1000f;
        private const float BatchTimeSeconds = 120f;

        public static ReconstructionMassEvaluation EvaluateMass(float sourceMass,
                float protoMatterMass, float outputMass) {
            if (!IsPositiveFinite(sourceMass) || !IsPositiveFinite(protoMatterMass) ||
                    !IsPositiveFinite(outputMass)) {
                return new ReconstructionMassEvaluation(false, 0f, 0f, 0f);
            }

            var totalInputMass = sourceMass + protoMatterMass;
            if (float.IsNaN(totalInputMass) || float.IsInfinity(totalInputMass)) {
                return new ReconstructionMassEvaluation(false, 0f, 0f, 0f);
            }

            return new ReconstructionMassEvaluation(true, sourceMass, totalInputMass, outputMass);
        }

        public static MaterialTier RequiredSubstrateTier(MaterialTier targetTier) {
            switch (targetTier) {
            case MaterialTier.Common:
            case MaterialTier.OreOrOrganic:
                return MaterialTier.Common;
            case MaterialTier.Industrial:
                return MaterialTier.OreOrOrganic;
            case MaterialTier.Rare:
                return MaterialTier.Industrial;
            case MaterialTier.Endgame:
                return MaterialTier.Rare;
            default:
                throw new ArgumentOutOfRangeException("targetTier");
            }
        }

        public static float BaseProtoMatterKg(MaterialTier targetTier) {
            switch (targetTier) {
            case MaterialTier.Common:
                return 50f;
            case MaterialTier.OreOrOrganic:
                return 75f;
            case MaterialTier.Industrial:
                return 100f;
            case MaterialTier.Rare:
                return 200f;
            case MaterialTier.Endgame:
                return 300f;
            default:
                throw new ArgumentOutOfRangeException("targetTier");
            }
        }

        public static ReconstructionPlan CreatePlan(MaterialRule targetRule,
                ResolvedOptions options) {
            if (targetRule == null) {
                throw new ArgumentNullException("targetRule");
            }
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            var protoMatterKg = BaseProtoMatterKg(targetRule.Tier) * options.CostMultiplier;
            if (!IsPositiveFinite(protoMatterKg)) {
                throw new ArgumentOutOfRangeException("options",
                    "Reconstruction Proto-Matter cost must be finite and positive.");
            }

            return new ReconstructionPlan(targetRule.ElementId, targetRule.Tier,
                RequiredSubstrateTier(targetRule.Tier), BatchMassKg, protoMatterKg,
                BatchMassKg, BatchTimeSeconds);
        }

        private static bool IsPositiveFinite(float value) {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
