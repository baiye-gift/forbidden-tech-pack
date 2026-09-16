using System;
using ForbiddenTechnologyPack.Core;

internal static class CoolingMathTests {
    public static void Run() {
        var water = new CoolantPacket(10f, 293.15f, 373.15f, 4.179f);
        var safe = CoolingMath.Evaluate(water, 160000f);
        AssertEx.True(safe.CanAcceptHeat, "water accepts heat");
        AssertEx.Near(296.98f, safe.OutputKelvin, 0.02f, "water output temperature");

        var nearBoiling = new CoolantPacket(10f, 372.5f, 373.15f, 4.179f);
        AssertEx.False(CoolingMath.Evaluate(nearBoiling, 160000f).CanAcceptHeat,
            "packet may not cross transition margin");

        var unchanged = CoolingMath.Evaluate(water, 0f);
        AssertEx.True(unchanged.CanAcceptHeat, "zero heat is safe");
        AssertEx.Near(293.15f, unchanged.OutputKelvin, 0.0001f, "zero heat leaves temperature unchanged");

        AssertEx.Throws<ArgumentOutOfRangeException>(() =>
            CoolingMath.Evaluate(new CoolantPacket(0f, 293f, 373f, 4f), 1f),
            "zero mass is invalid");
        AssertEx.Throws<ArgumentOutOfRangeException>(() =>
            CoolingMath.Evaluate(new CoolantPacket(10f, 293f, 373f, 0f), 1f),
            "zero heat capacity is invalid");
        AssertEx.Throws<ArgumentOutOfRangeException>(() =>
            CoolingMath.Evaluate(new CoolantPacket(10f, float.NaN, 373f, 4f), 1f),
            "nonfinite packet data is invalid");
        AssertEx.Throws<ArgumentOutOfRangeException>(() =>
            CoolingMath.Evaluate(water, float.PositiveInfinity),
            "nonfinite heat is invalid");
    }
}
