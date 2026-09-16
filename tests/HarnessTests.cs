using System;

internal static class HarnessTests {
    public static void Run() {
        AssertEx.Near(1.0, 1.05, 0.1, "finite values within tolerance");
        AssertEx.Throws<ArgumentOutOfRangeException>(delegate { AssertEx.Near(double.NaN, 1.0, 0.1, "nan expected"); }, "nan expected is rejected");
        AssertEx.Throws<ArgumentOutOfRangeException>(delegate { AssertEx.Near(1.0, double.NaN, 0.1, "nan actual"); }, "nan actual is rejected");
        AssertEx.Throws<ArgumentOutOfRangeException>(delegate { AssertEx.Near(1.0, double.PositiveInfinity, 0.1, "infinite actual"); }, "infinite actual is rejected");
        AssertEx.Throws<ArgumentOutOfRangeException>(delegate { AssertEx.Near(1.0, 1.0, double.NaN, "nan tolerance"); }, "nan tolerance is rejected");
        AssertEx.Throws<ArgumentOutOfRangeException>(delegate { AssertEx.Near(1.0, 1.0, -0.1, "negative tolerance"); }, "negative tolerance is rejected");
    }
}
