using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.limitex.avatar.compressor.editor.texture
{
    /// <summary>
    /// CPU-based texture analysis backend.
    /// Reads pixels via RenderTexture GPU→CPU readback, then runs
    /// analysis strategies on thread pool threads via Parallel.ForEach.
    /// </summary>
    public class CpuAnalysisBackend : ITextureAnalysisBackend
    {
        private readonly ITextureComplexityAnalyzer _standardAnalyzer;
        private readonly ITextureComplexityAnalyzer _normalMapAnalyzer;
        private readonly TextureProcessor _processor;

        public CpuAnalysisBackend(
            ITextureComplexityAnalyzer standardAnalyzer,
            ITextureComplexityAnalyzer normalMapAnalyzer,
            TextureProcessor processor
        )
        {
            _standardAnalyzer = standardAnalyzer;
            _normalMapAnalyzer = normalMapAnalyzer;
            _processor = processor;
        }

        /// <summary>
        /// Analyzes a batch of textures in parallel.
        /// Phase 1 (main thread): reads pixels one texture at a time, immediately
        /// samples down to analysis resolution, then releases the full-resolution
        /// array so it can be GC'd before the next texture is read.
        /// Phase 2 (thread pool): runs analysis strategies on the small sampled data.
        /// Each work item's pixel data is released immediately after analysis to
        /// keep peak memory proportional to the degree of parallelism, not to the
        /// total number of textures.
        /// </summary>
        public Dictionary<Texture2D, float> AnalyzeBatch(
            Dictionary<Texture2D, TextureInfo> textures
        )
        {
            // Phase 1: Read pixels one at a time and downsample immediately.
            // Only the small ProcessedPixelData (~512×512) is retained; full-resolution
            // Color[] is released after each texture, keeping peak memory at O(1 texture).
            // Texture name is cached here because Unity API (Object.name) cannot
            // be called from background threads used by Parallel.ForEach.
            var workItems = new List<AnalysisWorkItem>();

            var results = new ConcurrentDictionary<Texture2D, float>();

            foreach (var kvp in textures)
            {
                var texture = kvp.Key;
                var info = kvp.Value;

                if (texture == null)
                    continue;

                var pixels = _processor.GetReadablePixelsSingle(texture);

                if (pixels == null || pixels.Length == 0)
                {
                    Debug.LogWarning(
                        $"[TextureCompressor] No pixel data for '{texture.name}', using default analysis"
                    );
                    results[texture] = AnalysisConstants.DefaultComplexityScore;
                    continue;
                }

                // Downsample and preprocess immediately, then discard full-res pixels
                var processed = PreprocessPixels(
                    pixels,
                    texture.width,
                    texture.height,
                    info.IsNormalMap
                );

                var analyzer = info.IsNormalMap ? _normalMapAnalyzer : _standardAnalyzer;
                workItems.Add(
                    new AnalysisWorkItem
                    {
                        Texture = texture,
                        TextureName = texture.name,
                        Data = processed,
                        IsNormalMap = info.IsNormalMap,
                        Analyzer = analyzer,
                    }
                );
            }

            // Phase 2: Truly parallel analysis on small sampled data (no lock contention)
            // Limit outer parallelism to leave threads for inner parallelism (CombinedStrategy)
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = System.Math.Min(
                    8,
                    System.Math.Max(1, System.Environment.ProcessorCount / 2)
                ),
            };

            Parallel.ForEach(
                workItems,
                parallelOptions,
                item =>
                {
                    try
                    {
                        float score;
                        if (
                            !item.IsNormalMap
                            && item.Data.OpaqueCount
                                < AnalysisConstants.MinOpaquePixelsForStandardAnalysis
                        )
                        {
                            score =
                                AnalysisConstants.DefaultComplexityScore
                                * AnalysisConstants.SparseTexturePenalty;
                        }
                        else
                        {
                            score = item.Analyzer.Analyze(item.Data).Score;
                        }

                        results[item.Texture] = score;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning(
                            $"[TextureCompressor] CPU analysis failed for '{item.TextureName}': {e.Message}"
                        );
                        results[item.Texture] = AnalysisConstants.DefaultComplexityScore;
                    }
                    finally
                    {
                        // Release pixel arrays immediately so they can be GC'd while
                        // other items are still being analyzed.  Peak retained memory
                        // becomes O(parallelism) instead of O(total textures).
                        item.Data = default;
                    }
                }
            );

            return new Dictionary<Texture2D, float>(results);
        }

        /// <summary>
        /// Mutable work item that allows pixel data to be released after analysis.
        /// A class (not struct/tuple) so that Parallel.ForEach can null out the Data
        /// field to release Color[] and float[] references while other items are
        /// still being processed.
        /// </summary>
        private sealed class AnalysisWorkItem
        {
            public Texture2D Texture;
            public string TextureName;
            public ProcessedPixelData Data;
            public bool IsNormalMap;
            public ITextureComplexityAnalyzer Analyzer;
        }

        /// <summary>
        /// Downsamples full-resolution pixels and preprocesses them into analysis-ready data.
        /// Called on the main thread so the full-res Color[] can be released immediately after.
        /// </summary>
        private static ProcessedPixelData PreprocessPixels(
            Color[] pixels,
            int width,
            int height,
            bool isNormalMap
        )
        {
            PixelSampler.SampleIfNeeded(
                pixels,
                width,
                height,
                out Color[] sampledPixels,
                out int sampledWidth,
                out int sampledHeight
            );

            int totalSampledPixels = sampledWidth * sampledHeight;

            if (isNormalMap)
            {
                return new ProcessedPixelData
                {
                    OpaquePixels = sampledPixels,
                    Grayscale = AlphaExtractor.ConvertToGrayscale(sampledPixels),
                    Width = sampledWidth,
                    Height = sampledHeight,
                    OpaqueCount = totalSampledPixels,
                    IsNormalMap = true,
                };
            }

            AlphaExtractor.ExtractOpaquePixels(
                sampledPixels,
                sampledWidth,
                sampledHeight,
                out Color[] opaquePixels,
                out float[] grayscale,
                out int opaqueCount
            );

            return new ProcessedPixelData
            {
                OpaquePixels = opaquePixels,
                Grayscale = grayscale,
                Width = sampledWidth,
                Height = sampledHeight,
                OpaqueCount = opaqueCount,
                IsNormalMap = false,
            };
        }
    }
}
