using System;
using ForbiddenTechnologyPack.Core;
using KSerialization;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Buildings.Compiler {
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class CoolantController : KMonoBehaviour, ISim1000ms {
        internal static readonly Operational.Flag SafeCoolant =
            new Operational.Flag("SafeCoolant", Operational.Flag.Type.Requirement);

        private static StatusItem unsafeCoolantStatus;

        [Serialize]
        private bool releasingHeatedPacket;

        [MyCmpGet]
        private Operational operational = null;

        [MyCmpGet]
        private ConduitConsumer consumer = null;

        [MyCmpGet]
        private ConduitDispenser dispenser = null;

        public Storage storage;
        public float heatJoulesPerSecond;

        protected override void OnSpawn() {
            base.OnSpawn();
            EnsureStatusItem();
            ApplyFlowState();
            RefreshSafeCoolantFlag(1f, false);
        }

        protected override void OnCleanUp() {
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt) {
            if (releasingHeatedPacket && !HasCoolantMass()) {
                releasingHeatedPacket = false;
                ApplyFlowState();
            }

            if (releasingHeatedPacket) {
                SetSafeCoolant(false);
                return;
            }

            var coolant = FindCoolant();
            var decision = Evaluate(coolant, dt);
            var canAcceptHeat = decision != null && decision.CanAcceptHeat;
            SetSafeCoolant(canAcceptHeat);
            if (!canAcceptHeat || operational == null || !operational.IsActive) {
                return;
            }

            // A packet changes temperature only after every precondition succeeds. It then remains
            // sealed in this saved storage until the output conduit accepts the complete mass.
            coolant.Temperature = decision.OutputKelvin;
            releasingHeatedPacket = true;
            ApplyFlowState();
            SetSafeCoolant(false);
        }

        private void RefreshSafeCoolantFlag(float dt, bool requireActive) {
            if (releasingHeatedPacket) {
                SetSafeCoolant(false);
                return;
            }
            if (requireActive && (operational == null || !operational.IsActive)) {
                return;
            }

            var decision = Evaluate(FindCoolant(), dt);
            SetSafeCoolant(decision != null && decision.CanAcceptHeat);
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

        private bool HasCoolantMass() {
            return storage != null && storage.MassStored() > 0.0001f;
        }

        private void ApplyFlowState() {
            if (consumer != null) {
                consumer.SetOnState(!releasingHeatedPacket);
            }
            if (dispenser != null) {
                dispenser.SetOnState(releasingHeatedPacket);
            }
        }

        private void SetSafeCoolant(bool safe) {
            if (operational != null && operational.GetFlag(SafeCoolant) != safe) {
                operational.SetFlag(SafeCoolant, safe);
            }
            var selectable = GetComponent<KSelectable>();
            if (selectable != null) {
                selectable.ToggleStatusItem(unsafeCoolantStatus, !safe, this);
            }
        }

        private static void EnsureStatusItem() {
            if (unsafeCoolantStatus == null) {
                unsafeCoolantStatus = new StatusItem("BaiyeMatterCompilerUnsafeCoolant",
                    STRINGS.BUILDING.STATUSITEMS.BAIYEMATTERCOMPILERUNSAFECOOLANT.NAME,
                    STRINGS.BUILDING.STATUSITEMS.BAIYEMATTERCOMPILERUNSAFECOOLANT.TOOLTIP,
                    "status_item_no_liquid_to_pump", StatusItem.IconType.Custom,
                    NotificationType.BadMinor, false, OverlayModes.LiquidConduits.ID);
            }
        }
    }
}
