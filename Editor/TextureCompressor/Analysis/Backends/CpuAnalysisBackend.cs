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
        private readonly ComplexityCalculator _complexityCalc;

        public CpuAnalysisBackend(
            ITextureComplexityAnalyzer standardAnalyzer,
            ITextureComplexityAnalyzer normalMapAnalyzer,
            TextureProcessor processor,
            ComplexityCalculator complexityCalc
        )
        {
            _standardAnalyzer = standardAnalyzer;
            _normalMapAnalyzer = normalMapAnalyzer;
            _processor = processor;
            _complexityCalc = complexityCalc;
        }

        /// <summary>
        /// Analyzes a batch of textures in parallel.
        /// Pixel data is pre-loaded in a single lock scope on the main thread,
        /// then analysis runs in true parallel with no lock contention.
        /// </summary>
        public Dictionary<Texture2D, TextureAnalysisResult> AnalyzeBatch(
            Dictionary<Texture2D, TextureInfo> textures
        )
        {
            // Phase 1: Batch pixel reading on main thread (single lock acquisition)
            var allPixels = _processor.GetReadablePixelsBatch(textures.Keys);

            // Phase 2: Build analysis work items from pre-loaded pixel data
            var pixelDataList =
                new List<(
                    Texture2D Texture,
                    TexturePixelData Data,
                    ITextureComplexityAnalyzer Analyzer
                )>();

            foreach (var kvp in textures)
            {
                var texture = kvp.Key;
                var info = kvp.Value;

                if (
                    !allPixels.TryGetValue(texture, out var pixels)
                    || pixels == null
                    || pixels.Length == 0
                )
                    continue;

                var data = new TexturePixelData
                {
                    Texture = texture,
                    Pixels = pixels,
                    Width = texture.width,
                    Height = texture.height,
                    IsNormalMap = info.IsNormalMap,
                    IsEmission = info.IsEmission,
                };

                var analyzer = info.IsNormalMap ? _normalMapAnalyzer : _standardAnalyzer;
                pixelDataList.Add((texture, data, analyzer));
            }

            // Phase 3: Truly parallel analysis (no lock contention)
            // Limit outer parallelism to leave threads for inner parallelism (CombinedStrategy)
            var results = new ConcurrentDictionary<Texture2D, TextureAnalysisResult>();
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = System.Math.Max(1, System.Environment.ProcessorCount / 2),
            };

            Parallel.ForEach(
                pixelDataList,
                parallelOptions,
                item =>
                {
                    var result = AnalyzeSingle(item.Data, item.Analyzer);
                    results[item.Texture] = result;
                }
            );

            var output = new Dictionary<Texture2D, TextureAnalysisResult>();
            foreach (var kvp in results)
            {
                output[kvp.Key] = kvp.Value;
            }
            return output;
        }

        /// <summary>
        /// Analyzes a single texture's complexity.
        /// </summary>
        private TextureAnalysisResult AnalyzeSingle(
            TexturePixelData data,
            ITextureComplexityAnalyzer analyzer
        )
        {
            PixelSampler.SampleIfNeeded(
                data.Pixels,
                data.Width,
                data.Height,
                out Color[] sampledPixels,
                out int sampledWidth,
                out int sampledHeight
            );

            TextureComplexityResult complexityResult;
            int totalSampledPixels = sampledWidth * sampledHeight;

            if (data.IsNormalMap)
            {
                // Normal maps use all pixels (no alpha extraction)
                var processedData = new ProcessedPixelData
                {
                    OpaquePixels = sampledPixels,
                    Grayscale = AlphaExtractor.ConvertToGrayscale(sampledPixels),
                    Width = sampledWidth,
                    Height = sampledHeight,
                    OpaqueCount = totalSampledPixels,
                    IsNormalMap = true,
                };
                complexityResult = analyzer.Analyze(processedData);
            }
            else
            {
                // Extract opaque pixels while preserving 2D structure
                AlphaExtractor.ExtractOpaquePixels(
                    sampledPixels,
                    sampledWidth,
                    sampledHeight,
                    out Color[] opaquePixels,
                    out float[] grayscale,
                    out int opaqueCount
                );

                if (opaqueCount < AnalysisConstants.MinOpaquePixelsForStandardAnalysis)
                {
                    // Too few opaque pixels for meaningful analysis
                    complexityResult = new TextureComplexityResult(
                        AnalysisConstants.DefaultComplexityScore * 0.2f,
                        "Too few opaque pixels for analysis"
                    );
                }
                else
                {
                    var processedData = new ProcessedPixelData
                    {
                        OpaquePixels = opaquePixels,
                        Grayscale = grayscale,
                        Width = sampledWidth,
                        Height = sampledHeight,
                        OpaqueCount = opaqueCount,
                        IsNormalMap = false,
                    };
                    complexityResult = analyzer.Analyze(processedData);
                }
            }

            return AnalysisResultHelper.BuildResult(
                complexityResult.Score,
                data.Width,
                data.Height,
                data.IsEmission,
                data.IsNormalMap,
                CheckSignificantAlpha(data.Pixels),
                _complexityCalc,
                _processor
            );
        }

        /// <summary>
        /// Checks if the pixel data contains significant alpha using sampling.
        /// Replicates TextureFormatSelector.HasSignificantAlpha logic on pre-loaded pixels
        /// to avoid redundant GPU readback.
        /// </summary>
        private static bool CheckSignificantAlpha(Color[] pixels)
        {
            int sampleCount = Mathf.Min(pixels.Length, 10000);
            int step = Mathf.Max(1, pixels.Length / sampleCount);
            float threshold = AnalysisConstants.SignificantAlphaThreshold / 255f;

            for (int i = 0; i < pixels.Length; i += step)
            {
                if (pixels[i].a < threshold)
                    return true;
            }

            return false;
        }
    }
}
