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
            EditorDrawUtils.DrawSectionHeader(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:preset")
            );

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
                "TextureCompressor:label:presetHighQuality:tooltip",
                PresetColors.HighQuality,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Quality,
                "TextureCompressor:label:presetQuality:tooltip",
                PresetColors.Quality,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Balanced,
                "TextureCompressor:label:presetBalanced:tooltip",
                PresetColors.Balanced,
                buttonWidth
            );
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(ButtonSpacing);

            EditorGUILayout.BeginHorizontal();
            DrawPresetButton(
                config,
                CompressorPreset.Aggressive,
                "TextureCompressor:label:presetAggressive:tooltip",
                PresetColors.Aggressive,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Maximum,
                "TextureCompressor:label:presetMaximum:tooltip",
                PresetColors.Maximum,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Custom,
                "TextureCompressor:label:presetCustom:tooltip",
                PresetColors.Custom,
                buttonWidth
            );
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPresetButton(
            TextureCompressor config,
            CompressorPreset preset,
            string tooltipKey,
            Color color,
            float width
        )
        {
            bool isSelected = config.Preset == preset;

            if (
                EditorDrawUtils.DrawColoredButton(
                    GetPresetDisplayName(preset),
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

        internal static string GetPresetDisplayName(CompressorPreset preset)
        {
            return preset switch
            {
                CompressorPreset.HighQuality => "High Quality",
                CompressorPreset.Quality => "Quality",
                CompressorPreset.Balanced => "Balanced",
                CompressorPreset.Aggressive => "Aggressive",
                CompressorPreset.Maximum => "Maximum",
                CompressorPreset.Custom => "Custom",
                _ => preset.ToString(),
            };
        }

        private static void DrawPresetDescription(CompressorPreset preset)
        {
            string description;
            MessageType messageType;

            switch (preset)
            {
                case CompressorPreset.HighQuality:
                    description = AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:presetHighQualityDescription"
                    );
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Quality:
                    description = AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:presetQualityDescription"
                    );
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Balanced:
                    description = AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:presetBalancedDescription"
                    );
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Aggressive:
                    description = AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:presetAggressiveDescription"
                    );
                    messageType = MessageType.Warning;
                    break;

                case CompressorPreset.Maximum:
                    description = AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:presetMaximumDescription"
                    );
                    messageType = MessageType.Warning;
                    break;

                case CompressorPreset.Custom:
                    description = AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:presetCustomDescription"
                    );
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
