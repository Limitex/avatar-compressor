using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Draws the preview section with texture analysis results.
    /// Owns its own search box.
    /// </summary>
    internal sealed class PreviewSection
    {
        private readonly PreviewGenerator _generator = new PreviewGenerator();
        private readonly SearchBoxControl _searchBox = new();
        private TexturePreviewData[] _previewData;
        private int _previewSettingsHash;
        private bool _showPreview;
        private Vector2 _scrollPosition;

        /// <summary>
        /// Draws the preview section.
        /// </summary>
        public void Draw(TextureCompressor config)
        {
            bool isOutdated = IsPreviewOutdated(config);

            if (
                GUILayout.Button(
                    AvatarCompressorLocalization.Tr("TextureCompressor:button:previewGenerate"),
                    GUILayout.Height(35)
                )
            )
            {
                GeneratePreview(config);
                _showPreview = true;
            }

            if (_showPreview && _previewData != null && _previewData.Length > 0)
            {
                if (isOutdated)
                {
                    EditorGUILayout.HelpBox(
                        AvatarCompressorLocalization.Tr(
                            "TextureCompressor:message:previewOutdated"
                        ),
                        MessageType.Warning
                    );
                }
                DrawPreview(config);
            }
            else if (_showPreview && (_previewData == null || _previewData.Length == 0))
            {
                EditorGUILayout.HelpBox(
                    AvatarCompressorLocalization.Tr("TextureCompressor:message:previewNoTextures"),
                    MessageType.Info
                );

                if (GUILayout.Button(AvatarCompressorLocalization.Tr("Common:label:close")))
                {
                    _showPreview = false;
                }
            }
        }

        private void GeneratePreview(TextureCompressor config)
        {
            var analysisBackend = TextureCompressorPreferences.AnalysisBackend;
            var resizeBackend = TextureCompressorPreferences.ResizeBackend;
            _previewSettingsHash = PreviewGenerator.ComputeSettingsHash(
                config,
                analysisBackend,
                resizeBackend
            );
            _previewData = _generator.Generate(config, analysisBackend, resizeBackend);
            _searchBox.InvalidateCountCache();
        }

        private bool IsPreviewOutdated(TextureCompressor config)
        {
            if (!_showPreview || _previewData == null)
                return false;

            return PreviewGenerator.ComputeSettingsHash(
                    config,
                    TextureCompressorPreferences.AnalysisBackend,
                    TextureCompressorPreferences.ResizeBackend
                ) != _previewSettingsHash;
        }

        private void DrawPreview(TextureCompressor config)
        {
            EditorGUILayout.Space(10);

            bool isSearching = _searchBox.IsSearching;
            int totalCount = _previewData.Length;
            int filteredCount = _searchBox.CountMatches(_previewData, MatchesPreviewSearch);

            // Build header text
            string headerText;
            if (isSearching && filteredCount != totalCount)
            {
                headerText =
                    _generator.FrozenCount > 0
                        ? AvatarCompressorLocalization.Tr(
                            "TextureCompressor:message:previewHeaderFilteredFrozen",
                            filteredCount,
                            totalCount,
                            _generator.FrozenCount
                        )
                        : AvatarCompressorLocalization.Tr(
                            "TextureCompressor:message:previewHeaderFiltered",
                            filteredCount,
                            totalCount
                        );
            }
            else
            {
                headerText =
                    _generator.FrozenCount > 0
                        ? AvatarCompressorLocalization.Tr(
                            "TextureCompressor:message:previewHeaderSummaryFrozen",
                            _generator.ProcessedCount,
                            _generator.FrozenCount,
                            _generator.SkippedCount
                        )
                        : AvatarCompressorLocalization.Tr(
                            "TextureCompressor:message:previewHeaderSummary",
                            _generator.ProcessedCount,
                            _generator.SkippedCount
                        );
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
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:previewOriginal"),
                GUILayout.Width(75)
            );
            EditorGUILayout.LabelField(
                MemoryCalculator.FormatBytes(totalOriginal),
                EditorStyles.boldLabel
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:previewAfter"),
                GUILayout.Width(75)
            );
            EditorGUILayout.LabelField(
                MemoryCalculator.FormatBytes(totalAfter),
                EditorStyles.boldLabel
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:previewSavings"),
                GUILayout.Width(75)
            );
            Color originalColor = GUI.color;
            GUI.color = Color.green;
            long savedBytes = totalOriginal - totalAfter;
            EditorGUILayout.LabelField(
                $"{savings:P0} (-{MemoryCalculator.FormatBytes(savedBytes)})",
                EditorStyles.boldLabel
            );
            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            if (config.DetectUnusedTextures || config.BakeLilToonTextures)
            {
                string helpKey =
                    config.DetectUnusedTextures && config.BakeLilToonTextures
                        ? "TextureCompressor:message:previewBuildOnlyBoth"
                    : config.DetectUnusedTextures
                        ? "TextureCompressor:message:previewBuildOnlyUnused"
                    : "TextureCompressor:message:previewBuildOnlyBake";

                EditorGUILayout.HelpBox(AvatarCompressorLocalization.Tr(helpKey), MessageType.Info);
            }

            // Search box for preview textures
            _searchBox.Draw(filteredCount, totalCount);

            // Show "no results" message when searching with no matches
            if (isSearching && filteredCount == 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    AvatarCompressorLocalization.Tr(
                        "TextureCompressor:message:previewNoSearchResults"
                    ),
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.Space(5);

                _scrollPosition = EditorGUILayout.BeginScrollView(
                    _scrollPosition,
                    GUILayout.MaxHeight(300)
                );

                bool hasDrawnProcessedHeader = false;
                bool hasDrawnFrozenHeader = false;
                bool hasDrawnSkippedHeader = false;

                for (int i = 0; i < _previewData.Length; i++)
                {
                    var data = _previewData[i];

                    // Skip items that don't match search
                    if (isSearching && !MatchesPreviewSearch(data))
                        continue;

                    // Section headers
                    if (data.IsProcessed && !data.IsFrozen && !hasDrawnProcessedHeader)
                    {
                        EditorGUILayout.LabelField(
                            AvatarCompressorLocalization.Tr(
                                "TextureCompressor:label:previewToCompress"
                            ),
                            EditorStyles.boldLabel
                        );
                        hasDrawnProcessedHeader = true;
                    }

                    if (data.IsProcessed && data.IsFrozen && !hasDrawnFrozenHeader)
                    {
                        EditorGUILayout.Space(10);
                        EditorGUILayout.LabelField(
                            AvatarCompressorLocalization.Tr(
                                "TextureCompressor:label:previewFrozenManual"
                            ),
                            EditorStyles.boldLabel
                        );
                        hasDrawnFrozenHeader = true;
                    }

                    if (!data.IsProcessed && !hasDrawnSkippedHeader)
                    {
                        EditorGUILayout.Space(10);
                        EditorGUILayout.LabelField(
                            AvatarCompressorLocalization.Tr(
                                "TextureCompressor:label:previewSkippedTextures"
                            ),
                            EditorStyles.boldLabel
                        );
                        hasDrawnSkippedHeader = true;
                    }

                    DrawPreviewEntry(config, data);
                }

                EditorGUILayout.EndScrollView();

                // Show hidden count when searching
                if (isSearching && filteredCount < totalCount)
                {
                    EditorDrawUtils.DrawHiddenCount(totalCount - filteredCount);
                }
            }

            EditorGUILayout.Space(5);

            if (
                GUILayout.Button(
                    AvatarCompressorLocalization.Tr("TextureCompressor:button:previewClose")
                )
            )
            {
                _showPreview = false;
                _previewData = null;
            }
        }

        private void DrawPreviewEntry(TextureCompressor config, TexturePreviewData data)
        {
            bool isSkipped = !data.IsProcessed;
            bool frozenStateChanged = data.IsFrozen != config.IsFrozen(data.Guid);

            if (isSkipped || frozenStateChanged)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            EditorGUILayout.BeginVertical(GUILayout.Width(45));
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal(GUILayout.Width(45));
            GUILayout.FlexibleSpace();
            ThumbnailControl.DrawClickable(data.Texture);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.EnumValue("TextureCompressor", data.TextureType),
                EditorStylesCache.CenteredBoldLabel,
                GUILayout.Width(45)
            );
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                data.Texture.name,
                EditorStylesCache.ClippedBoldLabel,
                GUILayout.MinWidth(0)
            );

            // Freeze/Unfreeze button
            if (data.IsProcessed)
            {
                if (data.IsFrozen)
                {
                    var savedColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                    if (
                        GUILayout.Button(
                            AvatarCompressorLocalization.Tr("TextureCompressor:button:unfreeze"),
                            GUILayout.Width(85)
                        )
                    )
                    {
                        Undo.RecordObject(config, "Unfreeze Texture");
                        config.UnfreezeTexture(data.Guid);
                        EditorUtility.SetDirty(config);
                    }
                    GUI.backgroundColor = savedColor;
                }
                else
                {
                    if (
                        GUILayout.Button(
                            AvatarCompressorLocalization.Tr(
                                "TextureCompressor:button:previewFreeze"
                            ),
                            GUILayout.Width(85)
                        )
                    )
                    {
                        Undo.RecordObject(config, "Freeze Texture");
                        var frozenSettings = new FrozenTextureSettings(
                            data.Guid,
                            data.RecommendedDivisor,
                            FrozenTextureFormat.Auto,
                            false
                        );
                        config.SetFrozenSettings(data.Guid, frozenSettings);
                        EditorUtility.SetDirty(config);
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

            if (isSkipped || frozenStateChanged)
            {
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawProcessedDetails(TexturePreviewData data)
        {
            // Complexity bar
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:previewComplexity"),
                GUILayout.Width(85)
            );

            Color complexityColor = Color.Lerp(Color.green, Color.red, data.Complexity);
            EditorDrawUtils.DrawProgressBar(data.Complexity, 100, 16, complexityColor);

            EditorGUILayout.LabelField($"{data.Complexity:P0}", GUILayout.Width(45));
            EditorGUILayout.EndHorizontal();

            // Size
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:previewSize"),
                GUILayout.Width(85)
            );

            string sizeText;
            string manualIndicator = data.IsFrozen
                ? AvatarCompressorLocalization.Tr("TextureCompressor:label:previewManualSuffix")
                : "";
            if (data.RecommendedDivisor > 1)
            {
                sizeText = AvatarCompressorLocalization.Tr(
                    "TextureCompressor:message:previewResized",
                    data.OriginalSize.x,
                    data.OriginalSize.y,
                    data.RecommendedSize.x,
                    data.RecommendedSize.y,
                    data.RecommendedDivisor,
                    manualIndicator
                );
            }
            else
            {
                sizeText = AvatarCompressorLocalization.Tr(
                    "TextureCompressor:message:previewUnchanged",
                    data.OriginalSize.x,
                    data.OriginalSize.y,
                    manualIndicator
                );
            }
            EditorGUILayout.LabelField(sizeText, GUILayout.MinWidth(0));
            EditorGUILayout.EndHorizontal();

            // Format
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:previewFormat"),
                GUILayout.Width(85)
            );
            if (data.PredictedFormat.HasValue)
            {
                string formatName = TextureFormatUtils.GetDisplayName(data.PredictedFormat.Value);
                string formatInfo = TextureFormatUtils.GetInfo(data.PredictedFormat.Value);
                if (
                    data.IsFrozen
                    && data.FrozenSettings != null
                    && data.FrozenSettings.Format != FrozenTextureFormat.Auto
                )
                {
                    formatInfo += AvatarCompressorLocalization.Tr(
                        "TextureCompressor:label:previewManualSuffix"
                    );
                }
                var formatColor = TextureFormatUtils.GetColor(data.PredictedFormat.Value);

                var savedGuiColor = GUI.color;
                GUI.color = formatColor;
                EditorGUILayout.LabelField(formatName, EditorStyles.boldLabel, GUILayout.Width(70));
                GUI.color = savedGuiColor;
                EditorGUILayout.LabelField(
                    formatInfo,
                    EditorStyles.miniLabel,
                    GUILayout.MinWidth(0)
                );
            }
            else
            {
                EditorGUILayout.LabelField(
                    AvatarCompressorLocalization.Tr("Common:label:notAvailable"),
                    EditorStyles.miniLabel
                );
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSkippedDetails(TexturePreviewData data)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:previewSize"),
                GUILayout.Width(85)
            );
            EditorGUILayout.LabelField($"{data.OriginalSize.x}x{data.OriginalSize.y}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                AvatarCompressorLocalization.Tr("TextureCompressor:label:previewReason"),
                GUILayout.Width(85)
            );
            string reasonText = AvatarCompressorLocalization.EnumValue(
                "TextureCompressor",
                data.SkipReason
            );
            EditorGUILayout.LabelField(reasonText, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private bool MatchesPreviewSearch(TexturePreviewData data)
        {
            string assetPath = GuidPathCache.GetPath(data.Guid);
            string textureName = data.Texture != null ? data.Texture.name : "";

            return _searchBox.MatchesSearchAny(
                textureName,
                assetPath,
                data.TextureType.ToString(),
                AvatarCompressorLocalization.EnumValue("TextureCompressor", data.TextureType)
            );
        }
    }
}
