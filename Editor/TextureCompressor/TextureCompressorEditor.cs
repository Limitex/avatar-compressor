using System.Collections.Generic;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.texture;
using UnityEngine;
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
        private SerializedProperty _enableLogging;

        private bool _showPreview;
        private TexturePreviewData[] _previewData;
        private int _processedCount;
        private int _skippedCount;

        // Hash of settings when preview was generated (for outdated detection)
        private int _previewSettingsHash;

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
            _enableLogging = serializedObject.FindProperty("EnableLogging");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var compressor = (TextureCompressor)target;

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
            EditorGUILayout.PropertyField(_forcePowerOfTwo, new GUIContent("Force Power of 2"));

            EditorGUILayout.Space(10);

            DrawSectionHeader("Size Filters");
            EditorGUILayout.PropertyField(_minSourceSize, new GUIContent("Min Source Size"));
            EditorGUILayout.PropertyField(_skipIfSmallerThan, new GUIContent("Skip If Smaller Than"));
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
                hash = hash * 31 + config.gameObject.GetInstanceID();
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
            var collector = new TextureCollector(
                config.MinSourceSize,
                config.SkipIfSmallerThan,
                config.ProcessMainTextures,
                config.ProcessNormalMaps,
                config.ProcessEmissionMaps,
                config.ProcessOtherTextures
            );

            var resizer = new TextureResizer(
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

            var allTextures = collector.CollectAll(config.gameObject);

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
                resizer,
                complexityCalc
            );

            var analysisResults = processedTextures.Count > 0
                ? analyzer.AnalyzeBatch(processedTextures)
                : new Dictionary<Texture2D, TextureAnalysisResult>();

            var processedList = new List<TexturePreviewData>();
            var skippedList = new List<TexturePreviewData>();

            foreach (var kvp in allTextures)
            {
                var tex = kvp.Key;
                var info = kvp.Value;

                if (info.IsProcessed && analysisResults.TryGetValue(tex, out var analysis))
                {
                    processedList.Add(new TexturePreviewData
                    {
                        Texture = tex,
                        Path = AssetDatabase.GetAssetPath(tex),
                        Complexity = analysis.NormalizedComplexity,
                        RecommendedDivisor = analysis.RecommendedDivisor,
                        OriginalSize = new Vector2Int(tex.width, tex.height),
                        RecommendedSize = analysis.RecommendedResolution,
                        TextureType = info.TextureType,
                        IsProcessed = true,
                        SkipReason = SkipReason.None
                    });
                }
                else
                {
                    skippedList.Add(new TexturePreviewData
                    {
                        Texture = tex,
                        Path = AssetDatabase.GetAssetPath(tex),
                        Complexity = 0f,
                        RecommendedDivisor = 1,
                        OriginalSize = new Vector2Int(tex.width, tex.height),
                        RecommendedSize = new Vector2Int(tex.width, tex.height),
                        TextureType = info.TextureType,
                        IsProcessed = false,
                        SkipReason = info.SkipReason
                    });
                }
            }

            _processedCount = processedList.Count;
            _skippedCount = skippedList.Count;

            // Combine and sort: processed textures first (sorted by path), then skipped (sorted by path)
            var allPreviewData = new List<TexturePreviewData>(processedList.Count + skippedList.Count);
            processedList.Sort((a, b) => string.Compare(a.Path, b.Path, System.StringComparison.Ordinal));
            skippedList.Sort((a, b) => string.Compare(a.Path, b.Path, System.StringComparison.Ordinal));
            allPreviewData.AddRange(processedList);
            allPreviewData.AddRange(skippedList);
            _previewData = allPreviewData.ToArray();
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space(10);

            DrawSectionHeader($"Preview ({_processedCount} to compress, {_skippedCount} skipped)");

            long totalOriginal = 0;
            long totalAfter = 0;

            foreach (var data in _previewData)
            {
                totalOriginal += (long)data.OriginalSize.x * data.OriginalSize.y * 4;
                totalAfter += (long)data.RecommendedSize.x * data.RecommendedSize.y * 4;
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
            bool hasDrawnSkippedHeader = false;

            foreach (var data in _previewData)
            {
                if (data.IsProcessed && !hasDrawnProcessedHeader)
                {
                    EditorGUILayout.LabelField("Textures to Compress", EditorStyles.boldLabel);
                    hasDrawnProcessedHeader = true;
                }

                if (!data.IsProcessed && !hasDrawnSkippedHeader)
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Skipped Textures", EditorStyles.boldLabel);
                    hasDrawnSkippedHeader = true;
                }

                bool isSkipped = !data.IsProcessed;
                if (isSkipped)
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
                    if (data.RecommendedDivisor > 1)
                    {
                        sizeText = $"{data.OriginalSize.x}x{data.OriginalSize.y} → {data.RecommendedSize.x}x{data.RecommendedSize.y} (÷{data.RecommendedDivisor})";
                    }
                    else
                    {
                        sizeText = $"{data.OriginalSize.x}x{data.OriginalSize.y} (unchanged)";
                    }
                    EditorGUILayout.LabelField(sizeText);
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
                        _ => "Skipped"
                    };
                    EditorGUILayout.LabelField(reasonText, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                if (isSkipped)
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
    }
}
