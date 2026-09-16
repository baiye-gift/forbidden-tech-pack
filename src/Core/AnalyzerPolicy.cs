using System;
using System.Collections.Generic;

namespace ForbiddenTechnologyPack.Core {
    public static class AnalyzerPolicy {
        public static IReadOnlyList<string> VisibleRuleIds(IEnumerable<MaterialRule> rules,
                UnlockState unlocks, ISet<string> activeRuleIds) {
            if (rules == null) {
                throw new ArgumentNullException("rules");
            }
            if (unlocks == null) {
                throw new ArgumentNullException("unlocks");
            }
            if (activeRuleIds == null) {
                throw new ArgumentNullException("activeRuleIds");
            }

            var visible = new List<string>();
            foreach (var rule in rules) {
                if (rule != null && activeRuleIds.Contains(rule.ElementId) &&
                        !unlocks.IsUnlocked(rule.ElementId)) {
                    visible.Add(rule.ElementId);
                }
            }
            return visible;
        }

        public static bool ShouldConsumeSample(ResolvedOptions options) {
            return options != null && options.ConsumeSamples;
        }
    }
}
