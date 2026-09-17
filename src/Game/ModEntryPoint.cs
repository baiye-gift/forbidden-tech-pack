namespace ForbiddenTechnologyPack.Game {
    public sealed class ModEntryPoint : KMod.UserMod2 {
        public override void OnLoad(HarmonyLib.Harmony harmony) {
            base.OnLoad(harmony);
            PeterHan.PLib.Core.PUtil.InitLibrary(true);
            // The mod keeps its STRINGS root in the global namespace so ONI can
            // resolve building/research keys directly. Register that tree explicitly;
            // PLib's assembly scanner now trips on a null namespace under U59.
            LocString.CreateLocStringKeys(typeof(global::STRINGS), null);
            new PeterHan.PLib.Options.POptions().RegisterOptions(this, typeof(Options.ForbiddenTechOptions));
            Options.ForbiddenTechOptions.Load();
            UnityEngine.Debug.Log("[ForbiddenTechnologyPack] 0.1.0 loaded");
        }
    }
}
