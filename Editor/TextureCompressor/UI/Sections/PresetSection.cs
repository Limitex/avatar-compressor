using System.Collections.Generic;
using System.Linq;
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

            if (config.Preset == CompressorPreset.Custom)
            {
                EditorGUILayout.Space(10);
                DrawCustomModeSelector(config);
            }

            EditorGUILayout.Space(10);
            DrawPresetDescription(config.Preset);

            if (config.Preset != CompressorPreset.Custom)
            {
                EditorGUILayout.Space(10);
                DrawSettingsSummary(config);
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
                CompressorPreset.Balanced,
                "Balanced",
                "Balance of\nquality and size",
                PresetColors.Balanced,
                buttonWidth
            );
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(ButtonSpacing);

            EditorGUILayout.BeginHorizontal();
            DrawPresetButton(
                config,
                CompressorPreset.Aggressive,
                "Aggressive",
                "Smaller file size\nSome quality loss",
                PresetColors.Aggressive,
                buttonWidth
            );
            GUILayout.Space(ButtonSpacing);
            DrawPresetButton(
                config,
                CompressorPreset.Maximum,
                "Maximum",
                "Smallest size\nNoticeable quality loss",
                PresetColors.Maximum,
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

        // Cached button rects for dropdown menu positioning, keyed by config instance ID.
        // GetLastRect() only returns accurate values during Repaint events.
        // Using a dictionary to support multiple Inspector windows showing different objects.
        // Stores (Rect, accessTime) to enable LRU-style eviction of oldest entries.
        private static readonly Dictionary<
            int,
            (Rect rect, double accessTime)
        > _customPresetButtonRects = new();
        private const int MaxCachedRects = 32;

        private static void EvictOldestCacheEntry()
        {
            if (_customPresetButtonRects.Count == 0)
                return;

            var first = _customPresetButtonRects.First();
            int oldestKey = first.Key;
            double oldestTime = first.Value.accessTime;

            foreach (var kvp in _customPresetButtonRects)
            {
                if (kvp.Value.accessTime < oldestTime)
                {
                    oldestTime = kvp.Value.accessTime;
                    oldestKey = kvp.Key;
                }
            }

            _customPresetButtonRects.Remove(oldestKey);
        }

        private static void DrawCustomModeSelector(TextureCompressor config)
        {
            EditorGUILayout.BeginHorizontal();

            float availableWidth = EditorGUIUtility.currentViewWidth - 20f;
            float buttonWidth = (availableWidth - ButtonSpacing) / 2f;

            // Edit Mode button
            bool isEditMode = config.CustomPresetAsset == null || config.IsInCustomEditMode;
            if (
                EditorDrawUtils.DrawColoredButton(
                    "Edit Mode",
                    "Manually configure compression settings",
                    PresetColors.EditMode,
                    isEditMode,
                    height: 24f,
                    width: buttonWidth
                )
            )
            {
                Undo.RecordObject(config, "Switch to Edit Mode");
                config.SwitchToCustomEditMode();
                EditorUtility.SetDirty(config);
            }

            GUILayout.Space(ButtonSpacing);

            // Custom Preset dropdown button
            bool isUseOnly = config.IsInUseOnlyMode;
            string presetLabel = "Custom Preset \u25BE";
            bool clicked = EditorDrawUtils.DrawColoredButton(
                presetLabel,
                "Select a custom preset from the menu",
                PresetColors.CustomPreset,
                isUseOnly,
                height: 24f,
                width: buttonWidth
            );

            // Capture rect during Repaint for accurate positioning
            if (Event.current.type == EventType.Repaint)
            {
                int instanceId = config.GetInstanceID();
                double currentTime = EditorApplication.timeSinceStartup;

                // Evict oldest entry if cache is full (LRU-style)
                if (
                    _customPresetButtonRects.Count >= MaxCachedRects
                    && !_customPresetButtonRects.ContainsKey(instanceId)
                )
                {
                    EvictOldestCacheEntry();
                }

                _customPresetButtonRects[instanceId] = (
                    GUILayoutUtility.GetLastRect(),
                    currentTime
                );
            }

            if (clicked)
            {
                int instanceId = config.GetInstanceID();
                Rect buttonRect;
                if (_customPresetButtonRects.TryGetValue(instanceId, out var cached))
                {
                    buttonRect = cached.rect;
                    // Update access time on read (LRU)
                    _customPresetButtonRects[instanceId] = (
                        cached.rect,
                        EditorApplication.timeSinceStartup
                    );
                }
                else
                {
                    buttonRect = GUILayoutUtility.GetLastRect();
                }
                ShowCustomPresetMenu(config, buttonRect);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void ShowCustomPresetMenu(TextureCompressor config, Rect buttonRect)
        {
            var menu = CustomPresetScanner.BuildPresetMenu(
                currentPreset: config.CustomPresetAsset,
                onPresetSelected: (preset) =>
                {
                    Undo.RecordObject(config, "Apply Custom Preset");
                    config.ApplyCustomPreset(preset);
                    EditorUtility.SetDirty(config);
                }
            );

            menu.DropDown(buttonRect);
        }

        private static void DrawPresetDescription(CompressorPreset preset)
        {
            string description;
            MessageType messageType;

            switch (preset)
            {
                case CompressorPreset.HighQuality:
                    description =
                        "High Quality Mode: Maximum quality preservation with minimal compression. "
                        + "Only very simple textures (solid colors) will be slightly compressed. "
                        + "Best for showcase avatars or when VRAM is not a concern.";
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Quality:
                    description =
                        "Quality Mode: Preserves texture detail as much as possible. "
                        + "Only low-complexity textures (solid colors, simple gradients) will be compressed. "
                        + "Best for avatars where visual quality is the priority.";
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Balanced:
                    description =
                        "Balanced Mode: Good compromise between quality and VRAM savings. "
                        + "Detailed textures are preserved, while simpler textures are compressed. "
                        + "Recommended for most use cases.";
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Aggressive:
                    description =
                        "Aggressive Mode: Prioritizes smaller file size over quality. "
                        + "Most textures will be compressed to some degree. "
                        + "Good for Quest avatars or when VRAM is limited.";
                    messageType = MessageType.Warning;
                    break;

                case CompressorPreset.Maximum:
                    description =
                        "Maximum Compression: Compresses all textures as much as possible. "
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

        /// <summary>
        /// Draws a compact summary of the current compression settings.
        /// </summary>
        /// <param name="config">The compressor configuration.</param>
        /// <param name="title">Optional title for the summary section.</param>
        public static void DrawSettingsSummary(
            TextureCompressor config,
            string title = "Current Settings Summary"
        )
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

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
