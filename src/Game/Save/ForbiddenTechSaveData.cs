using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Safety;
using HarmonyLib;
using KSerialization;

namespace ForbiddenTechnologyPack.Game.Save {
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class ForbiddenTechSaveData : KMonoBehaviour {
        [Serialize]
        private int dataVersion;

        [Serialize]
        private List<string> unlockedElementIds = new List<string>();

        [Serialize]
        private bool safeRemovalStarted;

        [Serialize]
        private bool safeRemovalCompleted;

        private UnlockState unlockState;

        public static ForbiddenTechSaveData Instance { get; private set; }

        public event System.Action UnlocksChanged;
        public event System.Action SafeRemovalChanged;

        public bool SafeRemovalStarted {
            get { return safeRemovalStarted; }
        }

        public bool SafeRemovalCompleted {
            get { return safeRemovalCompleted; }
        }

        protected override void OnSpawn() {
            base.OnSpawn();
            unlockState = UnlockState.FromSerialized(dataVersion, unlockedElementIds);
            dataVersion = unlockState.Version;
            unlockedElementIds = new List<string>(unlockState.ToSerialized());
            Instance = this;
            if (safeRemovalCompleted) {
                SafeRemovalController.ApplyCompletedVisibility();
            }
        }

        protected override void OnCleanUp() {
            if (Instance == this) {
                Instance = null;
            }
            base.OnCleanUp();
        }

        public bool Unlock(Tag elementTag) {
            EnsureState();
            if (safeRemovalStarted || !elementTag.IsValid || !unlockState.Unlock(elementTag.Name)) {
                return false;
            }

            PersistState();
            var handler = UnlocksChanged;
            if (handler != null) {
                handler();
            }
            return true;
        }

        public bool IsUnlocked(Tag elementTag) {
            EnsureState();
            return elementTag.IsValid && unlockState.IsUnlocked(elementTag.Name);
        }

        public UnlockState GetUnlockState() {
            EnsureState();
            return UnlockState.FromSerialized(unlockState.Version, unlockState.ToSerialized());
        }

        public bool BeginSafeRemoval() {
            if (safeRemovalStarted) {
                return false;
            }
            safeRemovalStarted = true;
            safeRemovalCompleted = false;
            NotifySafeRemovalChanged();
            return true;
        }

        public void SetSafeRemovalCompleted(bool complete) {
            if (safeRemovalCompleted == complete) {
                return;
            }
            safeRemovalCompleted = complete;
            NotifySafeRemovalChanged();
        }

        private void NotifySafeRemovalChanged() {
            var handler = SafeRemovalChanged;
            if (handler != null) {
                handler();
            }
        }

        private void EnsureState() {
            if (unlockState == null) {
                unlockState = UnlockState.FromSerialized(dataVersion, unlockedElementIds);
            }
        }

        private void PersistState() {
            dataVersion = unlockState.Version;
            unlockedElementIds = new List<string>(unlockState.ToSerialized());
        }
    }

    [HarmonyPatch(typeof(global::Game), "OnPrefabInit")]
    public static class GameOnPrefabInitPatch {
        [HarmonyPostfix]
        public static void Postfix(global::Game __instance) {
            if (__instance != null) {
                __instance.gameObject.AddOrGet<ForbiddenTechSaveData>();
            }
        }
    }
}
