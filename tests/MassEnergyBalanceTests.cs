using System;
using ForbiddenTechnologyPack.Core;

internal static class MassEnergyBalanceTests {
    public static void Run() {
        AssertEx.Near(48000, MassEnergyBalance.GrossWatts, .01, "reactor gross power");
        AssertEx.Near(40000, MassEnergyBalance.NetWatts, .01, "reactor net power");
        AssertEx.Near(12000000, MassEnergyBalance.TotalRecoveredJoulesPerKg, 1, "all electrical and thermal output counted");
        AssertEx.True(MassEnergyBalance.CondenseJoulesPerKg > MassEnergyBalance.TotalRecoveredJoulesPerKg, "even perfect heat recovery cannot amplify the loop");
        AssertEx.Near(500, MassEnergyBalance.CondenseSecondsPerKg, .01, "real paid batch duration");
        AssertEx.Near(12000, MassEnergyBalance.CondenserHeatWatts, .01, "condenser remainder is process heat");
        AssertEx.Near(.004, MassEnergyBalance.FuelForSeconds(.2), .000001, "native tick fuel");
        AssertEx.Near(8000, MassEnergyBalance.NetWatts * .2, .01, "tick net electricity");
        AssertEx.Near(40000, MassEnergyBalance.ReactorHeatWatts * .2, .01, "tick return heat includes operating energy");
        AssertEx.Near(0, MassEnergyBalance.FuelForSeconds(0), 0, "paused consumes no mass");
        AssertEx.Throws<ArgumentOutOfRangeException>(()=>MassEnergyBalance.FuelForSeconds(double.NaN), "NaN cannot become work");
        AssertEx.Throws<ArgumentOutOfRangeException>(()=>MassEnergyBalance.FuelForSeconds(-1), "negative work rejected");
        foreach (BalancePreset preset in Enum.GetValues(typeof(BalancePreset))) {
            var options = PackOptions.Resolve(new RawOptions { Preset = preset, CostMultiplier=.05f, HeatMultiplier=0f, PowerMultiplier=.1f });
            AssertEx.True(MassEnergyBalance.CondenseJoulesPerKg > MassEnergyBalance.TotalRecoveredJoulesPerKg, "material preset cannot bypass energy floor: "+preset);
        }
    }
}
