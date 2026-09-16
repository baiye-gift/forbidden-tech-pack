using ForbiddenTechnologyPack.Core;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace ForbiddenTechnologyPack.Game.Options {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class ForbiddenTechOptions {
        private static readonly ResolvedOptions DefaultOptions = PackOptions.Resolve(new RawOptions());

        public static ResolvedOptions Current { get; private set; } = DefaultOptions;

        [JsonProperty]
        [Option("Balance Preset", "Strong is the approved default. Choose Custom to use the individual multipliers below.")]
        public BalancePreset Preset { get; set; } = BalancePreset.Strong;

        [JsonProperty]
        [Option("Enable Matter Compilation", "Hides the module's research and build entries when disabled. Existing buildings remain safe.")]
        public bool ModuleEnabled { get; set; } = true;

        [JsonProperty]
        [Option("Enable Matter Analyzer", "Hides the Matter Analyzer build entry when disabled.")]
        public bool AnalyzerEnabled { get; set; } = true;

        [JsonProperty]
        [Option("Enable Mass Crusher", "Hides the Mass Crusher build entry when disabled.")]
        public bool CrusherEnabled { get; set; } = true;

        [JsonProperty]
        [Option("Enable Matter Compiler", "Hides the Matter Compiler build entry when disabled.")]
        public bool CompilerEnabled { get; set; } = true;

        [JsonProperty]
        [Option("Proto-Matter Recovery", "Custom preset only.")]
        [Limit(0.05f, 1.00f)]
        public float RecoveryRate { get; set; } = 0.90f;

        [JsonProperty]
        [Option("Compilation Cost Multiplier", "Custom preset only.")]
        [Limit(0.05f, 20.00f)]
        public float CostMultiplier { get; set; } = 1.00f;

        [JsonProperty]
        [Option("Power Multiplier", "Custom preset only.")]
        [Limit(0.10f, 10.00f)]
        public float PowerMultiplier { get; set; } = 1.00f;

        [JsonProperty]
        [Option("Heat Multiplier", "Custom preset only.")]
        [Limit(0.00f, 10.00f)]
        public float HeatMultiplier { get; set; } = 1.00f;

        [JsonProperty]
        [Option("Allow Industrial Materials", "Custom preset only.")]
        public bool AllowIndustrial { get; set; } = true;

        [JsonProperty]
        [Option("Allow Rare Materials", "Custom preset only.")]
        public bool AllowRare { get; set; } = true;

        [JsonProperty]
        [Option("Allow Endgame Materials", "Custom preset only.")]
        public bool AllowEndgame { get; set; } = true;

        [JsonProperty]
        [Option("Consume Analyzer Samples", "Custom preset only.")]
        public bool ConsumeSamples { get; set; } = true;

        public static void Load() {
            var settings = POptions.ReadSettings<ForbiddenTechOptions>();
            if (settings == null) {
                settings = new ForbiddenTechOptions();
            }

            Current = PackOptions.Resolve(settings.ToRawOptions());
        }

        private RawOptions ToRawOptions() {
            return new RawOptions {
                Preset = Preset,
                ModuleEnabled = ModuleEnabled,
                AnalyzerEnabled = AnalyzerEnabled,
                CrusherEnabled = CrusherEnabled,
                CompilerEnabled = CompilerEnabled,
                RecoveryRate = RecoveryRate,
                CostMultiplier = CostMultiplier,
                PowerMultiplier = PowerMultiplier,
                HeatMultiplier = HeatMultiplier,
                AllowIndustrial = AllowIndustrial,
                AllowRare = AllowRare,
                AllowEndgame = AllowEndgame,
                ConsumeSamples = ConsumeSamples
            };
        }
    }
}
