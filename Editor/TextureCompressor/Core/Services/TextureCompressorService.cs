using System.Collections.Generic;
using System.Linq;
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

        // Flag to avoid repeating the same warning for every texture (per-build instance)
        private bool _streamingMipmapsWarningShown;

        // Flag to avoid repeating build context warning (per-build instance)
        private bool _buildContextWarningShown;

        public TextureCompressorService(
            TextureCompressor config,
            AnalysisBackendPreference backendPreference = AnalysisBackendPreference.Auto
        )
        {
            _config = config;

            _frozenLookup = FrozenTextureSettings.BuildLookup(config.FrozenTextures);

            _collector = new TextureCollector(
                config.MinSourceSize,
                config.SkipIfSmallerThan,
                config.ProcessMainTextures,
                config.ProcessNormalMaps,
                config.ProcessEmissionMaps,
                config.ProcessOtherTextures,
                config.SkipUnknownUncompressedTextures,
                config.ExcludedPaths,
                config.ExcludedTextures,
                config.FrozenTextures
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
                backendPreference
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

            var rawScores = _analyzer.AnalyzeBatch(textures);

            // Build analysis results (score → divisor → resolution) in the service layer
            var analysisResults = new Dictionary<Texture2D, TextureAnalysisResult>();
            foreach (var kvp in rawScores)
            {
                var texture = kvp.Key;
                if (!textures.TryGetValue(texture, out var info))
                    continue;

                analysisResults[texture] = AnalysisResultHelper.BuildResult(
                    kvp.Value,
                    texture.width,
                    texture.height,
                    info.IsEmission,
                    info.IsNormalMap,
                    _complexityCalc,
                    _processor
                );
            }

            // Process each texture through the full pipeline (resize → preprocess → compress → register)
            // one at a time to keep peak memory at O(1) intermediate textures.
            var processedTextures = new Dictionary<Texture2D, Texture2D>();

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

                var analysis = resolved.Value.Analysis;
                var formatOverride = resolved.Value.FormatOverride;
                var sourceFormat = originalTexture.format;
                bool isNormalMap = textureInfo.IsNormalMap;

                // Resize (lock acquired and released inside ResizeSingle)
                var resizedTexture = _processor.ResizeSingle(
                    originalTexture,
                    analysis,
                    isNormalMap
                );
                if (resizedTexture == null)
                    continue;

                // Always detect alpha on the resized texture (the actual data being compressed).
                // Original-resolution alpha regions may be lost during downscaling, so checking
                // the resized output avoids choosing an alpha-capable format (BC7 at 8bpp)
                // when the compressed texture has no meaningful alpha (DXT1 at 4bpp suffices).
                bool hasAlpha = TextureFormatSelector.HasSignificantAlpha(resizedTexture);

                var targetFormat = _formatSelector.ResolveTargetFormat(
                    sourceFormat,
                    isNormalMap,
                    analysis.NormalizedComplexity,
                    hasAlpha,
                    formatOverride
                );

                var sourceLayout = isNormalMap
                    ? NormalMapSourceLayoutDetector.Resolve(originalTexture, sourceFormat)
                    : NormalMapPreprocessor.SourceLayout.Auto;

                bool preserveAlpha =
                    isNormalMap
                    && NormalMapPreprocessor.ShouldPreserveSemanticAlpha(
                        targetFormat,
                        sourceLayout,
                        hasAlpha
                    );

                // Save original pixels BEFORE destructive normal map preprocessing (for fallback restore)
                Color32[] originalPixels = null;
                // isReadable is always true for BlitResize output (new Texture2D); guard is defensive
                if (isNormalMap && resizedTexture.isReadable)
                {
                    originalPixels = resizedTexture.GetPixels32();
                }

                // Preprocess + Compress (both inside protection so PrepareForCompression
                // failures also trigger pixel restore and fallback)
                if (resizedTexture.format != targetFormat)
                {
                    if (
                        !TryPreprocessAndCompress(
                            resizedTexture,
                            sourceFormat,
                            targetFormat,
                            isNormalMap,
                            preserveAlpha,
                            sourceLayout
                        )
                    )
                    {
                        // Primary compression failed — restore pixels and attempt fallback
                        if (originalPixels != null)
                        {
                            resizedTexture.SetPixels32(originalPixels);
                            resizedTexture.Apply(resizedTexture.mipmapCount > 1);
                        }

                        if (
                            !ApplyFallbackCompression(
                                resizedTexture,
                                sourceFormat,
                                isNormalMap,
                                sourceLayout
                            )
                        )
                        {
                            Object.DestroyImmediate(resizedTexture);
                            continue;
                        }
                    }
                }

                resizedTexture.name = originalTexture.name + "_compressed";

                if (enableLogging)
                {
                    var frozenInfo =
                        formatOverride.HasValue && formatOverride.Value != FrozenTextureFormat.Auto
                            ? " [FROZEN]"
                            : "";
                    Debug.Log(
                        $"[{Name}] {originalTexture.name}: "
                            + $"{originalTexture.width}x{originalTexture.height} -> "
                            + $"{resizedTexture.width}x{resizedTexture.height} ({resizedTexture.format}){frozenInfo} "
                            + $"(Complexity: {analysis.NormalizedComplexity:P0}, "
                            + $"Divisor: {analysis.RecommendedDivisor}x)"
                    );
                }

                // Enable mipmap streaming to avoid NDMF warnings
                using var serializedTexture = new SerializedObject(resizedTexture);
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

                foreach (var reference in textureInfo.References)
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
        private (
            TextureAnalysisResult Analysis,
            FrozenTextureFormat? FormatOverride
        )? ResolveAnalysis(
            Texture2D originalTexture,
            TextureInfo textureInfo,
            Dictionary<Texture2D, TextureAnalysisResult> analysisResults,
            bool enableLogging
        )
        {
            string guid = textureInfo.AssetGuid;

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

                // Alpha detection deferred to after resize (resized textures are always readable)
                var analysis = new TextureAnalysisResult(
                    AnalysisConstants.DefaultComplexityScore,
                    divisor,
                    resolution
                );

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
        /// Applies normal map preprocessing (if applicable) and compresses the texture.
        /// Both steps are protected so that a failure in either triggers fallback.
        /// </summary>
        /// <returns>True if preprocessing and compression both succeeded, false otherwise.</returns>
        private bool TryPreprocessAndCompress(
            Texture2D texture,
            TextureFormat sourceFormat,
            TextureFormat targetFormat,
            bool isNormalMap,
            bool preserveAlpha,
            NormalMapPreprocessor.SourceLayout sourceLayout
        )
        {
            try
            {
                if (isNormalMap)
                {
                    _normalMapPreprocessor.PrepareForCompression(
                        texture,
                        sourceFormat,
                        targetFormat,
                        preserveAlpha,
                        sourceLayout
                    );
                }

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
                return false;
            }
        }

        /// <summary>
        /// Applies fallback compression when primary compression fails.
        /// </summary>
        private bool ApplyFallbackCompression(
            Texture2D texture,
            TextureFormat sourceFormat,
            bool isNormalMap,
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
                    // Fallback formats (DXT5/ASTC) never preserve alpha; only BC7 can.
                    _normalMapPreprocessor.PrepareForCompression(
                        texture,
                        sourceFormat,
                        fallbackFormat,
                        preserveAlpha: false,
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
