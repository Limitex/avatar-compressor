using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws the settings section (custom settings and advanced settings foldout).
    /// </summary>
    public static class SettingsSection
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
            bool isEditable = CustomPresetEditorState.IsCustomEditable(config);

            if (isEditable)
            {
                DrawAllSettings(config, serializedObject, compactMode: false);
            }
            else
            {
                showAdvanced = EditorGUILayout.Foldout(
                    showAdvanced,
                    "Advanced Settings (Read Only)",
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
                "Strategy",
                compactMode
            );

            if (config.Strategy == AnalysisStrategyType.Combined)
            {
                if (!compactMode)
                    EditorGUI.indentLevel++;
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.FastWeight),
                    "Fast Weight",
                    compactMode
                );
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.HighAccuracyWeight),
                    "High Accuracy Weight",
                    compactMode
                );
                DrawPropertyWithModifiedIndicator(
                    config,
                    serializedObject,
                    nameof(TextureCompressor.PerceptualWeight),
                    "Perceptual Weight",
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
                "High (Keep Detail)",
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.LowComplexityThreshold),
                "Low (Compress More)",
                compactMode
            );

            DrawSectionSpacing(compactMode);

            // Resolution Settings
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinDivisor),
                "Min Divisor",
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MaxDivisor),
                "Max Divisor",
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MaxResolution),
                "Max Resolution",
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinResolution),
                "Min Resolution",
                compactMode
            );

            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.ForcePowerOfTwo),
                "Force Power of 2",
                compactMode,
                "When enabled, dimensions are rounded to nearest power of 2.\n"
                    + "When disabled, dimensions are rounded to nearest multiple of 4.\n"
                    + "Note: All output dimensions are always multiples of 4 for DXT/BC compression compatibility."
            );
            if (!compactMode)
            {
                EditorGUILayout.HelpBox(
                    "Output dimensions are always multiples of 4 for DXT/BC compression compatibility. "
                        + "Example: 150x150 becomes 152x152.",
                    MessageType.Info
                );
            }

            DrawSectionSpacing(compactMode);

            // Size Filters
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.MinSourceSize),
                "Min Source Size",
                compactMode
            );
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.SkipIfSmallerThan),
                "Skip If Smaller Than",
                compactMode
            );

            DrawSectionSpacing(compactMode);

            // Compression Format
            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.TargetPlatform),
                "Target Platform",
                compactMode
            );

            DrawPropertyWithModifiedIndicator(
                config,
                serializedObject,
                nameof(TextureCompressor.UseHighQualityFormatForHighComplexity),
                "High Quality for Complex",
                compactMode,
                "Use BC7/ASTC_4x4 for high complexity textures (uses Complexity Threshold)"
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
            string label,
            bool compactMode,
            string tooltip = null
        )
        {
            if (compactMode)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName));
            }
            else
            {
                bool isModified = IsFieldModified(config, propertyName);
                string displayLabel = label ?? propertyName;
                if (isModified)
                {
                    displayLabel = displayLabel + " *";
                }

                var originalColor = GUI.contentColor;
                if (isModified)
                {
                    GUI.contentColor = EditorStylesCache.ModifiedStatusStyle.normal.textColor;
                }

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(propertyName),
                    new GUIContent(displayLabel, tooltip)
                );

                GUI.contentColor = originalColor;
            }
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
