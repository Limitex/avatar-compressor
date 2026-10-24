using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.ui
{
    internal static class LocalizedEnumPopup
    {
        public static T Draw<T>(GUIContent label, T value, Func<T, string> displayName)
            where T : struct, Enum
        {
            return Draw(EditorGUILayout.GetControlRect(), label, value, displayName);
        }

        public static void DrawProperty<T>(
            SerializedProperty property,
            GUIContent label,
            Func<T, string> displayName,
            Func<Rect, GUIContent, int, GUIContent[], int> popup = null
        )
            where T : struct, Enum
        {
            var position = EditorGUILayout.GetControlRect();
            label = EditorGUI.BeginProperty(position, label, property);

            bool previousShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            var currentValue = (T)Enum.ToObject(typeof(T), property.intValue);
            var newValue = Draw(position, label, currentValue, displayName, popup);
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = Convert.ToInt32(newValue);
            }

            EditorGUI.showMixedValue = previousShowMixedValue;
            EditorGUI.EndProperty();
        }

        private static T Draw<T>(
            Rect position,
            GUIContent label,
            T value,
            Func<T, string> displayName,
            Func<Rect, GUIContent, int, GUIContent[], int> popup = null
        )
            where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            var displayNames = values.Select(entry => new GUIContent(displayName(entry))).ToArray();
            int currentIndex = Array.IndexOf(values, value);
            int newIndex =
                popup != null
                    ? popup(position, label, currentIndex, displayNames)
                    : EditorGUI.Popup(position, label, currentIndex, displayNames);
            return newIndex >= 0 && newIndex < values.Length ? values[newIndex] : value;
        }
    }
}
