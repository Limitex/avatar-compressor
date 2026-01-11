using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Draws the footer section with GitHub link.
    /// </summary>
    public class FooterDrawer
    {
        private static GUIStyle _centeredMiniLabelStyle;
        private static GUIStyle CenteredMiniLabelStyle => _centeredMiniLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        private static GUIStyle _linkStyle;
        private static GUIStyle LinkStyle => _linkStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.3f, 0.5f, 0.9f) },
            hover = { textColor = new Color(0.4f, 0.6f, 1.0f) }
        };

        public void Draw()
        {
            GUIDrawing.DrawSeparator();

            var savedColor = GUI.color;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            EditorGUILayout.LabelField("Bugs? Ideas? Let us know! Stars appreciated.", CenteredMiniLabelStyle);
            GUI.color = savedColor;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var linkContent = new GUIContent("Limitex/avatar-compressor", "Open GitHub repository");
            if (GUILayout.Button(linkContent, LinkStyle))
            {
                Application.OpenURL("https://github.com/Limitex/avatar-compressor");
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
