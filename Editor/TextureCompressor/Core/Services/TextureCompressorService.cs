using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dev.limitex.avatar.compressor.editor;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// Service that handles texture compression logic.
    /// Implements ICompressor for consistency with other compressors.
    /// </summary>
    public class TextureCompressorService : ICompressor
    {
        public string Name => "Texture Compressor";

        private readonly TextureCompressor _config;
        private readonly TextureCollector _collector;
        private readonly TextureProcessor _processor;
        private readonly TextureFormatSelector _formatSelector;
        private readonly ComplexityCalculator _complexityCalc;
        private readonly TextureAnalyzer _analyzer;
        private readonly NormalMapPreprocessor _normalMapPreprocessor;
        private readonly Dictionary<string, FrozenTextureSettings> _frozenLookup;

        // Flag to avoid repeating the same warning for every texture
        private static bool _streamingMipmapsWarningShown;

        // Flag to avoid repeating build context warning
        private static bool _buildContextWarningShown;

        public TextureCompressorService(TextureCompressor config)
        {
            _config = config;

            // Build frozen texture lookup (GUID -> Settings)
            _frozenLookup = new Dictionary<string, FrozenTextureSettings>();
            foreach (var frozen in config.FrozenTextures)
            {
                if (!string.IsNullOrEmpty(frozen.TextureGuid))
                    _frozenLookup[frozen.TextureGuid] = frozen;
            }

            // Get frozen skip GUIDs (textures with Skip=true should be excluded from collection)
            var frozenSkipGuids = config
                .FrozenTextures.Where(f => f.Skip && !string.IsNullOrEmpty(f.TextureGuid))
                .Select(f => f.TextureGuid);

            _collector = new TextureCollector(
                config.MinSourceSize,
                config.SkipIfSmallerThan,
                config.ProcessMainTextures,
                config.ProcessNormalMaps,
                config.ProcessEmissionMaps,
                config.ProcessOtherTextures,
                config.ExcludedPaths,
                frozenSkipGuids
            );

            _processor = new TextureProcessor(
                config.MinResolution,
                config.MaxResolution,
                config.ForcePowerOfTwo
            );

            _formatSelector = new TextureFormatSelector(
                config.TargetPlatform,
                config.UseHighQualityFormatForHighComplexity,
                config.HighComplexityThreshold
            );

            _complexityCalc = new ComplexityCalculator(
                config.HighComplexityThreshold,
                config.LowComplexityThreshold,
                config.MinDivisor,
                config.MaxDivisor
            );

            _analyzer = new TextureAnalyzer(
                config.Strategy,
                config.FastWeight,
                config.HighAccuracyWeight,
                config.PerceptualWeight,
                _processor,
                _complexityCalc
            );

            _normalMapPreprocessor = new NormalMapPreprocessor();
        }

        /// <summary>
        /// Compresses textures in the avatar hierarchy (ICompressor interface).
        /// Only processes materials on Renderers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>WARNING:</b> This method modifies the Renderer components on the provided GameObject
        /// by replacing their material references with cloned materials. While the original
        /// material asset files (.mat) are NOT modified, the scene will be marked as dirty
        /// if called outside of an NDMF build context.
        /// </para>
        /// <para>
        /// For production use, prefer using the NDMF plugin (TextureCompressorPass) which
        /// operates on a cloned avatar and properly handles animation-referenced materials.
        /// </para>
        /// </remarks>
        public void Compress(GameObject root, bool enableLogging)
        {
            // Warn if materials are linked to asset files (indicates non-build context usage)
            WarnIfNotInBuildContext(root);

            // Collect only Renderer materials for ICompressor interface
            var references = MaterialCollector.CollectFromRenderers(root);
            CompressWithMappings(references, enableLogging);
        }

        /// <summary>
        /// Warns if materials appear to be asset files, which indicates usage outside NDMF build context.
        /// In NDMF build context, materials should already be cloned/runtime objects.
        /// </summary>
        private void WarnIfNotInBuildContext(GameObject root)
        {
            if (_buildContextWarningShown)
                return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                        continue;

                    string assetPath = AssetDatabase.GetAssetPath(material);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        _buildContextWarningShown = true;
                        Debug.LogWarning(
                            $"[{Name}] Material '{material.name}' is an asset file ({assetPath}). "
                                + "This suggests usage outside NDMF build context. "
                                + "While original asset files will NOT be modified, the Renderer's material "
                                + "references will be changed. For non-destructive workflow, use the NDMF plugin."
                        );
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Compresses textures from the given material references and returns mapping information.
        /// </summary>
        /// <param name="materialReferences">Material references to process (from Renderers, animations, components, etc.)</param>
        /// <param name="enableLogging">Whether to log progress</param>
        /// <returns>Tuple containing original-to-compressed texture mappings and original-to-cloned material mappings</returns>
        public (
            Dictionary<Texture2D, Texture2D> ProcessedTextures,
            Dictionary<Material, Material> ClonedMaterials
        ) CompressWithMappings(
            IEnumerable<MaterialReference> materialReferences,
            bool enableLogging
        )
        {
            var referenceList = materialReferences.ToList();

            // Clone all materials and update Renderer references
            var clonedMaterials = MaterialCloner.CloneAndReplace(referenceList);

            // Collect textures from cloned materials
            var clonedMaterialList = clonedMaterials.Values.ToList();
            var textures = new Dictionary<Texture2D, TextureInfo>();
            _collector.CollectFromMaterials(clonedMaterialList, textures);

            if (textures.Count == 0)
            {
                if (enableLogging)
                {
                    Debug.Log($"[{Name}] No textures found to process.");
                }
                return (new Dictionary<Texture2D, Texture2D>(), clonedMaterials);
            }

            if (enableLogging)
            {
                Debug.Log($"[{Name}] Processing {textures.Count} textures...");
            }

            var analysisResults = _analyzer.AnalyzeBatch(textures);

            // Step 1: Resolve analysis/frozen settings and prepare resize work items
            var resizeItems =
                new List<(
                    Texture2D Source,
                    TextureAnalysisResult Analysis,
                    bool IsNormalMap,
                    TextureInfo Info,
                    FrozenTextureFormat? FormatOverride
                )>();

            foreach (var kvp in textures)
            {
                var originalTexture = kvp.Key;
                var textureInfo = kvp.Value;

                var resolved = ResolveAnalysis(
                    originalTexture,
                    textureInfo,
                    analysisResults,
                    enableLogging
                );
                if (resolved == null)
                    continue;

                resizeItems.Add(
                    (
                        originalTexture,
                        resolved.Value.Analysis,
                        textureInfo.IsNormalMap,
                        textureInfo,
                        resolved.Value.FormatOverride
                    )
                );
            }

            // Step 2: Batch resize all textures (single lock acquisition)
            var resizedTextures = _processor.ResizeBatch(
                resizeItems.Select(item => (item.Source, item.Analysis, item.IsNormalMap))
            );

            // Step 3: Resolve format and preprocess normal maps in parallel
            var preprocessResults =
                new ConcurrentDictionary<
                    Texture2D,
                    (
                        Texture2D Resized,
                        TextureFormat TargetFormat,
                        bool IsNormalMap,
                        bool PreserveAlpha,
                        NormalMapPreprocessor.SourceLayout SourceLayout
                    )
                >();

            Parallel.ForEach(
                resizeItems,
                item =>
                {
                    if (!resizedTextures.TryGetValue(item.Source, out var resizedTexture))
                        return;

                    bool hasAlpha = false;
                    bool hasAlphaComputed = false;
                    bool GetHasAlpha()
                    {
                        if (!hasAlphaComputed)
                        {
                            hasAlpha = TextureFormatSelector.HasSignificantAlpha(resizedTexture);
                            hasAlphaComputed = true;
                        }
                        return hasAlpha;
                    }

                    TextureFormat targetFormat;
                    if (
                        item.FormatOverride.HasValue
                        && item.FormatOverride.Value != FrozenTextureFormat.Auto
                    )
                    {
                        targetFormat = TextureFormatSelector.ConvertFrozenFormat(
                            item.FormatOverride.Value
                        );
                    }
                    else if (TextureFormatSelector.IsCompressedFormat(item.Source.format))
                    {
                        targetFormat = item.Source.format;
                    }
                    else
                    {
                        targetFormat = _formatSelector.PredictFormat(
                            item.IsNormalMap,
                            item.Analysis.NormalizedComplexity,
                            GetHasAlpha()
                        );
                    }

                    var sourceLayout = item.IsNormalMap
                        ? NormalMapSourceLayoutDetector.Resolve(item.Source, item.Source.format)
                        : NormalMapPreprocessor.SourceLayout.Auto;

                    bool preserveAlpha =
                        item.IsNormalMap
                        && NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                            targetFormat,
                            sourceLayout,
                            GetHasAlpha()
                        );

                    // Normal map pixel preprocessing (CPU-bound, safe to parallelize)
                    if (item.IsNormalMap && resizedTexture.isReadable)
                    {
                        _normalMapPreprocessor.PrepareForCompression(
                            resizedTexture,
                            item.Source.format,
                            targetFormat,
                            preserveAlpha,
                            sourceLayout
                        );
                    }

                    preprocessResults[item.Source] = (
                        resizedTexture,
                        targetFormat,
                        item.IsNormalMap,
                        preserveAlpha,
                        sourceLayout
                    );
                }
            );

            // Step 4: Sequential compression (EditorUtility.CompressTexture is main-thread-only)
            var processedTextures = new Dictionary<Texture2D, Texture2D>();

            foreach (var item in resizeItems)
            {
                var originalTexture = item.Source;

                if (processedTextures.ContainsKey(originalTexture))
                    continue;

                if (
                    !preprocessResults.TryGetValue(
                        originalTexture,
                        out var preprocessed
                    )
                )
                    continue;

                var resizedTexture = preprocessed.Resized;

                // Apply compression (normal map preprocessing already done in parallel step)
                if (resizedTexture.format != preprocessed.TargetFormat)
                {
                    ApplyCompressionOnly(
                        resizedTexture,
                        item.Source.format,
                        preprocessed.TargetFormat,
                        preprocessed.IsNormalMap,
                        preprocessed.PreserveAlpha,
                        preprocessed.SourceLayout
                    );
                }

                resizedTexture.name = originalTexture.name + "_compressed";

                if (enableLogging)
                {
                    var frozenInfo =
                        item.FormatOverride.HasValue
                        && item.FormatOverride.Value != FrozenTextureFormat.Auto
                            ? " [FROZEN]"
                            : "";
                    Debug.Log(
                        $"[{Name}] {originalTexture.name}: "
                            + $"{originalTexture.width}x{originalTexture.height} -> "
                            + $"{resizedTexture.width}x{resizedTexture.height} ({resizedTexture.format}){frozenInfo} "
                            + $"(Complexity: {item.Analysis.NormalizedComplexity:P0}, "
                            + $"Divisor: {item.Analysis.RecommendedDivisor}x)"
                    );
                }

                // Enable mipmap streaming to avoid NDMF warnings
                var serializedTexture = new SerializedObject(resizedTexture);
                var streamingMipmaps = serializedTexture.FindProperty("m_StreamingMipmaps");
                if (streamingMipmaps != null)
                {
                    streamingMipmaps.boolValue = true;
                    serializedTexture.ApplyModifiedPropertiesWithoutUndo();
                }
                else if (!_streamingMipmapsWarningShown)
                {
                    _streamingMipmapsWarningShown = true;
                    Debug.LogWarning(
                        $"[{Name}] Could not enable streaming mipmaps: "
                            + "property 'm_StreamingMipmaps' not found. This may indicate a Unity version difference."
                    );
                }

                ObjectRegistry.RegisterReplacedObject(originalTexture, resizedTexture);

                foreach (var reference in item.Info.References)
                {
                    reference.Material.SetTexture(reference.PropertyName, resizedTexture);
                }

                processedTextures[originalTexture] = resizedTexture;
            }

            if (enableLogging)
            {
                LogSummary(textures, processedTextures);
            }

            return (processedTextures, clonedMaterials);
        }

        /// <summary>
        /// Resolves analysis settings for a texture (frozen override or analysis result).
        /// </summary>
        /// <returns>Resolved analysis and format override, or null if the texture should be skipped.</returns>
        private (TextureAnalysisResult Analysis, FrozenTextureFormat? FormatOverride)? ResolveAnalysis(
            Texture2D originalTexture,
            TextureInfo textureInfo,
            Dictionary<Texture2D, TextureAnalysisResult> analysisResults,
            bool enableLogging
        )
        {
            string assetPath = AssetDatabase.GetAssetPath(originalTexture);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            // Check if texture is frozen (non-skipped frozen textures are still in collection)
            if (
                !string.IsNullOrEmpty(guid)
                && _frozenLookup.TryGetValue(guid, out var frozenSettings)
                && !frozenSettings.Skip
            )
            {
                int divisor = frozenSettings.Divisor;
                Vector2Int resolution = _processor.CalculateNewDimensions(
                    originalTexture.width,
                    originalTexture.height,
                    divisor
                );

                var analysis = new TextureAnalysisResult(0.5f, divisor, resolution);

                if (enableLogging)
                {
                    Debug.Log(
                        $"[{Name}] Using frozen settings for '{originalTexture.name}': "
                            + $"Divisor={divisor}, Format={frozenSettings.Format}"
                    );
                }

                return (analysis, frozenSettings.Format);
            }

            if (!analysisResults.TryGetValue(originalTexture, out var analysisResult))
            {
                Debug.LogWarning(
                    $"[{Name}] Skipping texture '{originalTexture.name}': analysis failed"
                );
                return null;
            }

            return (analysisResult, null);
        }

        /// <summary>
        /// Applies compression only (normal map preprocessing already done in parallel step).
        /// Falls back to standard compression if the primary format fails.
        /// </summary>
        private bool ApplyCompressionOnly(
            Texture2D texture,
            TextureFormat sourceFormat,
            TextureFormat targetFormat,
            bool isNormalMap,
            bool preserveAlpha,
            NormalMapPreprocessor.SourceLayout sourceLayout
        )
        {
            // Save original pixels for fallback restore
            Color32[] originalPixels = null;
            if (isNormalMap && texture.isReadable)
            {
                originalPixels = texture.GetPixels32();
            }

            try
            {
                EditorUtility.CompressTexture(
                    texture,
                    targetFormat,
                    TextureCompressionQuality.Best
                );
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    $"[{Name}] Failed to compress texture to {targetFormat}: {e.Message}. "
                        + "Attempting fallback."
                );

                // Restore original pixels before fallback re-preprocessing
                if (originalPixels != null)
                {
                    texture.SetPixels32(originalPixels);
                    texture.Apply(texture.mipmapCount > 1);
                }

                return ApplyFallbackCompression(
                    texture,
                    sourceFormat,
                    isNormalMap,
                    preserveAlpha,
                    sourceLayout
                );
            }
        }

        /// <summary>
        /// Applies fallback compression when primary compression fails.
        /// </summary>
        private bool ApplyFallbackCompression(
            Texture2D texture,
            TextureFormat sourceFormat,
            bool isNormalMap,
            bool preserveAlpha,
            NormalMapPreprocessor.SourceLayout sourceLayout
        )
        {
            try
            {
                var platform = TextureFormatSelector.ResolvePlatform(_config.TargetPlatform);
                var fallbackFormat =
                    platform == CompressionPlatform.Mobile
                        ? TextureFormat.ASTC_6x6
                        : TextureFormat.DXT5;

                if (isNormalMap)
                {
                    // Recalculate alpha preservation for fallback format
                    // (DXT5/ASTC never preserve alpha, only BC7 can)
                    bool fallbackPreserveAlpha =
                        preserveAlpha && fallbackFormat == TextureFormat.BC7;
                    _normalMapPreprocessor.PrepareForCompression(
                        texture,
                        sourceFormat,
                        fallbackFormat,
                        fallbackPreserveAlpha,
                        sourceLayout
                    );
                }

                EditorUtility.CompressTexture(
                    texture,
                    fallbackFormat,
                    TextureCompressionQuality.Normal
                );
                Debug.Log($"[{Name}] Fallback compression to {fallbackFormat} succeeded.");
                return true;
            }
            catch (System.Exception fallbackEx)
            {
                Debug.LogError(
                    $"[{Name}] Fallback compression also failed: {fallbackEx.Message}. "
                        + "Texture will remain uncompressed."
                );
                return false;
            }
        }

        private void LogSummary(
            Dictionary<Texture2D, TextureInfo> original,
            Dictionary<Texture2D, Texture2D> processed
        )
        {
            long originalSize = 0;
            long compressedSize = 0;

            foreach (var kvp in original)
            {
                var origTex = kvp.Key;
                originalSize += Profiler.GetRuntimeMemorySizeLong(origTex);

                if (processed.TryGetValue(origTex, out var compTex))
                {
                    compressedSize += Profiler.GetRuntimeMemorySizeLong(compTex);
                }
            }

            float savings = originalSize > 0 ? 1f - (float)compressedSize / originalSize : 0f;

            Debug.Log(
                $"[{Name}] Complete: "
                    + $"{originalSize / 1024f / 1024f:F2}MB -> {compressedSize / 1024f / 1024f:F2}MB "
                    + $"({savings:P0} reduction)"
            );
        }
    }
}
