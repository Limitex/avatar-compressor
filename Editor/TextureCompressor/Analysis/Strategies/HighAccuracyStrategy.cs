using dev.limitex.avatar.compressor.editor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// High accuracy analysis strategy using DCT, GLCM, and entropy.
    /// </summary>
    public class HighAccuracyStrategy : ITextureComplexityAnalyzer
    {
        public TextureComplexityResult Analyze(ProcessedPixelData data)
        {
            float dctRatio = ImageMath.CalculateDctHighFrequencyRatio(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount);

            var glcm = ImageMath.CalculateGlcmFeatures(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount);

            float entropy = ImageMath.CalculateEntropy(
                data.Grayscale, data.OpaqueCount);

            float normalizedEntropy = MathUtils.NormalizeWithPercentile(
                entropy,
                AnalysisConstants.EntropyPercentileLow,
                AnalysisConstants.EntropyPercentileHigh);
            float normalizedContrast = MathUtils.NormalizeWithPercentile(
                glcm.contrast,
                AnalysisConstants.ContrastPercentileLow,
                AnalysisConstants.ContrastPercentileHigh);

            float score = Mathf.Clamp01(
                AnalysisConstants.HighAccuracyDctWeight * dctRatio +
                AnalysisConstants.HighAccuracyContrastWeight * normalizedContrast +
                AnalysisConstants.HighAccuracyHomogeneityWeight * (1f - glcm.homogeneity) +
                AnalysisConstants.HighAccuracyEnergyWeight * (1f - Mathf.Sqrt(glcm.energy)) +
                AnalysisConstants.HighAccuracyEntropyWeight * normalizedEntropy
            );

            return new TextureComplexityResult(score);
        }
    }
}
