using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.ui;
using dev.limitex.avatar.compressor.texture;
using UnityEngine;
using UnityEditor;

namespace dev.limitex.avatar.compressor.texture.ui
{
    /// <summary>
    /// Draws the preview section with texture analysis results.
    /// </summary>
    public class PreviewSection
    {
        private readonly PreviewGenerator _generator = new PreviewGenerator();
        private TexturePreviewData[] _previewData;
        private int _previewSettingsHash;
        private bool _showPreview;
        private Vector2 _scrollPosition;

        /// <summary>
        /// Draws the preview section.
        /// </summary>
        public void Draw(TextureCompressor config, SearchBoxControl search)
        {
            bool isOutdated = IsPreviewOutdated(config);

            if (GUILayout.Button("Preview Compression Results", GUILayout.Height(35)))
            {
                GeneratePreview(config);
                _showPreview = true;
                search.InvalidateCache();
            }

            if (_showPreview && _previewData != null && _previewData.Length > 0)
            {
                if (isOutdated)
                {
                    EditorGUILayout.HelpBox("Preview is outdated. Settings or target object have changed since the preview was generated. Click 'Preview Compression Results' to refresh.", MessageType.Warning);
                }
                DrawPreview(config, search);
            }
            else if (_showPreview && (_previewData == null || _previewData.Length == 0))
            {
                EditorGUILayout.HelpBox("No textures found matching the current filter settings.", MessageType.Info);

                if (GUILayout.Button("Close"))
                {
                    _showPreview = false;
                }
            }
        }

        /// <summary>
        /// Invalidates the preview cache.
        /// </summary>
        public void InvalidateCache()
        {
            _previewData = null;
            _previewSettingsHash = 0;
        }

        private void GeneratePreview(TextureCompressor config)
        {
            _previewSettingsHash = PreviewGenerator.ComputeSettingsHash(config);
            _previewData = _generator.Generate(config);
        }

        private bool IsPreviewOutdated(TextureCompressor config)
        {
            if (!_showPreview || _previewData == null)
                return false;

            return PreviewGenerator.ComputeSettingsHash(config) != _previewSettingsHash;
        }

