using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Safety;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace ForbiddenTechnologyPack.Game.Options {
    public enum LocalizedBalancePreset {
        [Option("相对平衡", "降低原质回收率，并提高建造、耗电和发热成本。")]
        Balanced = 0,

        [Option("强力（默认）", "高回收率与标准成本，适合作为默认体验。")]
        Strong = 1,

        [Option("极度超模", "最高回收率、大幅降低成本，并允许所有材料。")]
        Extreme = 2,

        [Option("自定义", "使用下方的自定义参数。")]
        Custom = 3
    }

    [JsonObject(MemberSerialization.OptIn)]
    [RestartRequired]
    public sealed class ForbiddenTechOptions {
        private static readonly ResolvedOptions DefaultOptions = PackOptions.Resolve(new RawOptions());

        public static ResolvedOptions Current { get; private set; } = DefaultOptions;

        [JsonProperty]
        [Option("平衡预设", "推荐使用“强力（默认）”；选择“自定义”后才会使用下方的独立参数。", "预设")]
        public LocalizedBalancePreset Preset { get; set; } = LocalizedBalancePreset.Strong;

        [JsonProperty]
        [Option("启用物质编译模块", "关闭后隐藏本模组的研究和建造项目；已建建筑仍会保持存档安全。", "模块与建筑")]
        public bool ModuleEnabled { get; set; } = true;

        [JsonProperty]
        [Option("启用物质分析仪", "关闭后隐藏物质分析仪的建造项目。", "模块与建筑")]
        public bool AnalyzerEnabled { get; set; } = true;

        [JsonProperty]
        [Option("启用质量粉碎机", "关闭后隐藏质量粉碎机的建造项目。", "模块与建筑")]
        public bool CrusherEnabled { get; set; } = true;

        [JsonProperty]
        [Option("启用物质编译器", "关闭后隐藏物质编译器的建造项目。", "模块与建筑")]
        public bool CompilerEnabled { get; set; } = true;

        [JsonProperty]
        [Option("原质回收率", "仅在选择“自定义”预设时生效。", "自定义参数")]
        [Limit(0.05f, 1.00f)]
        public float RecoveryRate { get; set; } = 0.90f;

        [JsonProperty]
        [Option("编译成本倍率", "仅在选择“自定义”预设时生效。", "自定义参数")]
        [Limit(0.05f, 20.00f)]
        public float CostMultiplier { get; set; } = 1.00f;

        [JsonProperty]
        [Option("耗电倍率", "仅在选择“自定义”预设时生效。", "自定义参数")]
        [Limit(0.10f, 10.00f)]
        public float PowerMultiplier { get; set; } = 1.00f;

        [JsonProperty]
        [Option("发热倍率", "仅在选择“自定义”预设时生效。", "自定义参数")]
        [Limit(0.00f, 10.00f)]
        public float HeatMultiplier { get; set; } = 1.00f;

        [JsonProperty]
        [Option("允许工业材料", "仅在选择“自定义”预设时生效。", "材料范围")]
        public bool AllowIndustrial { get; set; } = true;

        [JsonProperty]
        [Option("允许稀有材料", "仅在选择“自定义”预设时生效。", "材料范围")]
        public bool AllowRare { get; set; } = true;

        [JsonProperty]
        [Option("允许终局材料", "仅在选择“自定义”预设时生效。", "材料范围")]
        public bool AllowEndgame { get; set; } = true;

        [JsonProperty]
        [Option("分析时消耗样本", "仅在选择“自定义”预设时生效。", "自定义参数")]
        public bool ConsumeSamples { get; set; } = true;

        [Option("准备安全移除", "在殖民地中停用本模组前使用。确认两次后，它会转换原质并移除自定义建筑。", "存档安全")]
        public System.Action<object> PrepareSafeRemoval {
            get { return SafeRemovalDialog.ShowFirstConfirmation; }
        }

        public static void Load() {
            var settings = POptions.ReadSettings<ForbiddenTechOptions>();
            if (settings == null) {
                settings = new ForbiddenTechOptions();
            }

            Current = PackOptions.Resolve(settings.ToRawOptions());
        }

        private RawOptions ToRawOptions() {
            return new RawOptions {
                Preset = (BalancePreset)Preset,
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
