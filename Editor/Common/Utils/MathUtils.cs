using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Common mathematical utility methods.
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Normalizes a value using expected percentile range.
        /// Values below lowPercentile return 0, above highPercentile return 1.
        /// </summary>
        public static float NormalizeWithPercentile(
            float value,
            float lowPercentile,
            float highPercentile
        )
        {
            if (value <= lowPercentile)
                return 0f;
            if (value >= highPercentile)
                return 1f;
            return (value - lowPercentile) / (highPercentile - lowPercentile);
        }
    }
}
