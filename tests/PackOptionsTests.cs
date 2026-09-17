using ForbiddenTechnologyPack.Core;

internal static class PackOptionsTests {
    public static void Run() {
        var strong = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Strong });
        AssertEx.Near(0.90f, strong.RecoveryRate, 0.0001f, "strong recovery");
        AssertEx.Near(1.00f, strong.CostMultiplier, 0.0001f, "strong cost");
        AssertEx.True(strong.AllowEndgame, "strong endgame");
        AssertEx.True(strong.ConsumeSamples, "strong sample use");
        AssertEx.True(strong.ReconstructorEnabled, "strong reconstructor enabled");
        AssertEx.True(strong.EntropyDiverterEnabled, "strong entropy diverter enabled");
        AssertEx.True(strong.AnnihilationReactorEnabled, "strong annihilation reactor enabled");

        var extreme = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Extreme });
        AssertEx.Near(0.25f, extreme.CostMultiplier, 0.0001f, "extreme cost");
        AssertEx.False(extreme.ConsumeSamples, "extreme sample use");

        var custom = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Custom,
            RecoveryRate = 4f,
            CostMultiplier = -1f,
            PowerMultiplier = 0f,
            HeatMultiplier = 99f
        });
        AssertEx.Near(1f, custom.RecoveryRate, 0.0001f, "recovery clamp");
        AssertEx.Near(0.05f, custom.CostMultiplier, 0.0001f, "cost clamp");
        AssertEx.Near(0.10f, custom.PowerMultiplier, 0.0001f, "power clamp");
        AssertEx.Near(10f, custom.HeatMultiplier, 0.0001f, "heat clamp");

        var disabledBuilding = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Extreme,
            ModuleEnabled = false,
            AnalyzerEnabled = false,
            CrusherEnabled = true,
            CompilerEnabled = false,
            ReconstructorEnabled = false,
            EntropyDiverterEnabled = true,
            AnnihilationReactorEnabled = false
        });
        AssertEx.False(disabledBuilding.ModuleEnabled, "preset preserves module switch");
        AssertEx.False(disabledBuilding.AnalyzerEnabled, "preset preserves analyzer switch");
        AssertEx.True(disabledBuilding.CrusherEnabled, "preset preserves crusher switch");
        AssertEx.False(disabledBuilding.CompilerEnabled, "preset preserves compiler switch");
        AssertEx.False(disabledBuilding.ReconstructorEnabled, "preset preserves reconstructor switch");
        AssertEx.True(disabledBuilding.EntropyDiverterEnabled, "preset preserves entropy diverter switch");
        AssertEx.False(disabledBuilding.AnnihilationReactorEnabled, "preset preserves annihilation reactor switch");

        var invalid = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Custom,
            RecoveryRate = float.NaN,
            CostMultiplier = float.PositiveInfinity,
            PowerMultiplier = float.NegativeInfinity,
            HeatMultiplier = float.NaN
        });
        AssertEx.Near(0.05f, invalid.RecoveryRate, 0.0001f, "nan recovery clamps");
        AssertEx.Near(0.05f, invalid.CostMultiplier, 0.0001f, "infinite cost clamps");
        AssertEx.Near(0.10f, invalid.PowerMultiplier, 0.0001f, "infinite power clamps");
        AssertEx.Near(0.00f, invalid.HeatMultiplier, 0.0001f, "nan heat clamps");
    }
}
