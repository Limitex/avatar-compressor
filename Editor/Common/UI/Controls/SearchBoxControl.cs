using System;
using System.Collections.Generic;
using dev.limitex.avatar.compressor.common;
using UnityEngine;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// A reusable search box control with fuzzy search support and result caching.
    /// </summary>
    public class SearchBoxControl
    {
        /// <summary>
        /// Current search text.
        /// </summary>
        public string SearchText { get; private set; } = "";

        /// <summary>
        /// Whether fuzzy search is enabled.
        /// </summary>
        public bool UseFuzzySearch { get; private set; }

        /// <summary>
        /// Returns true if search is active (non-empty search text).
        /// </summary>
        public bool IsSearching => !string.IsNullOrEmpty(SearchText);

        /// <summary>
        /// Event fired when search text or fuzzy search option changes.
        /// </summary>
        public event Action OnSearchChanged;

        // Cache state
        private string _cachedSearchText;
        private bool _cachedUseFuzzySearch;
        private HashSet<int> _cachedMatchIndices;
        private Func<int, bool> _matchFunction;
        private int _cachedItemCount;

        /// <summary>
        /// Draws the search box UI.
        /// </summary>
        /// <returns>True if search parameters changed.</returns>
        public bool Draw()
        {
            bool changed = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            // Search icon
            var searchIcon = EditorGUIUtility.IconContent("d_Search Icon");
            if (searchIcon != null && searchIcon.image != null)
            {
                GUILayout.Label(searchIcon, GUILayout.Width(20), GUILayout.Height(18));
            }

            // Text field with placeholder
            var textFieldRect = EditorGUILayout.GetControlRect();
            GUI.SetNextControlName("SearchField");
            EditorGUI.BeginChangeCheck();
            var newSearchText = EditorGUI.TextField(textFieldRect, SearchText);
            if (EditorGUI.EndChangeCheck())
            {
                SearchText = newSearchText;
                changed = true;
            }

            // Draw placeholder when empty and not focused
            if (string.IsNullOrEmpty(SearchText) && GUI.GetNameOfFocusedControl() != "SearchField")
            {
                var placeholderRect = textFieldRect;
                placeholderRect.x += 3;
                EditorGUI.LabelField(placeholderRect, "Search textures...", EditorStylesCache.PlaceholderStyle);
            }

            // Clear button
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(SearchText));
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                SearchText = "";
                GUI.FocusControl(null);
                changed = true;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Show options when searching
            if (!string.IsNullOrEmpty(SearchText))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(30);

                // Fuzzy search toggle
                EditorGUI.BeginChangeCheck();
                var newUseFuzzySearch = EditorGUILayout.ToggleLeft("Fuzzy", UseFuzzySearch, GUILayout.Width(55));
                if (EditorGUI.EndChangeCheck())
                {
                    UseFuzzySearch = newUseFuzzySearch;
                    changed = true;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            if (changed)
            {
                InvalidateCache();
                OnSearchChanged?.Invoke();
            }

            return changed;
        }

        /// <summary>
        /// Draws the hit count display.
        /// </summary>
        /// <param name="totalHits">Total number of matching items.</param>
        /// <param name="frozenHits">Number of frozen texture matches.</param>
        /// <param name="previewHits">Number of preview texture matches.</param>
        public void DrawHitCount(int totalHits, int frozenHits, int previewHits)
        {
            if (string.IsNullOrEmpty(SearchText)) return;

            string hitText = totalHits == 1 ? "1 hit" : $"{totalHits} hits";
            if (frozenHits > 0 || previewHits > 0)
            {
                hitText += $" (Frozen: {frozenHits}, Preview: {previewHits})";
            }
            EditorGUILayout.LabelField(hitText, EditorStylesCache.HitCountStyle);
        }

        /// <summary>
        /// Draws a hidden count indicator.
        /// </summary>
        /// <param name="hiddenCount">Number of hidden items.</param>
        public static void DrawHiddenCount(int hiddenCount)
        {
            string hiddenText = hiddenCount == 1 ? "1 hidden" : $"{hiddenCount} hidden";
            EditorGUILayout.LabelField(hiddenText, EditorStylesCache.HiddenCountStyle);
        }

        /// <summary>
        /// Clears the search and resets the control.
        /// </summary>
        public void Clear()
        {
            SearchText = "";
            UseFuzzySearch = false;
            InvalidateCache();
        }

        /// <summary>
        /// Invalidates the search cache, forcing recalculation on next access.
        /// </summary>
        public void InvalidateCache()
        {
            _cachedMatchIndices = null;
            _cachedSearchText = null;
            _cachedItemCount = 0;
        }

        /// <summary>
        /// Checks if an item at the given index matches the search.
        /// </summary>
        /// <param name="index">Item index.</param>
        /// <param name="itemCount">Total item count.</param>
        /// <param name="matchFunction">Function that returns true if item at index matches search.</param>
        /// <returns>True if item matches or no search is active.</returns>
        public bool IsMatch(int index, int itemCount, Func<int, bool> matchFunction)
        {
            if (!IsSearching) return true;

            UpdateCache(itemCount, matchFunction);
            return _cachedMatchIndices?.Contains(index) ?? false;
        }

        /// <summary>
        /// Counts the number of matching items.
        /// </summary>
        /// <param name="itemCount">Total item count.</param>
        /// <param name="matchFunction">Function that returns true if item at index matches search.</param>
        /// <returns>Number of matching items.</returns>
        public int CountMatches(int itemCount, Func<int, bool> matchFunction)
        {
            if (!IsSearching) return itemCount;

            UpdateCache(itemCount, matchFunction);
            return _cachedMatchIndices?.Count ?? 0;
        }

        /// <summary>
        /// Checks if a string matches the current search.
        /// </summary>
        /// <param name="text">Text to match against.</param>
        /// <returns>True if text matches search.</returns>
        public bool MatchesSearch(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (!IsSearching) return true;

            if (UseFuzzySearch)
            {
                return FuzzyMatcher.Match(text, SearchText);
            }
            else
            {
                return text.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        /// <summary>
        /// Checks if any of the provided strings matches the current search.
        /// </summary>
        /// <param name="texts">Texts to match against.</param>
        /// <returns>True if any text matches search.</returns>
        public bool MatchesSearchAny(params string[] texts)
        {
            if (!IsSearching) return true;

            foreach (var text in texts)
            {
                if (MatchesSearch(text))
                    return true;
            }
            return false;
        }

        private void UpdateCache(int itemCount, Func<int, bool> matchFunction)
        {
            // Check if cache is valid
            if (_cachedSearchText == SearchText &&
                _cachedUseFuzzySearch == UseFuzzySearch &&
                _cachedItemCount == itemCount &&
                _matchFunction == matchFunction &&
                _cachedMatchIndices != null)
            {
                return;
            }

            // Update cache state
            _cachedSearchText = SearchText;
            _cachedUseFuzzySearch = UseFuzzySearch;
            _cachedItemCount = itemCount;
            _matchFunction = matchFunction;

            // Rebuild cache
            _cachedMatchIndices = new HashSet<int>();
            for (int i = 0; i < itemCount; i++)
            {
                if (matchFunction(i))
                {
                    _cachedMatchIndices.Add(i);
                }
            }
        }
    }
}
