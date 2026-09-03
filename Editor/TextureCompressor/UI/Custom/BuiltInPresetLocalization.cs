using System.Collections.Generic;
using dev.limitex.avatar.compressor;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Localizes bundled preset display data without modifying serialized preset assets.
    /// </summary>
    internal static class BuiltInPresetLocalization
    {
        private static readonly IReadOnlyDictionary<string, string> PresetKeys = new Dictionary<
            string,
            string
        >
        {
            ["5239a248c3cecc8438149fd847e07082"] = "high_quality_plus",
            ["1de212fc9c0e5db45889f9b52723b1d9"] = "quality_plus",
            ["dc3ad49e6d7ef4f429bb5967cf64b644"] = "balanced_plus",
            ["738122bf69ebfef46a329e3c46e09e60"] = "aggressive_plus",
            ["7881623902e2305439c564a23f41f40c"] = "maximum_plus",
        };

        internal static bool IsBuiltIn(CustomTextureCompressorPreset preset)
        {
            return TryGetPresetKey(preset, out _);
        }

        internal static string GetDisplayName(CustomTextureCompressorPreset preset)
        {
            return TryGetPresetKey(preset, out var key)
                ? AvatarCompressorLocalization.Tr($"built_in_preset.{key}.name")
                : preset.name;
        }

        internal static string GetDescription(CustomTextureCompressorPreset preset)
        {
            return TryGetPresetKey(preset, out var key)
                ? AvatarCompressorLocalization.Tr($"built_in_preset.{key}.description")
                : preset.Description;
        }

        internal static string GetMenuPath(CustomTextureCompressorPreset preset)
        {
            if (!TryGetPresetKey(preset, out var key))
                return preset.MenuPath;

            return AvatarCompressorLocalization.Tr(
                "preset_menu.built_in_path",
                AvatarCompressorLocalization.Tr($"built_in_preset.{key}.name")
            );
        }

        private static bool TryGetPresetKey(
            CustomTextureCompressorPreset preset,
            out string presetKey
        )
        {
            presetKey = null;
            if (preset == null)
                return false;

            string path = AssetDatabase.GetAssetPath(preset);
            string guid = AssetDatabase.AssetPathToGUID(path);
            return !string.IsNullOrEmpty(guid) && PresetKeys.TryGetValue(guid, out presetKey);
        }
    }
}
