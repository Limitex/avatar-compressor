using UnityEngine;
using UnityEditor;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Service for resizing textures.
    /// Uses lock to ensure thread safety for RenderTexture operations.
    /// </summary>
    public class TextureResizer
    {
        // Lock object for thread-safe RenderTexture operations
        private static readonly object RenderTextureLock = new object();

        private readonly int _minResolution;
        private readonly int _maxResolution;
        private readonly bool _forcePowerOfTwo;

        public TextureResizer(int minResolution, int maxResolution, bool forcePowerOfTwo)
        {
            _minResolution = minResolution;
            _maxResolution = maxResolution;
            _forcePowerOfTwo = forcePowerOfTwo;
        }

        /// <summary>
        /// Resizes a texture using pre-computed analysis result.
        /// </summary>
        public Texture2D Resize(Texture2D source, TextureAnalysisResult analysis, bool enableLogging, bool isNormalMap = false)
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
            CompressTexture(result, source.format, isNormalMap);

            if (enableLogging)
            {
                var format = result.format;
                Debug.Log($"[TextureCompressor] {source.name}: " +
                          $"{source.width}x{source.height} → " +
                          $"{result.width}x{result.height} ({format}) " +
                          $"(Complexity: {analysis.NormalizedComplexity:P0}, " +
                          $"Divisor: {analysis.RecommendedDivisor}x)");
            }

            return result;
        }

        /// <summary>
        /// Compresses a texture, preserving the original format if already compressed,
        /// or converting to DXT if uncompressed.
        /// </summary>
        private void CompressTexture(Texture2D texture, TextureFormat sourceFormat, bool isNormalMap)
        {
            TextureFormat targetFormat;

            // Check if source is already a compressed format - preserve it
            if (IsCompressedFormat(sourceFormat))
            {
                targetFormat = sourceFormat;
            }
            else
            {
                // Uncompressed format (RGBA32, ARGB32, RGB24, etc.) - convert to DXT
                if (isNormalMap)
                {
                    // BC5 is optimal for normal maps (2 channels, high quality)
                    targetFormat = TextureFormat.BC5;
                }
                else if (HasSignificantAlpha(texture))
                {
                    // DXT5 for textures with alpha channel (8 bpp)
                    targetFormat = TextureFormat.DXT5;
                }
                else
                {
                    // DXT1 for opaque textures (4 bpp, most efficient)
                    targetFormat = TextureFormat.DXT1;
                }
            }

            try
            {
                EditorUtility.CompressTexture(texture, targetFormat, TextureCompressionQuality.Best);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TextureCompressor] Failed to compress texture to {targetFormat}: {e.Message}");
            }
        }

        /// <summary>
        /// Checks if the texture format is a compressed format.
        /// </summary>
        private bool IsCompressedFormat(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.DXT1:
                case TextureFormat.DXT1Crunched:
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC4:
                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_5x5:
                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_8x8:
                case TextureFormat.ASTC_10x10:
                case TextureFormat.ASTC_12x12:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGB:
                case TextureFormat.ETC2_RGBA1:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.PVRTC_RGBA2:
                case TextureFormat.PVRTC_RGBA4:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks if the texture has significant alpha (non-fully-opaque pixels).
        /// </summary>
        private bool HasSignificantAlpha(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            int sampleCount = Mathf.Min(pixels.Length, 10000);
            int step = Mathf.Max(1, pixels.Length / sampleCount);

            for (int i = 0; i < pixels.Length; i += step)
            {
                if (pixels[i].a < 250)
                {
                    return true;
                }
            }
            return false;
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
