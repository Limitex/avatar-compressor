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
                "TextureCompressor:prop:strategy",
                compactMode,
                "TextureCompressor:prop:strategy:tooltip"
            );

            if (config.Strategy == AnalysisStrategyType.Combined)
            {
                if (!compactMode)
                    EditorGUI.indentLevel++;
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.FastWeight),
                    "TextureCompressor:prop:fastWeight",
                    compactMode
                );
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.HighAccuracyWeight),
                    "TextureCompressor:prop:highAccuracyWeight",
                    compactMode
                );
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.PerceptualWeight),
                    "TextureCompressor:prop:perceptualWeight",
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
                "TextureCompressor:prop:highThreshold",
                compactMode,
                "TextureCompressor:prop:highThreshold:tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.LowComplexityThreshold),
                "TextureCompressor:prop:lowThreshold",
                compactMode,
                "TextureCompressor:prop:lowThreshold:tooltip"
            );

            DrawSectionSpacing(compactMode);

            // Resolution Settings
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinDivisor),
                "TextureCompressor:prop:minDivisor",
                compactMode,
                "TextureCompressor:prop:minDivisor:tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MaxDivisor),
                "TextureCompressor:prop:maxDivisor",
                compactMode,
                "TextureCompressor:prop:maxDivisor:tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MaxResolution),
                "TextureCompressor:prop:maxResolution",
                compactMode,
                "TextureCompressor:prop:maxResolution:tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinResolution),
                "TextureCompressor:prop:minResolution",
                compactMode,
                "TextureCompressor:prop:minResolution:tooltip"
            );

            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.ForcePowerOfTwo),
                "TextureCompressor:prop:forcePowerOfTwo",
                compactMode,
                "TextureCompressor:prop:forcePowerOfTwo:tooltip"
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
                "TextureCompressor:prop:minSourceSize",
                compactMode,
                "TextureCompressor:prop:minSourceSize:tooltip"
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.SkipIfSmallerThan),
                "TextureCompressor:prop:skipIfSmaller",
                compactMode,
                "TextureCompressor:prop:skipIfSmaller:tooltip"
            );

            DrawSectionSpacing(compactMode);

            // Compression Format
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.TargetPlatform),
                "TextureCompressor:prop:targetPlatform",
                compactMode,
                "TextureCompressor:prop:targetPlatform:tooltip"
            );

            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.UseHighQualityFormatForHighComplexity),
                "TextureCompressor:prop:highQualityComplex",
                compactMode,
                "TextureCompressor:prop:highQualityComplex:tooltip"
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
                AvatarCompressorLocalization.DrawEnumProperty<AnalysisStrategyType>(
                    "TextureCompressor",
                    property,
                    content
                );
            }
            else if (propertyName == nameof(TextureCompressor.TargetPlatform))
            {
                AvatarCompressorLocalization.DrawEnumProperty<CompressionPlatform>(
                    "TextureCompressor",
                    property,
                    content
                );
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
