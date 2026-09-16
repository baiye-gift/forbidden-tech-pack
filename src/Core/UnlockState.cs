using System;
using System.Collections.Generic;
using System.Linq;

namespace ForbiddenTechnologyPack.Core {
    public sealed class UnlockState {
        public const int CurrentVersion = 1;

        private readonly HashSet<string> elementIds;

        private UnlockState(IEnumerable<string> serializedIds) {
            elementIds = new HashSet<string>(StringComparer.Ordinal);
            AddValidIds(serializedIds);
        }

        public int Version {
            get { return CurrentVersion; }
        }

        public IReadOnlyCollection<string> ElementIds {
            get { return elementIds; }
        }

        public static UnlockState FromSerialized(int dataVersion, IEnumerable<string> serializedIds) {
            return new UnlockState(serializedIds);
        }

        public bool Unlock(string elementId) {
            return !string.IsNullOrWhiteSpace(elementId) && elementIds.Add(elementId);
        }

        public bool IsUnlocked(string elementId) {
            return !string.IsNullOrWhiteSpace(elementId) && elementIds.Contains(elementId);
        }

        public IReadOnlyList<string> ActiveUnlocked(ISet<string> activeRuleIds) {
            if (activeRuleIds == null) {
                throw new ArgumentNullException("activeRuleIds");
            }

            return elementIds.Where(activeRuleIds.Contains)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
        }

        public IReadOnlyList<string> ToSerialized() {
            return elementIds.OrderBy(id => id, StringComparer.Ordinal).ToList();
        }

        private void AddValidIds(IEnumerable<string> serializedIds) {
            if (serializedIds == null) {
                return;
            }

            foreach (var elementId in serializedIds) {
                if (!string.IsNullOrWhiteSpace(elementId)) {
                    elementIds.Add(elementId);
                }
            }
        }
    }
}
