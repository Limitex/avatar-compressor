using System.Collections.Generic;
using dev.limitex.avatar.compressor;
using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;
using UnityEngine;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AnalysisBackendFactoryTests
    {
        private TextureProcessor _processor;
        private ComplexityCalculator _complexityCalc;

        [SetUp]
        public void SetUp()
        {
            _processor = new TextureProcessor(32, 2048, true);
            _complexityCalc = new ComplexityCalculator(0.7f, 0.3f, 1, 8);
        }

        #region Factory Creation

        [Test]
        public void Create_WithFastStrategy_ReturnsNonNull()
        {
            var backend = CreateBackend(AnalysisStrategyType.Fast);
            Assert.IsNotNull(backend);
        }

        [Test]
        public void Create_WithHighAccuracyStrategy_ReturnsNonNull()
        {
            var backend = CreateBackend(AnalysisStrategyType.HighAccuracy);
            Assert.IsNotNull(backend);
        }

        [Test]
        public void Create_WithPerceptualStrategy_ReturnsNonNull()
        {
            var backend = CreateBackend(AnalysisStrategyType.Perceptual);
            Assert.IsNotNull(backend);
        }

        [Test]
        public void Create_WithCombinedStrategy_ReturnsNonNull()
        {
            var backend = CreateBackend(AnalysisStrategyType.Combined);
            Assert.IsNotNull(backend);
        }

        [Test]
        public void Create_MultipleCalls_ReturnNewInstances()
        {
            var backend1 = CreateBackend(AnalysisStrategyType.Fast);
            var backend2 = CreateBackend(AnalysisStrategyType.Fast);
            Assert.AreNotSame(backend1, backend2);
        }

        #endregion

        #region Functional Validation

        [Test]
        public void Create_Backend_CanAnalyzeEmptyBatch()
        {
            var backend = CreateBackend(AnalysisStrategyType.Fast);
            var textures = new Dictionary<Texture2D, TextureInfo>();

            var result = backend.AnalyzeBatch(textures);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Create_Backend_CanAnalyzeSingleTexture()
        {
            var backend = CreateBackend(AnalysisStrategyType.Fast);
            var texture = CreateUniformTexture(64, 64, Color.gray);
            var textures = new Dictionary<Texture2D, TextureInfo>
            {
                {
                    texture,
                    new TextureInfo { IsNormalMap = false, IsEmission = false }
                },
            };

            var result = backend.AnalyzeBatch(textures);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey(texture));
            Assert.That(result[texture].NormalizedComplexity, Is.InRange(0f, 1f));

            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Helpers

        private ITextureAnalysisBackend CreateBackend(AnalysisStrategyType strategy)
        {
            return AnalysisBackendFactory.Create(strategy, 1f, 0f, 0f, _processor, _complexityCalc);
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

        #endregion
    }
}
