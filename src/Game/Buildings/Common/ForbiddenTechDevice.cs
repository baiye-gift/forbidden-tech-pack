using System;
using System.Collections.Generic;

namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    public sealed class ForbiddenTechDevice : KMonoBehaviour {
        internal static readonly Operational.Flag ProtoMatterStable =
            new Operational.Flag("ForbiddenTechProtoMatterStable", Operational.Flag.Type.Requirement);

        private readonly HashSet<string> activeSources =
            new HashSet<string>(StringComparer.Ordinal);
        private Operational operational;

        protected override void OnSpawn() {
            base.OnSpawn();
            operational = GetComponent<Operational>();
            if (operational != null) {
                operational.SetFlag(ProtoMatterStable, true);
            }
            ProtoMatterInterferenceManager.Register(this);
        }

        protected override void OnCleanUp() {
            ProtoMatterInterferenceManager.Unregister(this);
            activeSources.Clear();
            base.OnCleanUp();
        }

        internal void SetInterferenceSource(string sourceId, bool active) {
            if (string.IsNullOrEmpty(sourceId)) {
                return;
            }

            var changed = active ? activeSources.Add(sourceId) : activeSources.Remove(sourceId);
            if (!changed) {
                return;
            }

            var stable = activeSources.Count == 0;
            if (operational != null && operational.GetFlag(ProtoMatterStable) != stable) {
                operational.SetFlag(ProtoMatterStable, stable);
            }

            var fabricator = GetComponent<ComplexFabricator>();
            if (fabricator != null) {
                fabricator.SetQueueDirty();
            }
        }
    }
}
