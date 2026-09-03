using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws the settings section (custom settings and advanced settings foldout).
    /// </summary>
    internal static class SettingsSection
    {
        /// <summary>
        /// Draws custom settings when in Custom preset mode, or advanced settings foldout otherwise.
        /// </summary>
        public static void Draw(
            TextureCompressor config,
            SerializedObject serializedObject,
            ref bool showAdvanced
        )
        {
            bool isEditable = PresetEditorState.IsCustomEditable(config);

            if (isEditable)
            {
                DrawAllSettings(config, serializedObject, compactMode: false);
            }
            else
            {
                showAdvanced = EditorGUILayout.Foldout(
                    showAdvanced,
                    AvatarCompressorLocalization.Tr("settings.advanced_read_only"),
                    true
                );
                if (showAdvanced)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    DrawAllSettings(config, serializedObject, compactMode: true);
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        private static void DrawAllSettings(
            TextureCompressor config,
            SerializedObject serializedObject,
            bool compactMode
        )
        {
            if (compactMode)
                EditorGUI.indentLevel++;

            // Analysis Strategy
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.Strategy),
                "settings.strategy",
                compactMode,
                "settings.strategy.tooltip"
            );

            if (config.Strategy == AnalysisStrategyType.Combined)
            {
                if (!compactMode)
                    EditorGUI.indentLevel++;
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.FastWeight),
                    "settings.fast_weight",
                    compactMode
                );
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.HighAccuracyWeight),
                    "settings.high_accuracy_weight",
                    compactMode
                );
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.PerceptualWeight),
                    "settings.perceptual_weight",
                    compactMode
                );
                if (!compactMode)
                    EditorGUI.indentLevel--;
            }

            DrawSectionSpacing(compactMode);

            // Complexity Thresholds
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.HighComplexityThreshold),
                "settings.high_threshold",
                compactMode,
                "settings.high_threshold.tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.LowComplexityThreshold),
                "settings.low_threshold",
                compactMode,
                "settings.low_threshold.tooltip"
            );

            DrawSectionSpacing(compactMode);

            // Resolution Settings
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinDivisor),
                "settings.min_divisor",
                compactMode,
                "settings.min_divisor.tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MaxDivisor),
                "settings.max_divisor",
                compactMode,
                "settings.max_divisor.tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MaxResolution),
                "settings.max_resolution",
                compactMode,
                "settings.max_resolution.tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinResolution),
                "settings.min_resolution",
                compactMode,
                "settings.min_resolution.tooltip"
            );

            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.ForcePowerOfTwo),
                "settings.force_power_of_two",
                compactMode,
                "settings.force_power_of_two.tooltip"
            );
            if (!compactMode)
            {
                EditorGUILayout.HelpBox(
                    AvatarCompressorLocalization.Tr("settings.multiple_of_four.help"),
                    MessageType.Info
                );
            }

            DrawSectionSpacing(compactMode);

            // Size Filters
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinSourceSize),
                "settings.min_source_size",
                compactMode,
                "settings.min_source_size.tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.SkipIfSmallerThan),
                "settings.skip_if_smaller",
                compactMode,
                "settings.skip_if_smaller.tooltip"
            );

            DrawSectionSpacing(compactMode);

            // Compression Format
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.TargetPlatform),
                "settings.target_platform",
                compactMode,
                "settings.target_platform.tooltip"
            );

            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.UseHighQualityFormatForHighComplexity),
                "settings.high_quality_complex",
                compactMode,
                "settings.high_quality_complex.tooltip"
            );

            if (compactMode)
                EditorGUI.indentLevel--;
        }

        private static void DrawSectionSpacing(bool compactMode)
        {
            if (!compactMode)
            {
                EditorGUILayout.Space(10);
            }
        }

        private static void DrawPropertyWithModifiedIndicator(
            TextureCompressor config,
            SerializedObject serializedObject,
            string propertyName,
            string labelKey,
            bool compactMode,
            string tooltipKey = null
        )
        {
            bool isModified = !compactMode && IsFieldModified(config, propertyName);
            string displayLabel = AvatarCompressorLocalization.Tr(labelKey);
            if (isModified)
            {
                displayLabel += " *";
            }

            var originalColor = GUI.contentColor;
            if (isModified)
            {
                GUI.contentColor = EditorStylesCache.ModifiedStatusStyle.normal.textColor;
            }

            var property = serializedObject.FindProperty(propertyName);
            var content = new GUIContent(
                displayLabel,
                tooltipKey == null ? null : AvatarCompressorLocalization.Tr(tooltipKey)
            );

            if (propertyName == nameof(TextureCompressor.Strategy))
            {
                var currentValue = (AnalysisStrategyType)property.intValue;
                var newValue = AvatarCompressorLocalization.EnumPopup(content, currentValue);
                property.intValue = (int)newValue;
            }
            else if (propertyName == nameof(TextureCompressor.TargetPlatform))
            {
                var currentValue = (CompressionPlatform)property.intValue;
                var newValue = AvatarCompressorLocalization.EnumPopup(content, currentValue);
                property.intValue = (int)newValue;
            }
            else
            {
                EditorGUILayout.PropertyField(property, content);
            }

            GUI.contentColor = originalColor;
        }

        private static bool IsFieldModified(TextureCompressor config, string fieldName)
        {
            if (config.Preset != CompressorPreset.Custom)
                return false;

            if (config.CustomPresetAsset == null)
                return false;

            return config.CustomPresetAsset.IsFieldModified(fieldName, config);
        }
    }
}
