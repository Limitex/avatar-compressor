using System.Collections.Generic;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Fuzzy string matching using the Bitap algorithm with Wu-Manber extension.
    /// </summary>
    public static class FuzzyMatcher
    {
        private const int MaxPatternLength = 31;
        private const int NoMatchMask = ~0;

        /// <summary>
        /// Performs fuzzy matching using the Bitap algorithm.
        /// </summary>
        /// <param name="text">The text to search in</param>
        /// <param name="pattern">The pattern to search for</param>
        /// <param name="maxErrors">Maximum number of errors (insertions, deletions, substitutions) allowed</param>
        /// <returns>True if the pattern matches within the allowed error tolerance</returns>
        public static bool Match(string text, string pattern, int maxErrors = 1)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;

            if (string.IsNullOrEmpty(text))
                return false;

            if (maxErrors < 0)
                maxErrors = 0;

            text = text.ToLowerInvariant();
            pattern = pattern.ToLowerInvariant();

            int m = pattern.Length;
            int n = text.Length;

            if (m > n + maxErrors)
                return false;

            if (text.Contains(pattern))
                return true;

            if (m > MaxPatternLength)
                return false;

            return BitapFuzzyMatch(text, pattern, maxErrors);
        }

        private static bool BitapFuzzyMatch(string text, string pattern, int maxErrors)
        {
            int m = pattern.Length;
            int n = text.Length;

            // patternMask[c] has bit i cleared if pattern[i] == c
            var patternMask = new Dictionary<char, int>();
            for (int i = 0; i < m; i++)
            {
                char c = pattern[i];
                if (!patternMask.ContainsKey(c))
                    patternMask[c] = NoMatchMask;
                patternMask[c] &= ~(1 << i);
            }

            // R[k]: bit i is 0 if first i chars of pattern match with k errors
            var R = new int[maxErrors + 1];
            for (int k = 0; k <= maxErrors; k++)
                R[k] = NoMatchMask;

            int matchBit = 1 << (m - 1);

            for (int i = 0; i < n; i++)
            {
                char c = text[i];
                int mask = patternMask.TryGetValue(c, out int charMask) ? charMask : NoMatchMask;

                int oldRk1 = R[0];
                R[0] = (R[0] << 1) | mask;

                for (int k = 1; k <= maxErrors; k++)
                {
                    int tmp = R[k];
                    // match | substitution | insertion | deletion
                    R[k] = ((R[k] << 1) | mask) & (oldRk1 << 1) & (R[k - 1] << 1) & oldRk1;
                    oldRk1 = tmp;
                }

                for (int k = 0; k <= maxErrors; k++)
                {
                    if ((R[k] & matchBit) == 0)
                        return true;
                }
            }

            return false;
        }
    }
}
