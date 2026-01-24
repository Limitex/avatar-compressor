using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Scans for CustomTextureCompressorPreset assets with MenuPath and builds hierarchical menus.
    /// </summary>
    public static class CustomPresetScanner
    {
        private static List<CustomTextureCompressorPreset> _cachedPresets;
        private static double _cacheTime;
        private const double CacheValiditySeconds = 2.0;

        /// <summary>
        /// Gets all CustomTextureCompressorPreset assets that have a non-empty MenuPath.
        /// Results are cached for performance.
        /// </summary>
        public static List<CustomTextureCompressorPreset> GetMenuPresets()
        {
            double currentTime = EditorApplication.timeSinceStartup;

            if (_cachedPresets != null && (currentTime - _cacheTime) < CacheValiditySeconds)
            {
                return new List<CustomTextureCompressorPreset>(_cachedPresets);
            }

            var presets = new List<CustomTextureCompressorPreset>();

            string[] guids = AssetDatabase.FindAssets("t:CustomTextureCompressorPreset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<CustomTextureCompressorPreset>(path);

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

            return new List<CustomTextureCompressorPreset>(presets);
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
        /// <param name="currentPreset">The currently selected preset (for checkmark display).</param>
        /// <param name="onPresetSelected">Callback when a preset is selected from the menu.</param>
        /// <returns>A configured GenericMenu ready to be shown.</returns>
        public static GenericMenu BuildPresetMenu(
            CustomTextureCompressorPreset currentPreset,
            Action<CustomTextureCompressorPreset> onPresetSelected
        )
        {
            var menu = new GenericMenu();

            var presets = GetMenuPresets();

            if (presets.Count > 0)
            {
                AddPresetsToMenu(menu, presets, currentPreset, onPresetSelected);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("No presets available"));
            }

            return menu;
        }

        private static void AddPresetsToMenu(
            GenericMenu menu,
            List<CustomTextureCompressorPreset> presets,
            CustomTextureCompressorPreset currentPreset,
            Action<CustomTextureCompressorPreset> onPresetSelected
        )
        {
            foreach (var preset in presets)
            {
                var presetRef = preset; // Capture for closure
                bool isSelected = currentPreset != null && currentPreset == preset;
                menu.AddItem(
                    new GUIContent(preset.MenuPath),
                    isSelected,
                    () => onPresetSelected?.Invoke(presetRef)
                );
            }
        }
    }
}
