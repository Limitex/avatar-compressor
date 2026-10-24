using System;
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
                    AvatarCompressorLocalization.Tr("TextureCompressor:message:advancedReadOnly"),
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
                compactMode,
                (property, content) =>
                    LocalizedEnumPopup.DrawProperty<AnalysisStrategyType>(
                        property,
                        content,
                        TextureCompressorEnumLabels.Text
                    )
            );

            if (config.Strategy == AnalysisStrategyType.Combined)
            {
                if (!compactMode)
                    EditorGUI.indentLevel++;
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.FastWeight),
                    compactMode
                );
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.HighAccuracyWeight),
                    compactMode
                );
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.PerceptualWeight),
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
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.LowComplexityThreshold),
                compactMode
            );

            DrawSectionSpacing(compactMode);

            // Resolution Settings
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinDivisor),
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MaxDivisor),
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MaxResolution),
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinResolution),
                compactMode
            );

            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.ForcePowerOfTwo),
                compactMode
            );
            if (!compactMode)
            {
                EditorGUILayout.HelpBox(
                    AvatarCompressorLocalization.Tr("TextureCompressor:message:multipleOfFourHelp"),
                    MessageType.Info
                );
            }

            DrawSectionSpacing(compactMode);

            // Size Filters
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinSourceSize),
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.SkipIfSmallerThan),
                compactMode
            );

            DrawSectionSpacing(compactMode);

            // Compression Format
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.TargetPlatform),
                compactMode,
                (property, content) =>
                    LocalizedEnumPopup.DrawProperty<CompressionPlatform>(
                        property,
                        content,
                        TextureCompressorEnumLabels.Text
                    )
            );

            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.UseHighQualityFormatForHighComplexity),
                compactMode
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
            bool compactMode,
            Action<SerializedProperty, GUIContent> drawField = null
        )
        {
            bool isModified = !compactMode && IsFieldModified(config, propertyName);
            var content = TextureCompressorPropertyLabels.Content(propertyName);
            if (isModified)
            {
                content.text += " *";
            }

            var originalColor = GUI.contentColor;
            if (isModified)
            {
                GUI.contentColor = EditorStylesCache.ModifiedStatusStyle.normal.textColor;
            }

            var property = serializedObject.FindProperty(propertyName);

            if (drawField != null)
            {
                drawField(property, content);
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
