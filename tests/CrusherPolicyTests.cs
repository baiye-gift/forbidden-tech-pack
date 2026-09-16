using ForbiddenTechnologyPack.Core;

internal static class CrusherPolicyTests {
    public static void Run() {
        var strong = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Strong });
        var balanced = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Balanced });

        AssertEx.Near(90f, CrusherPolicy.OutputKg(100f, strong), 0.0001f, "strong batch");
        AssertEx.Near(75f, CrusherPolicy.OutputKg(100f, balanced), 0.0001f, "balanced batch");
        AssertEx.False(CrusherPolicy.CanCrush(ModIdentity.ProtoMatterId), "proto matter denied");
        AssertEx.True(CrusherPolicy.CanStart(100f, 90f, 100f), "space available");
        AssertEx.False(CrusherPolicy.CanStart(100f, 90f, 89.99f), "output blocked");
    }
}
