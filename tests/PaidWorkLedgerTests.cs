using System;
using ForbiddenTechnologyPack.Core;

internal static class PaidWorkLedgerTests {
    public static void Run() {
        var ledger = new PaidWorkLedger();
        AssertEx.Near(0, ledger.TakeSeconds(.2f, 9600f), .00001f, "unpaid work is zero");
        ledger.AddPayment(960, 9600);
        AssertEx.Near(.1f, ledger.TakeSeconds(.2f, 9600f), .00001f, "partial payment scales work");
        AssertEx.Near(0, ledger.TakeSeconds(.2f, 9600f), .00001f, "a receipt cannot be spent twice");
        ledger.AddPayment(96000, 9600);
        AssertEx.Near(.2f, ledger.TakeSeconds(10, 9600f), .00001f, "credit is limited to one simulation tick");
        ledger.AddPayment(1920, 9600);ledger.Clear();
        AssertEx.Near(0, ledger.TakeSeconds(.2f, 9600f), .00001f, "paused or blocked work loses transient credit");
        foreach(var bad in new[]{0f,-1f,float.NaN,float.PositiveInfinity}) {
            ledger.AddPayment(bad, 9600);
            AssertEx.Near(0, ledger.TakeSeconds(.2f, 9600f), .00001f, "invalid receipt is ignored");
        }
        ledger.AddPayment(1920,9600);
        AssertEx.Near(0, ledger.TakeSeconds(float.NaN,9600), .00001f, "invalid duration never produces work");
        AssertEx.Near(0, ledger.TakeSeconds(.2f,0), .00001f, "invalid power never produces work");
        AssertEx.Near(.05f, ledger.TakeSeconds(.05f,9600), .00001f, "final partial batch only spends its remaining duration");
        AssertEx.Near(.15f, ledger.TakeSeconds(.2f,9600), .00001f, "unspent remainder is bounded and exact");
    }
}
