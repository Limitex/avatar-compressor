using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace dev.limitex.avatar.compressor.texture.editor
{
    /// <summary>
    /// Generates texture preview data for the editor UI.
    /// </summary>
    public class TexturePreviewGenerator
    {
        /// <summary>
        /// Result of preview generation.
        /// </summary>
        public class PreviewResult
        {
            public TexturePreviewData[] PreviewData;
            public int ProcessedCount;
            public int FrozenCount;
            public int SkippedCount;
            public int SettingsHash;
        }

        /// <summary>
        /// Generates preview data for all textures in the compressor's scope.
        /// </summary>
        public static PreviewResult Generate(TextureCompressor config)
        {
            int settingsHash = ComputeSettingsHash(config);

            // Build frozen texture lookup (GUID -> Settings)
            var frozenLookup = new Dictionary<string, FrozenTextureSettings>();
            foreach (var frozen in config.FrozenTextures)
            {
                if (!string.IsNullOrEmpty(frozen.TextureGuid))
                    frozenLookup[frozen.TextureGuid] = frozen;
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

            // Collect additional materials from animations and components
            var additionalMaterialRefs = new List<MaterialReference>();
            additionalMaterialRefs.AddRange(MaterialCollector.CollectFromAnimator(config.gameObject));
            additionalMaterialRefs.AddRange(MaterialCollector.CollectFromComponents(config.gameObject));

            var additionalMaterials = MaterialCollector.GetDistinctMaterials(additionalMaterialRefs);
            if (additionalMaterials.Any())
            {
                collector.CollectFromMaterials(additionalMaterials, allTextures, collectAll: true);
            }

            if (allTextures.Count == 0)
            {
                return new PreviewResult
                {
                    PreviewData = new TexturePreviewData[0],
                    ProcessedCount = 0,
                    FrozenCount = 0,
                    SkippedCount = 0,
                    SettingsHash = settingsHash
                };
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
                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                frozenLookup.TryGetValue(guid, out var frozenSettings);
                bool isFrozen = frozenSettings != null;

                if (info.IsProcessed && analysisResults.TryGetValue(tex, out var analysis))
                {
                    long originalMemory = Profiler.GetRuntimeMemorySizeLong(tex);
                    bool isNormalMap = info.TextureType == "Normal";
                    bool hasAlpha = TextureFormatSelector.HasSignificantAlpha(tex);

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
                            targetFormat = tex.format;
                        }
                        else
                        {
                            targetFormat = formatSelector.PredictFormat(isNormalMap, analysis.NormalizedComplexity, hasAlpha);
                        }
                    }

                    long estimatedMemory = TextureFormatUtils.EstimateCompressedMemory(
                        recommendedSize.x,
                        recommendedSize.y,
                        targetFormat);

                    var previewData = new TexturePreviewData
                    {
                        Texture = tex,
                        Guid = guid,
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
                        skippedList.Add(new TexturePreviewData
                        {
                            Texture = tex,
                            Guid = guid,
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
                        Guid = guid,
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

            // Sort by path
            var pathCache = new Dictionary<string, string>();
            foreach (var data in processedList.Concat(frozenList).Concat(skippedList))
            {
                if (!string.IsNullOrEmpty(data.Guid) && !pathCache.ContainsKey(data.Guid))
                    pathCache[data.Guid] = AssetDatabase.GUIDToAssetPath(data.Guid);
            }

            System.Comparison<TexturePreviewData> pathComparison = (a, b) =>
                string.Compare(
                    pathCache.TryGetValue(a.Guid, out var pathA) ? pathA : "",
                    pathCache.TryGetValue(b.Guid, out var pathB) ? pathB : "",
                    System.StringComparison.Ordinal);

            processedList.Sort(pathComparison);
            frozenList.Sort(pathComparison);
            skippedList.Sort(pathComparison);

            var allPreviewData = new List<TexturePreviewData>(processedList.Count + frozenList.Count + skippedList.Count);
            allPreviewData.AddRange(processedList);
            allPreviewData.AddRange(frozenList);
            allPreviewData.AddRange(skippedList);

            return new PreviewResult
            {
                PreviewData = allPreviewData.ToArray(),
                ProcessedCount = processedList.Count,
                FrozenCount = frozenList.Count,
                SkippedCount = skippedList.Count,
                SettingsHash = settingsHash
            };
        }

        /// <summary>
        /// Computes a hash of the compressor settings for detecting outdated previews.
        /// </summary>
        public static int ComputeSettingsHash(TextureCompressor config)
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

                foreach (var frozen in config.FrozenTextures)
                {
                    hash = hash * 31 + (frozen.TextureGuid?.GetHashCode() ?? 0);
                    hash = hash * 31 + frozen.Divisor;
                    hash = hash * 31 + frozen.Format.GetHashCode();
                    hash = hash * 31 + frozen.Skip.GetHashCode();
                }

                return hash;
            }
        }
    }
}
