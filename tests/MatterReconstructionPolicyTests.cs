using ForbiddenTechnologyPack.Core;

internal static class MatterReconstructionPolicyTests {
    public static void Run() {
        var nominal = MatterReconstructionPolicy.EvaluateMass(1000f, 100f, 1000f);
        AssertEx.True(nominal.IsValid, "nominal reconstruction mass is valid");
        AssertEx.True(nominal.IsMassSafe, "nominal reconstruction does not create mass");
        AssertEx.Near(1100f, nominal.TotalInputMass, 0.0001f, "nominal total input mass");
        AssertEx.Near(1000f, nominal.OutputMass, 0.0001f, "nominal output mass");
        AssertEx.Near(100f, nominal.UnreturnedMass, 0.0001f, "consumed proto-matter may leave the current material stream");

        var exact = MatterReconstructionPolicy.EvaluateMass(900f, 100f, 1000f);
        AssertEx.True(exact.IsMassSafe, "using all input mass is allowed");
        AssertEx.Near(0f, exact.UnreturnedMass, 0.0001f, "exact conversion has no unreturned mass");

        var over = MatterReconstructionPolicy.EvaluateMass(1000f, 100f, 1200f);
        AssertEx.True(over.IsValid, "finite positive over-output request is structurally valid");
        AssertEx.False(over.IsMassSafe, "reconstruction cannot create mass from nothing");

        AssertEx.False(MatterReconstructionPolicy.EvaluateMass(0f, 100f, 100f).IsValid,
            "source mass must be positive");
        AssertEx.False(MatterReconstructionPolicy.EvaluateMass(100f, 0f, 100f).IsValid,
            "proto-matter cost must be positive");
        AssertEx.False(MatterReconstructionPolicy.EvaluateMass(100f, 10f, 0f).IsValid,
            "output mass must be positive");
        AssertEx.False(MatterReconstructionPolicy.EvaluateMass(float.NaN, 10f, 10f).IsValid,
            "NaN source mass is rejected");
        AssertEx.False(MatterReconstructionPolicy.EvaluateMass(10f, float.PositiveInfinity, 10f).IsValid,
            "infinite proto-matter mass is rejected");
        AssertEx.False(MatterReconstructionPolicy.EvaluateMass(10f, 10f, float.NegativeInfinity).IsValid,
            "infinite output mass is rejected");
    }
}
