using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;

internal static class AnalyzerPolicyTests {
    public static void Run() {
        var rules = new[] {
            MaterialRule.ForTier("Iron", MaterialTier.Industrial),
            MaterialRule.ForTier("Diamond", MaterialTier.Rare),
            MaterialRule.ForTier("Niobium", MaterialTier.Endgame)
        };
        var unlocked = UnlockState.FromSerialized(1, new[] { "Iron" });
        var visible = AnalyzerPolicy.VisibleRuleIds(rules, unlocked,
            new HashSet<string> { "Iron", "Diamond" });
        AssertEx.SequenceEqual(new[] { "Diamond" }, visible, "only active locked samples");

        var strong = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Strong });
        var extreme = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Extreme });
        AssertEx.True(AnalyzerPolicy.ShouldConsumeSample(strong), "strong consumes");
        AssertEx.False(AnalyzerPolicy.ShouldConsumeSample(extreme), "extreme preserves");
    }
}
