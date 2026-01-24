using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Manages editor-only state for custom preset mode.
    /// State resets on domain reload; objects with presets default to use-only mode.
    /// </summary>
    public static class CustomPresetEditorState
    {
        // Stores edit mode state per TextureCompressor instance ID.
        // When true, the user is manually editing custom settings.
        // When false (or not present), the user is in use-only mode if a preset is assigned.
        // Stores (isEditMode, accessTime) to enable LRU-style eviction of oldest entries.
        private static readonly Dictionary<
            int,
            (bool isEditMode, double accessTime)
        > _editModeStates = new();

        /// <summary>
        /// Maximum number of cached states before LRU eviction occurs.
        /// Exposed for testing purposes.
        /// </summary>
        public const int MaxCachedStates = 64;

        /// <summary>
        /// Gets the current number of cached states. For testing purposes.
        /// </summary>
        public static int CachedStateCount => _editModeStates.Count;

        /// <summary>
        /// Clears all cached states. For testing purposes.
        /// </summary>
        public static void ClearAllStates()
        {
            _editModeStates.Clear();
        }

        /// <summary>
        /// Checks if a state exists for the given config. For testing purposes.
        /// </summary>
        public static bool HasStateFor(TextureCompressor config)
        {
            if (config == null)
                return false;
            return _editModeStates.ContainsKey(config.GetInstanceID());
        }

        private static void EvictOldestEntry()
        {
            if (_editModeStates.Count == 0)
                return;

            var first = _editModeStates.First();
            int oldestKey = first.Key;
            double oldestTime = first.Value.accessTime;

            foreach (var kvp in _editModeStates)
            {
                if (kvp.Value.accessTime < oldestTime)
                {
                    oldestTime = kvp.Value.accessTime;
                    oldestKey = kvp.Key;
                }
            }

            _editModeStates.Remove(oldestKey);
        }

        /// <summary>
        /// Checks if the specified config is in edit mode.
        /// </summary>
        public static bool IsInEditMode(TextureCompressor config)
        {
            if (config == null)
                return false;

            int instanceId = config.GetInstanceID();
            if (_editModeStates.TryGetValue(instanceId, out var state))
            {
                // Update access time on read (LRU)
                _editModeStates[instanceId] = (
                    state.isEditMode,
                    EditorApplication.timeSinceStartup
                );
                return state.isEditMode;
            }

            return false;
        }

        /// <summary>
        /// Sets the edit mode state for the specified config.
        /// </summary>
        public static void SetEditMode(TextureCompressor config, bool isEditMode)
        {
            if (config == null)
                return;

            int instanceId = config.GetInstanceID();
            double currentTime = EditorApplication.timeSinceStartup;

            // Evict oldest entry if cache is full (LRU-style)
            if (
                _editModeStates.Count >= MaxCachedStates
                && !_editModeStates.ContainsKey(instanceId)
            )
            {
                EvictOldestEntry();
            }

            _editModeStates[instanceId] = (isEditMode, currentTime);
        }

        /// <summary>
        /// Checks if the custom preset is in use-only mode (not editing).
        /// Use-only mode is active when a preset is assigned and edit mode is not enabled.
        /// </summary>
        public static bool IsInUseOnlyMode(TextureCompressor config)
        {
            if (config == null)
                return false;

            return config.CustomPresetAsset != null && !IsInEditMode(config);
        }

        /// <summary>
        /// Checks if the config is in Custom preset mode and settings are editable.
        /// Returns true when: Custom preset is selected AND (no preset asset assigned OR in edit mode).
        /// </summary>
        public static bool IsCustomEditable(TextureCompressor config)
        {
            if (config == null || config.Preset != CompressorPreset.Custom)
                return false;

            // Editable if: no preset assigned, OR explicitly in edit mode
            return config.CustomPresetAsset == null || IsInEditMode(config);
        }

        /// <summary>
        /// Switches the config to edit mode.
        /// </summary>
        public static void SwitchToEditMode(TextureCompressor config)
        {
            if (config == null)
                return;

            SetEditMode(config, true);
        }

        /// <summary>
        /// Applies a custom preset and switches to use-only mode.
        /// </summary>
        public static void ApplyPresetAndSwitchToUseOnly(
            TextureCompressor config,
            CustomCompressorPreset preset
        )
        {
            if (config == null || preset == null)
                return;

            config.Preset = CompressorPreset.Custom;
            config.CustomPresetAsset = preset;
            SetEditMode(config, false);
            preset.ApplyTo(config);
        }
    }
}
