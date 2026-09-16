using System;
using System.Collections.Generic;

namespace ForbiddenTechnologyPack.Core {
    public static class ConversionMath {
        public static float CrusherOutputKg(float inputKg, ResolvedOptions options) {
            ValidateMass(inputKg, "inputKg");
            ValidateOptions(options);
            return inputKg * options.RecoveryRate;
        }

        public static float CompilerInputKg(float outputKg, MaterialRule rule, ResolvedOptions options) {
            ValidateMass(outputKg, "outputKg");
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }
            ValidateOptions(options);
            return outputKg * rule.ProtoMatterPerKg * options.CostMultiplier;
        }

        public static bool IsRoundTripSafe(MaterialRule rule, ResolvedOptions options) {
            if (rule == null || options == null) {
                return false;
            }

            try {
                return CrusherOutputKg(1f, options) < CompilerInputKg(1f, rule, options);
            } catch (ArgumentOutOfRangeException) {
                return false;
            }
        }

        public static IList<string> ValidateAll(IEnumerable<MaterialRule> rules, ResolvedOptions options) {
            if (rules == null) {
                throw new ArgumentNullException("rules");
            }

            var unsafeIds = new List<string>();
            foreach (var rule in rules) {
                if (rule == null || !IsRoundTripSafe(rule, options)) {
                    unsafeIds.Add(rule == null ? string.Empty : rule.ElementId);
                }
            }
            return unsafeIds;
        }

        private static void ValidateMass(float massKg, string parameterName) {
            if (float.IsNaN(massKg) || float.IsInfinity(massKg) || massKg < 0f) {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void ValidateOptions(ResolvedOptions options) {
            if (options == null || float.IsNaN(options.RecoveryRate) || float.IsInfinity(options.RecoveryRate) ||
                    float.IsNaN(options.CostMultiplier) || float.IsInfinity(options.CostMultiplier) ||
                    options.RecoveryRate < 0f || options.CostMultiplier <= 0f) {
                throw new ArgumentOutOfRangeException("options");
            }
        }
    }
}
