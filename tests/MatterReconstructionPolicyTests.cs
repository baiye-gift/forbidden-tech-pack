using System;
using ForbiddenTechnologyPack.Core;

internal static class MatterReconstructionPolicyTests {
    public static void Run() {
        var nominal = MatterReconstructionPolicy.EvaluateMass(1000f, 100f, 1000f);
        AssertEx.True(nominal.IsValid, "nominal reconstruction mass is valid");
        AssertEx.True(nominal.IsMassSafe, "nominal reconstruction does not create mass");
        AssertEx.Near(1100f, nominal.TotalInputMass, 0.0001f, "nominal total input mass");
        AssertEx.Near(1000f, nominal.OutputMass, 0.0001f, "nominal output mass");
        AssertEx.Near(100f, nominal.UnreturnedMass, 0.0001f,
            "consumed proto-matter leaves the ordinary material stream");

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

        AssertEx.Equal(MaterialTier.Common,
            MatterReconstructionPolicy.RequiredSubstrateTier(MaterialTier.Common),
            "common targets use common reality substrate");
        AssertEx.Equal(MaterialTier.Common,
            MatterReconstructionPolicy.RequiredSubstrateTier(MaterialTier.OreOrOrganic),
            "ore-or-organic targets use common reality substrate");
        AssertEx.Equal(MaterialTier.OreOrOrganic,
            MatterReconstructionPolicy.RequiredSubstrateTier(MaterialTier.Industrial),
            "industrial targets use ore-or-organic reality substrate");
        AssertEx.Equal(MaterialTier.Industrial,
            MatterReconstructionPolicy.RequiredSubstrateTier(MaterialTier.Rare),
            "rare targets use industrial reality substrate");
        AssertEx.Equal(MaterialTier.Rare,
            MatterReconstructionPolicy.RequiredSubstrateTier(MaterialTier.Endgame),
            "endgame targets use rare reality substrate");

        AssertEx.Near(50f, MatterReconstructionPolicy.BaseProtoMatterKg(MaterialTier.Common),
            0.0001f, "common base proto-matter cost");
        AssertEx.Near(75f, MatterReconstructionPolicy.BaseProtoMatterKg(MaterialTier.OreOrOrganic),
            0.0001f, "ore-or-organic base proto-matter cost");
        AssertEx.Near(100f, MatterReconstructionPolicy.BaseProtoMatterKg(MaterialTier.Industrial),
            0.0001f, "industrial base proto-matter cost");
        AssertEx.Near(200f, MatterReconstructionPolicy.BaseProtoMatterKg(MaterialTier.Rare),
            0.0001f, "rare base proto-matter cost");
        AssertEx.Near(300f, MatterReconstructionPolicy.BaseProtoMatterKg(MaterialTier.Endgame),
            0.0001f, "endgame base proto-matter cost");

        var options = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Custom,
            CostMultiplier = 1.5f
        });
        var target = MaterialRule.ForTier("Steel", MaterialTier.Industrial);
        var plan = MatterReconstructionPolicy.CreatePlan(target, options);
        AssertEx.Equal("Steel", plan.TargetElementId, "plan keeps target element id");
        AssertEx.Equal(MaterialTier.Industrial, plan.TargetTier, "plan keeps target tier");
        AssertEx.Equal(MaterialTier.OreOrOrganic, plan.SubstrateTier,
            "plan selects the required reality substrate tier");
        AssertEx.Near(1000f, plan.SubstrateKg, 0.0001f, "one reconstruction batch uses 1000 kg substrate");
        AssertEx.Near(150f, plan.ProtoMatterKg, 0.0001f,
            "proto-matter cost is scaled by CostMultiplier");
        AssertEx.Near(1000f, plan.ProductKg, 0.0001f,
            "proto-matter never contributes to product mass");
        AssertEx.Near(120f, plan.TimeSeconds, 0.0001f, "reconstruction batch time is fixed");

        var mass = MatterReconstructionPolicy.EvaluateMass(plan.SubstrateKg,
            plan.ProtoMatterKg, plan.ProductKg);
        AssertEx.True(mass.IsMassSafe, "generated reconstruction plan is mass-safe");
        AssertEx.Near(plan.ProtoMatterKg, mass.UnreturnedMass, 0.0001f,
            "the entire proto-matter charge is consumed to lever reality");

        AssertEx.Throws<ArgumentNullException>(delegate {
            MatterReconstructionPolicy.CreatePlan(null, options);
        }, "null target rule is rejected");
        AssertEx.Throws<ArgumentNullException>(delegate {
            MatterReconstructionPolicy.CreatePlan(target, null);
        }, "null options are rejected");
    }
}
