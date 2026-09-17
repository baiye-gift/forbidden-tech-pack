using ForbiddenTechnologyPack.Core;

internal static class ProtoMatterInterferencePolicyTests {
    public static void Run() {
        AssertEx.True(ProtoMatterInterferencePolicy.IsInsideRadius(0, 0, 3, 4, 5),
            "3-4-5 target is inside radius");
        AssertEx.False(ProtoMatterInterferencePolicy.IsInsideRadius(0, 0, 6, 0, 5),
            "target beyond radius is outside");
        AssertEx.True(ProtoMatterInterferencePolicy.IsInsideRadius(4, -2, 4, -2, 0),
            "source cell is inside zero radius");
        AssertEx.False(ProtoMatterInterferencePolicy.IsInsideRadius(0, 0, 1, 0, -1),
            "negative radius never matches");

        var state = new ProtoMatterInterferenceState();
        AssertEx.False(state.IsInterfered, "new receiver starts stable");
        AssertEx.Equal(0, state.ActiveSourceCount, "new receiver has no active sources");

        AssertEx.True(state.SetSource("reactor-a", true), "first source changes state");
        AssertEx.True(state.IsInterfered, "one source causes interference");
        AssertEx.Equal(1, state.ActiveSourceCount, "one source is tracked");

        AssertEx.False(state.SetSource("reactor-a", true), "duplicate activation is idempotent");
        AssertEx.Equal(1, state.ActiveSourceCount, "duplicate activation does not double count");

        AssertEx.True(state.SetSource("reactor-b", true), "second source changes state");
        AssertEx.Equal(2, state.ActiveSourceCount, "overlapping sources are tracked independently");

        AssertEx.True(state.SetSource("reactor-a", false), "first source can clear independently");
        AssertEx.True(state.IsInterfered, "second source keeps receiver interfered");
        AssertEx.Equal(1, state.ActiveSourceCount, "one overlapping source remains");

        AssertEx.False(state.SetSource("reactor-a", false), "duplicate removal is idempotent");
        AssertEx.True(state.SetSource("reactor-b", false), "last source can clear");
        AssertEx.False(state.IsInterfered, "receiver is stable after last source clears");
        AssertEx.Equal(0, state.ActiveSourceCount, "no sources remain");

        AssertEx.False(state.SetSource(null, true), "null source is ignored");
        AssertEx.False(state.SetSource(string.Empty, true), "empty source is ignored");
    }
}
