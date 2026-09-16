using ForbiddenTechnologyPack.Core;

internal static class MaterialClassifierTests {
    public static void Run() {
        var strong = PackOptions.Resolve(new RawOptions { Preset = BalancePreset.Strong });

        var dirt = ElementDescriptor.Solid("Dirt", "Agricultural", "BuildableRaw");
        MaterialRule dirtRule;
        AssertEx.True(MaterialClassifier.TryCreateRule(dirt, strong, out dirtRule), "dirt allowed");
        AssertEx.Equal(MaterialTier.Common, dirtRule.Tier, "dirt tier");
        AssertEx.Near(1.25f, dirtRule.ProtoMatterPerKg, 0.0001f, "dirt cost");

        var copperOre = ElementDescriptor.Solid("Cuprite", "Metal", "MetalOre");
        AssertEx.Equal(MaterialTier.OreOrOrganic,
            MaterialClassifier.CreateRule(copperOre, strong).Tier, "ore tier");

        AssertEx.True(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Algae", "Organic"), strong, out _), "organic allowed");
        AssertEx.True(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Steel", "Industrial"), strong, out _), "industrial allowed");
        AssertEx.True(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Diamond", "Rare"), strong, out _), "rare allowed");
        AssertEx.True(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Thermium", "Endgame", "Rare"), strong, out _), "endgame allowed");

        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.SpecialSolid("Unobtanium"), strong, out _), "neutronium denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.NonSolid("Water"), strong, out _), "liquid denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("MushBar", "Food"), strong, out _), "food denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("MirthLeafSeed", "Seed"), strong, out _), "seed denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("HatchEgg", "Egg"), strong, out _), "egg denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Artifact", "Artifact"), strong, out _), "artifact denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("QuestThing", "QuestItem"), strong, out _), "quest denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Hatch", "Creature"), strong, out _), "creature denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Ignored", "Noncrushable"), strong, out _), "noncrushable denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Disabled", "Common", isStorable: false), strong, out _), "not storable denied");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("InactiveDlc", "Common", isActive: false), strong, out _), "inactive dlc denied");

        var noAdvanced = PackOptions.Resolve(new RawOptions {
            Preset = BalancePreset.Custom,
            AllowIndustrial = false,
            AllowRare = false,
            AllowEndgame = false
        });
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Steel", "Industrial"), noAdvanced, out _), "industrial switch");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Diamond", "Rare"), noAdvanced, out _), "rare switch");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Thermium", "Endgame"), noAdvanced, out _), "endgame switch");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Tungsten", "Endgame", "Rare", "Industrial", "Common"),
            PackOptions.Resolve(new RawOptions {
                Preset = BalancePreset.Custom,
                AllowIndustrial = true,
                AllowRare = true,
                AllowEndgame = false
            }), out _), "higher disabled tier does not downgrade");
        AssertEx.False(MaterialClassifier.TryCreateRule(
            ElementDescriptor.Solid("Mystery", "Metal"), strong, out _), "unknown tier denied");
    }
}
