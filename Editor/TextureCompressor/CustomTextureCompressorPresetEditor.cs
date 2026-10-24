using dev.limitex.avatar.compressor.editor.texture.ui;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    [CustomEditor(typeof(CustomTextureCompressorPreset))]
    [CanEditMultipleObjects]
    internal sealed class CustomTextureCompressorPresetEditor : Editor
    {
        private void OnEnable()
        {
            AvatarCompressorLocalization.Changed += Repaint;
        }

        private void OnDisable()
        {
            AvatarCompressorLocalization.Changed -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (AvatarCompressorPreferences.ShowLanguagePickerInInspector)
            {
                AvatarCompressorLocalization.DrawLanguagePicker();
                EditorGUILayout.Space(8);
            }

            DrawHeader("TextureCompressor:label:presetSettings");
            DrawProperty(nameof(CustomTextureCompressorPreset.Lock));
            DrawProperty(nameof(CustomTextureCompressorPreset.Description));

            EditorGUILayout.Space(8);
            DrawHeader("TextureCompressor:label:presetMenu");
            DrawProperty(nameof(CustomTextureCompressorPreset.MenuPath));
            DrawProperty(nameof(CustomTextureCompressorPreset.MenuOrder));

            EditorGUILayout.Space(8);
            DrawHeader("TextureCompressor:label:analysisStrategy");
            DrawEnumProperty<AnalysisStrategyType>(nameof(CustomTextureCompressorPreset.Strategy));

            DrawHeader("TextureCompressor:label:combinedWeights");
            DrawProperty(nameof(CustomTextureCompressorPreset.FastWeight));
            DrawProperty(nameof(CustomTextureCompressorPreset.HighAccuracyWeight));
            DrawProperty(nameof(CustomTextureCompressorPreset.PerceptualWeight));

            DrawHeader("TextureCompressor:label:complexityThresholds");
            DrawProperty(nameof(CustomTextureCompressorPreset.HighComplexityThreshold));
            DrawProperty(nameof(CustomTextureCompressorPreset.LowComplexityThreshold));

            DrawHeader("TextureCompressor:label:resolution");
            DrawProperty(nameof(CustomTextureCompressorPreset.MinDivisor));
            DrawProperty(nameof(CustomTextureCompressorPreset.MaxDivisor));
            DrawProperty(nameof(CustomTextureCompressorPreset.MaxResolution));
            DrawProperty(nameof(CustomTextureCompressorPreset.MinResolution));
            DrawProperty(nameof(CustomTextureCompressorPreset.ForcePowerOfTwo));

            DrawHeader("TextureCompressor:label:sizeFilters");
            DrawProperty(nameof(CustomTextureCompressorPreset.MinSourceSize));
            DrawProperty(nameof(CustomTextureCompressorPreset.SkipIfSmallerThan));

            DrawHeader("TextureCompressor:label:compressionFormat");
            DrawEnumProperty<CompressionPlatform>(
                nameof(CustomTextureCompressorPreset.TargetPlatform)
            );
            DrawProperty(
                nameof(CustomTextureCompressorPreset.UseHighQualityFormatForHighComplexity)
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

        private void DrawProperty(string propertyName)
        {
            GUI.SetNextControlName(propertyName);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(propertyName),
                TextureCompressorPropertyLabels.Content(propertyName),
                true
            );
        }

        private void DrawEnumProperty<T>(string propertyName)
            where T : struct, System.Enum
        {
            var property = serializedObject.FindProperty(propertyName);
            LocalizedEnumPopup.DrawProperty<T>(
                property,
                TextureCompressorPropertyLabels.Content(propertyName),
                TextureCompressorEnumLabels.Text
            );
        }
    }
}
