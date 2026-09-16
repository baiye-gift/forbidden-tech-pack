using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;
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

        private UnlockState unlockState;

        public static ForbiddenTechSaveData Instance { get; private set; }

        public event System.Action UnlocksChanged;
        public event System.Action SafeRemovalChanged;

        public bool SafeRemovalStarted {
            get { return safeRemovalStarted; }
        }

        protected override void OnSpawn() {
            base.OnSpawn();
            unlockState = UnlockState.FromSerialized(dataVersion, unlockedElementIds);
            dataVersion = unlockState.Version;
            unlockedElementIds = new List<string>(unlockState.ToSerialized());
            Instance = this;
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
            var handler = SafeRemovalChanged;
            if (handler != null) {
                handler();
            }
            return true;
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
