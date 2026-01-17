using NUnit.Framework;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class TextureComplexityResultTests
    {
        #region Single Parameter Constructor Tests

        [Test]
        public void Constructor_ScoreOnly_SetsScore()
        {
            var result = new TextureComplexityResult(0.5f);

            Assert.That(result.Score, Is.EqualTo(0.5f));
        }

        [Test]
        public void Constructor_VeryLowScore_GeneratesAppropiateSummary()
        {
            var result = new TextureComplexityResult(0.1f);

            Assert.That(result.Summary, Does.Contain("Very low").IgnoreCase);
            Assert.That(result.Summary, Does.Contain("heavily compressed").IgnoreCase);
        }

        [Test]
        public void Constructor_LowScore_GeneratesAppropiateSummary()
        {
            var result = new TextureComplexityResult(0.3f);

            Assert.That(result.Summary, Does.Contain("Low").IgnoreCase);
            Assert.That(result.Summary, Does.Contain("suitable").IgnoreCase);
        }

        [Test]
        public void Constructor_MediumScore_GeneratesAppropiateSummary()
        {
            var result = new TextureComplexityResult(0.5f);

            Assert.That(result.Summary, Does.Contain("Medium").IgnoreCase);
            Assert.That(result.Summary, Does.Contain("moderate").IgnoreCase);
        }

        [Test]
        public void Constructor_HighScore_GeneratesAppropiateSummary()
        {
            var result = new TextureComplexityResult(0.7f);

            Assert.That(result.Summary, Does.Contain("High").IgnoreCase);
            Assert.That(result.Summary, Does.Contain("light").IgnoreCase);
        }

        [Test]
        public void Constructor_VeryHighScore_GeneratesAppropiateSummary()
        {
            var result = new TextureComplexityResult(0.9f);

            Assert.That(result.Summary, Does.Contain("Very high").IgnoreCase);
            Assert.That(result.Summary, Does.Contain("minimal").IgnoreCase);
        }

        #endregion

        #region Two Parameter Constructor Tests

        [Test]
        public void Constructor_WithCustomSummary_SetsScore()
        {
            var result = new TextureComplexityResult(0.5f, "Custom summary");

            Assert.That(result.Score, Is.EqualTo(0.5f));
        }

        [Test]
        public void Constructor_WithCustomSummary_SetsSummary()
        {
            var result = new TextureComplexityResult(0.5f, "Custom summary text");

            Assert.That(result.Summary, Is.EqualTo("Custom summary text"));
        }

        [Test]
        public void Constructor_WithNullSummary_SetsNullSummary()
        {
            var result = new TextureComplexityResult(0.5f, null);

            Assert.That(result.Summary, Is.Null);
        }

        [Test]
        public void Constructor_WithEmptySummary_SetsEmptySummary()
        {
            var result = new TextureComplexityResult(0.5f, "");

            Assert.That(result.Summary, Is.Empty);
        }

        #endregion

        #region Boundary Value Tests

        [Test]
        public void Constructor_ZeroScore_IsValid()
        {
            var result = new TextureComplexityResult(0f);

            Assert.That(result.Score, Is.EqualTo(0f));
        }

        [Test]
        public void Constructor_OneScore_IsValid()
        {
            var result = new TextureComplexityResult(1f);

            Assert.That(result.Score, Is.EqualTo(1f));
        }

        [Test]
        public void Constructor_BoundaryAt0_2_VeryLow()
        {
            var result = new TextureComplexityResult(0.19f);

            Assert.That(result.Summary, Does.Contain("Very low").IgnoreCase);
        }

        [Test]
        public void Constructor_BoundaryAt0_2_Low()
        {
            var result = new TextureComplexityResult(0.2f);

            Assert.That(result.Summary, Does.Contain("Low").IgnoreCase);
            Assert.That(result.Summary, Does.Not.Contain("Very low").IgnoreCase);
        }

        [Test]
        public void Constructor_BoundaryAt0_4_Low()
        {
            var result = new TextureComplexityResult(0.39f);

            Assert.That(result.Summary, Does.Contain("Low").IgnoreCase);
        }

        [Test]
        public void Constructor_BoundaryAt0_4_Medium()
        {
            var result = new TextureComplexityResult(0.4f);

            Assert.That(result.Summary, Does.Contain("Medium").IgnoreCase);
        }

        [Test]
        public void Constructor_BoundaryAt0_6_Medium()
        {
            var result = new TextureComplexityResult(0.59f);

            Assert.That(result.Summary, Does.Contain("Medium").IgnoreCase);
        }

        [Test]
        public void Constructor_BoundaryAt0_6_High()
        {
            var result = new TextureComplexityResult(0.6f);

            Assert.That(result.Summary, Does.Contain("High").IgnoreCase);
            Assert.That(result.Summary, Does.Not.Contain("Very high").IgnoreCase);
        }

        [Test]
        public void Constructor_BoundaryAt0_8_High()
        {
            var result = new TextureComplexityResult(0.79f);

            Assert.That(result.Summary, Does.Contain("High").IgnoreCase);
            Assert.That(result.Summary, Does.Not.Contain("Very high").IgnoreCase);
        }

        [Test]
        public void Constructor_BoundaryAt0_8_VeryHigh()
        {
            var result = new TextureComplexityResult(0.8f);

            Assert.That(result.Summary, Does.Contain("Very high").IgnoreCase);
        }

        #endregion

        #region IAnalysisResult Implementation Tests

        [Test]
        public void Score_ImplementsIAnalysisResult()
        {
            var result = new TextureComplexityResult(0.5f);

            Assert.That(result.Score, Is.EqualTo(0.5f));
        }

        [Test]
        public void Summary_ImplementsIAnalysisResult()
        {
            var result = new TextureComplexityResult(0.5f);

            Assert.That(result.Summary, Is.Not.Null.And.Not.Empty);
        }

        #endregion

        #region Struct Behavior Tests

        [Test]
        public void Struct_DefaultValue_HasZeroScoreAndNullSummary()
        {
            var result = default(TextureComplexityResult);

            Assert.That(result.Score, Is.EqualTo(0f));
            Assert.That(result.Summary, Is.Null);
        }

        [Test]
        public void Struct_Assignment_CopiesValues()
        {
            var original = new TextureComplexityResult(0.75f, "Test summary");
            var copy = original;

            Assert.That(copy.Score, Is.EqualTo(original.Score));
            Assert.That(copy.Summary, Is.EqualTo(original.Summary));
        }

        #endregion
    }
}
