using System;
using System.IO;
using HarmonyLib;

namespace ForbiddenTechnologyPack.Game.Localization {
    internal static class TranslationLoader {
        internal static string ContentPath;
        internal static void Apply() {
            if (string.IsNullOrEmpty(ContentPath)) return;
            var language = global::Localization.GetCurrentLanguageCode();
            var chinese = language != null && (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                || language.EndsWith("chinese", StringComparison.OrdinalIgnoreCase));
            var path = Path.Combine(ContentPath, "translations", chinese ? "zh.po" : "en.po");
            ApplyFile(path);
        }
        internal static void ApplyFile(string path) {
            if (!File.Exists(path)) return;
            var entries = global::Localization.LoadStringsFile(path, false);
            string parameters = "", links = "", counts = "";
            // Do not call RegisterForTranslation: its namespace scan rejects this global STRINGS tree.
            global::Localization.OverloadStrings(entries, "STRINGS", typeof(global::STRINGS),
                ref parameters, ref links, ref counts);
            // Native OverloadStrings changes LocString.text but not Strings.Get(key).
            // Re-register only this mod's validated hierarchy for ToString/UI consumers.
            LocString.CreateLocStringKeys(typeof(global::STRINGS), null);
            if (!string.IsNullOrEmpty(parameters + links + counts))
                UnityEngine.Debug.LogWarning("[ForbiddenTechnologyPack] Translation validation: " + parameters + links + counts);
        }
    }
    [HarmonyPatch(typeof(global::Localization), nameof(global::Localization.Initialize))]
    internal static class TranslationInitializePatch {
        private static void Postfix() { TranslationLoader.Apply(); }
    }
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    internal static class TranslationBuildingsPatch {
        private static void Prefix() { TranslationLoader.Apply(); }
    }
}
