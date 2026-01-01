using dev.limitex.avatar.compressor.common;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Perceptual analysis strategy using variance, edge density, and detail density.
    /// </summary>
    public class PerceptualStrategy : ITextureComplexityAnalyzer
    {
        public TextureComplexityResult Analyze(ProcessedPixelData data)
        {
            if (data.Width < 8 || data.Height < 8 || data.OpaqueCount < 64)
                return new TextureComplexityResult(0.5f);

            float avgVariance = ImageMath.CalculateBlockVariance(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount, 4);

            float avgEdge = ImageMath.CalculateEdgeDensity(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount);

            float detailDensity = ImageMath.CalculateDetailDensity(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount, avgVariance);

            float varianceScore = MathUtils.NormalizeWithPercentile(avgVariance, 0.001f, 0.05f);
            float edgeScore = MathUtils.NormalizeWithPercentile(avgEdge, 0.02f, 0.3f);

            float score = Mathf.Clamp01(
                0.4f * varianceScore +
                0.3f * edgeScore +
                0.3f * detailDensity
            );

            return new TextureComplexityResult(score);
        }
    }
}
