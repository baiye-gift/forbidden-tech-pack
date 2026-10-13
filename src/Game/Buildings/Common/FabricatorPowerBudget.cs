using System;
using System.Collections.Generic;
using System.Reflection;
using ForbiddenTechnologyPack.Core;
using HarmonyLib;

namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    public sealed class FabricatorPowerBudget : KMonoBehaviour, ISim200ms {
        internal static readonly Dictionary<int, FabricatorPowerBudget> Live =
            new Dictionary<int, FabricatorPowerBudget>();
        private readonly PaidWorkLedger ledger = new PaidWorkLedger();
        private EnergyConsumer consumer;
        private Operational operational;
        public bool IsSettling { get; private set; }

        protected override void OnSpawn() {
            base.OnSpawn();
            consumer = GetComponent<EnergyConsumer>();
            operational = GetComponent<Operational>();
            if (consumer != null) Live[consumer.GetInstanceID()] = this;
        }
        protected override void OnCleanUp() {
            if (consumer != null) Live.Remove(consumer.GetInstanceID());
            ledger.Clear();
            base.OnCleanUp();
        }
        public void Sim200ms(float dt) {
            if (operational == null || !operational.IsOperational || !operational.IsActive)
                Clear();
        }
        public float TakeWorkSeconds(float dt) {
            return !IsSettling || consumer == null ? 0f : ledger.TakeSeconds(dt, consumer.WattsNeededWhenActive);
        }
        public void Clear() { ledger.Clear(); }
        internal void RecordPayment(float joules) {
            if (consumer == null || joules <= 0f || float.IsNaN(joules) || float.IsInfinity(joules)) return;
            ledger.AddPayment(joules, consumer.WattsNeededWhenActive);
        }

        // CircuitManager has recorded all receipts before it announces Unpowered.
        // Commit that already paid slice while the old Powered flag is still valid;
        // the native method then applies its ordinary brownout and recovery rules.
        internal void SettlePayment() {
            if (operational == null || !operational.IsOperational || !operational.IsActive) {
                Clear(); return;
            }
            IsSettling = true;
            try {
                var reactor = GetComponent<MassEnergy.ProtoAnnihilationReactor>();
                if (reactor != null) reactor.Sim200ms(.2f);
                else {
                    var fabricator = GetComponent<ComplexFabricator>();
                    if (fabricator != null) fabricator.Sim200ms(.2f);
                }
            } finally {
                IsSettling = false;
                // A receipt belongs to this settlement, never the next order or tick.
                Clear();
            }
        }
    }

    // Observe the native payment receipts. The returned value is UNPAID energy.
    [HarmonyPatch]
    internal static class FabricatorPaymentPatch {
        private static IEnumerable<MethodBase> TargetMethods() {
            yield return AccessTools.Method(typeof(CircuitManager), "PowerFromGenerator");
            yield return AccessTools.Method(typeof(CircuitManager), "PowerFromBatteries");
        }
        private static void Prefix(float joules_needed, out float __state) { __state = joules_needed; }
        private static void Postfix(IEnergyConsumer c, float __result, float __state) {
            var consumer = c as EnergyConsumer;
            FabricatorPowerBudget budget;
            if (consumer != null && FabricatorPowerBudget.Live.TryGetValue(consumer.GetInstanceID(), out budget))
                budget.RecordPayment(Math.Max(0f, __state - __result));
        }
    }

    [HarmonyPatch(typeof(EnergyConsumer), nameof(EnergyConsumer.SetConnectionStatus))]
    internal static class FabricatorBrownoutPatch {
        private static void Prefix(EnergyConsumer __instance, CircuitManager.ConnectionStatus connection_status) {
            FabricatorPowerBudget budget;
            if (connection_status == CircuitManager.ConnectionStatus.Unpowered &&
                    FabricatorPowerBudget.Live.TryGetValue(__instance.GetInstanceID(), out budget))
                budget.SettlePayment();
        }
        private static void Postfix(EnergyConsumer __instance, CircuitManager.ConnectionStatus connection_status) {
            FabricatorPowerBudget budget;
            if (connection_status == CircuitManager.ConnectionStatus.Powered &&
                    FabricatorPowerBudget.Live.TryGetValue(__instance.GetInstanceID(), out budget))
                budget.SettlePayment();
        }
    }
}
