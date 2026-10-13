using System;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Buildings.Compiler;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Save;
using KSerialization;
using UnityEngine;
using Klei;

namespace ForbiddenTechnologyPack.Game.Buildings.MassEnergy {
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class ProtoAnnihilationReactor : KMonoBehaviour, ISim200ms, ISaveLoadable {
        [Serialize] private float ignitionJoules;
        [Serialize] private double totalFuelKg;
        [Serialize] private double totalGrossJoules;
        public Storage fuel;
        private Operational operational;
        private Generator generator;
        private FabricatorPowerBudget power;
        private CoolantController coolant;
        private KBatchedAnimController animation;
        private RadiationEmitter radiation;
        private string lastAnimation;
        private bool spawned;
        private bool working;
        public bool Ignited { get { return ignitionJoules >= MassEnergyBalance.IgnitionJoules; } }
        public float IgnitionProgress { get { return ignitionJoules / MassEnergyBalance.IgnitionJoules; } }
        public double TotalFuelKg { get { return totalFuelKg; } }
        public double TotalGrossJoules { get { return totalGrossJoules; } }
        public float BufferedJoules { get { return generator == null ? 0 : generator.JoulesAvailable; } }

        protected override void OnSpawn() {
            base.OnSpawn();
            operational = GetComponent<Operational>(); generator = GetComponent<Generator>();
            power = GetComponent<FabricatorPowerBudget>(); coolant = GetComponent<CoolantController>();
            animation = GetComponent<KBatchedAnimController>(); radiation = GetComponent<RadiationEmitter>();
            ignitionJoules = float.IsNaN(ignitionJoules) || float.IsInfinity(ignitionJoules) ? 0 :
                Mathf.Clamp(ignitionJoules, 0, MassEnergyBalance.IgnitionJoules);
            spawned = true;
            EnforceRemovalGuard();
            Subscribe((int)GameHashes.DeconstructComplete, OnDeconstruct);
            Subscribe((int)GameHashes.OperationalChanged, OnOperationalChanged);
            SetAnimation("idle");
            if (radiation != null) radiation.SetEmitting(false);
        }
        protected override void OnCleanUp() {
            spawned = false;
            Unsubscribe((int)GameHashes.DeconstructComplete, OnDeconstruct);
            Unsubscribe((int)GameHashes.OperationalChanged, OnOperationalChanged);
            if (power != null) power.Clear();
            if (radiation != null) radiation.SetEmitting(false);
            base.OnCleanUp();
        }
        public void Sim200ms(float dt) {
            if (!spawned || operational == null || power == null || coolant == null) return;
            EnforceRemovalGuard();
            coolant.heatJoulesPerSecond = Ignited ? MassEnergyBalance.ReactorHeatWatts : MassEnergyBalance.OperatingWatts;
            bool ready = operational.IsOperational && CanRun(.2f);
            operational.SetActive(ready);
            if (!ready) { StopVisuals(); return; }
            // Regular scheduler ticks advertise demand; payment settlement owns work.
            if (!power.IsSettling) return;
            var requested = Math.Min(Math.Max(0, dt), .2f);
            if (!Ignited) requested = Math.Min(requested,
                (MassEnergyBalance.IgnitionJoules - ignitionJoules) / MassEnergyBalance.OperatingWatts);
            var seconds = power.TakeWorkSeconds(requested);
            if (seconds <= 0) { StopVisuals(false); return; }
            if (!coolant.TryProcessWork(seconds)) { StopVisuals(); return; }
            if (!Ignited) {
                working = true;
                ignitionJoules = Math.Min(MassEnergyBalance.IgnitionJoules,
                    ignitionJoules + seconds * MassEnergyBalance.OperatingWatts);
                SetAnimation("charging_loop");
                return;
            }
            // Native storage returns the actual consumed mass, not a requested amount.
            float consumed, temperature; SimUtil.DiseaseInfo disease;
            fuel.ConsumeAndGetDisease(ProtoMatterRegistration.Tag,
                (float)MassEnergyBalance.FuelForSeconds(seconds), out consumed, out disease, out temperature);
            if (consumed <= 0) { StopVisuals(); return; }
            var actualSeconds = Math.Min(seconds, consumed / MassEnergyBalance.FuelKgPerSecond);
            generator.GenerateJoules(MassEnergyBalance.GrossWatts * actualSeconds);
            working = true;
            totalFuelKg += consumed; totalGrossJoules += MassEnergyBalance.GrossWatts * actualSeconds;
            SetAnimation("working_loop");
            if (radiation != null) radiation.SetEmitting(true);
        }
        private void EnforceRemovalGuard() {
            if (operational != null && ForbiddenTechSaveData.Instance != null && ForbiddenTechSaveData.Instance.SafeRemovalStarted)
                operational.SetFlag(Safety.SafeRemovalController.SafeRemovalAllowed, false);
        }
        private bool CanRun(float seconds) {
            if (fuel == null || fuel.GetAmountAvailable(ProtoMatterRegistration.Tag) + .000001f <
                    MassEnergyBalance.FuelForSeconds(seconds) || coolant == null || !coolant.IsReady) return false;
            return !Ignited || generator != null && generator.Capacity - generator.JoulesAvailable >= MassEnergyBalance.GrossWatts * seconds;
        }
        public string StateText {
            get {
                if (fuel == null || fuel.GetAmountAvailable(ProtoMatterRegistration.Tag) < .004f) return STRINGS.UI.MASSENERGY.FUEL_EMPTY;
                if (coolant != null && !coolant.IsReady) return coolant.StateText;
                if (Ignited && generator != null && generator.Capacity - generator.JoulesAvailable < 9600f) return STRINGS.UI.MASSENERGY.POWER_FULL;
                if (operational == null || !operational.IsOperational) return STRINGS.UI.MATTER.NOT_OPERATIONAL;
                return !Ignited ? string.Format(STRINGS.UI.MASSENERGY.IGNITING, IgnitionProgress * 100f) :
                    working ? (string)STRINGS.UI.MASSENERGY.REACTING : (string)STRINGS.UI.MASSENERGY.READY;
            }
        }
        public string DetailText { get { return string.Format(STRINGS.UI.MASSENERGY.REACTOR_DETAIL,
            fuel == null ? 0f : fuel.GetAmountAvailable(ProtoMatterRegistration.Tag), ignitionJoules / 1000f,
            BufferedJoules / 1000f, totalFuelKg, totalGrossJoules / 1000000d) + "\n" +
            (coolant == null ? "" : coolant.DetailText); } }
        private void OnOperationalChanged(object data) {
            if (operational == null || !operational.IsOperational) StopVisuals();
        }
        private void StopVisuals(bool clear = true) {
            working = false;
            if (clear && power != null) power.Clear();
            SetAnimation("idle");
            if (radiation != null) radiation.SetEmitting(false);
        }
        private void SetAnimation(string name) {
            if (lastAnimation == name || animation == null) return;
            lastAnimation = name; animation.Play(name, KAnim.PlayMode.Loop);
        }
        private void OnDeconstruct(object data) { ReturnStorage(); }
        internal void ReturnStorage() {
            PrepareRemoval();
            foreach (var storage in GetComponents<Storage>()) storage.DropAll(true, true, Vector3.zero, true, null);
        }
        internal void PrepareRemoval() {
            if (operational != null) { operational.SetFlag(Safety.SafeRemovalController.SafeRemovalAllowed, false); operational.SetActive(false); }
            StopVisuals();
        }
    }
}
