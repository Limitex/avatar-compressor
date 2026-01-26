using dev.limitex.avatar.compressor;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Manages editor-only state for custom preset mode.
    /// State resets on domain reload; objects with presets default to use-only mode.
    /// </summary>
    public static class PresetEditorState
    {
        private const int MaxCachedStates = 64;

        // Stores edit mode state per TextureCompressor instance ID.
        // When true, the user is manually editing custom settings.
        // When false (or not present), the user is in use-only mode if a preset is assigned.
        private static readonly LruCache<int, bool> _editModeCache = new(MaxCachedStates);

        /// <summary>
        /// Checks if the specified config is in edit mode.
        /// </summary>
        public static bool IsInEditMode(TextureCompressor config)
        {
            if (config == null)
                return false;

            return _editModeCache.TryGetValue(config.GetInstanceID(), out var isEditMode)
                && isEditMode;
        }

        /// <summary>
        /// Sets the edit mode state for the specified config.
        /// </summary>
        public static void SetEditMode(TextureCompressor config, bool isEditMode)
        {
            if (config == null)
                return;

            _editModeCache.Set(config.GetInstanceID(), isEditMode);
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
        /// Returns true when: Custom preset is selected AND (no preset asset assigned OR in edit mode with no restrictions).
        /// </summary>
        public static bool IsCustomEditable(TextureCompressor config)
        {
            if (config == null || config.Preset != CompressorPreset.Custom)
                return false;

            // Editable if: no preset assigned
            if (config.CustomPresetAsset == null)
                return true;

            // Or in edit mode AND restriction allows direct edit
            if (IsInEditMode(config) && !GetRestriction(config).RequiresUnlink())
                return true;

            return false;
        }

        /// <summary>
        /// Gets the editing restriction for the config's preset.
        /// </summary>
        public static PresetRestriction GetRestriction(TextureCompressor config)
        {
            if (config?.CustomPresetAsset == null)
                return PresetRestriction.None;

            return PresetLocationResolver.GetRestriction(config.CustomPresetAsset);
        }

        /// <summary>
        /// Switches the config to edit mode.
        /// Does nothing if the preset requires unlinking (caller should check GetRestriction first).
        /// </summary>
        public static void SwitchToEditMode(TextureCompressor config)
        {
            if (config == null)
                return;

            if (GetRestriction(config).RequiresUnlink())
                return;

            SetEditMode(config, true);
        }

        /// <summary>
        /// Unlinks the preset and switches to edit mode.
        /// Current settings are preserved.
        /// </summary>
        public static void UnlinkPresetAndSwitchToEditMode(TextureCompressor config)
        {
            if (config == null)
                return;

            config.CustomPresetAsset = null;
            SetEditMode(config, true);
        }

        /// <summary>
        /// Applies a custom preset and switches to use-only mode.
        /// </summary>
        public static void ApplyPresetAndSwitchToUseOnly(
            TextureCompressor config,
            CustomTextureCompressorPreset preset
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
