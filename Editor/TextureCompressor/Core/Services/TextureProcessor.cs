using UnityEngine;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Service for processing textures (resizing and compression).
    /// Uses lock to ensure thread safety for RenderTexture operations.
    /// </summary>
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
                result = Copy(source);
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
                          $"{source.width}x{source.height} → " +
                          $"{result.width}x{result.height} ({format}){frozenInfo} " +
                          $"(Complexity: {analysis.NormalizedComplexity:P0}, " +
                          $"Divisor: {analysis.RecommendedDivisor}x)");
            }

            return result;
        }

        /// <summary>
        /// Calculates new dimensions based on divisor.
        /// </summary>
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

                Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                result.Apply();

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

                    readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                    readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    readable.Apply();

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
