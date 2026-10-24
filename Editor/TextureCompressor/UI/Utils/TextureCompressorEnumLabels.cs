using System;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    internal static class TextureCompressorEnumLabels
    {
        internal static string Text<T>(T value)
            where T : struct, Enum
        {
            string key = Key(value);
            return key == null
                ? $"<{typeof(T).Name}.{value}>"
                : AvatarCompressorLocalization.Tr(key);
        }

        // Keep translation keys stable when renaming enum types or members.
        private static string Key(Enum value)
        {
            return value switch
            {
                AnalysisStrategyType.Fast => "TextureCompressor:enum:analysisStrategyTypeFast",
                AnalysisStrategyType.HighAccuracy =>
                    "TextureCompressor:enum:analysisStrategyTypeHighAccuracy",
                AnalysisStrategyType.Perceptual =>
                    "TextureCompressor:enum:analysisStrategyTypePerceptual",
                AnalysisStrategyType.Combined =>
                    "TextureCompressor:enum:analysisStrategyTypeCombined",
                CompressionPlatform.Auto => "TextureCompressor:enum:compressionPlatformAuto",
                CompressionPlatform.Desktop => "TextureCompressor:enum:compressionPlatformDesktop",
                CompressionPlatform.Mobile => "TextureCompressor:enum:compressionPlatformMobile",
                FrozenTextureFormat.Auto => "TextureCompressor:enum:frozenTextureFormatAuto",
                FrozenTextureFormat.DXT1 => "TextureCompressor:enum:frozenTextureFormatDXT1",
                FrozenTextureFormat.DXT5 => "TextureCompressor:enum:frozenTextureFormatDXT5",
                FrozenTextureFormat.BC5 => "TextureCompressor:enum:frozenTextureFormatBC5",
                FrozenTextureFormat.BC7 => "TextureCompressor:enum:frozenTextureFormatBC7",
                FrozenTextureFormat.ASTC_4x4 => "TextureCompressor:enum:frozenTextureFormatASTC4x4",
                FrozenTextureFormat.ASTC_6x6 => "TextureCompressor:enum:frozenTextureFormatASTC6x6",
                FrozenTextureFormat.ASTC_8x8 => "TextureCompressor:enum:frozenTextureFormatASTC8x8",
                AnalysisBackendPreference.Auto =>
                    "TextureCompressor:enum:analysisBackendPreferenceAuto",
                AnalysisBackendPreference.CPU =>
                    "TextureCompressor:enum:analysisBackendPreferenceCPU",
                ResizeBackendPreference.Auto =>
                    "TextureCompressor:enum:resizeBackendPreferenceAuto",
                ResizeBackendPreference.CPU => "TextureCompressor:enum:resizeBackendPreferenceCPU",
                TexturePropertyCategory.Main =>
                    "TextureCompressor:enum:texturePropertyCategoryMain",
                TexturePropertyCategory.Normal =>
                    "TextureCompressor:enum:texturePropertyCategoryNormal",
                TexturePropertyCategory.Emission =>
                    "TextureCompressor:enum:texturePropertyCategoryEmission",
                TexturePropertyCategory.Other =>
                    "TextureCompressor:enum:texturePropertyCategoryOther",
                SkipReason.None => "TextureCompressor:enum:skipReasonNone",
                SkipReason.TooSmall => "TextureCompressor:enum:skipReasonTooSmall",
                SkipReason.FilteredByType => "TextureCompressor:enum:skipReasonFilteredByType",
                SkipReason.FrozenSkip => "TextureCompressor:enum:skipReasonFrozenSkip",
                SkipReason.RuntimeGenerated => "TextureCompressor:enum:skipReasonRuntimeGenerated",
                SkipReason.ExcludedPath => "TextureCompressor:enum:skipReasonExcludedPath",
                SkipReason.ExcludedTexture => "TextureCompressor:enum:skipReasonExcludedTexture",
                SkipReason.UnknownUncompressedProperty =>
                    "TextureCompressor:enum:skipReasonUnknownUncompressedProperty",
                _ => null,
            };
        }
    }
}
