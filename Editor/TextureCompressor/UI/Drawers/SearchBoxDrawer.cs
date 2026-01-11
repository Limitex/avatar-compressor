using dev.limitex.avatar.compressor.common;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Draws the search box for filtering textures.
    /// </summary>
    public class SearchBoxDrawer
    {
        private string _searchText = "";
        private bool _useFuzzySearch = true;

        private static GUIStyle _placeholderStyle;
        private static GUIStyle PlaceholderStyle => _placeholderStyle ??= new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Italic,
            normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
        };

        private static GUIStyle _hitCountStyle;
        private static GUIStyle HitCountStyle => _hitCountStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight
        };

        public string SearchText => _searchText;
        public bool UseFuzzySearch => _useFuzzySearch;

        public void Draw(int frozenHits, int previewHits, System.Action onRepaint)
        {
            int totalHits = frozenHits + previewHits;

            GUIDrawing.DrawSectionHeader("Texture Search");

            GUIDrawing.BeginBox();

            EditorGUILayout.BeginHorizontal();

            var searchIcon = EditorGUIUtility.IconContent("d_Search Icon");
            if (searchIcon != null && searchIcon.image != null)
            {
                GUILayout.Label(searchIcon, GUILayout.Width(20), GUILayout.Height(18));
            }

            var textFieldRect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginChangeCheck();
            _searchText = EditorGUI.TextField(textFieldRect, _searchText);
            if (EditorGUI.EndChangeCheck())
            {
                onRepaint();
            }

            if (string.IsNullOrEmpty(_searchText) && GUI.GetNameOfFocusedControl() != "SearchField")
            {
                var placeholderRect = textFieldRect;
                placeholderRect.x += 3;
                EditorGUI.LabelField(placeholderRect, "Search textures...", PlaceholderStyle);
            }

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_searchText));
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _searchText = "";
                GUI.FocusControl(null);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_searchText))
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Space(30);

                EditorGUI.BeginChangeCheck();
                _useFuzzySearch = EditorGUILayout.ToggleLeft("Fuzzy", _useFuzzySearch, GUILayout.Width(55));
                if (EditorGUI.EndChangeCheck())
                {
                    onRepaint();
                }

                string hitText = totalHits == 1 ? "1 hit" : $"{totalHits} hits";
                if (frozenHits > 0 || previewHits > 0)
                {
                    hitText += $" (Frozen: {frozenHits}, Preview: {previewHits})";
                }
                EditorGUILayout.LabelField(hitText, HitCountStyle);

                EditorGUILayout.EndHorizontal();
            }

            GUIDrawing.EndBox();
        }

        public bool MatchesFrozenSearch(FrozenTextureSettings frozen)
        {
            if (string.IsNullOrEmpty(_searchText))
                return true;

            string assetPath = AssetDatabase.GUIDToAssetPath(frozen.TextureGuid);
            string textureName = System.IO.Path.GetFileName(assetPath);

            return MatchesSearch(textureName) || MatchesSearch(assetPath);
        }

        public bool MatchesPreviewSearch(TexturePreviewData data)
        {
            if (string.IsNullOrEmpty(_searchText))
                return true;

            string assetPath = AssetDatabase.GUIDToAssetPath(data.Guid);
            string textureName = data.Texture != null ? data.Texture.name : "";

            return MatchesSearch(textureName) || MatchesSearch(assetPath) || MatchesSearch(data.TextureType);
        }

        private bool MatchesSearch(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            if (_useFuzzySearch)
            {
                return FuzzyMatcher.Match(text, _searchText);
            }
            else
            {
                return text.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        public int CountFrozenMatches(TextureCompressor compressor)
        {
            if (string.IsNullOrEmpty(_searchText))
                return compressor.FrozenTextures.Count;

            int count = 0;
            foreach (var frozen in compressor.FrozenTextures)
            {
                if (MatchesFrozenSearch(frozen))
                    count++;
            }
            return count;
        }

        public int CountPreviewMatches(TexturePreviewData[] previewData)
        {
            if (previewData == null || previewData.Length == 0)
                return 0;

            if (string.IsNullOrEmpty(_searchText))
                return previewData.Length;

            int count = 0;
            foreach (var data in previewData)
            {
                if (MatchesPreviewSearch(data))
                    count++;
            }
            return count;
        }
    }
}
