using dev.limitex.avatar.compressor.common;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Fast analysis strategy using Sobel gradient, spatial frequency, and color variance.
    /// </summary>
    public class FastAnalysisStrategy : ITextureComplexityAnalyzer
    {
        public TextureComplexityResult Analyze(ProcessedPixelData data)
        {
            float gradient = ImageMath.CalculateSobelGradient(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount);

            float spatialFreq = ImageMath.CalculateSpatialFrequency(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount);

            float colorVar = ImageMath.CalculateColorVariance(
                data.OpaquePixels, data.OpaqueCount);

            float normalizedGradient = MathUtils.NormalizeWithPercentile(gradient, 0.05f, 0.8f);
            float normalizedSpatialFreq = MathUtils.NormalizeWithPercentile(spatialFreq, 0.01f, 0.15f);
            float normalizedColorVar = MathUtils.NormalizeWithPercentile(colorVar, 0.005f, 0.08f);

            float score = Mathf.Clamp01(
                0.4f * normalizedGradient +
                0.35f * normalizedSpatialFreq +
                0.25f * normalizedColorVar
            );

            return new TextureComplexityResult(score);
        }
    }
}
