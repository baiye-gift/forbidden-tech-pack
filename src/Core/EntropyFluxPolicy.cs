using System;

namespace ForbiddenTechnologyPack.Core {
    public sealed class EntropyFluxResult {
        internal EntropyFluxResult(bool isValid, float hotMassKg, float coldMassKg,
                float transferredDtu, float hotOutputTemperatureK,
                float coldOutputTemperatureK, bool phaseLimited) {
            IsValid = isValid;
            HotMassKg = hotMassKg;
            ColdMassKg = coldMassKg;
            TransferredDtu = transferredDtu;
            DtuRemovedFromHot = transferredDtu;
            DtuAddedToCold = transferredDtu;
            HotOutputTemperatureK = hotOutputTemperatureK;
            ColdOutputTemperatureK = coldOutputTemperatureK;
            PhaseLimited = phaseLimited;
        }

        public bool IsValid { get; private set; }
        public float HotMassKg { get; private set; }
        public float ColdMassKg { get; private set; }
        public float TransferredDtu { get; private set; }
        public float DtuRemovedFromHot { get; private set; }
        public float DtuAddedToCold { get; private set; }
        public float HotOutputTemperatureK { get; private set; }
        public float ColdOutputTemperatureK { get; private set; }
        public bool PhaseLimited { get; private set; }
    }

    public static class EntropyFluxPolicy {
        public const float PhaseMarginKelvin = 1f;
        public const float MaxTransferDtuPerBatch = 4000000f;
        public const float ProtoMatterKgPerMillionDtu = 0.025f;

        public static EntropyFluxResult Evaluate(
                float hotMassKg, float hotShcDtuPerGramKelvin, float hotTemperatureK,
                float hotLowTemperatureK, float hotHighTemperatureK,
                float coldMassKg, float coldShcDtuPerGramKelvin, float coldTemperatureK,
                float coldLowTemperatureK, float coldHighTemperatureK,
                float requestedDtu) {
            if (!PositiveFinite(hotMassKg) || !PositiveFinite(coldMassKg) ||
                    !PositiveFinite(hotShcDtuPerGramKelvin) ||
                    !PositiveFinite(coldShcDtuPerGramKelvin) ||
                    !Finite(hotTemperatureK) || !Finite(coldTemperatureK) ||
                    !Finite(hotLowTemperatureK) || !Finite(hotHighTemperatureK) ||
                    !Finite(coldLowTemperatureK) || !Finite(coldHighTemperatureK) ||
                    !Finite(requestedDtu) || requestedDtu < 0f ||
                    hotHighTemperatureK - hotLowTemperatureK <= 2f * PhaseMarginKelvin ||
                    coldHighTemperatureK - coldLowTemperatureK <= 2f * PhaseMarginKelvin) {
                return Invalid(hotMassKg, coldMassKg, hotTemperatureK, coldTemperatureK);
            }

            var hotCapacity = hotMassKg * 1000f * hotShcDtuPerGramKelvin;
            var coldCapacity = coldMassKg * 1000f * coldShcDtuPerGramKelvin;
            if (!PositiveFinite(hotCapacity) || !PositiveFinite(coldCapacity)) {
                return Invalid(hotMassKg, coldMassKg, hotTemperatureK, coldTemperatureK);
            }

            var boundedRequest = Math.Min(requestedDtu, MaxTransferDtuPerBatch);
            var gradient = hotTemperatureK - coldTemperatureK;
            if (boundedRequest <= 0f || gradient <= 0f) {
                return new EntropyFluxResult(true, hotMassKg, coldMassKg, 0f,
                    hotTemperatureK, coldTemperatureK, false);
            }

            var equilibriumLimit = gradient /
                ((1f / hotCapacity) + (1f / coldCapacity));
            var hotPhaseLimit = hotCapacity * Math.Max(0f,
                hotTemperatureK - (hotLowTemperatureK + PhaseMarginKelvin));
            var coldPhaseLimit = coldCapacity * Math.Max(0f,
                (coldHighTemperatureK - PhaseMarginKelvin) - coldTemperatureK);

            if (!Finite(equilibriumLimit) || !Finite(hotPhaseLimit) ||
                    !Finite(coldPhaseLimit)) {
                return Invalid(hotMassKg, coldMassKg, hotTemperatureK, coldTemperatureK);
            }

            var prePhaseLimit = Math.Min(boundedRequest, Math.Max(0f, equilibriumLimit));
            var transfer = Math.Min(prePhaseLimit,
                Math.Min(hotPhaseLimit, coldPhaseLimit));
            transfer = Math.Max(0f, transfer);
            var phaseLimited = transfer + 0.001f < prePhaseLimit;

            var hotOut = hotTemperatureK - transfer / hotCapacity;
            var coldOut = coldTemperatureK + transfer / coldCapacity;
            if (!Finite(hotOut) || !Finite(coldOut)) {
                return Invalid(hotMassKg, coldMassKg, hotTemperatureK, coldTemperatureK);
            }

            return new EntropyFluxResult(true, hotMassKg, coldMassKg, transfer,
                hotOut, coldOut, phaseLimited);
        }

        public static float ProtoMatterCostKg(float transferredDtu, float costMultiplier) {
            if (!Finite(transferredDtu) || transferredDtu < 0f) {
                throw new ArgumentOutOfRangeException("transferredDtu");
            }
            if (!PositiveFinite(costMultiplier)) {
                throw new ArgumentOutOfRangeException("costMultiplier");
            }
            return transferredDtu / 1000000f * ProtoMatterKgPerMillionDtu * costMultiplier;
        }

        private static EntropyFluxResult Invalid(float hotMassKg, float coldMassKg,
                float hotTemperatureK, float coldTemperatureK) {
            return new EntropyFluxResult(false, hotMassKg, coldMassKg, 0f,
                hotTemperatureK, coldTemperatureK, false);
        }

        private static bool PositiveFinite(float value) {
            return value > 0f && Finite(value);
        }

        private static bool Finite(float value) {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
