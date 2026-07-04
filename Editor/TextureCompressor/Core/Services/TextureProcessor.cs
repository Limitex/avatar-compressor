using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Service for processing textures (resizing only).
    /// All output dimensions are guaranteed to be multiples of 4 for DXT/BC compression compatibility.
    /// </summary>
    public class TextureProcessor
    {
        private readonly int _minResolution;
        private readonly int _maxResolution;
        private readonly bool _forcePowerOfTwo;
        private readonly ITextureResizer _resizer;

        // No default for the backend preference: production call sites resolve it
        // from TextureCompressorPreferences, and tests must pick deterministically.
        public TextureProcessor(
            int minResolution,
            int maxResolution,
            bool forcePowerOfTwo,
            ResizeBackendPreference resizeBackendPreference
        )
        {
            _minResolution = minResolution;
            _maxResolution = maxResolution;
            _forcePowerOfTwo = forcePowerOfTwo;
            _resizer = AreaAverageResizerFactory.Create(resizeBackendPreference);
        }

        /// <summary>
        /// Calculates the target resize dimensions for a texture based on its analysis result.
        /// When no downscaling is needed and the source fits within max resolution, dimensions are
        /// kept at the source size (rounded to multiples of 4). Otherwise, uses the recommended resolution.
        /// </summary>
        private (int Width, int Height) CalculateResizeDimensions(
            Texture2D source,
            TextureAnalysisResult analysis
        )
        {
            if (
                analysis.RecommendedDivisor <= 1
                && source.width <= _maxResolution
                && source.height <= _maxResolution
            )
            {
                return (EnsureMultipleOf4(source.width), EnsureMultipleOf4(source.height));
            }

            return (analysis.RecommendedResolution.x, analysis.RecommendedResolution.y);
        }

        /// <summary>
        /// Ensures a dimension is a multiple of 4 for DXT/BC compression compatibility.
        /// </summary>
        /// <returns>The dimension rounded up to the nearest multiple of 4, minimum 4 (e.g., 150→152, 4→4, 2→4)</returns>
        private static int EnsureMultipleOf4(int dimension)
        {
            return Mathf.Max(4, ((dimension + 3) / 4) * 4);
        }

        /// <summary>
        /// Calculates new dimensions based on divisor.
        /// </summary>
        /// <returns>New dimensions clamped to min/max resolution and rounded appropriately</returns>
        public Vector2Int CalculateNewDimensions(int width, int height, int divisor)
        {
            int newWidth = Mathf.Max(width / divisor, _minResolution);
            int newHeight = Mathf.Max(height / divisor, _minResolution);

            newWidth = Mathf.Min(newWidth, _maxResolution);
            newHeight = Mathf.Min(newHeight, _maxResolution);

            if (_forcePowerOfTwo)
            {
                newWidth = Mathf.ClosestPowerOfTwo(newWidth);
                newHeight = Mathf.ClosestPowerOfTwo(newHeight);

                if (newWidth > _maxResolution)
                    newWidth = Mathf.ClosestPowerOfTwo(_maxResolution / 2) * 2;
                if (newHeight > _maxResolution)
                    newHeight = Mathf.ClosestPowerOfTwo(_maxResolution / 2) * 2;
            }
            else
            {
                // Ensure dimensions are multiples of 4 for DXT/BC compression compatibility
                newWidth = EnsureMultipleOf4(newWidth);
                newHeight = EnsureMultipleOf4(newHeight);
            }

            return new Vector2Int(newWidth, newHeight);
        }

        /// <summary>
        /// Resizes a single texture, acquiring and releasing the RenderTexture lock per call.
        /// Normal maps store vectors, not color, so they are always resized into
        /// linear-flagged output — their compressed formats (BC5 etc.) are sampled raw.
        /// </summary>
        /// <returns>A new readable RGBA32 Texture2D, or null if resize failed.</returns>
        public Texture2D ResizeSingle(
            Texture2D source,
            TextureAnalysisResult analysis,
            bool isNormalMap
        )
        {
            lock (TextureReadback.RenderTextureLock)
            {
                try
                {
                    var (newWidth, newHeight) = CalculateResizeDimensions(source, analysis);
                    return _resizer.Resize(
                        source,
                        newWidth,
                        newHeight,
                        forceLinearOutput: isNormalMap
                    );
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(
                        $"[TextureCompressor] Failed to resize texture '{source.name}': {e.Message}"
                    );
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets readable pixels from a single texture.
        /// For non-readable or sRGB textures, performs GPU→CPU readback via RenderTexture.
        /// sRGB textures always go through the blit path with an explicit linear RT
        /// so that hardware sRGB-to-linear decode is applied, matching the GPU analysis
        /// backend which gets the same hardware decode by binding sRGB textures directly.
        /// The temporary resources are released immediately, keeping peak memory low.
        /// </summary>
        public Color[] GetReadablePixelsSingle(Texture2D texture)
        {
            if (texture == null)
                return new Color[0];

            // sRGB textures must go through the blit path for consistent linear decode.
            if (texture.isReadable && !texture.isDataSRGB)
            {
                try
                {
                    return texture.GetPixels();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(
                        $"[TextureCompressor] Failed to read pixels from readable texture: {e.Message}"
                    );
                    return new Color[0];
                }
            }

            var readable = TextureReadback.BlitToReadable(
                texture,
                RenderTextureReadWrite.Linear,
                linearFlag: true
            );
            if (readable == null)
                return new Color[0];
            try
            {
                return readable.GetPixels();
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }
    }
}
