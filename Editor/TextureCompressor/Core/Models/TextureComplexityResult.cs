using dev.limitex.avatar.compressor.editor;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Result of texture complexity analysis implementing IAnalysisResult.
    /// </summary>
    public readonly struct TextureComplexityResult : IAnalysisResult
    {
        /// <summary>
        /// Normalized complexity score (0-1).
        /// Higher values indicate more complex textures that need higher resolution.
        /// </summary>
        public float Score { get; }

        /// <summary>
        /// Human-readable summary of the analysis.
        /// </summary>
        public string Summary { get; }

        public TextureComplexityResult(float score)
        {
            Score = score;
            Summary = score switch
            {
                < 0.2f => "Very low complexity - can be heavily compressed",
                < 0.4f => "Low complexity - suitable for compression",
                < 0.6f => "Medium complexity - moderate compression recommended",
                < 0.8f => "High complexity - light compression only",
                _ => "Very high complexity - minimal compression recommended",
            };
        }

        public TextureComplexityResult(float score, string summary)
        {
            Score = score;
            Summary = summary;
        }
    }
}
