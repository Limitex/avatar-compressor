using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Provides clickable thumbnail drawing for textures.
    /// </summary>
    public static class ThumbnailControl
    {
        /// <summary>
        /// Draws a clickable thumbnail that pings the asset in Project window when clicked.
        /// </summary>
        /// <param name="texture">The texture to display (can be null).</param>
        /// <param name="size">The size of the thumbnail.</param>
        public static void DrawClickable(Texture2D texture, float size = 40f)
        {
            // Temporarily enable GUI to allow clicking even when inside a disabled group
            var wasEnabled = GUI.enabled;
            GUI.enabled = true;

            var preview = texture != null ? AssetPreview.GetAssetPreview(texture) : null;
            var thumbnailContent = new GUIContent(
                preview ?? Texture2D.whiteTexture,
                "Click to highlight in Project"
            );
            var thumbnailStyle = new GUIStyle(GUI.skin.label)
            {
                padding = new RectOffset(0, 0, 0, 0),
            };

            if (
                GUILayout.Button(
                    thumbnailContent,
                    thumbnailStyle,
                    GUILayout.Width(size),
                    GUILayout.Height(size)
                )
            )
            {
                if (texture != null)
                {
                    EditorGUIUtility.PingObject(texture);
                }
            }

            if (texture != null)
            {
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }

            GUI.enabled = wasEnabled;
        }
    }
}
