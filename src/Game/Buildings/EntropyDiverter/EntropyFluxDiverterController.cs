using System;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Options;
using KSerialization;

namespace ForbiddenTechnologyPack.Game.Buildings.EntropyDiverter {
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class EntropyFluxDiverterController : KMonoBehaviour, ISim1000ms {
        private const float EmptyThresholdKg = 0.0001f;

        [Serialize]
        private bool processedPair;

        [MyCmpGet]
        private Operational operational = null;

        [MyCmpGet]
        private ForbiddenTechDevice forbiddenDevice = null;

        public Storage hotStorage;
        public Storage coldStorage;
        public Storage protoMatterStorage;
        public ConduitConsumer hotConsumer;
        public ConduitConsumer coldConsumer;
        public ConduitDispenser hotDispenser;
        public ConduitDispenser coldDispenser;

        protected override void OnSpawn() {
            base.OnSpawn();
            ApplyFlowState();
        }

        public void Sim1000ms(float dt) {
            if (processedPair) {
                if (IsEmpty(hotStorage) && IsEmpty(coldStorage)) {
                    processedPair = false;
                    ApplyFlowState();
                }
                return;
            }

            if (operational == null || !operational.IsActive ||
                    (forbiddenDevice != null && forbiddenDevice.IsInterfered)) {
                return;
            }

            var hot = FindLiquid(hotStorage);
            var cold = FindLiquid(coldStorage);
            if (hot == null || cold == null || hot.Element == null || cold.Element == null) {
                return;
            }

            var decision = EntropyFluxPolicy.Evaluate(
                hot.Mass, hot.Element.specificHeatCapacity, hot.Temperature,
                hot.Element.lowTemp, hot.Element.highTemp,
                cold.Mass, cold.Element.specificHeatCapacity, cold.Temperature,
                cold.Element.lowTemp, cold.Element.highTemp,
                EntropyFluxPolicy.MaxTransferDtuPerBatch);
            if (!decision.IsValid || decision.TransferredDtu <= 0f) {
                return;
            }

            var protoMatterCost = EntropyFluxPolicy.ProtoMatterCostKg(
                decision.TransferredDtu, ForbiddenTechOptions.Current.CostMultiplier);
            if (protoMatterStorage == null ||
                    protoMatterStorage.GetMassAvailable(ProtoMatterRegistration.Tag) + 0.0001f <
                    protoMatterCost) {
                return;
            }

            // All preconditions have succeeded. Mutate both packets and catalyst exactly once,
            // then seal the pair until both processed packets leave through their outputs.
            hot.Temperature = decision.HotOutputTemperatureK;
            cold.Temperature = decision.ColdOutputTemperatureK;
            if (protoMatterCost > 0f) {
                protoMatterStorage.ConsumeIgnoringDisease(ProtoMatterRegistration.Tag,
                    protoMatterCost);
            }
            processedPair = true;
            ApplyFlowState();
        }

        internal void PrepareForSafeRemoval() {
            if (hotConsumer != null) hotConsumer.SetOnState(false);
            if (coldConsumer != null) coldConsumer.SetOnState(false);
            if (hotDispenser != null) hotDispenser.SetOnState(false);
            if (coldDispenser != null) coldDispenser.SetOnState(false);
        }

        private void ApplyFlowState() {
            var accepting = !processedPair;
            if (hotConsumer != null) hotConsumer.SetOnState(accepting);
            if (coldConsumer != null) coldConsumer.SetOnState(accepting);
            if (hotDispenser != null) hotDispenser.SetOnState(processedPair);
            if (coldDispenser != null) coldDispenser.SetOnState(processedPair);
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
