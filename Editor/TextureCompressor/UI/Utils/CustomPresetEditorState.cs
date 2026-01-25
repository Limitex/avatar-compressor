using dev.limitex.avatar.compressor;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Information about editing restrictions for a preset.
    /// </summary>
    public readonly struct EditRestrictionInfo
    {
        public bool IsLocked { get; }
        public bool IsBuiltIn { get; }
        public bool IsInPackage { get; }

        /// <summary>
        /// Returns true if the preset can be edited directly without unlinking.
        /// </summary>
        public bool CanDirectEdit => !IsLocked && !IsBuiltIn && !IsInPackage;

        /// <summary>
        /// Returns true if the preset requires unlinking to edit.
        /// </summary>
        public bool RequiresUnlink => IsLocked || IsBuiltIn || IsInPackage;

        public EditRestrictionInfo(bool isLocked, bool isBuiltIn, bool isInPackage)
        {
            IsLocked = isLocked;
            IsBuiltIn = isBuiltIn;
            IsInPackage = isInPackage;
        }
    }

    /// <summary>
    /// Manages editor-only state for custom preset mode.
    /// State resets on domain reload; objects with presets default to use-only mode.
    /// </summary>
    public static class CustomPresetEditorState
    {
        /// <summary>
        /// Maximum number of cached states before LRU eviction occurs.
        /// Exposed for testing purposes.
        /// </summary>
        public const int MaxCachedStates = 64;

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
        /// Gets the editing restriction information for the config's preset.
        /// </summary>
        public static EditRestrictionInfo GetEditRestriction(TextureCompressor config)
        {
            if (config?.CustomPresetAsset == null)
                return new EditRestrictionInfo(false, false, false);

            var path = AssetDatabase.GetAssetPath(config.CustomPresetAsset);
            bool isLocked = config.CustomPresetAsset.Lock;
            bool isBuiltIn = IsBuiltInPreset(path);
            // IsBuiltIn takes priority: if it's in this package, don't mark as generic "package"
            bool isInPackage = !isBuiltIn && IsInPackage(path);

            return new EditRestrictionInfo(isLocked, isBuiltIn, isInPackage);
        }

        private static UnityEditor.PackageManager.PackageInfo _packageInfo;

        /// <summary>
        /// Checks if the preset is a built-in preset (located within this package).
        /// Returns false if running outside of a package context (e.g., during development).
        /// </summary>
        private static bool IsBuiltInPreset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            _packageInfo ??= UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(CustomPresetEditorState).Assembly
            );

            // When not installed as a package, all presets are considered user presets
            if (_packageInfo == null)
                return false;

            return assetPath.StartsWith(_packageInfo.assetPath);
        }

        /// <summary>
        /// Checks if the preset is located within any package (Packages/ folder).
        /// </summary>
        private static bool IsInPackage(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            return assetPath.StartsWith("Packages/");
        }

        /// <summary>
        /// Switches the config to edit mode.
        /// Does nothing if the preset requires unlinking (caller should check GetEditRestriction first).
        /// </summary>
        public static void SwitchToEditMode(TextureCompressor config)
        {
            if (config == null)
                return;

            if (GetEditRestriction(config).RequiresUnlink)
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
