namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Centralized constants for texture analysis algorithms.
    /// </summary>
    public static class AnalysisConstants
    {
        #region Image Processing

        /// <summary>
        /// Block size for DCT (Discrete Cosine Transform) analysis.
        /// Standard JPEG uses 8x8 blocks.
        /// </summary>
        public const int DctBlockSize = 8;

        /// <summary>
        /// Number of gray levels for GLCM (Gray Level Co-occurrence Matrix).
        /// 16 levels provides good balance between accuracy and performance.
        /// </summary>
        public const int GlcmLevels = 16;

        /// <summary>
        /// Number of bins for histogram-based calculations.
        /// 256 corresponds to 8-bit grayscale precision.
        /// </summary>
        public const int HistogramBins = 256;

        #endregion

        #region Sampling

        /// <summary>
        /// Maximum pixels to sample for analysis (512x512).
        /// Larger textures are downsampled to this size for performance.
        /// </summary>
        public const int MaxSampledPixels = 262144;

        /// <summary>
        /// Minimum dimension when sampling large textures.
        /// </summary>
        public const int MinSampledDimension = 64;

        #endregion

        #region Analysis Thresholds

        /// <summary>
        /// Minimum texture dimension for perceptual analysis.
        /// Textures smaller than this use fallback scoring.
        /// </summary>
        public const int MinAnalysisDimension = 8;

        /// <summary>
        /// Minimum opaque pixel count for standard analysis.
        /// Below this threshold, textures are considered too sparse.
        /// </summary>
        public const int MinOpaquePixelsForStandardAnalysis = 100;

        /// <summary>
        /// Minimum dimension for normal map analysis.
        /// </summary>
        public const int MinNormalMapDimension = 4;

        /// <summary>
        /// Threshold for considering total weight as zero in combined strategy.
        /// </summary>
        public const float ZeroWeightThreshold = 0.0001f;

        #endregion

        #region Fast Strategy Weights

        /// <summary>
        /// Weight for Sobel gradient in fast analysis.
        /// </summary>
        public const float FastGradientWeight = 0.4f;

        /// <summary>
        /// Weight for spatial frequency in fast analysis.
        /// </summary>
        public const float FastSpatialFrequencyWeight = 0.35f;

        /// <summary>
        /// Weight for color variance in fast analysis.
        /// </summary>
        public const float FastColorVarianceWeight = 0.25f;

        #endregion

        #region Fast Strategy Normalization Bounds

        public const float GradientPercentileLow = 0.05f;
        public const float GradientPercentileHigh = 0.8f;
        public const float SpatialFreqPercentileLow = 0.01f;
        public const float SpatialFreqPercentileHigh = 0.15f;
        public const float ColorVariancePercentileLow = 0.005f;
        public const float ColorVariancePercentileHigh = 0.08f;

        #endregion

        #region High Accuracy Strategy Weights

        /// <summary>
        /// Weight for DCT high frequency ratio.
        /// </summary>
        public const float HighAccuracyDctWeight = 0.35f;

        /// <summary>
        /// Weight for GLCM contrast.
        /// </summary>
        public const float HighAccuracyContrastWeight = 0.25f;

        /// <summary>
        /// Weight for GLCM homogeneity (inverted).
        /// </summary>
        public const float HighAccuracyHomogeneityWeight = 0.20f;

        /// <summary>
        /// Weight for GLCM energy (inverted).
        /// </summary>
        public const float HighAccuracyEnergyWeight = 0.10f;

        /// <summary>
        /// Weight for entropy.
        /// </summary>
        public const float HighAccuracyEntropyWeight = 0.10f;

        #endregion

        #region High Accuracy Strategy Normalization Bounds

        public const float EntropyPercentileLow = 2f;
        public const float EntropyPercentileHigh = 7f;
        public const float ContrastPercentileLow = 5f;
        public const float ContrastPercentileHigh = 80f;

        #endregion

        #region Perceptual Strategy Weights

        /// <summary>
        /// Weight for block variance in perceptual analysis.
        /// </summary>
        public const float PerceptualVarianceWeight = 0.4f;

        /// <summary>
        /// Weight for edge density in perceptual analysis.
        /// </summary>
        public const float PerceptualEdgeWeight = 0.3f;

        /// <summary>
        /// Weight for detail density in perceptual analysis.
        /// </summary>
        public const float PerceptualDetailWeight = 0.3f;

        /// <summary>
        /// Block size for variance calculation in perceptual analysis.
        /// </summary>
        public const int PerceptualBlockSize = 4;

        #endregion

        #region Perceptual Strategy Normalization Bounds

        public const float VariancePercentileLow = 0.001f;
        public const float VariancePercentileHigh = 0.05f;
        public const float EdgePercentileLow = 0.02f;
        public const float EdgePercentileHigh = 0.3f;

        #endregion

        #region Normal Map Analysis

        /// <summary>
        /// Multiplier for normal map variation score.
        /// Amplifies the variation to better utilize the 0-1 range.
        /// </summary>
        public const float NormalMapVariationMultiplier = 2f;

        /// <summary>
        /// Step size for normal map sampling.
        /// </summary>
        public const int NormalMapSampleStep = 2;

        #endregion

        #region Combined Strategy Defaults

        /// <summary>
        /// Default weight for Fast strategy in combined analysis.
        /// </summary>
        public const float CombinedDefaultFastWeight = 0.3f;

        /// <summary>
        /// Default weight for HighAccuracy strategy in combined analysis.
        /// </summary>
        public const float CombinedDefaultHighAccuracyWeight = 0.5f;

        /// <summary>
        /// Default weight for Perceptual strategy in combined analysis.
        /// </summary>
        public const float CombinedDefaultPerceptualWeight = 0.2f;

        #endregion

        #region Default Values

        /// <summary>
        /// Default complexity score when analysis cannot be performed.
        /// 0.5 represents medium complexity (conservative approach).
        /// </summary>
        public const float DefaultComplexityScore = 0.5f;

        #endregion

        #region Alpha Detection

        /// <summary>
        /// Threshold for considering a pixel as having significant alpha.
        /// Pixels with alpha below this value (0-255) are considered transparent.
        /// 250 allows for near-opaque pixels while detecting meaningful transparency.
        /// </summary>
        public const byte SignificantAlphaThreshold = 250;

        #endregion

        #region Mobile Format Selection

        /// <summary>
        /// Ratio of high complexity threshold used to determine medium complexity.
        /// Medium complexity = complexity >= HighComplexityThreshold * MediumComplexityRatio.
        /// 0.5 means textures with complexity at half the high threshold are considered medium.
        /// </summary>
        public const float MediumComplexityRatio = 0.5f;

        #endregion
    }
}
