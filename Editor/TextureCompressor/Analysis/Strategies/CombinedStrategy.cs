using System.Threading.Tasks;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Combined analysis strategy using weighted average of Fast, HighAccuracy, and Perceptual strategies.
    /// Sub-strategies run in parallel for improved throughput.
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
            // Run all three strategies in parallel (read-only access to shared data)
            float fast = 0f,
                highAcc = 0f,
                perceptual = 0f;

            var fastTask = Task.Run(() => fast = _fastStrategy.Analyze(data).Score);
            var highAccTask = Task.Run(() => highAcc = _highAccuracyStrategy.Analyze(data).Score);
            var perceptualTask = Task.Run(() =>
                perceptual = _perceptualStrategy.Analyze(data).Score
            );

            Task.WaitAll(fastTask, highAccTask, perceptualTask);

            float totalWeight = _fastWeight + _highAccuracyWeight + _perceptualWeight;

            // Avoid division by zero - use equal weights if all are zero
            if (totalWeight < AnalysisConstants.ZeroWeightThreshold)
            {
                return new TextureComplexityResult(
                    Mathf.Clamp01((fast + highAcc + perceptual) / 3f),
                    "Combined analysis with equal weights (all weights were zero)"
                );
            }

            float combined =
                (
                    fast * _fastWeight
                    + highAcc * _highAccuracyWeight
                    + perceptual * _perceptualWeight
                ) / totalWeight;

            return new TextureComplexityResult(Mathf.Clamp01(combined));
        }
    }
}
