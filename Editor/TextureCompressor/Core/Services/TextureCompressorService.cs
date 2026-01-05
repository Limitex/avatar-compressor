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

        public void Compress(GameObject root, bool enableLogging)
        {
            MaterialCloner.CloneMaterials(root);

            var textures = _collector.Collect(root);

            if (textures.Count == 0)
            {
                if (enableLogging)
                {
                    Debug.Log($"[{Name}] No textures found to process.");
                }
                return;
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
                      $"{originalSize / 1024f / 1024f:F2}MB → {compressedSize / 1024f / 1024f:F2}MB " +
                      $"({savings:P0} reduction)");
        }
    }
}
