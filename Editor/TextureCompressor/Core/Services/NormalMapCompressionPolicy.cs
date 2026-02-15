using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Encapsulates normal-map compression policy decisions.
    /// </summary>
    public static class NormalMapCompressionPolicy
    {
        /// <summary>
        /// Determines whether semantic alpha should be preserved in BC7 normal-map output.
        /// </summary>
        public static bool ShouldPreserveSemanticAlpha(
            TextureFormat targetFormat,
            NormalMapPreprocessor.SourceLayout sourceLayout,
            bool hasSignificantAlpha
        )
        {
            bool sourceStoresExplicitSignedZ = sourceLayout == NormalMapPreprocessor.SourceLayout.RGB;
            return targetFormat == TextureFormat.BC7
                && sourceLayout != NormalMapPreprocessor.SourceLayout.AG
                && (sourceStoresExplicitSignedZ || hasSignificantAlpha);
        }
    }
}
