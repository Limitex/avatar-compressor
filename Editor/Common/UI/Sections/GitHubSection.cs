using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Draws the GitHub link section at the bottom of the inspector.
    /// </summary>
    public static class GitHubSection
    {
        private const string GitHubUrl = "https://github.com/Limitex/avatar-compressor";
        private const string LinkText = "Limitex/avatar-compressor";

        /// <summary>
        /// Draws the GitHub section with link.
        /// </summary>
        public static void Draw()
        {
            EditorDrawUtils.DrawSeparator();

            // Gray text (line 1)
            var savedColor = GUI.color;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            EditorGUILayout.LabelField(
                "Bugs? Ideas? Let us know! Stars appreciated.",
                EditorStylesCache.CenteredLabel
            );
            GUI.color = savedColor;

            // Clickable link (line 2)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var linkContent = new GUIContent(LinkText, "Open GitHub repository");
            var linkRect = GUILayoutUtility.GetRect(linkContent, EditorStylesCache.LinkStyle);
            var isHovering = linkRect.Contains(Event.current.mousePosition);

            // Theme-aware link colors
            Color normalColor = EditorGUIUtility.isProSkin
                ? new Color(0.4f, 0.6f, 1.0f)
                : new Color(0.2f, 0.4f, 0.8f);
            Color hoverColor = EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.8f, 1.0f)
                : new Color(0.3f, 0.5f, 0.9f);

            var linkSavedColor = GUI.color;
            GUI.color = isHovering ? hoverColor : normalColor;
            if (GUI.Button(linkRect, linkContent, EditorStylesCache.LinkStyle))
            {
                Application.OpenURL(GitHubUrl);
            }
            GUI.color = linkSavedColor;
            EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
