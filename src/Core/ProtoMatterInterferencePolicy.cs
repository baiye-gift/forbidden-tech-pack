using System;
using System.Collections.Generic;

namespace ForbiddenTechnologyPack.Core {
    public static class ProtoMatterInterferencePolicy {
        public static bool IsInsideRadius(int sourceX, int sourceY, int targetX, int targetY,
                int radius) {
            if (radius < 0) {
                return false;
            }

            var deltaX = (long)targetX - sourceX;
            var deltaY = (long)targetY - sourceY;
            var squaredRadius = (long)radius * radius;
            return deltaX * deltaX + deltaY * deltaY <= squaredRadius;
        }
    }

    public sealed class ProtoMatterInterferenceState {
        private readonly HashSet<string> activeSources =
            new HashSet<string>(StringComparer.Ordinal);

        public bool IsInterfered {
            get { return activeSources.Count > 0; }
        }

        public int ActiveSourceCount {
            get { return activeSources.Count; }
        }

        public bool SetSource(string sourceId, bool active) {
            if (string.IsNullOrEmpty(sourceId)) {
                return false;
            }

            return active ? activeSources.Add(sourceId) : activeSources.Remove(sourceId);
        }
    }
}
