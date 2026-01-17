using dev.limitex.avatar.compressor.editor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Fast analysis strategy using Sobel gradient, spatial frequency, and color variance.
    /// </summary>
    public class FastAnalysisStrategy : ITextureComplexityAnalyzer
    {
        public TextureComplexityResult Analyze(ProcessedPixelData data)
        {
            float gradient = ImageMath.CalculateSobelGradient(
                data.Grayscale,
                data.Width,
                data.Height,
                data.OpaqueCount
            );

            float spatialFreq = ImageMath.CalculateSpatialFrequency(
                data.Grayscale,
                data.Width,
                data.Height,
                data.OpaqueCount
            );

            float colorVar = ImageMath.CalculateColorVariance(data.OpaquePixels, data.OpaqueCount);

            float normalizedGradient = MathUtils.NormalizeWithPercentile(
                gradient,
                AnalysisConstants.GradientPercentileLow,
                AnalysisConstants.GradientPercentileHigh
            );
            float normalizedSpatialFreq = MathUtils.NormalizeWithPercentile(
                spatialFreq,
                AnalysisConstants.SpatialFreqPercentileLow,
                AnalysisConstants.SpatialFreqPercentileHigh
            );
            float normalizedColorVar = MathUtils.NormalizeWithPercentile(
                colorVar,
                AnalysisConstants.ColorVariancePercentileLow,
                AnalysisConstants.ColorVariancePercentileHigh
            );

            float score = Mathf.Clamp01(
                AnalysisConstants.FastGradientWeight * normalizedGradient
                    + AnalysisConstants.FastSpatialFrequencyWeight * normalizedSpatialFreq
                    + AnalysisConstants.FastColorVarianceWeight * normalizedColorVar
            );

            return new TextureComplexityResult(score);
        }
    }
}
