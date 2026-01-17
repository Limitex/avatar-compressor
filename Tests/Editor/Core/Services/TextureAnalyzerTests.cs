using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureAnalyzerTests
    {
        private TextureProcessor _processor;
        private ComplexityCalculator _complexityCalc;

        [SetUp]
        public void SetUp()
        {
            _processor = new TextureProcessor(32, 2048, true);
            _complexityCalc = new ComplexityCalculator(0.7f, 0.3f, 1, 8);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithFastStrategy_CreatesAnalyzer()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            Assert.IsNotNull(analyzer);
        }

        [Test]
        public void Constructor_WithHighAccuracyStrategy_CreatesAnalyzer()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.HighAccuracy,
                0f, 1f, 0f,
                _processor,
                _complexityCalc);

            Assert.IsNotNull(analyzer);
        }

        [Test]
        public void Constructor_WithPerceptualStrategy_CreatesAnalyzer()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Perceptual,
                0f, 0f, 1f,
                _processor,
                _complexityCalc);

            Assert.IsNotNull(analyzer);
        }

        [Test]
        public void Constructor_WithCombinedStrategy_CreatesAnalyzer()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Combined,
                1f, 1f, 1f,
                _processor,
                _complexityCalc);

            Assert.IsNotNull(analyzer);
        }

        [Test]
        public void Constructor_WithZeroWeights_CreatesAnalyzer()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Combined,
                0f, 0f, 0f,
                _processor,
                _complexityCalc);

            Assert.IsNotNull(analyzer);
        }

        #endregion

        #region AnalyzeBatch Tests

        [Test]
        public void AnalyzeBatch_EmptyDictionary_ReturnsEmptyResult()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var textures = new Dictionary<Texture2D, TextureInfo>();
            var result = analyzer.AnalyzeBatch(textures);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void AnalyzeBatch_SingleTexture_ReturnsOneResult()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var texture = CreateTestTexture(64, 64);
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { texture, new TextureInfo { IsNormalMap = false, IsEmission = false } }
            };

            var result = analyzer.AnalyzeBatch(textures);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void AnalyzeBatch_MultipleTextures_ReturnsAllResults()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var texture1 = CreateTestTexture(64, 64);
            var texture2 = CreateTestTexture(128, 128);
            var texture3 = CreateTestTexture(256, 256);

            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { texture1, new TextureInfo { IsNormalMap = false, IsEmission = false } },
                { texture2, new TextureInfo { IsNormalMap = false, IsEmission = false } },
                { texture3, new TextureInfo { IsNormalMap = false, IsEmission = false } }
            };

            var result = analyzer.AnalyzeBatch(textures);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsKey(texture1));
            Assert.IsTrue(result.ContainsKey(texture2));
            Assert.IsTrue(result.ContainsKey(texture3));

            Object.DestroyImmediate(texture1);
            Object.DestroyImmediate(texture2);
            Object.DestroyImmediate(texture3);
        }

        [Test]
        public void AnalyzeBatch_NormalMapTexture_UsesNormalMapAnalyzer()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var texture = CreateFlatNormalMapTexture(64, 64);
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { texture, new TextureInfo { IsNormalMap = true, IsEmission = false } }
            };

            var result = analyzer.AnalyzeBatch(textures);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            // Normal maps with flat normals should have low complexity
            Assert.That(result[texture].NormalizedComplexity, Is.LessThan(0.5f));

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void AnalyzeBatch_EmissionTexture_AppliesBoost()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var textureNormal = CreateTestTexture(64, 64);
            var textureEmission = CreateTestTexture(64, 64);

            var texturesNormal = new Dictionary<Texture2D, TextureInfo>
            {
                { textureNormal, new TextureInfo { IsNormalMap = false, IsEmission = false } }
            };
            var texturesEmission = new Dictionary<Texture2D, TextureInfo>
            {
                { textureEmission, new TextureInfo { IsNormalMap = false, IsEmission = true } }
            };

            var resultNormal = analyzer.AnalyzeBatch(texturesNormal);
            var resultEmission = analyzer.AnalyzeBatch(texturesEmission);

            // Emission should have lower or equal complexity due to 10% boost
            Assert.That(resultEmission[textureEmission].NormalizedComplexity,
                Is.LessThanOrEqualTo(resultNormal[textureNormal].NormalizedComplexity));

            Object.DestroyImmediate(textureNormal);
            Object.DestroyImmediate(textureEmission);
        }

        [Test]
        public void AnalyzeBatch_MixedTextureTypes_HandlesCorrectly()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var mainTex = CreateTestTexture(64, 64);
            var normalTex = CreateFlatNormalMapTexture(64, 64);
            var emissionTex = CreateTestTexture(64, 64);

            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { mainTex, new TextureInfo { IsNormalMap = false, IsEmission = false } },
                { normalTex, new TextureInfo { IsNormalMap = true, IsEmission = false } },
                { emissionTex, new TextureInfo { IsNormalMap = false, IsEmission = true } }
            };

            var result = analyzer.AnalyzeBatch(textures);

            Assert.AreEqual(3, result.Count);

            Object.DestroyImmediate(mainTex);
            Object.DestroyImmediate(normalTex);
            Object.DestroyImmediate(emissionTex);
        }

        #endregion

        #region Result Validation Tests

        [Test]
        public void AnalyzeBatch_Result_HasValidComplexity()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var texture = CreateNoiseTexture(64, 64, 42);
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { texture, new TextureInfo { IsNormalMap = false, IsEmission = false } }
            };

            var result = analyzer.AnalyzeBatch(textures);

            Assert.That(result[texture].NormalizedComplexity, Is.InRange(0f, 1f));

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void AnalyzeBatch_Result_HasValidDivisor()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var texture = CreateNoiseTexture(64, 64, 42);
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { texture, new TextureInfo { IsNormalMap = false, IsEmission = false } }
            };

            var result = analyzer.AnalyzeBatch(textures);

            Assert.That(result[texture].RecommendedDivisor, Is.GreaterThanOrEqualTo(1));
            Assert.That(result[texture].RecommendedDivisor, Is.LessThanOrEqualTo(8));

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void AnalyzeBatch_Result_HasValidResolution()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var texture = CreateNoiseTexture(128, 128, 42);
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { texture, new TextureInfo { IsNormalMap = false, IsEmission = false } }
            };

            var result = analyzer.AnalyzeBatch(textures);

            Assert.That(result[texture].RecommendedResolution.x, Is.GreaterThanOrEqualTo(32));
            Assert.That(result[texture].RecommendedResolution.y, Is.GreaterThanOrEqualTo(32));
            Assert.That(result[texture].RecommendedResolution.x, Is.LessThanOrEqualTo(2048));
            Assert.That(result[texture].RecommendedResolution.y, Is.LessThanOrEqualTo(2048));

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void AnalyzeBatch_UniformTexture_ReturnsLowComplexity()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var texture = CreateUniformTexture(64, 64, Color.gray);
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { texture, new TextureInfo { IsNormalMap = false, IsEmission = false } }
            };

            var result = analyzer.AnalyzeBatch(textures);

            Assert.That(result[texture].NormalizedComplexity, Is.LessThan(0.3f));

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void AnalyzeBatch_NoiseTexture_ReturnsHigherComplexity()
        {
            var analyzer = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var texture = CreateNoiseTexture(64, 64, 42);
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { texture, new TextureInfo { IsNormalMap = false, IsEmission = false } }
            };

            var result = analyzer.AnalyzeBatch(textures);

            Assert.That(result[texture].NormalizedComplexity, Is.GreaterThan(0.1f));

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Strategy Comparison Tests

        [Test]
        public void AnalyzeBatch_DifferentStrategies_ProduceDifferentResults()
        {
            var analyzerFast = new TextureAnalyzer(
                AnalysisStrategyType.Fast,
                1f, 0f, 0f,
                _processor,
                _complexityCalc);

            var analyzerHighAccuracy = new TextureAnalyzer(
                AnalysisStrategyType.HighAccuracy,
                0f, 1f, 0f,
                _processor,
                _complexityCalc);

            var texture = CreateNoiseTexture(64, 64, 42);
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                { texture, new TextureInfo { IsNormalMap = false, IsEmission = false } }
            };

            var resultFast = analyzerFast.AnalyzeBatch(textures);
            var resultHighAccuracy = analyzerHighAccuracy.AnalyzeBatch(textures);

            // Both should be valid but may differ
            Assert.That(resultFast[texture].NormalizedComplexity, Is.InRange(0f, 1f));
            Assert.That(resultHighAccuracy[texture].NormalizedComplexity, Is.InRange(0f, 1f));

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Helper Methods

        private static Texture2D CreateTestTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            System.Random random = new System.Random(123);

            for (int i = 0; i < pixels.Length; i++)
            {
                float v = (float)random.NextDouble();
                pixels[i] = new Color(v, v, v, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateUniformTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateNoiseTexture(int width, int height, int seed)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            System.Random random = new System.Random(seed);

            for (int i = 0; i < pixels.Length; i++)
            {
                float r = (float)random.NextDouble();
                float g = (float)random.NextDouble();
                float b = (float)random.NextDouble();
                pixels[i] = new Color(r, g, b, 1f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateFlatNormalMapTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            // Flat normal pointing up: (0, 0, 1) encoded as (0.5, 0.5, 1.0)
            Color flatNormal = new Color(0.5f, 0.5f, 1f, 1f);

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = flatNormal;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion
    }
}
