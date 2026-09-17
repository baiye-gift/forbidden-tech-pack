using System;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Options;
using KSerialization;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Buildings.AnnihilationReactor {
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class MatterAnnihilationReactorController : KMonoBehaviour, ISim1000ms {
        private const float EmptyThresholdKg = 0.0001f;
        private const float StabilityTemperatureK = 493.15f;
        private const float GrossGenerationWatts = 40000f;

        [Serialize]
        private ReactorState state = ReactorState.Offline;

        [Serialize]
        private float stateElapsedSeconds;

        [Serialize]
        private float conditionElapsedSeconds;

        [Serialize]
        private bool conditionHealthy;

        [Serialize]
        private bool conditionInitialized;

        [Serialize]
        private bool decoherenceConsequencesApplied;

        [Serialize]
        private float interferenceSecondsRemaining;

        [Serialize]
        private bool processedCoolant;

        [MyCmpGet]
        private Operational operational = null;

        [MyCmpGet]
        private EnergyConsumer energyConsumer = null;

        [MyCmpGet]
        private ForbiddenTechDevice forbiddenDevice = null;

        [MyCmpGet]
        private PrimaryElement primaryElement = null;

        public Storage coolantStorage;
        public Storage protoMatterStorage;
        public ConduitConsumer coolantConsumer;
        public ConduitDispenser coolantDispenser;
        public Generator generator;

        private string interferenceSourceId;

        protected override void OnSpawn() {
            base.OnSpawn();
            interferenceSourceId = "annihilation-reactor:" + GetInstanceID();
            ApplyCoolantFlowState();
            if (interferenceSecondsRemaining > 0f) {
                ApplyOwnedInterference();
            }
            UpdateOperationalActivity();
        }

        protected override void OnCleanUp() {
            RemoveOwnedInterference();
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt) {
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) {
                return;
            }

            UpdateOwnedInterference(dt);
            RefreshCoolantBatch();

            var logicEnabled = operational == null ||
                operational.GetFlag(LogicOperationalController.LogicOperationalFlag);
            var powered = energyConsumer != null && energyConsumer.IsPowered;
            var healthy = EvaluateHealthy(dt, powered, logicEnabled);

            stateElapsedSeconds += dt;
            UpdateConditionTimer(healthy, dt);

            var previous = state;
            var next = AnnihilationReactorPolicy.Next(
                state, healthy, logicEnabled, !logicEnabled,
                stateElapsedSeconds, conditionElapsedSeconds);
            if (next != state) {
                EnterState(previous, next);
            }

            UpdateOperationalActivity();

            if (state == ReactorState.Stable && healthy) {
                RunStableReaction(dt);
            } else if (state != ReactorState.Stable && generator != null) {
                generator.ResetJoules();
            }
        }

        internal void PrepareForSafeRemoval() {
            state = ReactorState.Offline;
            stateElapsedSeconds = 0f;
            conditionElapsedSeconds = 0f;
            conditionInitialized = false;
            interferenceSecondsRemaining = 0f;
            RemoveOwnedInterference();
            if (generator != null) {
                generator.ResetJoules();
            }
            if (operational != null) {
                operational.SetActive(false);
            }
            if (coolantConsumer != null) {
                coolantConsumer.SetOnState(false);
            }
            if (coolantDispenser != null) {
                coolantDispenser.SetOnState(false);
            }
        }

        private bool EvaluateHealthy(float dt, bool powered, bool logicEnabled) {
            if (!powered || !logicEnabled ||
                    (forbiddenDevice != null && forbiddenDevice.IsInterfered)) {
                return false;
            }
            if (primaryElement != null && primaryElement.Temperature > StabilityTemperatureK) {
                return false;
            }

            var protoMatterRequired =
                0.2f * ForbiddenTechOptions.Current.CostMultiplier * dt;
            if (protoMatterStorage == null ||
                    protoMatterStorage.GetMassAvailable(ProtoMatterRegistration.Tag) +
                    EmptyThresholdKg < protoMatterRequired) {
                return false;
            }

            return CanCoolantAbsorb(
                1200000f * ForbiddenTechOptions.Current.HeatMultiplier * dt);
        }

        private bool CanCoolantAbsorb(float heatDtu) {
            if (processedCoolant || heatDtu < 0f || float.IsNaN(heatDtu) ||
                    float.IsInfinity(heatDtu)) {
                return false;
            }

            var coolant = FindLiquid(coolantStorage);
            if (coolant == null || coolant.Element == null ||
                    coolant.Element.specificHeatCapacity <= 0f) {
                return false;
            }

            var heatCapacity = coolant.Mass * 1000f * coolant.Element.specificHeatCapacity;
            if (heatCapacity <= 0f || float.IsNaN(heatCapacity) ||
                    float.IsInfinity(heatCapacity)) {
                return false;
            }

            var outputTemperature = coolant.Temperature + heatDtu / heatCapacity;
            var maximumLiquidTemperature =
                coolant.Element.highTemp - EntropyFluxPolicy.PhaseMarginKelvin;
            return !float.IsNaN(outputTemperature) && !float.IsInfinity(outputTemperature) &&
                outputTemperature <= maximumLiquidTemperature;
        }

        private void RunStableReaction(float dt) {
            var coolant = FindLiquid(coolantStorage);
            if (coolant == null || coolant.Element == null) {
                return;
            }

            var protoMatterRequired =
                0.2f * ForbiddenTechOptions.Current.CostMultiplier * dt;
            var heatDtu =
                1200000f * ForbiddenTechOptions.Current.HeatMultiplier * dt;
            if (!CanCoolantAbsorb(heatDtu)) {
                return;
            }

            var heatCapacity = coolant.Mass * 1000f * coolant.Element.specificHeatCapacity;
            coolant.Temperature += heatDtu / heatCapacity;
            if (protoMatterRequired > 0f) {
                protoMatterStorage.ConsumeIgnoringDisease(
                    ProtoMatterRegistration.Tag, protoMatterRequired);
            }

            if (generator != null) {
                generator.GenerateJoules(GrossGenerationWatts * dt);
            }

            processedCoolant = true;
            ApplyCoolantFlowState();
        }

        private void EnterState(ReactorState previous, ReactorState next) {
            state = next;
            stateElapsedSeconds = 0f;
            conditionElapsedSeconds = 0f;
            conditionInitialized = false;

            if (next == ReactorState.Charging) {
                // A new reaction cycle owns a fresh exactly-once decoherence boundary.
                decoherenceConsequencesApplied = false;
            }

            if (AnnihilationReactorPolicy.IsDecoherenceEntry(previous, next) &&
                    !decoherenceConsequencesApplied) {
                ApplyDecoherenceConsequences();
                decoherenceConsequencesApplied = true;
            }

            if (next != ReactorState.Stable && generator != null) {
                generator.ResetJoules();
            }
        }

        private void ApplyDecoherenceConsequences() {
            var availableProtoMatter = protoMatterStorage == null ? 0f :
                protoMatterStorage.GetMassAvailable(ProtoMatterRegistration.Tag);
            var lostProtoMatter =
                AnnihilationReactorPolicy.CalculateProtoMatterLoss(availableProtoMatter);
            if (lostProtoMatter > 0f && protoMatterStorage != null) {
                protoMatterStorage.ConsumeIgnoringDisease(
                    ProtoMatterRegistration.Tag, lostProtoMatter);
            }

            var heatPulseDtu = AnnihilationReactorPolicy.CalculateHeatPulseDtu(
                lostProtoMatter, ForbiddenTechOptions.Current.HeatMultiplier);
            ApplyHeatPulseToBuilding(heatPulseDtu);

            interferenceSecondsRemaining =
                AnnihilationReactorPolicy.DecoherenceInterferenceSeconds;
            ApplyOwnedInterference();
        }

        private void ApplyHeatPulseToBuilding(float heatDtu) {
            if (heatDtu <= 0f || primaryElement == null || primaryElement.Element == null ||
                    primaryElement.Mass <= 0f ||
                    primaryElement.Element.specificHeatCapacity <= 0f) {
                return;
            }

            var heatCapacity = primaryElement.Mass * 1000f *
                primaryElement.Element.specificHeatCapacity;
            if (heatCapacity <= 0f || float.IsNaN(heatCapacity) ||
                    float.IsInfinity(heatCapacity)) {
                return;
            }
            primaryElement.Temperature += heatDtu / heatCapacity;
        }

        private void UpdateConditionTimer(bool healthy, float dt) {
            if (!conditionInitialized || conditionHealthy != healthy) {
                conditionHealthy = healthy;
                conditionInitialized = true;
                conditionElapsedSeconds = dt;
                return;
            }
            conditionElapsedSeconds += dt;
        }

        private void UpdateOperationalActivity() {
            if (operational == null) {
                return;
            }

            var constraintState = state == ReactorState.Charging ||
                state == ReactorState.Stable ||
                state == ReactorState.Fluctuating ||
                state == ReactorState.Critical;
            operational.SetActive(constraintState && operational.IsOperational);
        }

        private void RefreshCoolantBatch() {
            if (processedCoolant && IsEmpty(coolantStorage)) {
                processedCoolant = false;
                ApplyCoolantFlowState();
            }
        }

        private void ApplyCoolantFlowState() {
            var accepting = !processedCoolant;
            if (coolantConsumer != null) {
                coolantConsumer.SetOnState(accepting);
            }
            if (coolantDispenser != null) {
                coolantDispenser.SetOnState(processedCoolant);
            }
        }

        private void UpdateOwnedInterference(float dt) {
            if (interferenceSecondsRemaining <= 0f) {
                return;
            }
            interferenceSecondsRemaining = Mathf.Max(0f, interferenceSecondsRemaining - dt);
            if (interferenceSecondsRemaining <= 0f) {
                RemoveOwnedInterference();
            }
        }

        private void ApplyOwnedInterference() {
            var cell = Grid.PosToCell(this);
            if (!string.IsNullOrEmpty(interferenceSourceId) && Grid.IsValidCell(cell)) {
                ProtoMatterInterferenceManager.ApplySource(
                    interferenceSourceId, cell,
                    AnnihilationReactorPolicy.DecoherenceInterferenceRadiusCells);
            }
        }

        private void RemoveOwnedInterference() {
            if (!string.IsNullOrEmpty(interferenceSourceId)) {
                ProtoMatterInterferenceManager.RemoveSource(interferenceSourceId);
            }
        }

        private static PrimaryElement FindLiquid(Storage storage) {
            if (storage == null) {
                return null;
            }
            for (var index = 0; index < storage.items.Count; index++) {
                var item = storage.items[index];
                var primary = item == null ? null : item.GetComponent<PrimaryElement>();
                if (primary != null && primary.Mass > EmptyThresholdKg &&
                        primary.Element != null && primary.Element.IsLiquid) {
                    return primary;
                }
            }
            return null;
        }

        private static bool IsEmpty(Storage storage) {
            return storage == null || storage.MassStored() <= EmptyThresholdKg;
        }
    }
}
