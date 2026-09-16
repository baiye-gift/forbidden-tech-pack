namespace ForbiddenTechnologyPack.Game {
    public sealed class ModEntryPoint : KMod.UserMod2 {
        public override void OnLoad(HarmonyLib.Harmony harmony) {
            base.OnLoad(harmony);
            PeterHan.PLib.Core.PUtil.InitLibrary(true);
            new PeterHan.PLib.Options.POptions().RegisterOptions(this, typeof(Options.ForbiddenTechOptions));
            new PeterHan.PLib.Database.PLocalization().Register();
            Options.ForbiddenTechOptions.Load();
            UnityEngine.Debug.Log("[ForbiddenTechnologyPack] 0.1.0 loaded");
        }
    }
}
