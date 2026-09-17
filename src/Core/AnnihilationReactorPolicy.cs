using System;

namespace ForbiddenTechnologyPack.Core {
    public enum ReactorState {
        Offline,
        Charging,
        Stable,
        Fluctuating,
        Critical,
        Decohered,
        CoolingLockout
    }

    public static class AnnihilationReactorPolicy {
        public const float ChargeSeconds = 30f;
        public const float StableFaultGraceSeconds = 5f;
        public const float FluctuationRecoverySeconds = 10f;
        public const float FluctuationEscalationSeconds = 15f;
        public const float CriticalRecoverySeconds = 5f;
        public const float CriticalDecoherenceSeconds = 10f;
        public const float LockoutSeconds = 30f;
        public const float DecoherenceLossFraction = 0.35f;
        public const float DecoherenceHeatDtuPerKgLost = 20000000f;
        public const int DecoherenceInterferenceRadiusCells = 12;
        public const float DecoherenceInterferenceSeconds = 60f;

        public static ReactorState Next(ReactorState state, bool healthy,
                bool startRequested, bool stopRequested,
                float stateElapsedSeconds, float conditionElapsedSeconds) {
            ValidateDuration(stateElapsedSeconds, "stateElapsedSeconds");
            ValidateDuration(conditionElapsedSeconds, "conditionElapsedSeconds");

            switch (state) {
                case ReactorState.Offline:
                    return startRequested && !stopRequested && healthy
                        ? ReactorState.Charging : ReactorState.Offline;

                case ReactorState.Charging:
                    if (stopRequested || !healthy || !startRequested) {
                        return ReactorState.Offline;
                    }
                    return stateElapsedSeconds >= ChargeSeconds
                        ? ReactorState.Stable : ReactorState.Charging;

                case ReactorState.Stable:
                    if (stopRequested || !startRequested) {
                        return ReactorState.CoolingLockout;
                    }
                    if (!healthy && conditionElapsedSeconds >= StableFaultGraceSeconds) {
                        return ReactorState.Fluctuating;
                    }
                    return ReactorState.Stable;

                case ReactorState.Fluctuating:
                    if (stopRequested || !startRequested) {
                        return ReactorState.CoolingLockout;
                    }
                    if (healthy && conditionElapsedSeconds >= FluctuationRecoverySeconds) {
                        return ReactorState.Stable;
                    }
                    if (!healthy && conditionElapsedSeconds >= FluctuationEscalationSeconds) {
                        return ReactorState.Critical;
                    }
                    return ReactorState.Fluctuating;

                case ReactorState.Critical:
                    if (stopRequested || !startRequested) {
                        return ReactorState.CoolingLockout;
                    }
                    if (healthy && conditionElapsedSeconds >= CriticalRecoverySeconds) {
                        return ReactorState.CoolingLockout;
                    }
                    if (!healthy && conditionElapsedSeconds >= CriticalDecoherenceSeconds) {
                        return ReactorState.Decohered;
                    }
                    return ReactorState.Critical;

                case ReactorState.Decohered:
                    return ReactorState.CoolingLockout;

                case ReactorState.CoolingLockout:
                    return healthy && stateElapsedSeconds >= LockoutSeconds
                        ? ReactorState.Offline : ReactorState.CoolingLockout;

                default:
                    throw new ArgumentOutOfRangeException("state");
            }
        }

        public static float CalculateProtoMatterLoss(float availableKg) {
            if (!Finite(availableKg) || availableKg < 0f) {
                throw new ArgumentOutOfRangeException("availableKg");
            }
            return availableKg * DecoherenceLossFraction;
        }

        public static float CalculateHeatPulseDtu(float lostProtoMatterKg,
                float heatMultiplier) {
            if (!Finite(lostProtoMatterKg) || lostProtoMatterKg < 0f) {
                throw new ArgumentOutOfRangeException("lostProtoMatterKg");
            }
            if (!Finite(heatMultiplier) || heatMultiplier <= 0f) {
                throw new ArgumentOutOfRangeException("heatMultiplier");
            }
            var result = lostProtoMatterKg * DecoherenceHeatDtuPerKgLost * heatMultiplier;
            if (!Finite(result) || result < 0f) {
                throw new ArgumentOutOfRangeException("lostProtoMatterKg");
            }
            return result;
        }

        public static bool IsDecoherenceEntry(ReactorState previous, ReactorState next) {
            return previous != ReactorState.Decohered && next == ReactorState.Decohered;
        }

        private static void ValidateDuration(float value, string parameterName) {
            if (!Finite(value) || value < 0f) {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static bool Finite(float value) {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
