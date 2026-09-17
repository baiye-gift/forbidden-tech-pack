using ForbiddenTechnologyPack.Core;

internal static class EntropyFluxPolicyTests {
    public static void Run() {
        var nominal = EntropyFluxPolicy.Evaluate(
            10f, 4f, 373.15f, 273.15f, 473.15f,
            10f, 4f, 293.15f, 273.15f, 373.15f,
            2000000f);
        AssertEx.True(nominal.IsValid, "nominal entropy transfer is valid");
        AssertEx.True(nominal.TransferredDtu > 0f, "nominal transfer moves heat");
        AssertEx.Near(nominal.DtuRemovedFromHot, nominal.DtuAddedToCold, 0.01f,
            "entropy diverter conserves transferred energy");
        AssertEx.Near(10f, nominal.HotMassKg, 0.0001f, "hot mass is unchanged");
        AssertEx.Near(10f, nominal.ColdMassKg, 0.0001f, "cold mass is unchanged");
        AssertEx.True(nominal.HotOutputTemperatureK >= nominal.ColdOutputTemperatureK,
            "single transfer cannot invert hot and cold ordering");

        var equilibrium = EntropyFluxPolicy.Evaluate(
            10f, 4f, 373.15f, 273.15f, 473.15f,
            10f, 4f, 293.15f, 273.15f, 473.15f,
            100000000f);
        AssertEx.Near(equilibrium.HotOutputTemperatureK,
            equilibrium.ColdOutputTemperatureK, 0.001f,
            "oversized request clamps at thermal equilibrium");

        var hotPhaseGuard = EntropyFluxPolicy.Evaluate(
            1f, 1f, 275.15f, 273.15f, 500f,
            100f, 1f, 250f, 200f, 500f,
            10000000f);
        AssertEx.True(hotPhaseGuard.PhaseLimited, "hot lower phase boundary limits transfer");
        AssertEx.True(hotPhaseGuard.HotOutputTemperatureK >= 274.15f,
            "hot liquid keeps one kelvin lower-bound margin");

        var coldPhaseGuard = EntropyFluxPolicy.Evaluate(
            100f, 1f, 400f, 200f, 500f,
            1f, 1f, 398f, 200f, 400f,
            10000000f);
        AssertEx.True(coldPhaseGuard.PhaseLimited, "cold upper phase boundary limits transfer");
        AssertEx.True(coldPhaseGuard.ColdOutputTemperatureK <= 399f,
            "cold liquid keeps one kelvin upper-bound margin");

        var noGradient = EntropyFluxPolicy.Evaluate(
            10f, 4f, 300f, 250f, 400f,
            10f, 4f, 300f, 250f, 400f,
            1000000f);
        AssertEx.True(noGradient.IsValid, "equal-temperature streams are valid inputs");
        AssertEx.Near(0f, noGradient.TransferredDtu, 0.0001f,
            "equal-temperature streams transfer no heat");

        AssertEx.False(EntropyFluxPolicy.Evaluate(
            0f, 4f, 350f, 250f, 450f,
            10f, 4f, 300f, 250f, 450f,
            1000f).IsValid, "zero hot mass is rejected");
        AssertEx.False(EntropyFluxPolicy.Evaluate(
            10f, float.NaN, 350f, 250f, 450f,
            10f, 4f, 300f, 250f, 450f,
            1000f).IsValid, "nonfinite SHC is rejected");
        AssertEx.False(EntropyFluxPolicy.Evaluate(
            10f, 4f, 350f, 350f, 351f,
            10f, 4f, 300f, 250f, 450f,
            1000f).IsValid, "phase interval without two-kelvin safety room is rejected");

        AssertEx.Near(0.1f, EntropyFluxPolicy.ProtoMatterCostKg(4000000f, 1f),
            0.0001f, "four million DTU costs 0.1 kg Proto-Matter");
        AssertEx.Near(0.15f, EntropyFluxPolicy.ProtoMatterCostKg(4000000f, 1.5f),
            0.0001f, "cost multiplier scales catalyst cost");
        AssertEx.Near(0f, EntropyFluxPolicy.ProtoMatterCostKg(0f, 3f),
            0.0001f, "zero transfer consumes zero Proto-Matter");
        AssertEx.Throws<System.ArgumentOutOfRangeException>(delegate {
            EntropyFluxPolicy.ProtoMatterCostKg(-1f, 1f);
        }, "negative transferred energy is rejected");
    }
}
