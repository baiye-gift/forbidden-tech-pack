using System;
using System.Collections.Generic;

namespace ForbiddenTechnologyPack.Core {
    public static class CompilerRecipeFilter {
        public static bool HasOutputCapacity(float resultMassKg, float remainingCapacityKg) {
            ValidateNonNegativeFinite(resultMassKg, "resultMassKg");
            ValidateNonNegativeFinite(remainingCapacityKg, "remainingCapacityKg");
            return remainingCapacityKg >= resultMassKg;
        }

        public static IReadOnlyList<string> SelectIds(IEnumerable<MaterialRule> rules,
                UnlockState unlockState, ISet<string> activeRecipeIds) {
            if (rules == null) {
                throw new ArgumentNullException("rules");
            }
            if (unlockState == null) {
                throw new ArgumentNullException("unlockState");
            }
            if (activeRecipeIds == null) {
                throw new ArgumentNullException("activeRecipeIds");
            }

            var selected = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in rules) {
                if (rule != null && unlockState.IsUnlocked(rule.ElementId) &&
                        activeRecipeIds.Contains(rule.ElementId) && seen.Add(rule.ElementId)) {
                    selected.Add(rule.ElementId);
                }
            }
            return selected;
        }

        private static void ValidateNonNegativeFinite(float value, string parameterName) {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f) {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
