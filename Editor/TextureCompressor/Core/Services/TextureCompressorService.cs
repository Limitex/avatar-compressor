using System.Collections.Generic;
using System.Linq;
using dev.limitex.avatar.compressor.common;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace dev.limitex.avatar.compressor.texture
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
        private readonly ComplexityCalculator _complexityCalc;
        private readonly TextureAnalyzer _analyzer;
        private readonly Dictionary<string, FrozenTextureSettings> _frozenLookup;

        // Flag to avoid repeating the same warning for every texture
        private static bool _streamingMipmapsWarningShown;

        // Flag to avoid repeating build context warning
        private static bool _buildContextWarningShown;

        public TextureCompressorService(TextureCompressor config)
        {
            _config = config;

            // Build frozen texture lookup
            _frozenLookup = new Dictionary<string, FrozenTextureSettings>();
            foreach (var frozen in config.FrozenTextures)
            {
                if (!string.IsNullOrEmpty(frozen.TexturePath))
                    _frozenLookup[frozen.TexturePath] = frozen;
            }

            // Get frozen skip paths (textures with Skip=true should be excluded from collection)
            var frozenSkipPaths = config.FrozenTextures
                .Where(f => f.Skip && !string.IsNullOrEmpty(f.TexturePath))
                .Select(f => f.TexturePath);

            _collector = new TextureCollector(
                config.MinSourceSize,
                config.SkipIfSmallerThan,
                config.ProcessMainTextures,
                config.ProcessNormalMaps,
                config.ProcessEmissionMaps,
                config.ProcessOtherTextures,
                frozenSkipPaths
            );

            _processor = new TextureProcessor(
                config.MinResolution,
                config.MaxResolution,
                config.ForcePowerOfTwo,
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
        }

        /// <summary>
        /// Compresses textures in the avatar hierarchy (ICompressor interface).
        /// Does not process animation-referenced materials.
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

            CompressWithAnimationSupport(root, enableLogging, null);
        }

        /// <summary>
        /// Warns if materials appear to be asset files, which indicates usage outside NDMF build context.
        /// In NDMF build context, materials should already be cloned/runtime objects.
        /// </summary>
        private void WarnIfNotInBuildContext(GameObject root)
        {
            if (_buildContextWarningShown) return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;

                    string assetPath = AssetDatabase.GetAssetPath(material);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        _buildContextWarningShown = true;
                        Debug.LogWarning(
                            $"[{Name}] Material '{material.name}' is an asset file ({assetPath}). " +
                            "This suggests usage outside NDMF build context. " +
                            "While original asset files will NOT be modified, the Renderer's material " +
                            "references will be changed. For non-destructive workflow, use the NDMF plugin.");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Compresses textures in the avatar hierarchy with additional materials from animations.
        /// This is an extended version used by TextureCompressorPass for NDMF integration.
        /// Unlike the ICompressor.Compress method, this returns mappings needed for animation curve updates.
        /// </summary>
        /// <param name="root">Avatar root GameObject</param>
        /// <param name="enableLogging">Whether to log progress</param>
        /// <param name="additionalMaterials">Additional materials to process (e.g., from animations)</param>
        /// <returns>Tuple containing processed textures and cloned materials mappings for animation curve updates</returns>
        public (Dictionary<Texture2D, Texture2D> ProcessedTextures, Dictionary<Material, Material> ClonedMaterials) CompressWithAnimationSupport(
            GameObject root,
            bool enableLogging,
            IEnumerable<Material> additionalMaterials)
        {
            // Clone all materials: renderer materials + additional materials (e.g., from animations)
            var clonedMaterials = MaterialCloner.CloneMaterials(root, additionalMaterials);

            // Collect textures from Renderers (now using cloned materials)
            var textures = _collector.Collect(root);

            // Collect textures from additional materials using cloned versions, because:
            // 1. Animation curves will be rewritten to point to cloned materials
            // 2. Texture references need to be updated on cloned materials
            if (additionalMaterials != null)
            {
                var clonedAdditionalMaterials = new List<Material>();
                foreach (var originalMat in additionalMaterials)
                {
                    if (originalMat == null) continue;

                    if (clonedMaterials.TryGetValue(originalMat, out var clonedMat))
                    {
                        clonedAdditionalMaterials.Add(clonedMat);
                    }
                    else
                    {
                        // This should not happen as MaterialCloner.CloneMaterials receives
                        // the same additionalMaterials list. Log warning and skip.
                        Debug.LogWarning(
                            $"[{Name}] Internal error: Material '{originalMat.name}' should have been cloned " +
                            "but wasn't found in clonedMaterials dictionary. Skipping.");
                    }
                }

                _collector.CollectFromMaterials(clonedAdditionalMaterials, textures);
            }

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
            var processedTextures = new Dictionary<Texture2D, Texture2D>();

            foreach (var kvp in textures)
            {
                var originalTexture = kvp.Key;
                var textureInfo = kvp.Value;

                if (processedTextures.ContainsKey(originalTexture)) continue;

                string assetPath = AssetDatabase.GetAssetPath(originalTexture);
                TextureAnalysisResult analysis;
                FrozenTextureSettings frozenSettings = null;
                FrozenTextureFormat? formatOverride = null;

                // Check if texture is frozen (non-skipped frozen textures are still in collection)
                if (_frozenLookup.TryGetValue(assetPath, out frozenSettings) && !frozenSettings.Skip)
                {
                    // Use frozen settings instead of analysis
                    int divisor = frozenSettings.Divisor;
                    Vector2Int resolution = _processor.CalculateNewDimensions(
                        originalTexture.width, originalTexture.height, divisor);

                    // Create analysis result with frozen values
                    // Complexity is set to 0.5 as a neutral value for format selection when format is Auto
                    analysis = new TextureAnalysisResult(0.5f, divisor, resolution);
                    formatOverride = frozenSettings.Format;

                    if (enableLogging)
                    {
                        Debug.Log($"[{Name}] Using frozen settings for '{originalTexture.name}': " +
                                  $"Divisor={divisor}, Format={frozenSettings.Format}");
                    }
                }
                else
                {
                    // Normal analysis path
                    if (!analysisResults.TryGetValue(originalTexture, out analysis))
                    {
                        Debug.LogWarning($"[{Name}] Skipping texture '{originalTexture.name}': analysis failed");
                        continue;
                    }
                }

                var compressedTexture = _processor.Resize(originalTexture, analysis, enableLogging, textureInfo.IsNormalMap, formatOverride);
                compressedTexture.name = originalTexture.name + "_compressed";

                // Enable mipmap streaming to avoid NDMF warnings
                var serializedTexture = new SerializedObject(compressedTexture);
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
                        $"[{Name}] Could not enable streaming mipmaps: " +
                        "property 'm_StreamingMipmaps' not found. This may indicate a Unity version difference.");
                }

                // Register the texture replacement in ObjectRegistry so that subsequent NDMF plugins
                // can track which original texture was replaced. This maintains proper reference
                // tracking across the build pipeline for tools like TexTransTool and Avatar Optimizer.
                ObjectRegistry.RegisterReplacedObject(originalTexture, compressedTexture);

                processedTextures[originalTexture] = compressedTexture;

                foreach (var reference in textureInfo.References)
                {
                    reference.Material.SetTexture(reference.PropertyName, compressedTexture);
                }
            }

            if (enableLogging)
            {
                LogSummary(textures, processedTextures);
            }

            return (processedTextures, clonedMaterials);
        }

        private void LogSummary(
            Dictionary<Texture2D, TextureInfo> original,
            Dictionary<Texture2D, Texture2D> processed)
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

            Debug.Log($"[{Name}] Complete: " +
                      $"{originalSize / 1024f / 1024f:F2}MB -> {compressedSize / 1024f / 1024f:F2}MB " +
                      $"({savings:P0} reduction)");
        }
    }
}
