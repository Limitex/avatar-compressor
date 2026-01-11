using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace dev.limitex.avatar.compressor.texture
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Avatar Compressor/LAC Texture Compressor")]
    public class TextureCompressor : MonoBehaviour, IEditorOnly
    {
        [Header("Preset")]
        [Tooltip("Quick preset selection for common use cases")]
        public CompressorPreset Preset = CompressorPreset.Balanced;

        [Header("Analysis Strategy")]
        [Tooltip("Complexity analysis method")]
        public AnalysisStrategyType Strategy = AnalysisStrategyType.Combined;

        [Header("Combined Strategy Weights")]
        [Range(0f, 1f)] public float FastWeight = 0.3f;
        [Range(0f, 1f)] public float HighAccuracyWeight = 0.5f;
        [Range(0f, 1f)] public float PerceptualWeight = 0.2f;

        [Header("Complexity Thresholds")]
        [Tooltip("Textures with complexity above this will use minimal compression")]
        [Range(0f, 1f)] public float HighComplexityThreshold = 0.7f;
        [Tooltip("Textures with complexity below this will use maximum compression")]
        [Range(0f, 1f)] public float LowComplexityThreshold = 0.2f;

        [Header("Resolution Settings")]
        [Tooltip("Minimum resolution divisor (1 = no reduction)")]
        [Range(1, 4)] public int MinDivisor = 1;
        [Tooltip("Maximum resolution divisor")]
        [Range(2, 16)] public int MaxDivisor = 8;
        [Tooltip("Maximum output resolution")]
        public int MaxResolution = 2048;
        [Tooltip("Minimum output resolution")]
        public int MinResolution = 32;
        [Tooltip("Force output to power of 2 dimensions")]
        public bool ForcePowerOfTwo = true;

        [Header("Texture Filters")]
        [Tooltip("Process main textures (_MainTex, _BaseMap, etc.)")]
        public bool ProcessMainTextures = true;
        [Tooltip("Process normal maps")]
        public bool ProcessNormalMaps = true;
        [Tooltip("Process emission textures")]
        public bool ProcessEmissionMaps = true;
        [Tooltip("Process other textures (metallic, roughness, etc.)")]
        public bool ProcessOtherTextures = true;

        [Header("Size Filters")]
        [Tooltip("Only process textures larger than this size")]
        public int MinSourceSize = 256;
        [Tooltip("Skip textures smaller than or equal to this size")]
        public int SkipIfSmallerThan = 128;

        [Header("Path Exclusions")]
        [Tooltip("Texture paths starting with these prefixes will be excluded from compression")]
        public List<string> ExcludedPaths = new List<string>(ExcludedPathPresets.GetDefaultPaths());

        [Header("Compression Format")]
        [Tooltip("Target platform for compression format selection (Auto detects from build target)")]
        public CompressionPlatform TargetPlatform = CompressionPlatform.Auto;
        [Tooltip("Use BC7/ASTC_4x4 format for high complexity textures (highest quality)")]
        public bool UseHighQualityFormatForHighComplexity = true;

        [Header("Debug")]
        public bool EnableLogging = true;

        [Header("Frozen Textures")]
        [Tooltip("Textures with manually specified compression settings")]
        public List<FrozenTextureSettings> FrozenTextures = new List<FrozenTextureSettings>();

        /// <summary>
        /// Gets frozen settings for a texture by its asset path.
        /// </summary>
        public FrozenTextureSettings GetFrozenSettings(string assetPath)
        {
            return FrozenTextures.Find(f => f.TexturePath == assetPath);
        }

        /// <summary>
        /// Checks if a texture is frozen.
        /// </summary>
        public bool IsFrozen(string assetPath)
        {
            return FrozenTextures.Exists(f => f.TexturePath == assetPath);
        }

        /// <summary>
        /// Adds or updates frozen settings for a texture.
        /// Validates that divisor is a valid power of 2 (1, 2, 4, 8, or 16).
        /// </summary>
        public void SetFrozenSettings(string assetPath, FrozenTextureSettings settings)
        {
            // Validate divisor is a valid power of 2
            if (!IsValidDivisor(settings.Divisor))
            {
                Debug.LogWarning($"[TextureCompressor] Invalid divisor {settings.Divisor} for frozen texture. Using closest valid value.");
                settings.Divisor = GetClosestValidDivisor(settings.Divisor);
            }

            var existingIndex = FrozenTextures.FindIndex(f => f.TexturePath == assetPath);
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
        /// Removes frozen settings for a texture.
        /// </summary>
        public void UnfreezeTexture(string assetPath)
        {
            FrozenTextures.RemoveAll(f => f.TexturePath == assetPath);
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
            ProcessMainTextures = true;
            ProcessNormalMaps = true;
            ProcessEmissionMaps = true;
            ProcessOtherTextures = true;
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
            ProcessMainTextures = true;
            ProcessNormalMaps = true;
            ProcessEmissionMaps = true;
            ProcessOtherTextures = true;
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
            ProcessMainTextures = true;
            ProcessNormalMaps = true;
            ProcessEmissionMaps = true;
            ProcessOtherTextures = true;
            MinSourceSize = 256;
            SkipIfSmallerThan = 128;
            TargetPlatform = CompressionPlatform.Auto;
            UseHighQualityFormatForHighComplexity = true;
        }

        private void ApplyAggressivePreset()
        {
            Strategy = AnalysisStrategyType.Fast;
            FastWeight = 0.5f;
            HighAccuracyWeight = 0.3f;
            PerceptualWeight = 0.2f;
            HighComplexityThreshold = 0.8f;
            LowComplexityThreshold = 0.3f;
            MinDivisor = 2;
            MaxDivisor = 8;
            MaxResolution = 2048;
            MinResolution = 32;
            ForcePowerOfTwo = true;
            ProcessMainTextures = true;
            ProcessNormalMaps = true;
            ProcessEmissionMaps = true;
            ProcessOtherTextures = true;
            MinSourceSize = 128;
            SkipIfSmallerThan = 64;
            TargetPlatform = CompressionPlatform.Auto;
            UseHighQualityFormatForHighComplexity = false;
        }

        private void ApplyMaximumPreset()
        {
            Strategy = AnalysisStrategyType.Fast;
            FastWeight = 0.6f;
            HighAccuracyWeight = 0.3f;
            PerceptualWeight = 0.1f;
            HighComplexityThreshold = 0.9f;
            LowComplexityThreshold = 0.4f;
            MinDivisor = 2;
            MaxDivisor = 16;
            MaxResolution = 2048;
            MinResolution = 32;
            ForcePowerOfTwo = true;
            ProcessMainTextures = true;
            ProcessNormalMaps = true;
            ProcessEmissionMaps = true;
            ProcessOtherTextures = true;
            MinSourceSize = 64;
            SkipIfSmallerThan = 32;
            TargetPlatform = CompressionPlatform.Auto;
            UseHighQualityFormatForHighComplexity = false;
        }
    }

    public enum CompressorPreset
    {
        HighQuality,
        Quality,
        Balanced,
        Aggressive,
        Maximum,
        Custom
    }

    /// <summary>
    /// Strategy type for complexity analysis.
    /// </summary>
    public enum AnalysisStrategyType
    {
        Fast,
        HighAccuracy,
        Perceptual,
        Combined
    }

    /// <summary>
    /// Target platform for texture compression format selection.
    /// </summary>
    public enum CompressionPlatform
    {
        Auto,       // Detect from build target
        Desktop,    // DXT/BC formats (PC VRChat)
        Mobile      // ASTC formats (Quest/Android)
    }
}
