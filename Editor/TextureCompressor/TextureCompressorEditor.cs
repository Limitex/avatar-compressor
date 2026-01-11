using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.texture;
using nadena.dev.ndmf.runtime;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;

namespace dev.limitex.avatar.compressor.texture.editor
{
    [CustomEditor(typeof(TextureCompressor))]
    public class TextureCompressorEditor : CompressorEditorBase
    {
        private SerializedProperty _preset;
        private SerializedProperty _strategy;
        private SerializedProperty _fastWeight;
        private SerializedProperty _highAccuracyWeight;
        private SerializedProperty _perceptualWeight;
        private SerializedProperty _highComplexityThreshold;
        private SerializedProperty _lowComplexityThreshold;
        private SerializedProperty _minDivisor;
        private SerializedProperty _maxDivisor;
        private SerializedProperty _maxResolution;
        private SerializedProperty _minResolution;
        private SerializedProperty _forcePowerOfTwo;
        private SerializedProperty _processMainTextures;
        private SerializedProperty _processNormalMaps;
        private SerializedProperty _processEmissionMaps;
        private SerializedProperty _processOtherTextures;
        private SerializedProperty _minSourceSize;
        private SerializedProperty _skipIfSmallerThan;
        private SerializedProperty _excludedPaths;
        private SerializedProperty _targetPlatform;
        private SerializedProperty _useHighQualityFormatForHighComplexity;
        private SerializedProperty _enableLogging;
        private SerializedProperty _frozenTextures;

        private bool _showPreview;
        private TexturePreviewData[] _previewData;
        private int _processedCount;
        private int _skippedCount;
        private int _frozenCount;

        // Hash of settings when preview was generated (for outdated detection)
        private int _previewSettingsHash;

        // Frozen textures section state
        private bool _showFrozenSection = true;
        private Vector2 _frozenScrollPosition;

        // Excluded paths section state (collapsed by default)
        private bool _showExcludedPathsSection = false;

        private static readonly Color HighQualityColor = new Color(0.1f, 0.9f, 0.6f);
        private static readonly Color QualityColor = new Color(0.2f, 0.8f, 0.4f);
        private static readonly Color BalancedColor = new Color(0.3f, 0.6f, 0.9f);
        private static readonly Color AggressiveColor = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color MaximumColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color CustomColor = new Color(0.7f, 0.5f, 0.9f);

        private struct TexturePreviewData
        {
            public Texture2D Texture;
            public string Path;
            public float Complexity;
            public int RecommendedDivisor;
            public Vector2Int OriginalSize;
            public Vector2Int RecommendedSize;
            public string TextureType;
            public bool IsProcessed;
            public SkipReason SkipReason;
            public long OriginalMemory;
            public long EstimatedMemory;
            public bool IsNormalMap;
            public TextureFormat? PredictedFormat;
            public bool HasAlpha;
            public bool IsFrozen;
            public FrozenTextureSettings FrozenSettings;
        }

