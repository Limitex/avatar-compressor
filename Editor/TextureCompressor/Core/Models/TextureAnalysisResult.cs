using dev.limitex.avatar.compressor.editor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Result of texture complexity analysis.
    /// </summary>
    public readonly struct TextureAnalysisResult : IAnalysisResult
    {
        /// <summary>
        /// Normalized complexity value (0-1).
        /// </summary>
        public float NormalizedComplexity { get; }

        /// <summary>
        /// Recommended divisor for resolution reduction.
        /// </summary>
        public int RecommendedDivisor { get; }

        /// <summary>
        /// Recommended output resolution.
        /// </summary>
        public Vector2Int RecommendedResolution { get; }

        // IAnalysisResult implementation
        public float Score => NormalizedComplexity;

        public string Summary => $"Complexity: {(int)(NormalizedComplexity * 100)}%, " +
                                 $"Divisor: {RecommendedDivisor}x, " +
                                 $"Target: {RecommendedResolution.x}x{RecommendedResolution.y}";

        public TextureAnalysisResult(float complexity, int divisor, Vector2Int resolution)
        {
            NormalizedComplexity = complexity;
            RecommendedDivisor = divisor;
            RecommendedResolution = resolution;
        }
    }
}
