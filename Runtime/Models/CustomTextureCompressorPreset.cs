using UnityEngine;

namespace dev.limitex.avatar.compressor
{
    /// <summary>
    /// ScriptableObject that stores custom texture compressor preset settings.
    /// Can be shared across multiple avatars and projects.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewTextureCompressorPreset",
        menuName = "Avatar Compressor/Texture Compressor/CustomTextureCompressorPreset"
    )]
    public class CustomTextureCompressorPreset : ScriptableObject
    {
        [Header("Preset Settings")]
        [Tooltip("Lock this preset to prevent editing when selected")]
        public bool Lock = false;

        [Tooltip("Description shown when this preset is selected")]
        [TextArea(2, 4)]
        public string Description = "";

        [Header("Menu")]
        [Tooltip(
            "Path in the Custom preset menu (e.g., 'Quest' or 'PC/High Detail'). Empty = not shown in menu."
        )]
        public string MenuPath = "";

        [Tooltip("Display order in the menu. Lower values appear first.")]
        public int MenuOrder = 1000;

        [Header("Analysis Strategy")]
        [Tooltip("Complexity analysis method")]
        public AnalysisStrategyType Strategy = AnalysisStrategyType.Combined;

        [Header("Combined Strategy Weights")]
        [Range(0f, 1f)]
        public float FastWeight = 0.3f;

        [Range(0f, 1f)]
        public float HighAccuracyWeight = 0.5f;

        [Range(0f, 1f)]
        public float PerceptualWeight = 0.2f;

        [Header("Complexity Thresholds")]
        [Tooltip("Textures with complexity above this will use minimal compression")]
        [Range(0f, 1f)]
        public float HighComplexityThreshold = 0.7f;

        [Tooltip("Textures with complexity below this will use maximum compression")]
        [Range(0f, 1f)]
        public float LowComplexityThreshold = 0.2f;

        [Header("Resolution Settings")]
        [Tooltip("Minimum resolution divisor (1 = no reduction)")]
        [Range(1, 4)]
        public int MinDivisor = 1;

        [Tooltip("Maximum resolution divisor")]
        [Range(2, 16)]
        public int MaxDivisor = 8;

        [Tooltip("Maximum output resolution")]
        public int MaxResolution = 2048;

        [Tooltip("Minimum output resolution")]
        public int MinResolution = 32;

        [Tooltip("Force output to power of 2 dimensions")]
        public bool ForcePowerOfTwo = true;

        [Header("Size Filters")]
        [Tooltip("Only process textures larger than this size")]
        public int MinSourceSize = 256;

        [Tooltip("Skip textures smaller than or equal to this size")]
        public int SkipIfSmallerThan = 128;

        [Header("Compression Format")]
        [Tooltip(
            "Target platform for compression format selection (Auto detects from build target)"
        )]
        public CompressionPlatform TargetPlatform = CompressionPlatform.Auto;

        [Tooltip("Use BC7/ASTC_4x4 format for high complexity textures (highest quality)")]
        public bool UseHighQualityFormatForHighComplexity = true;

        /// <summary>
        /// Applies settings from this preset to a TextureCompressor component.
        /// </summary>
        public void ApplyTo(TextureCompressor compressor)
        {
            compressor.Strategy = Strategy;
            compressor.FastWeight = FastWeight;
            compressor.HighAccuracyWeight = HighAccuracyWeight;
            compressor.PerceptualWeight = PerceptualWeight;
            compressor.HighComplexityThreshold = HighComplexityThreshold;
            compressor.LowComplexityThreshold = LowComplexityThreshold;
            compressor.MinDivisor = MinDivisor;
            compressor.MaxDivisor = MaxDivisor;
            compressor.MaxResolution = MaxResolution;
            compressor.MinResolution = MinResolution;
            compressor.ForcePowerOfTwo = ForcePowerOfTwo;
            compressor.MinSourceSize = MinSourceSize;
            compressor.SkipIfSmallerThan = SkipIfSmallerThan;
            compressor.TargetPlatform = TargetPlatform;
            compressor.UseHighQualityFormatForHighComplexity =
                UseHighQualityFormatForHighComplexity;
        }

        /// <summary>
        /// Copies settings from a TextureCompressor component to this preset.
        /// </summary>
        public void CopyFrom(TextureCompressor compressor)
        {
            Strategy = compressor.Strategy;
            FastWeight = compressor.FastWeight;
            HighAccuracyWeight = compressor.HighAccuracyWeight;
            PerceptualWeight = compressor.PerceptualWeight;
            HighComplexityThreshold = compressor.HighComplexityThreshold;
            LowComplexityThreshold = compressor.LowComplexityThreshold;
            MinDivisor = compressor.MinDivisor;
            MaxDivisor = compressor.MaxDivisor;
            MaxResolution = compressor.MaxResolution;
            MinResolution = compressor.MinResolution;
            ForcePowerOfTwo = compressor.ForcePowerOfTwo;
            MinSourceSize = compressor.MinSourceSize;
            SkipIfSmallerThan = compressor.SkipIfSmallerThan;
            TargetPlatform = compressor.TargetPlatform;
            UseHighQualityFormatForHighComplexity =
                compressor.UseHighQualityFormatForHighComplexity;
        }

        /// <summary>
        /// Checks if the given compressor has the same settings as this preset.
        /// </summary>
        public bool MatchesSettings(TextureCompressor compressor)
        {
            return Strategy == compressor.Strategy
                && Mathf.Approximately(FastWeight, compressor.FastWeight)
                && Mathf.Approximately(HighAccuracyWeight, compressor.HighAccuracyWeight)
                && Mathf.Approximately(PerceptualWeight, compressor.PerceptualWeight)
                && Mathf.Approximately(HighComplexityThreshold, compressor.HighComplexityThreshold)
                && Mathf.Approximately(LowComplexityThreshold, compressor.LowComplexityThreshold)
                && MinDivisor == compressor.MinDivisor
                && MaxDivisor == compressor.MaxDivisor
                && MaxResolution == compressor.MaxResolution
                && MinResolution == compressor.MinResolution
                && ForcePowerOfTwo == compressor.ForcePowerOfTwo
                && MinSourceSize == compressor.MinSourceSize
                && SkipIfSmallerThan == compressor.SkipIfSmallerThan
                && TargetPlatform == compressor.TargetPlatform
                && UseHighQualityFormatForHighComplexity
                    == compressor.UseHighQualityFormatForHighComplexity;
        }

        /// <summary>
        /// Checks if a specific field differs from the compressor's current value.
        /// </summary>
        /// <param name="fieldName">The field name (property name on TextureCompressor)</param>
        /// <param name="compressor">The compressor to compare against</param>
        /// <returns>True if the field value differs from the preset</returns>
        public bool IsFieldModified(string fieldName, TextureCompressor compressor)
        {
            return fieldName switch
            {
                nameof(Strategy) => Strategy != compressor.Strategy,
                nameof(FastWeight) => !Mathf.Approximately(FastWeight, compressor.FastWeight),
                nameof(HighAccuracyWeight) => !Mathf.Approximately(
                    HighAccuracyWeight,
                    compressor.HighAccuracyWeight
                ),
                nameof(PerceptualWeight) => !Mathf.Approximately(
                    PerceptualWeight,
                    compressor.PerceptualWeight
                ),
                nameof(HighComplexityThreshold) => !Mathf.Approximately(
                    HighComplexityThreshold,
                    compressor.HighComplexityThreshold
                ),
                nameof(LowComplexityThreshold) => !Mathf.Approximately(
                    LowComplexityThreshold,
                    compressor.LowComplexityThreshold
                ),
                nameof(MinDivisor) => MinDivisor != compressor.MinDivisor,
                nameof(MaxDivisor) => MaxDivisor != compressor.MaxDivisor,
                nameof(MaxResolution) => MaxResolution != compressor.MaxResolution,
                nameof(MinResolution) => MinResolution != compressor.MinResolution,
                nameof(ForcePowerOfTwo) => ForcePowerOfTwo != compressor.ForcePowerOfTwo,
                nameof(MinSourceSize) => MinSourceSize != compressor.MinSourceSize,
                nameof(SkipIfSmallerThan) => SkipIfSmallerThan != compressor.SkipIfSmallerThan,
                nameof(TargetPlatform) => TargetPlatform != compressor.TargetPlatform,
                nameof(UseHighQualityFormatForHighComplexity) =>
                    UseHighQualityFormatForHighComplexity
                        != compressor.UseHighQualityFormatForHighComplexity,
                _ => false,
            };
        }
    }
}
