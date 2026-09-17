using System;
using System.Collections.Generic;

namespace ForbiddenTechnologyPack.Core {
    public sealed class RegistrationPlan {
        private readonly string[] buildingIds;

        public RegistrationPlan(IEnumerable<string> buildingIds) {
            if (buildingIds == null) {
                throw new ArgumentNullException("buildingIds");
            }

            this.buildingIds = new List<string>(buildingIds).ToArray();
        }

        public IReadOnlyList<string> BuildingIds {
            get { return buildingIds; }
        }

        public bool HasAnyBuildings {
            get { return buildingIds.Length > 0; }
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

            var buildingIds = new List<string>();
            if (!options.ModuleEnabled) {
                return new RegistrationPlan(buildingIds);
            }

            if (options.AnalyzerEnabled) {
                buildingIds.Add(ModIdentity.MatterAnalyzerId);
            }
            if (options.CrusherEnabled) {
                buildingIds.Add(ModIdentity.MassCrusherId);
            }
            if (options.CompilerEnabled) {
                buildingIds.Add(ModIdentity.MatterCompilerId);
            }
            if (options.ReconstructorEnabled) {
                buildingIds.Add(ModIdentity.MatterReconstructorId);
            }
            if (options.EntropyDiverterEnabled) {
                buildingIds.Add(ModIdentity.EntropyFluxDiverterId);
            }
            if (options.AnnihilationReactorEnabled) {
                buildingIds.Add(ModIdentity.MatterAnnihilationReactorId);
            }

            return new RegistrationPlan(buildingIds);
        }
    }
}
