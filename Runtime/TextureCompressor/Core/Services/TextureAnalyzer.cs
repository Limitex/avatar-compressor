using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.limitex.avatar.compressor.texture
{
    /// <summary>
    /// Service for analyzing texture complexity in batch.
    /// Shared between Plugin and Editor to avoid code duplication.
    /// </summary>
    public class TextureAnalyzer
    {
        private readonly ITextureComplexityAnalyzer _standardAnalyzer;
        private readonly ITextureComplexityAnalyzer _normalMapAnalyzer;
        private readonly TextureResizer _resizer;
        private readonly ComplexityCalculator _complexityCalc;

        public TextureAnalyzer(
            AnalysisStrategyType strategy,
            float fastWeight,
            float highAccuracyWeight,
            float perceptualWeight,
            TextureResizer resizer,
            ComplexityCalculator complexityCalc)
        {
            _standardAnalyzer = AnalyzerFactory.Create(strategy, fastWeight, highAccuracyWeight, perceptualWeight);
            _normalMapAnalyzer = AnalyzerFactory.CreateNormalMapAnalyzer();
            _resizer = resizer;
            _complexityCalc = complexityCalc;
        }

        /// <summary>
        /// Analyzes a batch of textures in parallel.
        /// </summary>
        public Dictionary<Texture2D, TextureAnalysisResult> AnalyzeBatch(
            Dictionary<Texture2D, TextureInfo> textures)
        {
            var pixelDataList = new List<(Texture2D Texture, TexturePixelData Data, ITextureComplexityAnalyzer Analyzer)>();

            foreach (var kvp in textures)
            {
                var texture = kvp.Key;
                var info = kvp.Value;
                var pixels = _resizer.GetReadablePixels(texture);

                if (pixels.Length == 0) continue;

                var data = new TexturePixelData
                {
                    Texture = texture,
                    Pixels = pixels,
                    Width = texture.width,
                    Height = texture.height,
                    IsNormalMap = info.IsNormalMap,
                    IsEmission = info.IsEmission
                };

                var analyzer = info.IsNormalMap ? _normalMapAnalyzer : _standardAnalyzer;
                pixelDataList.Add((texture, data, analyzer));
            }

            var results = new ConcurrentDictionary<Texture2D, TextureAnalysisResult>();

            Parallel.ForEach(pixelDataList, item =>
            {
                var result = AnalyzeSingle(item.Data, item.Analyzer);
                results[item.Texture] = result;
            });

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
            ITextureComplexityAnalyzer analyzer)
        {
            PixelSampler.SampleIfNeeded(data.Pixels, data.Width, data.Height,
                out Color[] sampledPixels, out int sampledWidth, out int sampledHeight);

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
                    IsEmission = false
                };
                complexityResult = analyzer.Analyze(processedData);
            }
            else
            {
                // Extract opaque pixels while preserving 2D structure
                AlphaExtractor.ExtractOpaquePixels(sampledPixels, sampledWidth, sampledHeight,
                    out Color[] opaquePixels, out float[] grayscale, out int opaqueCount);

                if (opaqueCount < 100)
                {
                    // Too few opaque pixels for meaningful analysis
                    complexityResult = new TextureComplexityResult(0.1f, "Too few opaque pixels for analysis");
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
                        IsEmission = data.IsEmission
                    };
                    complexityResult = analyzer.Analyze(processedData);

                    if (data.IsEmission)
                    {
                        // Emission maps get a 10% quality boost
                        complexityResult = new TextureComplexityResult(
                            complexityResult.Score * 0.9f,
                            complexityResult.Summary + " (emission boost applied)");
                    }
                }
            }

            float complexity = Mathf.Clamp01(complexityResult.Score);
            int divisor = _complexityCalc.CalculateRecommendedDivisor(complexity);
            Vector2Int resolution = _resizer.CalculateNewDimensions(data.Width, data.Height, divisor);

            return new TextureAnalysisResult(complexity, divisor, resolution);
        }
    }
}
