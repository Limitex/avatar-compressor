using dev.limitex.avatar.compressor.editor.texture.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Localized inspector for custom texture compressor preset assets.
    /// </summary>
    [CustomEditor(typeof(CustomTextureCompressorPreset))]
    internal sealed class CustomTextureCompressorPresetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AvatarCompressorLocalization.DrawLanguagePicker();
            EditorGUILayout.Space(8);

            var preset = (CustomTextureCompressorPreset)target;

            DrawHeader("preset_asset.settings");
            DrawProperty(nameof(CustomTextureCompressorPreset.Lock), "preset_asset.lock");

            if (BuiltInPresetLocalization.IsBuiltIn(preset))
            {
                EditorGUILayout.LabelField(
                    AvatarCompressorLocalization.Tr("preset_asset.description"),
                    EditorStyles.miniLabel
                );
                EditorGUILayout.LabelField(
                    BuiltInPresetLocalization.GetDescription(preset),
                    EditorStyles.wordWrappedLabel
                );
            }
            else
            {
                DrawProperty(
                    nameof(CustomTextureCompressorPreset.Description),
                    "preset_asset.description"
                );
            }

            EditorGUILayout.Space(8);
            DrawHeader("preset_asset.menu");
            if (BuiltInPresetLocalization.IsBuiltIn(preset))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        AvatarCompressorLocalization.Content(
                            "preset_asset.menu_path",
                            "preset_asset.menu_path.tooltip"
                        ),
                        BuiltInPresetLocalization.GetMenuPath(preset)
                    );
                }
            }
            else
            {
                DrawProperty(
                    nameof(CustomTextureCompressorPreset.MenuPath),
                    "preset_asset.menu_path",
                    "preset_asset.menu_path.tooltip"
                );
            }
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MenuOrder),
                "preset_asset.menu_order",
                "preset_asset.menu_order.tooltip"
            );

            EditorGUILayout.Space(8);
            DrawHeader("settings.analysis_strategy");
            DrawEnumProperty<AnalysisStrategyType>(
                nameof(CustomTextureCompressorPreset.Strategy),
                "settings.strategy",
                "settings.strategy.tooltip"
            );

            DrawHeader("settings.combined_weights");
            DrawProperty(nameof(CustomTextureCompressorPreset.FastWeight), "settings.fast_weight");
            DrawProperty(
                nameof(CustomTextureCompressorPreset.HighAccuracyWeight),
                "settings.high_accuracy_weight"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.PerceptualWeight),
                "settings.perceptual_weight"
            );

            DrawHeader("settings.complexity_thresholds");
            DrawProperty(
                nameof(CustomTextureCompressorPreset.HighComplexityThreshold),
                "settings.high_threshold",
                "settings.high_threshold.tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.LowComplexityThreshold),
                "settings.low_threshold",
                "settings.low_threshold.tooltip"
            );

            DrawHeader("settings.resolution");
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MinDivisor),
                "settings.min_divisor",
                "settings.min_divisor.tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MaxDivisor),
                "settings.max_divisor",
                "settings.max_divisor.tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MaxResolution),
                "settings.max_resolution",
                "settings.max_resolution.tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MinResolution),
                "settings.min_resolution",
                "settings.min_resolution.tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.ForcePowerOfTwo),
                "settings.force_power_of_two",
                "settings.force_power_of_two.tooltip"
            );

            DrawHeader("settings.size_filters");
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MinSourceSize),
                "settings.min_source_size",
                "settings.min_source_size.tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.SkipIfSmallerThan),
                "settings.skip_if_smaller",
                "settings.skip_if_smaller.tooltip"
            );

            DrawHeader("settings.compression_format");
            DrawEnumProperty<CompressionPlatform>(
                nameof(CustomTextureCompressorPreset.TargetPlatform),
                "settings.target_platform",
                "settings.target_platform.tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.UseHighQualityFormatForHighComplexity),
                "settings.high_quality_complex",
                "settings.high_quality_complex.tooltip"
            );

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawHeader(string key)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr(key),
                EditorStyles.boldLabel
            );
        }

        private void DrawProperty(string propertyName, string labelKey, string tooltipKey = null)
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(propertyName),
                AvatarCompressorLocalization.Content(labelKey, tooltipKey),
                true
            );
        }

        private void DrawEnumProperty<T>(
            string propertyName,
            string labelKey,
            string tooltipKey = null
        )
            where T : struct, System.Enum
        {
            var property = serializedObject.FindProperty(propertyName);
            var currentValue = (T)System.Enum.ToObject(typeof(T), property.intValue);
            var newValue = AvatarCompressorLocalization.EnumPopup(
                AvatarCompressorLocalization.Content(labelKey, tooltipKey),
                currentValue
            );
            property.intValue = System.Convert.ToInt32(newValue);
        }
    }
}
