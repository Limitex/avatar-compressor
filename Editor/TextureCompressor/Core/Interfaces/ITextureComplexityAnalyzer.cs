using dev.limitex.avatar.compressor.editor;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Interface for texture complexity analysis strategies.
    /// Inherits from IAnalyzer for consistency with the common interface pattern.
    /// </summary>
    public interface ITextureComplexityAnalyzer : IAnalyzer<ProcessedPixelData, TextureComplexityResult>
    {
    }
}
