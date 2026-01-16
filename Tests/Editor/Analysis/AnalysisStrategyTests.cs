using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AnalysisStrategyTests
    {
        #region Fast Strategy Tests

        [Test]
        public void FastStrategy_UniformImage_ReturnsLowComplexity()
        {
            var strategy = new FastAnalysisStrategy();
            var data = CreateUniformProcessedData(64, 64, 0.5f);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.LessThan(0.3f));
        }

        [Test]
        public void FastStrategy_NoiseImage_ReturnsHigherComplexity()
        {
            var strategy = new FastAnalysisStrategy();
            var data = CreateNoiseProcessedData(64, 64, 42);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.GreaterThan(0.2f));
        }

        [Test]
        public void FastStrategy_ScoreInValidRange()
        {
            var strategy = new FastAnalysisStrategy();
            var data = CreateNoiseProcessedData(64, 64, 123);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.InRange(0f, 1f));
        }

        #endregion

        #region High Accuracy Strategy Tests

        [Test]
        public void HighAccuracyStrategy_UniformImage_ReturnsLowComplexity()
        {
            var strategy = new HighAccuracyStrategy();
            var data = CreateUniformProcessedData(64, 64, 0.5f);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.LessThan(0.3f));
        }

        [Test]
        public void HighAccuracyStrategy_ScoreInValidRange()
        {
            var strategy = new HighAccuracyStrategy();
            var data = CreateNoiseProcessedData(64, 64, 456);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.InRange(0f, 1f));
        }

        #endregion

        #region Perceptual Strategy Tests

        [Test]
        public void PerceptualStrategy_UniformImage_ReturnsLowComplexity()
        {
            var strategy = new PerceptualStrategy();
            var data = CreateUniformProcessedData(64, 64, 0.5f);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.LessThan(0.3f));
        }

        [Test]
        public void PerceptualStrategy_SmallImage_ReturnsDefaultScore()
        {
            var strategy = new PerceptualStrategy();
            var data = CreateUniformProcessedData(4, 4, 0.5f);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.EqualTo(AnalysisConstants.DefaultComplexityScore));
            Assert.That(result.Summary, Does.Contain("too small"));
        }

        [Test]
        public void PerceptualStrategy_TooFewOpaquePixels_ReturnsDefaultScore()
        {
            var strategy = new PerceptualStrategy();
            var data = CreateUniformProcessedData(16, 16, 0.5f);
            data.OpaqueCount = 50; // Less than MinOpaquePixelsForAnalysis

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.EqualTo(AnalysisConstants.DefaultComplexityScore));
        }

        [Test]
        public void PerceptualStrategy_ScoreInValidRange()
        {
            var strategy = new PerceptualStrategy();
            var data = CreateNoiseProcessedData(64, 64, 789);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.InRange(0f, 1f));
        }

        #endregion

        #region Combined Strategy Tests

        [Test]
        public void CombinedStrategy_EqualWeights_AveragesScores()
        {
            var strategy = new CombinedStrategy(1f, 1f, 1f);
            var data = CreateNoiseProcessedData(64, 64, 42);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.InRange(0f, 1f));
        }

        [Test]
        public void CombinedStrategy_ZeroWeights_UsesEqualWeights()
        {
            var strategy = new CombinedStrategy(0f, 0f, 0f);
            var data = CreateNoiseProcessedData(64, 64, 42);

            var result = strategy.Analyze(data);

            Assert.That(result.Score, Is.InRange(0f, 1f));
            Assert.That(result.Summary, Does.Contain("equal weights"));
        }

        [Test]
        public void CombinedStrategy_SingleWeight_UsesSingleStrategy()
        {
            var fastOnly = new CombinedStrategy(1f, 0f, 0f);
            var fastDirect = new FastAnalysisStrategy();
            var data = CreateNoiseProcessedData(64, 64, 42);

            var combinedResult = fastOnly.Analyze(data);
            var directResult = fastDirect.Analyze(data);

            Assert.That(combinedResult.Score, Is.EqualTo(directResult.Score).Within(0.01f));
        }

        #endregion

        #region Normal Map Analyzer Tests

        [Test]
        public void NormalMapAnalyzer_FlatNormals_ReturnsLowComplexity()
        {
            var analyzer = new NormalMapAnalyzer();
            var data = CreateFlatNormalMapData(64, 64);

            var result = analyzer.Analyze(data);

            Assert.That(result.Score, Is.LessThan(0.2f));
        }

        [Test]
        public void NormalMapAnalyzer_SmallImage_ReturnsDefaultScore()
        {
            var analyzer = new NormalMapAnalyzer();
            var data = CreateFlatNormalMapData(2, 2);

            var result = analyzer.Analyze(data);

            Assert.That(result.Score, Is.EqualTo(AnalysisConstants.DefaultComplexityScore));
        }

        [Test]
        public void NormalMapAnalyzer_VariedNormals_ReturnsHigherComplexity()
        {
            var analyzer = new NormalMapAnalyzer();
            var data = CreateVariedNormalMapData(64, 64);

            var result = analyzer.Analyze(data);

            Assert.That(result.Score, Is.GreaterThan(0f));
        }

        [Test]
        public void NormalMapAnalyzer_ScoreInValidRange()
        {
            var analyzer = new NormalMapAnalyzer();
            var data = CreateVariedNormalMapData(64, 64);

            var result = analyzer.Analyze(data);

            Assert.That(result.Score, Is.InRange(0f, 1f));
        }

        #endregion

        #region Helper Methods

        private static ProcessedPixelData CreateUniformProcessedData(int width, int height, float value)
        {
            int count = width * height;
            Color[] pixels = new Color[count];
            float[] grayscale = new float[count];

            for (int i = 0; i < count; i++)
            {
                pixels[i] = new Color(value, value, value, 1f);
                grayscale[i] = value;
            }

            return new ProcessedPixelData
            {
                OpaquePixels = pixels,
                Grayscale = grayscale,
                Width = width,
                Height = height,
                OpaqueCount = count,
                IsNormalMap = false,
                IsEmission = false
            };
        }

        private static ProcessedPixelData CreateNoiseProcessedData(int width, int height, int seed)
        {
            int count = width * height;
            Color[] pixels = new Color[count];
            float[] grayscale = new float[count];
            System.Random random = new System.Random(seed);

            for (int i = 0; i < count; i++)
            {
                float v = (float)random.NextDouble();
                pixels[i] = new Color(v, v, v, 1f);
                grayscale[i] = v;
            }

            return new ProcessedPixelData
            {
                OpaquePixels = pixels,
                Grayscale = grayscale,
                Width = width,
                Height = height,
                OpaqueCount = count,
                IsNormalMap = false,
                IsEmission = false
            };
        }

        private static ProcessedPixelData CreateFlatNormalMapData(int width, int height)
        {
            int count = width * height;
            Color[] pixels = new Color[count];
            float[] grayscale = new float[count];

            // Flat normal pointing up: (0, 0, 1) encoded as (0.5, 0.5, 1.0)
            Color flatNormal = new Color(0.5f, 0.5f, 1f, 1f);

            for (int i = 0; i < count; i++)
            {
                pixels[i] = flatNormal;
                grayscale[i] = 0.5f;
            }

            return new ProcessedPixelData
            {
                OpaquePixels = pixels,
                Grayscale = grayscale,
                Width = width,
                Height = height,
                OpaqueCount = count,
                IsNormalMap = true,
                IsEmission = false
            };
        }

        private static ProcessedPixelData CreateVariedNormalMapData(int width, int height)
        {
            int count = width * height;
            Color[] pixels = new Color[count];
            float[] grayscale = new float[count];
            System.Random random = new System.Random(42);

            for (int i = 0; i < count; i++)
            {
                // Random normals encoded as RGB
                float r = (float)random.NextDouble();
                float g = (float)random.NextDouble();
                float b = (float)random.NextDouble() * 0.5f + 0.5f; // Z always positive
                pixels[i] = new Color(r, g, b, 1f);
                grayscale[i] = (r + g + b) / 3f;
            }

            return new ProcessedPixelData
            {
                OpaquePixels = pixels,
                Grayscale = grayscale,
                Width = width,
                Height = height,
                OpaqueCount = count,
                IsNormalMap = true,
                IsEmission = false
            };
        }

        #endregion
    }
}
