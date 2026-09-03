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

            DrawHeader("TextureCompressor:label:presetSettings");
            DrawProperty(
                nameof(CustomTextureCompressorPreset.Lock),
                "TextureCompressor:prop:presetLock"
            );

            if (BuiltInPresetLocalization.IsBuiltIn(preset))
            {
                EditorGUILayout.LabelField(
                    AvatarCompressorLocalization.Tr("TextureCompressor:prop:presetDescription"),
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
                    "TextureCompressor:prop:presetDescription"
                );
            }

            EditorGUILayout.Space(8);
            DrawHeader("TextureCompressor:label:presetMenu");
            if (BuiltInPresetLocalization.IsBuiltIn(preset))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        AvatarCompressorLocalization.Content(
                            "TextureCompressor:prop:presetMenuPath",
                            "TextureCompressor:prop:presetMenuPath:tooltip"
                        ),
                        BuiltInPresetLocalization.GetMenuPath(preset)
                    );
                }
            }
            else
            {
                DrawProperty(
                    nameof(CustomTextureCompressorPreset.MenuPath),
                    "TextureCompressor:prop:presetMenuPath",
                    "TextureCompressor:prop:presetMenuPath:tooltip"
                );
            }
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MenuOrder),
                "TextureCompressor:prop:presetMenuOrder",
                "TextureCompressor:prop:presetMenuOrder:tooltip"
            );

            EditorGUILayout.Space(8);
            DrawHeader("TextureCompressor:label:analysisStrategy");
            DrawEnumProperty<AnalysisStrategyType>(
                nameof(CustomTextureCompressorPreset.Strategy),
                "TextureCompressor:prop:strategy",
                "TextureCompressor:prop:strategy:tooltip"
            );

            DrawHeader("TextureCompressor:label:combinedWeights");
            DrawProperty(
                nameof(CustomTextureCompressorPreset.FastWeight),
                "TextureCompressor:prop:fastWeight"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.HighAccuracyWeight),
                "TextureCompressor:prop:highAccuracyWeight"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.PerceptualWeight),
                "TextureCompressor:prop:perceptualWeight"
            );

            DrawHeader("TextureCompressor:label:complexityThresholds");
            DrawProperty(
                nameof(CustomTextureCompressorPreset.HighComplexityThreshold),
                "TextureCompressor:prop:highThreshold",
                "TextureCompressor:prop:highThreshold:tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.LowComplexityThreshold),
                "TextureCompressor:prop:lowThreshold",
                "TextureCompressor:prop:lowThreshold:tooltip"
            );

            DrawHeader("TextureCompressor:label:resolution");
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MinDivisor),
                "TextureCompressor:prop:minDivisor",
                "TextureCompressor:prop:minDivisor:tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MaxDivisor),
                "TextureCompressor:prop:maxDivisor",
                "TextureCompressor:prop:maxDivisor:tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MaxResolution),
                "TextureCompressor:prop:maxResolution",
                "TextureCompressor:prop:maxResolution:tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MinResolution),
                "TextureCompressor:prop:minResolution",
                "TextureCompressor:prop:minResolution:tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.ForcePowerOfTwo),
                "TextureCompressor:prop:forcePowerOfTwo",
                "TextureCompressor:prop:forcePowerOfTwo:tooltip"
            );

            DrawHeader("TextureCompressor:label:sizeFilters");
            DrawProperty(
                nameof(CustomTextureCompressorPreset.MinSourceSize),
                "TextureCompressor:prop:minSourceSize",
                "TextureCompressor:prop:minSourceSize:tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.SkipIfSmallerThan),
                "TextureCompressor:prop:skipIfSmaller",
                "TextureCompressor:prop:skipIfSmaller:tooltip"
            );

            DrawHeader("TextureCompressor:label:compressionFormat");
            DrawEnumProperty<CompressionPlatform>(
                nameof(CustomTextureCompressorPreset.TargetPlatform),
                "TextureCompressor:prop:targetPlatform",
                "TextureCompressor:prop:targetPlatform:tooltip"
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.UseHighQualityFormatForHighComplexity),
                "TextureCompressor:prop:highQualityComplex",
                "TextureCompressor:prop:highQualityComplex:tooltip"
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
            AvatarCompressorLocalization.DrawEnumProperty<T>(
                property,
                AvatarCompressorLocalization.Content(labelKey, tooltipKey)
            );
        }
    }
}
