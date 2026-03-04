using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Shared helper for building TextureAnalysisResult from a raw complexity score.
    /// Centralizes the score → divisor → resolution pipeline used by both CPU and GPU backends.
    /// </summary>
    public static class AnalysisResultHelper
    {
        public static TextureAnalysisResult BuildResult(
            float score,
            int sourceWidth,
            int sourceHeight,
            bool isEmission,
            bool isNormalMap,
            bool hasSignificantAlpha,
            ComplexityCalculator complexityCalc,
            TextureProcessor processor
        )
        {
            float clamped = Mathf.Clamp01(score);
            if (isEmission && !isNormalMap)
            {
                clamped = Mathf.Clamp01(clamped / AnalysisConstants.EmissionScoreBoostDivisor);
            }
            int divisor = complexityCalc.CalculateRecommendedDivisor(clamped);
            Vector2Int resolution = processor.CalculateNewDimensions(
                sourceWidth,
                sourceHeight,
                divisor
            );

            return new TextureAnalysisResult(clamped, divisor, resolution, hasSignificantAlpha);
        }
    }
}
