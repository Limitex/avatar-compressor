using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Draws the GitHub link section at the bottom of the inspector.
    /// </summary>
    internal static class GitHubSection
    {
        internal const string Repository = "Limitex/avatar-compressor";
        internal const string GitHubUrl = "https://github.com/" + Repository;

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
                AvatarCompressorLocalization.Tr("Common:message:githubFeedback"),
                EditorStylesCache.CenteredLabel
            );
            GUI.color = savedColor;

            // Clickable link (line 2)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var linkContent = new GUIContent(
                Repository,
                AvatarCompressorLocalization.Tr("Common:button:openGitHub:tooltip")
            );
            var linkRect = GUILayoutUtility.GetRect(linkContent, EditorStylesCache.LinkStyle);
            if (EditorDrawUtils.DrawLink(linkRect, linkContent))
            {
                Application.OpenURL(GitHubUrl);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
