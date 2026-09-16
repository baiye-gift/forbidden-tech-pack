using System;

namespace ForbiddenTechnologyPack.Core {
    public static class CrusherPolicy {
        public static float OutputKg(float inputKg, ResolvedOptions options) {
            return ConversionMath.CrusherOutputKg(inputKg, options);
        }

        public static bool CanCrush(string elementId) {
            return !string.IsNullOrWhiteSpace(elementId) &&
                !string.Equals(elementId, ModIdentity.ProtoMatterId, StringComparison.Ordinal);
        }

        public static bool CanStart(float inputKg, float expectedOutputKg, float freeOutputCapacityKg) {
            return IsFiniteNonNegative(inputKg) && inputKg > 0f &&
                IsFiniteNonNegative(expectedOutputKg) && IsFiniteNonNegative(freeOutputCapacityKg) &&
                expectedOutputKg <= freeOutputCapacityKg;
        }

        private static bool IsFiniteNonNegative(float value) {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
