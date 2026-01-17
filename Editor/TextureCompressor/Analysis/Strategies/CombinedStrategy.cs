using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Combined analysis strategy using weighted average of Fast, HighAccuracy, and Perceptual strategies.
    /// </summary>
    public class CombinedStrategy : ITextureComplexityAnalyzer
    {
        private readonly FastAnalysisStrategy _fastStrategy;
        private readonly HighAccuracyStrategy _highAccuracyStrategy;
        private readonly PerceptualStrategy _perceptualStrategy;

        private readonly float _fastWeight;
        private readonly float _highAccuracyWeight;
        private readonly float _perceptualWeight;

        public CombinedStrategy(float fastWeight, float highAccuracyWeight, float perceptualWeight)
        {
            _fastStrategy = new FastAnalysisStrategy();
            _highAccuracyStrategy = new HighAccuracyStrategy();
            _perceptualStrategy = new PerceptualStrategy();

            _fastWeight = fastWeight;
            _highAccuracyWeight = highAccuracyWeight;
            _perceptualWeight = perceptualWeight;
        }

        public TextureComplexityResult Analyze(ProcessedPixelData data)
        {
            float fast = _fastStrategy.Analyze(data).Score;
            float highAcc = _highAccuracyStrategy.Analyze(data).Score;
            float perceptual = _perceptualStrategy.Analyze(data).Score;

            float totalWeight = _fastWeight + _highAccuracyWeight + _perceptualWeight;

            // Avoid division by zero - use equal weights if all are zero
            if (totalWeight < AnalysisConstants.ZeroWeightThreshold)
            {
                return new TextureComplexityResult(
                    Mathf.Clamp01((fast + highAcc + perceptual) / 3f),
                    "Combined analysis with equal weights (all weights were zero)");
            }

            float combined = (
                fast * _fastWeight +
                highAcc * _highAccuracyWeight +
                perceptual * _perceptualWeight
            ) / totalWeight;

            return new TextureComplexityResult(Mathf.Clamp01(combined));
        }
    }
}
