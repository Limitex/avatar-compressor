namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Factory that selects the appropriate analysis backend
    /// based on system capabilities.
    /// </summary>
    public static class AnalysisBackendFactory
    {
        /// <summary>
        /// Creates the best available analysis backend.
        /// Currently returns CPU backend; GPU backend will be added when compute shaders are ready.
        /// </summary>
        public static ITextureAnalysisBackend Create(
            AnalysisStrategyType strategy,
            float fastWeight,
            float highAccuracyWeight,
            float perceptualWeight,
            TextureProcessor processor,
            ComplexityCalculator complexityCalc
        )
        {
            var standardAnalyzer = AnalyzerFactory.Create(
                strategy,
                fastWeight,
                highAccuracyWeight,
                perceptualWeight
            );
            var normalMapAnalyzer = AnalyzerFactory.CreateNormalMapAnalyzer();

            return new CpuAnalysisBackend(
                standardAnalyzer,
                normalMapAnalyzer,
                processor,
                complexityCalc
            );
        }
    }
}
