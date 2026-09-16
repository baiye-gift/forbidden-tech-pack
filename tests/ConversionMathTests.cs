using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;

internal static class ConversionMathTests {
    public static void Run() {
        var strong = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Strong });
        AssertEx.Near(90f, ConversionMath.CrusherOutputKg(100f, strong), 0.0001f, "crusher recovery");

        foreach (MaterialTier tier in Enum.GetValues(typeof(MaterialTier))) {
            var rule = MaterialRule.ForTier("test-" + tier, tier);
            AssertEx.True(ConversionMath.IsRoundTripSafe(rule, strong), "safe " + tier);
            float proto = ConversionMath.CrusherOutputKg(100f, strong);
            float rebuilt = proto / (rule.ProtoMatterPerKg * strong.CostMultiplier);
            AssertEx.True(rebuilt < 100f, "strict loss " + tier);
            AssertEx.Near(rule.ProtoMatterPerKg, ConversionMath.CompilerInputKg(1f, rule, strong), 0.0001f,
                "compiler cost " + tier);
        }

        var common = MaterialRule.ForTier("Dirt", MaterialTier.Common);
        var unsafeOptions = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Custom,
            RecoveryRate = 1f,
            CostMultiplier = 0.05f
        });
        AssertEx.False(ConversionMath.IsRoundTripSafe(common, unsafeOptions), "unsafe low cost detected");
        AssertEx.SequenceEqual(new[] { "Dirt", "Steel" },
            ConversionMath.ValidateAll(new[] { common, MaterialRule.ForTier("Steel", MaterialTier.Industrial) }, unsafeOptions),
            "validation reports unsafe rules");
        AssertEx.Throws<ArgumentOutOfRangeException>(() => ConversionMath.CrusherOutputKg(-1f, strong),
            "negative crusher input denied");
        AssertEx.Throws<ArgumentException>(() => ConversionMath.CompilerInputKg(1f, null, strong),
            "null rule denied");
    }
}
