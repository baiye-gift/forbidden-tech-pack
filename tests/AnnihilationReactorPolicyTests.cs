using ForbiddenTechnologyPack.Core;

internal static class AnnihilationReactorPolicyTests {
    public static void Run() {
        AssertEx.Equal(ReactorState.Offline,
            AnnihilationReactorPolicy.Next(ReactorState.Offline, true, false, false, 0f, 0f),
            "offline reactor stays offline without start request");
        AssertEx.Equal(ReactorState.Charging,
            AnnihilationReactorPolicy.Next(ReactorState.Offline, true, true, false, 0f, 0f),
            "healthy start request begins charging");
        AssertEx.Equal(ReactorState.Offline,
            AnnihilationReactorPolicy.Next(ReactorState.Offline, false, true, false, 0f, 0f),
            "unhealthy reactor cannot begin charging");

        AssertEx.Equal(ReactorState.Charging,
            AnnihilationReactorPolicy.Next(ReactorState.Charging, true, true, false, 29.9f, 29.9f),
            "charging requires full thirty seconds");
        AssertEx.Equal(ReactorState.Stable,
            AnnihilationReactorPolicy.Next(ReactorState.Charging, true, true, false, 30f, 30f),
            "charging completes into stable state");
        AssertEx.Equal(ReactorState.Offline,
            AnnihilationReactorPolicy.Next(ReactorState.Charging, false, true, false, 12f, 1f),
            "charging loses confinement when prerequisites fail");

        AssertEx.Equal(ReactorState.Stable,
            AnnihilationReactorPolicy.Next(ReactorState.Stable, false, true, false, 300f, 4.9f),
            "new stable-state fault gets a five-second grace period");
        AssertEx.Equal(ReactorState.Fluctuating,
            AnnihilationReactorPolicy.Next(ReactorState.Stable, false, true, false, 305f, 5f),
            "five seconds of stable-state fault enters fluctuation");
        AssertEx.Equal(ReactorState.CoolingLockout,
            AnnihilationReactorPolicy.Next(ReactorState.Stable, true, true, true, 50f, 50f),
            "automation stop requests orderly lockout");

        AssertEx.Equal(ReactorState.Stable,
            AnnihilationReactorPolicy.Next(ReactorState.Fluctuating, true, true, false, 30f, 10f),
            "ten healthy seconds recover fluctuation to stable");
        AssertEx.Equal(ReactorState.Fluctuating,
            AnnihilationReactorPolicy.Next(ReactorState.Fluctuating, false, true, false, 14.9f, 14.9f),
            "fluctuation remains before fifteen fault seconds");
        AssertEx.Equal(ReactorState.Critical,
            AnnihilationReactorPolicy.Next(ReactorState.Fluctuating, false, true, false, 15f, 15f),
            "fifteen fault seconds escalate to critical");

        AssertEx.Equal(ReactorState.CoolingLockout,
            AnnihilationReactorPolicy.Next(ReactorState.Critical, true, true, false, 7f, 5f),
            "critical state can emergency-shutdown after five healthy seconds");
        AssertEx.Equal(ReactorState.Critical,
            AnnihilationReactorPolicy.Next(ReactorState.Critical, false, true, false, 9.9f, 9.9f),
            "critical reactor holds before ten fault seconds");
        AssertEx.Equal(ReactorState.Decohered,
            AnnihilationReactorPolicy.Next(ReactorState.Critical, false, true, false, 10f, 10f),
            "ten critical fault seconds trigger decoherence");
        AssertEx.Equal(ReactorState.CoolingLockout,
            AnnihilationReactorPolicy.Next(ReactorState.Decohered, false, true, false, 0f, 0f),
            "decohered state advances to cooling lockout after entry effects");

        AssertEx.Equal(ReactorState.CoolingLockout,
            AnnihilationReactorPolicy.Next(ReactorState.CoolingLockout, true, false, false, 29.9f, 29.9f),
            "lockout lasts at least thirty seconds");
        AssertEx.Equal(ReactorState.Offline,
            AnnihilationReactorPolicy.Next(ReactorState.CoolingLockout, true, false, false, 30f, 30f),
            "safe thirty-second lockout returns offline");
        AssertEx.Equal(ReactorState.CoolingLockout,
            AnnihilationReactorPolicy.Next(ReactorState.CoolingLockout, false, false, false, 100f, 100f),
            "unsafe reactor cannot leave lockout");

        AssertEx.Near(3.5f, AnnihilationReactorPolicy.CalculateProtoMatterLoss(10f),
            0.0001f, "decoherence loses thirty-five percent of available Proto-Matter");
        AssertEx.Near(0f, AnnihilationReactorPolicy.CalculateProtoMatterLoss(0f),
            0.0001f, "empty reactor loses no Proto-Matter");
        AssertEx.Near(70000000f,
            AnnihilationReactorPolicy.CalculateHeatPulseDtu(3.5f, 1f), 1f,
            "heat pulse is proportional to actual lost Proto-Matter");
        AssertEx.Near(140000000f,
            AnnihilationReactorPolicy.CalculateHeatPulseDtu(3.5f, 2f), 1f,
            "heat multiplier scales decoherence pulse");
        AssertEx.True(AnnihilationReactorPolicy.IsDecoherenceEntry(
            ReactorState.Critical, ReactorState.Decohered),
            "critical-to-decohered is a one-shot event boundary");
        AssertEx.False(AnnihilationReactorPolicy.IsDecoherenceEntry(
            ReactorState.Decohered, ReactorState.CoolingLockout),
            "leaving decohered must not replay entry effects");

        AssertEx.Throws<System.ArgumentOutOfRangeException>(delegate {
            AnnihilationReactorPolicy.Next(ReactorState.Stable, true, true, false, -1f, 0f);
        }, "negative state duration is rejected");
        AssertEx.Throws<System.ArgumentOutOfRangeException>(delegate {
            AnnihilationReactorPolicy.CalculateProtoMatterLoss(float.NaN);
        }, "nonfinite available Proto-Matter is rejected");
        AssertEx.Throws<System.ArgumentOutOfRangeException>(delegate {
            AnnihilationReactorPolicy.CalculateHeatPulseDtu(1f, 0f);
        }, "nonpositive heat multiplier is rejected");
    }
}
