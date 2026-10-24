using System;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    internal static class TextureCompressorPropertyLabels
    {
        internal static GUIContent Content(string propertyName)
        {
            (string labelKey, bool hasTooltip) = propertyName switch
            {
                nameof(CustomTextureCompressorPreset.Lock) => (
                    "TextureCompressor:prop:presetLock",
                    true
                ),
                nameof(CustomTextureCompressorPreset.Description) => (
                    "TextureCompressor:prop:presetDescription",
                    true
                ),
                nameof(CustomTextureCompressorPreset.MenuPath) => (
                    "TextureCompressor:prop:presetMenuPath",
                    true
                ),
                nameof(CustomTextureCompressorPreset.MenuOrder) => (
                    "TextureCompressor:prop:presetMenuOrder",
                    true
                ),
                nameof(CustomTextureCompressorPreset.Strategy) => (
                    "TextureCompressor:prop:strategy",
                    true
                ),
                nameof(CustomTextureCompressorPreset.FastWeight) => (
                    "TextureCompressor:prop:fastWeight",
                    false
                ),
                nameof(CustomTextureCompressorPreset.HighAccuracyWeight) => (
                    "TextureCompressor:prop:highAccuracyWeight",
                    false
                ),
                nameof(CustomTextureCompressorPreset.PerceptualWeight) => (
                    "TextureCompressor:prop:perceptualWeight",
                    false
                ),
                nameof(CustomTextureCompressorPreset.HighComplexityThreshold) => (
                    "TextureCompressor:prop:highThreshold",
                    true
                ),
                nameof(CustomTextureCompressorPreset.LowComplexityThreshold) => (
                    "TextureCompressor:prop:lowThreshold",
                    true
                ),
                nameof(CustomTextureCompressorPreset.MinDivisor) => (
                    "TextureCompressor:prop:minDivisor",
                    true
                ),
                nameof(CustomTextureCompressorPreset.MaxDivisor) => (
                    "TextureCompressor:prop:maxDivisor",
                    true
                ),
                nameof(CustomTextureCompressorPreset.MaxResolution) => (
                    "TextureCompressor:prop:maxResolution",
                    true
                ),
                nameof(CustomTextureCompressorPreset.MinResolution) => (
                    "TextureCompressor:prop:minResolution",
                    true
                ),
                nameof(CustomTextureCompressorPreset.ForcePowerOfTwo) => (
                    "TextureCompressor:prop:forcePowerOfTwo",
                    true
                ),
                nameof(CustomTextureCompressorPreset.MinSourceSize) => (
                    "TextureCompressor:prop:minSourceSize",
                    true
                ),
                nameof(CustomTextureCompressorPreset.SkipIfSmallerThan) => (
                    "TextureCompressor:prop:skipIfSmaller",
                    true
                ),
                nameof(CustomTextureCompressorPreset.TargetPlatform) => (
                    "TextureCompressor:prop:targetPlatform",
                    true
                ),
                nameof(CustomTextureCompressorPreset.UseHighQualityFormatForHighComplexity) => (
                    "TextureCompressor:prop:highQualityComplex",
                    true
                ),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(propertyName),
                    propertyName,
                    "No localized label is defined for this setting."
                ),
            };

            return AvatarCompressorLocalization.Content(
                labelKey,
                hasTooltip ? labelKey + ":tooltip" : null
            );
        }
    }
}
