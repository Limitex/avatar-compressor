using dev.limitex.avatar.compressor.common;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Interface for texture complexity analysis strategies.
    /// Inherits from IAnalyzer for consistency with the common interface pattern.
    /// </summary>
    public interface ITextureComplexityAnalyzer : IAnalyzer<ProcessedPixelData, TextureComplexityResult>
    {
    }
}
