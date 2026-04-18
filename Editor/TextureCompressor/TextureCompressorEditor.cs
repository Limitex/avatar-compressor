using dev.limitex.avatar.compressor.editor.texture.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Custom editor for the TextureCompressor component.
    /// </summary>
    [CustomEditor(typeof(TextureCompressor))]
    public class TextureCompressorEditor : CompressorEditorBase
    {
        // Section components (each owns its own search and UI state)
        private FrozenTexturesSection _frozenSection;
        private PreviewSection _previewSection;

        // UI state for sections that don't own their own
        private bool _showExclusionsSection;
        private bool _showTextureFiltersSection;

        private void OnEnable()
        {
            _frozenSection = new FrozenTexturesSection();
            _previewSection = new PreviewSection();
        }

        protected override void DrawInspectorContent()
        {
            var config = (TextureCompressor)target;

            EditorGUILayout.Space(5);

            // Preset section
            PresetSection.Draw(config);
            EditorGUILayout.Space(10);

            // Settings section
            SettingsSection.Draw(config, serializedObject, ref _showAdvancedSettings);
            EditorGUILayout.Space(10);

            // Texture filters
            FilterSection.DrawTextureFilters(config, ref _showTextureFiltersSection);
            EditorGUILayout.Space(10);

            // Exclusions (textures + paths)
            FilterSection.DrawExclusions(config, ref _showExclusionsSection);
            EditorGUILayout.Space(15);

            // Frozen textures
            _frozenSection.Draw(config);
            EditorGUILayout.Space(15);

            // Preview section
            _previewSection.Draw(config);
        }
    }
}
