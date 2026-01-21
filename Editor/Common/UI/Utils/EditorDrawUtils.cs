using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    /// <summary>
    /// Provides common drawing utilities for editor UI.
    /// </summary>
    public static class EditorDrawUtils
    {
        /// <summary>
        /// Draws a section header with bold label.
        /// </summary>
        public static void DrawSectionHeader(string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        /// <summary>
        /// Draws a progress bar with custom color.
        /// </summary>
        public static void DrawProgressBar(float value, float width, float height, Color fillColor)
        {
            Rect progressRect = EditorGUILayout.GetControlRect(
                GUILayout.Width(width),
                GUILayout.Height(height)
            );
            EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.2f, 0.2f));
            Rect filledRect = new Rect(
                progressRect.x,
                progressRect.y,
                progressRect.width * value,
                progressRect.height
            );
            EditorGUI.DrawRect(filledRect, fillColor);
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
        /// Draws a colored button with selection highlight.
        /// </summary>
        /// <param name="label">Button label text.</param>
        /// <param name="tooltip">Tooltip text.</param>
        /// <param name="color">Background color when selected.</param>
        /// <param name="isSelected">Whether the button is currently selected.</param>
        /// <param name="height">Button height.</param>
        /// <param name="width">Button width. If 0 or negative, uses ExpandWidth.</param>
        /// <returns>True if clicked.</returns>
        public static bool DrawColoredButton(
            string label,
            string tooltip,
            Color color,
            bool isSelected,
            float height = 40f,
            float width = 0f
        )
        {
            var style = EditorStylesCache.CreateButtonStyle(isSelected, height);

            Color originalBg = GUI.backgroundColor;
            if (isSelected)
            {
                GUI.backgroundColor = color;
            }

            GUILayoutOption widthOption =
                width > 0f ? GUILayout.Width(width) : GUILayout.ExpandWidth(true);
            bool clicked = GUILayout.Button(new GUIContent(label, tooltip), style, widthOption);

            GUI.backgroundColor = originalBg;

            return clicked;
        }
    }
}
