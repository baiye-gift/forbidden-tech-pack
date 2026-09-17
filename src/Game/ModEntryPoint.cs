namespace ForbiddenTechnologyPack.Game {
    public sealed class ModEntryPoint : KMod.UserMod2 {
        public override void OnLoad(HarmonyLib.Harmony harmony) {
            base.OnLoad(harmony);
            PeterHan.PLib.Core.PUtil.InitLibrary(true);
            // Register the generated vanilla STRINGS root keys before any building,
            // research, or search cache asks ONI for display names. PLib localization
            // registration alone does not create the STRINGS.* runtime key tree.
            LocString.CreateLocStringKeys(typeof(global::STRINGS), null);
            new PeterHan.PLib.Options.POptions().RegisterOptions(this, typeof(Options.ForbiddenTechOptions));
            new PeterHan.PLib.Database.PLocalization().Register();
            Options.ForbiddenTechOptions.Load();
            UnityEngine.Debug.Log("[ForbiddenTechnologyPack] 0.1.0 loaded");
        }
    }
}
