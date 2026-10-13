using System;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using KSerialization;
using HarmonyLib;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Buildings.Compiler {
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class CoolantController : KMonoBehaviour, ISim200ms {
        internal static readonly Operational.Flag SafeCoolant =
            new Operational.Flag("SafeCoolant", Operational.Flag.Type.Requirement);

        [Serialize]
        private bool releasingHeatedPacket;

        [MyCmpGet]
        private Operational operational = null;

        [MyCmpGet]
        private ConduitConsumer consumer = null;

        [MyCmpGet]
        private ConduitDispenser dispenser = null;

        public Storage storage;
        public Storage heatedStorage;
        public float heatJoulesPerSecond;
        private long transferInterval = -1;
        private float transferredKg;
        private double lastWorkTime;
        private double lastSmallReturnTime = double.NegativeInfinity;

        public bool IsReady { get { var packet = FindCoolant(); return packet != null && HasOutputSpace(packet) && Evaluate(packet, 0.2f)?.CanAcceptHeat == true; } }
        public string StateText { get { return FindCoolant() != null && !HasOutputSpace(FindCoolant()) ? STRINGS.UI.MATTER.COOLANT_DRAIN :
            FindCoolant() == null ? STRINGS.UI.MATTER.COOLANT_EMPTY : STRINGS.UI.MATTER.COOLANT_HOT; } }
        public string DetailText { get {
            var coolant = FindCoolant();
            return string.Format(STRINGS.UI.MATTER.COOLANT_HELP, storage == null ? 0f : storage.MassStored(),
                coolant == null ? "—" : GameUtil.GetFormattedTemperature(coolant.Temperature)) + "\n" +
                string.Format(STRINGS.UI.MATTER.HEATED_BUFFER, heatedStorage == null ? 0f : heatedStorage.MassStored());
        } }

        protected override void OnSpawn() {
            base.OnSpawn();
            lastWorkTime = SimulationTime.Now;
            // Upgrade the 0.1.0 saved latch: these are already heated packets.
            if (releasingHeatedPacket && storage != null && heatedStorage != null) {
                storage.Transfer(heatedStorage, true, true);
                releasingHeatedPacket = false;
            }
            if (consumer != null) consumer.SetOnState(true);
            if (dispenser != null) dispenser.SetOnState(true);
            RefreshSafeCoolantFlag(0.2f);
        }

        public void Sim200ms(float dt) {
            RefreshSafeCoolantFlag(0.2f);
            var coolant = FindCoolant();
            var decision = Evaluate(coolant, 0.2f);
            // Flushing is a transport operation, never a side effect of a status read.
            if (coolant != null && decision != null && !decision.CanAcceptHeat && heatedStorage != null) {
                var amount = Math.Min(coolant.Mass, Math.Min(TransferAllowance(dt), heatedStorage.RemainingCapacity()));
                if (amount > 0f)
                    transferredKg += storage.Transfer(heatedStorage, coolant.Element.tag, amount, true, true);
                RefreshSafeCoolantFlag(0.2f);
            }
        }

        internal bool TryProcessWork(float seconds) {
            if (seconds <= 0f || float.IsNaN(seconds) ||
                    float.IsInfinity(seconds)) {
                return false;
            }
            var coolant = FindCoolant();
            var decision = Evaluate(coolant, seconds);
            if (decision == null || !decision.CanAcceptHeat || !HasOutputSpace(coolant, seconds)) {
                SetSafeCoolant(false);
                return false;
            }
            var previousTemperature = coolant.Temperature;
            var amount = Math.Min(coolant.Mass, 10f * seconds);
            if (TransferAllowance(seconds) + .000001f < amount) return false;
            // Warm the actual reservoir before splitting: the remaining liquid retains
            // its share of paid heat, and native Store merges the outgoing share correctly.
            coolant.Temperature = decision.OutputKelvin;
            var moved = storage.Transfer(heatedStorage, coolant.Element.tag, amount, true, true);
            if (moved <= 0f) {
                coolant.Temperature = previousTemperature;
                return false;
            }
            transferredKg += moved;
            lastWorkTime = SimulationTime.Now;
            RefreshSafeCoolantFlag(0.2f);
            return true;
        }

        private void RefreshSafeCoolantFlag(float dt) {
            var coolant = FindCoolant();
            var decision = Evaluate(coolant, dt);
            SetSafeCoolant(decision != null && decision.CanAcceptHeat && HasOutputSpace(coolant));
        }

        private float TransferAllowance(float seconds) {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds) || GameClock.Instance == null)
                return 0f;
            // Use the actual native 200 ms electricity epoch, not a rounded float
            // clock bucket. Load-balanced callbacks share this same allowance.
            var interval = SimulationTime.PowerInterval;
            if (interval != transferInterval) { transferInterval = interval; transferredKg = 0f; }
            return Math.Min(10f * Math.Min(seconds, .2f), Math.Max(0f, 2f - transferredKg));
        }

        internal bool CanReleaseReturn(PrimaryElement packet) {
            return packet != null && heatedStorage != null && OutputBatchPolicy.CanReleaseLiquid(packet.Mass,
                heatedStorage.RemainingCapacity(), SimulationTime.Now - lastWorkTime, SimulationTime.Now - lastSmallReturnTime);
        }
        internal void ObserveReturn(float previousMass) {
            if (heatedStorage == null) return;
            var released = previousMass - heatedStorage.MassStored();
            if (released > 0f && released < OutputBatchPolicy.LiquidBatchKg)
                lastSmallReturnTime = SimulationTime.Now;
        }

        private CoolingDecision Evaluate(PrimaryElement coolant, float dt) {
            if (coolant == null || coolant.Element == null || !coolant.Element.IsLiquid ||
                    coolant.Mass <= 0f) {
                return null;
            }

            try {
                return CoolingMath.Evaluate(new CoolantPacket(coolant.Mass, coolant.Temperature,
                    coolant.Element.highTemp, coolant.Element.specificHeatCapacity),
                    heatJoulesPerSecond * Math.Max(0f, dt));
            } catch (ArgumentOutOfRangeException) {
                return null;
            }
        }

        private PrimaryElement FindCoolant() {
            if (storage == null) {
                return null;
            }
            for (var index = 0; index < storage.items.Count; index++) {
                var item = storage.items[index];
                var primary = item == null ? null : item.GetComponent<PrimaryElement>();
                if (primary != null && primary.Mass > 0f && primary.Element != null &&
                        primary.Element.IsLiquid) {
                    return primary;
                }
            }
            return null;
        }

        private bool HasOutputSpace(PrimaryElement packet, float seconds = 0.2f) {
            return packet != null && heatedStorage != null &&
                heatedStorage.RemainingCapacity() >= Math.Min(packet.Mass, 10f * seconds);
        }

        private void SetSafeCoolant(bool safe) {
            if (operational != null && operational.GetFlag(SafeCoolant) != safe) {
                operational.SetFlag(SafeCoolant, safe);
            }
            // FabricatorStatus owns the contextual message (empty, hot, or blocked).
        }
    }

    [HarmonyPatch(typeof(ConduitDispenser), "FindSuitableElement")]
    internal static class CoolantReturnBatchPatch {
        private static void Postfix(ConduitDispenser __instance, ref PrimaryElement __result) {
            var coolant = __instance.GetComponent<CoolantController>();
            if (coolant != null && __instance.storage == coolant.heatedStorage && !coolant.CanReleaseReturn(__result))
                __result = null;
        }
    }
    [HarmonyPatch(typeof(ConduitDispenser), "Dispense")]
    internal static class CoolantReturnObservationPatch {
        private static void Prefix(ConduitDispenser __instance, out float __state) {
            var coolant = __instance.GetComponent<CoolantController>();
            __state = coolant != null && __instance.storage == coolant.heatedStorage ? coolant.heatedStorage.MassStored() : -1f;
        }
        private static void Postfix(ConduitDispenser __instance, float __state) {
            var coolant = __instance.GetComponent<CoolantController>();
            if (__state >= 0f && coolant != null) coolant.ObserveReturn(__state);
        }
    }
}
