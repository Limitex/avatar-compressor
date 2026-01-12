using dev.limitex.avatar.compressor.editor;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Custom editor for TextureCompressor component.
    /// </summary>
    [CustomEditor(typeof(TextureCompressor))]
    public class TextureCompressorEditor : CompressorEditorBase
    {
        // Serialized properties
        private SerializedProperty _enableLogging;

        // UI state
        private bool _showAdvancedSettings;
        private CompressorPropertySet _propertySet;

        // Preview state
        private bool _showPreview;
        private TexturePreviewData[] _previewData;
        private int _processedCount;
        private int _skippedCount;
        private int _frozenCount;
        private int _previewSettingsHash;

        // Excluded paths section state
        private bool _showExcludedPathsSection = false;

        // Drawer instances
        private PresetSectionDrawer _presetDrawer;
        private FrozenTexturesDrawer _frozenDrawer;
        private PreviewSectionDrawer _previewDrawer;
        private SearchBoxDrawer _searchDrawer;
        private FooterDrawer _footerDrawer;

        private void OnEnable()
        {
            _enableLogging = serializedObject.FindProperty("EnableLogging");
            _propertySet = new CompressorPropertySet(serializedObject);

            // Initialize drawers
            _presetDrawer = new PresetSectionDrawer();
            _frozenDrawer = new FrozenTexturesDrawer();
            _previewDrawer = new PreviewSectionDrawer();
            _searchDrawer = new SearchBoxDrawer();
            _footerDrawer = new FooterDrawer();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var compressor = (TextureCompressor)target;

            // Avatar root warning
            DrawAvatarRootWarning(compressor.transform);

            EditorGUILayout.Space(5);

            // Preset section
            _presetDrawer.DrawPresetSection(compressor);
            EditorGUILayout.Space(10);
            _presetDrawer.DrawPresetDescription(compressor.Preset);
            EditorGUILayout.Space(10);

            // Settings section
            if (compressor.Preset == CompressorPreset.Custom)
            {
                _presetDrawer.DrawCustomSettings(compressor, _propertySet);
            }
            else
            {
                _presetDrawer.DrawPresetSummary(compressor);
                EditorGUILayout.Space(5);

                _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings (Read Only)", true);
                if (_showAdvancedSettings)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    _presetDrawer.DrawAllSettings(compressor, _propertySet);
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.Space(10);
            _presetDrawer.DrawTextureFilters(compressor);
            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(_enableLogging, new GUIContent("Enable Logging"));

            EditorGUILayout.Space(10);

            // Excluded paths section
            int excludedCount = compressor.ExcludedPaths.Count;
            string excludedLabel = excludedCount > 0
                ? $"Path Exclusions ({excludedCount})"
                : "Path Exclusions";
            _showExcludedPathsSection = EditorGUILayout.Foldout(_showExcludedPathsSection, excludedLabel, true);
            if (_showExcludedPathsSection)
            {
                _presetDrawer.DrawExcludedPathsSection(compressor);
            }

            EditorGUILayout.Space(15);

            // Search section
            int frozenHits = _searchDrawer.CountFrozenMatches(compressor);
            int previewHits = _searchDrawer.CountPreviewMatches(_previewData);
            _searchDrawer.Draw(frozenHits, previewHits, Repaint);

            EditorGUILayout.Space(10);

            // Frozen textures section
            _frozenDrawer.Draw(compressor, _searchDrawer.SearchText, _searchDrawer.MatchesFrozenSearch);

            EditorGUILayout.Space(15);

            // Preview section
            DrawPreviewSection(compressor);

            EditorGUILayout.Space(15);

            // Footer
            _footerDrawer.Draw();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPreviewSection(TextureCompressor compressor)
        {
            bool isOutdated = IsPreviewOutdated(compressor);

            _previewDrawer.DrawPreviewButton(() =>
            {
                GeneratePreview(compressor);
                _showPreview = true;
            });

            if (_showPreview && _previewData != null && _previewData.Length > 0)
            {
                if (isOutdated)
                {
                    _previewDrawer.DrawOutdatedWarning();
                }

                _previewDrawer.DrawPreview(
                    compressor,
                    _previewData,
                    _processedCount,
                    _frozenCount,
                    _skippedCount,
                    _searchDrawer.SearchText,
                    _searchDrawer.MatchesPreviewSearch,
                    () =>
                    {
                        _showPreview = false;
                        _previewData = null;
                    });
            }
            else if (_showPreview && (_previewData == null || _previewData.Length == 0))
            {
                _previewDrawer.DrawNoTexturesMessage(() => _showPreview = false);
            }
        }

        private void GeneratePreview(TextureCompressor config)
        {
            var result = TexturePreviewGenerator.Generate(config);
            _previewData = result.PreviewData;
            _processedCount = result.ProcessedCount;
            _frozenCount = result.FrozenCount;
            _skippedCount = result.SkippedCount;
            _previewSettingsHash = result.SettingsHash;
        }

        private bool IsPreviewOutdated(TextureCompressor config)
        {
            if (!_showPreview || _previewData == null)
                return false;

            return TexturePreviewGenerator.ComputeSettingsHash(config) != _previewSettingsHash;
        }
    }
}
