using System;

namespace ForbiddenTechnologyPack.Core {
    public sealed class RecipePlan {
        internal RecipePlan(string elementId, string analyzerId, string crusherId, string compilerId,
                float analyzerInputKg, bool analyzerInputDoNotConsume, float analyzerTimeSeconds,
                float crusherInputKg, float crusherOutputKg, float crusherTimeSeconds,
                float compilerInputKg, float compilerOutputKg, float compilerTimeSeconds,
                bool isRoundTripSafe) {
            ElementId = elementId;
            AnalyzerId = analyzerId;
            CrusherId = crusherId;
            CompilerId = compilerId;
            AnalyzerInputKg = analyzerInputKg;
            AnalyzerInputDoNotConsume = analyzerInputDoNotConsume;
            AnalyzerTimeSeconds = analyzerTimeSeconds;
            CrusherInputKg = crusherInputKg;
            CrusherOutputKg = crusherOutputKg;
            CrusherTimeSeconds = crusherTimeSeconds;
            CompilerInputKg = compilerInputKg;
            CompilerOutputKg = compilerOutputKg;
            CompilerTimeSeconds = compilerTimeSeconds;
            IsRoundTripSafe = isRoundTripSafe;
        }

        public string ElementId { get; private set; }
        public string AnalyzerId { get; private set; }
        public string CrusherId { get; private set; }
        public string CompilerId { get; private set; }
        public float AnalyzerInputKg { get; private set; }
        public bool AnalyzerInputDoNotConsume { get; private set; }
        public float AnalyzerTimeSeconds { get; private set; }
        public float CrusherInputKg { get; private set; }
        public float CrusherOutputKg { get; private set; }
        public float CrusherTimeSeconds { get; private set; }
        public float CompilerInputKg { get; private set; }
        public float CompilerOutputKg { get; private set; }
        public float CompilerTimeSeconds { get; private set; }
        public bool IsRoundTripSafe { get; private set; }
    }

    public static class RecipePlanFactory {
        private const float AnalyzerInputKg = 10f;
        private const float AnalyzerTimeSeconds = 30f;
        private const float CrusherInputKg = 100f;
        private const float CrusherTimeSeconds = 40f;

        public static RecipePlan Create(MaterialRule rule, ResolvedOptions options) {
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }
            if (options == null) {
                throw new ArgumentNullException("options");
            }
            if (!ConversionMath.IsRoundTripSafe(rule, options)) {
                throw new ArgumentException("Recipe would permit a Proto-Matter round-trip gain.", "options");
            }

            var compilerOutputKg = IsSmallBatch(rule.Tier) ? 10f : 100f;
            var compilerInputKg = ConversionMath.CompilerInputKg(compilerOutputKg, rule, options);
            var crusherOutputKg = ConversionMath.CrusherOutputKg(CrusherInputKg, options);
            return new RecipePlan(rule.ElementId, AnalyzerId(rule.ElementId), CrusherId(rule.ElementId),
                CompilerId(rule.ElementId), AnalyzerInputKg, !options.ConsumeSamples, AnalyzerTimeSeconds,
                CrusherInputKg, crusherOutputKg, CrusherTimeSeconds, compilerInputKg, compilerOutputKg,
                30f * rule.ProtoMatterPerKg, true);
        }

        public static string AnalyzerId(string elementId) {
            return "BaiyeMatterAnalyze_" + ValidateElementId(elementId);
        }

        public static string CrusherId(string elementId) {
            return "BaiyeMatterCrush_" + ValidateElementId(elementId);
        }

        public static string CompilerId(string elementId) {
            return "BaiyeMatterCompile_" + ValidateElementId(elementId);
        }

        private static bool IsSmallBatch(MaterialTier tier) {
            return tier == MaterialTier.Rare || tier == MaterialTier.Endgame;
        }

        private static string ValidateElementId(string elementId) {
            if (string.IsNullOrWhiteSpace(elementId)) {
                throw new ArgumentException("Element ID is required.", "elementId");
            }

            return elementId;
        }
    }
}
