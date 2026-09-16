using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;

internal static class CompilerPolicyTests {
    public static void Run() {
        var visible = CompilerRecipeFilter.SelectIds(
            new[] { Rule("Iron"), Rule("Diamond"), Rule("Niobium") },
            UnlockState.FromSerialized(1, new[] { "Iron", "Niobium" }),
            new HashSet<string> { "Iron", "Diamond" });
        AssertEx.SequenceEqual(new[] { "Iron" }, visible, "unlocked active recipes only");

        var ordered = CompilerRecipeFilter.SelectIds(
            new[] { Rule("Niobium"), Rule("Iron"), Rule("Diamond") },
            UnlockState.FromSerialized(1, new[] { "Diamond", "Iron", "Niobium" }),
            new HashSet<string> { "Diamond", "Iron", "Niobium" });
        AssertEx.SequenceEqual(new[] { "Niobium", "Iron", "Diamond" }, ordered,
            "catalog ordering is preserved");

        AssertEx.Throws<ArgumentNullException>(() => CompilerRecipeFilter.SelectIds(
            null, UnlockState.FromSerialized(1, new string[0]), new HashSet<string>()),
            "rules are required");
        AssertEx.Throws<ArgumentNullException>(() => CompilerRecipeFilter.SelectIds(
            new MaterialRule[0], null, new HashSet<string>()),
            "unlock state is required");
        AssertEx.Throws<ArgumentNullException>(() => CompilerRecipeFilter.SelectIds(
            new MaterialRule[0], UnlockState.FromSerialized(1, new string[0]), null),
            "active recipe ids are required");

        AssertEx.True(CompilerRecipeFilter.HasOutputCapacity(100f, 100f),
            "an exact-size result batch fits");
        AssertEx.False(CompilerRecipeFilter.HasOutputCapacity(100f, 99.99f),
            "an entire result batch is required");
        AssertEx.Throws<ArgumentOutOfRangeException>(() =>
            CompilerRecipeFilter.HasOutputCapacity(float.NaN, 100f),
            "nonfinite result mass is invalid");
    }

    private static MaterialRule Rule(string id) {
        return MaterialRule.ForTier(id, MaterialTier.Common);
    }
}
