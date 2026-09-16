namespace ForbiddenTechnologyPack.Game {
    public sealed class ModEntryPoint : KMod.UserMod2 {
        public override void OnLoad(HarmonyLib.Harmony harmony) {
            base.OnLoad(harmony);
            PeterHan.PLib.Core.PUtil.InitLibrary(true);
            UnityEngine.Debug.Log("[ForbiddenTechnologyPack] 0.1.0 loaded");
        }
    }
}
