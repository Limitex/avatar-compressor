using NUnit.Framework;
using dev.limitex.avatar.compressor.texture;

namespace dev.limitex.avatar.compressor.tests
{
    [TestFixture]
    public class AnalysisConstantsTests
    {
        [Test]
        public void DctBlockSize_IsPowerOfTwo()
        {
            Assert.That(IsPowerOfTwo(AnalysisConstants.DctBlockSize), Is.True);
        }

        [Test]
        public void DctBlockSize_IsStandardJpegSize()
        {
            Assert.AreEqual(8, AnalysisConstants.DctBlockSize);
        }

        [Test]
        public void HistogramBins_Is8BitPrecision()
        {
            Assert.AreEqual(256, AnalysisConstants.HistogramBins);
        }

        [Test]
        public void MaxSampledPixels_Is512x512()
        {
            Assert.AreEqual(512 * 512, AnalysisConstants.MaxSampledPixels);
        }

        [Test]
        public void FastStrategyWeights_SumToOne()
        {
            float sum = AnalysisConstants.FastGradientWeight +
                        AnalysisConstants.FastSpatialFrequencyWeight +
                        AnalysisConstants.FastColorVarianceWeight;

            Assert.That(sum, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void HighAccuracyStrategyWeights_SumToOne()
        {
            float sum = AnalysisConstants.HighAccuracyDctWeight +
                        AnalysisConstants.HighAccuracyContrastWeight +
                        AnalysisConstants.HighAccuracyHomogeneityWeight +
                        AnalysisConstants.HighAccuracyEnergyWeight +
                        AnalysisConstants.HighAccuracyEntropyWeight;

            Assert.That(sum, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void PerceptualStrategyWeights_SumToOne()
        {
            float sum = AnalysisConstants.PerceptualVarianceWeight +
                        AnalysisConstants.PerceptualEdgeWeight +
                        AnalysisConstants.PerceptualDetailWeight;

            Assert.That(sum, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void DefaultComplexityScore_IsInValidRange()
        {
            Assert.That(AnalysisConstants.DefaultComplexityScore, Is.InRange(0f, 1f));
        }

        [Test]
        public void DefaultComplexityScore_IsMediumValue()
        {
            Assert.AreEqual(0.5f, AnalysisConstants.DefaultComplexityScore);
        }

        [Test]
        public void MinAnalysisDimension_IsPositive()
        {
            Assert.That(AnalysisConstants.MinAnalysisDimension, Is.GreaterThan(0));
        }

        [Test]
        public void MinOpaquePixelsThresholds_AreOrdered()
        {
            Assert.That(AnalysisConstants.MinOpaquePixelsForAnalysis,
                Is.LessThanOrEqualTo(AnalysisConstants.MinOpaquePixelsForStandardAnalysis));
        }

        [Test]
        public void NormalizationBounds_AreOrdered()
        {
            // Gradient bounds
            Assert.That(AnalysisConstants.GradientPercentileLow,
                Is.LessThan(AnalysisConstants.GradientPercentileHigh));

            // Spatial frequency bounds
            Assert.That(AnalysisConstants.SpatialFreqPercentileLow,
                Is.LessThan(AnalysisConstants.SpatialFreqPercentileHigh));

            // Color variance bounds
            Assert.That(AnalysisConstants.ColorVariancePercentileLow,
                Is.LessThan(AnalysisConstants.ColorVariancePercentileHigh));

            // Entropy bounds
            Assert.That(AnalysisConstants.EntropyPercentileLow,
                Is.LessThan(AnalysisConstants.EntropyPercentileHigh));

            // Contrast bounds
            Assert.That(AnalysisConstants.ContrastPercentileLow,
                Is.LessThan(AnalysisConstants.ContrastPercentileHigh));

            // Variance bounds
            Assert.That(AnalysisConstants.VariancePercentileLow,
                Is.LessThan(AnalysisConstants.VariancePercentileHigh));

            // Edge bounds
            Assert.That(AnalysisConstants.EdgePercentileLow,
                Is.LessThan(AnalysisConstants.EdgePercentileHigh));
        }

        [Test]
        public void ZeroWeightThreshold_IsPositiveAndSmall()
        {
            Assert.That(AnalysisConstants.ZeroWeightThreshold, Is.GreaterThan(0f));
            Assert.That(AnalysisConstants.ZeroWeightThreshold, Is.LessThan(0.01f));
        }

        [Test]
        public void NormalMapVariationMultiplier_IsPositive()
        {
            Assert.That(AnalysisConstants.NormalMapVariationMultiplier, Is.GreaterThan(0f));
        }

        [Test]
        public void CombinedDefaultWeights_SumToOne()
        {
            float sum = AnalysisConstants.CombinedDefaultFastWeight +
                        AnalysisConstants.CombinedDefaultHighAccuracyWeight +
                        AnalysisConstants.CombinedDefaultPerceptualWeight;

            Assert.That(sum, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void CombinedDefaultWeights_ArePositive()
        {
            Assert.That(AnalysisConstants.CombinedDefaultFastWeight, Is.GreaterThanOrEqualTo(0f));
            Assert.That(AnalysisConstants.CombinedDefaultHighAccuracyWeight, Is.GreaterThanOrEqualTo(0f));
            Assert.That(AnalysisConstants.CombinedDefaultPerceptualWeight, Is.GreaterThanOrEqualTo(0f));
        }

        private static bool IsPowerOfTwo(int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }
    }
}
