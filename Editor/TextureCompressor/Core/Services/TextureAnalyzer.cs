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
            AnalysisBackendPreference backendPreference = AnalysisBackendPreference.Auto
        )
        {
            _backend = AnalysisBackendFactory.Create(
                strategy,
                fastWeight,
                highAccuracyWeight,
                perceptualWeight,
                processor,
                backendPreference
            );
        }

        /// <summary>
        /// Analyzes a batch of textures and returns raw complexity scores (0-1).
        /// </summary>
        public Dictionary<Texture2D, float> AnalyzeBatch(
            Dictionary<Texture2D, TextureInfo> textures
        )
        {
            return _backend.AnalyzeBatch(textures);
        }
    }
}
