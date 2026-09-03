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
            "Packages/dev.limitex.avatar-compressor/Editor/Localization/";

        internal static readonly string[] SupportedLanguages =
        {
            "en-US",
            "zh-Hans",
            "ja-JP",
            "ko-KR",
        };

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
            return Tr($"enum.{typeof(T).Name}.{value}");
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

        internal static void DrawLanguagePicker()
        {
            // Accessing Localizer first ensures all four LAC locales are registered globally.
            _ = Localizer;
            LanguageSwitcher.DrawImmediate();
        }

        internal static List<LocalizationAsset> LoadLocalizationAssets()
        {
            return SupportedLanguages
                .Select(language =>
                    AssetDatabase.LoadAssetAtPath<LocalizationAsset>(
                        LocalizationFolder + language + ".po"
                    )
                )
                .Where(asset => asset != null)
                .ToList();
        }
    }
}
