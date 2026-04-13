using System;
using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Reusable search control with fuzzy search support.
    /// Holds search state, draws the UI via Unity's <see cref="SearchField"/>, and provides matching logic.
    /// </summary>
    public class SearchBoxControl
    {
        /// <summary>
        /// Current search text.
        /// </summary>
        public string SearchText { get; private set; }

        /// <summary>
        /// Whether fuzzy search is enabled.
        /// </summary>
        public bool UseFuzzySearch { get; private set; }

        /// <summary>
        /// Returns true if search is active (non-empty search text).
        /// </summary>
        public bool IsSearching => !string.IsNullOrEmpty(SearchText);

        private readonly SearchField _searchField = new();

        // Count cache — avoids recomputing every IMGUI frame
        private int _cachedCount;
        private string _cachedCountSearchText;
        private bool _cachedCountUseFuzzy;
        private object _cachedSourceRef;
        private int _cachedSourceCount;

        /// <summary>
        /// Creates a new SearchBoxControl with optional initial state.
        /// </summary>
        /// <param name="searchText">Initial search text.</param>
        /// <param name="useFuzzySearch">Initial fuzzy search state.</param>
        public SearchBoxControl(string searchText = "", bool useFuzzySearch = false)
        {
            SearchText = searchText;
            UseFuzzySearch = useFuzzySearch;
        }

        /// <summary>
        /// Draws the search box UI and updates internal state.
        /// </summary>
        /// <param name="matchedCount">Number of matched items (-1 to hide the summary).</param>
        /// <param name="totalCount">Total number of items (-1 to hide the summary).</param>
        /// <returns>True if search parameters changed.</returns>
        public bool Draw(int matchedCount = -1, int totalCount = -1)
        {
            bool changed = false;

            var newText = _searchField.OnGUI(SearchText);
            if (newText != SearchText)
            {
                SearchText = newText;
                changed = true;
            }

            if (IsSearching)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                var fuzzy = EditorGUILayout.ToggleLeft(
                    "Fuzzy",
                    UseFuzzySearch,
                    GUILayout.Width(55)
                );
                if (EditorGUI.EndChangeCheck())
                {
                    UseFuzzySearch = fuzzy;
                    changed = true;
                }

                GUILayout.FlexibleSpace();

                if (totalCount >= 0)
                {
                    GUILayout.Label(
                        $"Showing {matchedCount} of {totalCount}",
                        EditorStyles.miniLabel
                    );
                }

                EditorGUILayout.EndHorizontal();
            }

            return changed;
        }

        /// <summary>
        /// Returns the number of items matching the current search, with caching.
        /// When not searching, returns <c>items.Count</c> without invoking the predicate.
        /// The cache is keyed on (SearchText, UseFuzzySearch, collection reference, items.Count)
        /// and is automatically invalidated when any of these change.
        /// </summary>
        /// <param name="items">Source collection.</param>
        /// <param name="predicate">Per-item match function (only called on cache miss).</param>
        public int CountMatches<T>(IReadOnlyCollection<T> items, Func<T, bool> predicate)
        {
            if (!IsSearching)
                return items.Count;

            if (
                SearchText == _cachedCountSearchText
                && UseFuzzySearch == _cachedCountUseFuzzy
                && ReferenceEquals(items, _cachedSourceRef)
                && items.Count == _cachedSourceCount
            )
            {
                return _cachedCount;
            }

            int count = 0;
            foreach (var item in items)
            {
                if (predicate(item))
                    count++;
            }

            _cachedCount = count;
            _cachedCountSearchText = SearchText;
            _cachedCountUseFuzzy = UseFuzzySearch;
            _cachedSourceRef = items;
            _cachedSourceCount = items.Count;
            return count;
        }

        /// <summary>
        /// Invalidates the count cache, forcing <see cref="CountMatches{T}"/> to recount
        /// on its next call. Use when the source data changes without a count change
        /// (e.g. preview regeneration).
        /// </summary>
        public void InvalidateCountCache()
        {
            _cachedCountSearchText = null;
            _cachedSourceRef = null;
        }

        /// <summary>
        /// Clears the search and resets the control.
        /// </summary>
        public void Clear()
        {
            SearchText = "";
            UseFuzzySearch = false;
            InvalidateCountCache();
        }

        /// <summary>
        /// Checks if a string matches the current search.
        /// When not searching, always returns true (all items visible).
        /// When searching, returns false for null/empty text (no content to match against).
        /// </summary>
        /// <param name="text">Text to match against.</param>
        /// <returns>True if text matches search, or true if not searching.</returns>
        public bool MatchesSearch(string text)
        {
            if (!IsSearching)
                return true;
            return MatchesCore(text);
        }

        /// <summary>
        /// Checks if either of the provided strings matches the current search.
        /// </summary>
        public bool MatchesSearchAny(string text1, string text2)
        {
            if (!IsSearching)
                return true;

            return MatchesCore(text1) || MatchesCore(text2);
        }

        /// <summary>
        /// Checks if any of the three provided strings matches the current search.
        /// </summary>
        public bool MatchesSearchAny(string text1, string text2, string text3)
        {
            if (!IsSearching)
                return true;

            return MatchesCore(text1) || MatchesCore(text2) || MatchesCore(text3);
        }

        /// <summary>
        /// Core matching logic. Assumes IsSearching is true.
        /// </summary>
        private bool MatchesCore(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            if (UseFuzzySearch)
            {
                return FuzzyMatcher.Match(text, SearchText);
            }
            else
            {
                return text.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
    }
}
