using dev.limitex.avatar.compressor.editor.texture;
using NUnit.Framework;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AnalysisResultHelperTests
    {
        private TextureProcessor _processor;
        private ComplexityCalculator _complexityCalc;

        [SetUp]
        public void SetUp()
        {
            _processor = new TextureProcessor(32, 2048, true);
            _complexityCalc = new ComplexityCalculator(0.7f, 0.3f, 1, 8);
        }

        #region Score Clamping

        [Test]
        public void BuildResult_NormalScore_ReturnsClamped()
        {
            var result = Build(score: 0.5f);
            Assert.That(result.NormalizedComplexity, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void BuildResult_NegativeScore_ClampsToZero()
        {
            var result = Build(score: -0.5f);
            Assert.That(result.NormalizedComplexity, Is.EqualTo(0f));
        }

        [Test]
        public void BuildResult_ScoreAboveOne_ClampsToOne()
        {
            var result = Build(score: 1.5f);
            Assert.That(result.NormalizedComplexity, Is.EqualTo(1f));
        }

        [Test]
        public void BuildResult_ExactlyZero_ReturnsZero()
        {
            var result = Build(score: 0f);
            Assert.That(result.NormalizedComplexity, Is.EqualTo(0f));
        }

        [Test]
        public void BuildResult_ExactlyOne_ReturnsOne()
        {
            var result = Build(score: 1f);
            Assert.That(result.NormalizedComplexity, Is.EqualTo(1f));
        }

        #endregion

        #region Emission Boost

        [Test]
        public void BuildResult_Emission_AppliesBoostDivisor()
        {
            // score 0.45 / 0.9 = 0.5
            var result = Build(score: 0.45f, isEmission: true);
            Assert.That(result.NormalizedComplexity, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void BuildResult_EmissionHighScore_ClampsToOne()
        {
            // score 0.95 / 0.9 = 1.0556... → clamped to 1.0
            var result = Build(score: 0.95f, isEmission: true);
            Assert.That(result.NormalizedComplexity, Is.EqualTo(1f));
        }

        [Test]
        public void BuildResult_EmissionAndNormalMap_NoBoost()
        {
            // When both isEmission and isNormalMap, no boost is applied
            var result = Build(score: 0.45f, isEmission: true, isNormalMap: true);
            Assert.That(result.NormalizedComplexity, Is.EqualTo(0.45f).Within(0.001f));
        }

        [Test]
        public void BuildResult_NonEmission_NoBoost()
        {
            var result = Build(score: 0.45f, isEmission: false);
            Assert.That(result.NormalizedComplexity, Is.EqualTo(0.45f).Within(0.001f));
        }

        #endregion

        #region Alpha Flag

        [Test]
        public void BuildResult_HasSignificantAlphaTrue_IsPreserved()
        {
            var result = Build(score: 0.5f, hasSignificantAlpha: true);
            Assert.IsTrue(result.HasSignificantAlpha);
        }

        [Test]
        public void BuildResult_HasSignificantAlphaFalse_IsPreserved()
        {
            var result = Build(score: 0.5f, hasSignificantAlpha: false);
            Assert.IsFalse(result.HasSignificantAlpha);
        }

        #endregion

        #region Divisor and Resolution

        [Test]
        public void BuildResult_RecommendedDivisor_IsInValidRange()
        {
            var result = Build(score: 0.5f);
            Assert.That(result.RecommendedDivisor, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.RecommendedDivisor, Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void BuildResult_HighScore_GetsLowDivisor()
        {
            var result = Build(score: 0.9f);
            Assert.That(result.RecommendedDivisor, Is.EqualTo(1));
        }

        [Test]
        public void BuildResult_LowScore_GetsHighDivisor()
        {
            var result = Build(score: 0.1f);
            Assert.That(result.RecommendedDivisor, Is.EqualTo(8));
        }

        [Test]
        public void BuildResult_Resolution_IsPositive()
        {
            var result = Build(score: 0.5f, sourceWidth: 256, sourceHeight: 256);
            Assert.That(result.RecommendedResolution.x, Is.GreaterThan(0));
            Assert.That(result.RecommendedResolution.y, Is.GreaterThan(0));
        }

        [Test]
        public void BuildResult_Resolution_RespectsProcessorMaximum()
        {
            var result = Build(score: 1f, sourceWidth: 4096, sourceHeight: 4096);
            Assert.That(result.RecommendedResolution.x, Is.LessThanOrEqualTo(2048));
            Assert.That(result.RecommendedResolution.y, Is.LessThanOrEqualTo(2048));
        }

        #endregion

        #region Helpers

        private TextureAnalysisResult Build(
            float score,
            int sourceWidth = 256,
            int sourceHeight = 256,
            bool isEmission = false,
            bool isNormalMap = false,
            bool hasSignificantAlpha = false
        )
        {
            return AnalysisResultHelper.BuildResult(
                score,
                sourceWidth,
                sourceHeight,
                isEmission,
                isNormalMap,
                hasSignificantAlpha,
                _complexityCalc,
                _processor
            );
        }

        #endregion
    }
}
