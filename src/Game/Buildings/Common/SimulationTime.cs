using HarmonyLib;

namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    internal static class SimulationTime {
        internal static long PowerInterval { get; private set; }
        internal static void BeginPowerInterval() { PowerInterval++; }
        // GetTime() sums into a float and loses sub-second precision in old colonies.
        internal static double Now {
            get { return GameClock.Instance == null ? 0d :
                GameClock.Instance.GetCycle() * 600d + GameClock.Instance.GetTimeSinceStartOfCycle(); }
        }
    }
    [HarmonyPatch(typeof(CircuitManager), "Sim200msLast")]
    internal static class PowerIntervalPatch {
        private static void Prefix() { SimulationTime.BeginPowerInterval(); }
    }
}
