using System;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Factory for creating texture complexity analyzers.
    /// </summary>
    public static class AnalyzerFactory
    {
        /// <summary>
        /// Creates an analyzer for the specified strategy type.
        /// </summary>
        public static ITextureComplexityAnalyzer Create(
            AnalysisStrategyType type,
            float fastWeight = AnalysisConstants.CombinedDefaultFastWeight,
            float highAccuracyWeight = AnalysisConstants.CombinedDefaultHighAccuracyWeight,
            float perceptualWeight = AnalysisConstants.CombinedDefaultPerceptualWeight
        )
        {
            switch (type)
            {
                case AnalysisStrategyType.Fast:
                    return new FastAnalysisStrategy();

                case AnalysisStrategyType.HighAccuracy:
                    return new HighAccuracyStrategy();

                case AnalysisStrategyType.Perceptual:
                    return new PerceptualStrategy();

                case AnalysisStrategyType.Combined:
                    return new CombinedStrategy(fastWeight, highAccuracyWeight, perceptualWeight);

                default:
                    throw new ArgumentException($"Unknown strategy type: {type}", nameof(type));
            }
        }

        /// <summary>
        /// Creates a Normal Map specialized analyzer.
        /// </summary>
        public static ITextureComplexityAnalyzer CreateNormalMapAnalyzer()
        {
            return new NormalMapAnalyzer();
        }
    }
}
