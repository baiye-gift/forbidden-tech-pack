using System;

namespace ForbiddenTechnologyPack.Core {
    // A transient one-tick credit. Saves keep completed work, never unspent power.
    public sealed class PaidWorkLedger {
        private float creditJoules;
        public void AddPayment(float joules, float watts) {
            if (!PositiveFinite(joules) || !PositiveFinite(watts)) return;
            creditJoules = Math.Min(watts * 0.2f, creditJoules + joules);
        }
        public float TakeSeconds(float requestedSeconds, float watts) {
            if (!PositiveFinite(requestedSeconds) || !PositiveFinite(watts)) return 0f;
            var seconds = Math.Min(requestedSeconds, creditJoules / watts);
            creditJoules = Math.Max(0f, creditJoules - seconds * watts);
            return seconds;
        }
        public void Clear() { creditJoules = 0f; }
        private static bool PositiveFinite(float v) {
            return v > 0f && !float.IsNaN(v) && !float.IsInfinity(v);
        }
    }
}
