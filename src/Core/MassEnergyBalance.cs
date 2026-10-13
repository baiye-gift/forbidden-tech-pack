using System;
namespace ForbiddenTechnologyPack.Core {
    public static class MassEnergyBalance {
        // Game-scaled joules/kDTU ledger; this is not a literal c^2 yield.
        public const float GrossWatts = 48000f;
        public const float OperatingWatts = 8000f;
        public const float NetWatts = GrossWatts - OperatingWatts;
        public const float ReactorHeatWatts = 200000f;
        public const float FuelKgPerSecond = .02f;
        public const float CondenserWatts = 36000f;
        public const float CondenserHeatWatts = 12000f;
        public const float IgnitionJoules = 600000f;
        public const float GeneratorCapacityJoules = 96000f;
        public const double CondenseJoulesPerKg = 18000000d;
        public static double TotalRecoveredJoulesPerKg {
            get { return (NetWatts + ReactorHeatWatts) / (double)FuelKgPerSecond; }
        }
        public static double CondenseSecondsPerKg { get { return CondenseJoulesPerKg / CondenserWatts; } }
        public static double FuelForSeconds(double seconds) {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            return seconds * FuelKgPerSecond;
        }
    }
}
