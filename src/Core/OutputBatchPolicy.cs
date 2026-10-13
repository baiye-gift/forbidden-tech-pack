using System;

namespace ForbiddenTechnologyPack.Core {
    public static class OutputBatchPolicy {
        public const float LiquidBatchKg = 5f;
        public const double TailDelaySeconds = 2d;
        public static bool CanReleaseLiquid(float massKg, float remainingKg, double idleSeconds, double sinceSmallOutput) {
            if (massKg <= 0f || float.IsNaN(massKg) || float.IsInfinity(massKg)) return false;
            if (sinceSmallOutput < TailDelaySeconds) return false;
            if (massKg >= LiquidBatchKg) return true;
            // Full mixed-element buffers must be able to drain without deadlocking.
            return remainingKg < .001f || idleSeconds >= TailDelaySeconds;
        }
    }
}
