using System;
using System.Collections.Generic;

namespace dev.limitex.avatar.compressor.common
{
    /// <summary>
    /// Fuzzy string matching using the Bitap algorithm (Shift-Or / Baeza-Yates-Gonnet).
    /// Supports approximate string matching with configurable error tolerance.
    /// </summary>
    public static class FuzzyMatcher
    {
        private const int MaxPatternLength = 31; // int bit width - 1
        private const int NoMatchMask = ~0; // All bits set (no match)

        /// <summary>
        /// Performs fuzzy matching using the Bitap algorithm.
        /// </summary>
        /// <param name="text">The text to search in</param>
        /// <param name="pattern">The pattern to search for</param>
        /// <param name="maxErrors">Maximum number of errors (insertions, deletions, substitutions) allowed. Must be non-negative.</param>
        /// <returns>True if the pattern matches within the allowed error tolerance</returns>
        public static bool Match(string text, string pattern, int maxErrors = 1)
        {
            if (string.IsNullOrEmpty(pattern))
                return true;

            if (string.IsNullOrEmpty(text))
                return false;

            // Ensure maxErrors is non-negative
            if (maxErrors < 0)
                maxErrors = 0;

            // Case-insensitive matching
            text = text.ToLowerInvariant();
            pattern = pattern.ToLowerInvariant();

            int m = pattern.Length;
            int n = text.Length;

            // Pattern is too long compared to text - cannot match even with maxErrors
            // (need at least pattern.Length - maxErrors characters in text)
            if (m > n + maxErrors)
                return false;

            // Exact substring match (fast path for short patterns or exact matches)
            if (text.Contains(pattern))
                return true;

            // Pattern too long for bitap - fall back to simple contains with tolerance
            if (m > MaxPatternLength)
            {
                return FallbackMatch(text, pattern, maxErrors);
            }

            // Bitap algorithm with Wu-Manber modification for fuzzy matching
            return BitapFuzzyMatch(text, pattern, maxErrors);
        }

        /// <summary>
        /// Core Bitap algorithm implementation with fuzzy matching support.
        /// Uses Dictionary for Unicode support (handles non-ASCII characters like Japanese).
        /// </summary>
        private static bool BitapFuzzyMatch(string text, string pattern, int maxErrors)
        {
            int m = pattern.Length;
            int n = text.Length;

            // Build pattern bitmasks for each character using Dictionary for Unicode support
            // patternMask[c] has bit i set if pattern[i] == c
            var patternMask = new Dictionary<char, int>();

            for (int i = 0; i < m; i++)
            {
                char c = pattern[i];
                if (!patternMask.ContainsKey(c))
                    patternMask[c] = NoMatchMask;
                patternMask[c] &= ~(1 << i);
            }

            // R[k] tracks matching state for k errors
            // Bit i is 0 if the first i characters of pattern match ending at current position
            var R = new int[maxErrors + 1];
            for (int k = 0; k <= maxErrors; k++)
                R[k] = NoMatchMask;

            int matchBit = 1 << (m - 1);

            for (int i = 0; i < n; i++)
            {
                char c = text[i];
                int mask = patternMask.TryGetValue(c, out int m_) ? m_ : NoMatchMask;

                int oldRk1 = R[0];

                // Exact match state update
                R[0] = (R[0] << 1) | mask;

                // Fuzzy match state updates for k = 1 to maxErrors
                for (int k = 1; k <= maxErrors; k++)
                {
                    int tmp = R[k];
                    // R[k] = (R[k] << 1 | patternMask[c]) - current char match with k errors
                    //      & (oldRk1 << 1)               - substitution: match with k-1 errors + 1 substitution
                    //      & (R[k-1] << 1)               - insertion: match with k-1 errors + skip pattern char
                    //      & oldRk1                      - deletion: match with k-1 errors + skip text char
                    R[k] = ((R[k] << 1) | mask)
                         & (oldRk1 << 1)
                         & (R[k - 1] << 1)
                         & oldRk1;
                    oldRk1 = tmp;
                }

                // Check if any error level matched
                for (int k = 0; k <= maxErrors; k++)
                {
                    if ((R[k] & matchBit) == 0)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fallback matching for patterns longer than MaxPatternLength.
        /// Uses a sliding window approach with character-level comparison.
        /// </summary>
        private static bool FallbackMatch(string text, string pattern, int maxErrors)
        {
            int m = pattern.Length;
            int n = text.Length;

            if (m > n + maxErrors)
                return false;

            // Sliding window with Levenshtein-like distance check
            for (int start = 0; start <= n - m + maxErrors && start < n; start++)
            {
                int errors = 0;
                int pi = 0;
                int ti = start;

                while (pi < m && ti < n && errors <= maxErrors)
                {
                    if (pattern[pi] == text[ti])
                    {
                        pi++;
                        ti++;
                    }
                    else
                    {
                        errors++;
                        // Try substitution (advance both)
                        pi++;
                        ti++;
                    }
                }

                // Check if we matched the whole pattern
                if (pi == m && errors <= maxErrors)
                    return true;
            }

            return false;
        }
    }
}
