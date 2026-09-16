using System;
using ForbiddenTechnologyPack.Core;

internal static class SafeRemovalTests {
    public static void Run() {
        var report = new SafeRemovalReport();
        report.RecordConvertedObject(12.5f);
        report.RecordConvertedObject(7.5f);
        report.RecordReturnedInputs(3);
        report.RecordRemovedBuilding();
        report.RecordRemovedBuilding();
        report.Finish(0);

        AssertEx.Equal(2, report.ConvertedObjectCount, "converted object count");
        AssertEx.Near(20f, report.ConvertedMassKg, 0.0001f, "converted mass");
        AssertEx.Equal(3, report.ReturnedInputCount, "returned input count");
        AssertEx.Equal(2, report.RemovedBuildingCount, "removed building count");
        AssertEx.True(report.IsComplete, "zero remaining custom objects is complete");

        var incomplete = new SafeRemovalReport();
        incomplete.Finish(2);
        AssertEx.False(incomplete.IsComplete, "remaining custom objects block completion");
        AssertEx.Equal(2, incomplete.RemainingCustomObjectCount, "remaining object count retained");

        AssertEx.Throws<ArgumentOutOfRangeException>(() =>
            new SafeRemovalReport().RecordConvertedObject(float.NaN),
            "nonfinite converted mass is invalid");
    }
}
