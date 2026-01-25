using dev.limitex.avatar.compressor;
using UnityEditor;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws settings summary for TextureCompressor.
    /// Extracted to avoid circular dependency between PresetSection and CustomSection.
    /// </summary>
    public static class SettingsSummaryDrawer
    {
        /// <summary>
        /// Draws a compact summary of the current compression settings.
        /// </summary>
        /// <param name="config">The compressor configuration.</param>
        /// <param name="title">Optional title for the summary section.</param>
        public static void Draw(TextureCompressor config, string title = "Current Settings Summary")
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"Strategy: {config.Strategy}");
            EditorGUILayout.LabelField(
                $"Divisor Range: {config.MinDivisor}x - {config.MaxDivisor}x"
            );
            EditorGUILayout.LabelField(
                $"Resolution Range: {config.MinResolution}px - {config.MaxResolution}px"
            );
            EditorGUILayout.LabelField(
                $"Complexity Thresholds: {config.LowComplexityThreshold:P0} - {config.HighComplexityThreshold:P0}"
            );

            EditorGUILayout.EndVertical();
        }
    }
}
