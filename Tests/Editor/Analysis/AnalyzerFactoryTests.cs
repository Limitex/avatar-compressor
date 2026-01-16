using System;
using NUnit.Framework;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AnalyzerFactoryTests
    {
        #region Create Tests - Strategy Types

        [Test]
        public void Create_FastStrategy_ReturnsFastAnalysisStrategy()
        {
            var analyzer = AnalyzerFactory.Create(AnalysisStrategyType.Fast);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<FastAnalysisStrategy>(analyzer);
        }

        [Test]
        public void Create_HighAccuracyStrategy_ReturnsHighAccuracyStrategy()
        {
            var analyzer = AnalyzerFactory.Create(AnalysisStrategyType.HighAccuracy);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<HighAccuracyStrategy>(analyzer);
        }

        [Test]
        public void Create_PerceptualStrategy_ReturnsPerceptualStrategy()
        {
            var analyzer = AnalyzerFactory.Create(AnalysisStrategyType.Perceptual);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<PerceptualStrategy>(analyzer);
        }

        [Test]
        public void Create_CombinedStrategy_ReturnsCombinedStrategy()
        {
            var analyzer = AnalyzerFactory.Create(AnalysisStrategyType.Combined);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<CombinedStrategy>(analyzer);
        }

        [Test]
        public void Create_InvalidStrategyType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                AnalyzerFactory.Create((AnalysisStrategyType)999);
            });
        }

        #endregion

        #region Create Tests - Combined Strategy Weights

        [Test]
        public void Create_CombinedWithDefaultWeights_UsesDefaultWeights()
        {
            var analyzer = AnalyzerFactory.Create(AnalysisStrategyType.Combined);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<CombinedStrategy>(analyzer);
        }

        [Test]
        public void Create_CombinedWithCustomWeights_AcceptsCustomWeights()
        {
            var analyzer = AnalyzerFactory.Create(
                AnalysisStrategyType.Combined,
                fastWeight: 0.5f,
                highAccuracyWeight: 0.3f,
                perceptualWeight: 0.2f);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<CombinedStrategy>(analyzer);
        }

        [Test]
        public void Create_CombinedWithZeroWeights_AcceptsZeroWeights()
        {
            var analyzer = AnalyzerFactory.Create(
                AnalysisStrategyType.Combined,
                fastWeight: 0f,
                highAccuracyWeight: 1f,
                perceptualWeight: 0f);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<CombinedStrategy>(analyzer);
        }

        [Test]
        public void Create_CombinedWithAllZeroWeights_StillCreates()
        {
            var analyzer = AnalyzerFactory.Create(
                AnalysisStrategyType.Combined,
                fastWeight: 0f,
                highAccuracyWeight: 0f,
                perceptualWeight: 0f);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<CombinedStrategy>(analyzer);
        }

        #endregion

        #region Create Tests - Non-Combined Strategies Ignore Weights

        [Test]
        public void Create_FastWithWeights_IgnoresWeights()
        {
            var analyzer = AnalyzerFactory.Create(
                AnalysisStrategyType.Fast,
                fastWeight: 0.9f,
                highAccuracyWeight: 0.05f,
                perceptualWeight: 0.05f);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<FastAnalysisStrategy>(analyzer);
        }

        [Test]
        public void Create_HighAccuracyWithWeights_IgnoresWeights()
        {
            var analyzer = AnalyzerFactory.Create(
                AnalysisStrategyType.HighAccuracy,
                fastWeight: 0.1f,
                highAccuracyWeight: 0.8f,
                perceptualWeight: 0.1f);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<HighAccuracyStrategy>(analyzer);
        }

        [Test]
        public void Create_PerceptualWithWeights_IgnoresWeights()
        {
            var analyzer = AnalyzerFactory.Create(
                AnalysisStrategyType.Perceptual,
                fastWeight: 0.2f,
                highAccuracyWeight: 0.2f,
                perceptualWeight: 0.6f);

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<PerceptualStrategy>(analyzer);
        }

        #endregion

        #region CreateNormalMapAnalyzer Tests

        [Test]
        public void CreateNormalMapAnalyzer_ReturnsNormalMapAnalyzer()
        {
            var analyzer = AnalyzerFactory.CreateNormalMapAnalyzer();

            Assert.IsNotNull(analyzer);
            Assert.IsInstanceOf<NormalMapAnalyzer>(analyzer);
        }

        [Test]
        public void CreateNormalMapAnalyzer_ReturnsNewInstanceEachTime()
        {
            var analyzer1 = AnalyzerFactory.CreateNormalMapAnalyzer();
            var analyzer2 = AnalyzerFactory.CreateNormalMapAnalyzer();

            Assert.AreNotSame(analyzer1, analyzer2);
        }

        #endregion

        #region Create Tests - Returns New Instance Each Time

        [Test]
        public void Create_SameType_ReturnsNewInstanceEachTime()
        {
            var analyzer1 = AnalyzerFactory.Create(AnalysisStrategyType.Fast);
            var analyzer2 = AnalyzerFactory.Create(AnalysisStrategyType.Fast);

            Assert.AreNotSame(analyzer1, analyzer2);
        }

        [Test]
        public void Create_AllTypes_ReturnDifferentTypes()
        {
            var fast = AnalyzerFactory.Create(AnalysisStrategyType.Fast);
            var highAccuracy = AnalyzerFactory.Create(AnalysisStrategyType.HighAccuracy);
            var perceptual = AnalyzerFactory.Create(AnalysisStrategyType.Perceptual);
            var combined = AnalyzerFactory.Create(AnalysisStrategyType.Combined);
            var normalMap = AnalyzerFactory.CreateNormalMapAnalyzer();

            Assert.AreNotEqual(fast.GetType(), highAccuracy.GetType());
            Assert.AreNotEqual(fast.GetType(), perceptual.GetType());
            Assert.AreNotEqual(fast.GetType(), combined.GetType());
            Assert.AreNotEqual(fast.GetType(), normalMap.GetType());
            Assert.AreNotEqual(highAccuracy.GetType(), perceptual.GetType());
            Assert.AreNotEqual(highAccuracy.GetType(), combined.GetType());
            Assert.AreNotEqual(perceptual.GetType(), combined.GetType());
        }

        #endregion

        #region Interface Implementation Tests

        [Test]
        public void Create_AllStrategies_ImplementITextureComplexityAnalyzer()
        {
            var strategies = new[]
            {
                AnalyzerFactory.Create(AnalysisStrategyType.Fast),
                AnalyzerFactory.Create(AnalysisStrategyType.HighAccuracy),
                AnalyzerFactory.Create(AnalysisStrategyType.Perceptual),
                AnalyzerFactory.Create(AnalysisStrategyType.Combined),
                AnalyzerFactory.CreateNormalMapAnalyzer()
            };

            foreach (var strategy in strategies)
            {
                Assert.IsInstanceOf<ITextureComplexityAnalyzer>(strategy);
            }
        }

        #endregion
    }
}