        private void OnEnable()
        {
            _preset = serializedObject.FindProperty("Preset");
            _strategy = serializedObject.FindProperty("Strategy");
            _fastWeight = serializedObject.FindProperty("FastWeight");
            _highAccuracyWeight = serializedObject.FindProperty("HighAccuracyWeight");
            _perceptualWeight = serializedObject.FindProperty("PerceptualWeight");
            _highComplexityThreshold = serializedObject.FindProperty("HighComplexityThreshold");
            _lowComplexityThreshold = serializedObject.FindProperty("LowComplexityThreshold");
            _minDivisor = serializedObject.FindProperty("MinDivisor");
            _maxDivisor = serializedObject.FindProperty("MaxDivisor");
            _maxResolution = serializedObject.FindProperty("MaxResolution");
            _minResolution = serializedObject.FindProperty("MinResolution");
            _forcePowerOfTwo = serializedObject.FindProperty("ForcePowerOfTwo");
            _processMainTextures = serializedObject.FindProperty("ProcessMainTextures");
            _processNormalMaps = serializedObject.FindProperty("ProcessNormalMaps");
            _processEmissionMaps = serializedObject.FindProperty("ProcessEmissionMaps");
            _processOtherTextures = serializedObject.FindProperty("ProcessOtherTextures");
            _minSourceSize = serializedObject.FindProperty("MinSourceSize");
            _skipIfSmallerThan = serializedObject.FindProperty("SkipIfSmallerThan");
            _excludedPaths = serializedObject.FindProperty("ExcludedPaths");
            _targetPlatform = serializedObject.FindProperty("TargetPlatform");
            _useHighQualityFormatForHighComplexity = serializedObject.FindProperty("UseHighQualityFormatForHighComplexity");
            _enableLogging = serializedObject.FindProperty("EnableLogging");
            _frozenTextures = serializedObject.FindProperty("FrozenTextures");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var compressor = (TextureCompressor)target;

            // Check if component is on avatar root
            if (!RuntimeUtil.IsAvatarRoot(compressor.transform))
            {
                DrawHelpBox(
                    "This component should be placed on the avatar root GameObject. " +
                    "While it will still work, placing it on the avatar root is recommended.",
                    MessageType.Warning);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.Space(5);

            DrawPresetSection(compressor);
            EditorGUILayout.Space(10);
            DrawPresetDescription(compressor.Preset);
            EditorGUILayout.Space(10);

            if (compressor.Preset == CompressorPreset.Custom)
            {
                DrawCustomSettings(compressor);
            }
            else
            {
                DrawPresetSummary(compressor);
                EditorGUILayout.Space(5);

                _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings (Read Only)", true);
                if (_showAdvancedSettings)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    DrawAllSettings(compressor);
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.Space(10);
            DrawTextureFilters(compressor);
            EditorGUILayout.Space(10);

            EditorGUILayout.PropertyField(_enableLogging, new GUIContent("Enable Logging"));

            EditorGUILayout.Space(10);

            // Excluded paths section (collapsed by default for troubleshooting)
            int excludedCount = compressor.ExcludedPaths.Count;
            string excludedLabel = excludedCount > 0
                ? $"Path Exclusions ({excludedCount})"
                : "Path Exclusions";
            _showExcludedPathsSection = EditorGUILayout.Foldout(_showExcludedPathsSection, excludedLabel, true);
            if (_showExcludedPathsSection)
            {
                DrawExcludedPathsSection(compressor);
            }

            EditorGUILayout.Space(15);
            DrawFrozenTexturesSection(compressor);

            EditorGUILayout.Space(15);
            DrawPreviewSection(compressor);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPresetSection(TextureCompressor compressor)
        {
            DrawSectionHeader("Compression Preset");

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

            if (DrawColoredButton(label, tooltip, color, isSelected))
            {
                Undo.RecordObject(compressor, "Change Compressor Preset");
                compressor.ApplyPreset(preset);
                EditorUtility.SetDirty(compressor);
            }
        }

        private void DrawPresetDescription(CompressorPreset preset)
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

            DrawHelpBox(description, messageType);
        }

        private void DrawPresetSummary(TextureCompressor compressor)
        {
            BeginBox();
            DrawSectionHeader("Current Settings Summary");

            EditorGUILayout.LabelField($"Strategy: {compressor.Strategy}");
            EditorGUILayout.LabelField($"Divisor Range: {compressor.MinDivisor}x - {compressor.MaxDivisor}x");
            EditorGUILayout.LabelField($"Resolution Range: {compressor.MinResolution}px - {compressor.MaxResolution}px");
            EditorGUILayout.LabelField($"Complexity Thresholds: {compressor.LowComplexityThreshold:P0} - {compressor.HighComplexityThreshold:P0}");

            EndBox();
        }

        private void DrawCustomSettings(TextureCompressor compressor)
        {
            DrawSectionHeader("Analysis Strategy");
            EditorGUILayout.PropertyField(_strategy);

            if (compressor.Strategy == AnalysisStrategyType.Combined)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_fastWeight, new GUIContent("Fast Weight"));
                EditorGUILayout.PropertyField(_highAccuracyWeight, new GUIContent("High Accuracy Weight"));
                EditorGUILayout.PropertyField(_perceptualWeight, new GUIContent("Perceptual Weight"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(10);

            DrawSectionHeader("Complexity Thresholds");
            EditorGUILayout.PropertyField(_highComplexityThreshold, new GUIContent("High (Keep Detail)"));
            EditorGUILayout.PropertyField(_lowComplexityThreshold, new GUIContent("Low (Compress More)"));

            EditorGUILayout.Space(10);

            DrawSectionHeader("Resolution Settings");
            EditorGUILayout.PropertyField(_minDivisor, new GUIContent("Min Divisor"));
            EditorGUILayout.PropertyField(_maxDivisor, new GUIContent("Max Divisor"));
            EditorGUILayout.PropertyField(_maxResolution, new GUIContent("Max Resolution"));
            EditorGUILayout.PropertyField(_minResolution, new GUIContent("Min Resolution"));
            EditorGUILayout.PropertyField(_forcePowerOfTwo, new GUIContent("Force Power of 2",
                "When enabled, dimensions are rounded to nearest power of 2.\n" +
                "When disabled, dimensions are rounded to nearest multiple of 4.\n" +
                "Note: All output dimensions are always multiples of 4 for DXT/BC compression compatibility."));
            EditorGUILayout.HelpBox(
                "Output dimensions are always multiples of 4 for DXT/BC compression compatibility. " +
                "Example: 150x150 becomes 152x152.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            DrawSectionHeader("Size Filters");
            EditorGUILayout.PropertyField(_minSourceSize, new GUIContent("Min Source Size"));
            EditorGUILayout.PropertyField(_skipIfSmallerThan, new GUIContent("Skip If Smaller Than"));

            EditorGUILayout.Space(10);

            DrawSectionHeader("Compression Format");
            EditorGUILayout.PropertyField(_targetPlatform, new GUIContent("Target Platform"));
            EditorGUILayout.PropertyField(_useHighQualityFormatForHighComplexity,
                new GUIContent("High Quality for Complex", "Use BC7/ASTC_4x4 for high complexity textures (uses Complexity Threshold)"));
        }

        private void DrawAllSettings(TextureCompressor compressor)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_strategy);

            if (compressor.Strategy == AnalysisStrategyType.Combined)
            {
                EditorGUILayout.PropertyField(_fastWeight);
                EditorGUILayout.PropertyField(_highAccuracyWeight);
                EditorGUILayout.PropertyField(_perceptualWeight);
            }

            EditorGUILayout.PropertyField(_highComplexityThreshold);
            EditorGUILayout.PropertyField(_lowComplexityThreshold);
            EditorGUILayout.PropertyField(_minDivisor);
            EditorGUILayout.PropertyField(_maxDivisor);
            EditorGUILayout.PropertyField(_maxResolution);
            EditorGUILayout.PropertyField(_minResolution);
            EditorGUILayout.PropertyField(_forcePowerOfTwo);
            EditorGUILayout.PropertyField(_minSourceSize);
            EditorGUILayout.PropertyField(_skipIfSmallerThan);
            EditorGUILayout.PropertyField(_targetPlatform);
            EditorGUILayout.PropertyField(_useHighQualityFormatForHighComplexity);

            EditorGUI.indentLevel--;
        }

        private void DrawTextureFilters(TextureCompressor compressor)
        {
            DrawSectionHeader("Texture Filters");

            BeginBox();
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
            EndBox();
        }

        private void DrawExcludedPathsSection(TextureCompressor compressor)
        {
            BeginBox();

            // Display existing paths
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

                // Show warning if path doesn't exist
                if (!string.IsNullOrWhiteSpace(currentPath) && !IsValidAssetPath(currentPath))
                {
                    var savedColor = GUI.color;
                    GUI.color = new Color(1f, 0.7f, 0.3f);
                    EditorGUILayout.LabelField("  ⚠ Path not found", EditorStyles.miniLabel);
                    GUI.color = savedColor;
                }
            }

            // Add new path button with dropdown for presets
            if (GUILayout.Button("+ Add Path..."))
            {
                ShowAddPathMenu(compressor);
            }

            if (compressor.ExcludedPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("Textures with paths starting with listed prefixes will be skipped.", MessageType.None);
            }

            EndBox();
        }

        private void ShowAddPathMenu(TextureCompressor compressor)
        {
            var menu = new GenericMenu();

            // Empty path option
            menu.AddItem(new GUIContent("Empty"), false, () =>
            {
                Undo.RecordObject(compressor, "Add Excluded Path");
                compressor.ExcludedPaths.Add("");
                EditorUtility.SetDirty(compressor);
            });

            // Separator and presets
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

        private void DrawFrozenTexturesSection(TextureCompressor compressor)
        {
            int frozenCount = compressor.FrozenTextures.Count;
            _showFrozenSection = EditorGUILayout.Foldout(_showFrozenSection,
                $"Frozen Textures ({frozenCount})", true);

            if (!_showFrozenSection) return;

            if (frozenCount == 0)
            {
                DrawHelpBox("No frozen textures. Click 'Freeze' on textures in Preview to add manual overrides.", MessageType.Info);
                return;
            }

            // Only use ScrollView when there are many items (3+)
            bool useScrollView = frozenCount >= 3;

            if (useScrollView)
            {
                _frozenScrollPosition = EditorGUILayout.BeginScrollView(_frozenScrollPosition, GUILayout.MaxHeight(250));
            }

            for (int i = compressor.FrozenTextures.Count - 1; i >= 0; i--)
            {
                var frozen = compressor.FrozenTextures[i];
                DrawFrozenTextureEntry(compressor, frozen, i);
            }

            if (useScrollView)
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawFrozenTextureEntry(TextureCompressor compressor, FrozenTextureSettings frozen, int index)
        {
            // Track if we need to remove this entry after drawing
            bool shouldRemove = false;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Thumbnail
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(frozen.TexturePath);
            var preview = texture != null ? AssetPreview.GetAssetPreview(texture) : null;
            GUILayout.Label(preview ?? Texture2D.whiteTexture, GUILayout.Width(40), GUILayout.Height(40));

            EditorGUILayout.BeginVertical();

            // Header row with texture name and unfreeze button
            EditorGUILayout.BeginHorizontal();

            string textureName = System.IO.Path.GetFileName(frozen.TexturePath);
            EditorGUILayout.LabelField(textureName, EditorStyles.boldLabel);

            if (GUILayout.Button("Unfreeze", GUILayout.Width(70)))
            {
                shouldRemove = true;
            }

            EditorGUILayout.EndHorizontal();

            // Warning if texture asset is missing
            if (texture == null)
            {
                var savedColor = GUI.color;
                GUI.color = new Color(1f, 0.7f, 0.3f);
                EditorGUILayout.LabelField("Texture not found", EditorStyles.miniLabel);
                GUI.color = savedColor;
            }

            // Skip checkbox
            EditorGUI.BeginChangeCheck();
            bool skip = EditorGUILayout.Toggle("Skip compression", frozen.Skip);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(compressor, "Change Frozen Skip");
                frozen.Skip = skip;
                EditorUtility.SetDirty(compressor);
            }

            // Disable divisor and format controls when skip is enabled
            EditorGUI.BeginDisabledGroup(frozen.Skip);

            // Divisor selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Divisor:", GUILayout.Width(60));

            int[] divisors = { 1, 2, 4, 8, 16 };
            foreach (int div in divisors)
            {
                bool isSelected = frozen.Divisor == div;
                var style = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;

                EditorGUI.BeginChangeCheck();
                if (GUILayout.Toggle(isSelected, div.ToString(), style, GUILayout.Width(35)))
                {
                    if (!isSelected)
                    {
                        Undo.RecordObject(compressor, "Change Frozen Divisor");
                        frozen.Divisor = div;
                        EditorUtility.SetDirty(compressor);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            // Format selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Format:", GUILayout.Width(60));

            EditorGUI.BeginChangeCheck();
            var newFormat = (FrozenTextureFormat)EditorGUILayout.EnumPopup(frozen.Format);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(compressor, "Change Frozen Format");
                frozen.Format = newFormat;
                EditorUtility.SetDirty(compressor);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // Handle removal after layout is complete
            if (shouldRemove)
            {
                Undo.RecordObject(compressor, "Unfreeze Texture");
                compressor.FrozenTextures.RemoveAt(index);
                EditorUtility.SetDirty(compressor);
            }
        }

        private void DrawPreviewSection(TextureCompressor compressor)
        {
            bool isOutdated = IsPreviewOutdated(compressor);

            if (GUILayout.Button("Preview Compression Results", GUILayout.Height(35)))
            {
                GeneratePreview(compressor);
                _showPreview = true;
            }

            if (_showPreview && _previewData != null && _previewData.Length > 0)
            {
                if (isOutdated)
                {
                    DrawHelpBox("Preview is outdated. Settings or target object have changed since the preview was generated. Click 'Preview Compression Results' to refresh.", MessageType.Warning);
                }
                DrawPreview();
            }
            else if (_showPreview && (_previewData == null || _previewData.Length == 0))
            {
                DrawHelpBox("No textures found matching the current filter settings.", MessageType.Info);

                if (GUILayout.Button("Close"))
                {
                    _showPreview = false;
                }
            }
        }

        private int ComputeSettingsHash(TextureCompressor config)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + config.Preset.GetHashCode();
                hash = hash * 31 + config.Strategy.GetHashCode();
                hash = hash * 31 + config.FastWeight.GetHashCode();
                hash = hash * 31 + config.HighAccuracyWeight.GetHashCode();
                hash = hash * 31 + config.PerceptualWeight.GetHashCode();
                hash = hash * 31 + config.HighComplexityThreshold.GetHashCode();
                hash = hash * 31 + config.LowComplexityThreshold.GetHashCode();
                hash = hash * 31 + config.MinDivisor;
                hash = hash * 31 + config.MaxDivisor;
                hash = hash * 31 + config.MaxResolution;
                hash = hash * 31 + config.MinResolution;
                hash = hash * 31 + config.ForcePowerOfTwo.GetHashCode();
                hash = hash * 31 + config.ProcessMainTextures.GetHashCode();
                hash = hash * 31 + config.ProcessNormalMaps.GetHashCode();
                hash = hash * 31 + config.ProcessEmissionMaps.GetHashCode();
                hash = hash * 31 + config.ProcessOtherTextures.GetHashCode();
                hash = hash * 31 + config.MinSourceSize;
                hash = hash * 31 + config.SkipIfSmallerThan;
                hash = hash * 31 + config.ExcludedPaths.Count;
                foreach (var path in config.ExcludedPaths)
                {
                    hash = hash * 31 + (path?.GetHashCode() ?? 0);
                }
                hash = hash * 31 + config.TargetPlatform.GetHashCode();
                hash = hash * 31 + config.UseHighQualityFormatForHighComplexity.GetHashCode();
                hash = hash * 31 + config.gameObject.GetInstanceID();

                // Include frozen textures in hash
                foreach (var frozen in config.FrozenTextures)
                {
                    hash = hash * 31 + (frozen.TexturePath?.GetHashCode() ?? 0);
                    hash = hash * 31 + frozen.Divisor;
                    hash = hash * 31 + frozen.Format.GetHashCode();
                    hash = hash * 31 + frozen.Skip.GetHashCode();
                }

                return hash;
            }
        }

        private bool IsPreviewOutdated(TextureCompressor config)
        {
            if (!_showPreview || _previewData == null)
                return false;

            return ComputeSettingsHash(config) != _previewSettingsHash;
        }

        private void GeneratePreview(TextureCompressor config)
        {
            _previewSettingsHash = ComputeSettingsHash(config);

            // Build frozen texture lookup
            var frozenLookup = new Dictionary<string, FrozenTextureSettings>();
            foreach (var frozen in config.FrozenTextures)
            {
                if (!string.IsNullOrEmpty(frozen.TexturePath))
                    frozenLookup[frozen.TexturePath] = frozen;
            }

            var collector = new TextureCollector(
                config.MinSourceSize,
                config.SkipIfSmallerThan,
                config.ProcessMainTextures,
                config.ProcessNormalMaps,
                config.ProcessEmissionMaps,
                config.ProcessOtherTextures,
                config.ExcludedPaths
            );

            var processor = new TextureProcessor(
                config.MinResolution,
                config.MaxResolution,
                config.ForcePowerOfTwo
            );

            var complexityCalc = new ComplexityCalculator(
                config.HighComplexityThreshold,
                config.LowComplexityThreshold,
                config.MinDivisor,
                config.MaxDivisor
            );

            var formatSelector = new TextureFormatSelector(
                config.TargetPlatform,
                config.UseHighQualityFormatForHighComplexity,
                config.HighComplexityThreshold
            );

            var allTextures = collector.CollectAll(config.gameObject);

            // Collect additional materials from animations and components using MaterialCollector
            var additionalMaterialRefs = new List<MaterialReference>();
            additionalMaterialRefs.AddRange(MaterialCollector.CollectFromAnimator(config.gameObject));
            additionalMaterialRefs.AddRange(MaterialCollector.CollectFromComponents(config.gameObject));

            var additionalMaterials = MaterialCollector.GetDistinctMaterials(additionalMaterialRefs);
            if (additionalMaterials.Any())
            {
                // Use collectAll: true to include skipped textures in preview
                collector.CollectFromMaterials(additionalMaterials, allTextures, collectAll: true);
            }

            if (allTextures.Count == 0)
            {
                _previewData = new TexturePreviewData[0];
                return;
            }

            var processedTextures = new Dictionary<Texture2D, TextureInfo>();
            foreach (var kvp in allTextures)
            {
                if (kvp.Value.IsProcessed)
                {
                    processedTextures[kvp.Key] = kvp.Value;
                }
            }

            var analyzer = new TextureAnalyzer(
                config.Strategy,
                config.FastWeight,
                config.HighAccuracyWeight,
                config.PerceptualWeight,
                processor,
                complexityCalc
            );

            var analysisResults = processedTextures.Count > 0
                ? analyzer.AnalyzeBatch(processedTextures)
                : new Dictionary<Texture2D, TextureAnalysisResult>();

            var processedList = new List<TexturePreviewData>();
            var frozenList = new List<TexturePreviewData>();
            var skippedList = new List<TexturePreviewData>();

            foreach (var kvp in allTextures)
            {
                var tex = kvp.Key;
                var info = kvp.Value;
                string assetPath = AssetDatabase.GetAssetPath(tex);

                // Check if texture is frozen
                frozenLookup.TryGetValue(assetPath, out var frozenSettings);
                bool isFrozen = frozenSettings != null;

                if (info.IsProcessed && analysisResults.TryGetValue(tex, out var analysis))
                {
                    long originalMemory = Profiler.GetRuntimeMemorySizeLong(tex);
                    bool isNormalMap = info.TextureType == "Normal";
                    bool hasAlpha = TextureFormatSelector.HasSignificantAlpha(tex);

                    // For frozen textures, use frozen settings for preview
                    int divisor;
                    Vector2Int recommendedSize;
                    TextureFormat targetFormat;

                    if (isFrozen && !frozenSettings.Skip)
                    {
                        divisor = frozenSettings.Divisor;
                        recommendedSize = processor.CalculateNewDimensions(tex.width, tex.height, divisor);

                        if (frozenSettings.Format != FrozenTextureFormat.Auto)
                        {
                            targetFormat = TextureFormatSelector.ConvertFrozenFormat(frozenSettings.Format);
                        }
                        else if (TextureFormatSelector.IsCompressedFormat(tex.format))
                        {
                            // Preserve original compressed format (matches CompressTexture behavior)
                            targetFormat = tex.format;
                        }
                        else
                        {
                            targetFormat = formatSelector.PredictFormat(isNormalMap, 0.5f, hasAlpha);
                        }
                    }
                    else
                    {
                        divisor = analysis.RecommendedDivisor;
                        recommendedSize = analysis.RecommendedResolution;

                        if (TextureFormatSelector.IsCompressedFormat(tex.format))
                        {
                            // Preserve original compressed format (matches CompressTexture behavior)
                            targetFormat = tex.format;
                        }
                        else
                        {
                            targetFormat = formatSelector.PredictFormat(isNormalMap, analysis.NormalizedComplexity, hasAlpha);
                        }
                    }

                    long estimatedMemory = EstimateCompressedMemory(
                        recommendedSize.x,
                        recommendedSize.y,
                        targetFormat);

                    var previewData = new TexturePreviewData
                    {
                        Texture = tex,
                        Path = assetPath,
                        Complexity = analysis.NormalizedComplexity,
                        RecommendedDivisor = divisor,
                        OriginalSize = new Vector2Int(tex.width, tex.height),
                        RecommendedSize = recommendedSize,
                        TextureType = info.TextureType,
                        IsProcessed = true,
                        SkipReason = SkipReason.None,
                        OriginalMemory = originalMemory,
                        EstimatedMemory = estimatedMemory,
                        IsNormalMap = isNormalMap,
                        PredictedFormat = targetFormat,
                        HasAlpha = hasAlpha,
                        IsFrozen = isFrozen && !frozenSettings.Skip,
                        FrozenSettings = frozenSettings
                    };

                    if (isFrozen && frozenSettings.Skip)
                    {
                        // Frozen with Skip=true - add to skipped list
                        skippedList.Add(new TexturePreviewData
                        {
                            Texture = tex,
                            Path = assetPath,
                            Complexity = 0f,
                            RecommendedDivisor = 1,
                            OriginalSize = new Vector2Int(tex.width, tex.height),
                            RecommendedSize = new Vector2Int(tex.width, tex.height),
                            TextureType = info.TextureType,
                            IsProcessed = false,
                            SkipReason = SkipReason.FrozenSkip,
                            OriginalMemory = originalMemory,
                            EstimatedMemory = originalMemory,
                            IsNormalMap = isNormalMap,
                            PredictedFormat = null,
                            HasAlpha = false,
                            IsFrozen = true,
                            FrozenSettings = frozenSettings
                        });
                    }
                    else if (isFrozen)
                    {
                        frozenList.Add(previewData);
                    }
                    else
                    {
                        processedList.Add(previewData);
                    }
                }
                else
                {
                    long originalMemory = Profiler.GetRuntimeMemorySizeLong(tex);

                    skippedList.Add(new TexturePreviewData
                    {
                        Texture = tex,
                        Path = assetPath,
                        Complexity = 0f,
                        RecommendedDivisor = 1,
                        OriginalSize = new Vector2Int(tex.width, tex.height),
                        RecommendedSize = new Vector2Int(tex.width, tex.height),
                        TextureType = info.TextureType,
                        IsProcessed = false,
                        SkipReason = info.SkipReason,
                        OriginalMemory = originalMemory,
                        EstimatedMemory = originalMemory,
                        IsNormalMap = info.TextureType == "Normal",
                        PredictedFormat = null,
                        HasAlpha = false,
                        IsFrozen = isFrozen && frozenSettings != null && frozenSettings.Skip,
                        FrozenSettings = frozenSettings
                    });
                }
            }

            _processedCount = processedList.Count;
            _frozenCount = frozenList.Count;
            _skippedCount = skippedList.Count;

            // Combine and sort: processed first, then frozen, then skipped (each sorted by path)
            var allPreviewData = new List<TexturePreviewData>(processedList.Count + frozenList.Count + skippedList.Count);
            processedList.Sort((a, b) => string.Compare(a.Path, b.Path, System.StringComparison.Ordinal));
            frozenList.Sort((a, b) => string.Compare(a.Path, b.Path, System.StringComparison.Ordinal));
            skippedList.Sort((a, b) => string.Compare(a.Path, b.Path, System.StringComparison.Ordinal));
            allPreviewData.AddRange(processedList);
            allPreviewData.AddRange(frozenList);
            allPreviewData.AddRange(skippedList);
            _previewData = allPreviewData.ToArray();
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space(10);

            string frozenInfo = _frozenCount > 0 ? $", {_frozenCount} frozen" : "";
            DrawSectionHeader($"Preview ({_processedCount} to compress{frozenInfo}, {_skippedCount} skipped)");

            long totalOriginal = 0;
            long totalAfter = 0;

            foreach (var data in _previewData)
            {
                totalOriginal += data.OriginalMemory;
                totalAfter += data.EstimatedMemory;
            }

            float savings = totalOriginal > 0 ? 1f - (float)totalAfter / totalOriginal : 0f;

            BeginBox();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Original:", GUILayout.Width(60));
            EditorGUILayout.LabelField(FormatBytes(totalOriginal), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("After:", GUILayout.Width(60));
            EditorGUILayout.LabelField(FormatBytes(totalAfter), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Savings:", GUILayout.Width(60));
            Color originalColor = GUI.color;
            GUI.color = Color.green;
            long savedBytes = totalOriginal - totalAfter;
            EditorGUILayout.LabelField($"{savings:P0} (-{FormatBytes(savedBytes)})", EditorStyles.boldLabel);
            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();

            EndBox();

            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(300));

            bool hasDrawnProcessedHeader = false;
            bool hasDrawnFrozenHeader = false;
            bool hasDrawnSkippedHeader = false;

            var compressor = (TextureCompressor)target;

            foreach (var data in _previewData)
            {
                // Section headers
                if (data.IsProcessed && !data.IsFrozen && !hasDrawnProcessedHeader)
                {
                    EditorGUILayout.LabelField("Textures to Compress", EditorStyles.boldLabel);
                    hasDrawnProcessedHeader = true;
                }

                if (data.IsProcessed && data.IsFrozen && !hasDrawnFrozenHeader)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Frozen Textures (Manual Override)", EditorStyles.boldLabel);
                    hasDrawnFrozenHeader = true;
                }

                if (!data.IsProcessed && !hasDrawnSkippedHeader)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Skipped Textures", EditorStyles.boldLabel);
                    hasDrawnSkippedHeader = true;
                }

                bool isSkipped = !data.IsProcessed;
                // Check if texture was frozen after preview was generated (real-time check)
                bool isFrozenNow = !data.IsFrozen && compressor.IsFrozen(data.Path);
                if (isSkipped || isFrozenNow)
                {
                    EditorGUI.BeginDisabledGroup(true);
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                var preview = AssetPreview.GetAssetPreview(data.Texture);
                GUILayout.Label(preview ?? Texture2D.whiteTexture, GUILayout.Width(40), GUILayout.Height(40));

                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(data.Texture.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"[{data.TextureType}]", GUILayout.Width(60));

                // Freeze/Frozen button for processed textures
                if (data.IsProcessed)
                {
                    if (data.IsFrozen)
                    {
                        // Show Unfreeze button for frozen textures
                        var savedColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                        if (GUILayout.Button("Unfreeze", GUILayout.Width(70)))
                        {
                            Undo.RecordObject(compressor, "Unfreeze Texture");
                            compressor.UnfreezeTexture(data.Path);
                            EditorUtility.SetDirty(compressor);
                        }
                        GUI.backgroundColor = savedColor;
                    }
                    else
                    {
                        // Show Freeze button
                        if (GUILayout.Button("Freeze", GUILayout.Width(70)))
                        {
                            Undo.RecordObject(compressor, "Freeze Texture");
                            var frozenSettings = new FrozenTextureSettings(data.Path, data.RecommendedDivisor, FrozenTextureFormat.Auto, false);
                            compressor.SetFrozenSettings(data.Path, frozenSettings);
                            EditorUtility.SetDirty(compressor);
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (data.IsProcessed)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Complexity:", GUILayout.Width(70));

                    Color complexityColor = Color.Lerp(Color.green, Color.red, data.Complexity);
                    DrawProgressBar(data.Complexity, 100, 16, complexityColor);

                    EditorGUILayout.LabelField($"{data.Complexity:P0}", GUILayout.Width(45));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Size:", GUILayout.Width(70));

                    string sizeText;
                    string manualIndicator = data.IsFrozen ? " (manual)" : "";
                    if (data.RecommendedDivisor > 1)
                    {
                        sizeText = $"{data.OriginalSize.x}x{data.OriginalSize.y} → {data.RecommendedSize.x}x{data.RecommendedSize.y} (÷{data.RecommendedDivisor}){manualIndicator}";
                    }
                    else
                    {
                        sizeText = $"{data.OriginalSize.x}x{data.OriginalSize.y} (unchanged){manualIndicator}";
                    }
                    EditorGUILayout.LabelField(sizeText);
                    EditorGUILayout.EndHorizontal();

                    // Display predicted compression format
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Format:", GUILayout.Width(70));
                    if (data.PredictedFormat.HasValue)
                    {
                        string formatName = GetFormatDisplayName(data.PredictedFormat.Value);
                        string formatInfo = GetFormatInfo(data.PredictedFormat.Value);
                        if (data.IsFrozen && data.FrozenSettings != null && data.FrozenSettings.Format != FrozenTextureFormat.Auto)
                        {
                            formatInfo += " (manual)";
                        }
                        var formatColor = GetFormatColor(data.PredictedFormat.Value);

                        var savedGuiColor = GUI.color;
                        GUI.color = formatColor;
                        EditorGUILayout.LabelField(formatName, EditorStyles.boldLabel, GUILayout.Width(70));
                        GUI.color = savedGuiColor;
                        EditorGUILayout.LabelField(formatInfo, EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("N/A", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Size:", GUILayout.Width(70));
                    EditorGUILayout.LabelField($"{data.OriginalSize.x}x{data.OriginalSize.y}");
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Reason:", GUILayout.Width(70));
                    string reasonText = data.SkipReason switch
                    {
                        SkipReason.TooSmall => "Too small",
                        SkipReason.FilteredByType => "Filtered by type",
                        SkipReason.FrozenSkip => "User frozen (skipped)",
                        SkipReason.RuntimeGenerated => "Runtime generated",
                        SkipReason.ExcludedPath => "Excluded by path",
                        _ => "Skipped"
                    };
                    EditorGUILayout.LabelField(reasonText, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                if (isSkipped || isFrozenNow)
                {
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Close Preview"))
            {
                _showPreview = false;
                _previewData = null;
            }
        }

        /// <summary>
        /// Estimates compressed memory size based on target format.
        /// </summary>
        private long EstimateCompressedMemory(int width, int height, TextureFormat format)
        {
            float bitsPerPixel = GetBitsPerPixel(format);
            return (long)(width * height * bitsPerPixel / 8f);
        }

        /// <summary>
        /// Returns bits per pixel for the given texture format.
        /// </summary>
        private float GetBitsPerPixel(TextureFormat format)
        {
            switch (format)
            {
                // DXT/BC formats (Desktop)
                case TextureFormat.DXT1:
                case TextureFormat.DXT1Crunched:
                    return 4f;
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC5:
                case TextureFormat.BC7:
                    return 8f;
                case TextureFormat.BC4:
                    return 4f;
                case TextureFormat.BC6H:
                    return 8f;

                // ASTC formats (Mobile)
                case TextureFormat.ASTC_4x4:
                    return 8f;
                case TextureFormat.ASTC_5x5:
                    return 5.12f;
                case TextureFormat.ASTC_6x6:
                    return 3.56f;
                case TextureFormat.ASTC_8x8:
                    return 2f;
                case TextureFormat.ASTC_10x10:
                    return 1.28f;
                case TextureFormat.ASTC_12x12:
                    return 0.89f;

                // Uncompressed formats
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                    return 32f;
                case TextureFormat.RGB24:
                    return 24f;
                case TextureFormat.RGB565:
                case TextureFormat.RGBA4444:
                case TextureFormat.ARGB4444:
                    return 16f;

                default:
                    return 32f; // Assume uncompressed RGBA
            }
        }

        /// <summary>
        /// Gets a user-friendly display name for a texture format.
        /// </summary>
        private static string GetFormatDisplayName(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.DXT1 => "DXT1",
                TextureFormat.DXT5 => "DXT5",
                TextureFormat.BC5 => "BC5",
                TextureFormat.BC7 => "BC7",
                TextureFormat.ASTC_4x4 => "ASTC 4x4",
                TextureFormat.ASTC_6x6 => "ASTC 6x6",
                TextureFormat.ASTC_8x8 => "ASTC 8x8",
                _ => format.ToString()
            };
        }

        /// <summary>
        /// Gets additional info about a texture format (bpp, quality, use case).
        /// </summary>
        private static string GetFormatInfo(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.DXT1 => "4 bpp, RGB only, fastest",
                TextureFormat.DXT5 => "8 bpp, RGBA, good quality",
                TextureFormat.BC5 => "8 bpp, normal maps",
                TextureFormat.BC7 => "8 bpp, highest quality",
                TextureFormat.ASTC_4x4 => "8 bpp, highest quality",
                TextureFormat.ASTC_6x6 => "3.56 bpp, balanced",
                TextureFormat.ASTC_8x8 => "2 bpp, most efficient",
                _ => ""
            };
        }

        /// <summary>
        /// Gets a color to represent the format quality/efficiency.
        /// </summary>
        private static Color GetFormatColor(TextureFormat format)
        {
            return format switch
            {
                // High quality formats - green
                TextureFormat.BC7 => new Color(0.2f, 0.8f, 0.4f),
                TextureFormat.ASTC_4x4 => new Color(0.2f, 0.8f, 0.4f),

                // Normal map formats - cyan
                TextureFormat.BC5 => new Color(0.2f, 0.7f, 0.9f),

                // Balanced formats - yellow
                TextureFormat.DXT5 => new Color(0.9f, 0.8f, 0.2f),
                TextureFormat.ASTC_6x6 => new Color(0.9f, 0.8f, 0.2f),

                // Efficient formats - orange
                TextureFormat.DXT1 => new Color(0.9f, 0.6f, 0.2f),
                TextureFormat.ASTC_8x8 => new Color(0.9f, 0.6f, 0.2f),

                _ => Color.white
            };
        }

        /// <summary>
        /// Checks if the given path prefix exists as a valid folder in the project.
        /// </summary>
        private static bool IsValidAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Remove trailing slash for folder check
            string folderPath = path.TrimEnd('/', '\\');

            // Check if it's a valid folder
            if (AssetDatabase.IsValidFolder(folderPath))
                return true;

            // Check if parent folder exists (for partial paths like "Assets/Textures/")
            int lastSlash = folderPath.LastIndexOfAny(new[] { '/', '\\' });
            if (lastSlash > 0)
            {
                string parentPath = folderPath.Substring(0, lastSlash);
                if (AssetDatabase.IsValidFolder(parentPath))
                    return true;
            }

            // Check Packages folder specially (may not appear as regular folder)
            if (path.StartsWith("Packages/"))
            {
                // Extract package name (e.g., "com.vrcfury.temp" from "Packages/com.vrcfury.temp/")
                string[] parts = path.Split('/');
                if (parts.Length >= 2)
                {
                    string packagePath = $"Packages/{parts[1]}";
                    // Check if package.json exists in the package
                    string packageJsonPath = $"{packagePath}/package.json";
                    return System.IO.File.Exists(packageJsonPath);
                }
            }

            return false;
        }

    }
}
