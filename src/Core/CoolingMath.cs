using System;

namespace ForbiddenTechnologyPack.Core {
    public sealed class CoolantPacket {
        public CoolantPacket(float massKg, float temperatureKelvin, float highTransitionKelvin,
                float specificHeatKJPerKgK) {
            MassKg = massKg;
            TemperatureKelvin = temperatureKelvin;
            HighTransitionKelvin = highTransitionKelvin;
            SpecificHeatKJPerKgK = specificHeatKJPerKgK;
        }

        public float MassKg { get; private set; }
        public float TemperatureKelvin { get; private set; }
        public float HighTransitionKelvin { get; private set; }
        public float SpecificHeatKJPerKgK { get; private set; }
    }

    public sealed class CoolingDecision {
        internal CoolingDecision(bool canAcceptHeat, float outputKelvin) {
            CanAcceptHeat = canAcceptHeat;
            OutputKelvin = outputKelvin;
        }

        public bool CanAcceptHeat { get; private set; }
        public float OutputKelvin { get; private set; }
    }

    public static class CoolingMath {
        private const float TransitionMarginKelvin = 0.5f;

        public static CoolingDecision Evaluate(CoolantPacket packet, float heatJoules) {
            if (packet == null) {
                throw new ArgumentNullException("packet");
            }
            ValidatePositiveFinite(packet.MassKg, "packet.MassKg");
            ValidateFinite(packet.TemperatureKelvin, "packet.TemperatureKelvin");
            ValidateFinite(packet.HighTransitionKelvin, "packet.HighTransitionKelvin");
            ValidatePositiveFinite(packet.SpecificHeatKJPerKgK, "packet.SpecificHeatKJPerKgK");
            ValidateFinite(heatJoules, "heatJoules");
            if (heatJoules < 0f) {
                throw new ArgumentOutOfRangeException("heatJoules");
            }
            if (heatJoules == 0f) {
                return new CoolingDecision(true, packet.TemperatureKelvin);
            }

            var deltaKelvin = heatJoules /
                (packet.MassKg * packet.SpecificHeatKJPerKgK * 1000f);
            var outputKelvin = packet.TemperatureKelvin + deltaKelvin;
            if (!IsFinite(outputKelvin) || outputKelvin >= packet.HighTransitionKelvin - TransitionMarginKelvin) {
                return new CoolingDecision(false, packet.TemperatureKelvin);
            }

            return new CoolingDecision(true, outputKelvin);
        }

        private static void ValidatePositiveFinite(float value, string parameterName) {
            ValidateFinite(value, parameterName);
            if (value <= 0f) {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void ValidateFinite(float value, string parameterName) {
            if (!IsFinite(value)) {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static bool IsFinite(float value) {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
