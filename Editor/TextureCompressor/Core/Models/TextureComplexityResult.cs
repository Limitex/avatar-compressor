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

        public TextureComplexityResult(float score)
        {
            Score = score;
        }
    }
}
