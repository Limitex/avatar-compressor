using System.Collections.Generic;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Facade for texture complexity analysis.
    /// Delegates to an ITextureAnalysisBackend implementation (CPU or GPU).
    /// </summary>
    public class TextureAnalyzer
    {
        private readonly ITextureAnalysisBackend _backend;

        public TextureAnalyzer(
            AnalysisStrategyType strategy,
            float fastWeight,
            float highAccuracyWeight,
            float perceptualWeight,
            TextureProcessor processor,
            ComplexityCalculator complexityCalc
        )
        {
            _backend = AnalysisBackendFactory.Create(
                strategy,
                fastWeight,
                highAccuracyWeight,
                perceptualWeight,
                processor,
                complexityCalc
            );
        }

        /// <summary>
        /// Constructor for dependency injection (testing).
        /// </summary>
        internal TextureAnalyzer(ITextureAnalysisBackend backend)
        {
            _backend = backend;
        }

        /// <summary>
        /// Analyzes a batch of textures in parallel.
        /// </summary>
        public Dictionary<Texture2D, TextureAnalysisResult> AnalyzeBatch(
            Dictionary<Texture2D, TextureInfo> textures
        )
        {
            return _backend.AnalyzeBatch(textures);
        }
    }
}
