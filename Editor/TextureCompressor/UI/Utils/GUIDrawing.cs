using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Static methods for Editor GUI drawing.
    /// </summary>
    public static class GUIDrawing
    {
        // Cached GUIStyles
        private static GUIStyle _hiddenCountStyle;
        public static GUIStyle HiddenCountStyle => _hiddenCountStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };

        private static GUIStyle _thumbnailButtonStyle;
        public static GUIStyle ThumbnailButtonStyle => _thumbnailButtonStyle ??= new GUIStyle(GUI.skin.label)
        {
            padding = new RectOffset(0, 0, 0, 0)
        };

        private const float ButtonHeight = 40f;
        private static GUIStyle _coloredButtonStyle;
        private static GUIStyle _coloredButtonSelectedStyle;

        /// <summary>
        /// Draws a colored button.
        /// </summary>
        public static bool DrawColoredButton(string label, string tooltip, Color color, bool isSelected)
        {
            _coloredButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Normal,
                fixedHeight = ButtonHeight
            };
            _coloredButtonSelectedStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = ButtonHeight
            };

            var style = isSelected ? _coloredButtonSelectedStyle : _coloredButtonStyle;

            Color originalBg = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = color;
            }

            bool clicked = GUILayout.Button(new GUIContent(label, tooltip), style, GUILayout.ExpandWidth(true));

            GUI.backgroundColor = originalBg;

            return clicked;
        }

        /// <summary>
        /// Draws a progress bar with custom color.
        /// </summary>
        public static void DrawProgressBar(float value, float width, float height, Color fillColor)
        {
            Rect progressRect = EditorGUILayout.GetControlRect(GUILayout.Width(width), GUILayout.Height(height));
            EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.2f, 0.2f));
            Rect filledRect = new Rect(progressRect.x, progressRect.y, progressRect.width * value, progressRect.height);
            EditorGUI.DrawRect(filledRect, fillColor);
        }

        /// <summary>
        /// Draws a help box with message type.
        /// </summary>
        public static void DrawHelpBox(string message, MessageType type)
        {
            EditorGUILayout.HelpBox(message, type);
        }

        /// <summary>
        /// Draws a section header.
        /// </summary>
        public static void DrawSectionHeader(string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        /// <summary>
        /// Draws a horizontal line separator.
        /// </summary>
        public static void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// Begins a boxed section.
        /// </summary>
        public static void BeginBox()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        }

        /// <summary>
        /// Ends a boxed section.
        /// </summary>
        public static void EndBox()
        {
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Formats bytes to human-readable string.
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / 1024f / 1024f:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024f:F2} KB";
            return $"{bytes} B";
        }

        /// <summary>
        /// Draws a clickable texture thumbnail that highlights the asset in Project window when clicked.
        /// </summary>
        public static void DrawClickableThumbnail(Texture2D texture, float size = 40f)
        {
            var preview = texture != null ? AssetPreview.GetAssetPreview(texture) : null;
            var tooltip = texture != null ? "Click to highlight in Project" : null;
            var thumbnailContent = new GUIContent(preview ?? Texture2D.whiteTexture, tooltip);
            if (GUILayout.Button(thumbnailContent, ThumbnailButtonStyle, GUILayout.Width(size), GUILayout.Height(size)))
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
        }
    }
}
