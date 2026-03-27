using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.ui;
using UnityEditor;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture.ui
{
    /// <summary>
    /// Generates preview data for texture compression.
    /// </summary>
    public class PreviewGenerator
    {
        private static LruCache<
            (string guid, Hash128 contentHash, int analysisHash, bool isNormalMap, bool isEmission),
            TextureAnalysisResult
        > AnalysisCache = new(256);

        [InitializeOnLoadMethod]
        private static void ClearCacheOnDomainReload()
        {
            AnalysisCache = new(256);
        }

        /// <summary>
        /// Number of textures that will be processed.
        /// </summary>
        public int ProcessedCount { get; private set; }

        /// <summary>
        /// Number of frozen textures.
        /// </summary>
        public int FrozenCount { get; private set; }

        /// <summary>
        /// Number of skipped textures.
        /// </summary>
        public int SkippedCount { get; private set; }

        /// <summary>
        /// Generates preview data for the given configuration.
        /// </summary>
        public TexturePreviewData[] Generate(
            TextureCompressor config,
            AnalysisBackendPreference backendPreference = AnalysisBackendPreference.Auto
        )
        {
            var frozenLookup = FrozenTextureSettings.BuildLookup(config.FrozenTextures);

            var frozenSkipGuids = config
                .FrozenTextures.Where(f => f.Skip && !string.IsNullOrEmpty(f.TextureGuid))
                .Select(f => f.TextureGuid);

            var collector = new TextureCollector(
                config.MinSourceSize,
                config.SkipIfSmallerThan,
                config.ProcessMainTextures,
                config.ProcessNormalMaps,
                config.ProcessEmissionMaps,
                config.ProcessOtherTextures,
                config.SkipUnknownUncompressedTextures,
                config.ExcludedPaths,
                frozenSkipGuids
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
            additionalMaterialRefs.AddRange(
                MaterialCollector.CollectFromAnimator(config.gameObject)
            );
            additionalMaterialRefs.AddRange(
                MaterialCollector.CollectFromComponents(config.gameObject)
            );

            var additionalMaterials = MaterialCollector.GetDistinctMaterials(
                additionalMaterialRefs
            );
            if (additionalMaterials.Any())
            {
                collector.CollectFromMaterials(additionalMaterials, allTextures, collectAll: true);
            }

            if (allTextures.Count == 0)
            {
                ProcessedCount = 0;
                FrozenCount = 0;
                SkippedCount = 0;
                return new TexturePreviewData[0];
            }

            var processedTextures = new Dictionary<Texture2D, TextureInfo>();
            foreach (var kvp in allTextures)
            {
                if (!kvp.Value.IsProcessed)
                    continue;

                processedTextures[kvp.Key] = kvp.Value;
            }

            var analyzer = new TextureAnalyzer(
                config.Strategy,
                config.FastWeight,
                config.HighAccuracyWeight,
                config.PerceptualWeight,
                processor,
                complexityCalc,
                backendPreference
            );

            // Use cache to avoid re-analyzing unchanged textures
            int analysisHash = ComputeAnalysisHash(config, backendPreference);
            var analysisResults = new Dictionary<Texture2D, TextureAnalysisResult>();

            var assetInfoCache = new Dictionary<Texture2D, (string guid, Hash128 contentHash)>();

            if (processedTextures.Count > 0)
            {
                var needsAnalysis = new Dictionary<Texture2D, TextureInfo>();

                foreach (var kvp in processedTextures)
                {
                    string path = AssetDatabase.GetAssetPath(kvp.Key);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    var contentHash = AssetDatabase.GetAssetDependencyHash(path);
                    assetInfoCache[kvp.Key] = (guid, contentHash);
                    var cacheKey = (
                        guid,
                        contentHash,
                        analysisHash,
                        kvp.Value.IsNormalMap,
                        kvp.Value.IsEmission
                    );

                    if (
                        !string.IsNullOrEmpty(guid)
                        && AnalysisCache.TryGetValue(cacheKey, out var cached)
                    )
                    {
                        analysisResults[kvp.Key] = cached;
                    }
                    else
                    {
                        needsAnalysis[kvp.Key] = kvp.Value;
                    }
                }

                if (needsAnalysis.Count > 0)
                {
                    var newResults = analyzer.AnalyzeBatch(needsAnalysis);
                    foreach (var kvp in newResults)
                    {
                        analysisResults[kvp.Key] = kvp.Value;

                        if (
                            assetInfoCache.TryGetValue(kvp.Key, out var info)
                            && !string.IsNullOrEmpty(info.guid)
                            && processedTextures.TryGetValue(kvp.Key, out var texInfo)
                        )
                        {
                            AnalysisCache.Set(
                                (
                                    info.guid,
                                    info.contentHash,
                                    analysisHash,
                                    texInfo.IsNormalMap,
                                    texInfo.IsEmission
                                ),
                                kvp.Value
                            );
                        }
                    }
                }
            }

            var processedList = new List<TexturePreviewData>();
            var frozenList = new List<TexturePreviewData>();
            var skippedList = new List<TexturePreviewData>();

            foreach (var kvp in allTextures)
            {
                var tex = kvp.Key;
                var info = kvp.Value;

                string guid;
                if (assetInfoCache.TryGetValue(tex, out var cachedAssetInfo))
                {
                    guid = cachedAssetInfo.guid;
                }
                else
                {
                    string assetPath = AssetDatabase.GetAssetPath(tex);
                    guid = AssetDatabase.AssetPathToGUID(assetPath);
                }

                // Skip textures with invalid GUID (e.g., runtime-generated textures)
                if (string.IsNullOrEmpty(guid))
                    continue;

                // Check if texture is frozen
                frozenLookup.TryGetValue(guid, out var frozenSettings);
                bool isFrozen = frozenSettings != null;

                if (info.IsProcessed && analysisResults.TryGetValue(tex, out var analysis))
                {
                    long originalMemory = MemoryCalculator.CalculateCompressedMemory(
                        tex.width,
                        tex.height,
                        tex.format,
                        tex.mipmapCount
                    );
                    bool isNormalMap = info.TextureType == "Normal";

                    int divisor;
                    Vector2Int recommendedSize;
                    TextureFormat targetFormat;
                    bool hasAlpha;

                    if (isFrozen && !frozenSettings.Skip)
                    {
                        divisor = frozenSettings.Divisor;
                        recommendedSize = processor.CalculateNewDimensions(
                            tex.width,
                            tex.height,
                            divisor
                        );
                        // Match the build pipeline: frozen textures always flow through
                        // the resize/copy path before alpha detection, even when dimensions
                        // are unchanged.
                        var frozenAnalysis = new TextureAnalysisResult(
                            AnalysisConstants.DefaultComplexityScore,
                            divisor,
                            recommendedSize
                        );
                        var resizedTex = processor.ResizeSingle(tex, frozenAnalysis, isNormalMap);
                        if (resizedTex != null)
                        {
                            hasAlpha = TextureFormatSelector.HasSignificantAlpha(resizedTex);
                            Object.DestroyImmediate(resizedTex);
                        }
                        else
                        {
                            hasAlpha = TextureFormatSelector.HasSignificantAlpha(tex);
                        }
                        targetFormat = formatSelector.ResolveTargetFormat(
                            tex.format,
                            isNormalMap,
                            AnalysisConstants.DefaultComplexityScore,
                            hasAlpha,
                            frozenSettings.Format
                        );
                    }
                    else
                    {
                        divisor = analysis.RecommendedDivisor;
                        recommendedSize = analysis.RecommendedResolution;
                        // Match the build pipeline: detect alpha on the resized texture
                        // so that alpha regions lost during downscaling don't cause an
                        // unnecessary alpha-capable format (e.g. BC7 instead of DXT1).
                        var resizedTex = processor.ResizeSingle(tex, analysis, isNormalMap);
                        if (resizedTex != null)
                        {
                            hasAlpha = TextureFormatSelector.HasSignificantAlpha(resizedTex);
                            Object.DestroyImmediate(resizedTex);
                        }
                        else
                        {
                            hasAlpha = TextureFormatSelector.HasSignificantAlpha(tex);
                        }
                        targetFormat = formatSelector.ResolveTargetFormat(
                            tex.format,
                            isNormalMap,
                            analysis.NormalizedComplexity,
                            hasAlpha,
                            null
                        );
                    }

                    long estimatedMemory = MemoryCalculator.CalculateCompressedMemory(
                        recommendedSize.x,
                        recommendedSize.y,
                        targetFormat,
                        tex.mipmapCount
                    );

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
                        FrozenSettings = frozenSettings,
                    };

                    if (isFrozen)
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
                    long originalMemory = MemoryCalculator.CalculateCompressedMemory(
                        tex.width,
                        tex.height,
                        tex.format,
                        tex.mipmapCount
                    );

                    skippedList.Add(
                        new TexturePreviewData
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
                            FrozenSettings = frozenSettings,
                        }
                    );
                }
            }

            ProcessedCount = processedList.Count;
            FrozenCount = frozenList.Count;
            SkippedCount = skippedList.Count;

            // Sort by file path
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
                    System.StringComparison.Ordinal
                );

            processedList.Sort(pathComparison);
            frozenList.Sort(pathComparison);
            skippedList.Sort(pathComparison);

            var allPreviewData = new List<TexturePreviewData>(
                processedList.Count + frozenList.Count + skippedList.Count
            );
            allPreviewData.AddRange(processedList);
            allPreviewData.AddRange(frozenList);
            allPreviewData.AddRange(skippedList);

            return allPreviewData.ToArray();
        }

        /// <summary>
        /// Computes a hash of analysis-affecting settings only (Strategy, Weights, Thresholds, Resolution).
        /// Used as the cache key for analysis results.
        /// </summary>
        private static int ComputeAnalysisHash(
            TextureCompressor config,
            AnalysisBackendPreference backendPreference
        )
        {
            unchecked
            {
                int hash = 17;
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
                hash = hash * 31 + backendPreference.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Computes a hash of the configuration for change detection.
        /// </summary>
        public static int ComputeSettingsHash(
            TextureCompressor config,
            AnalysisBackendPreference backendPreference = AnalysisBackendPreference.Auto
        )
        {
            unchecked
            {
                // Start from analysis hash (Strategy, Weights, Thresholds, Resolution)
                int hash = ComputeAnalysisHash(config, backendPreference);
                hash = hash * 31 + config.Preset.GetHashCode();
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
