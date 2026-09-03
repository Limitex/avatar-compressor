using dev.limitex.avatar.compressor;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws settings summary for TextureCompressor.
    /// Extracted to avoid circular dependency between PresetSection and CustomSection.
    /// </summary>
    internal static class SettingsSummaryDrawer
    {
        /// <summary>
        /// Draws a compact summary of the current compression settings.
        /// </summary>
        /// <param name="config">The compressor configuration.</param>
        /// <param name="title">Optional title for the summary section.</param>
        public static void Draw(TextureCompressor config, string title = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                title ?? AvatarCompressorLocalization.Tr("summary.current_settings"),
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr(
                    "summary.strategy",
                    AvatarCompressorLocalization.EnumValue(config.Strategy)
                )
            );
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr(
                    "summary.divisor_range",
                    config.MinDivisor,
                    config.MaxDivisor
                )
            );
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr(
                    "summary.resolution_range",
                    config.MinResolution,
                    config.MaxResolution
                )
            );
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr(
                    "summary.complexity_thresholds",
                    config.LowComplexityThreshold,
                    config.HighComplexityThreshold
                )
            );

            EditorGUILayout.EndVertical();
        }
    }
}
