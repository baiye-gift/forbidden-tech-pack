using System;
using System.Collections.Generic;

namespace ForbiddenTechnologyPack.Core {
    public sealed class RegistrationPlan {
        private readonly string[] phase1BuildingIds;
        private readonly string[] phase2BuildingIds;
        private readonly string[] buildingIds;

        public RegistrationPlan(IEnumerable<string> phase1BuildingIds,
                IEnumerable<string> phase2BuildingIds) {
            if (phase1BuildingIds == null) {
                throw new ArgumentNullException("phase1BuildingIds");
            }
            if (phase2BuildingIds == null) {
                throw new ArgumentNullException("phase2BuildingIds");
            }

            this.phase1BuildingIds = new List<string>(phase1BuildingIds).ToArray();
            this.phase2BuildingIds = new List<string>(phase2BuildingIds).ToArray();

            var all = new List<string>(this.phase1BuildingIds.Length + this.phase2BuildingIds.Length);
            all.AddRange(this.phase1BuildingIds);
            all.AddRange(this.phase2BuildingIds);
            buildingIds = all.ToArray();
        }

        public IReadOnlyList<string> BuildingIds {
            get { return buildingIds; }
        }

        public IReadOnlyList<string> Phase1BuildingIds {
            get { return phase1BuildingIds; }
        }

        public IReadOnlyList<string> Phase2BuildingIds {
            get { return phase2BuildingIds; }
        }

        public bool HasAnyBuildings {
            get { return buildingIds.Length > 0; }
        }

        public bool HasPhase1Buildings {
            get { return phase1BuildingIds.Length > 0; }
        }

        public bool HasPhase2Buildings {
            get { return phase2BuildingIds.Length > 0; }
        }

        public bool IsEnabled(string buildingId) {
            if (string.IsNullOrEmpty(buildingId)) {
                return false;
            }

            for (var index = 0; index < buildingIds.Length; index++) {
                if (string.Equals(buildingIds[index], buildingId, StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }
    }

    public static class RegistrationPolicy {
        public static RegistrationPlan Create(ResolvedOptions options) {
            if (options == null) {
                throw new ArgumentNullException("options");
            }

            var phase1BuildingIds = new List<string>();
            var phase2BuildingIds = new List<string>();
            if (!options.ModuleEnabled) {
                return new RegistrationPlan(phase1BuildingIds, phase2BuildingIds);
            }

            if (options.AnalyzerEnabled) {
                phase1BuildingIds.Add(ModIdentity.MatterAnalyzerId);
            }
            if (options.CrusherEnabled) {
                phase1BuildingIds.Add(ModIdentity.MassCrusherId);
            }
            if (options.CompilerEnabled) {
                phase1BuildingIds.Add(ModIdentity.MatterCompilerId);
            }

            if (options.ReconstructorEnabled) {
                phase2BuildingIds.Add(ModIdentity.MatterReconstructorId);
            }
            if (options.EntropyDiverterEnabled) {
                phase2BuildingIds.Add(ModIdentity.EntropyFluxDiverterId);
            }
            if (options.AnnihilationReactorEnabled) {
                phase2BuildingIds.Add(ModIdentity.MatterAnnihilationReactorId);
            }

            return new RegistrationPlan(phase1BuildingIds, phase2BuildingIds);
        }
    }
}
