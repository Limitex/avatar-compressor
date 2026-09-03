using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws the preset selection section.
    /// </summary>
    internal static class PresetSection
    {
        /// <summary>
        /// Draws the complete preset section including buttons, description, and summary.
        /// </summary>
        public static void Draw(TextureCompressor config)
        {
            EditorDrawUtils.DrawSectionHeader(AvatarCompressorLocalization.Tr("preset.section"));

            DrawPresetButtons(config);

            EditorGUILayout.Space(10);
            DrawPresetDescription(config.Preset);

            if (config.Preset == CompressorPreset.Custom)
            {
                EditorGUILayout.Space(10);
                CustomSection.Draw(config);
            }
            else
            {
                EditorGUILayout.Space(10);
                SettingsSummaryDrawer.Draw(config);
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
                "preset.high_quality",
                "preset.high_quality.tooltip",
                PresetColors.HighQuality,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Quality,
                "preset.quality",
                "preset.quality.tooltip",
                PresetColors.Quality,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Balanced,
                "preset.balanced",
                "preset.balanced.tooltip",
                PresetColors.Balanced,
                buttonWidth
            );
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(ButtonSpacing);

            EditorGUILayout.BeginHorizontal();
            DrawPresetButton(
                config,
                CompressorPreset.Aggressive,
                "preset.aggressive",
                "preset.aggressive.tooltip",
                PresetColors.Aggressive,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Maximum,
                "preset.maximum",
                "preset.maximum.tooltip",
                PresetColors.Maximum,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Custom,
                "preset.custom",
                "preset.custom.tooltip",
                PresetColors.Custom,
                buttonWidth
            );
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPresetButton(
            TextureCompressor config,
            CompressorPreset preset,
            string labelKey,
            string tooltipKey,
            Color color,
            float width
        )
        {
            bool isSelected = config.Preset == preset;

            if (
                EditorDrawUtils.DrawColoredButton(
                    AvatarCompressorLocalization.Tr(labelKey),
                    AvatarCompressorLocalization.Tr(tooltipKey),
                    color,
                    isSelected,
                    width: width
                )
            )
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
                    description = AvatarCompressorLocalization.Tr(
                        "preset.high_quality.description"
                    );
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Quality:
                    description = AvatarCompressorLocalization.Tr("preset.quality.description");
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Balanced:
                    description = AvatarCompressorLocalization.Tr("preset.balanced.description");
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Aggressive:
                    description = AvatarCompressorLocalization.Tr("preset.aggressive.description");
                    messageType = MessageType.Warning;
                    break;

                case CompressorPreset.Maximum:
                    description = AvatarCompressorLocalization.Tr("preset.maximum.description");
                    messageType = MessageType.Warning;
                    break;

                case CompressorPreset.Custom:
                    description = AvatarCompressorLocalization.Tr("preset.custom.description");
                    messageType = MessageType.Info;
                    break;

                default:
                    description = "";
                    messageType = MessageType.None;
                    break;
            }

            EditorGUILayout.HelpBox(description, messageType);
        }
    }
}
