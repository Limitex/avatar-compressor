using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Service for calculating recommended divisor from complexity score.
    /// </summary>
    public class ComplexityCalculator
    {
        private readonly float _highComplexityThreshold;
        private readonly float _lowComplexityThreshold;
        private readonly int _minDivisor;
        private readonly int _maxDivisor;

        public ComplexityCalculator(
            float highComplexityThreshold,
            float lowComplexityThreshold,
            int minDivisor,
            int maxDivisor
        )
        {
            _highComplexityThreshold = highComplexityThreshold;
            _lowComplexityThreshold = lowComplexityThreshold;
            _minDivisor = minDivisor;
            _maxDivisor = maxDivisor;
        }

        /// <summary>
        /// Calculates recommended divisor based on complexity.
        /// High complexity = low divisor (preserve detail).
        /// Low complexity = high divisor (more compression).
        /// </summary>
        public int CalculateRecommendedDivisor(float complexity)
        {
            float t;
            if (Mathf.Approximately(_highComplexityThreshold, _lowComplexityThreshold))
            {
                // Avoid division by zero - use middle divisor
                t = 0.5f;
            }
            else if (complexity >= _highComplexityThreshold)
            {
                t = 0f;
            }
            else if (complexity <= _lowComplexityThreshold)
            {
                t = 1f;
            }
            else
            {
                t =
                    1f
                    - (complexity - _lowComplexityThreshold)
                        / (_highComplexityThreshold - _lowComplexityThreshold);
            }

            float logMin = Mathf.Log(_minDivisor, 2);
            float logMax = Mathf.Log(_maxDivisor, 2);
            float logDivisor = Mathf.Lerp(logMin, logMax, t);

            int divisor = Mathf.RoundToInt(Mathf.Pow(2, Mathf.Round(logDivisor)));
            return Mathf.Clamp(divisor, _minDivisor, _maxDivisor);
        }
    }
}
