using dev.limitex.avatar.compressor.editor.texture.ui;
using dev.limitex.avatar.compressor.editor.ui;
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
        // State-holding components
        private SearchBoxControl _searchBox;
        private PreviewSection _previewSection;

        // UI state
        private bool _showExcludedPathsSection;
        private bool _showFrozenSection = true;
        private Vector2 _frozenScrollPosition;

        private void OnEnable()
        {
            _searchBox = new SearchBoxControl();
            _previewSection = new PreviewSection();
        }

        protected override void DrawInspectorContent()
        {
            var config = (TextureCompressor)target;

            EditorGUILayout.Space(5);

            // Preset section
            PresetSection.Draw(config);

            // Custom preset section (only shown when Custom is selected)
            CustomPresetSection.Draw(config);
            EditorGUILayout.Space(10);

            // Settings section
            SettingsSection.Draw(config, serializedObject, ref _showAdvancedSettings);
            EditorGUILayout.Space(10);

            // Texture filters
            FilterSection.DrawTextureFilters(config);
            EditorGUILayout.Space(10);

            // Enable logging
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("EnableLogging"),
                new GUIContent("Enable Logging")
            );
            EditorGUILayout.Space(10);

            // Excluded paths
            FilterSection.DrawExcludedPaths(config, ref _showExcludedPathsSection);
            EditorGUILayout.Space(15);

            // Search box
            _searchBox.Draw();
            EditorGUILayout.Space(10);

            // Frozen textures
            FrozenTexturesSection.Draw(
                config,
                _searchBox,
                ref _showFrozenSection,
                ref _frozenScrollPosition
            );
            EditorGUILayout.Space(15);

            // Preview section
            _previewSection.Draw(config, _searchBox);
        }
    }
}
