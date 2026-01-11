using System;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Draws the texture preview section.
    /// </summary>
    public class PreviewSectionDrawer
    {
        private Vector2 _scrollPosition;

        public void DrawPreviewButton(Action onGeneratePreview)
        {
            if (GUILayout.Button("Preview Compression Results", GUILayout.Height(35)))
            {
                onGeneratePreview();
            }
        }

        public void DrawOutdatedWarning()
        {
            GUIDrawing.DrawHelpBox("Preview is outdated. Settings or target object have changed since the preview was generated. Click 'Preview Compression Results' to refresh.", MessageType.Warning);
        }

        public void DrawNoTexturesMessage(Action onClose)
        {
            GUIDrawing.DrawHelpBox("No textures found matching the current filter settings.", MessageType.Info);

            if (GUILayout.Button("Close"))
            {
                onClose();
            }
        }

        public void DrawPreview(
            TextureCompressor compressor,
            TexturePreviewData[] previewData,
            int processedCount,
            int frozenCount,
            int skippedCount,
            string searchText,
            Func<TexturePreviewData, bool> matchesSearch,
            Action onClose)
        {
            EditorGUILayout.Space(10);

            bool isSearching = !string.IsNullOrEmpty(searchText);
            int totalCount = previewData.Length;
            int filteredCount = isSearching ? CountMatches(previewData, matchesSearch) : totalCount;

            string headerText;
            if (isSearching && filteredCount != totalCount)
            {
                string frozenInfo = frozenCount > 0 ? $", {frozenCount} frozen" : "";
                headerText = $"Preview ({filteredCount}/{totalCount} shown{frozenInfo})";
            }
            else
            {
                string frozenInfo = frozenCount > 0 ? $", {frozenCount} frozen" : "";
                headerText = $"Preview ({processedCount} to compress{frozenInfo}, {skippedCount} skipped)";
            }
            GUIDrawing.DrawSectionHeader(headerText);

            DrawMemorySummary(previewData);

            if (isSearching && filteredCount == 0)
            {
                EditorGUILayout.Space(5);
                GUIDrawing.DrawHelpBox("No textures match the search.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(5);
                DrawPreviewList(compressor, previewData, isSearching, filteredCount, totalCount, matchesSearch);
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Close Preview"))
            {
                onClose();
            }
        }

        private void DrawMemorySummary(TexturePreviewData[] previewData)
        {
            long totalOriginal = 0;
            long totalAfter = 0;

            foreach (var data in previewData)
            {
                totalOriginal += data.OriginalMemory;
                totalAfter += data.EstimatedMemory;
            }

            float savings = totalOriginal > 0 ? 1f - (float)totalAfter / totalOriginal : 0f;

            GUIDrawing.BeginBox();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Original:", GUILayout.Width(60));
            EditorGUILayout.LabelField(GUIDrawing.FormatBytes(totalOriginal), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("After:", GUILayout.Width(60));
            EditorGUILayout.LabelField(GUIDrawing.FormatBytes(totalAfter), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Savings:", GUILayout.Width(60));
            Color originalColor = GUI.color;
            GUI.color = Color.green;
            long savedBytes = totalOriginal - totalAfter;
            EditorGUILayout.LabelField($"{savings:P0} (-{GUIDrawing.FormatBytes(savedBytes)})", EditorStyles.boldLabel);
            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();

            GUIDrawing.EndBox();
        }

        private void DrawPreviewList(
            TextureCompressor compressor,
            TexturePreviewData[] previewData,
            bool isSearching,
            int filteredCount,
            int totalCount,
            Func<TexturePreviewData, bool> matchesSearch)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(300));

            bool hasDrawnProcessedHeader = false;
            bool hasDrawnFrozenHeader = false;
            bool hasDrawnSkippedHeader = false;

            foreach (var data in previewData)
            {
                if (isSearching && !matchesSearch(data))
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

                DrawPreviewEntry(compressor, data);
            }

            EditorGUILayout.EndScrollView();

            if (isSearching && filteredCount < totalCount)
            {
                int hiddenCount = totalCount - filteredCount;
                string hiddenText = hiddenCount == 1 ? "1 hidden" : $"{hiddenCount} hidden";
                EditorGUILayout.LabelField(hiddenText, GUIDrawing.HiddenCountStyle);
            }
        }

        private void DrawPreviewEntry(TextureCompressor compressor, TexturePreviewData data)
        {
            bool isSkipped = !data.IsProcessed;
            bool isFrozenNow = !data.IsFrozen && compressor.IsFrozen(data.Guid);

            if (isSkipped || isFrozenNow)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUIDrawing.DrawClickableThumbnail(data.Texture);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(data.Texture.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"[{data.TextureType}]", GUILayout.Width(60));

            if (data.IsProcessed)
            {
                DrawFreezeButton(compressor, data);
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

        private void DrawFreezeButton(TextureCompressor compressor, TexturePreviewData data)
        {
            if (data.IsFrozen)
            {
                var savedColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                if (GUILayout.Button("Unfreeze", GUILayout.Width(70)))
                {
                    Undo.RecordObject(compressor, "Unfreeze Texture");
                    compressor.UnfreezeTexture(data.Guid);
                    EditorUtility.SetDirty(compressor);
                }
                GUI.backgroundColor = savedColor;
            }
            else
            {
                if (GUILayout.Button("Freeze", GUILayout.Width(70)))
                {
                    Undo.RecordObject(compressor, "Freeze Texture");
                    var frozenSettings = new FrozenTextureSettings(data.Guid, data.RecommendedDivisor, FrozenTextureFormat.Auto, false);
                    compressor.SetFrozenSettings(data.Guid, frozenSettings);
                    EditorUtility.SetDirty(compressor);
                }
            }
        }

        private void DrawProcessedDetails(TexturePreviewData data)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Complexity:", GUILayout.Width(70));

            Color complexityColor = Color.Lerp(Color.green, Color.red, data.Complexity);
            GUIDrawing.DrawProgressBar(data.Complexity, 100, 16, complexityColor);

            EditorGUILayout.LabelField($"{data.Complexity:P0}", GUILayout.Width(45));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Size:", GUILayout.Width(70));

            string sizeText;
            string manualIndicator = data.IsFrozen ? " (manual)" : "";
            if (data.RecommendedDivisor > 1)
            {
                sizeText = $"{data.OriginalSize.x}x{data.OriginalSize.y} -> {data.RecommendedSize.x}x{data.RecommendedSize.y} (/{data.RecommendedDivisor}){manualIndicator}";
            }
            else
            {
                sizeText = $"{data.OriginalSize.x}x{data.OriginalSize.y} (unchanged){manualIndicator}";
            }
            EditorGUILayout.LabelField(sizeText);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Format:", GUILayout.Width(70));
            if (data.PredictedFormat.HasValue)
            {
                string formatName = TextureFormatUtils.GetFormatDisplayName(data.PredictedFormat.Value);
                string formatInfo = TextureFormatUtils.GetFormatInfo(data.PredictedFormat.Value);
                if (data.IsFrozen && data.FrozenSettings != null && data.FrozenSettings.Format != FrozenTextureFormat.Auto)
                {
                    formatInfo += " (manual)";
                }
                var formatColor = TextureFormatUtils.GetFormatColor(data.PredictedFormat.Value);

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

        private int CountMatches(TexturePreviewData[] previewData, Func<TexturePreviewData, bool> matchesSearch)
        {
            int count = 0;
            foreach (var data in previewData)
            {
                if (matchesSearch(data))
                    count++;
            }
            return count;
        }
    }
}
