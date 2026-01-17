using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Provides cached GUIStyle instances for consistent UI appearance.
    /// Styles are lazily initialized to avoid issues with Unity's skin not being ready.
    /// Cache is cleared on domain reload to ensure fresh styles after script recompilation.
    /// </summary>
    public static class EditorStylesCache
    {
        private static GUIStyle _centeredLabel;
        private static GUIStyle _linkStyle;
        private static GUIStyle _placeholderStyle;
        private static GUIStyle _hitCountStyle;
        private static GUIStyle _hiddenCountStyle;

        /// <summary>
        /// Clears all cached styles on domain reload to prevent stale references.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCacheOnDomainReload()
        {
            _centeredLabel = null;
            _linkStyle = null;
            _placeholderStyle = null;
            _hitCountStyle = null;
            _hiddenCountStyle = null;
        }

        /// <summary>
        /// Centered mini label style.
        /// </summary>
        public static GUIStyle CenteredLabel =>
            _centeredLabel ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
            };

        /// <summary>
        /// Link style for clickable text (no underline, uses GUI.color for coloring).
        /// </summary>
        public static GUIStyle LinkStyle => _linkStyle ??= new GUIStyle(EditorStyles.miniLabel);

        /// <summary>
        /// Placeholder text style (italic, gray).
        /// </summary>
        public static GUIStyle PlaceholderStyle =>
            _placeholderStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) },
            };

        /// <summary>
        /// Hit count style (right-aligned mini label).
        /// </summary>
        public static GUIStyle HitCountStyle =>
            _hitCountStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
            };

        /// <summary>
        /// Hidden count style (right-aligned, gray mini label).
        /// </summary>
        public static GUIStyle HiddenCountStyle =>
            _hiddenCountStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
            };

        /// <summary>
        /// Creates a button style with optional bold font.
        /// </summary>
        public static GUIStyle CreateButtonStyle(bool isSelected, float height = 40f)
        {
            return new GUIStyle(GUI.skin.button)
            {
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                fixedHeight = height,
            };
        }
    }
}
