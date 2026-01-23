using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Scans for CustomCompressorPreset assets with MenuPath and builds hierarchical menus.
    /// </summary>
    public static class CustomPresetScanner
    {
        private static List<CustomCompressorPreset> _cachedPresets;
        private static double _cacheTime;
        private const double CacheValiditySeconds = 2.0;

        /// <summary>
        /// Gets all CustomCompressorPreset assets that have a non-empty MenuPath.
        /// Results are cached for performance.
        /// </summary>
        public static List<CustomCompressorPreset> GetMenuPresets()
        {
            double currentTime = EditorApplication.timeSinceStartup;

            if (_cachedPresets != null && (currentTime - _cacheTime) < CacheValiditySeconds)
            {
                return new List<CustomCompressorPreset>(_cachedPresets);
            }

            var presets = new List<CustomCompressorPreset>();

            string[] guids = AssetDatabase.FindAssets("t:CustomCompressorPreset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<CustomCompressorPreset>(path);

                if (preset != null && !string.IsNullOrEmpty(preset.MenuPath))
                {
                    presets.Add(preset);
                }
            }

            // Sort by MenuPath for consistent ordering
            presets.Sort(
                (a, b) => string.Compare(a.MenuPath, b.MenuPath, StringComparison.Ordinal)
            );

            _cachedPresets = presets;
            _cacheTime = currentTime;

            return new List<CustomCompressorPreset>(presets);
        }

        /// <summary>
        /// Clears the preset cache, forcing a fresh scan on next access.
        /// </summary>
        public static void ClearCache()
        {
            _cachedPresets = null;
        }

        /// <summary>
        /// Builds a GenericMenu for custom preset selection.
        /// </summary>
        /// <param name="onPresetSelected">Callback when a preset is selected from the menu.</param>
        /// <returns>A configured GenericMenu ready to be shown.</returns>
        public static GenericMenu BuildPresetMenu(Action<CustomCompressorPreset> onPresetSelected)
        {
            var menu = new GenericMenu();

            var presets = GetMenuPresets();

            if (presets.Count > 0)
            {
                AddPresetsToMenu(menu, presets, onPresetSelected);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("No presets available"));
            }

            return menu;
        }

        private static void AddPresetsToMenu(
            GenericMenu menu,
            List<CustomCompressorPreset> presets,
            Action<CustomCompressorPreset> onPresetSelected
        )
        {
            foreach (var preset in presets)
            {
                var presetRef = preset; // Capture for closure
                menu.AddItem(
                    new GUIContent(preset.MenuPath),
                    false,
                    () => onPresetSelected?.Invoke(presetRef)
                );
            }
        }
    }
}
