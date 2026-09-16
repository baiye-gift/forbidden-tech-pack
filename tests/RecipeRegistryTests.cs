using ForbiddenTechnologyPack.Core;

internal static class RecipeRegistryTests {
    public static void Run() {
        var strong = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Strong });
        var common = RecipePlanFactory.Create(MaterialRule.ForTier("Dirt", MaterialTier.Common), strong);
        AssertEx.Equal("BaiyeMatterAnalyze_Dirt", common.AnalyzerId, "analyzer id");
        AssertEx.Equal("BaiyeMatterCrush_Dirt", common.CrusherId, "crusher id");
        AssertEx.Equal("BaiyeMatterCompile_Dirt", common.CompilerId, "compiler id");
        AssertEx.Near(10f, common.AnalyzerInputKg, 0.0001f, "sample mass");
        AssertEx.False(common.AnalyzerInputDoNotConsume, "strong samples are consumed");
        AssertEx.Near(30f, common.AnalyzerTimeSeconds, 0.0001f, "analyzer time");
        AssertEx.Near(100f, common.CrusherInputKg, 0.0001f, "crusher input");
        AssertEx.Near(90f, common.CrusherOutputKg, 0.0001f, "crusher output");
        AssertEx.Near(40f, common.CrusherTimeSeconds, 0.0001f, "crusher time");
        AssertEx.Near(125f, common.CompilerInputKg, 0.0001f, "common compiler input");
        AssertEx.Near(100f, common.CompilerOutputKg, 0.0001f, "common batch");
        AssertEx.Near(37.5f, common.CompilerTimeSeconds, 0.0001f, "common compiler time");
        AssertEx.True(common.IsRoundTripSafe, "common compile crush loop loses mass");

        var endgame = RecipePlanFactory.Create(MaterialRule.ForTier("Isoresin", MaterialTier.Endgame), strong);
        AssertEx.Near(120f, endgame.CompilerInputKg, 0.0001f, "endgame compiler input");
        AssertEx.Near(10f, endgame.CompilerOutputKg, 0.0001f, "endgame batch");
        AssertEx.Near(360f, endgame.CompilerTimeSeconds, 0.0001f, "endgame compiler time");
        AssertEx.True(endgame.IsRoundTripSafe, "endgame compile crush loop loses mass");

        var retainedSamples = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Custom,
            RecoveryRate = 0.9f,
            CostMultiplier = 1f,
            ConsumeSamples = false
        });
        var freeSample = RecipePlanFactory.Create(MaterialRule.ForTier("Dirt", MaterialTier.Common), retainedSamples);
        AssertEx.True(freeSample.AnalyzerInputDoNotConsume, "non-consuming samples are retained");

        var unsafeOptions = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Custom,
            RecoveryRate = 1f,
            CostMultiplier = 0.05f
        });
        AssertEx.Throws<System.ArgumentException>(() => RecipePlanFactory.Create(
            MaterialRule.ForTier("Dirt", MaterialTier.Common), unsafeOptions),
            "unsafe round trip recipe denied");
    }
}