        private void DrawPreview(TextureCompressor config, SearchBoxControl search)
        {
            EditorGUILayout.Space(10);

            bool isSearching = search.IsSearching;
            int totalCount = _previewData.Length;
            int filteredCount = isSearching ? CountPreviewMatches(search) : totalCount;

            // Build header text
            string frozenInfo = _generator.FrozenCount > 0 ? $", {_generator.FrozenCount} frozen" : "";
            string headerText;
            if (isSearching && filteredCount != totalCount)
            {
                headerText = $"Preview ({filteredCount}/{totalCount} shown{frozenInfo})";
            }
            else
            {
                headerText = $"Preview ({_generator.ProcessedCount} to compress{frozenInfo}, {_generator.SkippedCount} skipped)";
            }
            EditorGUILayout.LabelField(headerText, EditorStyles.boldLabel);

            // Calculate totals
            long totalOriginal = 0;
            long totalAfter = 0;
            foreach (var data in _previewData)
            {
                totalOriginal += data.OriginalMemory;
                totalAfter += data.EstimatedMemory;
            }

            float savings = totalOriginal > 0 ? 1f - (float)totalAfter / totalOriginal : 0f;

            // Draw summary box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Original:", GUILayout.Width(60));
            EditorGUILayout.LabelField(MemoryCalculator.FormatBytes(totalOriginal), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("After:", GUILayout.Width(60));
            EditorGUILayout.LabelField(MemoryCalculator.FormatBytes(totalAfter), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Savings:", GUILayout.Width(60));
            Color originalColor = GUI.color;
            GUI.color = Color.green;
            long savedBytes = totalOriginal - totalAfter;
            EditorGUILayout.LabelField($"{savings:P0} (-{MemoryCalculator.FormatBytes(savedBytes)})", EditorStyles.boldLabel);
            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Show "no results" message when searching with no matches
            if (isSearching && filteredCount == 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("No textures match the search.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(5);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(300));

                bool hasDrawnProcessedHeader = false;
                bool hasDrawnFrozenHeader = false;
                bool hasDrawnSkippedHeader = false;

                for (int i = 0; i < _previewData.Length; i++)
                {
                    var data = _previewData[i];

                    // Skip items that don't match search
                    if (isSearching && !MatchesPreviewSearch(data, search))
                        continue;

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

                    DrawPreviewEntry(config, data, search);
                }

                EditorGUILayout.EndScrollView();

                // Show hidden count when searching
                if (isSearching && filteredCount < totalCount)
                {
                    SearchBoxControl.DrawHiddenCount(totalCount - filteredCount);
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Close Preview"))
            {
                _showPreview = false;
                _previewData = null;
            }
        }

        private void DrawPreviewEntry(TextureCompressor config, TexturePreviewData data, SearchBoxControl search)
        {
            bool isSkipped = !data.IsProcessed;
            bool isFrozenNow = !data.IsFrozen && config.IsFrozen(data.Guid);

            if (isSkipped || isFrozenNow)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Thumbnail
            ThumbnailControl.DrawClickable(data.Texture);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(data.Texture.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"[{data.TextureType}]", GUILayout.Width(60));

            // Freeze/Unfreeze button
            if (data.IsProcessed)
            {
                if (data.IsFrozen)
                {
                    var savedColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                    if (GUILayout.Button("Unfreeze", GUILayout.Width(70)))
                    {
                        Undo.RecordObject(config, "Unfreeze Texture");
                        config.UnfreezeTexture(data.Guid);
                        EditorUtility.SetDirty(config);
                        search.InvalidateCache();
                    }
                    GUI.backgroundColor = savedColor;
                }
                else
                {
                    if (GUILayout.Button("Freeze", GUILayout.Width(70)))
                    {
                        Undo.RecordObject(config, "Freeze Texture");
                        var frozenSettings = new FrozenTextureSettings(data.Guid, data.RecommendedDivisor, FrozenTextureFormat.Auto, false);
                        config.SetFrozenSettings(data.Guid, frozenSettings);
                        EditorUtility.SetDirty(config);
                        search.InvalidateCache();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            if (data.IsProcessed)
            {
                DrawProcessedDetails(data);
            }
            else
            {
                DrawSkippedDetails(data);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (isSkipped || isFrozenNow)
            {
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawProcessedDetails(TexturePreviewData data)
        {
            // Complexity bar
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Complexity:", GUILayout.Width(70));

            Color complexityColor = Color.Lerp(Color.green, Color.red, data.Complexity);
            EditorDrawUtils.DrawProgressBar(data.Complexity, 100, 16, complexityColor);

            EditorGUILayout.LabelField($"{data.Complexity:P0}", GUILayout.Width(45));
            EditorGUILayout.EndHorizontal();

            // Size
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Size:", GUILayout.Width(70));

            string sizeText;
            string manualIndicator = data.IsFrozen ? " (manual)" : "";
            if (data.RecommendedDivisor > 1)
            {
                sizeText = $"{data.OriginalSize.x}x{data.OriginalSize.y} \u2192 {data.RecommendedSize.x}x{data.RecommendedSize.y} (\u00f7{data.RecommendedDivisor}){manualIndicator}";
            }
            else
            {
                sizeText = $"{data.OriginalSize.x}x{data.OriginalSize.y} (unchanged){manualIndicator}";
            }
            EditorGUILayout.LabelField(sizeText);
            EditorGUILayout.EndHorizontal();

            // Format
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Format:", GUILayout.Width(70));
            if (data.PredictedFormat.HasValue)
            {
                string formatName = TextureFormatUtils.GetDisplayName(data.PredictedFormat.Value);
                string formatInfo = TextureFormatUtils.GetInfo(data.PredictedFormat.Value);
                if (data.IsFrozen && data.FrozenSettings != null && data.FrozenSettings.Format != FrozenTextureFormat.Auto)
                {
                    formatInfo += " (manual)";
                }
                var formatColor = TextureFormatUtils.GetColor(data.PredictedFormat.Value);

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

        private void DrawSkippedDetails(TexturePreviewData data)
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

        private int CountPreviewMatches(SearchBoxControl search)
        {
            if (_previewData == null) return 0;

            int count = 0;
            foreach (var data in _previewData)
            {
                if (MatchesPreviewSearch(data, search))
                    count++;
            }
            return count;
        }

        private bool MatchesPreviewSearch(TexturePreviewData data, SearchBoxControl search)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(data.Guid);
            string textureName = data.Texture != null ? data.Texture.name : "";

            return search.MatchesSearch(textureName) || search.MatchesSearch(assetPath) || search.MatchesSearch(data.TextureType);
        }
    }
}
