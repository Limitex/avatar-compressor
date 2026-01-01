using dev.limitex.avatar.compressor.common;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture
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

            float normalizedEntropy = MathUtils.NormalizeWithPercentile(entropy, 2f, 7f);
            float normalizedContrast = MathUtils.NormalizeWithPercentile(glcm.contrast, 5f, 80f);

            float score = Mathf.Clamp01(
                0.35f * dctRatio +
                0.25f * normalizedContrast +
                0.20f * (1f - glcm.homogeneity) +
                0.10f * (1f - Mathf.Sqrt(glcm.energy)) +
                0.10f * normalizedEntropy
            );

            return new TextureComplexityResult(score);
        }
    }
}
