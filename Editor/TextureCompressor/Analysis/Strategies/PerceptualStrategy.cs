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
            if (data.Width < AnalysisConstants.MinAnalysisDimension ||
                data.Height < AnalysisConstants.MinAnalysisDimension)
            {
                return new TextureComplexityResult(
                    AnalysisConstants.DefaultComplexityScore,
                    "Texture too small for perceptual analysis");
            }

            float avgVariance = ImageMath.CalculateBlockVariance(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount,
                AnalysisConstants.PerceptualBlockSize);

            float avgEdge = ImageMath.CalculateEdgeDensity(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount);

            float detailDensity = ImageMath.CalculateDetailDensity(
                data.Grayscale, data.Width, data.Height, data.OpaqueCount, avgVariance);

            float varianceScore = MathUtils.NormalizeWithPercentile(
                avgVariance,
                AnalysisConstants.VariancePercentileLow,
                AnalysisConstants.VariancePercentileHigh);
            float edgeScore = MathUtils.NormalizeWithPercentile(
                avgEdge,
                AnalysisConstants.EdgePercentileLow,
                AnalysisConstants.EdgePercentileHigh);

            float score = Mathf.Clamp01(
                AnalysisConstants.PerceptualVarianceWeight * varianceScore +
                AnalysisConstants.PerceptualEdgeWeight * edgeScore +
                AnalysisConstants.PerceptualDetailWeight * detailDensity
            );

            return new TextureComplexityResult(score);
        }
    }
}
