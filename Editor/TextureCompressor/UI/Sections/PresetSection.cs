using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws the preset selection section.
    /// </summary>
    public static class PresetSection
    {
        /// <summary>
        /// Draws the complete preset section including buttons, description, and summary.
        /// </summary>
        public static void Draw(TextureCompressor config)
        {
            EditorDrawUtils.DrawSectionHeader("Compression Preset");

            DrawPresetButtons(config);
            EditorGUILayout.Space(10);
            DrawPresetDescription(config.Preset);

            if (config.Preset != CompressorPreset.Custom)
            {
                EditorGUILayout.Space(10);
                DrawPresetSummary(config);
            }
        }

        private const int ButtonsPerRow = 3;
        private const float ButtonSpacing = 2f;

        private static void DrawPresetButtons(TextureCompressor config)
        {
            float availableWidth = EditorGUIUtility.currentViewWidth - 20f;
            float buttonWidth =
                (availableWidth - ButtonSpacing * (ButtonsPerRow - 1)) / ButtonsPerRow;

            EditorGUILayout.BeginHorizontal();
            DrawPresetButton(
                config,
                CompressorPreset.HighQuality,
                "High Quality",
                "Highest quality\nMinimal compression",
                PresetColors.HighQuality,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Quality,
                "Quality",
                "Good quality\nLight compression",
                PresetColors.Quality,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Standard,
                "Standard",
                "Balance of\nquality and size",
                PresetColors.Standard,
                buttonWidth
            );
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(ButtonSpacing);

            EditorGUILayout.BeginHorizontal();
            DrawPresetButton(
                config,
                CompressorPreset.Balanced,
                "Balanced",
                "Smaller file size\nSome quality loss",
                PresetColors.Balanced,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Aggressive,
                "Aggressive",
                "Smallest size\nNoticeable quality loss",
                PresetColors.Aggressive,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Custom,
                "Custom",
                "Manual\nconfiguration",
                PresetColors.Custom,
                buttonWidth
            );
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPresetButton(
            TextureCompressor config,
            CompressorPreset preset,
            string label,
            string tooltip,
            Color color,
            float width
        )
        {
            bool isSelected = config.Preset == preset;

            if (EditorDrawUtils.DrawColoredButton(label, tooltip, color, isSelected, width: width))
            {
                Undo.RecordObject(config, "Change Compressor Preset");
                config.ApplyPreset(preset);
                EditorUtility.SetDirty(config);
            }
        }

        private static void DrawPresetDescription(CompressorPreset preset)
        {
            string description;
            MessageType messageType;

            switch (preset)
            {
                case CompressorPreset.HighQuality:
                    description =
                        "High Quality Mode: Maximum quality preservation. "
                        + "Warning: Minimal compression - VRAM savings will be limited. "
                        + "Best for showcase avatars where visual quality is critical.";
                    messageType = MessageType.Warning;
                    break;

                case CompressorPreset.Quality:
                    description =
                        "Quality Mode: Preserves texture detail as much as possible. "
                        + "Only low-complexity textures (solid colors, simple gradients) will be compressed. "
                        + "Best for avatars where visual quality is the priority.";
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Standard:
                    description =
                        "Standard Mode: Good compromise between quality and VRAM savings. "
                        + "Detailed textures are preserved, while simpler textures are compressed. "
                        + "Recommended for most use cases.";
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Balanced:
                    description =
                        "Balanced Mode: Reduces file size while maintaining acceptable quality. "
                        + "Most textures will be compressed to some degree. "
                        + "Good for Quest avatars or when VRAM is limited.";
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Aggressive:
                    description =
                        "Aggressive Mode: Compresses all textures as much as possible. "
                        + "Significant quality loss may occur. "
                        + "Use only when file size is critical.";
                    messageType = MessageType.Warning;
                    break;

                case CompressorPreset.Custom:
                    description =
                        "Custom Mode: Full control over all compression settings. "
                        + "Configure each parameter manually for fine-tuned results.";
                    messageType = MessageType.Info;
                    break;

                default:
                    description = "";
                    messageType = MessageType.None;
                    break;
            }

            EditorGUILayout.HelpBox(description, messageType);
        }

        private static void DrawPresetSummary(TextureCompressor config)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorDrawUtils.DrawSectionHeader("Current Settings Summary");

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
