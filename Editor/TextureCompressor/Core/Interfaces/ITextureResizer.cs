using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Resizes a texture into a new readable RGBA32 <see cref="Texture2D"/>.
    /// Implementations average in linear space: sRGB sources are decoded before
    /// filtering and re-encoded afterwards, so sampling the result yields the
    /// filtered equivalent of sampling the source. Point-filtered sources are
    /// resampled with nearest neighbor to preserve exact texel values.
    /// </summary>
    internal interface ITextureResizer
    {
        /// <summary>
        /// Returns a new readable RGBA32 texture, or null on failure.
        /// The output keeps the source's color-space flag unless
        /// <paramref name="forceLinearOutput"/> is set, which skips the sRGB
        /// re-encode and flags the output linear — required for normal maps,
        /// whose compressed formats (BC5 etc.) are always sampled raw.
        /// </summary>
        Texture2D Resize(
            Texture2D source,
            int targetWidth,
            int targetHeight,
            bool forceLinearOutput
        );
    }
}
