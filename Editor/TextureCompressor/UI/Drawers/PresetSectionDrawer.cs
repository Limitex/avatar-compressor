using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Draws the preset selection and settings sections.
    /// </summary>
    public class PresetSectionDrawer
    {
        private static readonly Color HighQualityColor = new Color(0.1f, 0.9f, 0.6f);
        private static readonly Color QualityColor = new Color(0.2f, 0.8f, 0.4f);
        private static readonly Color BalancedColor = new Color(0.3f, 0.6f, 0.9f);
        private static readonly Color AggressiveColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color MaximumColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color CustomColor = new Color(0.7f, 0.5f, 0.9f);

        public void DrawPresetSection(TextureCompressor compressor)
        {
            GUIDrawing.DrawSectionHeader("Compression Preset");

            EditorGUILayout.BeginHorizontal();
            DrawPresetButton(compressor, CompressorPreset.HighQuality, "High Quality", "Highest quality\nMinimal compression", HighQualityColor);
            DrawPresetButton(compressor, CompressorPreset.Quality, "Quality", "Good quality\nLight compression", QualityColor);
            DrawPresetButton(compressor, CompressorPreset.Balanced, "Balanced", "Balance of\nquality and size", BalancedColor);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawPresetButton(compressor, CompressorPreset.Aggressive, "Aggressive", "Smaller file size\nSome quality loss", AggressiveColor);
            DrawPresetButton(compressor, CompressorPreset.Maximum, "Maximum", "Smallest size\nNoticeable quality loss", MaximumColor);
            DrawPresetButton(compressor, CompressorPreset.Custom, "Custom", "Manual\nconfiguration", CustomColor);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPresetButton(TextureCompressor compressor, CompressorPreset preset, string label, string tooltip, Color color)
        {
            bool isSelected = compressor.Preset == preset;

            if (GUIDrawing.DrawColoredButton(label, tooltip, color, isSelected))
            {
                Undo.RecordObject(compressor, "Change Compressor Preset");
                compressor.ApplyPreset(preset);
                EditorUtility.SetDirty(compressor);
            }
        }

        public void DrawPresetDescription(CompressorPreset preset)
        {
            string description;
            MessageType messageType;

            switch (preset)
            {
                case CompressorPreset.HighQuality:
                    description = "High Quality Mode: Maximum quality preservation with minimal compression. " +
                                  "Only very simple textures (solid colors) will be slightly compressed. " +
                                  "Best for showcase avatars or when VRAM is not a concern.";
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Quality:
                    description = "Quality Mode: Preserves texture detail as much as possible. " +
                                  "Only low-complexity textures (solid colors, simple gradients) will be compressed. " +
                                  "Best for avatars where visual quality is the priority.";
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Balanced:
                    description = "Balanced Mode: Good compromise between quality and VRAM savings. " +
                                  "Detailed textures are preserved, while simpler textures are compressed. " +
                                  "Recommended for most use cases.";
                    messageType = MessageType.Info;
                    break;

                case CompressorPreset.Aggressive:
                    description = "Aggressive Mode: Prioritizes smaller file size over quality. " +
                                  "Most textures will be compressed to some degree. " +
                                  "Good for Quest avatars or when VRAM is limited.";
                    messageType = MessageType.Warning;
                    break;

                case CompressorPreset.Maximum:
                    description = "Maximum Compression: Compresses all textures as much as possible. " +
                                  "Significant quality loss may occur. " +
                                  "Use only when file size is critical.";
                    messageType = MessageType.Warning;
                    break;

                case CompressorPreset.Custom:
                    description = "Custom Mode: Full control over all compression settings. " +
                                  "Configure each parameter manually for fine-tuned results.";
                    messageType = MessageType.Info;
                    break;

                default:
                    description = "";
                    messageType = MessageType.None;
                    break;
            }

            GUIDrawing.DrawHelpBox(description, messageType);
        }

        public void DrawPresetSummary(TextureCompressor compressor)
        {
            GUIDrawing.BeginBox();
            GUIDrawing.DrawSectionHeader("Current Settings Summary");

            EditorGUILayout.LabelField($"Strategy: {compressor.Strategy}");
            EditorGUILayout.LabelField($"Divisor Range: {compressor.MinDivisor}x - {compressor.MaxDivisor}x");
            EditorGUILayout.LabelField($"Resolution Range: {compressor.MinResolution}px - {compressor.MaxResolution}px");
            EditorGUILayout.LabelField($"Complexity Thresholds: {compressor.LowComplexityThreshold:P0} - {compressor.HighComplexityThreshold:P0}");

            GUIDrawing.EndBox();
        }

        public void DrawCustomSettings(TextureCompressor compressor, CompressorPropertySet props)
        {
            GUIDrawing.DrawSectionHeader("Analysis Strategy");
            EditorGUILayout.PropertyField(props.Strategy);

            if (compressor.Strategy == AnalysisStrategyType.Combined)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(props.FastWeight, new GUIContent("Fast Weight"));
                EditorGUILayout.PropertyField(props.HighAccuracyWeight, new GUIContent("High Accuracy Weight"));
                EditorGUILayout.PropertyField(props.PerceptualWeight, new GUIContent("Perceptual Weight"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            GUIDrawing.DrawSectionHeader("Complexity Thresholds");
            EditorGUILayout.PropertyField(props.HighComplexityThreshold, new GUIContent("High (Keep Detail)"));
            EditorGUILayout.PropertyField(props.LowComplexityThreshold, new GUIContent("Low (Compress More)"));

            EditorGUILayout.Space(10);

            GUIDrawing.DrawSectionHeader("Resolution Settings");
            EditorGUILayout.PropertyField(props.MinDivisor, new GUIContent("Min Divisor"));
            EditorGUILayout.PropertyField(props.MaxDivisor, new GUIContent("Max Divisor"));
            EditorGUILayout.PropertyField(props.MaxResolution, new GUIContent("Max Resolution"));
            EditorGUILayout.PropertyField(props.MinResolution, new GUIContent("Min Resolution"));
            EditorGUILayout.PropertyField(props.ForcePowerOfTwo, new GUIContent("Force Power of 2",
                "When enabled, dimensions are rounded to nearest power of 2.\n" +
                "When disabled, dimensions are rounded to nearest multiple of 4.\n" +
                "Note: All output dimensions are always multiples of 4 for DXT/BC compression compatibility."));
            EditorGUILayout.HelpBox(
                "Output dimensions are always multiples of 4 for DXT/BC compression compatibility. " +
                "Example: 150x150 becomes 152x152.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            GUIDrawing.DrawSectionHeader("Size Filters");
            EditorGUILayout.PropertyField(props.MinSourceSize, new GUIContent("Min Source Size"));
            EditorGUILayout.PropertyField(props.SkipIfSmallerThan, new GUIContent("Skip If Smaller Than"));

            EditorGUILayout.Space(10);

            GUIDrawing.DrawSectionHeader("Compression Format");
            EditorGUILayout.PropertyField(props.TargetPlatform, new GUIContent("Target Platform"));
            EditorGUILayout.PropertyField(props.UseHighQualityFormatForHighComplexity,
                new GUIContent("High Quality for Complex", "Use BC7/ASTC_4x4 for high complexity textures (uses Complexity Threshold)"));
        }

        public void DrawAllSettings(TextureCompressor compressor, CompressorPropertySet props)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(props.Strategy);

            if (compressor.Strategy == AnalysisStrategyType.Combined)
            {
                EditorGUILayout.PropertyField(props.FastWeight);
                EditorGUILayout.PropertyField(props.HighAccuracyWeight);
                EditorGUILayout.PropertyField(props.PerceptualWeight);
            }

            EditorGUILayout.PropertyField(props.HighComplexityThreshold);
            EditorGUILayout.PropertyField(props.LowComplexityThreshold);
            EditorGUILayout.PropertyField(props.MinDivisor);
            EditorGUILayout.PropertyField(props.MaxDivisor);
            EditorGUILayout.PropertyField(props.MaxResolution);
            EditorGUILayout.PropertyField(props.MinResolution);
            EditorGUILayout.PropertyField(props.ForcePowerOfTwo);
            EditorGUILayout.PropertyField(props.MinSourceSize);
            EditorGUILayout.PropertyField(props.SkipIfSmallerThan);
            EditorGUILayout.PropertyField(props.TargetPlatform);
            EditorGUILayout.PropertyField(props.UseHighQualityFormatForHighComplexity);

            EditorGUI.indentLevel--;
        }

        public void DrawTextureFilters(TextureCompressor compressor)
        {
            GUIDrawing.DrawSectionHeader("Texture Filters");

            GUIDrawing.BeginBox();
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            bool main = GUILayout.Toggle(compressor.ProcessMainTextures, "Main", GUILayout.Width(70));
            bool normal = GUILayout.Toggle(compressor.ProcessNormalMaps, "Normal", GUILayout.Width(70));
            bool emission = GUILayout.Toggle(compressor.ProcessEmissionMaps, "Emission", GUILayout.Width(80));
            bool other = GUILayout.Toggle(compressor.ProcessOtherTextures, "Other", GUILayout.Width(70));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(compressor, "Change Texture Filters");
                compressor.ProcessMainTextures = main;
                compressor.ProcessNormalMaps = normal;
                compressor.ProcessEmissionMaps = emission;
                compressor.ProcessOtherTextures = other;
                EditorUtility.SetDirty(compressor);
            }

            EditorGUILayout.EndHorizontal();
            GUIDrawing.EndBox();
        }

        public void DrawExcludedPathsSection(TextureCompressor compressor)
        {
            GUIDrawing.BeginBox();

            for (int i = compressor.ExcludedPaths.Count - 1; i >= 0; i--)
            {
                string currentPath = compressor.ExcludedPaths[i];

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                string newPath = EditorGUILayout.TextField(currentPath);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(compressor, "Edit Excluded Path");
                    compressor.ExcludedPaths[i] = newPath;
                    EditorUtility.SetDirty(compressor);
                    currentPath = newPath;
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(compressor, "Remove Excluded Path");
                    compressor.ExcludedPaths.RemoveAt(i);
                    EditorUtility.SetDirty(compressor);
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrWhiteSpace(currentPath) && !IsValidAssetPath(currentPath))
                {
                    var savedColor = GUI.color;
                    GUI.color = new Color(1f, 0.7f, 0.3f);
                    EditorGUILayout.LabelField("  Path not found", EditorStyles.miniLabel);
                    GUI.color = savedColor;
                }
            }

            if (GUILayout.Button("+ Add Path..."))
            {
                ShowAddPathMenu(compressor);
            }

            if (compressor.ExcludedPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("Textures with paths starting with listed prefixes will be skipped.", MessageType.None);
            }

            GUIDrawing.EndBox();
        }

        private void ShowAddPathMenu(TextureCompressor compressor)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Empty"), false, () =>
            {
                Undo.RecordObject(compressor, "Add Excluded Path");
                compressor.ExcludedPaths.Add("");
                EditorUtility.SetDirty(compressor);
            });

            if (ExcludedPathPresets.Presets.Length > 0)
            {
                menu.AddSeparator("");

                foreach (var preset in ExcludedPathPresets.Presets)
                {
                    bool alreadyAdded = compressor.ExcludedPaths.Contains(preset.Path);
                    if (alreadyAdded)
                    {
                        menu.AddDisabledItem(new GUIContent($"{preset.Label} (added)"));
                    }
                    else
                    {
                        var presetCopy = preset;
                        menu.AddItem(new GUIContent(preset.Label), false, () =>
                        {
                            Undo.RecordObject(compressor, $"Add {presetCopy.Label} Excluded Path");
                            compressor.ExcludedPaths.Add(presetCopy.Path);
                            EditorUtility.SetDirty(compressor);
                        });
                    }
                }
            }

            menu.ShowAsContext();
        }

        private static bool IsValidAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string folderPath = path.TrimEnd('/', '\\');

            if (AssetDatabase.IsValidFolder(folderPath))
                return true;

            int lastSlash = folderPath.LastIndexOfAny(new[] { '/', '\\' });
            if (lastSlash > 0)
            {
                string parentPath = folderPath.Substring(0, lastSlash);
                if (AssetDatabase.IsValidFolder(parentPath))
                    return true;
            }

            if (path.StartsWith("Packages/"))
            {
                string[] parts = path.Split('/');
                if (parts.Length >= 2)
                {
                    string packagePath = $"Packages/{parts[1]}";
                    string packageJsonPath = $"{packagePath}/package.json";
                    return System.IO.File.Exists(packageJsonPath);
                }
            }

            return false;
        }
    }
}
