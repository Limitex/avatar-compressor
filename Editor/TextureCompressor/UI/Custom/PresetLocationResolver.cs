using dev.limitex.avatar.compressor;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Resolves preset locations and determines their editing restrictions.
    /// </summary>
    public static class PresetLocationResolver
    {
        private static UnityEditor.PackageManager.PackageInfo _packageInfo;

        /// <summary>
        /// Gets the editing restriction for a preset based on its location and lock status.
        /// </summary>
        /// <param name="preset">The preset to check.</param>
        /// <returns>The restriction level for the preset.</returns>
        public static PresetRestriction GetRestriction(CustomTextureCompressorPreset preset)
        {
            if (preset == null)
                return PresetRestriction.None;

            var path = AssetDatabase.GetAssetPath(preset);

            if (IsBuiltInPreset(path))
                return PresetRestriction.BuiltIn;

            if (IsInPackage(path))
                return PresetRestriction.ExternalPackage;

            if (preset.Lock)
                return PresetRestriction.Locked;

            return PresetRestriction.None;
        }

        /// <summary>
        /// Checks if the preset is a built-in preset (located within this package).
        /// Returns false if running outside of a package context (e.g., during development).
        /// </summary>
        private static bool IsBuiltInPreset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            _packageInfo ??= UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(PresetLocationResolver).Assembly
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
    }
}
