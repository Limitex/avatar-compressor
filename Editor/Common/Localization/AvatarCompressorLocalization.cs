using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using nadena.dev.ndmf.localization;
using nadena.dev.ndmf.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor
{
    /// <summary>
    /// Shared access to the NDMF localization system for Avatar Compressor editor UI.
    /// </summary>
    internal static class AvatarCompressorLocalization
    {
        private const string LocalizationFolder =
            "Packages/dev.limitex.avatar-compressor/Editor/Common/Localization";

        internal static readonly Localizer Localizer = new Localizer(
            "en-US",
            LoadLocalizationAssets
        );

        internal static string Tr(string key)
        {
            return Localizer.GetLocalizedString(key);
        }

        internal static string Tr(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, Tr(key), args);
        }

        internal static GUIContent Content(string labelKey, string tooltipKey = null)
        {
            return new GUIContent(Tr(labelKey), tooltipKey == null ? null : Tr(tooltipKey));
        }

        internal static string EnumValue<T>(T value)
            where T : struct, Enum
        {
            string name = (typeof(T).Name + value).Replace("_", string.Empty);
            string keyName = char.ToLowerInvariant(name[0]) + name.Substring(1);
            return Tr($"TextureCompressor:enum:{keyName}");
        }

        internal static T EnumPopup<T>(GUIContent label, T value)
            where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            var displayNames = values.Select(EnumValue).ToArray();
            int currentIndex = Array.IndexOf(values, value);
            int newIndex = EditorGUILayout.Popup(label, currentIndex, displayNames);
            return newIndex >= 0 && newIndex < values.Length ? values[newIndex] : value;
        }

        internal static void DrawEnumProperty<T>(SerializedProperty property, GUIContent label)
            where T : struct, Enum
        {
            var position = EditorGUILayout.GetControlRect();
            label = EditorGUI.BeginProperty(position, label, property);

            bool previousShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            var currentValue = (T)Enum.ToObject(typeof(T), property.intValue);
            var newValue = EnumPopup(position, label, currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = Convert.ToInt32(newValue);
            }

            EditorGUI.showMixedValue = previousShowMixedValue;
            EditorGUI.EndProperty();
        }

        private static T EnumPopup<T>(Rect position, GUIContent label, T value)
            where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            var displayNames = values.Select(value => new GUIContent(EnumValue(value))).ToArray();
            int currentIndex = Array.IndexOf(values, value);
            int newIndex = EditorGUI.Popup(position, label, currentIndex, displayNames);
            return newIndex >= 0 && newIndex < values.Length ? values[newIndex] : value;
        }

        internal static void DrawLanguagePicker()
        {
            // Accessing Localizer first ensures all LAC locales are registered globally.
            _ = Localizer;
            LanguageSwitcher.DrawImmediate();
        }

        internal static List<LocalizationAsset> LoadLocalizationAssets()
        {
            return AssetDatabase
                .FindAssets("t:LocalizationAsset", new[] { LocalizationFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<LocalizationAsset>)
                .Where(asset => asset != null)
                .ToList();
        }
    }
}
