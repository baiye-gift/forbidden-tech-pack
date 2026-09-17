namespace ForbiddenTechnologyPack.Core {
    public sealed class ReconstructionMassEvaluation {
        public ReconstructionMassEvaluation(bool isValid, float totalInputMass, float outputMass) {
            IsValid = isValid;
            TotalInputMass = totalInputMass;
            OutputMass = outputMass;
            IsMassSafe = isValid && outputMass <= totalInputMass;
            UnreturnedMass = isValid && outputMass <= totalInputMass
                ? totalInputMass - outputMass
                : 0f;
        }

        public bool IsValid { get; private set; }
        public bool IsMassSafe { get; private set; }
        public float TotalInputMass { get; private set; }
        public float OutputMass { get; private set; }
        public float UnreturnedMass { get; private set; }
    }

    public static class MatterReconstructionPolicy {
        public static ReconstructionMassEvaluation EvaluateMass(float sourceMass,
                float protoMatterMass, float outputMass) {
            if (!IsPositiveFinite(sourceMass) || !IsPositiveFinite(protoMatterMass) ||
                    !IsPositiveFinite(outputMass)) {
                return new ReconstructionMassEvaluation(false, 0f, 0f);
            }

            var totalInputMass = sourceMass + protoMatterMass;
            if (float.IsNaN(totalInputMass) || float.IsInfinity(totalInputMass)) {
                return new ReconstructionMassEvaluation(false, 0f, 0f);
            }

            return new ReconstructionMassEvaluation(true, totalInputMass, outputMass);
        }

        private static bool IsPositiveFinite(float value) {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
