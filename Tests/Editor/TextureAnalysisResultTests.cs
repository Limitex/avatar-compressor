using NUnit.Framework;
using UnityEngine;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureAnalysisResultTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_SetsNormalizedComplexity()
        {
            var result = new TextureAnalysisResult(0.5f, 2, new Vector2Int(512, 512));

            Assert.That(result.NormalizedComplexity, Is.EqualTo(0.5f));
        }

        [Test]
        public void Constructor_SetsRecommendedDivisor()
        {
            var result = new TextureAnalysisResult(0.5f, 4, new Vector2Int(256, 256));

            Assert.That(result.RecommendedDivisor, Is.EqualTo(4));
        }

        [Test]
        public void Constructor_SetsRecommendedResolution()
        {
            var resolution = new Vector2Int(512, 256);
            var result = new TextureAnalysisResult(0.5f, 2, resolution);

            Assert.That(result.RecommendedResolution, Is.EqualTo(resolution));
        }

        #endregion

        #region IAnalysisResult Implementation Tests

        [Test]
        public void Score_ReturnsNormalizedComplexity()
        {
            var result = new TextureAnalysisResult(0.75f, 2, new Vector2Int(512, 512));

            Assert.That(result.Score, Is.EqualTo(result.NormalizedComplexity));
        }

        [Test]
        public void Summary_ContainsComplexityValue()
        {
            var result = new TextureAnalysisResult(0.5f, 2, new Vector2Int(512, 512));

            Assert.That(result.Summary, Does.Contain("50%").Or.Contain("0.5"));
        }

        [Test]
        public void Summary_ContainsDivisor()
        {
            var result = new TextureAnalysisResult(0.5f, 4, new Vector2Int(256, 256));

            Assert.That(result.Summary, Does.Contain("4"));
        }

        [Test]
        public void Summary_ContainsResolution()
        {
            var result = new TextureAnalysisResult(0.5f, 2, new Vector2Int(512, 256));

            Assert.That(result.Summary, Does.Contain("512"));
            Assert.That(result.Summary, Does.Contain("256"));
        }

        #endregion

        #region Boundary Value Tests

        [Test]
        public void Constructor_ZeroComplexity_IsValid()
        {
            var result = new TextureAnalysisResult(0f, 1, new Vector2Int(1024, 1024));

            Assert.That(result.NormalizedComplexity, Is.EqualTo(0f));
        }

        [Test]
        public void Constructor_MaxComplexity_IsValid()
        {
            var result = new TextureAnalysisResult(1f, 1, new Vector2Int(1024, 1024));

            Assert.That(result.NormalizedComplexity, Is.EqualTo(1f));
        }

        [Test]
        public void Constructor_MinDivisor_IsValid()
        {
            var result = new TextureAnalysisResult(0.5f, 1, new Vector2Int(1024, 1024));

            Assert.That(result.RecommendedDivisor, Is.EqualTo(1));
        }

        [Test]
        public void Constructor_LargeDivisor_IsValid()
        {
            var result = new TextureAnalysisResult(0.1f, 16, new Vector2Int(64, 64));

            Assert.That(result.RecommendedDivisor, Is.EqualTo(16));
        }

        #endregion

        #region Struct Behavior Tests

        [Test]
        public void Struct_DefaultValue_HasZeroValues()
        {
            var result = default(TextureAnalysisResult);

            Assert.That(result.NormalizedComplexity, Is.EqualTo(0f));
            Assert.That(result.RecommendedDivisor, Is.EqualTo(0));
            Assert.That(result.RecommendedResolution, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void Struct_Assignment_CopiesValues()
        {
            var original = new TextureAnalysisResult(0.75f, 2, new Vector2Int(512, 512));
            var copy = original;

            Assert.That(copy.NormalizedComplexity, Is.EqualTo(original.NormalizedComplexity));
            Assert.That(copy.RecommendedDivisor, Is.EqualTo(original.RecommendedDivisor));
            Assert.That(copy.RecommendedResolution, Is.EqualTo(original.RecommendedResolution));
        }

        #endregion
    }
}
