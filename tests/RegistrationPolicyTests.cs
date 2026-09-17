using System.Linq;
using ForbiddenTechnologyPack.Core;

internal static class RegistrationPolicyTests {
    public static void Run() {
        var strong = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Strong,
            ModuleEnabled = true,
            AnalyzerEnabled = true,
            CrusherEnabled = true,
            CompilerEnabled = true,
            ReconstructorEnabled = true,
            EntropyDiverterEnabled = true,
            AnnihilationReactorEnabled = true
        });
        var all = RegistrationPolicy.Create(strong);
        AssertEx.SequenceEqual(new[] {
            ModIdentity.MatterAnalyzerId,
            ModIdentity.MassCrusherId,
            ModIdentity.MatterCompilerId,
            ModIdentity.MatterReconstructorId,
            ModIdentity.EntropyFluxDiverterId,
            ModIdentity.MatterAnnihilationReactorId
        }, all.BuildingIds, "all buildings enabled");
        AssertEx.SequenceEqual(new[] {
            ModIdentity.MatterAnalyzerId,
            ModIdentity.MassCrusherId,
            ModIdentity.MatterCompilerId
        }, all.Phase1BuildingIds, "phase 1 research contents");
        AssertEx.SequenceEqual(new[] {
            ModIdentity.MatterReconstructorId,
            ModIdentity.EntropyFluxDiverterId,
            ModIdentity.MatterAnnihilationReactorId
        }, all.Phase2BuildingIds, "phase 2 research contents");
        AssertEx.True(all.HasAnyBuildings, "enabled module has build entries");
        AssertEx.True(all.HasPhase1Buildings, "phase 1 research has contents");
        AssertEx.True(all.HasPhase2Buildings, "phase 2 research has contents");

        var disabledOptions = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Strong,
            ModuleEnabled = true,
            AnalyzerEnabled = true,
            CrusherEnabled = true,
            CompilerEnabled = false,
            ReconstructorEnabled = false,
            EntropyDiverterEnabled = true,
            AnnihilationReactorEnabled = false
        });
        var disabled = RegistrationPolicy.Create(disabledOptions);
        AssertEx.False(disabled.BuildingIds.Contains(ModIdentity.MatterCompilerId), "compiler hidden");
        AssertEx.True(disabled.BuildingIds.Contains(ModIdentity.MassCrusherId), "crusher remains");
        AssertEx.False(disabled.BuildingIds.Contains(ModIdentity.MatterReconstructorId), "reconstructor hidden");
        AssertEx.True(disabled.BuildingIds.Contains(ModIdentity.EntropyFluxDiverterId), "entropy diverter remains");
        AssertEx.False(disabled.BuildingIds.Contains(ModIdentity.MatterAnnihilationReactorId), "annihilation reactor hidden");
        AssertEx.False(disabled.Phase1BuildingIds.Contains(ModIdentity.MatterCompilerId), "compiler removed from phase 1 research");
        AssertEx.SequenceEqual(new[] { ModIdentity.EntropyFluxDiverterId }, disabled.Phase2BuildingIds,
            "phase 2 research contains only enabled phase 2 buildings");

        var moduleOff = RegistrationPolicy.Create(PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Strong,
            ModuleEnabled = false,
            AnalyzerEnabled = true,
            CrusherEnabled = true,
            CompilerEnabled = true,
            ReconstructorEnabled = true,
            EntropyDiverterEnabled = true,
            AnnihilationReactorEnabled = true
        }));
        AssertEx.Equal(0, moduleOff.BuildingIds.Count, "module switch hides every building");
        AssertEx.False(moduleOff.HasAnyBuildings, "module switch hides research contents");
        AssertEx.False(moduleOff.HasPhase1Buildings, "module switch hides phase 1 research contents");
        AssertEx.False(moduleOff.HasPhase2Buildings, "module switch hides phase 2 research contents");
    }
}
