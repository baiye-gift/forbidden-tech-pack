namespace ForbiddenTechnologyPack.Core {
    public enum BalancePreset {
        Balanced,
        Strong,
        Extreme,
        Custom
    }

    public sealed class RawOptions {
        public BalancePreset Preset { get; set; } = BalancePreset.Strong;

        public bool ModuleEnabled { get; set; } = true;
        public bool AnalyzerEnabled { get; set; } = true;
        public bool CrusherEnabled { get; set; } = true;
        public bool CompilerEnabled { get; set; } = true;

        public float RecoveryRate { get; set; } = 0.90f;
        public float CostMultiplier { get; set; } = 1.00f;
        public float PowerMultiplier { get; set; } = 1.00f;
        public float HeatMultiplier { get; set; } = 1.00f;

        public bool AllowIndustrial { get; set; } = true;
        public bool AllowRare { get; set; } = true;
        public bool AllowEndgame { get; set; } = true;
        public bool ConsumeSamples { get; set; } = true;
    }

    public sealed class ResolvedOptions {
        public ResolvedOptions(float recoveryRate, float costMultiplier, float powerMultiplier,
                float heatMultiplier, bool allowIndustrial, bool allowRare, bool allowEndgame,
                bool consumeSamples, bool moduleEnabled, bool analyzerEnabled, bool crusherEnabled,
                bool compilerEnabled) {
            RecoveryRate = recoveryRate;
            CostMultiplier = costMultiplier;
            PowerMultiplier = powerMultiplier;
            HeatMultiplier = heatMultiplier;
            AllowIndustrial = allowIndustrial;
            AllowRare = allowRare;
            AllowEndgame = allowEndgame;
            ConsumeSamples = consumeSamples;
            ModuleEnabled = moduleEnabled;
            AnalyzerEnabled = analyzerEnabled;
            CrusherEnabled = crusherEnabled;
            CompilerEnabled = compilerEnabled;
        }

        public float RecoveryRate { get; private set; }
        public float CostMultiplier { get; private set; }
        public float PowerMultiplier { get; private set; }
        public float HeatMultiplier { get; private set; }
        public bool AllowIndustrial { get; private set; }
        public bool AllowRare { get; private set; }
        public bool AllowEndgame { get; private set; }
        public bool ConsumeSamples { get; private set; }
        public bool ModuleEnabled { get; private set; }
        public bool AnalyzerEnabled { get; private set; }
        public bool CrusherEnabled { get; private set; }
        public bool CompilerEnabled { get; private set; }
    }

    public static class PackOptions {
        public static ResolvedOptions Resolve(RawOptions raw) {
            if (raw == null) {
                raw = new RawOptions();
            }

            switch (raw.Preset) {
            case BalancePreset.Balanced:
                return Create(raw, 0.75f, 1.50f, 1.25f, 1.25f, true, true, false, true);
            case BalancePreset.Extreme:
                return Create(raw, 1.00f, 0.25f, 0.50f, 0.50f, true, true, true, false);
            case BalancePreset.Custom:
                return Create(raw,
                    Clamp(raw.RecoveryRate, 0.05f, 1.00f),
                    Clamp(raw.CostMultiplier, 0.05f, 20.00f),
                    Clamp(raw.PowerMultiplier, 0.10f, 10.00f),
                    Clamp(raw.HeatMultiplier, 0.00f, 10.00f),
                    raw.AllowIndustrial, raw.AllowRare, raw.AllowEndgame, raw.ConsumeSamples);
            case BalancePreset.Strong:
            default:
                return Create(raw, 0.90f, 1.00f, 1.00f, 1.00f, true, true, true, true);
            }
        }

        private static ResolvedOptions Create(RawOptions raw, float recoveryRate, float costMultiplier,
                float powerMultiplier, float heatMultiplier, bool allowIndustrial, bool allowRare,
                bool allowEndgame, bool consumeSamples) {
            return new ResolvedOptions(recoveryRate, costMultiplier, powerMultiplier, heatMultiplier,
                allowIndustrial, allowRare, allowEndgame, consumeSamples, raw.ModuleEnabled,
                raw.AnalyzerEnabled, raw.CrusherEnabled, raw.CompilerEnabled);
        }

        private static float Clamp(float value, float minimum, float maximum) {
            if (float.IsNaN(value) || float.IsInfinity(value)) {
                return minimum;
            }

            if (value < minimum) {
                return minimum;
            }

            if (value > maximum) {
                return maximum;
            }

            return value;
        }
    }
}
