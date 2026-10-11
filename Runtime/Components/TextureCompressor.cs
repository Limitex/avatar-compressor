using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace dev.limitex.avatar.compressor
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Avatar Compressor/LAC Texture Compressor")]
    internal sealed class TextureCompressor : MonoBehaviour, IEditorOnly
    {
        public CompressorPreset Preset = CompressorPreset.Balanced;

        public CustomTextureCompressorPreset CustomPresetAsset;

        public AnalysisStrategyType Strategy = AnalysisStrategyType.Combined;

        [Range(0f, 1f)]
        public float FastWeight = 0.3f;

        [Range(0f, 1f)]
        public float HighAccuracyWeight = 0.5f;

        [Range(0f, 1f)]
        public float PerceptualWeight = 0.2f;

        [Range(0f, 1f)]
        public float HighComplexityThreshold = 0.7f;

        [Range(0f, 1f)]
        public float LowComplexityThreshold = 0.2f;

        [Range(1, 4)]
        public int MinDivisor = 1;

        [Range(2, 16)]
        public int MaxDivisor = 8;

        public int MaxResolution = 2048;

        public int MinResolution = 64;

        public bool ForcePowerOfTwo = true;

        public bool ProcessMainTextures = true;

        public bool ProcessNormalMaps = true;

        public bool ProcessEmissionMaps = true;

        public bool ProcessOtherTextures = true;

        public bool SkipUnknownUncompressedTextures = true;

        public bool DetectUnusedTextures = true;

        public bool BakeLilToonTextures = true;

        public int MinSourceSize = 256;

        public int SkipIfSmallerThan = 128;

        public List<Texture2D> ExcludedTextures = new List<Texture2D>();

        public List<string> ExcludedPaths = new List<string>(ExcludedPathPresets.GetDefaultPaths());

        public CompressionPlatform TargetPlatform = CompressionPlatform.Auto;

        public bool UseHighQualityFormatForHighComplexity = true;

        public List<FrozenTextureSettings> FrozenTextures = new List<FrozenTextureSettings>();

        /// <summary>
        /// Gets frozen settings for a texture by its GUID.
        /// </summary>
        public FrozenTextureSettings GetFrozenSettings(string guid)
        {
            return FrozenTextures.Find(f => f.TextureGuid == guid);
        }

        /// <summary>
        /// Checks if a texture is frozen by its GUID.
        /// </summary>
        public bool IsFrozen(string guid)
        {
            return FrozenTextures.Exists(f => f.TextureGuid == guid);
        }

        /// <summary>
        /// Adds or updates frozen settings for a texture.
        /// Validates that divisor is a valid power of 2 (1, 2, 4, 8, or 16).
        /// </summary>
        public void SetFrozenSettings(string guid, FrozenTextureSettings settings)
        {
            // Validate divisor is a valid power of 2
            if (!IsValidDivisor(settings.Divisor))
            {
                Debug.LogWarning(
                    $"[TextureCompressor] Invalid divisor {settings.Divisor} for frozen texture. Using closest valid value."
                );
                settings.Divisor = GetClosestValidDivisor(settings.Divisor);
            }

            var existingIndex = FrozenTextures.FindIndex(f => f.TextureGuid == guid);
            if (existingIndex >= 0)
                FrozenTextures[existingIndex] = settings;
            else
                FrozenTextures.Add(settings);
        }

        /// <summary>
        /// Checks if a divisor value is valid (1, 2, 4, 8, or 16).
        /// </summary>
        public static bool IsValidDivisor(int divisor)
        {
            return divisor == 1 || divisor == 2 || divisor == 4 || divisor == 8 || divisor == 16;
        }

        /// <summary>
        /// Gets the closest valid divisor for an invalid value.
        /// </summary>
        public static int GetClosestValidDivisor(int divisor)
        {
            int[] validDivisors = { 1, 2, 4, 8, 16 };
            int closest = 1;
            int minDiff = int.MaxValue;

            foreach (int valid in validDivisors)
            {
                int diff = System.Math.Abs(divisor - valid);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = valid;
                }
            }

            return closest;
        }

        /// <summary>
        /// Removes frozen settings for a texture by its GUID.
        /// </summary>
        public void UnfreezeTexture(string guid)
        {
            FrozenTextures.RemoveAll(f => f.TextureGuid == guid);
        }

        /// <summary>
        /// Called by Unity when the component is first added or reset.
        /// Applies the default preset settings.
        /// </summary>
        private void Reset()
        {
            ApplyPreset(Preset);
        }

        /// <summary>
        /// Applies preset settings to this component.
        /// </summary>
        public void ApplyPreset(CompressorPreset preset)
        {
            Preset = preset;

            switch (preset)
            {
                case CompressorPreset.HighQuality:
                    ApplyHighQualityPreset();
                    break;
                case CompressorPreset.Quality:
                    ApplyQualityPreset();
                    break;
                case CompressorPreset.Balanced:
                    ApplyBalancedPreset();
                    break;
                case CompressorPreset.Aggressive:
                    ApplyAggressivePreset();
                    break;
                case CompressorPreset.Maximum:
                    ApplyMaximumPreset();
                    break;
                case CompressorPreset.Custom:
                    ApplyCustomPreset();
                    break;
            }
        }

        private void ApplyHighQualityPreset()
        {
            Strategy = AnalysisStrategyType.Combined;
            FastWeight = 0.1f;
            HighAccuracyWeight = 0.5f;
            PerceptualWeight = 0.4f;
            HighComplexityThreshold = 0.3f;
            LowComplexityThreshold = 0.1f;
            MinDivisor = 1;
            MaxDivisor = 2;
            MaxResolution = 2048;
            MinResolution = 256;
            ForcePowerOfTwo = true;
            MinSourceSize = 1024;
            SkipIfSmallerThan = 512;
            TargetPlatform = CompressionPlatform.Auto;
            UseHighQualityFormatForHighComplexity = true;
        }

        private void ApplyQualityPreset()
        {
            Strategy = AnalysisStrategyType.Combined;
            FastWeight = 0.2f;
            HighAccuracyWeight = 0.5f;
            PerceptualWeight = 0.3f;
            HighComplexityThreshold = 0.5f;
            LowComplexityThreshold = 0.15f;
            MinDivisor = 1;
            MaxDivisor = 4;
            MaxResolution = 2048;
            MinResolution = 128;
            ForcePowerOfTwo = true;
            MinSourceSize = 512;
            SkipIfSmallerThan = 256;
            TargetPlatform = CompressionPlatform.Auto;
            UseHighQualityFormatForHighComplexity = true;
        }

        private void ApplyBalancedPreset()
        {
            Strategy = AnalysisStrategyType.Combined;
            FastWeight = 0.3f;
            HighAccuracyWeight = 0.5f;
            PerceptualWeight = 0.2f;
            HighComplexityThreshold = 0.7f;
            LowComplexityThreshold = 0.2f;
            MinDivisor = 1;
            MaxDivisor = 8;
            MaxResolution = 2048;
            MinResolution = 64;
            ForcePowerOfTwo = true;
            MinSourceSize = 256;
            SkipIfSmallerThan = 128;
            TargetPlatform = CompressionPlatform.Auto;
            UseHighQualityFormatForHighComplexity = true;
        }

        private void ApplyAggressivePreset()
        {
            Strategy = AnalysisStrategyType.Combined;
            FastWeight = 0.4f;
            HighAccuracyWeight = 0.4f;
            PerceptualWeight = 0.2f;
            HighComplexityThreshold = 0.70f;
            LowComplexityThreshold = 0.20f;
            MinDivisor = 2;
            MaxDivisor = 8;
            MaxResolution = 2048;
            MinResolution = 32;
            ForcePowerOfTwo = true;
            MinSourceSize = 128;
            SkipIfSmallerThan = 64;
            TargetPlatform = CompressionPlatform.Auto;
            UseHighQualityFormatForHighComplexity = false;
        }

        private void ApplyMaximumPreset()
        {
            Strategy = AnalysisStrategyType.Combined;
            FastWeight = 0.5f;
            HighAccuracyWeight = 0.3f;
            PerceptualWeight = 0.2f;
            HighComplexityThreshold = 0.70f;
            LowComplexityThreshold = 0.15f;
            MinDivisor = 4;
            MaxDivisor = 16;
            MaxResolution = 2048;
            MinResolution = 32;
            ForcePowerOfTwo = true;
            MinSourceSize = 64;
            SkipIfSmallerThan = 32;
            TargetPlatform = CompressionPlatform.Auto;
            UseHighQualityFormatForHighComplexity = false;
        }

        private void ApplyCustomPreset()
        {
            if (CustomPresetAsset != null)
            {
                CustomPresetAsset.ApplyTo(this);
            }
        }
    }

    internal enum CompressorPreset
    {
        HighQuality,
        Quality,
        Balanced,
        Aggressive,
        Maximum,
        Custom,
    }

    /// <summary>
    /// Strategy type for complexity analysis.
    /// </summary>
    internal enum AnalysisStrategyType
    {
        Fast,
        HighAccuracy,
        Perceptual,
        Combined,
    }

    /// <summary>
    /// Target platform for texture compression format selection.
    /// </summary>
    internal enum CompressionPlatform
    {
        Auto, // Detect from build target
        Desktop, // DXT/BC formats (PC VRChat)
        Mobile, // ASTC formats (Quest/Android)
    }
}
