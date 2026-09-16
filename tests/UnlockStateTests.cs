using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;

internal static class UnlockStateTests {
    public static void Run() {
        var state = UnlockState.FromSerialized(0, new[] { "Iron", "Iron", "Niobium", "", "  " });
        AssertEx.Equal(1, state.Version, "migrated version");
        AssertEx.Equal(2, state.ElementIds.Count, "migration removes blank and duplicate ids");
        AssertEx.True(state.Unlock("Diamond"), "new unlock changed state");
        AssertEx.False(state.Unlock("Diamond"), "repeat unlock unchanged");
        AssertEx.False(state.Unlock("  "), "blank unlock unchanged");

        var active = state.ActiveUnlocked(new HashSet<string> { "Iron", "Diamond" });
        AssertEx.SequenceEqual(new[] { "Diamond", "Iron" }, active,
            "inactive dlc id retained but hidden");
        AssertEx.True(state.IsUnlocked("Niobium"), "inactive id retained");
        AssertEx.False(state.IsUnlocked("niobium"), "element ids use ordinal matching");

        AssertEx.SequenceEqual(new[] { "Diamond", "Iron", "Niobium" }, state.ToSerialized(),
            "serialization is ordinal sorted");

        var current = UnlockState.FromSerialized(1, new[] { "Steel", "Steel", "" });
        AssertEx.Equal(1, current.Version, "current version preserved");
        AssertEx.SequenceEqual(new[] { "Steel" }, current.ToSerialized(),
            "current save still normalizes persistence data");
    }
}
