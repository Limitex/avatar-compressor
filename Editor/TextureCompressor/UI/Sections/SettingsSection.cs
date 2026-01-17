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
            if (config.Preset == CompressorPreset.Custom)
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
            DrawSectionHeader("Analysis Strategy", compactMode);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Strategy"));

            if (config.Strategy == AnalysisStrategyType.Combined)
            {
                if (!compactMode)
                    EditorGUI.indentLevel++;
                DrawProperty(serializedObject, "FastWeight", "Fast Weight", compactMode);
                DrawProperty(
                    serializedObject,
                    "HighAccuracyWeight",
                    "High Accuracy Weight",
                    compactMode
                );
                DrawProperty(
                    serializedObject,
                    "PerceptualWeight",
                    "Perceptual Weight",
                    compactMode
                );
                if (!compactMode)
                    EditorGUI.indentLevel--;
            }

            DrawSectionSpacing(compactMode);

            // Complexity Thresholds
            DrawSectionHeader("Complexity Thresholds", compactMode);
            DrawProperty(
                serializedObject,
                "HighComplexityThreshold",
                "High (Keep Detail)",
                compactMode
            );
            DrawProperty(
                serializedObject,
                "LowComplexityThreshold",
                "Low (Compress More)",
                compactMode
            );

            DrawSectionSpacing(compactMode);

            // Resolution Settings
            DrawSectionHeader("Resolution Settings", compactMode);
            DrawProperty(serializedObject, "MinDivisor", "Min Divisor", compactMode);
            DrawProperty(serializedObject, "MaxDivisor", "Max Divisor", compactMode);
            DrawProperty(serializedObject, "MaxResolution", "Max Resolution", compactMode);
            DrawProperty(serializedObject, "MinResolution", "Min Resolution", compactMode);

            if (compactMode)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ForcePowerOfTwo"));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("ForcePowerOfTwo"),
                    new GUIContent(
                        "Force Power of 2",
                        "When enabled, dimensions are rounded to nearest power of 2.\n"
                            + "When disabled, dimensions are rounded to nearest multiple of 4.\n"
                            + "Note: All output dimensions are always multiples of 4 for DXT/BC compression compatibility."
                    )
                );
                EditorGUILayout.HelpBox(
                    "Output dimensions are always multiples of 4 for DXT/BC compression compatibility. "
                        + "Example: 150x150 becomes 152x152.",
                    MessageType.Info
                );
            }

            DrawSectionSpacing(compactMode);

            // Size Filters
            DrawSectionHeader("Size Filters", compactMode);
            DrawProperty(serializedObject, "MinSourceSize", "Min Source Size", compactMode);
            DrawProperty(
                serializedObject,
                "SkipIfSmallerThan",
                "Skip If Smaller Than",
                compactMode
            );

            DrawSectionSpacing(compactMode);

            // Compression Format
            DrawSectionHeader("Compression Format", compactMode);
            DrawProperty(serializedObject, "TargetPlatform", "Target Platform", compactMode);

            if (compactMode)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("UseHighQualityFormatForHighComplexity")
                );
            }
            else
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("UseHighQualityFormatForHighComplexity"),
                    new GUIContent(
                        "High Quality for Complex",
                        "Use BC7/ASTC_4x4 for high complexity textures (uses Complexity Threshold)"
                    )
                );
            }

            if (compactMode)
                EditorGUI.indentLevel--;
        }

        private static void DrawSectionHeader(string label, bool compactMode)
        {
            if (!compactMode)
            {
                EditorDrawUtils.DrawSectionHeader(label);
            }
        }

        private static void DrawSectionSpacing(bool compactMode)
        {
            if (!compactMode)
            {
                EditorGUILayout.Space(10);
            }
        }

        private static void DrawProperty(
            SerializedObject serializedObject,
            string propertyName,
            string label,
            bool compactMode
        )
        {
            if (compactMode)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName));
            }
            else
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(propertyName),
                    new GUIContent(label)
                );
            }
        }
    }
}
