using UnityEngine;
using UnityEditor;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Service for selecting and applying texture compression formats.
    /// Separates compression logic from resizing for single responsibility.
    /// </summary>
    public class TextureFormatSelector
    {
        private readonly CompressionPlatform _targetPlatform;
        private readonly bool _useHighQualityFormatForHighComplexity;
        private readonly float _highQualityComplexityThreshold;

        public TextureFormatSelector(
            CompressionPlatform targetPlatform = CompressionPlatform.Auto,
            bool useHighQualityFormatForHighComplexity = true,
            float highQualityComplexityThreshold = 0.7f)
        {
            _targetPlatform = targetPlatform;
            _useHighQualityFormatForHighComplexity = useHighQualityFormatForHighComplexity;
            _highQualityComplexityThreshold = highQualityComplexityThreshold;
        }

        /// <summary>
        /// Predicts the compression format that would be used for a texture without actually compressing it.
        /// Useful for preview functionality.
        /// </summary>
        /// <param name="isNormalMap">Whether this texture is a normal map</param>
        /// <param name="complexity">Normalized complexity value (0-1)</param>
        /// <param name="hasAlpha">Whether the texture has significant alpha</param>
        /// <returns>The predicted compression format</returns>
        public TextureFormat PredictFormat(bool isNormalMap, float complexity, bool hasAlpha)
        {
            var platform = ResolvePlatform(_targetPlatform);

            if (platform == CompressionPlatform.Mobile)
            {
                return SelectMobileFormat(isNormalMap, complexity, hasAlpha);
            }
            else
            {
                return SelectDesktopFormat(isNormalMap, complexity, hasAlpha);
            }
        }

        /// <summary>
        /// Compresses a texture using platform-appropriate format based on analysis.
        /// </summary>
        /// <param name="texture">The texture to compress (will be modified in place)</param>
        /// <param name="sourceFormat">The original format of the source texture</param>
        /// <param name="isNormalMap">Whether this texture is a normal map</param>
        /// <param name="complexity">Normalized complexity value (0-1)</param>
        /// <param name="formatOverride">Optional format override from frozen settings</param>
        /// <returns>True if compression was applied, false if skipped or failed</returns>
        public bool CompressTexture(Texture2D texture, TextureFormat sourceFormat, bool isNormalMap, float complexity, FrozenTextureFormat? formatOverride = null)
        {
            TextureFormat targetFormat;

            // Check for frozen format override (highest priority)
            if (formatOverride.HasValue && formatOverride.Value != FrozenTextureFormat.Auto)
            {
                targetFormat = ConvertFrozenFormat(formatOverride.Value);
            }
            // If source was already compressed, preserve the same format
            else if (IsCompressedFormat(sourceFormat))
            {
                targetFormat = sourceFormat;
            }
            else
            {
                // Determine if texture has alpha
                bool hasAlpha = HasSignificantAlpha(texture);

                // Select appropriate format based on platform and texture properties
                targetFormat = PredictFormat(isNormalMap, complexity, hasAlpha);
            }

            return ApplyCompression(texture, targetFormat);
        }

        /// <summary>
        /// Converts FrozenTextureFormat enum to Unity TextureFormat.
        /// </summary>
        public static TextureFormat ConvertFrozenFormat(FrozenTextureFormat format)
        {
            return format switch
            {
                FrozenTextureFormat.DXT1 => TextureFormat.DXT1,
                FrozenTextureFormat.DXT5 => TextureFormat.DXT5,
                FrozenTextureFormat.BC5 => TextureFormat.BC5,
                FrozenTextureFormat.BC7 => TextureFormat.BC7,
                FrozenTextureFormat.ASTC_4x4 => TextureFormat.ASTC_4x4,
                FrozenTextureFormat.ASTC_6x6 => TextureFormat.ASTC_6x6,
                FrozenTextureFormat.ASTC_8x8 => TextureFormat.ASTC_8x8,
                _ => throw new System.ArgumentException($"Unsupported frozen format: {format}")
            };
        }

        /// <summary>
        /// Applies compression to a texture with fallback handling.
        /// </summary>
        private bool ApplyCompression(Texture2D texture, TextureFormat targetFormat)
        {
            // Skip if texture is already in the target format
            if (texture.format == targetFormat)
            {
                return false;
            }

            try
            {
                EditorUtility.CompressTexture(texture, targetFormat, TextureCompressionQuality.Best);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TextureCompressor] Failed to compress texture to {targetFormat}: {e.Message}. " +
                                 $"Attempting fallback.");

                // Fallback to widely supported formats
                try
                {
                    var platform = ResolvePlatform(_targetPlatform);
                    var fallbackFormat = platform == CompressionPlatform.Mobile
                        ? TextureFormat.ASTC_6x6
                        : TextureFormat.DXT5;

                    EditorUtility.CompressTexture(texture, fallbackFormat, TextureCompressionQuality.Normal);
                    Debug.Log($"[TextureCompressor] Fallback compression to {fallbackFormat} succeeded.");
                    return true;
                }
                catch (System.Exception fallbackEx)
                {
                    Debug.LogError($"[TextureCompressor] Fallback compression also failed: {fallbackEx.Message}. " +
                                   $"Texture will remain uncompressed.");
                    return false;
                }
            }
        }

        /// <summary>
        /// Resolves the target platform from settings or auto-detects from build target.
        /// </summary>
        private static CompressionPlatform ResolvePlatform(CompressionPlatform setting)
        {
            if (setting != CompressionPlatform.Auto)
            {
                return setting;
            }

            var target = EditorUserBuildSettings.activeBuildTarget;
            return target == BuildTarget.Android
                ? CompressionPlatform.Mobile
                : CompressionPlatform.Desktop;
        }

        /// <summary>
        /// Selects compression format for Desktop (PC VRChat) - DXT/BC formats.
        /// </summary>
        private TextureFormat SelectDesktopFormat(bool isNormalMap, float complexity, bool hasAlpha)
        {
            if (isNormalMap)
            {
                // If normal map has alpha channel, use BC7 to preserve it
                if (hasAlpha)
                {
                    return TextureFormat.BC7;
                }
                // BC5 is optimal for normal maps without alpha (2 channels, high quality)
                return TextureFormat.BC5;
            }
            else if (_useHighQualityFormatForHighComplexity && complexity >= _highQualityComplexityThreshold)
            {
                // BC7 for high complexity textures (highest quality, 8 bpp)
                return TextureFormat.BC7;
            }
            else if (hasAlpha)
            {
                // DXT5 for textures with alpha channel (8 bpp)
                return TextureFormat.DXT5;
            }
            else
            {
                // DXT1 for opaque textures (4 bpp, most efficient)
                return TextureFormat.DXT1;
            }
        }

        /// <summary>
        /// Selects compression format for Mobile (Quest/Android) - ASTC formats.
        /// Uses complexity and alpha-based block size selection.
        /// </summary>
        private TextureFormat SelectMobileFormat(bool isNormalMap, float complexity, bool hasAlpha)
        {
            if (isNormalMap)
            {
                // ASTC 4x4 for normal maps (highest quality)
                return TextureFormat.ASTC_4x4;
            }

            // Alpha textures need higher quality to preserve transparency edges
            if (hasAlpha)
            {
                if (_useHighQualityFormatForHighComplexity && complexity >= _highQualityComplexityThreshold)
                {
                    // High complexity with alpha: ASTC 4x4 (8 bpp, highest quality)
                    return TextureFormat.ASTC_4x4;
                }
                else
                {
                    // Alpha textures: ASTC 6x6 minimum to preserve alpha edges
                    return TextureFormat.ASTC_6x6;
                }
            }

            // Opaque textures: complexity-based ASTC block size selection
            if (_useHighQualityFormatForHighComplexity && complexity >= _highQualityComplexityThreshold)
            {
                // High complexity: ASTC 4x4 (8 bpp, highest quality)
                return TextureFormat.ASTC_4x4;
            }
            else if (complexity >= _highQualityComplexityThreshold * AnalysisConstants.MediumComplexityRatio)
            {
                // Medium complexity: ASTC 6x6 (3.56 bpp, balanced)
                return TextureFormat.ASTC_6x6;
            }
            else
            {
                // Low complexity opaque: ASTC 8x8 (2 bpp, most efficient)
                return TextureFormat.ASTC_8x8;
            }
        }

        /// <summary>
        /// Checks if the texture format is a compressed format.
        /// </summary>
        public static bool IsCompressedFormat(TextureFormat format)
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
        /// Uses sampling for performance.
        /// </summary>
        public static bool HasSignificantAlpha(Texture2D texture)
        {
            // Assume alpha exists for null or non-readable textures to preserve quality.
            // This is a conservative approach that may result in slightly larger file sizes
            // but ensures transparency is not incorrectly discarded.
            if (texture == null || !texture.isReadable)
                return true;

            try
            {
                var pixels = texture.GetPixels32();
                int sampleCount = Mathf.Min(pixels.Length, 10000);
                int step = Mathf.Max(1, pixels.Length / sampleCount);

                for (int i = 0; i < pixels.Length; i += step)
                {
                    if (pixels[i].a < AnalysisConstants.SignificantAlphaThreshold)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                // Assume alpha exists on error to preserve quality
                return true;
            }
        }
    }
}
