using UnityEngine;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Service for processing textures (resizing and compression).
    /// Uses lock to ensure thread safety for RenderTexture operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Important:</b> All output texture dimensions are guaranteed to be multiples of 4,
    /// regardless of the <c>forcePowerOfTwo</c> setting. This is required for DXT/BC block
    /// compression formats which operate on 4x4 pixel blocks.
    /// </para>
    /// <para>
    /// When <c>forcePowerOfTwo</c> is true, dimensions are rounded to the nearest power of two.
    /// When false, dimensions are rounded up to the nearest multiple of 4.
    /// Example: 150x150 becomes 152x152 (not 150x150).
    /// </para>
    /// </remarks>
    public class TextureProcessor
    {
        // Lock object for thread-safe RenderTexture operations
        private static readonly object RenderTextureLock = new object();

        private readonly int _minResolution;
        private readonly int _maxResolution;
        private readonly bool _forcePowerOfTwo;
        private readonly TextureFormatSelector _formatSelector;

        public TextureProcessor(int minResolution, int maxResolution, bool forcePowerOfTwo,
            CompressionPlatform targetPlatform = CompressionPlatform.Auto,
            bool useHighQualityFormatForHighComplexity = true,
            float highQualityComplexityThreshold = 0.7f)
        {
            _minResolution = minResolution;
            _maxResolution = maxResolution;
            _forcePowerOfTwo = forcePowerOfTwo;
            _formatSelector = new TextureFormatSelector(
                targetPlatform,
                useHighQualityFormatForHighComplexity,
                highQualityComplexityThreshold
            );
        }

        /// <summary>
        /// Resizes a texture using pre-computed analysis result.
        /// </summary>
        /// <param name="source">Source texture to resize</param>
        /// <param name="analysis">Pre-computed analysis result with recommended settings</param>
        /// <param name="enableLogging">Whether to log the operation</param>
        /// <param name="isNormalMap">Whether this is a normal map texture</param>
        /// <param name="formatOverride">Optional format override from frozen settings</param>
        public Texture2D Resize(Texture2D source, TextureAnalysisResult analysis, bool enableLogging, bool isNormalMap = false, FrozenTextureFormat? formatOverride = null)
        {
            Texture2D result;
            if (analysis.RecommendedDivisor <= 1 &&
                source.width <= _maxResolution &&
                source.height <= _maxResolution)
            {
                // Even when copying, ensure dimensions are multiples of 4 for DXT/BC compression
                int width = EnsureMultipleOf4(source.width);
                int height = EnsureMultipleOf4(source.height);

                if (width == source.width && height == source.height)
                {
                    result = Copy(source);
                }
                else
                {
                    result = ResizeTo(source, width, height);
                }
            }
            else
            {
                result = ResizeTo(source, analysis.RecommendedResolution.x, analysis.RecommendedResolution.y);
            }

            // Apply compression to reduce memory usage
            _formatSelector.CompressTexture(result, source.format, isNormalMap, analysis.NormalizedComplexity, formatOverride);

            if (enableLogging)
            {
                var format = result.format;
                var frozenInfo = formatOverride.HasValue && formatOverride.Value != FrozenTextureFormat.Auto
                    ? " [FROZEN]"
                    : "";
                Debug.Log($"[TextureCompressor] {source.name}: " +
                          $"{source.width}x{source.height} -> " +
                          $"{result.width}x{result.height} ({format}){frozenInfo} " +
                          $"(Complexity: {analysis.NormalizedComplexity:P0}, " +
                          $"Divisor: {analysis.RecommendedDivisor}x)");
            }

            return result;
        }

        /// <summary>
        /// Ensures a dimension is a multiple of 4 for DXT/BC compression compatibility.
        /// </summary>
        /// <param name="dimension">The dimension to adjust</param>
        /// <returns>The dimension rounded up to the nearest multiple of 4 (minimum 4)</returns>
        /// <remarks>
        /// DXT (S3TC) and BC (BPTC) compression formats operate on 4x4 pixel blocks.
        /// Textures with dimensions not divisible by 4 may cause compression artifacts
        /// or failures. This method ensures all dimensions are compatible by rounding up.
        /// <para>Examples: 1->4, 5->8, 150->152, 256->256</para>
        /// </remarks>
        private static int EnsureMultipleOf4(int dimension)
        {
            return Mathf.Max(4, ((dimension + 3) / 4) * 4);
        }

        /// <summary>
        /// Calculates new dimensions based on divisor.
        /// </summary>
        /// <param name="width">Original texture width</param>
        /// <param name="height">Original texture height</param>
        /// <param name="divisor">Divisor to reduce dimensions (1, 2, 4, 8, etc.)</param>
        /// <returns>New dimensions clamped to min/max resolution and rounded appropriately</returns>
        /// <remarks>
        /// <para>
        /// <b>Breaking change in v0.3.5:</b> Output dimensions are now always multiples of 4,
        /// even when <c>forcePowerOfTwo</c> is false. This ensures compatibility with DXT/BC
        /// block compression formats which require 4x4 pixel blocks.
        /// </para>
        /// <para>
        /// Examples (with minResolution=32, maxResolution=2048, forcePowerOfTwo=false):
        /// <list type="bullet">
        /// <item><description>300x300, divisor=2 -> 152x152 (150 rounded up to nearest multiple of 4)</description></item>
        /// <item><description>400x400, divisor=2 -> 200x200 (already a multiple of 4)</description></item>
        /// <item><description>100x100, divisor=4 -> 32x32 (25 clamped to minResolution 32)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
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
        /// Resizes a texture to the specified dimensions.
        /// Thread-safe: uses lock to protect RenderTexture.active.
        /// </summary>
        public Texture2D ResizeTo(Texture2D source, int newWidth, int newHeight)
        {
            lock (RenderTextureLock)
            {
                RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
                rt.filterMode = FilterMode.Bilinear;

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                Graphics.Blit(source, rt);

                // Preserve mipmap setting from source texture
                // Mipmaps are important for performance, visual quality, and VRAM optimization
                Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, source.mipmapCount > 1);
                result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                result.Apply(source.mipmapCount > 1);

                // Copy texture settings from source
                result.wrapModeU = source.wrapModeU;
                result.wrapModeV = source.wrapModeV;
                result.wrapModeW = source.wrapModeW;
                result.filterMode = source.filterMode;
                result.anisoLevel = source.anisoLevel;

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);

                return result;
            }
        }

        /// <summary>
        /// Creates a copy of the texture.
        /// </summary>
        public Texture2D Copy(Texture2D source)
        {
            return ResizeTo(source, source.width, source.height);
        }

        /// <summary>
        /// Gets readable pixels from a texture.
        /// Thread-safe: uses lock to protect RenderTexture.active when needed.
        /// </summary>
        public Color[] GetReadablePixels(Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogWarning("[TextureCompressor] GetReadablePixels: texture is null");
                return new Color[0];
            }

            if (texture.isReadable)
            {
                try
                {
                    return texture.GetPixels();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[TextureCompressor] Failed to read pixels from readable texture: {e.Message}");
                    return new Color[0];
                }
            }

            // Non-readable texture requires RenderTexture operations
            lock (RenderTextureLock)
            {
                RenderTexture rt = null;
                RenderTexture previous = RenderTexture.active;
                Texture2D readable = null;

                try
                {
                    rt = RenderTexture.GetTemporary(
                        texture.width, texture.height, 0, RenderTextureFormat.ARGB32);

                    if (rt == null)
                    {
                        Debug.LogWarning("[TextureCompressor] Failed to create temporary RenderTexture");
                        return new Color[0];
                    }

                    Graphics.Blit(texture, rt);
                    RenderTexture.active = rt;

                    // Preserve mipmap setting from source texture
                    readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, texture.mipmapCount > 1);
                    readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readable.Apply(texture.mipmapCount > 1);

                    return readable.GetPixels();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[TextureCompressor] Failed to read pixels from texture '{texture.name}': {e.Message}");
                    return new Color[0];
                }
                finally
                {
                    RenderTexture.active = previous;

                    if (rt != null)
                    {
                        RenderTexture.ReleaseTemporary(rt);
                    }

                    if (readable != null)
                    {
                        Object.DestroyImmediate(readable);
                    }
                }
            }
        }
    }
}
